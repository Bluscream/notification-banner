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
        private NotifyIcon? _trayIcon;
        private Config _config;
        internal MyApplicationContext(NotificationQueue notificationQueue, Config config) {
            _notificationQueue = notificationQueue;
            _config = config;
            _notificationQueue.SetConfigForLogging(config);
            _notificationManager = new NotificationManager(notificationQueue, config, CreateBannerData);
            StartWebServer(config);
            InitializeTrayIcon();
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

        private void InitializeTrayIcon()
        {
            if (_config.TrayIcon)
            {
                _trayIcon = new NotifyIcon()
                {
                    Icon = SystemIcons.Information,
                    ContextMenuStrip = CreateContextMenu(),
                    Text = "Notification Banner",
                    Visible = true
                };
            }
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            // Boolean config toggles
            var primaryMenuItem = new ToolStripMenuItem("Use Primary Screen", null, TogglePrimary_Click)
            {
                Checked = _config.Primary,
                CheckOnClick = true
            };
            contextMenu.Items.Add(primaryMenuItem);
            
            var importantMenuItem = new ToolStripMenuItem("Important Mode", null, ToggleImportant_Click)
            {
                Checked = _config.Important,
                CheckOnClick = true
            };
            contextMenu.Items.Add(importantMenuItem);
            
            var consoleMenuItem = new ToolStripMenuItem("Show Console", null, ToggleConsole_Click)
            {
                Checked = _config.Console,
                CheckOnClick = true
            };
            contextMenu.Items.Add(consoleMenuItem);
            
            var trayIconMenuItem = new ToolStripMenuItem("Show Tray Icon", null, ToggleTrayIcon_Click)
            {
                Checked = _config.TrayIcon,
                CheckOnClick = true
            };
            contextMenu.Items.Add(trayIconMenuItem);
            
            var createDefaultConfigMenuItem = new ToolStripMenuItem("Create Default Configs", null, CreateDefaultConfigs_Click);
            contextMenu.Items.Add(createDefaultConfigMenuItem);
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Reload config
            var reloadMenuItem = new ToolStripMenuItem("Reload Config", null, ReloadConfig_Click);
            contextMenu.Items.Add(reloadMenuItem);
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Exit
            var exitAppMenuItem = new ToolStripMenuItem("Exit", null, ExitApplication_Click);
            contextMenu.Items.Add(exitAppMenuItem);
            
            return contextMenu;
        }

        private void TogglePrimary_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                _config.Primary = menuItem.Checked;
                SaveConfig();
            }
        }

        private void ToggleImportant_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                _config.Important = menuItem.Checked;
                SaveConfig();
            }
        }

        private void ToggleConsole_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                _config.Console = menuItem.Checked;
                SaveConfig();
                
                // Apply console setting immediately
                if (_config.Console)
                {
                    Bluscream.Utils.CreateConsole();
                    Bluscream.Utils.SetConsoleTitle("Notification Banner");
                }
                else
                {
                    Bluscream.Utils.HideConsoleWindow();
                }
            }
        }

        private void ToggleTrayIcon_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                _config.TrayIcon = menuItem.Checked;
                SaveConfig();
                
                // Apply tray icon setting immediately
                if (_config.TrayIcon)
                {
                    // Re-enable tray icon if it was disabled
                    if (_trayIcon == null)
                    {
                        InitializeTrayIcon();
                        Utils.Log(_config, "Tray icon enabled");
                    }
                }
                else
                {
                    // Disable tray icon but warn user
                    Utils.Log(_config, "Warning: Tray icon disabled. Use --trayicon command line argument or set TrayIcon=true in config file to re-enable.");
                    if (_trayIcon != null)
                    {
                        _trayIcon.Visible = false;
                        _trayIcon.Dispose();
                        _trayIcon = null;
                    }
                }
            }
        }

        private void CreateDefaultConfigs_Click(object? sender, EventArgs e)
        {
            try
            {
                int createdCount = 0;
                
                // Create global config if it doesn't exist
                if (_config.GlobalConfigPath?.Exists != true && !string.IsNullOrEmpty(_config.GlobalConfigPath?.FullName))
                {
                    _config.SaveToFile(_config.GlobalConfigPath.FullName);
                    Utils.Log(_config, $"Created global config: {_config.GlobalConfigPath.FullName}");
                    createdCount++;
                }
                
                // Create program config if it doesn't exist
                if (_config.ProgramConfigPath?.Exists != true && !string.IsNullOrEmpty(_config.ProgramConfigPath?.FullName))
                {
                    _config.SaveToFile(_config.ProgramConfigPath.FullName);
                    Utils.Log(_config, $"Created program config: {_config.ProgramConfigPath.FullName}");
                    createdCount++;
                }
                
                // Create user config if it doesn't exist
                if (_config.UserConfigPath?.Exists != true && !string.IsNullOrEmpty(_config.UserConfigPath?.FullName))
                {
                    _config.SaveToFile(_config.UserConfigPath.FullName);
                    Utils.Log(_config, $"Created user config: {_config.UserConfigPath.FullName}");
                    createdCount++;
                }
                
                if (createdCount > 0)
                {
                    Utils.Log(_config, $"Successfully created {createdCount} default config file(s)");
                }
                else
                {
                    Utils.Log(_config, "All default config files already exist");
                }
            }
            catch (Exception ex)
            {
                Utils.Log(_config, $"Error creating default configs: {ex.Message}");
            }
        }

        private void ReloadConfig_Click(object? sender, EventArgs e)
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
                    bool oldTrayIconSetting = _config.TrayIcon;
                    _config.LoadFromFile(configPath);
                    
                    // Handle tray icon setting changes
                    if (oldTrayIconSetting != _config.TrayIcon)
                    {
                        if (_config.TrayIcon && _trayIcon == null)
                        {
                            Utils.Log(_config, "TrayIcon enabled in config - creating tray icon");
                            InitializeTrayIcon();
                        }
                        else if (!_config.TrayIcon && _trayIcon != null)
                        {
                            Utils.Log(_config, "TrayIcon disabled in config - removing tray icon");
                            _trayIcon.Visible = false;
                            _trayIcon.Dispose();
                            _trayIcon = null;
                        }
                    }
                    
                    // Update the context menu to reflect new config values
                    if (_trayIcon?.ContextMenuStrip != null)
                    {
                        Utils.Log(_config, "Updating tray menu to reflect new config values");
                        _trayIcon.ContextMenuStrip.Dispose();
                        _trayIcon.ContextMenuStrip = CreateContextMenu();
                    }
                    
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

        private void SaveConfig()
        {
            try
            {
                // Save to the first available config path
                string? configPath = null;
                if (_config.GlobalConfigPath != null)
                {
                    configPath = _config.GlobalConfigPath.FullName;
                }
                else if (_config.ProgramConfigPath != null)
                {
                    configPath = _config.ProgramConfigPath.FullName;
                }
                else if (_config.UserConfigPath != null)
                {
                    configPath = _config.UserConfigPath.FullName;
                }

                if (configPath != null)
                {
                    _config.SaveToFile(configPath);
                }
            }
            catch (Exception ex)
            {
                Utils.Log(_config, $"Error saving config: {ex.Message}");
            }
        }

        private void ExitApplication_Click(object? sender, EventArgs e)
        {
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
                _notificationManager?.Dispose();
                _notificationManager = null;
                _webServer?.Stop();
                _webServer = null;
            }
            base.Dispose(disposing);
        }
    }
}