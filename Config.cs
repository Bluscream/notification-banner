using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Collections.Generic; // Added for Dictionary
using System.Text.RegularExpressions; // Added for Regex
using Bluscream;

namespace NotificationBanner {
    public class DefaultUserConfig : Config {
        public DefaultUserConfig() {
            LogFile = Path.Combine(Path.GetTempPath(), "banner.log");
        }
    }
    public class DefaultProgramConfig : Config {
        public DefaultProgramConfig() {
            LogFile = Path.Combine(Path.GetTempPath(), "banner.log");
        }
    }
    public partial class Config {
        public string? Message { get; set; } = string.Empty;
        public string? Title { get; set; } = "Notification";
        public string? Time { get; set; } = "10";
        // public string? Image { get; set; } // In DefaultIcon.cs
        public string? Position { get; set; } = "topleft";
        public bool Exit { get; set; } = false;
        public string? Color { get; set; } = string.Empty;
        public string? Sound { get; set; } = "C:\\Windows\\Media\\Windows Notify System Generic.wav";
        public string? Size { get; set; } = "100";
        public bool Primary { get; set; } = false;
        public bool Important { get; set; } = false;
        public string? ApiListenAddresses { get; set; } = "*:14969";
        public string? LogFile { get; set; } = Path.Combine(Path.GetTempPath(), "banner.log");
        public bool Console { get; set; } = false;
        public Dictionary<string, string> DefaultImages { get; set; } = new Dictionary<string, string> {
            { @"HASS\.Agent", "https://www.hass-agent.io/2.1/assets/images/logo/logo-256.png" }
        };

        public static Config Load(string[] args) {
            var exePath = Bluscream.Utils.GetOwnPath();
            var programConfigPath = exePath.ReplaceExtension("json");
            var userConfigPath = Environment.SpecialFolder.UserProfile.Combine(exePath.FileNameWithoutExtension + ".json");

            var config = new Config();
            if (!programConfigPath.Exists) {
                // config.SaveToFile(programConfigPath.FullName);
            } else {
                config.LoadFromFile(programConfigPath.FullName);
            }

            if (!userConfigPath.Exists) {
                // config.SaveToFile(userConfigPath.FullName);
            } else {
                config.LoadFromFile(userConfigPath.FullName);
            }

            bool imageSetByCmd = false;
            if (args.Length == 1 && !(args[0].StartsWith("-") || args[0].StartsWith("/"))) {
                config.Message = args[0];
            } else if (args.Length == 2 && !(args[0].StartsWith("-") || args[0].StartsWith("/") || args[1].StartsWith("-") || args[1].StartsWith("/"))) {
                config.Message = args[0];
                config.Title = args[1];
            } else {
                // Check if --image or similar was provided
                for (int i = 0; i < args.Length; i++) {
                    var arg = args[i];
                    if ((arg.StartsWith("--") || arg.StartsWith("-") || arg.StartsWith("/")) && arg.Length > 1) {
                        var key = arg.TrimStart('-','/').ToLowerInvariant();
                        if (key == "image") {
                            imageSetByCmd = true;
                            break;
                        }
                    }
                }
                config.ParseCommandLine(args);
            }

            // If Image is not set by command line and Title matches a regex, set Image
            if (!imageSetByCmd && !string.IsNullOrEmpty(config.Title)) {
                foreach (var kvp in config.DefaultImages) {
                    // Console.WriteLine($"Checking Title {config.Title} against {kvp.Key}");
                    if (System.Text.RegularExpressions.Regex.IsMatch(config.Title, kvp.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                        // Console.WriteLine($"Setting Image to {kvp.Value} for Title {config.Title}");
                        config.Image = kvp.Value;
                        // config.Title = "overridden";
                        break;
                    }
                }
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
                Utils.Log(this, $"Loaded config from: {path}");
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
                Utils.Log(this, $"Saved config to: {path}");
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
                        case "size": Size = value; break;
                        case "primary": Primary = true; break;
                        case "important": Important = true; break;
                        case "api-listen-addresses": ApiListenAddresses = value; break;
                        case "log-file": LogFile = value; break;
                        case "console": Console = true; break;
                    }
                }
            }
        }
    }
}
