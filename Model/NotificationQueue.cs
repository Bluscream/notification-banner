using System.Collections.Concurrent;
using NotificationBanner;
using System;

namespace NotificationBanner.Model {
    internal class NotificationQueue {
        private readonly ConcurrentQueue<Config> _queue = new();
        private int _skipped = 0;
        private const int MaxQueueLength = 100;
        private readonly object _lock = new();

        public void Enqueue(Config config) {
            lock (_lock) {
                if (_queue.Count >= MaxQueueLength) {
                    _skipped++;
                    Console.WriteLine($"[Queue] Skipped notification: {config?.Title} - {config?.Message} (skipped count: {_skipped})");
                    return;
                }
                _queue.Enqueue(config);
                // Console.WriteLine($"[Queue] Enqueued notification: {config?.Title} - {config?.Message}");
            }
        }

        public bool TryDequeue(out Config? config) {
            lock (_lock) {
                var result = _queue.TryDequeue(out config);
                if (result && config != null)
                    // Console.WriteLine($"[Queue] Dequeued notification: {config?.Title} - {config?.Message}");
                if (_queue.IsEmpty && _skipped > 0) {
                    // Add a notification about skipped items
                    var skippedMsg = $"{_skipped} notifications were skipped.";
                    _queue.Enqueue(new Config {
                        Message = skippedMsg,
                        Title = "Notification Queue",
                        Time = "5"
                    });
                    Console.WriteLine($"[Queue] Enqueued skipped notification: {skippedMsg}");
                    _skipped = 0;
                }
                return result;
            }
        }

        public bool IsEmpty => _queue.IsEmpty;
    }
} 