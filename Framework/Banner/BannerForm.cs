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
using Bluscream;

namespace NotificationBanner.Banner {
    /// <summary>
    /// This class implements the UI form used to show a Banner notification.
    /// </summary>
    public partial class BannerForm : Form {
        // --- Constants ---
        private const int BaseWidth = 438;
        private const int BaseHeight = 100;
        private const int BaseLogoSize = 40;
        private const int BaseLogoMargin = 15;
        private const int BaseLabelWidth = 350;
        private const int BaseLabelHeightTop = 25;
        private const int BaseLabelHeightTitle = 50;
        private const int BaseLabelMarginLeft = 70;
        private const int BaseLabelMarginTop = 15;
        private const int BaseLabelMarginTitleTop = 40;
        private const float BaseFontSizeTop = 12.5f;
        private const float BaseFontSizeTitle = 11.25f;
        private const double DefaultOpacity = 0.9;
        private static readonly Color DefaultBackColor = Color.FromArgb(45, 45, 45);

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
        /// Constructor for the <see cref="BannerForm"/> class
        /// </summary>
        public BannerForm() {
            StartPosition = FormStartPosition.Manual;
            Size = new Size(BaseWidth, BaseHeight);
            TopMost = true;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = DefaultBackColor;
            ForeColor = Color.White;
            Padding = new Padding(0);

            pbxLogo = new PictureBox {
                Size = new Size(BaseLogoSize, BaseLogoSize),
                Location = new Point(BaseLogoMargin, BaseLogoMargin),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            lblTop = new Label {
                AutoSize = false,
                Size = new Size(BaseLabelWidth, BaseLabelHeightTop),
                Location = new Point(BaseLabelMarginLeft, BaseLabelMarginTop),
                Font = new Font("Segoe UI", BaseFontSizeTop, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblTitle = new Label {
                AutoSize = false,
                Size = new Size(BaseLabelWidth, BaseLabelHeightTitle),
                Location = new Point(BaseLabelMarginLeft, BaseLabelMarginTitleTop),
                Font = new Font("Segoe UI", BaseFontSizeTitle),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft
            };

            Controls.Add(pbxLogo);
            Controls.Add(lblTop);
            Controls.Add(lblTitle);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams {
            get {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
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
            Size = new Size((int)(BaseWidth * scaleFactor), (int)(BaseHeight * scaleFactor));
            pbxLogo.Size = new Size((int)(BaseLogoSize * scaleFactor), (int)(BaseLogoSize * scaleFactor));
            pbxLogo.Location = new Point((int)(BaseLogoMargin * scaleFactor), (int)(BaseLogoMargin * scaleFactor));
            lblTop.Size = new Size((int)(BaseLabelWidth * scaleFactor), (int)(BaseLabelHeightTop * scaleFactor));
            lblTop.Location = new Point((int)(BaseLabelMarginLeft * scaleFactor), (int)(BaseLabelMarginTop * scaleFactor));
            lblTop.Font = new Font("Segoe UI", (float)(BaseFontSizeTop * scaleFactor), FontStyle.Bold);
            lblTitle.Size = new Size((int)(BaseLabelWidth * scaleFactor), (int)(BaseLabelHeightTitle * scaleFactor));
            lblTitle.Location = new Point((int)(BaseLabelMarginLeft * scaleFactor), (int)(BaseLabelMarginTitleTop * scaleFactor));
            lblTitle.Font = new Font("Segoe UI", (float)(BaseFontSizeTitle * scaleFactor));
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
            var ttl = TimeSpan.FromSeconds(int.TryParse(data.Config.Time, out var seconds) ? seconds : 10);
            if (_timerHide == null) {
                _timerHide = new Timer { Interval = (int)ttl.TotalMilliseconds };
                _timerHide.Tick += TimerHide_Tick!;
            } else {
                _timerHide.Enabled = false;
            }
            var sizeArg = string.IsNullOrWhiteSpace(data.Config.Size) ? "100" : data.Config.Size;
            if (double.TryParse(sizeArg, out var sizeValue)) {
                var scaleFactor = sizeValue / 100.0;
                ApplySizeScaling(scaleFactor);
            }
            pbxLogo.Image = data.Image ?? Bluscream.Utils.CreateDefaultIcon();
            ApplyBackgroundColorAndOpacity(data.Config.Color);
            _hiding = false;
            lblTop.Text = data.Config.Title ?? string.Empty;
            lblTitle.Text = data.Config.Message ?? string.Empty;
            Region = Region.FromHrgn(RoundedCorner.CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
            if (data.Position != null) {
                var (x, y) = data.Position(Width, Height, _currentOffset);
                Location = new Point(x, y);
            }
            _timerHide.Enabled = true;
            if (!Visible) Show();
            TopMost = true;
            PlaySoundAsync(data.Config.Sound);
        }

        /// <summary>
        /// Parse color and opacity from a string and apply to the form.
        /// </summary>
        private void ApplyBackgroundColorAndOpacity(string? colorString) {
            var (color, opacity) = Bluscream.Utils.ParseColorAndOpacity(colorString, DefaultBackColor, DefaultOpacity);
            BackColor = color;
            Opacity = opacity;
        }

        /// <summary>
        /// Play sound from file path or URL (async, disposes previous player/stream)
        /// </summary>
        private async void PlaySoundAsync(string? soundPath) {
            if (string.IsNullOrWhiteSpace(soundPath)) return;
            StopSound();
            try {
                _soundPlayer = new SoundPlayer();
                if (Uri.TryCreate(soundPath, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {
                    using var httpClient = new System.Net.Http.HttpClient();
                    var audioData = await httpClient.GetByteArrayAsync(uri);
                    _soundStream = new MemoryStream(audioData);
                    _soundPlayer.Stream = _soundStream;
                    _soundPlayer.Play();
                } else {
                    _soundPlayer.SoundLocation = soundPath;
                    _soundPlayer.Play();
                }
            } catch (Exception ex) {
                LogError($"[Sound] Error playing sound {soundPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop current sound playback and dispose resources, and cancel any sound download
        /// </summary>
        private void StopSound() {
            try {
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();
                _soundPlayer = null;
                _soundStream?.Dispose();
                _soundStream = null;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new();
            } catch (Exception ex) {
                LogError($"[Sound] Error stopping sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Centralized error logging
        /// </summary>
        private void LogError(string message) {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Update Location of banner depending of the position change
        /// </summary>
        public void UpdateLocationOpacity(int positionChange, double opacityChange, int hideChange) {
            _currentOffset += positionChange;
            if (_currentData != null && _currentData.Position != null) {
                var (x, y) = _currentData.Position(Width, Height, _currentOffset);
                Location = new Point(x, y);
            }
            Opacity -= opacityChange;
            _hide -= hideChange;
            if (Opacity <= 0.0 || _hide <= 0) {
                _hiding = true;
                Dispose();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _timerHide?.Dispose();
                StopSound();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Event handler for the "hiding" timer.
        /// </summary>
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
    }
}