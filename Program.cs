#pragma warning disable CA1416 // Windows-only API
using System.Runtime.InteropServices;
using NotificationBanner.Model;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using System.IO.Pipes;
using Bluscream;

namespace NotificationBanner {
    internal static class Program {
        private static WindowsFormsSynchronizationContext? _synchronizationContext;
        private static Mutex? _singleInstanceMutex;
        private const string MutexName = "notification-banner-single-instance";
        private const string PipeName = "notification-banner-pipe";
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        //[HandleProcessCorruptedStateExceptions]
        [STAThread]
        private static int Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main Thread";
            SetProcessDPIAware();

            Application.EnableVisualStyles();
#if NETCORE
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
            Application.SetCompatibleTextRenderingDefault(false);
            _synchronizationContext = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

            bool isFirstInstance;
            _singleInstanceMutex = new Mutex(true, MutexName, out isFirstInstance);
            var config = Config.Load(args);
            
            // Initialize logging system
            Bluscream.Logging.Initialize(config.LogFile, config.Console);
            
            if (!isFirstInstance)
            {
                NotificationPipeServer.SendNotification(config);
                return 0;
            }
            if (!config.Console)
            {
                Bluscream.Utils.HideConsoleWindow();
            }
            var notificationQueue = new NotificationQueue();
            
            // Only enqueue notification if message is provided
            if (!string.IsNullOrWhiteSpace(config.Message))
            {
                notificationQueue.Enqueue(config);
            }

            NotificationPipeServer pipeServer = new NotificationPipeServer();
            pipeServer.StartServer(notificationQueue.Enqueue);


            Application.Run(new MyApplicationContext(notificationQueue, config));
            return 0;
        }
    }
}
