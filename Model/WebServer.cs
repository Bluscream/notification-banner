using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NotificationBanner;

namespace NotificationBanner.Model
{
    internal class WebServer
    {
        private readonly NotificationQueue _notificationQueue;
        private readonly Config _config;
        private readonly List<TcpListener> _listeners = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _isRunning = false;

        public WebServer(NotificationQueue notificationQueue, Config config)
        {
            _notificationQueue = notificationQueue;
            _config = config;
        }

        public void Start(int apiListenPort)
        {
            if (apiListenPort <= 0 || apiListenPort > 65535)
            {
                Utils.LogError(_config, $"[WebServer] Invalid listen port: {apiListenPort}");
                return;
            }

            try
            {
                // Listen on all network interfaces using IPAddress.Any (0.0.0.0)
                var listener = new TcpListener(IPAddress.Any, apiListenPort);
                listener.Start();
                _listeners.Add(listener);
                
                Utils.Log(_config, $"[WebServer] Started listening on all interfaces (0.0.0.0):{apiListenPort}");
                
                // Start listening for requests
                _ = Task.Run(() => ListenForRequests(listener), _cancellationTokenSource.Token);
                
                _isRunning = true;
                Utils.Log(_config, "[WebServer] Web server is now running");
            }
            catch (Exception ex)
            {
                Utils.LogError(_config, $"Failed to start web server on port {apiListenPort}: {ex.Message}", ex);
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _cancellationTokenSource.Cancel();
            
            foreach (var listener in _listeners)
            {
                try
                {
                    listener.Stop();
                }
                catch (Exception ex)
                {
                    Utils.LogError(_config, "Error stopping listener", ex);
                }
            }
            
            _listeners.Clear();
            _isRunning = false;
            Utils.Log(_config, "[WebServer] Stopped");
        }

        private async Task ListenForRequests(TcpListener listener)
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client), _cancellationTokenSource.Token);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was disposed, exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    Utils.LogError(_config, "Error accepting client", ex);
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                // Get client IP address
                var clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                var clientIp = clientEndPoint?.Address.ToString() ?? "unknown";

