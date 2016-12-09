using System;
using System.Windows.Forms;

namespace RPowerLogViewer
{
    static class Program
    {
        private static MainView mainView;

        public static MainView GetMainView()
        {
            return mainView;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(mainView = new MainView());
        }
    }
}
