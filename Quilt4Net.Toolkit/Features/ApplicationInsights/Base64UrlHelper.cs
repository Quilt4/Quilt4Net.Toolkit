using System.Text;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public class Base64UrlHelper
{
    public static string EncodeToBase64Url(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Convert the input string to bytes
        byte[] bytes = Encoding.UTF8.GetBytes(input);

        // Convert to Base64
        string base64 = Convert.ToBase64String(bytes);

        // Make it URL-safe by replacing special characters and removing padding
        string base64Url = base64
            .Replace("+", "-")  // Replace '+' with '-'
            .Replace("/", "_")  // Replace '/' with '_'
            .TrimEnd('=');      // Remove padding '='

        return base64Url;
    }

    public static string DecodeFromBase64Url(string base64Url)
    {
        if (string.IsNullOrEmpty(base64Url))
            return string.Empty;

        // Convert Base64 URL-safe to standard Base64 by restoring characters and padding
        string base64 = base64Url
            .Replace("-", "+")  // Replace '-' with '+'
            .Replace("_", "/"); // Replace '_' with '/'

        // Add padding if necessary
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        // Convert the Base64 string back to bytes
        byte[] bytes = Convert.FromBase64String(base64);

        // Convert bytes to string
        return Encoding.UTF8.GetString(bytes);
    }
}