using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NotificationBanner.Util {
    public static class Utils {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public static void HideConsoleWindow() {
            try {
                var handle = GetConsoleWindow();
                if (handle != IntPtr.Zero) {
                    ShowWindow(handle, SW_HIDE);
                }
                var process = Process.GetCurrentProcess();
                if (process != null && process.MainWindowHandle != IntPtr.Zero) {
                    ShowWindow(process.MainWindowHandle, SW_HIDE);
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex}");
            }
        }

        public static void TryExitApplication()
        {
            try
            {
                // Try graceful exit
                System.Windows.Forms.Application.Exit();
                // Give a moment for the app to exit
                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Application.Exit() failed: {ex}");
            }
            try
            {
                // Try environment exit
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Environment.Exit() failed: {ex}");
            }
            try
            {
                // Last resort: kill the process
                var process = Process.GetCurrentProcess();
                process.Kill();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Process.Kill() failed: {ex}");
            }
        }
    }
} 