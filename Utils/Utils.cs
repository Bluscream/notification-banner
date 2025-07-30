using System;
using System.IO;
using NotificationBanner;

public static partial class Utils
{
    public static void Log(Config config, string message, params object[] args)
    {
        string formattedMessage;
        try
        {
            formattedMessage = string.Format(message, args);
        }
        catch (FormatException)
        {
            // If string.Format fails, just use the message as-is
            formattedMessage = message;
        }
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] {formattedMessage}";
        
        if (config.Console)
        {
            Console.WriteLine(logMessage);
        }
        
        if (!string.IsNullOrWhiteSpace(config.LogFile))
        {
            try
            {
                File.AppendAllText(config.LogFile, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
            }
        }
    }

    public static void LogError(Config config, string message, Exception? ex = null, params object[] args)
    {
        var errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
        Log(config, $"[ERROR] {errorMessage}", args);
        
        if (ex != null && config.Console)
        {
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        }
    }

    public static void ShowUrlReservationHelp()
    {
        Console.WriteLine("=== HttpListener URL Reservation Help ===");
        Console.WriteLine("To bind to all interfaces (*:port), you need to reserve the URL first.");
        Console.WriteLine();
        Console.WriteLine("Run these commands as Administrator:");
        Console.WriteLine();
        Console.WriteLine("1. Reserve URL for all users:");
        Console.WriteLine("   netsh http add urlacl url=http://+:14969/ user=everyone");
        Console.WriteLine();
        Console.WriteLine("2. Or reserve for current user only:");
        Console.WriteLine("   netsh http add urlacl url=http://+:14969/ user=%USERNAME%");
        Console.WriteLine();
        Console.WriteLine("3. To remove the reservation later:");
        Console.WriteLine("   netsh http delete urlacl url=http://+:14969/");
        Console.WriteLine();
        Console.WriteLine("Alternative: Use 127.0.0.1:14969 instead of *:14969 for localhost-only binding.");
        Console.WriteLine();
    }
}