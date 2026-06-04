using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;

namespace Controller
{
    public partial class MainWindow : Window
    {
        private Dictionary<TabItem, string> tabContents = new Dictionary<TabItem, string>();
        private string scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
        private ObservableCollection<TelemetryEntry> telemetryEntries = new ObservableCollection<TelemetryEntry>();
        private CancellationTokenSource telemetryCts;
        private Task telemetryListenerTask;
        private AiService aiService;
        private bool aiIsStreaming;

        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(scriptsPath)) Directory.CreateDirectory(scriptsPath);
            RefreshScriptList();
            
            // Load Lua Highlighting
            Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Lua");

            // Initialize first tab
            tabContents[ScriptTabs.Items[0] as TabItem] = "";

            // Bind telemetry list
            TelemetryListView.ItemsSource = telemetryEntries;

            // Init AI service
            try
            {
                aiService = new AiService();
                AiApiKeyBox.Password = aiService.GetApiKey();
                AiModelBox.Text = aiService.GetModel();
                if (aiService.HasApiKey)
                    AiStatusText.Text = "CONNECTED — deepseek-chat";
            }
            catch (Exception ex)
            {
                TerminalOutput.AppendText($"\n[ERROR] AI service init failed: {ex.Message}");
                aiService = new AiService();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Topmost_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = TopmostToggle.IsChecked ?? false;
        }

        private void AddTab_Click(object sender, RoutedEventArgs e)
        {
            int tabCount = ScriptTabs.Items.Count + 1;
            TabItem newTab = new TabItem { Header = $"Script {tabCount}" };
            ScriptTabs.Items.Add(newTab);
            tabContents[newTab] = "";
            ScriptTabs.SelectedItem = newTab;
        }

        private void MoveTabLeft_Click(object sender, RoutedEventArgs e)
        {
            int idx = ScriptTabs.SelectedIndex;
            if (idx <= 0) return;

            var item = ScriptTabs.Items[idx];
            var prev = ScriptTabs.Items[idx - 1];

            ScriptTabs.Items[idx] = prev;
            ScriptTabs.Items[idx - 1] = item;

            if (item is TabItem ti && prev is TabItem pi &&
                tabContents.ContainsKey(ti) && tabContents.ContainsKey(pi))
            {
                string temp = tabContents[ti];
                tabContents[ti] = tabContents[pi];
                tabContents[pi] = temp;
            }

            ScriptTabs.SelectedIndex = idx - 1;
        }

        private void MoveTabRight_Click(object sender, RoutedEventArgs e)
        {
            int idx = ScriptTabs.SelectedIndex;
            if (idx < 0 || idx >= ScriptTabs.Items.Count - 1) return;

            var item = ScriptTabs.Items[idx];
            var next = ScriptTabs.Items[idx + 1];

            ScriptTabs.Items[idx] = next;
            ScriptTabs.Items[idx + 1] = item;

            if (item is TabItem ti && next is TabItem ni &&
                tabContents.ContainsKey(ti) && tabContents.ContainsKey(ni))
            {
                string temp = tabContents[ti];
                tabContents[ti] = tabContents[ni];
                tabContents[ni] = temp;
            }

            ScriptTabs.SelectedIndex = idx + 1;
        }

