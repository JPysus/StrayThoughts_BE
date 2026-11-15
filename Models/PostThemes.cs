namespace StrayThoughts_BE.Models;

public static class PostThemes
{
    public const string Default = "sunny-joy";
    public const string WarmCoral = "warm-coral";
    public const string OceanBreeze = "ocean-breeze";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Default,
        WarmCoral,
        OceanBreeze
    };
}
