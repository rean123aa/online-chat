using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Controller
{
    public static class ThemeManager
    {
        public static string CurrentTheme { get; private set; } = "Synapse Orange";

        public static Color AccentColor => ((SolidColorBrush)Application.Current.Resources["AccentOrange"]).Color;
        public static Color TerminalColor => ((SolidColorBrush)Application.Current.Resources["TerminalGreen"]).Color;
        public static Color BgLightColor => ((SolidColorBrush)Application.Current.Resources["BgLight"]).Color;
        public static Color BgLighterColor => ((SolidColorBrush)Application.Current.Resources["BgLighter"]).Color;
        public static Color BgMediumColor => ((SolidColorBrush)Application.Current.Resources["BgMedium"]).Color;

        public static readonly string[] ThemeOrder = { "Synapse Orange", "Synapse Cyan", "Synapse Lime", "Synapse Red" };

        private static string SettingsPath => Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "theme_settings.json");

        public static readonly Dictionary<string, ThemePalette> Themes = new()
        {
            ["Synapse Orange"] = new ThemePalette
            {
                Name = "Synapse Orange",
                Accent = Color.FromRgb(0xFF, 0x8C, 0x00),
                AccentLight = Color.FromRgb(0xFF, 0xA7, 0x26),
                AccentDark = Color.FromRgb(0xE6, 0x73, 0x00),
                Terminal = Color.FromRgb(0x00, 0xFF, 0x00),
                TerminalDim = Color.FromRgb(0x00, 0xAA, 0x00),
                TerminalBg = Color.FromRgb(0x0A, 0x0A, 0x0A),
                BgDarkest = Color.FromRgb(0x0D, 0x0D, 0x0D),
                BgDark = Color.FromRgb(0x12, 0x12, 0x12),
                BgMedium = Color.FromRgb(0x1E, 0x1E, 0x1E),
                BgLight = Color.FromRgb(0x25, 0x25, 0x25),
                BgLighter = Color.FromRgb(0x2D, 0x2D, 0x2D),
                PanelBg = Color.FromRgb(0x14, 0x14, 0x14),
                TabBg = Color.FromRgb(0x1A, 0x1A, 0x1A),
                HeaderStart = Color.FromRgb(0x2D, 0x2D, 0x2D),
                HeaderEnd = Color.FromRgb(0x1E, 0x1E, 0x1E),
                FooterBg = Color.FromRgb(0x1A, 0x1A, 0x1A),
            },
            ["Synapse Cyan"] = new ThemePalette
            {
                Name = "Synapse Cyan",
                Accent = Color.FromRgb(0x00, 0xE5, 0xFF),
                AccentLight = Color.FromRgb(0x40, 0xF0, 0xFF),
                AccentDark = Color.FromRgb(0x00, 0xB8, 0xD4),
                Terminal = Color.FromRgb(0x00, 0xE5, 0xFF),
                TerminalDim = Color.FromRgb(0x00, 0x99, 0xAA),
                TerminalBg = Color.FromRgb(0x05, 0x0A, 0x10),
                BgDarkest = Color.FromRgb(0x08, 0x0F, 0x16),
                BgDark = Color.FromRgb(0x0D, 0x16, 0x20),
                BgMedium = Color.FromRgb(0x11, 0x18, 0x20),
                BgLight = Color.FromRgb(0x18, 0x20, 0x2A),
                BgLighter = Color.FromRgb(0x1A, 0x25, 0x30),
                PanelBg = Color.FromRgb(0x0D, 0x16, 0x20),
                TabBg = Color.FromRgb(0x11, 0x18, 0x20),
                HeaderStart = Color.FromRgb(0x1A, 0x25, 0x30),
                HeaderEnd = Color.FromRgb(0x11, 0x18, 0x20),
                FooterBg = Color.FromRgb(0x11, 0x18, 0x20),
            },
            ["Synapse Lime"] = new ThemePalette
            {
                Name = "Synapse Lime",
                Accent = Color.FromRgb(0x76, 0xFF, 0x03),
                AccentLight = Color.FromRgb(0x9A, 0xFF, 0x3A),
                AccentDark = Color.FromRgb(0x5A, 0xC8, 0x00),
                Terminal = Color.FromRgb(0x76, 0xFF, 0x03),
                TerminalDim = Color.FromRgb(0x4A, 0xAA, 0x00),
                TerminalBg = Color.FromRgb(0x05, 0x0A, 0x04),
                BgDarkest = Color.FromRgb(0x0A, 0x10, 0x08),
                BgDark = Color.FromRgb(0x0E, 0x16, 0x0C),
                BgMedium = Color.FromRgb(0x11, 0x1A, 0x10),
                BgLight = Color.FromRgb(0x18, 0x24, 0x18),
                BgLighter = Color.FromRgb(0x1A, 0x28, 0x1A),
                PanelBg = Color.FromRgb(0x0E, 0x16, 0x0C),
                TabBg = Color.FromRgb(0x11, 0x1A, 0x10),
                HeaderStart = Color.FromRgb(0x1A, 0x24, 0x18),
                HeaderEnd = Color.FromRgb(0x11, 0x1A, 0x10),
                FooterBg = Color.FromRgb(0x11, 0x1A, 0x10),
            },
            ["Synapse Red"] = new ThemePalette
            {
                Name = "Synapse Red",
                Accent = Color.FromRgb(0xFF, 0x52, 0x52),
                AccentLight = Color.FromRgb(0xFF, 0x75, 0x75),
                AccentDark = Color.FromRgb(0xD3, 0x2F, 0x2F),
                Terminal = Color.FromRgb(0xFF, 0x52, 0x52),
                TerminalDim = Color.FromRgb(0xAA, 0x33, 0x33),
                TerminalBg = Color.FromRgb(0x0A, 0x04, 0x04),
                BgDarkest = Color.FromRgb(0x10, 0x08, 0x08),
                BgDark = Color.FromRgb(0x14, 0x0C, 0x0C),
                BgMedium = Color.FromRgb(0x1A, 0x10, 0x10),
                BgLight = Color.FromRgb(0x20, 0x16, 0x16),
                BgLighter = Color.FromRgb(0x2A, 0x1A, 0x1A),
                PanelBg = Color.FromRgb(0x14, 0x0C, 0x0C),
                TabBg = Color.FromRgb(0x1A, 0x10, 0x10),
                HeaderStart = Color.FromRgb(0x2A, 0x1A, 0x1A),
                HeaderEnd = Color.FromRgb(0x1A, 0x10, 0x10),
                FooterBg = Color.FromRgb(0x1A, 0x10, 0x10),
            },
        };

        public static void LoadSavedTheme()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                    if (settings != null && Themes.ContainsKey(settings.Theme))
                    {
                        ApplyTheme(settings.Theme);
                        return;
                    }
                }
            }
            catch { }
            ApplyTheme("Synapse Orange");
        }

        public static void CycleTheme()
        {
            int idx = System.Array.IndexOf(ThemeOrder, CurrentTheme);
            if (idx < 0) idx = 0;
            int next = (idx + 1) % ThemeOrder.Length;
            ApplyTheme(ThemeOrder[next]);
        }

        public static void ApplyTheme(string themeName)
        {
            if (!Themes.TryGetValue(themeName, out var p)) return;
            CurrentTheme = themeName;

            var res = Application.Current.Resources;

            // Accent
            UpdateBrush(res, "AccentOrange", p.Accent);
            UpdateBrush(res, "AccentOrangeLight", p.AccentLight);
            UpdateBrush(res, "AccentOrangeDark", p.AccentDark);
            // Terminal
            UpdateBrush(res, "TerminalGreen", p.Terminal);
            UpdateBrush(res, "TerminalDim", p.TerminalDim);
            // Backgrounds
            UpdateBrush(res, "BgDarkest", p.BgDarkest);
            UpdateBrush(res, "BgDark", p.BgDark);
            UpdateBrush(res, "BgMedium", p.BgMedium);
            UpdateBrush(res, "BgLight", p.BgLight);
            UpdateBrush(res, "BgLighter", p.BgLighter);
            UpdateBrush(res, "PanelBg", p.PanelBg);
            UpdateBrush(res, "TabBg", p.TabBg);
            UpdateBrush(res, "FooterBg", p.FooterBg);
            // Header gradient
            UpdateBrush(res, "HeaderStart", p.HeaderStart);
            UpdateBrush(res, "HeaderEnd", p.HeaderEnd);
            // Terminal background
            UpdateBrush(res, "TerminalBg", p.TerminalBg);

            SaveTheme();

            foreach (Window window in Application.Current.Windows)
                window.InvalidateVisual();
        }

        private static void SaveTheme()
        {
            try
            {
                var settings = new ThemeSettings { Theme = CurrentTheme };
                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        private static void UpdateBrush(ResourceDictionary res, string key, Color color)
        {
            if (res[key] is SolidColorBrush brush)
                brush.Color = color;
            else
                res[key] = new SolidColorBrush(color);
        }

        private class ThemeSettings
        {
            public string Theme { get; set; }
        }
    }

    public class ThemePalette
    {
        public string Name { get; set; }
        public Color Accent { get; set; }
        public Color AccentLight { get; set; }
        public Color AccentDark { get; set; }
        public Color Terminal { get; set; }
        public Color TerminalDim { get; set; }
        public Color TerminalBg { get; set; }
        public Color BgDarkest { get; set; }
        public Color BgDark { get; set; }
        public Color BgMedium { get; set; }
        public Color BgLight { get; set; }
        public Color BgLighter { get; set; }
        public Color PanelBg { get; set; }
        public Color TabBg { get; set; }
        public Color HeaderStart { get; set; }
        public Color HeaderEnd { get; set; }
        public Color FooterBg { get; set; }
    }
}
