using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ToolProfile
{
    public class MessengerForm : Form
    {
        WebView2 webView;
        Panel topBar;
        Button btnBack, btnForward, btnRefresh, btnSettings;

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        bool IsMessengerUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            url = url.ToLower();

            return url.Contains("messenger.com") ||
                   url.Contains("facebook.com") ||
                   url.Contains("m.me");
        }

        Icon LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(name);
            return new Icon(stream);
        }

        public MessengerForm()
        {
            Text = " ToolProfile Messenger";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = LoadIcon("ToolProfile.Assets.MainMessenger.ico");
            FormBorderStyle = FormBorderStyle.None;

            BuildUI();
            InitializeWebView();
        }

        void BuildUI()
        {
            topBar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(0, 132, 255)
            };

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

            btnBack = new Button
            {
                Text = "←",
                Width = 40,
                Height = 30,
                Location = new Point(10, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => webView?.GoBack();

            btnForward = new Button
            {
                Text = "→",
                Width = 40,
                Height = 30,
                Location = new Point(60, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            btnForward.FlatAppearance.BorderSize = 0;
            btnForward.Click += (s, e) => webView?.GoForward();

            btnRefresh = new Button
            {
                Text = "↻",
                Width = 40,
                Height = 30,
                Location = new Point(110, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => webView?.Reload();

            btnSettings = new Button
            {
                Text = "⚙",
                Width = 40,
                Height = 30,
                Location = new Point(160, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += (s, e) => ShowSettings();

            var txtUrl = new TextBox
            {
                Text = "https://www.messenger.com",
                Location = new Point(210, 8),
                Width = 500,
                Height = 24
            };
            txtUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && webView != null)
                {
                    string url = txtUrl.Text.Trim();

                    // 🔒 CHECK URL MESSENGER
                    if (!IsMessengerUrl(url))
                    {
                        MessageBox.Show(
                            "This Messenger Lite window does not support browsing other websites!\n" +
                            "URL has been reset to Messenger.com",
                            "Invalid URL",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtUrl.Text = "https://www.messenger.com";
                        webView.CoreWebView2.Navigate("https://www.messenger.com");
                        return;
                    }

                    webView.Source = new Uri(url);
                }
            };

            topBar.Controls.AddRange(new Control[]
            {
              btnBack, btnForward, btnRefresh, btnSettings, txtUrl
            });

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "WebViewData",
                        "Messenger"
                    )
                }
            };

            webView.SourceChanged += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    if (webView.Source != null)
                        txtUrl.Text = webView.Source.ToString();
                }));
            };

            // Layout
            Controls.Add(webView);
            Controls.Add(topBar);
        }

        async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();

            webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            webView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
            webView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                this.Invoke(new Action(() =>
                {
                    string url = e.Uri;

                    var (isSafe, reason) = SafeLinkChecker.CheckUrl(url);

                    if (isSafe)
                    {
                        OpenLinkInTab(url);
                    }
                    else
                    {
                        var warning = new LinkWarningForm(url, reason);
                        warning.ShowDialog(this);

                        if (warning.UserAccepted)
                        {
                            OpenLinkInTab(url);

                            try
                            {
                                var uri = new Uri(url);
                                SafeLinkChecker.AddSafeDomain(uri.Host);
                            }
                            catch { }
                        }
                    }
                }));
            };

            webView.CoreWebView2.Navigate("https://www.messenger.com");

            webView.CoreWebView2.Profile.PreferredColorScheme =
                CoreWebView2PreferredColorScheme.Auto;

            webView.CoreWebView2.PermissionRequested += (s, e) =>
            {
                if (e.PermissionKind == CoreWebView2PermissionKind.Notifications)
                    e.State = CoreWebView2PermissionState.Allow;
            };
        }

        void ShowSettings()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Dark Mode", null, (s, e) => ToggleDarkMode());
            menu.Items.Add("Clear Cache", null, (s, e) => ClearCache());
            menu.Items.Add("Dev Tools", null, (s, e) => ToggleDevTools());
            menu.Items.Add("-");
            menu.Items.Add("Exit", null, (s, e) => Close());

            menu.Show(btnSettings, new Point(0, btnSettings.Height));
        }

        void ToggleDarkMode()
        {
            webView.CoreWebView2.Profile.PreferredColorScheme =
                webView.CoreWebView2.Profile.PreferredColorScheme ==
                CoreWebView2PreferredColorScheme.Dark
                    ? CoreWebView2PreferredColorScheme.Light
                    : CoreWebView2PreferredColorScheme.Dark;
        }

        async void ClearCache()
        {
            await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
            MessageBox.Show("Cache cleared!", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void ToggleDevTools()
        {
            webView.CoreWebView2.Settings.AreDevToolsEnabled =
                !webView.CoreWebView2.Settings.AreDevToolsEnabled;

            if (webView.CoreWebView2.Settings.AreDevToolsEnabled)
                webView.CoreWebView2.OpenDevToolsWindow();
        }

        private void OpenLinkInTab(string url)
        {
            string uniqueUrl = url;
            if (!url.Contains("?"))
                uniqueUrl = url + "?t=" + DateTime.Now.Ticks;
            else
                uniqueUrl = url + "&t=" + DateTime.Now.Ticks;

    


            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OpenLinkInTab(url)));
                return;
            }

            try
            {
                var existingForms = Application.OpenForms.OfType<TabControlForm>().ToList();
                var existingForm = existingForms.FirstOrDefault(f => !f.IsDisposed);

                if (existingForm != null)
                {
                    existingForm.AddNewTab(url);
                    existingForm.WindowState = FormWindowState.Normal;
                    existingForm.BringToFront();
                    existingForm.Focus();
                }
                else
                {
                    var tabForm = new TabControlForm();
                    tabForm.AddNewTab(url); 
                    tabForm.Show(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open link: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}