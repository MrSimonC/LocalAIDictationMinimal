using System.Text.RegularExpressions;
using DictationMinimal;
using TextCopy;

VoiceToAi voiceToAi = new();
Console.WriteLine("--- Listening ---");
voiceToAi.VoiceInputRecordVoice();
Console.ReadKey(true);
Console.WriteLine("--- Processing ---");
string textDictation = await voiceToAi.VoiceProcessRecordingToTextAsync();
textDictation = textDictation.Trim();
// Replace new lines (but not after a ".") with spaces
textDictation = NewlineToSpaceRegex().Replace(textDictation, " ");
textDictation = textDictation.Replace("  ", " ");
// Optionally post-process the transcription with a CSV file
string? whisperPostProcessingCsv = GetEnvironmentVariableFileContents("WHISPER_AI_POST_PROCESSING_PATH");
if (!string.IsNullOrEmpty(whisperPostProcessingCsv))
{
    textDictation = PostProcessWhisperWithCSV(whisperPostProcessingCsv, textDictation);
}
else
{
    Console.WriteLine("--- (No post-processing CSV file found) ---");
}
// Final output to Clipboard
await ClipboardService.SetTextAsync(textDictation);
Console.WriteLine("--- Raw transcription copied to clipboard ---");

static string PostProcessWhisperWithCSV(string whisperPostProcessingCsv, string textDictation)
{
    if (!string.IsNullOrEmpty(whisperPostProcessingCsv))
    {
        string[] initialWhisperPromptCsvLines = whisperPostProcessingCsv.Split("\r\n");
        for (int i = 1; i < initialWhisperPromptCsvLines.Length; i++)
        {
            string[] initialWhisperPromptCsvLine = initialWhisperPromptCsvLines[i].Split(",");
            string firstColumnValue = initialWhisperPromptCsvLine[0];
            if (firstColumnValue == "{newline}") // special self-made pattern representing a new line
            {
                firstColumnValue = "\n";
            }
            textDictation = textDictation.Replace(firstColumnValue, initialWhisperPromptCsvLine[1]);
        }
    }

    return textDictation;
}

static string? GetEnvironmentVariableFileContents(string environmentVariableName, bool throwIfContentsEmpty = false)
{
    string? filePath = Environment.GetEnvironmentVariable(environmentVariableName);
    return !string.IsNullOrEmpty(filePath) && File.Exists(filePath)
        ? File.ReadAllText(filePath)
        : throwIfContentsEmpty ? throw new ArgumentNullException(environmentVariableName) : null;
}

internal partial class Program
{
    [GeneratedRegex(@"(?<!\.)\r?\n\s*")]
    private static partial Regex NewlineToSpaceRegex();
}