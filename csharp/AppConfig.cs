using System.Globalization;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace TextPlus2;

sealed class AppConfig
{
    public const string FileName = "TextPlus2.toml";

    public string Theme { get; set; } = "system";
    public int Zoom { get; set; } = 100;
    public bool FileTree { get; set; } = true;
    public bool FollowOpenFile { get; set; }
    public bool WordWrap { get; set; }
    public string Font { get; set; } = "jetbrains";
    public string? Warning { get; private set; }

    public static AppConfig Current { get; private set; } = new();

    public static string Directory()
    {
        var process = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(process))
        {
            var name = Path.GetFileName(process);
            // dotnet run hosts the app. The file belongs next to the built app, not next to dotnet.
            if (!name.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                && !name.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(process);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }
        }

        return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static string FilePath => Path.Combine(Directory(), FileName);

    public static void Load()
    {
        var config = new AppConfig();
        Current = config;
        var path = FilePath;
        if (!File.Exists(path))
        {
            if (!config.Save())
                config.Warning = "Could not write TextPlus2.toml";
            return;
        }

        try
        {
            var text = File.ReadAllText(path);
            if (!TomlSerializer.TryDeserialize(text, out TomlTable? table) || table == null)
            {
                config.Warning = "Could not read TextPlus2.toml";
                return;
            }

            config.Theme = ThemeOf(Value(table, "theme"));
            config.Zoom = ZoomOf(Value(table, "zoom"));
            config.FileTree = BoolOf(Value(table, "file_tree"), true);
            config.FollowOpenFile = BoolOf(Value(table, "follow_open_file"), false);
            config.WordWrap = BoolOf(Value(table, "word_wrap"), false);
            config.Font = FontOf(Value(table, "font"));
        }
        catch (Exception)
        {
            config.Theme = "system";
            config.Zoom = 100;
            config.FileTree = true;
            config.FollowOpenFile = false;
            config.WordWrap = false;
            config.Font = "jetbrains";
            config.Warning = "Could not read TextPlus2.toml";
        }
    }

    public bool Save()
    {
        try
        {
            var path = FilePath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                System.IO.Directory.CreateDirectory(directory);

            // Rewrite the file that is already there. A second file in this folder
            // cannot be created when the program is installed under Program Files.
            var text = Serialize();
            File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string Serialize()
    {
        var tree = FileTree ? "true" : "false";
        var follow = FollowOpenFile ? "true" : "false";
        var wrap = WordWrap ? "true" : "false";
        return
            "# TextPlus2 settings. This file is in the same folder as TextPlus2.\n" +
            "# The View menu writes this file. You can edit it while TextPlus2 is closed.\n" +
            "\n" +
            "# system follows the operating system. light and dark stay on that theme.\n" +
            "theme = \"" + Theme + "\"\n" +
            "\n" +
            "# 50 to 300.\n" +
            "zoom = " + Zoom.ToString(CultureInfo.InvariantCulture) + "\n" +
            "\n" +
            "file_tree = " + tree + "\n" +
            "\n" +
            "follow_open_file = " + follow + "\n" +
            "\n" +
            "word_wrap = " + wrap + "\n" +
            "\n" +
            "# jetbrains, cascadia, or plex.\n" +
            "font = \"" + Font + "\"\n";
    }

    private static object? Value(TomlTable table, string key)
    {
        if (table.TryGetValue(key, out var direct))
            return direct;

        foreach (var pair in table)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }

    private static string ThemeOf(object? value)
    {
        if (value is not string text)
            return "system";

        return text.Trim().ToLowerInvariant() switch
        {
            "light" => "light",
            "dark" => "dark",
            "system" => "system",
            _ => "system",
        };
    }

    private static int ZoomOf(object? value)
    {
        double? zoom = value switch
        {
            byte or sbyte or short or ushort or int or uint or long or ulong => Convert.ToDouble(value, CultureInfo.InvariantCulture),
            float amount => amount,
            double amount => amount,
            string text when double.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
        if (zoom == null)
            return 100;

        var snapped = (int)Math.Round(zoom.Value / 10.0, MidpointRounding.AwayFromZero) * 10;
        return Math.Clamp(snapped, 50, 300);
    }

    private static bool BoolOf(object? value, bool fallback) =>
        value is bool bit ? bit : fallback;

    private static string FontOf(object? value)
    {
        if (value is not string text)
            return "jetbrains";

        return text.Trim().ToLowerInvariant() switch
        {
            "jetbrains" => "jetbrains",
            "cascadia" => "cascadia",
            "plex" => "plex",
            _ => "jetbrains",
        };
    }
}
