using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using CannockAutomation.Notifications;

namespace CannockAutomation
{
    public static class ProgramRecovery
    {

        public delegate int RecoveryDelegate(RecoveryData parameterData);
        
        [DllImport("kernel32.dll")]
        public static extern int RegisterApplicationRecoveryCallback(RecoveryDelegate recoveryCallback, RecoveryData parameterData, uint pingInterval, uint flags);

        [DllImport("kernel32.dll")]
        public static extern int ApplicationRecoveryInProgress(out bool canceled);

        [DllImport("kernel32.dll")]
        public static extern void ApplicationRecoveryFinished(bool success);

        [DllImport("kernel32.dll")]
        public static extern int UnregisterApplicationRecoveryCallback();

        [DllImport("kernel32.dll")]
        public static extern int GetApplicationRecoveryCallback(IntPtr processHandle, out RecoveryDelegate recoveryCallback, out string parameter, out uint pingInterval, out uint flags);

        [DllImport("kernel32.dll")]
        public static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.BStr)] string commandLineArgs, int flags);

        [Flags]
        public enum RestartRestrictions
        {
            None = 0,
            NotOnCrash = 1,
            NotOnHang = 2,
            NotOnPatch = 4,
            NotOnReboot = 8
        }

        [DllImport("kernel32.dll")]
        public static extern int UnregisterApplicationRestart();

        [DllImport("KERNEL32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetApplicationRestartSettings(IntPtr process, IntPtr commandLine, ref uint size, out uint flags);

        private static void DisplayRestartSettings()
        {
            IntPtr cmdptr = IntPtr.Zero;
            uint size = 0, flags;

            // Find out how big a buffer to allocate
            // for the command line.
            GetApplicationRestartSettings(Process.GetCurrentProcess().Handle, IntPtr.Zero, ref size, out flags);

            // Allocate the buffer on the unmanaged heap.
            cmdptr = Marshal.AllocHGlobal((int)size * sizeof(char));

            // Get the settings using the buffer.
            var ret = GetApplicationRestartSettings(Process.GetCurrentProcess().Handle, cmdptr, ref size, out flags);

            // Read the buffer's contents as a unicode string.
            var cmd = Marshal.PtrToStringUni(cmdptr);

            Console.WriteLine("cmdline: {0} size: {1} flags: {2}", cmd, size, flags);
            Console.WriteLine();

            // Free the buffer.
            Marshal.FreeHGlobal(cmdptr);
        }
        
        public static void RegisterForRecovery()
        {
            // Create the delegate that will invoke the recovery method.
            var recoveryCallback = new RecoveryDelegate(RecoveryProcedure);
            uint pingInterval = 5000;
            uint flags = 0;
            var parameter = RecoveryData.Get();

            // Register for recovery notification.
            int regReturn = RegisterApplicationRecoveryCallback (recoveryCallback, parameter, pingInterval, flags);
        }

        public static void UnregisterRcovery()
        {
            UnregisterApplicationRecoveryCallback();
        }

        public static void ReregisterForRecovery()
        {
            UnregisterRcovery();
            RegisterForRecovery();
        }

        public static void RegisterForRestart()
        {
            // Register for automatic restart if the application 
            // was terminated for any reason.
            RegisterApplicationRestart("/restart", (int)RestartRestrictions.None);
        }

        public static void UnregisterRestart()
        {
            UnregisterApplicationRestart();
        }

        public const String RecoveryFile = "recovery.txt";

        private static int RecoveryProcedure(RecoveryData parameter)
        {
            try
            {
                bool isCanceled;
                ApplicationRecoveryInProgress(out isCanceled);
                
                // Set up timer to notify WER that recovery work is in progress.
                var pinger = new Timer(4000);
                pinger.Elapsed += new ElapsedEventHandler(PingSystem);
                pinger.Enabled = true;

                // Do recovery work here.
                WriteRecoveryFile(parameter);

                pinger.Stop();               

                // Indicate that recovery work is done.
                Pushover.Log("In Recovery Mode");
                ApplicationRecoveryFinished(true);
            }
            catch (Exception e)
            {
                // Indicate that recovery work is done.
                Pushover.Alert(e.Message, $"Lilly RecoveryProcedure {e.GetType()}");
                ApplicationRecoveryFinished(false);
            }

            return 0;
        }

        public static void WriteRecoveryFile(RecoveryData data=null)
        {
            if (data == null) { data = RecoveryData.Get(); }
            File.WriteAllText(RecoveryFile, $"WasListening={data.WasListening}");
        }

        private static void PingSystem(object source, ElapsedEventArgs e)
        {
            bool isCanceled;
            ApplicationRecoveryInProgress(out isCanceled);
            if (isCanceled)
            {
                Pushover.Log("Recovery Canceled");
                Environment.Exit(2);
            }
        }

        public static Boolean RecoverLastSession(String command)
        {
            if (!File.Exists(RecoveryFile)) return false;

            var recovery = File.ReadAllText(RecoveryFile);
            File.Delete(RecoveryFile);

            Pushover.Log($"Recovering {recovery}");

            if (recovery.Contains("WasListening=True")) Lilly.StartListening();

            return recovery != String.Empty;
        }

    }

    public class RecoveryData
    {

        public readonly Boolean WasListening;

        public static RecoveryData Get()
        {
            return new RecoveryData(Lilly.IsListening);
        }
        
        public RecoveryData(Boolean isListening)
        {
            WasListening = isListening;
        }
    }
}
