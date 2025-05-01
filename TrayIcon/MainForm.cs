using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using VoiceToAILibrary;

namespace TrayIcon;

public partial class MainForm : Form
{
    private const int HOTKEY_ID = 1;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd, int id);

    private readonly NotifyIcon notifyIcon = new();
    private readonly ContextMenuStrip contextMenu = new();
    private readonly VoiceToAi voiceToAi = new();

    public MainForm()
    {
        // - no visible window
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        Visible = false;

        var assembly = Assembly.GetExecutingAssembly();
        using Stream? iconStream = assembly.GetManifestResourceStream("VoiceToAIDictation.MicrophoneBlue.ico");
        if (iconStream != null)
        {
            //this.Icon = new Icon(iconStream);
            notifyIcon.Icon = new Icon(iconStream);
        }
        else
        {
            MessageBox.Show("Icon resource not found.");
        }

        notifyIcon.Text = "VoiceToAI";
        notifyIcon.Visible = true;

        _ = contextMenu.Items.Add("Exit", null, (s, e) => Exit());
        notifyIcon.ContextMenuStrip = contextMenu;

        // - register global hotkey Ctrl+Alt+A
        bool ok = RegisterHotKey(Handle, HOTKEY_ID,
            MOD_CONTROL | MOD_ALT, (uint)Keys.A);
        if (!ok)
        {
            _ = MessageBox.Show("Could not register hotkey - maybe it�s already taken?");
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            OnHotKeyPressed();
        }

        base.WndProc(ref m);
    }

    private void OnHotKeyPressed()
    {
        Console.WriteLine("--- Listening ---");
        voiceToAi.VoiceInputRecordVoice();

        using SpeechDialog dlg = new(voiceToAi);
        _ = dlg.ShowDialog();
    }

    private bool _allowClose = false;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Only allow closing if user selected Exit from tray
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    private void Exit()
    {
        _allowClose = true;
        notifyIcon.Visible = false;
        Close();
        Application.Exit();
    }

    // - helper regex to turn newlines into spaces except after a dot
    public static Regex NewlineToSpaceRegex() => GenerateNewlineToSpaceRegex();
    
    [GeneratedRegex(@"(?<!\.)\r?\n")]
    private static partial Regex GenerateNewlineToSpaceRegex();
}
