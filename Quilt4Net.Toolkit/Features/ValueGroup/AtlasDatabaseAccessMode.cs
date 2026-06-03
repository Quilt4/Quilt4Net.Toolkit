namespace Quilt4Net.Toolkit.Features.ValueGroup;

/// <summary>
/// Privilege level of an <see cref="AtlasDatabaseAccessEntry"/> delivered in a
/// <see cref="ValueGroupBundle"/>: read-only (<c>readAnyDatabase</c>) or read-write
/// (<c>readWriteAnyDatabase</c>).
/// </summary>
public enum AtlasDatabaseAccessMode
{
    ReadOnly = 0,
    ReadWrite = 1,
}
