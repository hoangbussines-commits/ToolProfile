using System.Runtime.InteropServices;

namespace ToolProfile
{
    internal static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);
        [STAThread]
        static void Main()
        {
            SetCurrentProcessExplicitAppUserModelID("ToolProfile.MainApp");
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}