        private void ScriptTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabItem oldTab)
            {
                tabContents[oldTab] = Editor.Text;
            }

            if (ScriptTabs.SelectedItem is TabItem newTab)
            {
                Editor.Text = tabContents.ContainsKey(newTab) ? tabContents[newTab] : "";
            }
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            string script = Editor.Text;
            if (string.IsNullOrEmpty(script)) return;

            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "SvxSvc_Internal", PipeDirection.InOut))
                {
                    pipeClient.Connect(2000);
                    pipeClient.ReadTimeout = 2000;

                    using (StreamReader reader = new StreamReader(pipeClient, Encoding.ASCII, false, 128, true))
                    {
                        // Read HELO from DLL
                        string heloLine = null;
                        try { heloLine = reader.ReadLine(); }
                        catch (IOException) { }

                        if (!string.IsNullOrEmpty(heloLine) && heloLine.StartsWith("HELO|"))
                        {
                            TerminalOutput.AppendText($"\n[SYSTEM] DLL connected: {heloLine}");
                        }

                        // Write script
                        byte[] buffer = Encoding.ASCII.GetBytes(script);
                        pipeClient.Write(buffer, 0, buffer.Length);

                        // Read TEL telemetry response
                        string telLine = null;
                        try { telLine = reader.ReadLine(); }
                        catch (IOException) { }

                        if (!string.IsNullOrEmpty(telLine) && telLine.StartsWith("TEL|"))
                        {
                            var parts = telLine.Split('|');
                            if (parts.Length >= 8)
                            {
                                string timestamp = parts[1];
                                string pid = parts[2];
                                string tid = parts[3];
                                string luaState = parts[4];
                                string identity = parts[5];
                                string success = parts[6];
                                string source = parts[7];
                                string status = success == "1" ? "OK" : "FAIL";

                                TerminalOutput.AppendText($"\n[TEL] [{status}] {identity} | {timestamp} | {source}");

                                telemetryEntries.Insert(0, new TelemetryEntry
                                {
                                    Time = timestamp,
                                    Status = status,
                                    Identity = identity,
                                    Source = source
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TerminalOutput.AppendText($"\n[ERROR] Pipe connection failed: {ex.Message}");
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => Editor.Text = "";

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                try {
                    Editor.Text = File.ReadAllText(dialog.FileName);
                } catch (Exception ex) {
                    MessageBox.Show($"Failed to read file: {ex.Message}");
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            if (dialog.ShowDialog() == true)
            {
                try {
                    File.WriteAllText(dialog.FileName, Editor.Text);
                } catch (Exception ex) {
                    MessageBox.Show($"Failed to save file: {ex.Message}");
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshScriptList();

        private void RefreshScriptList()
        {
            ScriptList.Items.Clear();
            if (Directory.Exists(scriptsPath))
            {
                var files = Directory.GetFiles(scriptsPath, "*.*")
                                     .Where(s => s.EndsWith(".lua") || s.EndsWith(".txt"));
                foreach (var file in files)
                {
                    ScriptList.Items.Add(Path.GetFileName(file));
                }
            }
        }

        private void ScriptList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ScriptList.SelectedItem != null)
            {
                string fileName = ScriptList.SelectedItem.ToString();
                string fullPath = Path.Combine(scriptsPath, fileName);
                try {
                    Editor.Text = File.ReadAllText(fullPath);
                } catch (Exception ex) {
                    MessageBox.Show($"Failed to read script: {ex.Message}");
                }
            }
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow();
            options.Owner = this;
            options.ShowDialog();
        }

        private void ScriptHub_Click(object sender, RoutedEventArgs e)
        {
            ScriptHubWindow hub = new ScriptHubWindow();
            hub.Owner = this;
            hub.ShowDialog();
        }

        private void ThemeCycle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.CycleTheme();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.T)
            {
                ThemeManager.CycleTheme();
                e.Handled = true;
            }
        }

        private void StartTelemetryListener()
        {
            telemetryCts?.Cancel();
            try { telemetryListenerTask?.Wait(1000); } catch { }
            telemetryCts = new CancellationTokenSource();
            var token = telemetryCts.Token;
            telemetryListenerTask = Task.Run(() => TelemetryListenerLoop(token), token);
        }

        private async Task TelemetryListenerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var pipeClient = new NamedPipeClientStream(".", "SvxSvc_Telemetry", PipeDirection.In))
                    {
                        pipeClient.Connect(10000);
                        using (var reader = new StreamReader(pipeClient, Encoding.ASCII, false, 128, true))
                        {
                            while (!token.IsCancellationRequested)
                            {
                                string line = null;
                                try { line = reader.ReadLine(); }
                                catch (IOException) { break; }
                                catch (ObjectDisposedException) { break; }

                                if (string.IsNullOrEmpty(line)) break;

                                if (line.StartsWith("HELO|"))
                                {
                                    _ = Dispatcher.BeginInvoke(new Action(() =>
                                        TerminalOutput.AppendText($"\n[SYSTEM] Telemetry stream connected")));
                                    continue;
                                }

                                if (line.StartsWith("TEL|"))
                                {
                                    var parts = line.Split('|');
                                    if (parts.Length >= 8)
                                    {
                                        string timestamp = parts[1];
                                        string pid = parts[2];
                                        string tid = parts[3];
                                        string luaState = parts[4];
                                        string identity = parts[5];
                                        string success = parts[6];
                                        string source = parts[7];
                                        string status = success == "1" ? "OK" : "FAIL";

                                        _ = Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            TerminalOutput.AppendText($"\n[TEL] [{status}] {identity} | {timestamp} | {source}");
                                            telemetryEntries.Insert(0, new TelemetryEntry
                                            {
                                                Time = timestamp,
                                                Status = status,
                                                Identity = identity,
                                                Source = source
                                            });
                                        }));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Pipe not available yet or disconnected, retry after delay
                }

                if (!token.IsCancellationRequested)
                {
                    try { await Task.Delay(2000, token); }
                    catch (TaskCanceledException) { break; }
                }
            }
        }

        private void Inject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dllPath = Path.Combine(Path.GetTempPath(), "ResearchDLL.dll");
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "Controller.Resources.ResearchDLL.dll";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        TerminalOutput.Text += "\n[ERROR] DLL Resource not found. Build the DLL first.";
                        return;
                    }
                    using (FileStream fileStream = new FileStream(dllPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                TerminalOutput.Text += $"\n[SYSTEM] Attempting injection into RobloxPlayerBeta.exe...";
                var result = Injector.Inject("RobloxPlayerBeta", dllPath);
                
                if (result.success)
                {
                    TerminalOutput.Text += $"\n[SUCCESS] {result.message}";
                    StatusDot.Fill = Brushes.LimeGreen; // Turn dot green on success
                    StartTelemetryListener();
                }
                else
                {
                    TerminalOutput.Text += $"\n[ERROR] {result.message}";
                    StatusDot.Fill = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                TerminalOutput.Text += $"\n[ERROR] {ex.Message}";
            }
        }

        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TerminalOutput.Text += "\n[LAUNCH] Searching for Roblox installation...";

                string robloxPath = FindRobloxInstallation();
                if (robloxPath == null)
                {
                    TerminalOutput.Text += "\n[ERROR] RobloxPlayerBeta.exe not found. Roblox may not be installed.";
                    StatusDot.Fill = Brushes.Red;
                    return;
                }

                TerminalOutput.Text += $"\n[LAUNCH] Found at: {robloxPath}";
                TerminalOutput.Text += "\n[LAUNCH] Starting RobloxPlayerBeta.exe...";

                var startInfo = new ProcessStartInfo(robloxPath)
                {
                    WorkingDirectory = Path.GetDirectoryName(robloxPath),
                    UseShellExecute = true
                };
                Process.Start(startInfo);

                Process proc = null;
                for (int i = 1; i <= 30; i++)
                {
                    TerminalOutput.Text += $"\n[LAUNCH] Waiting for process... (attempt {i}/30)";
                    await Task.Delay(1000);

                    var procs = Process.GetProcessesByName("RobloxPlayerBeta");
                    if (procs.Length > 0)
                    {
                        proc = procs[0];
                        break;
                    }
                }

                if (proc == null)
                {
                    TerminalOutput.Text += "\n[ERROR] Process not found after 30 seconds. Timeout.";
                    StatusDot.Fill = Brushes.Red;
                    return;
                }

                TerminalOutput.Text += "\n[LAUNCH] Process found! Injecting...";

                string dllPath = Path.Combine(Path.GetTempPath(), "ResearchDLL.dll");
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "Controller.Resources.ResearchDLL.dll";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        TerminalOutput.Text += "\n[ERROR] DLL Resource not found. Build the DLL first.";
                        StatusDot.Fill = Brushes.Red;
                        return;
                    }
                    using (FileStream fileStream = new FileStream(dllPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                TerminalOutput.Text += "\n[SYSTEM] Attempting injection into RobloxPlayerBeta.exe...";
                var result = Injector.Inject("RobloxPlayerBeta", dllPath);

                if (result.success)
                {
                    TerminalOutput.Text += $"\n[SUCCESS] {result.message}";
                    StatusDot.Fill = Brushes.LimeGreen;
                    StartTelemetryListener();
                }
                else
                {
                    TerminalOutput.Text += $"\n[ERROR] {result.message}";
                    StatusDot.Fill = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                TerminalOutput.Text += $"\n[ERROR] {ex.Message}";
                StatusDot.Fill = Brushes.Red;
            }
        }

        private string FindRobloxInstallation()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string versionsPath = Path.Combine(localAppData, "Roblox", "Versions");

            if (Directory.Exists(versionsPath))
            {
                var dirs = Directory.GetDirectories(versionsPath)
                    .Select(d => new DirectoryInfo(d))
                    .Where(d => File.Exists(Path.Combine(d.FullName, "RobloxPlayerBeta.exe")))
                    .OrderByDescending(d => d.LastWriteTime)
                    .ToList();

                if (dirs.Count > 0)
                    return Path.Combine(dirs[0].FullName, "RobloxPlayerBeta.exe");
            }

            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string progPath = Path.Combine(progFilesX86, "Roblox", "RobloxPlayerBeta.exe");
            if (File.Exists(progPath))
                return progPath;

            try
            {
                using (var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(@"roblox-player\shell\open\command"))
                {
                    if (key != null)
                    {
                        string command = key.GetValue("") as string;
                        if (!string.IsNullOrEmpty(command))
                        {
                            command = command.Trim();
                            if (command.StartsWith("\""))
                            {
                                int endQuote = command.IndexOf("\"", 1);
                                if (endQuote > 0)
                                    return command.Substring(1, endQuote - 1);
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        // ─────────────────────────────────────────────
        //  AI ASSISTANT
        // ─────────────────────────────────────────────
        private void AiApiKey_Changed(object sender, RoutedEventArgs e)
        {
            if (aiService == null) return;
            aiService.SetApiKey(AiApiKeyBox.Password);
            AiStatusText.Text = aiService.HasApiKey
                ? "CONNECTED — deepseek-chat"
                : "READY";
        }

        private void AiSettings_Changed(object sender, TextChangedEventArgs e)
        {
            if (aiService == null) return;
            aiService.SetModel(AiModelBox.Text);
        }

        private void AiReset_Click(object sender, RoutedEventArgs e)
        {
            if (aiService == null) return;
            aiService.ResetConversation();
            AiChatPanel.Children.Clear();
            AiStatusText.Text = "CLEARED";
        }

        private async void AiSend_Click(object sender, RoutedEventArgs e)
        {
            await SendAiMessage();
        }

        private async void AiInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                await SendAiMessage();
            }
        }

        private async Task SendAiMessage()
        {
            if (aiIsStreaming || aiService == null) return;
            string input = AiInputBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;
            AiInputBox.Text = "";

            // Add user message
            AddChatBubble("YOU", input, $"#{ThemeManager.AccentColor.R:X2}{ThemeManager.AccentColor.G:X2}{ThemeManager.AccentColor.B:X2}");
            AiStatusText.Text = "PROCESSING...";

            // Add assistant bubble placeholder
            var respBubble = AddChatBubble("AI", "", "#00CC00");
            var respText = respBubble.FindName("BubbleText") as TextBlock;
            if (respText == null) return;

            aiIsStreaming = true;
            AiSendBtn.IsEnabled = false;

            try
            {
                var fullResponse = new StringBuilder();
                await foreach (string token in aiService.SendMessageStreaming(input))
                {
                    fullResponse.Append(token);
                    respText.Text = fullResponse.ToString();
                    AiChatScroller.ScrollToBottom();
                }
                respText.Text = fullResponse.ToString();
                AiStatusText.Text = "READY";
            }
            catch (Exception ex)
            {
                respText.Text = $"[ERROR] {ex.Message}";
                respText.Foreground = new SolidColorBrush(Colors.Red);
                AiStatusText.Text = "ERROR";
            }
            finally
            {
                aiIsStreaming = false;
                AiSendBtn.IsEnabled = true;
            }
        }

        private Border AddChatBubble(string sender, string message, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);

            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080808")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 5, 8, 5)
            };

            var stack = new StackPanel();

            var header = new TextBlock
            {
                Text = sender == "YOU" ? "> " : "# ",
                Foreground = new SolidColorBrush(color),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 2)
            };
            stack.Children.Add(header);

            var body = new TextBlock
            {
                Name = "BubbleText",
                Text = message,
                Foreground = sender == "YOU"
                    ? new SolidColorBrush(ThemeManager.AccentColor)
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00CC00")),
                FontSize = 11,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 0)
            };
            stack.Children.Add(body);

            border.Child = stack;
            AiChatPanel.Children.Add(border);

            while (AiChatPanel.Children.Count > 40)
                AiChatPanel.Children.RemoveAt(0);

            AiChatScroller.ScrollToBottom();
            return border;
        }
    }
}
