namespace Quilt4Net.Toolkit.Framework;

internal static class StringExtensions
{
    public static string NullIfEmpty(this string item)
    {
        if (string.IsNullOrEmpty(item)) return null;
        return item;
    }
}