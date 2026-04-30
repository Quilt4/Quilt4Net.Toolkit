using System.Security.Cryptography;

namespace Quilt4Net.Toolkit.Features.Diagnostics;

/// <summary>
/// Generates short, human-readable incident identifiers for cross-referencing
/// a user-facing error message with a corresponding log entry.
/// 6 chars over a 32-char alphabet, ambiguous characters (0/1/I/O) excluded.
/// </summary>
public static class IncidentId
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string New(int length = 6)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
