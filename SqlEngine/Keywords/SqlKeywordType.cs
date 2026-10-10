using System;

namespace SqlEngine
{
  /// <summary>
  /// Categories of SQL keywords used for contextual IntelliSense completion and syntax formatting.
  /// </summary>
  public enum SqlKeywordType
  {
    None = 0,

    // Core categories
    GeneralKeyword,
    Function,
    DataType,
    Custom,

    // Contextual categories
    StatementStart,
    QueryClause,
    WhereExpression,
    JoinCondition,
    OrderByExpression,
    SelectExpression,

    // DML
    InsertInto,
    InsertValues,
    UpdateSet,
    UpdateClause,
    DeleteClause,
    MergeInto,
    MergeUsing,
    MergeWhen,
    MergeAction,

    // DDL & Objects
    CreateObject,
    AlterObject,
    AlterTableAction,
    DropObject,

    // Scripting / Control Flow
    Transaction,
    WaitFor,
    SetOption,
    Cte
  }
}
