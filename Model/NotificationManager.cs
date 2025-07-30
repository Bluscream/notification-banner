using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NotificationBanner.Banner;
using Bluscream;

namespace NotificationBanner.Model {
    internal class NotificationManager {
        private readonly List<BannerForm> _activeBanners = new();
        private readonly object _lock = new();
        private readonly Config _config;
        private readonly NotificationQueue _notificationQueue;
        private readonly System.Windows.Forms.Timer _queueTimer;
        private readonly Func<Config, BannerData> _createBannerData;

        public NotificationManager(NotificationQueue notificationQueue, Config config, Func<Config, BannerData> createBannerData) {
            _notificationQueue = notificationQueue;
            _config = config;
            _createBannerData = createBannerData;
            
            _queueTimer = new System.Windows.Forms.Timer();
            _queueTimer.Interval = 100; // Check every 0.1s for faster response
            _queueTimer.Tick += (s, e) => ProcessQueue();
            ProcessQueue(); // Call before starting the timer
            _queueTimer.Start();
        }

        private void ProcessQueue() {
            lock (_lock) {
                // Remove disposed banners
                _activeBanners.RemoveAll(banner => banner.IsDisposed);
                
                // If we have room for more notifications, try to dequeue one
                if (_activeBanners.Count < _config.MaxNotificationsOnScreen) {
                    if (_notificationQueue.TryDequeue(out var config) && config != null) {
                        // Check if Do Not Disturb is active and notification is not marked as important
                        if (Bluscream.Utils.IsDoNotDisturbActive() && !config.Important) {
                            Utils.Log(config, $"[NotificationManager] Skipping notification due to Do Not Disturb mode: {config.Title} - {config.Message}");
                            ProcessQueue(); // Process next notification immediately
                            return;
                        }
                        
                        ShowNotification(config);
                    }
                }
            }
        }

        private void ShowNotification(Config config) {
            Utils.Log(config, $"[NotificationManager] ShowNotification: Title='{config.Title}', Message='{config.Message}'");
            var bannerData = _createBannerData(config);
            var bannerForm = new BannerForm();
            
            // Set up the banner's disposed event
            bannerForm.Disposed += (s, e) => {
                lock (_lock) {
                    _activeBanners.Remove(bannerForm);
                    Utils.Log(config, $"[NotificationManager] Banner disposed: Title='{config.Title}', Message='{config.Message}'");
                    // Only reposition if there are still active banners
                    if (_activeBanners.Count > 0) {
                        RepositionBanners();
                    }
                }
                
                if (config.Exit) {
                    Bluscream.Utils.Exit(0);
                } else {
                    // Process next notification immediately for faster response
                    ProcessQueue();
                }
            };
            
            // Set the data and show the banner first
            bannerForm.SetData(bannerData);
            bannerForm.Show();
            
            // Add to active banners list and position the new banner
            lock (_lock) {
                _activeBanners.Add(bannerForm);
                PositionNewBanner(bannerForm);
            }
        }

        private void PositionNewBanner(BannerForm newBanner) {
            if (_activeBanners.Count == 0) return;
            
            var bannerHeight = _activeBanners[0]?.Height ?? 100;
            var spacing = 10; // Space between banners
            var totalOffset = 0;
            
            // Calculate the offset for the new banner based on existing banners
            foreach (var banner in _activeBanners) {
                if (banner.IsDisposed) continue;
                if (banner == newBanner) continue; // Skip the new banner in calculation
                
                totalOffset += bannerHeight + spacing;
            }
            
            Utils.Log(null, $"[NotificationManager] PositionNewBanner: Title='{newBanner?.Text}', Offset={totalOffset}");
            // Position only the new banner
            newBanner?.UpdateOffset(totalOffset);
        }

        private void RepositionBanners() {
            if (_activeBanners.Count == 0) return;
            
            var bannerHeight = _activeBanners[0]?.Height ?? 100;
            var spacing = 10; // Space between banners
            var totalOffset = 0;
            
            foreach (var banner in _activeBanners) {
                if (banner.IsDisposed) continue;
                
                // Update the banner's offset for positioning
                banner.UpdateOffset(totalOffset);
                totalOffset += bannerHeight + spacing;
            }
        }



        public void Dispose() {
            _queueTimer?.Dispose();
            lock (_lock) {
                foreach (var banner in _activeBanners) {
                    if (!banner.IsDisposed) {
                        banner.Dispose();
                    }
                }
                _activeBanners.Clear();
            }
        }
    }
} 