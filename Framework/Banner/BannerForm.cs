#pragma warning disable CA1416 // Windows-only API

using System;
using System.ComponentModel; // For Win32Exception
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using Timer = System.Windows.Forms.Timer;
using SoundSwitch.UI.Menu.Util;
using NotificationBanner.Banner;
using System.IO;

namespace NotificationBanner.Banner {
    /// <summary>
    /// This class implements the UI form used to show a Banner notification.
    /// </summary>
    public partial class BannerForm : Form {
        private Timer? _timerHide;
        private bool _hiding;
        private BannerData? _currentData;
        private CancellationTokenSource _cancellationTokenSource = new();
        private int _currentOffset;
        private int _hide = 100;
        public Guid Id { get; } = Guid.NewGuid();
        private Label lblTop;
        private Label lblTitle;
        private PictureBox pbxLogo;
        private SoundPlayer? _soundPlayer;
        private MemoryStream? _soundStream;

        /// <summary>
        /// Get the Screen object
        /// </summary>
        private static Screen GetScreen() {
            return (false ? Screen.PrimaryScreen : Screen.FromPoint(Cursor.Position))!; // bool.Parse(ConfigurationManager.AppSettings["NotifyUsingPrimaryScreen"])
        }

        /// <summary>
        /// Constructor for the <see cref="BannerForm"/> class
        /// </summary>
        public BannerForm() {
            StartPosition = FormStartPosition.Manual;
            Size = new System.Drawing.Size(438, 100); // 350 * 1.25 = 438, 80 * 1.25 = 100
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
            ForeColor = System.Drawing.Color.White;
            Padding = new System.Windows.Forms.Padding(0);

            // Create UI controls
            pbxLogo = new PictureBox {
                Size = new Size(40, 40), // 32 * 1.25 = 40
                Location = new Point(15, 15), // 12 * 1.25 = 15
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            lblTop = new Label {
                AutoSize = false,
                Size = new Size(350, 25), // 280 * 1.25 = 350, 20 * 1.25 = 25
                Location = new Point(70, 15), // 56 * 1.25 = 70, 12 * 1.25 = 15
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold), // 10 * 1.25 = 12.5
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblTitle = new Label {
                AutoSize = false,
                Size = new Size(350, 50), // 280 * 1.25 = 350, 40 * 1.25 = 50
                Location = new Point(70, 40), // 56 * 1.25 = 70, 32 * 1.25 = 40
                Font = new Font("Segoe UI", 11.25f), // 9 * 1.25 = 11.25
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft
            };

            Controls.Add(pbxLogo);
            Controls.Add(lblTop);
            Controls.Add(lblTitle);

            // Remove focus/activation logic
            // this.Shown += (s, e) => {
            //     this.TopMost = true;
            //     this.BringToFront();
            //     this.Activate();
            // };
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams {
            get {
                var cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                // Used to hide the banner from alt+tab
                cp.ExStyle |= 0x80;
                // Add WS_EX_TRANSPARENT (0x20) and WS_EX_NOACTIVATE (0x8000000)
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
                cp.ExStyle |= 0x8000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        /// <summary>
        /// Apply size scaling to the form and controls
        /// </summary>
        /// <param name="scaleFactor">Scale factor (100 = 100%, 150 = 150%, etc.)</param>
        private void ApplySizeScaling(double scaleFactor) {
            if (scaleFactor <= 0) scaleFactor = 1.0;
            
            // Scale form size
            var baseWidth = 438; // 350 * 1.25 = 438
            var baseHeight = 100; // 80 * 1.25 = 100
            Size = new Size((int)(baseWidth * scaleFactor), (int)(baseHeight * scaleFactor));
            
            // Scale control sizes and positions
            pbxLogo.Size = new Size((int)(40 * scaleFactor), (int)(40 * scaleFactor)); // 32 * 1.25 = 40
            pbxLogo.Location = new Point((int)(15 * scaleFactor), (int)(15 * scaleFactor)); // 12 * 1.25 = 15
            
            lblTop.Size = new Size((int)(350 * scaleFactor), (int)(25 * scaleFactor)); // 280 * 1.25 = 350, 20 * 1.25 = 25
            lblTop.Location = new Point((int)(70 * scaleFactor), (int)(15 * scaleFactor)); // 56 * 1.25 = 70, 12 * 1.25 = 15
            lblTop.Font = new Font("Segoe UI", (float)(12.5 * scaleFactor), FontStyle.Bold); // 10 * 1.25 = 12.5
            
            lblTitle.Size = new Size((int)(350 * scaleFactor), (int)(50 * scaleFactor)); // 280 * 1.25 = 350, 40 * 1.25 = 50
            lblTitle.Location = new Point((int)(70 * scaleFactor), (int)(40 * scaleFactor)); // 56 * 1.25 = 70, 32 * 1.25 = 40
            lblTitle.Font = new Font("Segoe UI", (float)(11.25 * scaleFactor)); // 9 * 1.25 = 11.25
        }

        /// <summary>
        /// Called internally to configure pass notification parameters
        /// </summary>
        /// <param name="data">The configuration data to setup the notification UI</param>
        internal void SetData(BannerData data) {
            if (_currentData != null && _currentData.Priority > data.Priority) {
                return;
            }

            _currentData = data;
            var ttl = TimeSpan.FromSeconds(int.TryParse(data.Config.Time, out int seconds) ? seconds : 10);
            if (_timerHide == null) {
                _timerHide = new Timer { Interval = (int)ttl.TotalMilliseconds };
                _timerHide.Tick += TimerHide_Tick!;
            } else {
                _timerHide.Enabled = false;
            }

            // Apply size scaling
            var sizeArg = string.IsNullOrWhiteSpace(data.Config.Size) ? "100" : data.Config.Size;
            if (double.TryParse(sizeArg, out double sizeValue)) {
                var scaleFactor = sizeValue / 100.0;
                ApplySizeScaling(scaleFactor);
            }

            if (data.Image != null) {
                pbxLogo.Image = data.Image;
            } else {
                pbxLogo.Image = CreateDefaultIcon();
            }

            // Handle background color and opacity from config
            var config = data.Config;
            if (!string.IsNullOrWhiteSpace(config.Color)) {
                try {
                    var colorStr = config.Color.TrimStart('#');
                    Color color;
                    double opacity = 0.9;
                    if (colorStr.Length == 8) { // AARRGGBB
                        byte a = Convert.ToByte(colorStr.Substring(0, 2), 16);
                        byte r = Convert.ToByte(colorStr.Substring(2, 2), 16);
                        byte g = Convert.ToByte(colorStr.Substring(4, 2), 16);
                        byte b = Convert.ToByte(colorStr.Substring(6, 2), 16);
                        color = Color.FromArgb(r, g, b); // Use RGB for BackColor
                        opacity = a / 255.0;
                    } else if (colorStr.Length == 6) { // RRGGBB
                        byte r = Convert.ToByte(colorStr.Substring(0, 2), 16);
                        byte g = Convert.ToByte(colorStr.Substring(2, 2), 16);
                        byte b = Convert.ToByte(colorStr.Substring(4, 2), 16);
                        color = Color.FromArgb(r, g, b);
                        opacity = 0.9;
                    } else {
                        color = Color.FromArgb(45, 45, 45);
                    }
                    BackColor = color;
                    Opacity = opacity;
                } catch {
                    BackColor = Color.FromArgb(45, 45, 45);
                    Opacity = 0.9;
                }
            } else {
                BackColor = Color.FromArgb(45, 45, 45);
                Opacity = 0.9;
            }

            _hiding = false;
            lblTop.Text = config.Title ?? string.Empty;
            lblTitle.Text = config.Message ?? string.Empty;
            Region = Region.FromHrgn(RoundedCorner.CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            if (data.Position != null) {
                var (x, y) = data.Position(Width, Height, _currentOffset);
                Location = new System.Drawing.Point(x, y);
            }

            _timerHide.Enabled = true;

            Show();
            TopMost = true; // Ensure always on top while visible
            // Do not call BringToFront or Activate

            // Play sound if specified
            PlaySound(config.Sound);
        }

        /// <summary>
        /// Play sound from file path or URL
        /// </summary>
        /// <param name="soundPath">Path to WAV file or URL</param>
        private void PlaySound(string? soundPath) {
            if (string.IsNullOrWhiteSpace(soundPath)) return;

            try {
                // Stop any currently playing sound
                StopSound();

                // Create new sound player
                _soundPlayer = new SoundPlayer();

                // Check if it's a URL
                if (Uri.TryCreate(soundPath, UriKind.Absolute, out var uri) && 
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {
                    // Download and play from URL
                    Task.Run(async () => {
                        try {
                            using var httpClient = new System.Net.Http.HttpClient();
                            var audioData = await httpClient.GetByteArrayAsync(uri);
                            var stream = new MemoryStream(audioData);
                            // Assign and play on UI thread
                            if (this.InvokeRequired) {
                                this.Invoke(new Action(() => {
                                    try {
                                        _soundStream = stream;
                                        _soundPlayer.Stream = _soundStream;
                                        _soundPlayer.Play();
                                    } catch (Exception ex) {
                                        Console.WriteLine($"[Sound] Error on UI thread: {ex.Message}");
                                    }
                                }));
                            } else {
                                _soundStream = stream;
                                _soundPlayer.Stream = _soundStream;
                                _soundPlayer.Play();
                            }
                        } catch (Exception ex) {
                            Console.WriteLine($"[Sound] Error playing sound from URL {soundPath}: {ex.Message}");
                        }
                    });
                } else {
                    // Play from local file
                    _soundPlayer.SoundLocation = soundPath;
                    _soundPlayer.Play();
                }
            } catch (Exception ex) {
                Console.WriteLine($"[Sound] Error playing sound {soundPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop current sound playback
        /// </summary>
        private void StopSound() {
            try {
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();
                _soundPlayer = null;
                _soundStream?.Dispose();
                _soundStream = null;
            } catch (Exception ex) {
                Console.WriteLine($"[Sound] Error stopping sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Location of banner depending of the position change
        /// </summary>
        /// <param name="positionChange"></param>
        /// <param name="opacityChange"></param>
        /// <param name="hideChange"></param>
        public void UpdateLocationOpacity(int positionChange, double opacityChange, int hideChange) {
            _currentOffset += positionChange;
            if (_currentData != null && _currentData.Position != null)
                {
                    var (x, y) = _currentData.Position(Width, Height, _currentOffset);
                    Location = new System.Drawing.Point(x, y);
                }
            Opacity -= opacityChange;
            _hide -= hideChange;
            if (Opacity <= 0.0 || _hide <= 0) {
                _hiding = true;
                Dispose();
            }
        }

        /// <summary>
        /// Destroy current sound player (if any)
        /// </summary>
        private void DestroySound() {
            StopSound();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _timerHide?.Dispose();
                StopSound();
                _cancellationTokenSource?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Event handler for the "hiding" timer.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Arguments of the event</param>
        private void TimerHide_Tick(object sender, EventArgs e) {
            TriggerHidingDisposal();
        }

        /// <summary>
        /// Trigger hiding the banner and dispose when done fading out.
        /// </summary>
        private void TriggerHidingDisposal() {
            if (_hiding) return;

            _hiding = true;
            if (_timerHide != null)
                _timerHide.Enabled = false;
            DestroySound();
            FadeOut();
        }

        /// <summary>
        /// Implements an "fadeout" animation while hiding the window.
        /// In the end of the animation the form is self disposed.
        /// <remarks>The animation is canceled if the method <see cref="SetData"/> is called along the animation.</remarks>
        /// </summary>
        private async void FadeOut() {
            try {
                while (Opacity > 0.0) {
                    await Task.Delay(50);

                    if (!_hiding)
                        break;
                    Opacity -= 0.05;
                }

                if (_hiding) {
                    Dispose();
                }
            } catch (Win32Exception) {
                try {
                    Dispose();
                } catch (Exception) {
                    //Ignored
                }
            }
        }

        private Bitmap CreateDefaultIcon() {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap)) {
                // Dark background
                g.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), 0, 0, 32, 32);
                // Orange > symbol
                using (var pen = new Pen(Color.Orange, 2)) {
                    var points = new Point[] {
                        new Point(10, 8),
                        new Point(22, 16),
                        new Point(10, 24)
                    };
                    g.DrawLines(pen, points);
                }
            }
            return bitmap;
        }
    }
}