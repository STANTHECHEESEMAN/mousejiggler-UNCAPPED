#region header

// MouseJiggler - MainForm.cs
// 
// Created by: Alistair J R Young (avatar) at 2021/01/24 1:57 AM.
// Updates by: Dimitris Panokostas (midwan)
// Edited: Fully replaced TrackBar with txtPeriod input
//         Changed JigglePeriod to double with min 0.001 s

#endregion

#region using

using System;
using System.ComponentModel;
using System.Windows.Forms;
using ArkaneSystems.MouseJiggler.Properties;

#endregion

namespace ArkaneSystems.MouseJiggler;

public partial class MainForm : Form
{
    public MainForm()
        : this(false, false, false, 1.0)
    {
    }

    public MainForm(bool jiggleOnStartup, bool minimizeOnStartup, bool zenJiggleEnabled, double jigglePeriod)
    {
        InitializeComponent();

        JiggleOnStartup = jiggleOnStartup;

        cbMinimize.Checked = minimizeOnStartup;
        cbZen.Checked = zenJiggleEnabled;

        // Set initial period
        JigglePeriod = jigglePeriod;
        txtPeriod.Text = jigglePeriod.ToString("0.###");

        // Initial tray menu visibility
        trayMenu.Items[1].Visible = !cbJiggling.Checked;
        trayMenu.Items[2].Visible = cbJiggling.Checked;
    }

    public bool JiggleOnStartup { get; }

    private void MainForm_Load(object sender, EventArgs e)
    {
        if (JiggleOnStartup)
            cbJiggling.Checked = true;
    }

    private void UpdateNotificationAreaText()
    {
        if (!cbJiggling.Checked)
        {
            niTray.Text = @"Not jiggling the mouse.";
        }
        else
        {
            var ww = ZenJiggleEnabled ? "with" : "without";
            niTray.Text = $@"Jiggling mouse every {JigglePeriod:0.###} s, {ww} Zen.";
        }
    }

    private void cmdAbout_Click(object sender, EventArgs e)
    {
        new AboutBox().ShowDialog(this);
    }

    private void trayMenu_ClickOpen(object sender, EventArgs e)
    {
        niTray_DoubleClick(sender, e);
    }

    private void trayMenu_ClickExit(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void trayMenu_ClickStartJuggling(object sender, EventArgs e)
    {
        cbJiggling.Checked = true;
        UpdateNotificationAreaText();
    }

    private void trayMenu_ClickStopJuggling(object sender, EventArgs e)
    {
        cbJiggling.Checked = false;
        UpdateNotificationAreaText();
    }

    #region Property synchronization

    private void cbSettings_CheckedChanged(object sender, EventArgs e)
    {
        panelSettings.Visible = cbSettings.Checked;
    }

    private void cbMinimize_CheckedChanged(object sender, EventArgs e)
    {
        MinimizeOnStartup = cbMinimize.Checked;
    }

    private void cbZen_CheckedChanged(object sender, EventArgs e)
    {
        ZenJiggleEnabled = cbZen.Checked;
    }

    private void txtPeriod_TextChanged(object sender, EventArgs e)
    {
        if (!double.TryParse(txtPeriod.Text, out var value) || value < 0.001)
            return; // ignore invalid input

        JigglePeriod = value;
    }

    #endregion

    #region Do the Jiggle!

    protected bool Zig = true;

    private void cbJiggling_CheckedChanged(object sender, EventArgs e)
    {
        jiggleTimer.Enabled = cbJiggling.Checked;
        UpdateTrayMenu();
    }

    private void UpdateTrayMenu()
    {
        trayMenu.Items[1].Visible = !cbJiggling.Checked;
        trayMenu.Items[2].Visible = cbJiggling.Checked;
    }

    private void jiggleTimer_Tick(object sender, EventArgs e)
    {
        if (ZenJiggleEnabled)
            Helpers.Jiggle(0);
        else if (Zig)
            Helpers.Jiggle(4);
        else
            Helpers.Jiggle(-4);

        Zig = !Zig;
    }

    #endregion

    #region Minimize and restore

    private void cmdTrayify_Click(object sender, EventArgs e)
    {
        MinimizeToTray();
    }

    private void niTray_DoubleClick(object sender, EventArgs e)
    {
        RestoreFromTray();
    }

    private void MinimizeToTray()
    {
        Visible = false;
        ShowInTaskbar = false;
        niTray.Visible = true;
        UpdateNotificationAreaText();
    }

    private void RestoreFromTray()
    {
        Visible = true;
        ShowInTaskbar = true;
        niTray.Visible = false;
    }

    #endregion

    #region Settings property backing fields

    private double _jigglePeriod;
    private bool _minimizeOnStartup;
    private bool _zenJiggleEnabled;

    #endregion

    #region Settings properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool MinimizeOnStartup
    {
        get => _minimizeOnStartup;
        set
        {
            _minimizeOnStartup = value;
            Settings.Default.MinimizeOnStartup = value;
            Settings.Default.Save();
            OnPropertyChanged(nameof(MinimizeOnStartup));
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ZenJiggleEnabled
    {
        get => _zenJiggleEnabled;
        set
        {
            _zenJiggleEnabled = value;
            Settings.Default.ZenJiggle = value;
            Settings.Default.Save();
            OnPropertyChanged(nameof(ZenJiggleEnabled));
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double JigglePeriod
    {
        get => _jigglePeriod;
        set
        {
            _jigglePeriod = Math.Max(0.001, value); // enforce minimum
            Settings.Default.JigglePeriod = (int)(_jigglePeriod * 1000); // save ms as int

            // Timer interval in ms, capped at int.MaxValue
            long intervalMs = (long)(_jigglePeriod * 1000);
            jiggleTimer.Interval = intervalMs > int.MaxValue ? int.MaxValue : (int)intervalMs;

            lbPeriod.Text = $@"{_jigglePeriod:0.###} s";
            OnPropertyChanged(nameof(JigglePeriod));
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Minimize on start

    private bool _firstShown = true;

    private void MainForm_Shown(object sender, EventArgs e)
    {
        if (_firstShown && MinimizeOnStartup)
            MinimizeToTray();

        _firstShown = false;
    }

    #endregion
}