                // Read the HTTP request
                var requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestLine))
                    return;

                var parts = requestLine.Split(' ');
                if (parts.Length < 3)
                    return;

                var method = parts[0];
                var url = parts[1];

                // Read headers
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex).Trim();
                        var value = line.Substring(colonIndex + 1).Trim();
                        headers[key] = value;
                    }
                }

                Utils.Log(_config, $"[WebServer] {clientIp}: {method} {url}");

                // Handle GET requests
                if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    var config = ParseConfigFromUrl(url, clientIp);
                    
                    if (string.IsNullOrWhiteSpace(config.Message))
                    {
                        await SendHttpResponse(writer, "Missing 'message' parameter", 400);
                        return;
                    }

                    // Enqueue the notification
                    _notificationQueue.Enqueue(config);
                    
                    await SendHttpResponse(writer, "Notification queued successfully", 200);
                }
                else
                {
                    await SendHttpResponse(writer, "Method not allowed", 405);
                }
            }
            catch (IOException ex) when (ex.Message.Contains("aborted") || ex.Message.Contains("closed"))
            {
                // Client disconnected before we could send response - this is normal
                Utils.Log(_config, $"[WebServer] Client disconnected before response could be sent: {ex.Message}");
            }
            catch (Exception ex)
            {
                Utils.LogError(_config, "Error handling client", ex);
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Utils.Log(_config, $"[WebServer] Error closing client connection: {ex.Message}");
                }
            }
        }

        private async Task SendHttpResponse(StreamWriter writer, string message, int statusCode)
        {
            try
            {
                var statusText = statusCode switch
                {
                    200 => "OK",
                    400 => "Bad Request",
                    405 => "Method Not Allowed",
                    _ => "Internal Server Error"
                };

                var responseData = new { status = statusText, message = message };
                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(responseData);
                var contentLength = Encoding.UTF8.GetByteCount(jsonResponse);

                // Write HTTP response headers
                await writer.WriteAsync($"HTTP/1.1 {statusCode} {statusText}\r\n");
                await writer.WriteAsync("Content-Type: application/json; charset=utf-8\r\n");
                await writer.WriteAsync($"Content-Length: {contentLength}\r\n");
                await writer.WriteAsync("Access-Control-Allow-Origin: *\r\n");
                await writer.WriteAsync("Access-Control-Allow-Methods: GET, POST, PUT, DELETE, PATCH, OPTIONS\r\n");
                await writer.WriteAsync("Access-Control-Allow-Headers: Content-Type\r\n");
                await writer.WriteAsync("Connection: close\r\n");
                await writer.WriteAsync("\r\n");
                await writer.WriteAsync(jsonResponse);
                await writer.FlushAsync();
            }
            catch (IOException ex) when (ex.Message.Contains("aborted") || ex.Message.Contains("closed"))
            {
                // Client disconnected - this is normal behavior
                Utils.Log(_config, $"[WebServer] Client disconnected during response: {ex.Message}");
            }
            catch (Exception ex)
            {
                Utils.LogError(_config, "Error sending HTTP response", ex);
            }
        }

        private Config ParseConfigFromUrl(string url, string clientIp)
        {
            var config = new Config();
            
            // Parse query parameters
            var queryIndex = url.IndexOf('?');
            if (queryIndex >= 0)
            {
                var query = url.Substring(queryIndex + 1);
                var queryParams = ParseQueryString(query);
                
                // Map query parameters to config properties
                config.Message = queryParams.GetValueOrDefault("message");
                config.Title = queryParams.GetValueOrDefault("title") ?? $"Notification from {clientIp}";
                config.Time = queryParams.GetValueOrDefault("time") ?? "10";
                config.Image = queryParams.GetValueOrDefault("image");
                config.Position = queryParams.GetValueOrDefault("position")?.ToLowerInvariant() ?? "topleft";
                config.Exit = queryParams.GetValueOrDefault("exit")?.ToLowerInvariant() == "true";
                config.Color = queryParams.GetValueOrDefault("color");
                config.Sound = queryParams.GetValueOrDefault("sound") ?? "C:\\Windows\\Media\\Windows Notify System Generic.wav";
                config.Size = queryParams.GetValueOrDefault("size") ?? "100";
                config.Primary = queryParams.GetValueOrDefault("primary")?.ToLowerInvariant() == "true";
                config.Important = queryParams.GetValueOrDefault("important")?.ToLowerInvariant() == "true";

                // Handle image based on title (same logic as in Config.Load)
                if (string.IsNullOrEmpty(config.Image) && !string.IsNullOrEmpty(config.Title))
                {
                    foreach (var kvp in config.DefaultImages)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(config.Title, kvp.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            config.Image = kvp.Value;
                            break;
                        }
                    }
                }
            }

            return config;
        }

        private Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(query))
                return result;

            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    var value = Uri.UnescapeDataString(keyValue[1]);
                    result[key] = value;
                }
                else if (keyValue.Length == 1)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    result[key] = string.Empty;
                }
            }
            
            return result;
        }

        private List<string> GetNetworkInterfaceAddresses()
        {
            var addresses = new List<string>();
            
            try
            {
                // Always include localhost (doesn't require admin privileges)
                addresses.Add("127.0.0.1");
                
                // Try to get network interfaces, but don't fail if we can't
                try
                {
                    var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                    
                    foreach (var networkInterface in networkInterfaces)
                    {
                        // Only include interfaces that are up and not loopback
                        if (networkInterface.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                            networkInterface.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        {
                            var ipProperties = networkInterface.GetIPProperties();
                            
                            foreach (var ipAddress in ipProperties.UnicastAddresses)
                            {
                                // Only include IPv4 addresses
                                if (ipAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    var ip = ipAddress.Address.ToString();
                                    if (!addresses.Contains(ip))
                                    {
                                        addresses.Add(ip);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.Log(_config, $"[WebServer] Could not enumerate network interfaces: {ex.Message}. Using localhost only.");
                }
                
                Utils.Log(_config, $"[WebServer] Found {addresses.Count} network interfaces to bind to");
            }
            catch (Exception ex)
            {
                Utils.LogError(_config, "Error getting network interfaces", ex);
                // Fallback to localhost only
                addresses.Clear();
                addresses.Add("127.0.0.1");
            }
            
            return addresses;
        }
    }
} 