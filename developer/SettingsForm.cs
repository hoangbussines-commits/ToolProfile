namespace ToolProfile;

using System.Reflection;

public class SettingsForm : Form
{
    TextBox txtColor;
    CheckBox chkDisableBrowser;
    MainForm main;
    Label lblError; //  ERROR 
    Label lblStatus;
    Button btnSave; // ENABLE/DISABLE

    Icon LoadIcon(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(name);
        return new Icon(stream);
    }

    public SettingsForm(MainForm parent)
    {
        main = parent;

        Text = "ToolProfile Settings";
        Width = 500; 
        Height = 400;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        Icon = LoadIcon("ToolProfile.Assets.MainApps.ico");

        // === ERROR LABEL 
        lblError = new Label
        {
            Text = "* This box is required.",
            ForeColor = Color.Red,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            Dock = DockStyle.Top,
            Height = 24,
            Visible = false
        };

        lblStatus = new Label
        {
            ForeColor = Color.FromArgb(0, 120, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            Dock = DockStyle.Top,
            Height = 24,
            Visible = false
        };

        // === COLOR INPUT ===
        var lblColor = new Label
        {
            Text = "System interface color:",
            Dock = DockStyle.Top,
            Height = 24
        };

        txtColor = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "white / red / yellow / #FF00FF"
        };

        // 👇 TEXT CHANGED VALIDATE
        txtColor.TextChanged += (_, __) => ValidateInput();

        // === CHECKBOX ===
        chkDisableBrowser = new CheckBox
        {
            Text = "Turn off the browser's live interface",
            Dock = DockStyle.Top,
            Height = 28
        };



        // === SAVE BUTTON ===
        btnSave = new Button
        {
            Text = "Save",
            Dock = DockStyle.Bottom,
            Height = 34,
            Enabled = false 
        };

        var pnlPath = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(245, 245, 245)
        };
        var lblPathTitle = new Label
        {
            Text = "Settings path",
            Dock = DockStyle.Top,
            Height = 18,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.DimGray
        };

        var pathLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        pathLayout.RowStyles.Clear();
        pathLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32F) 
        );

        pathLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F)
        );
        pathLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 90F)
        );

        var txtPath = new TextBox
        {
            Text = main.SettingsPath,
            ReadOnly = true,
            Multiline = true,              
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9f),
            BackColor = Color.White,
            ScrollBars = ScrollBars.None
        };
        var btnCopy = new Button
        {
            Text = "COPY",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnCopy.FlatAppearance.BorderSize = 1;





        btnSave.Click += (_, __) =>
        {
            main.Settings.DisableLiveBrowser = chkDisableBrowser.Checked;
            main.Settings.InterfaceColor = txtColor.Text.Trim();
            main.SaveSettings();
            main.ApplySettings();
            Close();
        };

        btnCopy.Click += (_, __) =>
        {
            Clipboard.SetText(main.SettingsPath);
            ShowStatus("✔ Settings path copied");
        };

        //CONTROL PATH
        var pathRow = new Panel
        {
            Dock = DockStyle.Fill
        };
        pathLayout.Controls.Add(txtPath, 0, 0);
        pathLayout.Controls.Add(btnCopy, 1, 0);

        pnlPath.Controls.Add(pathLayout);
        pnlPath.Controls.Add(lblPathTitle);


        // === ADD CONTROLS 
        Controls.Add(btnSave);
        Controls.Add(pnlPath);          // 👈 EMBED PATH BOX
        Controls.Add(chkDisableBrowser);
        Controls.Add(txtColor);
        Controls.Add(lblColor);
        Controls.Add(lblStatus); // 👈 STATUS 
        Controls.Add(lblError); // 👈 ERROR

        // === LOAD CURRENT VALUE ===
        txtColor.Text = main.Settings.InterfaceColor;
        chkDisableBrowser.Checked = main.Settings.DisableLiveBrowser;

        ValidateInput(); // 👈 VALIDATE


    }

    void ShowStatus(string message, int autoHideMs = 2000)
    {
        lblStatus.Text = message;
        lblStatus.Visible = true;

        var timer = new System.Windows.Forms.Timer
        {
            Interval = autoHideMs
        };

        timer.Tick += (_, __) =>
        {
            timer.Stop();
            timer.Dispose();
            lblStatus.Visible = false;
        };

        timer.Start();
    }



    // 👇VALIDATE 
    void ValidateInput()
    {
        string color = txtColor.Text.Trim();
        bool isValid = !string.IsNullOrWhiteSpace(color);

     
        if (isValid)
        {
            try
            {
                ColorTranslator.FromHtml(color);
            }
            catch
            {
                isValid = false;
                lblError.Text = "* Invalid color format. Use name or hex (#RRGGBB).";
            }
        }
        else
        {
            lblError.Text = "* This box is required.";
        }

        // UI UPDATE
        lblError.Visible = !isValid;
        txtColor.BackColor = isValid ? SystemColors.Window : Color.FromArgb(255, 230, 230);
        txtColor.ForeColor = isValid ? SystemColors.WindowText : Color.DarkRed;
        btnSave.Enabled = isValid;
    }
}
