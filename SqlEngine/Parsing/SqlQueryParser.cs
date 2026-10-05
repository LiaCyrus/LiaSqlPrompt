using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlEngine.Ast;

namespace SqlEngine.Parsing
{
  public sealed class SqlQueryParser
  {
    public static SqlQueryResult Parse(string sql)
    {
      var result = new SqlQueryResult();
      if (string.IsNullOrWhiteSpace(sql))
        return result;

      var parser = new TSql160Parser(initialQuotedIdentifiers: true);
      TSqlFragment fragment;
      IList<ParseError> errors;

      using (var reader = new StringReader(sql))
      {
        fragment = parser.Parse(reader, out errors);
      }

      if (errors != null)
      {
        foreach (var err in errors)
        {
          result.Errors.Add(new SqlParseError(err.Message, err.Line, err.Column, err.Offset));
        }
      }

      if (fragment is TSqlScript script)
      {
        foreach (var batch in script.Batches)
        {
          foreach (var stmt in batch.Statements)
          {
            var converted = ConvertStatement(stmt);
            if (converted != null)
            {
              result.Statements.Add(converted);
            }
          }
        }
      }
      else if (fragment is TSqlStatement singleStmt)
      {
        var converted = ConvertStatement(singleStmt);
        if (converted != null)
        {
          result.Statements.Add(converted);
        }
      }

      return result;
    }

    /// <summary>
    /// Uses SQL Server ScriptDom parser to tokenize SQL text into TSqlParserTokens.
    /// </summary>
    public static IList<TSqlParserToken> Tokenize(string sql)
    {
      if (string.IsNullOrEmpty(sql))
        return Array.Empty<TSqlParserToken>();

      var parser = new TSql160Parser(initialQuotedIdentifiers: true);
      using (var reader = new StringReader(sql))
      {
        IList<ParseError> errors;
        return parser.GetTokenStream(reader, out errors) ?? Array.Empty<TSqlParserToken>();
      }
    }

    /// <summary>
    /// Uses the SQL parser to tokenize SQL text into strongly-typed SqlParsedToken objects,
    /// identifying keywords, identifiers, dots, whitespace, and comments.
    /// </summary>
    public static IList<SqlParsedToken> ParseTokens(string sql)
    {
      if (string.IsNullOrEmpty(sql))
        return Array.Empty<SqlParsedToken>();

      var rawTokens = Tokenize(sql);
      var list = new List<SqlParsedToken>(rawTokens.Count);

      foreach (var t in rawTokens)
      {
        if (t == null) continue;

        SqlParserTokenType pType;
        bool isKeyword = false;

        switch (t.TokenType)
        {
          case TSqlTokenType.WhiteSpace:
            pType = SqlParserTokenType.Whitespace;
            break;

          case TSqlTokenType.SingleLineComment:
          case TSqlTokenType.MultilineComment:
            pType = SqlParserTokenType.Comment;
            break;

          case TSqlTokenType.Dot:
            pType = SqlParserTokenType.Dot;
            break;

          case TSqlTokenType.AsciiStringLiteral:
          case TSqlTokenType.UnicodeStringLiteral:
            pType = SqlParserTokenType.StringLiteral;
            break;

          case TSqlTokenType.Integer:
          case TSqlTokenType.Numeric:
          case TSqlTokenType.Real:
          case TSqlTokenType.Money:
            pType = SqlParserTokenType.NumberLiteral;
            break;

          case TSqlTokenType.EndOfFile:
            pType = SqlParserTokenType.EndOfFile;
            break;

          case TSqlTokenType.Identifier:
          case TSqlTokenType.QuotedIdentifier:
            // Check if it's a known SQL built-in function or configured keyword
            // Note: quoted identifiers like [SELECT] are never keywords.
            if (t.TokenType == TSqlTokenType.Identifier && SqlKeywords.IsKeyword(t.Text))
            {
              pType = SqlParserTokenType.Keyword;
              isKeyword = true;
            }
            else
            {
              pType = SqlParserTokenType.Identifier;
            }
            break;

          case TSqlTokenType.LeftParenthesis:
            pType = SqlParserTokenType.LeftParenthesis;
            break;

          case TSqlTokenType.RightParenthesis:
            pType = SqlParserTokenType.RightParenthesis;
            break;

          case TSqlTokenType.Comma:
          case TSqlTokenType.Semicolon:
          case TSqlTokenType.Colon:
          case TSqlTokenType.LeftCurly:
          case TSqlTokenType.RightCurly:
          case TSqlTokenType.EqualsSign:
          case TSqlTokenType.Plus:
          case TSqlTokenType.Minus:
          case TSqlTokenType.Star:
          case TSqlTokenType.Divide:
          case TSqlTokenType.Percent:
          case TSqlTokenType.LessThan:
          case TSqlTokenType.GreaterThan:
          case TSqlTokenType.Bang:
          case TSqlTokenType.Tilde:
          case TSqlTokenType.Ampersand:
          case TSqlTokenType.Circumflex:
            pType = SqlParserTokenType.Symbol;
            break;

          case TSqlTokenType.Variable:
            pType = SqlParserTokenType.Identifier;
            break;

          default:
            // All other ScriptDom tokens are SQL language keywords/reserved words
            // (e.g. Select, From, Where, Exists, Join, Left, Inner, Order, Group, By, Having, etc.)
            pType = SqlParserTokenType.Keyword;
            isKeyword = true;
            break;
        }

        list.Add(new SqlParsedToken(pType, t.Text, t.Offset, isKeyword));
      }

      return list;
    }

