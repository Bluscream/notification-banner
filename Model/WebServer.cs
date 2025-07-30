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
                    _config.ParseUrl(url, clientIp);
                    
                    if (string.IsNullOrWhiteSpace(_config.Message))
                    {
                        await SendHttpResponse(writer, "Missing 'message' parameter", 400);
                        return;
                    }
                    
                    _notificationQueue.Enqueue(_config);
                    
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


    }
} 