using System.Text.RegularExpressions;

namespace PainelFelizCasaNova.Api.Services;

public static class SlugHelper
{
    public static string ToSlug(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var slug = input.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug;
    }
}