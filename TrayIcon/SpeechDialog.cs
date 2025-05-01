using DictationMinimal;
using TextCopy;

namespace TrayIcon;

public partial class SpeechDialog : Form
{
    private readonly VoiceToAi voiceToAi;

    public SpeechDialog(VoiceToAi voiceToAi)
    {
        this.voiceToAi = voiceToAi;

        Text = "Voice Input";
        Width = 300;
        Height = 120;

        Button buttonDone = new()
        {
            Text = "finished speaking",
            Dock = DockStyle.Fill
        };
        buttonDone.Click += async (s, e) =>
        {
            Console.WriteLine("--- Processing ---");
            string textDictation = await voiceToAi
                .VoiceProcessRecordingToTextAsync();
            textDictation = textDictation.Trim();
            textDictation = MainForm
                .NewlineToSpaceRegex().Replace(textDictation, " ");
            textDictation = textDictation.Replace("  ", " ");
            await ClipboardService.SetTextAsync(textDictation);
            Console.WriteLine("--- Raw transcription copied to clipboard ---");
            Close();
        };

        Controls.Add(buttonDone);
    }
}
