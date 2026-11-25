using TiktokStreakSaver.Models;

namespace TiktokStreakSaver.Services;

/// <summary>
/// Built-in coordinated theme presets (all 11 presets with both Dark and Light palettes).
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class ThemeCatalog
{
    public const string DefaultPresetId = "obsidian";

    private static ThemePalette Dark(
        string primary, string secondary, string accent,
        string bg, string bg2, string card,
        string text, string text2, string border,
        string success = "#10B981", string warning = "#F59E0B", string error = "#FF3B5C") =>
        new(
            Color.FromArgb(primary), Color.FromArgb(secondary), Color.FromArgb(accent),
            Color.FromArgb(bg), Color.FromArgb(bg2), Color.FromArgb(card),
            Color.FromArgb(text), Color.FromArgb(text2), Color.FromArgb(border),
            Color.FromArgb(success), Color.FromArgb(warning), Color.FromArgb(error),
            IsLight: false);

    private static ThemePalette Light(
        string primary, string secondary, string accent,
        string bg = "#F8FAFC", string bg2 = "#F1F5F9", string card = "#FFFFFF",
        string text = "#0F172A", string text2 = "#475569", string border = "#E2E8F0",
        string success = "#059669", string warning = "#D97706", string error = "#E11D48") =>
        new(
            Color.FromArgb(primary), Color.FromArgb(secondary), Color.FromArgb(accent),
            Color.FromArgb(bg), Color.FromArgb(bg2), Color.FromArgb(card),
            Color.FromArgb(text), Color.FromArgb(text2), Color.FromArgb(border),
            Color.FromArgb(success), Color.FromArgb(warning), Color.FromArgb(error),
            IsLight: true);

    public static IReadOnlyList<ThemePreset> Presets { get; } = new List<ThemePreset>
    {
        new(
            "obsidian",
            "Obsidian",
            "Default stealth obsidian palette",
            AppThemeMode.Dark,
            Dark(
                primary: "#FF3B5C", secondary: "#06B6D4", accent: "#8B5CF6",
                bg: "#0A0D12", bg2: "#161B24", card: "#13171F",
                text: "#FFFFFF", text2: "#8E95A5", border: "#1F2430",
                success: "#10B981", warning: "#F59E0B", error: "#FF3B5C"),
            Light(
                primary: "#FF3B5C", secondary: "#0891B2", accent: "#7C3AED",
                bg: "#F8FAFC", bg2: "#F1F5F9", card: "#FFFFFF",
                text: "#0F172A", text2: "#475569", border: "#E2E8F0")),

        new(
            "tiktok_neon",
            "TikTok Neon",
            "High-contrast cyber magenta & cyan",
            AppThemeMode.Dark,
            Dark(
                primary: "#FE2C55", secondary: "#25F4EE", accent: "#FF0050",
                bg: "#07080D", bg2: "#181B27", card: "#11131C",
                text: "#FFFFFF", text2: "#9CA3AF", border: "#262B3D",
                success: "#25F4EE", warning: "#FBBF24", error: "#FE2C55"),
            Light(
                primary: "#FE2C55", secondary: "#06B6D4", accent: "#E11D48",
                bg: "#FAFAFC", bg2: "#F1F5F9", card: "#FFFFFF",
                text: "#09090B", text2: "#52525B", border: "#E4E4E7")),

        new(
            "amoled_black",
            "AMOLED Black",
            "True pitch-black OLED battery saver",
            AppThemeMode.Dark,
            Dark(
                primary: "#FF3B5C", secondary: "#00E5FF", accent: "#A855F7",
                bg: "#000000", bg2: "#121218", card: "#09090C",
                text: "#FFFFFF", text2: "#94A3B8", border: "#22222C",
                success: "#00E599", warning: "#F59E0B", error: "#FF3B5C"),
            Light(
                primary: "#E11D48", secondary: "#0284C7", accent: "#9333EA",
                bg: "#FFFFFF", bg2: "#F4F4F5", card: "#FAFAFA",
                text: "#09090B", text2: "#52525B", border: "#E4E4E7")),

        new(
            "midnight_blue",
            "Midnight Blue",
            "Deep indigo slate with sky highlights",
            AppThemeMode.Dark,
            Dark(
                primary: "#38BDF8", secondary: "#818CF8", accent: "#6366F1",
                bg: "#0B1120", bg2: "#1B2642", card: "#131C31",
                text: "#F8FAFC", text2: "#94A3B8", border: "#273558",
                success: "#34D399", warning: "#FBBF24", error: "#FB7185"),
            Light(
                primary: "#0284C7", secondary: "#4F46E5", accent: "#6366F1",
                bg: "#F0F6FF", bg2: "#E2ECFA", card: "#FFFFFF",
                text: "#0F172A", text2: "#475569", border: "#CBD5E1")),

        new(
            "ocean",
            "Ocean",
            "Abyssal teal depths & aqua highlights",
            AppThemeMode.Dark,
            Dark(
                primary: "#06B6D4", secondary: "#38BDF8", accent: "#2DD4BF",
                bg: "#071824", bg2: "#133247", card: "#0D2536",
                text: "#F0FDFA", text2: "#7FB3C8", border: "#1D4660",
                success: "#2DD4BF", warning: "#FBBF24", error: "#FB7185"),
            Light(
                primary: "#0891B2", secondary: "#0284C7", accent: "#0D9488",
                bg: "#ECFEFF", bg2: "#E0F2FE", card: "#FFFFFF",
                text: "#083344", text2: "#155E75", border: "#BAE6FD")),

        new(
            "emerald",
            "Emerald",
            "Lush forest dark with mint accents",
            AppThemeMode.Dark,
            Dark(
                primary: "#10B981", secondary: "#34D399", accent: "#14B8A6",
                bg: "#06140E", bg2: "#132E22", card: "#0D2219",
                text: "#ECFDF5", text2: "#86B8A3", border: "#1F4434",
                success: "#34D399", warning: "#FBBF24", error: "#F87171"),
            Light(
                primary: "#059669", secondary: "#10B981", accent: "#0D9488",
                bg: "#ECFDF5", bg2: "#D1FAE5", card: "#FFFFFF",
                text: "#064E3B", text2: "#047857", border: "#A7F3D0")),

        new(
            "dracula",
            "Dracula",
            "Classic vampire dark purple & pink",
            AppThemeMode.Dark,
            Dark(
                primary: "#FF79C6", secondary: "#8BE9FD", accent: "#BD93F9",
                bg: "#282A36", bg2: "#3E4254", card: "#343746",
                text: "#F8F8F2", text2: "#B8BCCE", border: "#44475A",
                success: "#50FA7B", warning: "#FFB86C", error: "#FF5555"),
            Light(
                primary: "#DB2777", secondary: "#0284C7", accent: "#7C3AED",
                bg: "#F8F7FC", bg2: "#EDE9FE", card: "#FFFFFF",
                text: "#1E1B2E", text2: "#5B5475", border: "#DDD6FE",
                success: "#16A34A", warning: "#EA580C", error: "#DC2626")),

        new(
            "nord",
            "Nord",
            "Arctic frost blues & calm contrast",
            AppThemeMode.Dark,
            Dark(
                primary: "#88C0D0", secondary: "#81A1C1", accent: "#B48EAD",
                bg: "#2E3440", bg2: "#434C5E", card: "#3B4252",
                text: "#ECEFF4", text2: "#D8DEE9", border: "#4C566A",
                success: "#A3BE8C", warning: "#EBCB8B", error: "#BF616A"),
            Light(
                primary: "#5E81AC", secondary: "#0284C7", accent: "#B48EAD",
                bg: "#ECEFF4", bg2: "#E5E9F0", card: "#FFFFFF",
                text: "#2E3440", text2: "#4C566A", border: "#D8DEE9",
                success: "#4D7C0F", warning: "#B45309", error: "#BE123C")),

        new(
            "sunset",
            "Sunset",
            "Warm dusk coral, amber & plum",
            AppThemeMode.Dark,
            Dark(
                primary: "#FB7185", secondary: "#F59E0B", accent: "#F97316",
                bg: "#180D14", bg2: "#321B28", card: "#25141E",
                text: "#FFF1F2", text2: "#D6A8B8", border: "#4A283B",
                success: "#34D399", warning: "#FBBF24", error: "#F43F5E"),
            Light(
                primary: "#E11D48", secondary: "#D97706", accent: "#EA580C",
                bg: "#FFF7ED", bg2: "#FFEDD5", card: "#FFFFFF",
                text: "#431407", text2: "#9A3412", border: "#FED7AA")),

        new(
            "rose",
            "Rose",
            "Velvet rose & soft magenta glow",
            AppThemeMode.Dark,
            Dark(
                primary: "#F43F5E", secondary: "#EC4899", accent: "#FB7185",
                bg: "#170D12", bg2: "#2F1B24", card: "#23141B",
                text: "#FFF1F2", text2: "#D4A3B3", border: "#462836",
                success: "#10B981", warning: "#F59E0B", error: "#FB7185"),
            Light(
                primary: "#E11D48", secondary: "#DB2777", accent: "#F43F5E",
                bg: "#FFF1F2", bg2: "#FFE4E6", card: "#FFFFFF",
                text: "#4C0519", text2: "#881337", border: "#FECDD3")),

        new(
            "minimal_light",
            "Minimal Light",
            "Crisp daytime daylight surfaces",
            AppThemeMode.Light,
            Dark(
                primary: "#FF3B5C", secondary: "#38BDF8", accent: "#8B5CF6",
                bg: "#111827", bg2: "#1F2937", card: "#18212F",
                text: "#F9FAFB", text2: "#9CA3AF", border: "#374151"),
            Light(
                primary: "#FF3B5C", secondary: "#0284C7", accent: "#7C3AED",
                bg: "#F8FAFC", bg2: "#F1F5F9", card: "#FFFFFF",
                text: "#0F172A", text2: "#475569", border: "#E2E8F0"))
    };

    public static ThemePreset GetPreset(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var match = Presets.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }
        return Presets[0];
    }
}
