using System.Globalization;

namespace StrayThoughts_BE.Utilities;

public static class EmojiValidator
{
    public const int MaxLength = 32;

    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true; // Optional field

        var trimmed = input.Trim();

        // 1. Basic safety checks
        if (trimmed.Contains('<') || trimmed.Contains('>') || trimmed.Contains('&') || trimmed.Contains('"'))
            return false;

        // 2. Count visual text elements (grapheme clusters)
        int textElementCount = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(trimmed);

        while (enumerator.MoveNext())
        {
            textElementCount++;
        }

        // If they typed more than one emoji symbol, reject it
        if (textElementCount != 1)
        {
            return false;
        }

        // 3. Optional: Ensure the single character isn't a plain alphabet letter or number
        foreach (var rune in trimmed.EnumerateRunes())
        {
            if ((rune.Value >= 'a' && rune.Value <= 'z') ||
                (rune.Value >= 'A' && rune.Value <= 'Z') ||
                (rune.Value >= '0' && rune.Value <= '9'))
            {
                return false;
            }
        }

        return true;
    }
}
