using System.Globalization;
using System.Text;

namespace StrayThoughts_BE.Utilities;

public static class EmojiValidator
{
    public const int MaxLength = 32;

    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true; // Optional field

        var trimmed = input.Trim();

        if (trimmed.Length > MaxLength)
            return false;

        // 1. Basic safety checks
        if (trimmed.Contains('<') || trimmed.Contains('>') || trimmed.Contains('&') || trimmed.Contains('"'))
            return false;

        // Normalize so composed/decomposed sequences evaluate consistently
        var normalized = trimmed.Normalize(NormalizationForm.FormC);

        // 2. Must be strictly 1 visual text element (grapheme cluster)
        int textElementCount = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(normalized);

        while (enumerator.MoveNext())
        {
            textElementCount++;
            if (textElementCount > 1)
                return false;
        }

        if (textElementCount != 1)
            return false;

        // 3. Inspect runes: require multiple code points in the cluster + at least one emoji/keycap/variation rune
        int runeCount = 0;
        bool hasEmojiOrModifier = false;

        foreach (var rune in normalized.EnumerateRunes())
        {
            runeCount++;

            // Reject standalone ASCII letters/digits if ever paired unexpectedly, though keycaps like '1'
            // are paired with U+20E3 / U+FE0F inside a multi-rune cluster.
            if (rune.Value == 0x20E3 ||      // Combining Enclosing Keycap (e.g., 1️⃣)
                rune.Value == 0xFE0F ||      // Variation Selector-16 (emoji presentation)
                rune.Value == 0x200D ||      // Zero Width Joiner (ZWJ sequences)
                (rune.Value >= 0x1F3FB && rune.Value <= 0x1F3FF) || // Skin tone modifiers
                rune.Value >= 0x1F000)       // Main supplementary emoji/symbol blocks
            {
                hasEmojiOrModifier = true;
            }
        }

        // Requires a multi-code-point cluster with an emoji/keycap/variation component
        return runeCount > 1 && hasEmojiOrModifier;
    }
}
