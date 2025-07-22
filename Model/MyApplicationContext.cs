using NotificationBanner.Util;
using NotificationBanner.Banner;
using System.Drawing;
using NotificationBanner;

namespace NotificationBanner.Model {
    internal class MyApplicationContext : System.Windows.Forms.ApplicationContext {
        private readonly NotificationQueue _notificationQueue;
        private BannerForm? _bannerForm;
        private System.Windows.Forms.Timer? _queueTimer;
        private Config? _currentConfig;
        internal MyApplicationContext(NotificationQueue notificationQueue) {
            _notificationQueue = notificationQueue;
            StartQueueProcessing();
        }

        private void StartQueueProcessing() {
            _queueTimer = new System.Windows.Forms.Timer();
            _queueTimer.Interval = 500; // Check every 0.5s
            _queueTimer.Tick += (s, e) => ProcessQueue();
            ProcessQueue(); // Call before starting the timer
            _queueTimer.Start();
        }

        private void ProcessQueue() {
            if (_bannerForm != null && _bannerForm.Visible) return;
            if (_notificationQueue.TryDequeue(out var config) && config != null) {
                _currentConfig = config;
                var toastData = CreateBannerData(config);
                Console.WriteLine($"[AppContext] Showing notification: {toastData?.Config?.Title} - {toastData?.Config?.Message}");
                if (_bannerForm == null || _bannerForm.IsDisposed) {
                    _bannerForm = new BannerForm();
                    _bannerForm.Disposed += (s, e) => {
                        _bannerForm = null;
                        if (_currentConfig != null && _currentConfig.Exit) {
                            NotificationBanner.Util.Utils.TryExitApplication();
                        } else {
                            ProcessQueue(); // Immediately process the next notification
                        }
                    };
                }
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
            var parsedImage = imageArg?.ParseImage();
            if (parsedImage != null) toastData.Image = parsedImage.Resize(new Size() { Width = maxImageSize, Height = maxImageSize });
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
    }
}