    private static SourceSpan CreateSpan(TSqlFragment fragment)
    {
      if (fragment == null)
        return default;

      return new SourceSpan(
        fragment.StartOffset,
        fragment.FragmentLength,
        fragment.StartLine,
        fragment.StartColumn);
    }

    private static SqlStatement? ConvertStatement(TSqlStatement stmt)
    {
      if (stmt == null)
        return null;

      if (stmt is Microsoft.SqlServer.TransactSql.ScriptDom.SelectStatement domSelect)
      {
        return ConvertSelectStatement(domSelect);
      }
      if (stmt is Microsoft.SqlServer.TransactSql.ScriptDom.InsertStatement domInsert)
      {
        return ConvertInsertStatement(domInsert);
      }
      if (stmt is Microsoft.SqlServer.TransactSql.ScriptDom.UpdateStatement domUpdate)
      {
        return ConvertUpdateStatement(domUpdate);
      }
      if (stmt is Microsoft.SqlServer.TransactSql.ScriptDom.DeleteStatement domDelete)
      {
        return ConvertDeleteStatement(domDelete);
      }
      if (stmt is Microsoft.SqlServer.TransactSql.ScriptDom.MergeStatement domMerge)
      {
        return ConvertMergeStatement(domMerge);
      }
      if (stmt is Microsoft.SqlServer.TransactSql.ScriptDom.ExecuteStatement domExec)
      {
        return ConvertExecuteStatement(domExec);
      }

      return new UnknownStatement
      {
        Span = CreateSpan(stmt),
        RawSql = stmt.ToString()
      };
    }

    private static Ast.SelectStatement ConvertSelectStatement(Microsoft.SqlServer.TransactSql.ScriptDom.SelectStatement dom)
    {
      var stmt = new Ast.SelectStatement
      {
        Span = CreateSpan(dom)
      };

      if (dom.WithCtesAndXmlNamespaces?.CommonTableExpressions != null)
      {
        foreach (var cte in dom.WithCtesAndXmlNamespaces.CommonTableExpressions)
        {
          var cteDef = new CteDefinition
          {
            Span = CreateSpan(cte),
            Name = cte.ExpressionName?.Value ?? string.Empty
          };

          if (cte.Columns != null)
          {
            foreach (var col in cte.Columns)
            {
              cteDef.ColumnNames.Add(col.Value);
            }
          }

          if (cte.QueryExpression is QuerySpecification cteQuery)
          {
            cteDef.Query = ConvertQuerySpecification(cteQuery);
          }

          stmt.Ctes.Add(cteDef);
        }
      }

      if (dom.QueryExpression is QuerySpecification querySpec)
      {
        PopulateQuerySpecification(querySpec, stmt);
        if (querySpec.OrderByClause != null)
        {
          stmt.OrderBy = ConvertOrderByClause(querySpec.OrderByClause);
        }
      }
      else if (dom.QueryExpression?.OrderByClause != null)
      {
        stmt.OrderBy = ConvertOrderByClause(dom.QueryExpression.OrderByClause);
      }

      return stmt;
    }

