using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Controller
{
    public partial class ScriptHubWindow : Window
    {
        private class ScriptEntry
        {
            public string Name { get; set; }
            public string Author { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public string Url { get; set; }
            public string ColorHex { get; set; }
            public bool IsCustom { get; set; }
        }

        private List<ScriptEntry> _allScripts;
        private List<ScriptEntry> _filteredScripts;
        private ScriptEntry _selectedScript;
        private Border _selectedCardBorder;
        private HashSet<string> _favorites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<string> _autoExecute = new List<string>();
        private string _scriptsDir;
        private bool _suppressInfoEvents;

        public static List<string> AutoExecuteScripts { get; private set; }

        public ScriptHubWindow()
        {
            InitializeComponent();
            _scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
            if (!Directory.Exists(_scriptsDir)) Directory.CreateDirectory(_scriptsDir);
            LoadFavorites();
            LoadAutoExec();
            AutoExecuteScripts = _autoExecute;
            LoadScripts();
            RenderCards(_allScripts);
        }

        // ── Persistence ──
        private void LoadFavorites()
        {
            string path = Path.Combine(_scriptsDir, "favorites.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                        _favorites = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                }
                catch { }
            }
        }

        private void SaveFavorites()
        {
            string path = Path.Combine(_scriptsDir, "favorites.json");
            try
            {
                string json = JsonSerializer.Serialize(_favorites.ToList());
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private void LoadAutoExec()
        {
            string path = Path.Combine(_scriptsDir, "autoexec.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                        _autoExecute = list;
                }
                catch { }
            }
        }

        private void SaveAutoExec()
        {
            string path = Path.Combine(_scriptsDir, "autoexec.json");
            try
            {
                string json = JsonSerializer.Serialize(_autoExecute);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        // ── Script database ──
        private void LoadScripts()
        {
            _allScripts = new List<ScriptEntry>
            {
                new ScriptEntry { Name = "Infinite Yield", Author = "EdgeIY", Description = "The definitive admin command suite. 600+ commands for server control, player management, and system utilities.", Category = "Admin", Url = "https://raw.githubusercontent.com/EdgeIY/infiniteyield/master/source", ColorHex = "#FF8C00" },
                new ScriptEntry { Name = "CMD-X", Author = "CMD-X Team", Description = "Lightweight admin commands with a clean interface. Supports custom commands, aliases, and server-side execution.", Category = "Admin", Url = "https://raw.githubusercontent.com/CMD-X/CMD-X/master/Source", ColorHex = "#FF6D00" },
                new ScriptEntry { Name = "Fates Admin", Author = "fatesc", Description = "Full-featured admin panel with player teleport, kick/ban, server info, and a polished GUI. One of the most popular admin scripts.", Category = "Admin", Url = "https://raw.githubusercontent.com/fatesc/fates-admin/main/main.lua", ColorHex = "#D32F2F" },
                new ScriptEntry { Name = "Dark Dex V4", Author = "infyiff", Description = "Advanced instance explorer. Browse the full game tree, view properties, copy instance paths, and inspect hidden objects.", Category = "Utility", Url = "https://raw.githubusercontent.com/infyiff/backup/main/dex.lua", ColorHex = "#7C4DFF" },
                new ScriptEntry { Name = "Orca", Author = "richie0866", Description = "Modern script executor utility with enhanced compatibility, auto-completion, and improved error handling for Luau.", Category = "Utility", Url = "https://raw.githubusercontent.com/richie0866/orca/main/main.lua", ColorHex = "#00BCD4" },
                new ScriptEntry { Name = "Simple Spoof", Author = "GravityExploits", Description = "Spoof player name, user ID, and other identity fields. Lightweight and undetectable client-side spoofing utility.", Category = "Utility", Url = "https://raw.githubusercontent.com/GravityExploits/Simple-Spoofing/main/SimpleSpoof", ColorHex = "#76FF03" },
                new ScriptEntry { Name = "Akron", Author = "RealMasterOogway", Description = "Combat utility with aimbot, silent aim, and hitbox extender. Smooth targeting with customizable FOV and prediction.", Category = "Combat", Url = "https://raw.githubusercontent.com/RealMasterOogway/Akron/main/source.lua", ColorHex = "#FF1744" },
                new ScriptEntry { Name = "Aimbot + ESP", Author = "TrashScripts", Description = "Combined aimbot and ESP package. Features aim assist, snaplines, distance tracking, and health bars for all players.", Category = "Combat", Url = "https://raw.githubusercontent.com/TrashScripts/Roblox-Scripts/main/aimbot_esp.lua", ColorHex = "#F50057" },
                new ScriptEntry { Name = "Unnamed ESP", Author = "UnnamedESP", Description = "High-performance ESP with box traces, skeleton rendering, name tags, distance, and health indicators. Minimal FPS impact.", Category = "Visual", Url = "https://raw.githubusercontent.com/UnnamedESP/Unnamed-ESP/main/main.lua", ColorHex = "#00E5FF" },
                new ScriptEntry { Name = "Frost Hook", Author = "TheRealBalu", Description = "Visual enhancement suite with chams, tracers, and custom crosshair. Includes a clean settings panel for customization.", Category = "Visual", Url = "https://raw.githubusercontent.com/TheRealBalu/FrostHook/main/Main.lua", ColorHex = "#64FFDA" },
                new ScriptEntry { Name = "Coco Hub", Author = "MasterMast3r", Description = "Multi-game hub with scripts for Arsenal, Da Hood, Bedwars, and more. Regularly updated with new game support.", Category = "Hub", Url = "https://raw.githubusercontent.com/MasterMast3r/CoCo-Hub/main/CoCo%20Hub.lua", ColorHex = "#FFAB00" },
                new ScriptEntry { Name = "domainX", Author = "damplox", Description = "Versatile hub supporting 20+ games. Features aimbot, ESP, speed hacks, and game-specific exploits in a single package.", Category = "Hub", Url = "https://raw.githubusercontent.com/damplox/domainX/main/source", ColorHex = "#AA00FF" },
                new ScriptEntry { Name = "Cadacity Hub", Author = "realmasteroogway", Description = "All-in-one hub with combat, visual, and utility modules. Clean UI with toggleable features and keybind support.", Category = "Hub", Url = "https://raw.githubusercontent.com/realmasteroogway/Cadacity/main/main.lua", ColorHex = "#69F0AE" },
                new ScriptEntry { Name = "Luna Hub", Author = "lunarbootstrapper", Description = "Script hub focused on reliability and performance. Includes auto-update, script verification, and a curated script library.", Category = "Hub", Url = "https://raw.githubusercontent.com/lunarbootstrapper/Luna/main/script.lua", ColorHex = "#448AFF" },
                new ScriptEntry { Name = "Cadency", Author = "damplox", Description = "Lightweight hub with essential scripts for popular games. Fast loading, minimal UI, and reliable execution.", Category = "Hub", Url = "https://raw.githubusercontent.com/damplox/cadency/main/main.lua", ColorHex = "#FF6E40" },
                new ScriptEntry { Name = "Mid-Journey Hub", Author = "MidCommunity", Description = "Community-driven hub with scripts for 30+ games. Features auto-detection, script ratings, and a built-in update checker.", Category = "Hub", Url = "https://raw.githubusercontent.com/MidCommunity/Mid-Journey/main/loader.lua", ColorHex = "#B388FF" }
            };
            _filteredScripts = new List<ScriptEntry>(_allScripts);
        }

        // ── Render ──
        private void RenderCards(List<ScriptEntry> scripts)
        {
            ScriptContainer.Children.Clear();
            _selectedScript = null;
            _selectedCardBorder = null;
            ExecuteSelectedButton.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Collapsed;

            foreach (var script in scripts)
            {
                var card = CreateScriptCard(script);
                ScriptContainer.Children.Add(card);
            }

            ResultCount.Text = $"{scripts.Count} script{(scripts.Count != 1 ? "s" : "")} loaded";
        }

        private UIElement CreateScriptCard(ScriptEntry script)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ScriptCardStyle"),
                Width = 200,
                Tag = script
            };

            bool isAutoExec = _autoExecute.Contains(script.Name);
            Color color = (Color)ColorConverter.ConvertFromString(script.ColorHex);
            Color accentColor = ThemeManager.AccentColor;

            if (isAutoExec)
            {
                border.BorderThickness = new Thickness(2, 1, 1, 1);
                border.BorderBrush = new SolidColorBrush(accentColor);
            }

            border.MouseEnter += (s, e) =>
            {
                if (_selectedCardBorder == border) return;
                border.BorderBrush = new SolidColorBrush(accentColor);
                border.Background = new SolidColorBrush(ThemeManager.BgLighterColor);
            };
            border.MouseLeave += (s, e) =>
            {
                if (_selectedCardBorder == border) return;
                if (isAutoExec)
                    border.BorderBrush = new SolidColorBrush(accentColor);
                else
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                border.Background = new SolidColorBrush(ThemeManager.BgLightColor);
            };

            var containerGrid = new Grid();

            // Main content stack
            var stack = new StackPanel();

            var accentBar = new Border
            {
                Height = 3,
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(4, 4, 0, 0)
            };
            stack.Children.Add(accentBar);

            var iconBorder = new Border
            {
                Width = 40, Height = 40,
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(10, 10, 10, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            iconBorder.Child = new TextBlock
            {
                Text = script.Name.Substring(0, 1).ToUpper(),
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(iconBorder);

            stack.Children.Add(new TextBlock
            {
                Text = script.Name,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(10, 0, 10, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"by {script.Author}",
                Foreground = new SolidColorBrush(ThemeManager.AccentColor),
                FontSize = 10,
                Margin = new Thickness(10, 0, 10, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var desc = new TextBlock
            {
                Text = script.Description,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777")),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10, 0, 10, 8),
                MaxHeight = 42,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            stack.Children.Add(desc);

            var badgeBorder = new Border
            {
                Background = new SolidColorBrush(ThemeManager.BgMediumColor),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(10, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            badgeBorder.Child = new TextBlock
            {
                Text = script.Category.ToUpper(),
                Foreground = new SolidColorBrush(color),
                FontSize = 8,
                FontWeight = FontWeights.Bold
            };
            stack.Children.Add(badgeBorder);

            var execBtn = new Button
            {
                Content = "▶  EXECUTE",
                Height = 28,
                Margin = new Thickness(10, 4, 10, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = script
            };
            execBtn.Click += (s, e) =>
            {
                e.Handled = true;
                ExecuteScript(script);
            };
            stack.Children.Add(execBtn);

            containerGrid.Children.Add(stack);

            // Star toggle button (top-right corner)
            bool isFav = _favorites.Contains(script.Name);
            var starBtn = new Button
            {
                Content = isFav ? "\u2605" : "\u2606",
                Width = 24, Height = 24,
                FontSize = 14,
                Background = Brushes.Transparent,
                Foreground = isFav ? new SolidColorBrush(accentColor)
                                   : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 6, 6, 0),
                Cursor = Cursors.Hand,
                Tag = script
            };
            starBtn.Click += (s, e) =>
            {
                e.Handled = true;
                ToggleFavorite(script, starBtn);
            };
            containerGrid.Children.Add(starBtn);

            // Auto-exec badge
            if (isAutoExec)
            {
                var autoBadge = new Border
                {
                    Background = new SolidColorBrush(accentColor),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(4, 1, 4, 1),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                autoBadge.Child = new TextBlock
                {
                    Text = "AUTO",
                    Foreground = Brushes.White,
                    FontSize = 7,
                    FontWeight = FontWeights.Bold
                };
                containerGrid.Children.Add(autoBadge);
            }

            // Card click -> select (bubbles up only when execute/star buttons aren't clicked)
            border.MouseLeftButtonDown += (s, e) =>
            {
                SelectCard(script, border);
            };

            border.Child = containerGrid;
            return border;
        }

        private void ToggleFavorite(ScriptEntry script, Button starBtn)
        {
            if (_favorites.Contains(script.Name))
            {
                _favorites.Remove(script.Name);
                starBtn.Content = "\u2606";
                starBtn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
            }
            else
            {
                _favorites.Add(script.Name);
                starBtn.Content = "\u2605";
                starBtn.Foreground = new SolidColorBrush(ThemeManager.AccentColor);
            }
            SaveFavorites();

            // If Favorites category is currently active, re-filter
            bool favActive = false;
            foreach (var child in CategoryPanel.Children)
            {
                if (child is Button btn && btn.Tag is string tag && tag == "Selected"
                    && btn != CatAll)
                {
                    string c = btn.Content.ToString();
                    string[] pf = { "★  " };
                    foreach (var p in pf)
                    {
                        if (c.StartsWith(p)) { c = c.Substring(p.Length); break; }
                    }
                    if (c.Trim() == "Favorites") favActive = true;
                    break;
                }
            }
            if (favActive)
            {
                _filteredScripts = _allScripts.Where(s => _favorites.Contains(s.Name)).ToList();
                RenderCards(_filteredScripts);
            }

            // Update info panel if showing this script
            if (_selectedScript == script)
            {
                _suppressInfoEvents = true;
                InfoFavoriteToggle.IsChecked = _favorites.Contains(script.Name);
                _suppressInfoEvents = false;
            }
        }

        private void SelectCard(ScriptEntry script, Border cardBorder)
        {
            // Deselect previous
            if (_selectedCardBorder != null && _selectedCardBorder != cardBorder)
            {
                var prevScript = _selectedCardBorder.Tag as ScriptEntry;
                bool prevAuto = prevScript != null && _autoExecute.Contains(prevScript.Name);
                _selectedCardBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525"));
                _selectedCardBorder.BorderBrush = prevAuto
                    ? new SolidColorBrush(ThemeManager.AccentColor)
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            }

            // Select new
            _selectedScript = script;
            _selectedCardBorder = cardBorder;
            cardBorder.Background = new SolidColorBrush(ThemeManager.BgLighterColor);
            cardBorder.BorderBrush = new SolidColorBrush(ThemeManager.AccentColor);

            ShowInfoPanel(script);
            ExecuteSelectedButton.Visibility = Visibility.Visible;
        }

        private void ShowInfoPanel(ScriptEntry script)
        {
            InfoPanel.Visibility = Visibility.Visible;

            Color color = (Color)ColorConverter.ConvertFromString(script.ColorHex);
            InfoAccentBar.Background = new SolidColorBrush(color);
            InfoName.Text = script.Name;
            InfoName.Foreground = new SolidColorBrush(color);
            InfoAuthor.Text = $"by {script.Author}";
            InfoDescription.Text = script.Description;
            InfoCategoryText.Text = script.Category.ToUpper();
            InfoCategoryText.Foreground = new SolidColorBrush(color);
            InfoUrl.Text = script.Url;

            _suppressInfoEvents = true;
            InfoFavoriteToggle.IsChecked = _favorites.Contains(script.Name);
            InfoAutoExecToggle.IsChecked = _autoExecute.Contains(script.Name);
            _suppressInfoEvents = false;

            InfoExecuteButton.Tag = script;
        }

        // ── Info panel event handlers ──
        private void InfoFavorite_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressInfoEvents || _selectedScript == null) return;
            if (InfoFavoriteToggle.IsChecked == true)
                _favorites.Add(_selectedScript.Name);
            else
                _favorites.Remove(_selectedScript.Name);
            SaveFavorites();
            ReRenderCurrentCards();
        }

        private void InfoAutoExec_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressInfoEvents || _selectedScript == null) return;
            if (InfoAutoExecToggle.IsChecked == true)
            {
                if (!_autoExecute.Contains(_selectedScript.Name))
                    _autoExecute.Add(_selectedScript.Name);
            }
            else
            {
                _autoExecute.Remove(_selectedScript.Name);
            }
            SaveAutoExec();
            ReRenderCurrentCards();
        }

        private void InfoExecute_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScript != null)
                ExecuteScript(_selectedScript);
        }

        private void ReRenderCurrentCards()
        {
            var currentList = new List<ScriptEntry>(_filteredScripts);
            ScriptContainer.Children.Clear();
            _selectedCardBorder = null;

            string selectedName = _selectedScript?.Name;

            foreach (var script in currentList)
            {
                var card = CreateScriptCard(script);
                ScriptContainer.Children.Add(card);
                if (selectedName != null && script.Name == selectedName)
                {
                    if (card is Border b)
                    {
                        _selectedCardBorder = b;
                        b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                        b.BorderBrush = new SolidColorBrush(ThemeManager.AccentColor);
                    }
                }
            }
        }

        // ── Execute ──
        private void ExecuteScript(ScriptEntry script)
        {
            string payload = $"loadstring(game:HttpGet('{script.Url}'))()";
            bool ok = SendScript(payload, script.Name);
            FooterStatus.Text = ok
                ? $"Executed: {script.Name} — OK"
                : $"Executed: {script.Name} — FAILED (check pipe connection)";
        }

        private bool SendScript(string script, string scriptName)
        {
            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", "SvxSvc_Internal", PipeDirection.InOut))
                {
                    pipeClient.Connect(3000);
                    pipeClient.ReadTimeout = 2000;

                    using (var reader = new StreamReader(pipeClient, Encoding.ASCII, false, 128, true))
                    {
                        string heloLine = null;
                        try { heloLine = reader.ReadLine(); } catch (IOException) { }

                        byte[] buffer = Encoding.ASCII.GetBytes(script);
                        pipeClient.Write(buffer, 0, buffer.Length);

                        string telLine = null;
                        try { telLine = reader.ReadLine(); } catch (IOException) { }

                        if (!string.IsNullOrEmpty(telLine) && telLine.StartsWith("TEL|"))
                        {
                            var parts = telLine.Split('|');
                            if (parts.Length >= 7)
                                return parts[6] == "1";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to send script.\n\nEnsure the DLL is injected first!\n\nDetails: {ex.Message}",
                    "Pipe Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            return false;
        }

        // ── Category filter ──
        private void Category_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in CategoryPanel.Children)
            {
                if (child is Button btn)
                    btn.Tag = null;
            }

            var clicked = sender as Button;
            clicked.Tag = "Selected";

            string category = clicked.Content.ToString();
            string[] emojiPrefixes = { "★  ", "⚔  ", "🔧  ", "🎯  ", "👻  ", "🏰  " };
            foreach (var prefix in emojiPrefixes)
            {
                if (category.StartsWith(prefix))
                {
                    category = category.Substring(prefix.Length);
                    break;
                }
            }
            category = category.Trim();

            if (category == "All Scripts" || category == "All")
            {
                _filteredScripts = new List<ScriptEntry>(_allScripts);
            }
            else if (category == "Favorites")
            {
                _filteredScripts = _allScripts.Where(s => _favorites.Contains(s.Name)).ToList();
            }
            else
            {
                _filteredScripts = _allScripts.Where(s =>
                    s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            string search = SearchBox.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(search) && search != "search scripts...")
            {
                _filteredScripts = _filteredScripts.Where(s =>
                    s.Name.ToLower().Contains(search) ||
                    s.Author.ToLower().Contains(search) ||
                    s.Description.ToLower().Contains(search)).ToList();
            }

            RenderCards(_filteredScripts);
        }

        // ── Search filter ──
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(query))
            {
                RenderCards(_allScripts);
                return;
            }

            var results = _allScripts.Where(s =>
                s.Name.ToLower().Contains(query) ||
                s.Author.ToLower().Contains(query) ||
                s.Description.ToLower().Contains(query) ||
                s.Category.ToLower().Contains(query)).ToList();

            RenderCards(results);
        }

        // ── Custom URL ──
        private void LoadCustomUrl_Click(object sender, RoutedEventArgs e)
        {
            string url = CustomUrlBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                FooterStatus.Text = "Please enter a script URL";
                return;
            }

            var customScript = new ScriptEntry
            {
                Name = "Custom Script",
                Author = "User",
                Description = $"Custom script loaded from URL. Length: {url.Length} chars.",
                Category = "Custom",
                Url = url,
                ColorHex = "#00E5FF",
                IsCustom = true
            };

            _allScripts.Add(customScript);
            _filteredScripts.Add(customScript);

            var card = CreateScriptCard(customScript);
            ScriptContainer.Children.Add(card);
            ResultCount.Text = $"{_filteredScripts.Count} script{(_filteredScripts.Count != 1 ? "s" : "")} loaded";
            FooterStatus.Text = "Custom script loaded — click EXECUTE to run";

            ExecuteScript(customScript);
        }

        // ── Auto-Execute ──
        private void RunAutoExecute_Click(object sender, RoutedEventArgs e)
        {
            if (_autoExecute.Count == 0)
            {
                FooterStatus.Text = "No scripts in auto-execute list";
                return;
            }

            var toExecute = _allScripts.Where(s => _autoExecute.Contains(s.Name)).ToList();
            if (toExecute.Count == 0)
            {
                FooterStatus.Text = "Auto-exec scripts not found in library";
                return;
            }

            foreach (var script in toExecute)
            {
                ExecuteScript(script);
            }
            FooterStatus.Text = $"Auto-executed {toExecute.Count} script{(toExecute.Count != 1 ? "s" : "")}";
        }

        // ── Window event handlers ──
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadScripts();
            RenderCards(_allScripts);
            FooterStatus.Text = "Script list refreshed";
        }

        private void ExecuteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScript != null)
                ExecuteScript(_selectedScript);
        }
    }
}
