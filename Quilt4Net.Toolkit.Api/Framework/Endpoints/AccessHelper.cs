namespace Quilt4Net.Toolkit.Api.Framework.Endpoints;

public static class AccessHelper
{
    private const string Symbols = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // base36

    // Map from reindexed table (0–6)
    private static readonly AccessFlags[] _indexToAccess =
    [
        new(false, false, false), // 0
        new(true,  false, false), // 1
        new(false, true,  false), // 2
        new(true,  true,  false), // 3
        new(true,  false, true),  // 4
        new(false, true,  true),  // 5
        new(true,  true,  true)   // 6
    ];

    /// <summary>
    /// Decode the endpoint string to a list of access flags.
    /// </summary>
    /// <param name="encoded"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Dictionary<HealthEndpoint, AccessFlags> Decode(string encoded)
    {
        var endpoints = Enum.GetValues<HealthEndpoint>();

        if (encoded == null || encoded.Length < endpoints.Length)
        {
            encoded = $"{encoded}{new string('0', endpoints.Length - (encoded?.Length ?? 0))}";
        }
        else if (encoded.Length > endpoints.Length)
        {
            encoded = encoded.Substring(0, endpoints.Length);
        }
        if (encoded.Length != endpoints.Length) throw new ArgumentException($"Encoded string must be {endpoints.Length} characters.");

        var result = new Dictionary<HealthEndpoint, AccessFlags>();

        for (var i = 0; i < endpoints.Length; i++)
        {
            var index = Symbols.IndexOf(char.ToUpperInvariant(encoded[i]));
            if (index < 0 || index > 6) throw new ArgumentException($"Invalid value '{encoded[i]}' at position {i}");

            result[endpoints[i]] = _indexToAccess[index];
        }

        return result;
    }

    /// <summary>
    /// Encode endpoint access flags to string.
    /// </summary>
    /// <param name="accessMap"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string Encode(this Dictionary<HealthEndpoint, AccessFlags> accessMap)
    {
        var endpoints = Enum.GetValues<HealthEndpoint>();
        var chars = new char[7];

        for (var i = 0; i < endpoints.Length; i++)
        {
            var access = accessMap[endpoints[i]];

            var index = Array.FindIndex(_indexToAccess, a =>
                a.Get == access.Get &&
                a.Head == access.Head &&
                a.Visible == access.Visible);

            if (index < 0) throw new InvalidOperationException("Invalid AccessFlags combination.");

            chars[i] = Symbols[index];
        }

        return new string(chars);
    }
}