    private static Ast.SelectStatement ConvertQuerySpecification(QuerySpecification spec)
    {
      var stmt = new Ast.SelectStatement
      {
        Span = CreateSpan(spec)
      };
      PopulateQuerySpecification(spec, stmt);
      return stmt;
    }

    private static void PopulateQuerySpecification(QuerySpecification spec, Ast.SelectStatement stmt)
    {
      // 1. SELECT clause
      var selectClause = new Ast.SelectClause
      {
        Span = CreateSpan(spec),
        IsDistinct = spec.UniqueRowFilter == UniqueRowFilter.Distinct
      };

      if (spec.TopRowFilter != null)
      {
        selectClause.TopExpression = ConvertExpression(spec.TopRowFilter.Expression);
        selectClause.TopPercent = spec.TopRowFilter.Percent;
      }

      if (spec.SelectElements != null)
      {
        foreach (var elem in spec.SelectElements)
        {
          if (elem is SelectScalarExpression scalar)
          {
            var item = new SelectItem
            {
              Span = CreateSpan(scalar),
              Expression = ConvertExpression(scalar.Expression)!,
              Alias = scalar.ColumnName?.Value
            };
            selectClause.Items.Add(item);
          }
          else if (elem is SelectStarExpression star)
          {
            var item = new SelectItem
            {
              Span = CreateSpan(star),
              Expression = new StarExpression
              {
                Span = CreateSpan(star),
                TableOrAlias = star.Qualifier != null ? ConvertMultiPartIdentifier(star.Qualifier) : null
              }
            };
            selectClause.Items.Add(item);
          }
        }
      }
      stmt.Select = selectClause;

      // 2. FROM clause
      if (spec.FromClause != null)
      {
        stmt.From = ConvertFromClause(spec.FromClause);
      }

      // 3. WHERE clause
      if (spec.WhereClause != null)
      {
        stmt.Where = new Ast.WhereClause
        {
          Span = CreateSpan(spec.WhereClause),
          Condition = ConvertBooleanExpression(spec.WhereClause.SearchCondition)!
        };
      }

      // 4. GROUP BY clause
      if (spec.GroupByClause != null)
      {
        var groupBy = new Ast.GroupByClause
        {
          Span = CreateSpan(spec.GroupByClause)
        };
        foreach (var grp in spec.GroupByClause.GroupingSpecifications)
        {
          if (grp is ExpressionGroupingSpecification exprGrp)
          {
            var expr = ConvertExpression(exprGrp.Expression);
            if (expr != null)
            {
              groupBy.GroupingExpressions.Add(expr);
            }
          }
        }
        stmt.GroupBy = groupBy;
      }

      // 5. HAVING clause
      if (spec.HavingClause != null)
      {
        stmt.Having = new Ast.HavingClause
        {
          Span = CreateSpan(spec.HavingClause),
          Condition = ConvertBooleanExpression(spec.HavingClause.SearchCondition)!
        };
      }

      // 6. ORDER BY clause (if part of query spec)
      if (spec.OrderByClause != null)
      {
        stmt.OrderBy = ConvertOrderByClause(spec.OrderByClause);
      }
    }

    private static Ast.FromClause ConvertFromClause(Microsoft.SqlServer.TransactSql.ScriptDom.FromClause domFrom)
    {
      var from = new Ast.FromClause
      {
        Span = CreateSpan(domFrom)
      };

      if (domFrom.TableReferences != null)
      {
        foreach (var tblRef in domFrom.TableReferences)
        {
          ProcessTableReference(tblRef, from);
        }
      }

      return from;
    }

    private static void ProcessTableReference(TableReference tblRef, Ast.FromClause from)
    {
      if (tblRef is QualifiedJoin qualJoin)
      {
        // Recursively unpack the FirstTableReference
        ProcessTableReference(qualJoin.FirstTableReference, from);

        var join = new Ast.JoinClause
        {
          Span = CreateSpan(qualJoin),
          JoinType = ConvertQualifiedJoinType(qualJoin.QualifiedJoinType),
          Source = ConvertSingleTableReference(qualJoin.SecondTableReference),
          Condition = ConvertBooleanExpression(qualJoin.SearchCondition)
        };
        from.Joins.Add(join);
      }
      else if (tblRef is UnqualifiedJoin unqualJoin)
      {
        ProcessTableReference(unqualJoin.FirstTableReference, from);

        var join = new Ast.JoinClause
        {
          Span = CreateSpan(unqualJoin),
          JoinType = ConvertUnqualifiedJoinType(unqualJoin.UnqualifiedJoinType),
          Source = ConvertSingleTableReference(unqualJoin.SecondTableReference)
        };
        from.Joins.Add(join);
      }
      else
      {
        var src = ConvertSingleTableReference(tblRef);
        if (from.PrimarySource == null)
        {
          from.PrimarySource = src;
        }
        else
        {
          // Additional comma-delimited table source (equivalent to CROSS JOIN)
          from.Joins.Add(new Ast.JoinClause
          {
            Span = CreateSpan(tblRef),
            JoinType = Ast.JoinType.Cross,
            Source = src
          });
        }
      }
    }

