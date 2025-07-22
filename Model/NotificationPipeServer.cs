using System;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;
using NotificationBanner;

namespace NotificationBanner.Model {
    internal class NotificationPipeServer {
        private const string PipeName = "notification-banner-pipe";
        public void StartServer(Action<Config> onNotificationReceived) {
            Task.Run(async () => {
                while (true) {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In)) {
                        await server.WaitForConnectionAsync();
                        using (var reader = new StreamReader(server)) {
                            var pidLine = await reader.ReadLineAsync();
                            var json = await reader.ReadToEndAsync(); // Will finish when client closes pipe
                            if (!string.IsNullOrWhiteSpace(json)) {
                                try {
                                    int pid = -1;
                                    if (!string.IsNullOrWhiteSpace(pidLine))
                                        int.TryParse(pidLine, out pid);
                                    var config = JsonSerializer.Deserialize<Config>(json);
                                    Console.WriteLine($"[PipeServer] Received notification from PID {pid}: {config?.Title} - {config?.Message}");
                                    if (config != null) onNotificationReceived(config);
                                } catch (Exception ex) {
                                    Console.WriteLine($"[PipeServer] Error deserializing notification: {ex.Message}");
                                }
                            } else {
                                Console.WriteLine("[PipeServer] Received empty notification JSON.");
                            }
                        }
                    }
                }
            });
        }
        public static void SendNotification(Config config) {
            try {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out)) {
                    client.Connect(2000); // 2s timeout
                    var json = System.Text.Json.JsonSerializer.Serialize(config);
                    using (var writer = new StreamWriter(client) { AutoFlush = true }) {
                        int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                        writer.WriteLine(pid);
                        writer.Write(json);
                        writer.Flush();
                        Console.WriteLine($"[PipeClient] Sent notification: {config?.Title} - {config?.Message} (PID {pid})");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"[PipeClient] Error sending notification: {ex.Message}");
            }
        }
    }
} 