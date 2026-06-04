using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Controller
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();

            // Select current theme in dropdown
            foreach (ComboBoxItem item in ThemeSelector.Items)
            {
                if (item.Content.ToString() == ThemeManager.CurrentTheme)
                {
                    ThemeSelector.SelectedItem = item;
                    break;
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeSelector.SelectedItem is ComboBoxItem item)
            {
                string theme = item.Content.ToString();
                ThemeManager.ApplyTheme(theme);
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Collect all option values into a summary
            var options = new System.Collections.Generic.List<string>();

            if (FPSUnlock.IsChecked == true) options.Add("FPS Unlock");
            if (LowMemoryMode.IsChecked == true) options.Add("Low Memory");
            if (UnfocusedRender.IsChecked == true) options.Add("Unfocused Render");
            if (AutoLaunch.IsChecked == true) options.Add("Auto Launch");
            if (AutoExecute.IsChecked == true) options.Add("Auto Execute");
            if (AutoAttach.IsChecked == true) options.Add("Auto Attach");
            if (InternalUI.IsChecked == true) options.Add("Internal UI");
            if (DiscordRPC.IsChecked == true) options.Add("Discord RPC");
            if (SilentAbs.IsChecked == true) options.Add("Silent Aim");
            if (SystemTray.IsChecked == true) options.Add("System Tray");
            if (PipeRandom.IsChecked == true) options.Add("Pipe Randomization");
            if (IntegritySpoof.IsChecked == true) options.Add("Integrity Spoof");

            string summary = options.Count > 0
                ? string.Join(", ", options)
                : "No options enabled";

            MessageBox.Show(
                $"Options applied successfully.\n\nActive: {summary}",
                "Options",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
    }
}