    private static Ast.TableSource ConvertSingleTableReference(TableReference tblRef)
    {
      if (tblRef is NamedTableReference named)
      {
        var nts = new NamedTableSource
        {
          Span = CreateSpan(named),
          Alias = named.Alias?.Value
        };
        PopulateSchemaObjectName(named.SchemaObject, nts);
        return nts;
      }
      if (tblRef is QueryDerivedTable derived)
      {
        var dts = new DerivedTableSource
        {
          Span = CreateSpan(derived),
          Alias = derived.Alias?.Value
        };
        if (derived.QueryExpression is QuerySpecification qSpec)
        {
          dts.Query = ConvertQuerySpecification(qSpec);
        }
        return dts;
      }
      if (tblRef is SchemaObjectFunctionTableReference funcRef)
      {
        var tvf = new TableValuedFunctionSource
        {
          Span = CreateSpan(funcRef),
          Alias = funcRef.Alias?.Value
        };
        var funcExpr = new FunctionCallExpression
        {
          Span = CreateSpan(funcRef),
          Name = funcRef.SchemaObject?.BaseIdentifier?.Value ?? string.Empty
        };
        if (funcRef.Parameters != null)
        {
          foreach (var p in funcRef.Parameters)
          {
            var pExpr = ConvertExpression(p);
            if (pExpr != null) funcExpr.Arguments.Add(pExpr);
          }
        }
        tvf.FunctionCall = funcExpr;
        return tvf;
      }

      return new NamedTableSource
      {
        Span = CreateSpan(tblRef),
        TableName = tblRef?.ToString() ?? string.Empty
      };
    }

    private static void PopulateSchemaObjectName(SchemaObjectName objName, NamedTableSource nts)
    {
      if (objName == null) return;
      nts.TableName = objName.BaseIdentifier?.Value ?? string.Empty;
      nts.Schema = objName.SchemaIdentifier?.Value;
      nts.Database = objName.DatabaseIdentifier?.Value;
      nts.Server = objName.ServerIdentifier?.Value;
    }

    private static Ast.JoinType ConvertQualifiedJoinType(QualifiedJoinType type)
    {
      switch (type)
      {
        case QualifiedJoinType.Inner:
          return Ast.JoinType.Inner;
        case QualifiedJoinType.LeftOuter:
          return Ast.JoinType.LeftOuter;
        case QualifiedJoinType.RightOuter:
          return Ast.JoinType.RightOuter;
        case QualifiedJoinType.FullOuter:
          return Ast.JoinType.FullOuter;
        default:
          return Ast.JoinType.Inner;
      }
    }

    private static Ast.JoinType ConvertUnqualifiedJoinType(UnqualifiedJoinType type)
    {
      switch (type)
      {
        case UnqualifiedJoinType.CrossJoin:
          return Ast.JoinType.Cross;
        case UnqualifiedJoinType.CrossApply:
          return Ast.JoinType.CrossApply;
        case UnqualifiedJoinType.OuterApply:
          return Ast.JoinType.OuterApply;
        default:
          return Ast.JoinType.Cross;
      }
    }

    private static Ast.OrderByClause ConvertOrderByClause(Microsoft.SqlServer.TransactSql.ScriptDom.OrderByClause domOrder)
    {
      var order = new Ast.OrderByClause
      {
        Span = CreateSpan(domOrder)
      };
      if (domOrder.OrderByElements != null)
      {
        foreach (var elem in domOrder.OrderByElements)
        {
          order.Elements.Add(new OrderByElement
          {
            Span = CreateSpan(elem),
            Expression = ConvertExpression(elem.Expression)!,
            Direction = elem.SortOrder == SortOrder.Descending ? OrderDirection.Descending : OrderDirection.Ascending
          });
        }
      }
      return order;
    }

