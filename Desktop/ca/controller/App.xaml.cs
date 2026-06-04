using System;
using System.Windows;

namespace Controller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try { ThemeManager.LoadSavedTheme(); }
            catch { /* Theme init failed, using defaults */ }
        }
    }
}
