using System;
using System.Drawing;
using System.Windows.Forms;

namespace ToolProfile
{
    public class LinkWarningForm : Form
    {
        private string _url;
        private string _reason;

        public bool UserAccepted { get; private set; } = false;

        public LinkWarningForm(string url, string reason)
        {
            _url = url;
            _reason = reason;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Style như Discord warning
            this.Text = "⚠️ Link Safety Check";
            this.Size = new Size(450, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(47, 49, 54);
            this.ForeColor = Color.White;

            // Icon warning
            var picWarning = new PictureBox
            {
                Image = SystemIcons.Warning.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(48, 48),
                Location = new Point(20, 20)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "This link may not be safe",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(80, 20),
                Size = new Size(350, 30),
                ForeColor = Color.White
            };

            // URL preview
            var lblUrl = new Label
            {
                Text = $"URL: {ShortenUrl(_url, 40)}",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(80, 55),
                Size = new Size(350, 20),
                ForeColor = Color.LightGray
            };

            // Reason
            var lblReason = new Label
            {
                Text = $"Reason: {_reason}",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(80, 80),
                Size = new Size(350, 40),
                ForeColor = Color.FromArgb(255, 150, 150)
            };

            // Warning message
            var lblMessage = new Label
            {
                Text = "The link you're trying to open is not in our list of trusted websites. " +
                       "Make sure you trust the source before continuing.",
                Location = new Point(20, 130),
                Size = new Size(410, 40),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.WhiteSmoke
            };

            // Buttons
            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 32),
                Location = new Point(200, 180),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 68, 75),
                ForeColor = Color.White
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            var btnContinue = new Button
            {
                Text = "Continue Anyway",
                Size = new Size(120, 32),
                Location = new Point(310, 180),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(88, 101, 242), // Discord blue
                ForeColor = Color.White
            };
            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.Click += (s, e) =>
            {
                UserAccepted = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                picWarning, lblTitle, lblUrl, lblReason, lblMessage, btnCancel, btnContinue
            });

            // Enter key = Continue, Escape = Cancel
            this.AcceptButton = btnContinue;
            this.CancelButton = btnCancel;
        }

        private string ShortenUrl(string url, int maxLength)
        {
            if (url.Length <= maxLength) return url;
            return url.Substring(0, maxLength - 3) + "...";
        }
    }
}