    private static Ast.SqlExpression? ConvertExpression(ScalarExpression scalar)
    {
      if (scalar == null) return null;

      if (scalar is Microsoft.SqlServer.TransactSql.ScriptDom.ColumnReferenceExpression colRef)
      {
        var col = new Ast.ColumnReferenceExpression
        {
          Span = CreateSpan(colRef)
        };
        PopulateColumnReference(colRef.MultiPartIdentifier, col);
        return col;
      }
      if (scalar is Literal lit)
      {
        return new LiteralExpression
        {
          Span = CreateSpan(lit),
          Value = lit.Value,
          IsString = lit.LiteralType == LiteralType.String,
          IsNumeric = lit.LiteralType == LiteralType.Integer || lit.LiteralType == LiteralType.Numeric || lit.LiteralType == LiteralType.Real,
          IsNull = lit.LiteralType == LiteralType.Null
        };
      }
      if (scalar is NullLiteral nullLit)
      {
        return new LiteralExpression
        {
          Span = CreateSpan(nullLit),
          Value = "NULL",
          IsNull = true
        };
      }
      if (scalar is VariableReference varRef)
      {
        return new VariableExpression
        {
          Span = CreateSpan(varRef),
          Name = varRef.Name
        };
      }
      if (scalar is FunctionCall func)
      {
        var funcExpr = new FunctionCallExpression
        {
          Span = CreateSpan(func),
          Name = func.FunctionName?.Value ?? string.Empty,
          IsDistinct = func.UniqueRowFilter == UniqueRowFilter.Distinct
        };
        if (func.Parameters != null)
        {
          foreach (var p in func.Parameters)
          {
            var pExpr = ConvertExpression(p);
            if (pExpr != null) funcExpr.Arguments.Add(pExpr);
          }
        }
        return funcExpr;
      }
      if (scalar is Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpression bin)
      {
        return new Ast.BinaryExpression
        {
          Span = CreateSpan(bin),
          Left = ConvertExpression(bin.FirstExpression)!,
          Operator = ConvertBinaryOperator(bin.BinaryExpressionType),
          Right = ConvertExpression(bin.SecondExpression)!
        };
      }
      if (scalar is Microsoft.SqlServer.TransactSql.ScriptDom.UnaryExpression un)
      {
        return new Ast.UnaryExpression
        {
          Span = CreateSpan(un),
          Operator = ConvertUnaryOperator(un.UnaryExpressionType),
          Operand = ConvertExpression(un.Expression)!
        };
      }
      if (scalar is SearchedCaseExpression searchedCase)
      {
        var caseExpr = new Ast.CaseExpression
        {
          Span = CreateSpan(searchedCase)
        };
        if (searchedCase.WhenClauses != null)
        {
          foreach (var when in searchedCase.WhenClauses)
          {
            caseExpr.WhenClauses.Add(new Ast.WhenClause
            {
              Span = CreateSpan(when),
              Condition = ConvertBooleanExpression(when.WhenExpression)!,
              Result = ConvertExpression(when.ThenExpression)!
            });
          }
        }
        if (searchedCase.ElseExpression != null)
        {
          caseExpr.ElseExpression = ConvertExpression(searchedCase.ElseExpression);
        }
        return caseExpr;
      }
      if (scalar is SimpleCaseExpression simpleCase)
      {
        var caseExpr = new Ast.CaseExpression
        {
          Span = CreateSpan(simpleCase),
          InputExpression = ConvertExpression(simpleCase.InputExpression)
        };
        if (simpleCase.WhenClauses != null)
        {
          foreach (var when in simpleCase.WhenClauses)
          {
            caseExpr.WhenClauses.Add(new Ast.WhenClause
            {
              Span = CreateSpan(when),
              Condition = ConvertExpression(when.WhenExpression)!,
              Result = ConvertExpression(when.ThenExpression)!
            });
          }
        }
        if (simpleCase.ElseExpression != null)
        {
          caseExpr.ElseExpression = ConvertExpression(simpleCase.ElseExpression);
        }
        return caseExpr;
      }
      if (scalar is CastCall cast)
      {
        return new CastExpression
        {
          Span = CreateSpan(cast),
          Expression = ConvertExpression(cast.Parameter)!,
          TargetDataType = cast.DataType?.ToString() ?? string.Empty
        };
      }
      if (scalar is ConvertCall conv)
      {
        return new ConvertExpression
        {
          Span = CreateSpan(conv),
          TargetDataType = conv.DataType?.ToString() ?? string.Empty,
          Expression = ConvertExpression(conv.Parameter)!,
          Style = conv.Style != null ? ConvertExpression(conv.Style) : null
        };
      }
      if (scalar is ScalarSubquery subquery)
      {
        if (subquery.QueryExpression is QuerySpecification qSpec)
        {
          return new SubqueryExpression
          {
            Span = CreateSpan(subquery),
            Subquery = ConvertQuerySpecification(qSpec)
          };
        }
      }

      return new RawTextExpression
      {
        Span = CreateSpan(scalar),
        Text = scalar.ToString()
      };
    }

