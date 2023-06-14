namespace NesterovskyBros.VectorIndex;

/// <summary>
/// A range spliting space by a specified dimension into two subregions.
/// </summary>
public readonly record struct Range
{
    /// <summary>
    /// An item id.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// A parent id.
    /// </summary>
    public long ParentId { get; init; }

    /// <summary>
    /// Index of dimension being indexed.
    /// </summary>
    public int Dimension { get; init; }

    /// <summary>
    /// Min point of range.
    /// </summary>
    public float Min { get; init; }

    /// <summary>
    /// Max point of range.
    /// </summary>
    public float Max { get; init; }

    /// <summary>
    /// Optional id of low range.
    /// </summary>
    public long? LowId { get; init; }

    /// <summary>
    /// Optional id of high range.
    /// </summary>
    public long? HightId { get; init; }

    /// <summary>
    /// Optional point id fit into the range.
    /// </summary>
    public long? PointId { get; init; }
}
