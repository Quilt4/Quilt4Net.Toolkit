//namespace Quilt4Net.Toolkit.Api.Features.Dependency;

//internal static class EnumExtensions
//{
//    public static TEnum MaxEnum<TEnum>(params TEnum?[] values) where TEnum : struct, Enum
//    {
//        if (values == null || values.Length == 0) throw new ArgumentException("At least one enum value must be provided.", nameof(values));

//        var nonNullValues = values
//            .Where(v => v.HasValue)
//            .Select(v => v.Value)
//            .ToList();

//        if (nonNullValues.Count == 0) throw new ArgumentException("All provided enum values are null.", nameof(values));

//        return nonNullValues
//            .OrderByDescending(v => Convert.ToInt64(v))
//            .First();
//    }
//}