    private static Ast.SqlExpression? ConvertBooleanExpression(BooleanExpression boolExpr)
    {
      if (boolExpr == null) return null;

      if (boolExpr is BooleanComparisonExpression comp)
      {
        return new Ast.BinaryExpression
        {
          Span = CreateSpan(comp),
          Left = ConvertExpression(comp.FirstExpression)!,
          Operator = ConvertComparisonOperator(comp.ComparisonType),
          Right = ConvertExpression(comp.SecondExpression)!
        };
      }
      if (boolExpr is BooleanBinaryExpression bin)
      {
        return new Ast.BinaryExpression
        {
          Span = CreateSpan(bin),
          Left = ConvertBooleanExpression(bin.FirstExpression)!,
          Operator = bin.BinaryExpressionType == BooleanBinaryExpressionType.And ? BinaryOperator.And : BinaryOperator.Or,
          Right = ConvertBooleanExpression(bin.SecondExpression)!
        };
      }
      if (boolExpr is BooleanNotExpression notExpr)
      {
        return new Ast.UnaryExpression
        {
          Span = CreateSpan(notExpr),
          Operator = UnaryOperator.Not,
          Operand = ConvertBooleanExpression(notExpr.Expression)!
        };
      }
      if (boolExpr is BooleanParenthesisExpression paren)
      {
        // Unpack parenthesis while maintaining precedence
        return ConvertBooleanExpression(paren.Expression);
      }
      if (boolExpr is BooleanIsNullExpression isNull)
      {
        return new IsNullExpression
        {
          Span = CreateSpan(isNull),
          Expression = ConvertExpression(isNull.Expression)!,
          IsNotNull = isNull.IsNot
        };
      }
      if (boolExpr is InPredicate inPred)
      {
        var expr = new InExpression
        {
          Span = CreateSpan(inPred),
          Expression = ConvertExpression(inPred.Expression)!,
          IsNotIn = inPred.NotDefined
        };
        if (inPred.Values != null)
        {
          foreach (var v in inPred.Values)
          {
            var valExpr = ConvertExpression(v);
            if (valExpr != null) expr.Values.Add(valExpr);
          }
        }
        if (inPred.Subquery != null && inPred.Subquery.QueryExpression is QuerySpecification inSpec)
        {
          expr.Subquery = new SubqueryExpression
          {
            Span = CreateSpan(inPred.Subquery),
            Subquery = ConvertQuerySpecification(inSpec)
          };
        }
        return expr;
      }
      if (boolExpr is LikePredicate like)
      {
        return new LikeExpression
        {
          Span = CreateSpan(like),
          Expression = ConvertExpression(like.FirstExpression)!,
          IsNotLike = like.NotDefined,
          Pattern = ConvertExpression(like.SecondExpression)!,
          Escape = like.EscapeExpression != null ? ConvertExpression(like.EscapeExpression) : null
        };
      }
      if (boolExpr is ExistsPredicate exists)
      {
        var existsExpr = new ExistsExpression
        {
          Span = CreateSpan(exists)
        };
        if (exists.Subquery?.QueryExpression is QuerySpecification exSpec)
        {
          existsExpr.Subquery = new SubqueryExpression
          {
            Span = CreateSpan(exists.Subquery),
            Subquery = ConvertQuerySpecification(exSpec)
          };
        }
        return existsExpr;
      }

      return new RawTextExpression
      {
        Span = CreateSpan(boolExpr),
        Text = boolExpr.ToString()
      };
    }

