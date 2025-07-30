using System.Collections.Concurrent;
using NotificationBanner;
using System;

namespace NotificationBanner.Model {
    internal class NotificationQueue {
        private readonly ConcurrentQueue<Config> _queue = new();
        private int _skipped = 0;
        private const int MaxQueueLength = 100;
        private readonly object _lock = new();
        private Config? _config; // For logging purposes

        public void Enqueue(Config config) {
            lock (_lock) {
                if (_queue.Count >= MaxQueueLength) {
                    _skipped++;
                    if (_config != null) {
                        Utils.Log(_config, $"[Queue] Skipped notification: {config?.Title} - {config?.Message} (skipped count: {_skipped})");
                    }
                    return;
                }
                
                // Start timing stopwatch when notification is enqueued
                config.TimingStopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                _queue.Enqueue(config);
                if (_config != null) {
                    Utils.Log(_config, $"[Queue] Enqueued notification: {config?.Title} - {config?.Message}");
                }
            }
        }

        public bool TryDequeue(out Config? config) {
            lock (_lock) {
                var result = _queue.TryDequeue(out config);
                if (result && config != null && _config != null) {
                    Utils.Log(_config, $"[Queue] Dequeued notification: {config?.Title} - {config?.Message}");
                }
                if (_queue.IsEmpty && _skipped > 0) {
                    // Add a notification about skipped items
                    var skippedMsg = $"{_skipped} notifications were skipped.";
                    var skippedConfig = new Config {
                        Message = skippedMsg,
                        Title = "Notification Queue",
                        Time = "5"
                    };
                    _queue.Enqueue(skippedConfig);
                    if (_config != null) {
                        Utils.Log(_config, $"[Queue] Enqueued skipped notification: {skippedMsg}");
                    }
                    _skipped = 0;
                }
                return result;
            }
        }

        public bool IsEmpty => _queue.IsEmpty;

        public void SetConfigForLogging(Config config) {
            _config = config;
        }
    }
} 