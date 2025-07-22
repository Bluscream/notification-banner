using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace NotificationBanner {
    public partial class Config {
        public string? Message { get; set; } = string.Empty;
        public string? Title { get; set; } = "Notification";
        public string? Time { get; set; } = "10";
        // public string? Image { get; set; } // In DefaultIcon.cs
        public string? Position { get; set; } = "topleft";
        public bool Exit { get; set; } = false;
        public string? Color { get; set; } = string.Empty;
        public string? Sound { get; set; } = "C:\\Windows\\Media\\Windows Notify System Generic.wav";

        public static Config Load(string[] args) {
            var exePath = Assembly.GetEntryAssembly()?.Location ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory ?? Environment.GetCommandLineArgs().FirstOrDefault() ?? ".";
            var exeName = Path.GetFileNameWithoutExtension(exePath);
            var exeDir = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory;
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var programConfigPath = Path.Combine(exeDir, exeName + ".json");
            var userConfigPath = Path.Combine(userDir, exeName + ".json");

            var config = new Config();
            if (!File.Exists(programConfigPath))
                config.SaveToFile(programConfigPath);
            else
                config.LoadFromFile(programConfigPath);
            if (!File.Exists(userConfigPath))
                config.SaveToFile(userConfigPath);
            else
                config.LoadFromFile(userConfigPath);

            if (args.Length == 1 && !(args[0].StartsWith("-") || args[0].StartsWith("/"))) {
                config.Message = args[0];
            } else if (args.Length == 2 && !(args[0].StartsWith("-") || args[0].StartsWith("/") || args[1].StartsWith("-") || args[1].StartsWith("/"))) {
                config.Message = args[0];
                config.Title = args[1];
            } else {
                config.ParseCommandLine(args);
            }
            return config;
        }

        public void LoadFromFile(string path) {
            try {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<Config>(json);
                if (loaded == null) return;
                foreach (var prop in typeof(Config).GetProperties()) {
                    var value = prop.GetValue(loaded);
                    if (value != null) prop.SetValue(this, value);
                }
            } catch { /* ignore errors, fallback to defaults */ }
        }

        public void SaveToFile(string path) {
            try {
                var options = new JsonSerializerOptions {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, json);
            } catch { /* ignore errors */ }
        }

        public void ParseCommandLine(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];
                if ((arg.StartsWith("--") || arg.StartsWith("-") || arg.StartsWith("/")) && arg.Length > 1) {
                    var key = arg.TrimStart('-','/').ToLowerInvariant();
                    string? value = null;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && !args[i + 1].StartsWith("/")) {
                        value = args[i + 1];
                        i++;
                    }
                    switch (key) {
                        case "message": Message = value; break;
                        case "title": Title = value; break;
                        case "time": Time = value; break;
                        case "image": Image = value; break;
                        case "position": Position = value?.ToLowerInvariant(); break;
                        case "exit": Exit = true; break;
                        case "color": Color = value; break;
                        case "sound": Sound = value; break;
                    }
                }
            }
        }
    }
}