    private static void PopulateColumnReference(MultiPartIdentifier multiPart, Ast.ColumnReferenceExpression col)
    {
      if (multiPart?.Identifiers == null || multiPart.Identifiers.Count == 0)
        return;

      var ids = multiPart.Identifiers;
      int count = ids.Count;

      col.ColumnName = ids[count - 1].Value;

      if (count > 1)
      {
        col.TableOrAlias = ids[count - 2].Value;
      }
      if (count > 2)
      {
        col.Schema = ids[count - 3].Value;
      }
      if (count > 3)
      {
        col.Database = ids[count - 4].Value;
      }
    }

    private static string ConvertMultiPartIdentifier(MultiPartIdentifier multiPart)
    {
      if (multiPart?.Identifiers == null) return string.Empty;
      var list = new List<string>();
      foreach (var id in multiPart.Identifiers)
      {
        list.Add(id.Value);
      }
      return string.Join(".", list);
    }

    private static BinaryOperator ConvertBinaryOperator(BinaryExpressionType type)
    {
      switch (type)
      {
        case BinaryExpressionType.Add: return BinaryOperator.Add;
        case BinaryExpressionType.Subtract: return BinaryOperator.Subtract;
        case BinaryExpressionType.Multiply: return BinaryOperator.Multiply;
        case BinaryExpressionType.Divide: return BinaryOperator.Divide;
        case BinaryExpressionType.Modulo: return BinaryOperator.Modulo;
        case BinaryExpressionType.BitwiseAnd: return BinaryOperator.BitwiseAnd;
        case BinaryExpressionType.BitwiseOr: return BinaryOperator.BitwiseOr;
        case BinaryExpressionType.BitwiseXor: return BinaryOperator.BitwiseXor;
        default: return BinaryOperator.None;
      }
    }

    private static UnaryOperator ConvertUnaryOperator(UnaryExpressionType type)
    {
      switch (type)
      {
        case UnaryExpressionType.Positive: return UnaryOperator.Positive;
        case UnaryExpressionType.Negative: return UnaryOperator.Negative;
        case UnaryExpressionType.BitwiseNot: return UnaryOperator.BitwiseNot;
        default: return UnaryOperator.None;
      }
    }

    private static BinaryOperator ConvertComparisonOperator(BooleanComparisonType type)
    {
      switch (type)
      {
        case BooleanComparisonType.Equals: return BinaryOperator.Equal;
        case BooleanComparisonType.NotEqualToExclamation:
        case BooleanComparisonType.NotEqualToBrackets:
          return BinaryOperator.NotEqual;
        case BooleanComparisonType.GreaterThan: return BinaryOperator.GreaterThan;
        case BooleanComparisonType.LessThan: return BinaryOperator.LessThan;
        case BooleanComparisonType.GreaterThanOrEqualTo: return BinaryOperator.GreaterThanOrEqual;
        case BooleanComparisonType.LessThanOrEqualTo: return BinaryOperator.LessThanOrEqual;
        default: return BinaryOperator.None;
      }
    }

    private static Ast.InsertStatement ConvertInsertStatement(Microsoft.SqlServer.TransactSql.ScriptDom.InsertStatement dom)
    {
      var stmt = new Ast.InsertStatement
      {
        Span = CreateSpan(dom)
      };

      if (dom.InsertSpecification?.Target is NamedTableReference namedTarget)
      {
        stmt.Target = ConvertSingleTableReference(namedTarget);
      }

      if (dom.InsertSpecification?.Columns != null)
      {
        foreach (var c in dom.InsertSpecification.Columns)
        {
          var colExpr = new Ast.ColumnReferenceExpression
          {
            Span = CreateSpan(c)
          };
          PopulateColumnReference(c.MultiPartIdentifier, colExpr);
          stmt.Columns.Add(colExpr);
        }
      }

      if (dom.InsertSpecification?.InsertSource is ValuesInsertSource valuesSrc)
      {
        if (valuesSrc.RowValues != null)
        {
          foreach (var row in valuesSrc.RowValues)
          {
            var rowList = new List<Ast.SqlExpression>();
            foreach (var v in row.ColumnValues)
            {
              var expr = ConvertExpression(v);
              if (expr != null) rowList.Add(expr);
            }
            stmt.Values.Add(rowList);
          }
        }
      }
      else if (dom.InsertSpecification?.InsertSource is SelectInsertSource selectSrc)
      {
        if (selectSrc.Select is QuerySpecification qSpec)
        {
          stmt.SourceSelect = ConvertQuerySpecification(qSpec);
        }
      }

      return stmt;
    }

