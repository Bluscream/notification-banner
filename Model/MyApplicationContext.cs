using NotificationBanner.Banner;
using System.Drawing;
using NotificationBanner;
using Bluscream;
using System.Windows.Forms;

namespace NotificationBanner.Model {
    internal class MyApplicationContext : System.Windows.Forms.ApplicationContext {
        private readonly NotificationQueue _notificationQueue;
        private NotificationManager? _notificationManager;
        private WebServer? _webServer;
        private TrayIconManager? _trayIconManager;
        private Config _config;
        internal MyApplicationContext(NotificationQueue notificationQueue, Config config) {
            _notificationQueue = notificationQueue;
            _config = config;
            _notificationQueue.SetConfigForLogging(config);
            _notificationManager = new NotificationManager(notificationQueue, config, CreateBannerData);
            StartWebServer(config);
            InitializeTrayIconManager();
        }

        public BannerData CreateBannerData(Config config) {
            var imageArg = string.IsNullOrWhiteSpace(config.Image) ? null : config.Image;
            var posArg = string.IsNullOrWhiteSpace(config.Position) ? "0" : config.Position;
            var maxImageSize = 40;

            var toastData = new BannerData();
            toastData.Config = config;
            
            var parsedImage = imageArg != null ? Bluscream.Extensions.ParseImage(imageArg) : null;
            
            if (parsedImage != null) {
                toastData.Image = Bluscream.Extensions.Resize(parsedImage, new Size() { Width = maxImageSize, Height = maxImageSize });
            }
            
            toastData.Position = ParsePosition(posArg, config.Primary);
            
            return toastData;
        }

        private static BannerPositionEnum ParsePositionEnum(string posArg) {
            if (int.TryParse(posArg, out int posInt) && Enum.IsDefined(typeof(BannerPositionEnum), posInt)) {
                return (BannerPositionEnum)posInt;
            }
            if (Enum.TryParse<BannerPositionEnum>(posArg, true, out var posEnum)) {
                return posEnum;
            }
            return BannerPositionEnum.TopLeft;
        }

        private static (int x, int y) GetScreenPosition(BannerPositionEnum pos, int width, int height, int offset = 0, bool usePrimaryScreen = false) {
            var screen = usePrimaryScreen ? System.Windows.Forms.Screen.PrimaryScreen : System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            if (screen?.Bounds == null) {
                // Fallback to primary screen if cursor screen is null
                screen = System.Windows.Forms.Screen.PrimaryScreen;
            }
            
            // Ensure we have a valid screen with bounds
            if (screen?.Bounds == null) {
                // Ultimate fallback - use default values
                return (50, 60 + offset);
            }
            
            int x = 0, y = 0;
            switch (pos) {
                case BannerPositionEnum.TopLeft:
                    x = screen.Bounds.X + 50;
                    y = screen.Bounds.Y + 60 + offset;
                    break;
                case BannerPositionEnum.TopCenter:
                    x = screen.Bounds.X + (screen.Bounds.Width - width) / 2;
                    y = screen.Bounds.Y + 60 + offset;
                    break;
                case BannerPositionEnum.TopRight:
                    x = screen.Bounds.X + screen.Bounds.Width - width - 50;
                    y = screen.Bounds.Y + 60 + offset;
                    break;
                case BannerPositionEnum.BottomLeft:
                    x = screen.Bounds.X + 50;
                    y = screen.Bounds.Y + screen.Bounds.Height - height - 60 - offset;
                    break;
                case BannerPositionEnum.BottomCenter:
                    x = screen.Bounds.X + (screen.Bounds.Width - width) / 2;
                    y = screen.Bounds.Y + screen.Bounds.Height - height - 60 - offset;
                    break;
                case BannerPositionEnum.BottomRight:
                    x = screen.Bounds.X + screen.Bounds.Width - width - 50;
                    y = screen.Bounds.Y + screen.Bounds.Height - height - 60 - offset;
                    break;
                case BannerPositionEnum.Center:
                    x = screen.Bounds.X + (screen.Bounds.Width - width) / 2;
                    y = screen.Bounds.Y + (screen.Bounds.Height - height) / 2;
                    break;
            }
            return (x, y);
        }

        private BannerData.PositionDelegate ParsePosition(string posArg, bool usePrimaryScreen = false) {
            var posEnum = ParsePositionEnum(posArg);
            return (formWidth, formHeight, offset) => GetScreenPosition(posEnum, formWidth, formHeight, offset, usePrimaryScreen);
        }

        private void StartWebServer(Config config)
        {
            if (config.ApiListenPort > 0)
            {
                _webServer = new WebServer(_notificationQueue, config);
                _webServer.Start(config.ApiListenPort);
            }
        }

        private void InitializeTrayIconManager()
        {
            _trayIconManager = new TrayIconManager(_config, _notificationQueue, SaveConfig);
            _trayIconManager.ExitRequested += OnExitRequested;
            _trayIconManager.ReloadConfigRequested += OnReloadConfigRequested;
            _trayIconManager.Initialize();
        }





        private void OnReloadConfigRequested(object? sender, EventArgs e)
        {
            try
            {
                Utils.Log(_config, "Starting config reload...");
                
                // Determine which config file to reload from
                string? configPath = null;
                if (_config.GlobalConfigPath?.Exists == true)
                {
                    configPath = _config.GlobalConfigPath.FullName;
                    Utils.Log(_config, $"Found global config file: {configPath}");
                }
                else if (_config.ProgramConfigPath?.Exists == true)
                {
                    configPath = _config.ProgramConfigPath.FullName;
                    Utils.Log(_config, $"Found program config file: {configPath}");
                }
                else if (_config.UserConfigPath?.Exists == true)
                {
                    configPath = _config.UserConfigPath.FullName;
                    Utils.Log(_config, $"Found user config file: {configPath}");
                }

                if (configPath != null)
                {
                    Utils.Log(_config, $"Reloading config from: {configPath}");
                    _config.LoadFromFile(configPath);
                    
                    // Update tray icon manager with new config
                    _trayIconManager?.UpdateConfig(_config);
                    
                    Utils.Log(_config, "Config reloaded successfully");
                }
                else
                {
                    Utils.Log(_config, "No config file found to reload - checked global, program, and user config paths");
                }
            }
            catch (Exception ex)
            {
                Utils.Log(_config, $"Error reloading config: {ex.Message}");
            }
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            ExitThread();
        }

        private string SaveConfig(Config config)
        {
            try
            {
                // Save to the first available config path
                string? configPath = null;
                if (config.GlobalConfigPath != null)
                {
                    configPath = config.GlobalConfigPath.FullName;
                }
                else if (config.ProgramConfigPath != null)
                {
                    configPath = config.ProgramConfigPath.FullName;
                }
                else if (config.UserConfigPath != null)
                {
                    configPath = config.UserConfigPath.FullName;
                }

                if (configPath != null)
                {
                    config.SaveToFile(configPath);
                    return "";
                }
                return "No config path available";
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error saving config: {ex.Message}";
                Utils.Log(config, errorMessage);
                return errorMessage;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIconManager?.Dispose();
                _trayIconManager = null;
                _notificationManager?.Dispose();
                _notificationManager = null;
                _webServer?.Stop();
                _webServer = null;
            }
            base.Dispose(disposing);
        }
    }
}