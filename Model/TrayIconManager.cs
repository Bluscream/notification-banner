using System;
using System.Drawing;
using System.Windows.Forms;
using Bluscream;

namespace NotificationBanner.Model
{
    internal class TrayIconManager : IDisposable
    {
        private NotifyIcon? _trayIcon;
        private Config _config;
        private readonly NotificationQueue _notificationQueue;
        private readonly Func<Config, string> _saveConfigCallback;

        public TrayIconManager(Config config, NotificationQueue notificationQueue, Func<Config, string> saveConfigCallback)
        {
            _config = config;
            _notificationQueue = notificationQueue;
            _saveConfigCallback = saveConfigCallback;
        }

        public void Initialize()
        {
            if (_config.TrayIcon && _trayIcon == null)
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

        public void UpdateConfig(Config newConfig)
        {
            bool oldTrayIconSetting = _config.TrayIcon;
            _config = newConfig;

            // Handle tray icon setting changes
            if (oldTrayIconSetting != _config.TrayIcon)
            {
                if (_config.TrayIcon && _trayIcon == null)
                {
                    Utils.Log(_config, "TrayIcon enabled in config - creating tray icon");
                    Initialize();
                }
                else if (!_config.TrayIcon && _trayIcon != null)
                {
                    Utils.Log(_config, "TrayIcon disabled in config - removing tray icon");
                    Hide();
                }
            }

            // Update the context menu to reflect new config values
            if (_trayIcon?.ContextMenuStrip != null)
            {
                Utils.Log(_config, "Updating tray menu to reflect new config values");
                _trayIcon.ContextMenuStrip.Dispose();
                _trayIcon.ContextMenuStrip = CreateContextMenu();
            }
        }

        public void Show()
        {
            if (_trayIcon == null)
            {
                Initialize();
            }
            else
            {
                _trayIcon.Visible = true;
            }
        }

        public void Hide()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
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

        // Event handlers for all menu items
        public event EventHandler? ExitRequested;
        public event EventHandler? ReloadConfigRequested;

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
                        Initialize();
                        Utils.Log(_config, "Tray icon enabled");
                    }
                }
                else
                {
                    // Disable tray icon but warn user
                    Utils.Log(_config, "Warning: Tray icon disabled. Use --trayicon command line argument or set TrayIcon=true in config file to re-enable.");
                    Hide();
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
            ReloadConfigRequested?.Invoke(sender, e);
        }

        private void ExitApplication_Click(object? sender, EventArgs e)
        {
            ExitRequested?.Invoke(sender, e);
        }

        private void SaveConfig()
        {
            try
            {
                var result = _saveConfigCallback(_config);
                // Result contains any error message, but we'll let the callback handle logging
            }
            catch (Exception ex)
            {
                Utils.Log(_config, $"Error saving config: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }
}