    private static Ast.UpdateStatement ConvertUpdateStatement(Microsoft.SqlServer.TransactSql.ScriptDom.UpdateStatement dom)
    {
      var stmt = new Ast.UpdateStatement
      {
        Span = CreateSpan(dom)
      };

      if (dom.UpdateSpecification?.Target is NamedTableReference namedTarget)
      {
        stmt.Target = ConvertSingleTableReference(namedTarget);
      }

      if (dom.UpdateSpecification?.SetClauses != null)
      {
        foreach (var sc in dom.UpdateSpecification.SetClauses)
        {
          if (sc is AssignmentSetClause assignment)
          {
            var setAssign = new SetAssignment
            {
              Span = CreateSpan(assignment)
            };
            if (assignment.Column != null)
            {
              var col = new Ast.ColumnReferenceExpression
              {
                Span = CreateSpan(assignment.Column)
              };
              PopulateColumnReference(assignment.Column.MultiPartIdentifier, col);
              setAssign.Column = col;
            }
            if (assignment.NewValue != null)
            {
              setAssign.Value = ConvertExpression(assignment.NewValue)!;
            }
            stmt.SetAssignments.Add(setAssign);
          }
        }
      }

      if (dom.UpdateSpecification?.FromClause != null)
      {
        stmt.From = ConvertFromClause(dom.UpdateSpecification.FromClause);
      }

      if (dom.UpdateSpecification?.WhereClause != null)
      {
        stmt.Where = new Ast.WhereClause
        {
          Span = CreateSpan(dom.UpdateSpecification.WhereClause),
          Condition = ConvertBooleanExpression(dom.UpdateSpecification.WhereClause.SearchCondition)!
        };
      }

      return stmt;
    }

    private static Ast.DeleteStatement ConvertDeleteStatement(Microsoft.SqlServer.TransactSql.ScriptDom.DeleteStatement dom)
    {
      var stmt = new Ast.DeleteStatement
      {
        Span = CreateSpan(dom)
      };

      if (dom.DeleteSpecification?.Target is NamedTableReference namedTarget)
      {
        stmt.Target = ConvertSingleTableReference(namedTarget);
      }

      if (dom.DeleteSpecification?.FromClause != null)
      {
        stmt.From = ConvertFromClause(dom.DeleteSpecification.FromClause);
      }

      if (dom.DeleteSpecification?.WhereClause != null)
      {
        stmt.Where = new Ast.WhereClause
        {
          Span = CreateSpan(dom.DeleteSpecification.WhereClause),
          Condition = ConvertBooleanExpression(dom.DeleteSpecification.WhereClause.SearchCondition)!
        };
      }

      return stmt;
    }

    private static Ast.MergeStatement ConvertMergeStatement(Microsoft.SqlServer.TransactSql.ScriptDom.MergeStatement dom)
    {
      var stmt = new Ast.MergeStatement
      {
        Span = CreateSpan(dom)
      };

      if (dom.MergeSpecification?.Target != null)
      {
        stmt.Target = ConvertSingleTableReference(dom.MergeSpecification.Target);
      }
      if (dom.MergeSpecification?.TableReference != null)
      {
        stmt.Source = ConvertSingleTableReference(dom.MergeSpecification.TableReference);
      }
      if (dom.MergeSpecification?.SearchCondition != null)
      {
        stmt.OnCondition = ConvertBooleanExpression(dom.MergeSpecification.SearchCondition)!;
      }

      return stmt;
    }

    private static Ast.ExecuteStatement ConvertExecuteStatement(Microsoft.SqlServer.TransactSql.ScriptDom.ExecuteStatement dom)
    {
      var stmt = new Ast.ExecuteStatement
      {
        Span = CreateSpan(dom)
      };

      if (dom.ExecuteSpecification?.ExecutableEntity is ExecutableProcedureReference procRef)
      {
        stmt.ProcedureName = procRef.ProcedureReference?.ProcedureReference?.ToString() ?? string.Empty;
        if (procRef.Parameters != null)
        {
          foreach (var p in procRef.Parameters)
          {
            if (p.ParameterValue != null)
            {
              var expr = ConvertExpression(p.ParameterValue);
              if (expr != null) stmt.Parameters.Add(expr);
            }
          }
        }
      }

      return stmt;
    }
  }
}
