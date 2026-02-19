using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Core
{
    public static class ColorPaletteGenerator
    {
        // ---- Fixed palettes (start points) ----
        public static readonly Color[] CategoricalSoftSet12 =
        {
            Hex("#8DD3C7"), Hex("#FFFFB3"), Hex("#BEBADA"), Hex("#FB8072"),
            Hex("#80B1D3"), Hex("#FDB462"), Hex("#B3DE69"), Hex("#FCCDE5"),
            Hex("#D9D9D9"), Hex("#BC80BD"), Hex("#CCEBC5"), Hex("#FFED6F")
        };

        public static readonly Color[] Tableau10 =
        {
            Hex("#4E79A7"), Hex("#F28E2B"), Hex("#E15759"), Hex("#76B7B2"),
            Hex("#59A14F"), Hex("#EDC948"), Hex("#B07AA1"), Hex("#FF9DA7"),
            Hex("#9C755F"), Hex("#BAB0AC")
        };

        // Temperature stops (cool->warm). Use gradient generator for any N
        public static readonly (double pos, Color color)[] TemperatureStops =
        {
            (0.0, Hex("#2C7BB6")),
            (0.25, Hex("#00A6CA")),
            (0.5, Hex("#F9D057")),
            (0.75, Hex("#F29E2E")),
            (1.0, Hex("#D7191C"))
        };

        // ---- Public API: get N colors ----

        /// <summary>
        /// Returns N colors from a categorical base palette. If N > base size, generates additional colours.
        /// </summary>
        public static List<Color> GetCategorical(int n, Color[] basePalette, bool extendIfNeeded = true)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (basePalette == null || basePalette.Length == 0) throw new ArgumentException("Base palette empty.");

            var result = new List<Color>(n);

            if (!extendIfNeeded)
            {
                for (int i = 0; i < n; i++)
                    result.Add(basePalette[i % basePalette.Length]);
                return result;
            }

            // Start with base palette unique colours
            for (int i = 0; i < Math.Min(n, basePalette.Length); i++)
                result.Add(basePalette[i]);

            // Extend beyond base using evenly spaced HSV hues while keeping decent saturation/value.
            if (n > basePalette.Length)
            {
                int extra = n - basePalette.Length;
                var generated = GenerateDistinctHsv(extra, saturation: 0.62, value: 0.92, hueOffset: 0.11);
                result.AddRange(generated);
            }

            return result.Take(n).ToList();
        }

        /// <summary>
        /// Returns N colors for a continuous variable (e.g. Temperature) using gradient stops.
        /// </summary>
        public static List<Color> GetSequentialFromStops(int n, IReadOnlyList<(double pos, Color color)> stops)
            => ColorGradientInterpolationGenerator.GenerateDiscreteFromStops(stops, n, includeEndpoints: true);

        /// <summary>
        /// Assigns colors sequentially to items in the given order.
        /// </summary>
        public static Dictionary<string, Color> AssignSequential(
            IReadOnlyList<string> itemNames,
            IReadOnlyList<Color> colors)
        {
            if (itemNames == null) throw new ArgumentNullException(nameof(itemNames));
            if (colors == null || colors.Count == 0) throw new ArgumentException("Colors must not be empty.");

            var map = new Dictionary<string, Color>();
            for (int i = 0; i < itemNames.Count; i++)
                map[itemNames[i]] = colors[i % colors.Count];

            return map;
        }

        /// <summary>
        /// Stable deterministic mapping: each name hashes to a color index.
        /// This is best when item counts change per project and you want the same name to keep its colour.
        /// </summary>
        public static Dictionary<string, Color> AssignStableByName(
            IReadOnlyList<string> itemNames,
            IReadOnlyList<Color> palette,
            string salt = "SAM_UI")
        {
            if (itemNames == null) throw new ArgumentNullException(nameof(itemNames));
            if (palette == null || palette.Count == 0) throw new ArgumentException("Palette must not be empty.");

            var map = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in itemNames)
            {
                int idx = StableIndex(name ?? "", palette.Count, salt);
                map[name] = palette[idx];
            }

            return map;
        }

        /// <summary>
        /// Stable mapping but tries to avoid collisions by expanding palette on demand.
        /// Useful when you have many items and want better separation.
        /// </summary>
        public static Dictionary<string, Color> AssignStableDistinct(
            IReadOnlyList<string> itemNames,
            int minPaletteSize = 12,
            double saturation = 0.62,
            double value = 0.92,
            string salt = "SAM_UI")
        {
            if (itemNames == null) throw new ArgumentNullException(nameof(itemNames));
            int n = Math.Max(minPaletteSize, itemNames.Count);

            // generate palette equal to item count so collisions are minimal
            var palette = GenerateDistinctHsv(n, saturation, value, hueOffset: 0.11);

            return AssignStableByName(itemNames, palette, salt);
        }

        // ---- Generators ----

        /// <summary>
        /// Generates N visually distinct colors using HSV hue spacing.
        /// hueOffset allows changing starting hue to avoid too many similar colours when N is small.
        /// </summary>
        public static List<Color> GenerateDistinctHsv(int n, double saturation, double value, double hueOffset = 0.0)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));

            saturation = Query.Clamp(saturation, 0, 1);
            value = Query.Clamp(value, 0, 1);

            var colors = new List<Color>(n);
            // golden ratio conjugate gives good distribution for arbitrary n
            const double phi = 0.618033988749895;

            double h = hueOffset % 1.0;
            for (int i = 0; i < n; i++)
            {
                h = (h + phi) % 1.0;
                colors.Add(FromHsv(h * 360.0, saturation, value));
            }

            return colors;
        }

        /// <summary>
        /// HSV to RGB conversion.
        /// H in degrees [0..360), S,V in [0..1]
        /// </summary>
        public static Color FromHsv(double hDeg, double s, double v)
        {
            s = Query.Clamp(s, 0, 1);
            v = Query.Clamp(v, 0, 1);
            hDeg = (hDeg % 360 + 360) % 360;

            double c = v * s;
            double x = c * (1 - Math.Abs((hDeg / 60.0 % 2) - 1));
            double m = v - c;

            double r1, g1, b1;
            if (hDeg < 60) (r1, g1, b1) = (c, x, 0);
            else if (hDeg < 120) (r1, g1, b1) = (x, c, 0);
            else if (hDeg < 180) (r1, g1, b1) = (0, c, x);
            else if (hDeg < 240) (r1, g1, b1) = (0, x, c);
            else if (hDeg < 300) (r1, g1, b1) = (x, 0, c);
            else (r1, g1, b1) = (c, 0, x);

            int r = (int)Math.Round((r1 + m) * 255);
            int g = (int)Math.Round((g1 + m) * 255);
            int b = (int)Math.Round((b1 + m) * 255);

            return Color.FromArgb(255, Query.Clamp(r, 0, 255), Query.Clamp(g, 0, 255), Query.Clamp(b, 0, 255));
        }

        // ---- Utilities ----

        private static int StableIndex(string name, int modulo, string salt)
        {
            // SHA256(name + salt) -> int -> modulo
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{name}"));

            // use first 4 bytes as uint
            uint value = BitConverter.ToUInt32(bytes, 0);
            return (int)(value % (uint)modulo);
        }

        private static Color Hex(string hex)
        {
            hex = hex.Trim().TrimStart('#');
            int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }

        //private static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);
        
        //private static int Clamp255(int x) => x < 0 ? 0 : (x > 255 ? 255 : x);
    }
}
