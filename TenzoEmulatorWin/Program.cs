using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TenzoEmulatorWin
{
    internal static class Program
    {
        static Mutex? mutex;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

 
        private const int WM_SHOWME = 0x8001; // пользовательское сообщение

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool isNewInstance;

            mutex = new Mutex(true, "Global\\MyUniqueAppMutexName123123", out isNewInstance);

            if (!isNewInstance)
            {
                IntPtr hWnd = FindWindow(null, "Tenzo Emulator"); // <-- точное имя формы!

                if (hWnd != IntPtr.Zero)
                    PostMessage(hWnd, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);

                return;
            }
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}