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
        public string? Time { get; set; } = "5";
        // public string? Image { get; set; } // In DefaultIcon.cs
        public string? Position { get; set; } = "topleft";
        public bool Exit { get; set; } = false;
        public string? Color { get; set; } = string.Empty;
        public string? Sound { get; set; } = "C:\\Windows\\Media\\Windows Notify System Generic.wav";
        public string? Size { get; set; } = "100";
        public bool Primary { get; set; } = false;
        public bool Important { get; set; } = false;
        public int MaxNotificationsOnScreen { get; set; } = 4;
        public int ApiListenPort { get; set; } = 14969;
        public string? LogFile { get; set; } = Path.Combine(Path.GetTempPath(), "banner.log");
        public bool Console { get; set; } = false;
        public bool CreateDefaultConfig { get; set; } = false;
        public Dictionary<string, string> DefaultImages { get; set; } = new Dictionary<string, string> {
            { @"HASS\.Agent", "https://www.hass-agent.io/2.1/assets/images/logo/logo-256.png" }
        };
        [JsonIgnore]
        internal FileInfo? UserConfigPath { get; set; }
        [JsonIgnore]
        internal FileInfo? GlobalConfigPath { get; set; }
        [JsonIgnore]
        internal FileInfo? ProgramConfigPath { get; set; }
        
        [JsonIgnore]
        internal System.Diagnostics.Stopwatch? TimingStopwatch { get; set; }

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
                        case "max-notifications": 
                            if (int.TryParse(value, out int maxNotifications)) MaxNotificationsOnScreen = maxNotifications; 
                            break;
                        case "api-listen-port": 
                            if (int.TryParse(value, out int port)) ApiListenPort = port; 
                            break;
                        case "log-file": LogFile = value; break;
                        case "console": Console = true; break;
                    }
                }
            }
        }

        public void ParseUrl(string url, string clientIp = "unknown") {
            var queryIndex = url.IndexOf('?');
            if (queryIndex >= 0)
            {
                var query = url.Substring(queryIndex + 1);
                var queryParams = Bluscream.Utils.ParseQueryString(query);
                
                // Map query parameters to config properties only if they are provided
                if (queryParams.ContainsKey("message"))
                    Message = queryParams["message"];
                
                if (queryParams.ContainsKey("title"))
                    Title = queryParams["title"];
                else
                    Title = $"Notification from {clientIp}";
                
                if (queryParams.ContainsKey("time"))
                    Time = queryParams["time"];
                
                if (queryParams.ContainsKey("image"))
                    Image = queryParams["image"];
                
                if (queryParams.ContainsKey("position"))
                    Position = queryParams["position"]?.ToLowerInvariant();
                
                if (queryParams.ContainsKey("exit"))
                    Exit = queryParams["exit"]?.ToLowerInvariant() == "true";
                
                if (queryParams.ContainsKey("color"))
                    Color = queryParams["color"];
                
                if (queryParams.ContainsKey("sound"))
                    Sound = queryParams["sound"];
                
                if (queryParams.ContainsKey("size"))
                    Size = queryParams["size"];
                
                if (queryParams.ContainsKey("primary"))
                    Primary = queryParams["primary"]?.ToLowerInvariant() == "true";
                
                if (queryParams.ContainsKey("important"))
                    Important = queryParams["important"]?.ToLowerInvariant() == "true";

                if (queryParams.ContainsKey("max-notifications"))
                    if (int.TryParse(queryParams["max-notifications"], out int maxNotifications))
                        MaxNotificationsOnScreen = maxNotifications;

                // Handle image based on title (same logic as in Config.Load)
                if (string.IsNullOrEmpty(Image) && !string.IsNullOrEmpty(Title))
                {
                    foreach (var kvp in this.DefaultImages)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(Title, kvp.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            Image = kvp.Value;
                            break;
                        }
                    }
                }
            }
        }

        public Config Copy() {
            var copy = new Config();
            foreach (var prop in typeof(Config).GetProperties()) {
                if (prop.CanWrite && prop.CanRead) {
                    var value = prop.GetValue(this);
                    prop.SetValue(copy, value);
                }
            }
            return copy;
        }

        public static Config Load(string[] args) {
            var exePath = Bluscream.Utils.GetOwnPath();

            var config = new Config();
            config.GlobalConfigPath = Environment.SpecialFolder.CommonApplicationData.CombineFile(exePath.FileNameWithoutExtension() + ".json");
            config.ProgramConfigPath = exePath.ReplaceExtension("json");
            config.UserConfigPath = Environment.SpecialFolder.UserProfile.CombineFile(exePath.FileNameWithoutExtension() + ".json");
            if (config.GlobalConfigPath?.Exists ?? false) {
                config.LoadFromFile(config.GlobalConfigPath.FullName);
            } else if (config.ProgramConfigPath?.Exists ?? false) {
                config.LoadFromFile(config.ProgramConfigPath.FullName);
            } else if (config.UserConfigPath?.Exists ?? false) {
                config.LoadFromFile(config.UserConfigPath.FullName);
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

            if (!imageSetByCmd && !string.IsNullOrEmpty(config.Title)) {
                foreach (var kvp in config.DefaultImages) {
                    if (System.Text.RegularExpressions.Regex.IsMatch(config.Title, kvp.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                        config.Image = kvp.Value;
                        break;
                    }
                }
            }

            if (config.CreateDefaultConfig) {
                if (!config.GlobalConfigPath?.Exists ?? false) config.SaveToFile(config.GlobalConfigPath.FullName);
                if (!config.ProgramConfigPath?.Exists ?? false) config.SaveToFile(config.ProgramConfigPath.FullName);
                if (!config.UserConfigPath?.Exists ?? false) config.SaveToFile(config.UserConfigPath.FullName);
            }
            return config;
        }
    }
}
