using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Management;


public static class Utils {
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetFocusAssistState(out int state);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int FOCUS_ASSIST_OFF = 0;
    private const int FOCUS_ASSIST_PRIORITY_ONLY = 1;
    private const int FOCUS_ASSIST_ALARMS_ONLY = 2;

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

    public static string GetOwnPath() {
        var possiblePaths = new List<string> {
            Process.GetCurrentProcess().MainModule?.FileName,
            AppContext.BaseDirectory,
            Environment.GetCommandLineArgs().FirstOrDefault(),
            Assembly.GetEntryAssembly()?.Location,
            ".",
        };
        foreach (var path in possiblePaths) {
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path)) {
                return System.IO.Path.GetFullPath(path);
            }
        }
        return null;
    }

    private static bool IsDoNotDisturbActiveRegistry() {
        try {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings");
            if (key?.GetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED") is object value) {
                return value.ToString() == "0"; // Toasts disabled = Do Not Disturb
            }
        } catch (Exception ex) {
            Console.WriteLine($"[Utils] IsDoNotDisturbActiveRegistry failed: {ex.Message}");
        }
        return false;
    }
    private static bool IsDoNotDisturbActiveFocusAssist() {
        try {
            if (GetFocusAssistState(out int state)) {
                return state == FOCUS_ASSIST_PRIORITY_ONLY || state == FOCUS_ASSIST_ALARMS_ONLY;
            }
        } catch (Exception ex) {
            Console.WriteLine($"[Utils] IsDoNotDisturbActiveFocusAssist failed: {ex.Message}");
        }
        return false;
    }
    private static bool IsDoNotDisturbActiveFocusAssistCim() {
        try {
        const int FOCUS_ASSIST_OFF = 0;
        const int FOCUS_ASSIST_PRIORITY_ONLY = 1;
        const int FOCUS_ASSIST_ALARMS_ONLY = 2;
        try
        {
            var scope = new System.Management.ManagementScope(@"\\.\root\cimv2\mdm\dmmap");
            var query = new System.Management.ObjectQuery("SELECT QuietHoursState FROM MDM_Policy_Config_QuietHours");
            using (var searcher = new System.Management.ManagementObjectSearcher(scope, query))
            using (var results = searcher.Get())
            {
                foreach (System.Management.ManagementObject obj in results)
                {
                    var stateObj = obj["QuietHoursState"];
                    if (stateObj != null && int.TryParse(stateObj.ToString(), out int state))
                    {
                        return state == FOCUS_ASSIST_PRIORITY_ONLY || state == FOCUS_ASSIST_ALARMS_ONLY;
                    }
                }
            }
        }
        } catch (Exception ex) {
            Console.WriteLine($"[Utils] IsDoNotDisturbActiveFocusAssistRegistry failed: {ex.Message}");
        }
        return false;
    }
    public static bool IsDoNotDisturbActive() {
        return IsDoNotDisturbActiveRegistry() || IsDoNotDisturbActiveFocusAssist() || IsDoNotDisturbActiveFocusAssistCim();
    }

    public static void TryExitApplication()
    {
        try {
            System.Windows.Forms.Application.Exit();
            System.Threading.Thread.Sleep(500);
        } catch (Exception ex) {
            Console.Error.WriteLine($"Application.Exit() failed: {ex}");
        }
        try {
            Environment.Exit(0);
        } catch (Exception ex) {
            Console.Error.WriteLine($"Environment.Exit() failed: {ex}");
        } try {
            var process = Process.GetCurrentProcess();
            process.Kill();
        } catch (Exception ex) {
            Console.Error.WriteLine($"Process.Kill() failed: {ex}");
        }
    }
}