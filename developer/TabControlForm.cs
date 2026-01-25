using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public static class ProcessTitleSetter
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("propsys.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int PSGetPropertyKeyFromName(string name, out PROPERTYKEY key);

    [DllImport("propsys.dll", SetLastError = true)]
    private static extern int PropVariantFromString(string value, out PROPVARIANT variant);

    [DllImport("propsys.dll", SetLastError = true)]
    private static extern int PSSetPropertyValue(
        ref IPropertyStore propertyStore,
        ref PROPERTYKEY key,
        ref PROPVARIANT value);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHGetPropertyStoreFromWindow(
        IntPtr hwnd,
        ref Guid riid,
        out IPropertyStore propertyStore);

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr ptr;
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        int GetCount(out uint count);
        int GetAt(uint iProp, out PROPERTYKEY pkey);
        int GetValue(ref PROPERTYKEY pkey, out PROPVARIANT pv);
        int SetValue(ref PROPERTYKEY pkey, ref PROPVARIANT pv);
        int Commit();
    }

    public static void SetProcessTitle(string title)
    {
        try
        {
            // Get AppUserModel.ID property key
            PSGetPropertyKeyFromName("System.AppUserModel.ID", out PROPERTYKEY appIdKey);
            PSGetPropertyKeyFromName("System.Title", out PROPERTYKEY titleKey);

            // Create property variants
            PropVariantFromString($"ToolProfile.Tab.{DateTime.Now.Ticks}", out PROPVARIANT appIdValue);
            PropVariantFromString(title, out PROPVARIANT titleValue);

            // Get process window (hacky way for WebView2)
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var hwnd = process.MainWindowHandle;

            if (hwnd != IntPtr.Zero)
            {
                Guid iid = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
                SHGetPropertyStoreFromWindow(hwnd, ref iid, out IPropertyStore store);

                // Set properties
                PSSetPropertyValue(ref store, ref appIdKey, ref appIdValue);
                PSSetPropertyValue(ref store, ref titleKey, ref titleValue);
                store.Commit();

                Marshal.ReleaseComObject(store);
            }
        }
        catch { }
    }
}

namespace ToolProfile
{
    public class TabControlForm : Form
    {
        private TabControl tabControl;
        private TextBox txtUrl;
        private Button btnNewTab, btnCloseTab;
        private List<string> tabHistory = new List<string>();
        private string historyFile;
        private static TabControlForm _currentInstance; 
        private WebView2 _currentActiveBrowser = null;


        public TabControlForm(string initialUrl = "about:blank")
        {
            // Đóng form cũ nếu có
            if (_currentInstance != null && !_currentInstance.IsDisposed)
            {
                _currentInstance.Close();
            }

            _currentInstance = this; 


            // History file
            historyFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "WebViewData",
                "TabHistory.json"
            );

            LoadHistory();

            // Form setup
            Text = "ToolProfile Browser";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;

            BuildUI();
            CreateNewTab(initialUrl);

