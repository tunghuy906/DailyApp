using System;
using System.Windows.Forms;

namespace DailyPlannerApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}