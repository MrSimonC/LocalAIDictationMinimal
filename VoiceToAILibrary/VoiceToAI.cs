using System.Net.Http.Headers;
using System.Text;
using NAudio.Wave;
using Whisper.net;

namespace VoiceToAILibrary;

public class VoiceToAi
{
    private const string OutputWaveFilePath = "output.wav";
    public WaveInEvent WaveIn { get; set; } = new WaveInEvent();
    private TaskCompletionSource<bool>? _recordingStoppedTcs;
    private string? _whisperModelPath = "ggml-models/ggml-small.en.bin"; // Default to bundled GGML model

    public void SetWhisperModelPath(string modelPath)
    {
        _whisperModelPath = modelPath;
    }

    public void VoiceInputRecordVoice()
    {
        // Dispose previous WaveIn if exists
        WaveIn?.Dispose();
        WaveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1) // 16kHz mono required by Whisper.net
        };
        WaveFileWriter writer = new(OutputWaveFilePath, WaveIn.WaveFormat);
        WaveIn.DataAvailable += (sender, args) => writer.Write(args.Buffer, 0, args.BytesRecorded);
        WaveIn.RecordingStopped += (sender, args) =>
        {
            writer.Dispose();
            _ = (_recordingStoppedTcs?.TrySetResult(true));
        };
        WaveIn.StartRecording();
    }

    public async Task<string> VoiceProcessRecordingToTextAsync(string? initialPrompt = null)
    {
        _recordingStoppedTcs = new TaskCompletionSource<bool>();
        WaveIn.StopRecording();
        _ = await _recordingStoppedTcs.Task; // Wait for RecordingStopped and file release
        string? transcription = await CallWhisperApiAsync(OutputWaveFilePath, initialPrompt);
        File.Delete(OutputWaveFilePath);
        return transcription ?? string.Empty;
    }

    private static async Task<string?> CallWhisperApiAsync(string audioPath, string? initialPrompt = null)
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "ggml-models", "ggml-small.en.bin");
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        using var whisperFactory = WhisperFactory.FromPath(modelPath);
        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .SplitOnWord()
            .Build();
        using var fileStream = File.OpenRead(audioPath);
        StringBuilder transcription = new();
        await foreach (var result in processor.ProcessAsync(fileStream))
        {
            // Only add words if explicit word-level timings are available
            if (result.Tokens != null && result.Tokens.Count() > 0)
            {
                foreach (var word in result.Tokens)
                {
                    //var text = word.Text?.Trim() ?? "";
                    var text = word.Text ?? "";
                    if (!IsFilteredToken(text))
                    {
                        transcription.Append(text);
                    }
                }
            }
            else
            {
                Console.WriteLine($"WARNING: No word-level timings for segment '{result.Text}'. Skipping.");
            }
        }
        return transcription.ToString();
    }

    static bool IsFilteredToken(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return true;
        word = word.Trim();
        // Exclude any word that matches [anything]
        if (System.Text.RegularExpressions.Regex.IsMatch(word, @"^\[.*\]$")) return true;
        return false;
    }

}