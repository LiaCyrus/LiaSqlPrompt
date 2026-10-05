namespace SqlEngine.Ast
{
  public enum StatementType
  {
    Unknown,
    Select,
    Insert,
    Update,
    Delete,
    Merge,
    Execute
  }

  public enum JoinType
  {
    Inner,
    LeftOuter,
    RightOuter,
    FullOuter,
    Cross,
    CrossApply,
    OuterApply
  }

  public enum OrderDirection
  {
    Ascending,
    Descending
  }

  public enum BinaryOperator
  {
    None,
    // Arithmetic
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,

    // Comparison
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,

    // Logical
    And,
    Or
  }

  public enum UnaryOperator
  {
    None,
    Not,
    Positive,
    Negative,
    BitwiseNot
  }
}
