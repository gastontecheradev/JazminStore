using System.Globalization;
using System.Text;

namespace Jazmin.Services;

public static class SlugHelper
{
    public static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        var ascii = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var result = new StringBuilder();
        foreach (var ch in ascii)
        {
            if (char.IsLetterOrDigit(ch)) result.Append(ch);
            else if (ch == ' ' || ch == '-' || ch == '_') result.Append('-');
        }
        var s = System.Text.RegularExpressions.Regex.Replace(result.ToString(), "-+", "-").Trim('-');
        return string.IsNullOrEmpty(s) ? "item" : s;
    }
}

public static class OrderNumberHelper
{
    public static string Generate() =>
        $"J{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
