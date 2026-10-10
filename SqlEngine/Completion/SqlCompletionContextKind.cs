namespace SqlEngine.Completion
{
  public enum SqlCompletionContextKind
  {
    Unknown,

    // Statement
    StatementStart,

    // Query
    SelectClause,
    SelectExpression,
    FromClause,
    TableSource,
    TableAlias,

    JoinClause,
    JoinCondition,

    WhereClause,
    WhereExpression,

    GroupByClause,
    GroupByExpression,

    HavingClause,
    HavingExpression,

    OrderByClause,
    OrderByExpression,

    QueryClause,
    SetOperator,

    // CTE / Subquery
    CteStart,
    CteName,
    CteColumnList,
    CteQuery,
    Subquery,

    // INSERT
    InsertInto,
    InsertTable,
    InsertColumnList,
    InsertValues,
    InsertValueExpression,
    InsertSelect,

    // UPDATE
    UpdateTable,
    UpdateSet,
    UpdateColumn,
    UpdateValue,
    UpdateFrom,

    // DELETE
    DeleteFrom,
    DeleteTable,
    DeleteWhere,

    // MERGE
    MergeInto,
    MergeTarget,
    MergeUsing,
    MergeSource,
    MergeOn,
    MergeCondition,
    MergeWhen,
    MergeAction,
    MergeValues,

    // DDL
    CreateStatement,
    CreateObjectName,
    CreateTableColumnList,
    CreateTableColumnDefinition,

    AlterStatement,
    AlterTable,
    AlterTableAction,

    DropStatement,
    DropObject,

    // Procedure / Function execution
    ExecuteStatement,
    ExecuteObject,
    ExecuteParameter,

    // Variables & Parameters
    Variable,
    DeclareVariable,
    SetVariable,

    // Expressions & Objects
    Expression,
    ColumnReference,
    FunctionName,
    FunctionArgument,

    // Control flow
    IfCondition,
    WhileCondition,
    BeginBlock,
    EndBlock,

    // Transaction
    TransactionStatement,

    // Error handling
    TryBlock,
    CatchBlock,

    // Administration & Session
    UseDatabase,
    PrintExpression,
    WaitForStatement
  }
}
