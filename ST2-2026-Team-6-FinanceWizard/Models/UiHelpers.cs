using System.Globalization;
using System.Text.RegularExpressions;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    // Formats money with a symbol for well-known currencies ("EUR" -> €1,234.00)
    public static class Money
    {
        private static readonly Dictionary<string, string> Symbols = new(StringComparer.OrdinalIgnoreCase)
        {
            ["EUR"] = "€",
            ["USD"] = "$",
            ["GBP"] = "£"
        };

        public static string Symbol(string? currency)
        {
            if (string.IsNullOrWhiteSpace(currency)) return "€";
            var c = currency.Trim();
            return Symbols.TryGetValue(c, out var s) ? s : c;
        }

        public static string Format(decimal amount, string? currency)
        {
            var sym = Symbol(currency);
            var number = amount.ToString("N2", CultureInfo.InvariantCulture);
            return sym.Length == 1 ? sym + number : number + " " + sym;
        }
    }

    // Stable colour (hue 0-359) derived from a name, so every merchant / tag keeps its colour
    public static class Hue
    {
        public static int Of(string? text)
        {
            unchecked
            {
                int hash = 17;
                foreach (var ch in (text ?? string.Empty).Trim().ToLowerInvariant())
                {
                    hash = hash * 31 + ch;
                }
                return Math.Abs(hash % 360);
            }
        }
    }

    public static class CategoryIcons
    {
        public static readonly string[] All =
        {
            "cart", "bolt", "film", "home", "car", "heart", "gift", "coffee", "book", "plane", "tag"
        };
    }
    // Colours chosen with the colour picker ("#rrggbb"): validation + readable text colour
    public static class Colors
    {
        private static readonly Regex HexPattern = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        public static bool IsHex(string? color) =>
            !string.IsNullOrWhiteSpace(color) && HexPattern.IsMatch(color.Trim());

        // Returns "#rrggbb" in lower case, or null if the value is empty / not a valid hex colour
        public static string? Normalize(string? color) =>
            IsHex(color) ? color!.Trim().ToLowerInvariant() : null;

        // White or dark text, whichever is easier to read on top of the colour (WCAG contrast)
        public static string TextOn(string hex)
        {
            double bg = Luminance(hex);
            double white = 1.05 / (bg + 0.05);
            double darkL = Luminance("#1b2236");
            double dark = (bg + 0.05) / (darkL + 0.05);
            return white >= dark ? "#ffffff" : "#1b2236";
        }

        // Inline style for an avatar / chip: the chosen colour, or the automatic hue of the name
        public static string Style(string? color, string? name)
        {
            var n = Normalize(color);
            return n == null ? "--h:" + Hue.Of(name) : "--c:" + n + ";--on:" + TextOn(n);
        }

        // "has-color" switches the CSS from the automatic hue to the chosen colour
        public static string Class(string? color) => IsHex(color) ? "has-color" : string.Empty;

        private static double Luminance(string hex)
        {
            double Channel(int value)
            {
                double c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            int r = Convert.ToInt32(hex.Substring(1, 2), 16);
            int g = Convert.ToInt32(hex.Substring(3, 2), 16);
            int b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
        }
    }
}
