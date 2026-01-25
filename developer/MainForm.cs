using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ToolProfile
{
    public class MainForm : Form
    {
        TableLayoutPanel layout;
        WebView2 liveView;
        TextBox txtUsername;
        Button btnScan;
        DataGridView grid;
        Label lblProfileName;
        bool metaResolved = false;
        System.Windows.Forms.Timer resolveTimer;
        ScanManifest manifest;
        List<string> knownInputs = new();
        SuggestionPopup popup;
        
 
        Icon LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(name);
            return new Icon(stream);
        }


    [DllImport("user32.dll")]
    static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    public MainForm()
        {
            Text = "ToolProfile Scan";
            Width = 1500;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            Icon = LoadIcon("ToolProfile.Assets.MainApps.ico");

            BuildLayout();
            InitInteliscen(); 

        }
        //=INTELISCEN==
        void InitInteliscen()


        {
            manifest = LoadManifest();
            SyncManifest();
            BuildMemoryIndex();
            LoadSettings();     

            popup = new SuggestionPopup();
            popup.OnSelect += value =>
            {
                txtUsername.Text = value;
                popup.Hide();
                _ = ScanAsync();
            };

            txtUsername.KeyDown += (s, e) =>
            {
                if (!popup.Visible || popup.List.Items.Count == 0)
                    return;

                if (e.KeyCode == Keys.Down)
                {
                    int i = popup.List.SelectedIndex;
                    popup.List.SelectedIndex = Math.Min(i + 1, popup.List.Items.Count - 1);
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Up)
                {
                    int i = popup.List.SelectedIndex;
                    popup.List.SelectedIndex = Math.Max(i - 1, 0);
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    string value =
                        popup.List.SelectedItem?.ToString()
                        ?? popup.List.Items[0].ToString();

                    txtUsername.Text = value;
                    popup.Hide();
                    _ = ScanAsync();
                }
            };


            txtUsername.TextChanged += (_, __) =>
            {

                SyncManifest();        
                BuildMemoryIndex();    


                string input = txtUsername.Text.Trim();
                if (input.Length < 3)
                {
                    popup.Hide();
                    return;
                }

                var matches = knownInputs
                    .Where(x => IsSimilar(input, x))
                    .Take(5)
                    .ToList();

                if (!matches.Any())
                {
                    popup.Hide();
                    return;
                }

                popup.List.Items.Clear();
                popup.List.Items.AddRange(matches.ToArray());

                Point p = txtUsername.PointToScreen(new Point(0, txtUsername.Height));
                popup.Location = p;
                popup.Size = new Size(txtUsername.Width, 140);

                if (!popup.Visible)
                {
                    popup.Show(this);
                }

                popup.List.SelectedIndex = 0;
            };
        }
        string ScanDir => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "usersdata",
            "scanhistory"
        );
        public AppSettings Settings { get; private set; }

        string LocalLowBase => Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "..",
            "LocalLow",
            "h2odragonhoang developers",
            "ToolProfile"
        ));



        string SettingsDir => Path.Combine(LocalLowBase, "settings");
        public string SettingsPath => Path.Combine(SettingsDir, "settings.json");

        string ManifestPath => Path.Combine(ScanDir, "manifest.json");

        ScanManifest LoadManifest()
        {
            Directory.CreateDirectory(ScanDir);
            if (!File.Exists(ManifestPath))
                return new ScanManifest();

            return JsonSerializer.Deserialize<ScanManifest>(
                File.ReadAllText(ManifestPath)
            );
        }

        void LoadSettings()
        {
            Directory.CreateDirectory(SettingsDir);

            if (!File.Exists(SettingsPath))
            {
                Settings = new AppSettings();
                SaveSettings();
            }
            else
            {
                Settings = JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(SettingsPath)
                ) ?? new AppSettings();
            }

            ApplySettings();
        }

        void SaveManifest()
        {
            manifest.totalScans = manifest.entries.Count;
            File.WriteAllText(
                ManifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        void SyncManifest()
        {
            var files = Directory.GetFiles(ScanDir, "manifest_*.json")
                                 .Select(Path.GetFileName)
                                 .ToHashSet();

            manifest.entries.RemoveAll(e => !files.Contains(e.file));
            SaveManifest();
        }

        void BuildMemoryIndex()
        {
            knownInputs = manifest.entries
                .Select(e => e.input)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        //==END==
        //==SCAN RESULT==
        void SaveScanResult(string input, string type)
        {
            var exist = manifest.entries
                .FirstOrDefault(e => e.input.Equals(input, StringComparison.OrdinalIgnoreCase));

            if (exist != null)
            {
                exist.lastSeen = DateTime.Now;
                exist.scanCount++;
                SaveManifest();
                return;
            }

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string file = $"manifest_{ts}.json";

            File.WriteAllText(
                Path.Combine(ScanDir, file),
                JsonSerializer.Serialize(new
                {
                    timestamp = ts,
                    input,
                    type
                }, new JsonSerializerOptions { WriteIndented = true })
            );

            manifest.entries.Add(new ManifestEntry
            {
                file = file,
                input = input,
                type = type,
                created = DateTime.Now,
                lastSeen = DateTime.Now,
                scanCount = 1
            });

            SaveManifest();
            BuildMemoryIndex();
        }
        //==END==
        void BuildLayout()
        {
            //==TOOLTIP==
            ToolTip winTip = new ToolTip
            {
                InitialDelay = 300,       
                ReshowDelay = 100,
                AutoPopDelay = 2000,
                ShowAlways = true
            };
            //==END==


            // ROOT CONTAINER 
            var root = new Panel
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(root);

            // TOP BAR
            var topBar = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            //CUSTOM TITLE==


            topBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            Button CreateWinBtn(string symbol)
            {
                return new Button
                {
                    Text = symbol,
                    Dock = DockStyle.Right,
                    Width = 46,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Symbol", 10f, FontStyle.Regular),
                    ForeColor = Color.Black,
                    BackColor = Color.Transparent,
                    TabStop = false
                };
            }

            var btnClose = CreateWinBtn("✕");
            var btnMax = CreateWinBtn("❐");
            var btnMin = CreateWinBtn("─");

            btnClose.Click += (_, __) => Close();
            btnMin.Click += (_, __) => WindowState = FormWindowState.Minimized;
            btnMax.Click += (_, __) =>
            {
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            };

            void StyleWinBtn(Button b, Color hover, Color normal)
            {
                b.FlatAppearance.BorderSize = 0;
                b.MouseEnter += (_, __) => b.BackColor = hover;
                b.MouseLeave += (_, __) => b.BackColor = normal;
            }

            StyleWinBtn(btnClose, Color.FromArgb(232, 17, 35), Color.Transparent);
            StyleWinBtn(btnMax, Color.FromArgb(220, 220, 220), Color.Transparent);
            StyleWinBtn(btnMin, Color.FromArgb(220, 220, 220), Color.Transparent);


            topBar.Controls.Add(btnClose);
            topBar.Controls.Add(btnMax);
            topBar.Controls.Add(btnMin);

            var lblTitle = new Label
            {
                Text = Text,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 300,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.Black
            };


            lblTitle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            topBar.Controls.Add(lblTitle);
            topBar.Controls.SetChildIndex(lblTitle, 0);
            //==END==


            var btnSettings = new Button
            {
                Text = "⚙",
                Dock = DockStyle.Right,
                Width = 40,
                FlatStyle = FlatStyle.Flat
            };
            btnSettings.FlatAppearance.BorderSize = 0;

            btnSettings.Click += (_, __) =>
            {
                using var f = new SettingsForm(this);
                f.ShowDialog(this);
            };

            topBar.Controls.Add(btnSettings);

            var btnMessenger = new Button
            {
                Text = "💬",
                Dock = DockStyle.Right,
                Width = 40,
                FlatStyle = FlatStyle.Flat
            };
            btnMessenger.FlatAppearance.BorderSize = 0;
            btnMessenger.Click += (_, __) =>
            {
                var messengerForm = new MessengerForm();
                messengerForm.Show();
            };

            topBar.Controls.Add(btnMessenger); 


            // MAIN LAYOUT
            layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            root.Controls.Add(layout);
            root.Controls.Add(topBar);

            // LEFT — LIVE BROWSER
            liveView = new WebView2 { Dock = DockStyle.Fill };
            var liveGroup = new GroupBox
            {
                Text = "LIVE BROWSER",
                Dock = DockStyle.Fill
            };
            liveGroup.Controls.Add(liveView);
            layout.Controls.Add(liveGroup, 0, 0);

            // CENTER
            var midPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.White
            };
            layout.Controls.Add(midPanel, 1, 0);

            var lblCopyright = new Label
            {
                Text = "@copyright by hycoredragon - 2026",
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            midPanel.Controls.Add(lblCopyright);

            txtUsername = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "facebook username / id / url",
                Font = new Font("Segoe UI", 12f)
            };
            midPanel.Controls.Add(txtUsername);

            btnScan = new Button
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "SCAN",
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false   
            };

            btnScan.FlatAppearance.BorderSize = 1;
            btnScan.Click += async (_, __) => await ScanAsync();
            midPanel.Controls.Add(btnScan);

            // RIGHT — METADATA
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.Columns.Add("Field", "Field");
            grid.Columns.Add("Value", "Value");
            grid.Columns.Add("Status", "Status");
            grid.RowHeadersVisible = false;


            var metaGroup = new GroupBox
            {
                Text = "PROFILE METADATA",
                Dock = DockStyle.Fill
            };
            metaGroup.Controls.Add(grid);
            layout.Controls.Add(metaGroup, 2, 0);

            //==TOOLTIP ADD==
            winTip.SetToolTip(btnClose, "Close");
            winTip.SetToolTip(btnMax, "Maximize");
            winTip.SetToolTip(btnMin, "Minimize");
            winTip.SetToolTip(btnSettings, "Setting");
            winTip.SetToolTip(btnMessenger, "Messenger");

            //==END==
        }


        //==ApplyThemeColors==
        void ApplyThemeColors()
        {
            bool isDark =
                BackColor.R < 60 &&
                BackColor.G < 60 &&
                BackColor.B < 60;

            btnScan.ForeColor = Color.White;

            btnScan.BackColor = isDark
                ? Color.FromArgb(45, 45, 45)
                : SystemColors.Control;

            btnScan.FlatAppearance.BorderColor = isDark
                ? Color.Gainsboro
                : Color.Gray;

            Color textColor = isDark ? Color.WhiteSmoke : Color.Black;
            Color borderColor = isDark ? Color.Gainsboro : Color.Gray;

            // 🔹 GroupBox
            foreach (Control c in layout.Controls)
            {
                if (c is GroupBox gb)
                {
                    gb.ForeColor = textColor;
                }
            }

            // 🔹 DataGridView
            grid.BackgroundColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            grid.ForeColor = textColor;

            grid.ColumnHeadersDefaultCellStyle.BackColor =
                isDark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;

            grid.ColumnHeadersDefaultCellStyle.ForeColor = textColor;
            grid.EnableHeadersVisualStyles = false;

            grid.DefaultCellStyle.BackColor =
                isDark ? Color.FromArgb(35, 35, 35) : Color.White;

            grid.DefaultCellStyle.ForeColor = textColor;

            grid.DefaultCellStyle.SelectionBackColor =
                isDark ? Color.FromArgb(70, 70, 70) : SystemColors.Highlight;

            grid.DefaultCellStyle.SelectionForeColor =
                isDark ? Color.White : Color.White;

            // 🔹 Button
            btnScan.ForeColor = textColor;

            // 🔹 TextBox
            txtUsername.ForeColor = textColor;
            txtUsername.BackColor = isDark ? Color.FromArgb(25, 25, 25) : Color.White;

            // 🔹 LIVE BROWSER TITLE
            var liveGroup = layout.GetControlFromPosition(0, 0) as GroupBox;
            if (liveGroup != null)
                liveGroup.ForeColor = textColor;
        }
        void ApplyGridDarkTheme()
        {
            grid.EnableHeadersVisualStyles = false;

            grid.BackgroundColor = Color.FromArgb(25, 25, 25);
            grid.GridColor = Color.FromArgb(60, 60, 60);

            grid.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9f, FontStyle.Bold);

            grid.BorderStyle = BorderStyle.None;
        }

        //==END==
        //==COLOR INTERFACE==
        public void ApplySettings()
        {
            // 🎨 Interface color
            try
            {
                var c = ColorTranslator.FromHtml(Settings.InterfaceColor);
                BackColor = c;
            }
            catch
            {
                BackColor = Color.White;
            }

            // 👁️ Live Browser toggle
            if (Settings.DisableLiveBrowser)
            {
                layout.ColumnStyles[0].Width = 0;
                layout.ColumnStyles[1].Width = 45;
                layout.ColumnStyles[2].Width = 55;
                layout.GetControlFromPosition(0, 0).Visible = false;
            }
            else
            {
                layout.ColumnStyles[0].Width = 25;
                layout.ColumnStyles[1].Width = 35;
                layout.ColumnStyles[2].Width = 40;
                layout.GetControlFromPosition(0, 0).Visible = true;
            }
            ApplyThemeColors();
        }

        public void SaveSettings()
        {
            File.WriteAllText(
                SettingsPath,
                JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        //==END==

        //==LOGIC SCANNER & INTEL==
        bool IsFacebookUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out var uri)
                   && (uri.Host.Contains("facebook.com") || uri.Host.Contains("fb.com"));
        }
        string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";

            return s.ToLower()
                .Replace("https://", "")
                .Replace("http://", "")
                .Replace("www.", "")
                .Trim();
        }

        bool IsSimilar(string input, string known)
        {
            input = Normalize(input);
            known = Normalize(known);

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(known))
                return false;

            return known.Contains(input) || input.Contains(known);
        }

        //==END==
        string GetProfileNameFromTitle()
        {
            var title = liveView.CoreWebView2.DocumentTitle;
            if (!string.IsNullOrEmpty(title) && title.Contains("|"))
                return title.Split('|')[0].Trim();
            return null;
        }

        async Task<string> GetProfileNameFromDomAsync()
        {
            try
            {
                var result = await liveView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        const h1 = document.querySelector('h1');
                        return h1 ? h1.innerText : '';
                    })();
                ");
                return result?.Trim('"');
            }
            catch
            {
                return null;
            }
        }

        async Task EnsureWebViewAsync()
        {
            if (liveView.CoreWebView2 != null) return;

            await liveView.EnsureCoreWebView2Async();
            liveView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            liveView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            liveView.CoreWebView2.Settings.IsZoomControlEnabled = true;


            liveView.CoreWebView2.WebMessageReceived += async (s, e) =>
            {
                if (e.TryGetWebMessageAsString() != "DOM_READY") return;

                UpdateMeta("Navigation Status", "Completed");
                UpdateMeta("Final URL", liveView.Source?.ToString());
                UpdateMeta("Page Title", liveView.CoreWebView2.DocumentTitle);

                bool loggedIn = !liveView.Source.ToString().Contains("login");
                UpdateMeta("Session Type", loggedIn ? "Logged-in" : "Guest");
                UpdateMeta("Visibility", loggedIn ? "Public" : "Login Required");

                UpdateMeta("Images Loaded", "Yes");
                UpdateMeta("Scripts Executed", "Yes");
                UpdateMeta("Profile Type", "User");

                string name = await GetProfileNameFromDomAsync();
                if (string.IsNullOrWhiteSpace(name))
                    name = GetProfileNameFromTitle();

            };
        }

        async Task ScanAsync()
        {
            var user = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(user)) return;

            btnScan.Enabled = false;
            grid.Rows.Clear();
            metaResolved = false;
            await EnsureWebViewAsync();
            string profileUrl;

            if (IsFacebookUrl(user))
            {
                profileUrl = user;
                AddMeta("Input Type", "Direct URL", "Public");
            }
            else
            {
                profileUrl = $"https://www.facebook.com/{user}";
                AddMeta("Input Type", "Username / ID", "Public");
            }

            liveView.CoreWebView2.Navigate(profileUrl);

            AddMeta("Platform", "Facebook", "Public");
            AddMeta("Username / ID", user, "Public");
            AddMeta("Profile URL", profileUrl, "Public");
            AddMeta("Viewer Mode", "WebView2 (Browser)", "Public");

            AddMeta("Page Title", "Resolving...", "Runtime");
            AddMeta("Session Type", "Resolving...", "Context");
            AddMeta("Navigation Status", "Loading...", "Runtime");
            AddMeta("Final URL", "Resolving...", "Runtime");
            AddMeta("Profile Type", "Resolving...", "Detected");
            AddMeta("Visibility", "Resolving...", "Detected");
            AddMeta("Live Render", "Yes", "Runtime");
            AddMeta("Images Loaded", "Resolving...", "Runtime");
            AddMeta("Scripts Executed", "Resolving...", "Runtime");
            AddMeta("Viewer Context", "Embedded WebView", "Context");
            AddMeta("User Agent", "Edge WebView2", "Context");
            AddMeta("Device Mode", "Desktop", "Context"); // hay Mobile
            AddMeta("Language", "vi-VN", "Context");
            AddMeta("Browser Engine", "Chromium (WebView2)", "Context");
            AddMeta("OS Platform", Environment.OSVersion.ToString(), "Context");
            AddMeta("Architecture", Environment.Is64BitProcess ? "x64" : "x86", "Context");
            AddMeta("Runtime", ".NET " + Environment.Version, "Context");
            AddMeta("Time Zone", TimeZoneInfo.Local.DisplayName, "Context");
            AddMeta("System Language", CultureInfo.CurrentCulture.Name, "Context");
            AddMeta("UI Language", CultureInfo.CurrentUICulture.Name, "Context");
            AddMeta("DOM State", "Interactive", "Runtime");
            AddMeta("Scroll Enabled", "Yes", "Runtime");
            AddMeta("Media Support", "Images / Video", "Runtime");
            AddMeta("Lazy Load", "Enabled", "Runtime");
            AddMeta("Client Rendering", "CSR (React)", "Detected");
            InjectDomReadyScript();
            StartResolveTimer();

            SaveScanResult(
    user,
    IsFacebookUrl(user) ? "Direct URL" : "Username / ID"
);

            btnScan.Enabled = true;
        }

        async void InjectDomReadyScript()
        {
            await liveView.CoreWebView2.ExecuteScriptAsync(@"
                (function(){
                    if (document.readyState === 'complete' || document.readyState === 'interactive') {
                        window.chrome.webview.postMessage('DOM_READY');
                    } else {
                        document.addEventListener('DOMContentLoaded', () => {
                            window.chrome.webview.postMessage('DOM_READY');
                        });
                    }
                })();
            ");
        }

        void StartResolveTimer()
        {
            resolveTimer?.Stop();
            resolveTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            resolveTimer.Tick += (s, e) =>
            {
                resolveTimer.Stop();
                ResolveMetadata();
            };
            resolveTimer.Start();
        }

        void ResolveMetadata()
        {
            if (metaResolved) return;
            metaResolved = true;

            UpdateMeta("Navigation Status", "Completed");
            UpdateMeta("Final URL", liveView.Source?.ToString());
            UpdateMeta("Page Title", liveView.CoreWebView2.DocumentTitle);

            bool loggedIn = liveView.Source?.ToString().Contains("login") == false;
            UpdateMeta("Session Type", loggedIn ? "Logged-in" : "Guest");
            UpdateMeta("Visibility", loggedIn ? "Public" : "Login Required");

            UpdateMeta("Images Loaded", "Yes");
            UpdateMeta("Scripts Executed", "Yes");
            UpdateMeta("Profile Type", "User");
        }

        void AddMeta(string field, string value, string status)
        {
            grid.Rows.Add(field, value, status);
        }

        void UpdateMeta(string field, string value)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells[0].Value?.ToString() == field)
                {
                    row.Cells[1].Value = value;
                    return;
                }
            }
        }



        public class SuggestionPopup : Form
        {
            public ListBox List = new();
            public event Action<string> OnSelect;
            protected override bool ShowWithoutActivation => true;


            public SuggestionPopup()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;

                List.Dock = DockStyle.Fill;
                Controls.Add(List);

                List.DoubleClick += (_, __) => Select();
                List.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter) Select();
                };
            }

            void Select()
            {
                if (List.SelectedItem != null)
                    OnSelect?.Invoke(List.SelectedItem.ToString());
            }
        }
    }
}