            // Hotkey: Ctrl+Shift+T restore all tabs
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.Shift && e.KeyCode == Keys.T)
                    RestoreAllTabs();
            };

            this.FormClosed += (s, e) =>
            {
                if (_currentInstance == this)
                    _currentInstance = null;
            };
        }
        public void AddNewTab(string url)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AddNewTab(url)));
                return;
            }

            CreateNewTab(url);
            this.BringToFront();
            this.WindowState = FormWindowState.Normal;
            this.Focus();
        }

        private void BuildUI()
        {
            // TOP BAR
            var topBar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            btnNewTab = new Button
            {
                Text = "+",
                Size = new Size(40, 30),
                Location = new Point(10, 5),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnNewTab.Click += (s, e) => CreateNewTab();

            btnCloseTab = new Button
            {
                Text = "×",
                Size = new Size(40, 30),
                Location = new Point(60, 5),
                Font = new Font("Segoe UI", 12f),
                FlatStyle = FlatStyle.Flat
            };
            btnCloseTab.Click += (s, e) => CloseCurrentTab();

            txtUrl = new TextBox
            {
                Location = new Point(110, 8),
                Width = 800,
                Height = 24
            };
            txtUrl.KeyDown += async (s, e) =>  
            {
                if (e.KeyCode == Keys.Enter && tabControl.SelectedTab != null)
                {
                    var browser = tabControl.SelectedTab.Controls[0] as WebView2;
                    if (browser == null) return;

                    string url = txtUrl.Text.Trim();
                    if (string.IsNullOrEmpty(url)) return;

                    if (browser.CoreWebView2 == null)
                    {
                        try
                        {
                            await browser.EnsureCoreWebView2Async(); // 👈 CHỜ KHỞI TẠO
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"WebView2 init failed: {ex.Message}", "Error");
                            return;
                        }
                    }

                    if (!url.StartsWith("http://") &&
                        !url.StartsWith("https://") &&
                        !url.StartsWith("file://") &&
                        !url.StartsWith("about:"))
                    {
                        url = "https://" + url;
                    }

                    try
                    {
                        browser.Source = new Uri(url);
                        AddToHistory(url);

                        browser.Focus();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    catch (UriFormatException)
                    {
                        MessageBox.Show("Invalid URL format!", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Navigation error: {ex.Message}", "Error");
                    }
                }
            };

            topBar.Controls.AddRange(new Control[] { btnNewTab, btnCloseTab, txtUrl });

            // TAB CONTROL
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons
            };
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (_currentActiveBrowser != null && _currentActiveBrowser.CoreWebView2 != null)
                {
                    try
                    {
                        _currentActiveBrowser.CoreWebView2.TrySuspendAsync();
                    }
                    catch { }
                }

                if (tabControl.SelectedTab != null)
                {
                    var browser = tabControl.SelectedTab.Controls[0] as WebView2;
                    if (browser?.CoreWebView2 != null)
                    {
                        try
                        {
                            browser.CoreWebView2.Resume();
                            _currentActiveBrowser = browser;
                        }
                        catch { }
                    }
                }

                UpdateUrlBar();
            };

            Controls.Add(tabControl);
            Controls.Add(topBar);
        }

        private void CreateNewTab(string url = "about:blank")
        {
            var tabPage = new TabPage($"Tab {tabControl.TabCount + 1}")
            {
                BackColor = Color.White
            };

            var webView = new WebView2
            {
                Dock = DockStyle.Fill,
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "WebViewData",
                        "Tabs",
                        $"Tab_{tabControl.TabCount + 1}"
                    )
                }
            };

            webView.CoreWebView2InitializationCompleted += (s, e) =>
            {
                ProcessTitleSetter.SetProcessTitle($"Tab {tabControl.TabCount + 1}: {url}");

                if (!string.IsNullOrEmpty(url) && url != "about:blank")
                    webView.CoreWebView2.Navigate(url);
            };

            webView.SourceChanged += (s, e) =>
            {
                if (tabControl.SelectedTab == tabPage)
                    UpdateUrlBar();

                if (webView.Source != null)
                    AddToHistory(webView.Source.ToString());
            };

            webView.NavigationCompleted += (s, e) =>
            {
                string fullTitle = GetPageTitle(webView) ?? $"Tab {tabControl.TabCount}";

          
                string displayTitle = TruncateTabTitle(fullTitle, 12);

                tabPage.Text = displayTitle;

                tabPage.ToolTipText = fullTitle;
            };

            tabPage.Controls.Add(webView);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            _ = InitializeWebViewAsync(webView);
        }

        private string TruncateTabTitle(string title, int maxLength)
        {
            if (string.IsNullOrEmpty(title) || title.Length <= maxLength)
                return title;

            return title.Substring(0, maxLength - 3) + "...";


        }

        private async Task InitializeWebViewAsync(WebView2 webView)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                this.Invoke(new Action(() =>
                    CreateNewTab(e.Uri)));
            };
        }

        private void CloseCurrentTab()
        {
            if (tabControl.TabCount <= 1)
            {
                this.Close();
                return;
            }

            var currentTab = tabControl.SelectedTab;
            if (currentTab != null)
            {
                var browser = currentTab.Controls[0] as WebView2;
                if (browser?.Source != null)
                    AddToHistory(browser.Source.ToString());

                tabControl.TabPages.Remove(currentTab);
                browser?.Dispose();
            }
        }

        private void UpdateUrlBar()
        {
            if (tabControl.SelectedTab == null) return;

            var browser = tabControl.SelectedTab.Controls[0] as WebView2;
            if (browser?.Source != null)
                txtUrl.Text = browser.Source.ToString();
            else
                txtUrl.Text = "";

            txtUrl.Focus();
        }

        private string GetPageTitle(WebView2 webView)
        {
            try
            {
                return webView.CoreWebView2?.DocumentTitle;
            }
            catch { return null; }
        }

        private void AddToHistory(string url)
        {
            if (string.IsNullOrEmpty(url) || url == "about:blank") return;

            tabHistory.Remove(url); // Remove if exists
            tabHistory.Insert(0, url); // Add to beginning

            // Keep only last 100
            if (tabHistory.Count > 100)
                tabHistory = tabHistory.Take(100).ToList();

            SaveHistory();
        }

        private void SaveHistory()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(historyFile));
                File.WriteAllText(historyFile,
                    JsonSerializer.Serialize(tabHistory, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(historyFile))
                    tabHistory = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(historyFile))
                                 ?? new List<string>();
            }
            catch { }
        }

        private void RestoreAllTabs()
        {
            if (tabHistory.Count == 0) return;

            // Close all current tabs except first
            while (tabControl.TabCount > 1)
            {
                var tab = tabControl.TabPages[1];
                tabControl.TabPages.Remove(tab);
                (tab.Controls[0] as WebView2)?.Dispose();
            }

            // Restore history tabs
            foreach (var url in tabHistory)
            {
                if (url != "about:blank")
                    CreateNewTab(url);
            }

            MessageBox.Show($"Restored {tabHistory.Count} tabs!", "Tab Restore",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save all open tabs URLs
            var openTabs = new List<string>();
            foreach (TabPage tab in tabControl.TabPages)
            {
                var browser = tab.Controls[0] as WebView2;
                if (browser?.Source != null)
                    openTabs.Add(browser.Source.ToString());
            }

            // Merge with history
            tabHistory = openTabs.Concat(tabHistory).Distinct().Take(100).ToList();
            SaveHistory();

            base.OnFormClosing(e);
        }
    }
}