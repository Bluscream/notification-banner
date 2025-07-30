using NotificationBanner.Banner;
using System.Drawing;
using NotificationBanner;
using Bluscream;

namespace NotificationBanner.Model {
    internal class MyApplicationContext : System.Windows.Forms.ApplicationContext {
        private readonly NotificationQueue _notificationQueue;
        private BannerForm? _bannerForm;
        private System.Windows.Forms.Timer? _queueTimer;
        private Config? _currentConfig;
        private WebServer? _webServer;
        internal MyApplicationContext(NotificationQueue notificationQueue, Config config) {
            _notificationQueue = notificationQueue;
            _notificationQueue.SetConfigForLogging(config);
            StartQueueProcessing();
            StartWebServer(config);
        }

        private void StartQueueProcessing() {
            _queueTimer = new System.Windows.Forms.Timer();
            _queueTimer.Interval = 100; // Check every 0.1s for faster response
            _queueTimer.Tick += (s, e) => ProcessQueue();
            ProcessQueue(); // Call before starting the timer
            _queueTimer.Start();
        }

        private void ProcessQueue() {
            // If there's already a visible form, dispose it to show new notifications immediately
            if (_bannerForm != null && _bannerForm.Visible) {
                _bannerForm.Dispose();
                _bannerForm = null;
            }
            
            if (_notificationQueue.TryDequeue(out var config) && config != null) {
                _currentConfig = config;
                
                // Check if Do Not Disturb is active and notification is not marked as important
                if (Bluscream.Utils.IsDoNotDisturbActive() && !config.Important) {
                    Utils.Log(config, $"[AppContext] Skipping notification due to Do Not Disturb mode: {config.Title} - {config.Message}");
                    ProcessQueue(); // Process next notification immediately
                    return;
                }
                
                var toastData = CreateBannerData(config);
                
                // Create new banner form
                _bannerForm = new BannerForm();
                _bannerForm.Disposed += (s, e) => {
                    _bannerForm = null;
                    if (_currentConfig != null && _currentConfig.Exit) {
                        Bluscream.Utils.Exit(0);
                    } else {
                        // Process next notification immediately for faster response
                        ProcessQueue();
                    }
                };
                
                _bannerForm.SetData(toastData!);
                _bannerForm.Show();
            }
        }

        private BannerData CreateBannerData(Config config) {
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webServer?.Stop();
                _webServer = null;
            }
            base.Dispose(disposing);
        }
    }
}