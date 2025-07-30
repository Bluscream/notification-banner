using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private readonly List<HttpListener> _listeners = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _isRunning = false;

        public WebServer(NotificationQueue notificationQueue, Config config)
        {
            _notificationQueue = notificationQueue;
            _config = config;
        }

        public void Start(string apiListenAddresses)
        {
            if (string.IsNullOrWhiteSpace(apiListenAddresses))
                return;

            var addresses = apiListenAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();

            if (!addresses.Any())
                return;

            foreach (var address in addresses)
            {
                try
                {
                    // Handle IPv6 addresses properly
                    string formattedAddress = address;
                    if (address.StartsWith("[") && address.Contains("]:"))
                    {
                        // IPv6 address like [::]:port
                        var parts = address.Split("]:", 2);
                        if (parts.Length == 2)
                        {
                            var ipv6Part = parts[0].TrimStart('[');
                            var portPart = parts[1];
                            formattedAddress = $"[{ipv6Part}]:{portPart}";
                        }
                    }
                    
                    var listener = new HttpListener();
                    var url = $"http://{formattedAddress}/";
                    listener.Prefixes.Add(url);
                    listener.Start();
                    _listeners.Add(listener);
                    
                    Utils.Log(_config, $"[WebServer] Started listening on {url}");
                    
                    // Start listening for requests
                    _ = Task.Run(() => ListenForRequests(listener), _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    if (errorMessage.Contains("The request is not supported"))
                    {
                        errorMessage = "Invalid URL format or unsupported address. Try using 127.0.0.1:port or localhost:port";
                    }
                    Utils.LogError(_config, $"Failed to start listener on {address}: {errorMessage}", ex);
                }
            }

            _isRunning = true;
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
                    listener.Close();
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

        private async Task ListenForRequests(HttpListener listener)
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context), _cancellationTokenSource.Token);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was disposed, exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    Utils.LogError(_config, "Error accepting request", ex);
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                Utils.Log(_config, $"[WebServer] {request.HttpMethod} {request.Url}");
                
                // Handle all HTTP methods
                if (request.HttpMethod == "GET" || request.HttpMethod == "POST" || 
                    request.HttpMethod == "PUT" || request.HttpMethod == "DELETE" ||
                    request.HttpMethod == "PATCH" || request.HttpMethod == "OPTIONS")
                {
                    var config = ParseConfigFromRequest(request);
                    
                    if (string.IsNullOrWhiteSpace(config.Message))
                    {
                        await SendResponse(response, "Missing 'message' parameter", HttpStatusCode.BadRequest);
                        return;
                    }

                    // Enqueue the notification
                    _notificationQueue.Enqueue(config);
                    
                    await SendResponse(response, "Notification queued successfully", HttpStatusCode.OK);
                }
                else
                {
                    await SendResponse(response, "Method not allowed", HttpStatusCode.MethodNotAllowed);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(_config, "Error handling request", ex);
                await SendResponse(response, "Internal server error", HttpStatusCode.InternalServerError);
            }
            finally
            {
                response.Close();
            }
        }

        private Config ParseConfigFromRequest(HttpListenerRequest request)
        {
            var config = new Config();
            
            // Parse query parameters manually
            var queryParams = ParseQueryString(request.Url?.Query ?? "");
            
            // Map query parameters to config properties
            config.Message = queryParams.GetValueOrDefault("message");
            config.Title = queryParams.GetValueOrDefault("title") ?? "Notification";
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

            return config;
        }

        private Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(query))
                return result;

            // Remove the leading '?' if present
            if (query.StartsWith("?"))
                query = query.Substring(1);

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

        private async Task SendResponse(HttpListenerResponse response, string message, HttpStatusCode statusCode)
        {
            response.StatusCode = (int)statusCode;
            response.ContentType = "application/json";
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            var responseData = new
            {
                success = statusCode == HttpStatusCode.OK,
                message = message,
                timestamp = DateTime.UtcNow
            };

            var json = System.Text.Json.JsonSerializer.Serialize(responseData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
} 