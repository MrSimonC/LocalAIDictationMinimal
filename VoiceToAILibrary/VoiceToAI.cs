using System.Net.Http.Headers;
using System.Text;
using NAudio.Wave;
using Whisper.net;

namespace VoiceToAILibrary;

public class VoiceToAi : IDisposable
{
    private const string OutputWaveFilePath = "output.wav";
    public WaveInEvent WaveIn { get; set; } = new WaveInEvent();
    private TaskCompletionSource<bool>? _recordingStoppedTcs;
    private string? _whisperModelPath = "ggml-models/ggml-small.en.bin"; // Default to bundled GGML model

    // Fields to keep model and processor in memory
    private WhisperFactory? _whisperFactory;
    private WhisperProcessor? _whisperProcessor;
    private bool _isInitialized = false;

    public void SetWhisperModelPath(string modelPath)
    {
        _whisperModelPath = modelPath;
        _isInitialized = false; // Force re-init if model path changes
    }

    // Call this before first transcription
    public void InitWhisper()
    {
        if (_isInitialized) return;
        var modelPath = Path.Combine(AppContext.BaseDirectory, _whisperModelPath ?? "ggml-models/ggml-small.en.bin");
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        _whisperFactory?.Dispose();
        _whisperFactory = WhisperFactory.FromPath(modelPath);
        _whisperProcessor?.Dispose();
        _whisperProcessor = _whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .SplitOnWord()
            .Build();
        _isInitialized = true;
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
        InitWhisper();
        _recordingStoppedTcs = new TaskCompletionSource<bool>();
        WaveIn.StopRecording();
        _ = await _recordingStoppedTcs.Task; // Wait for RecordingStopped and file release
        string? transcription = await CallWhisperApiAsync(OutputWaveFilePath, initialPrompt);
        File.Delete(OutputWaveFilePath);
        return transcription ?? string.Empty;
    }

    private async Task<string?> CallWhisperApiAsync(string audioPath, string? initialPrompt = null)
    {
        if (_whisperProcessor is null)
            throw new InvalidOperationException("Whisper processor not initialized. Call InitWhisper() first.");
        using var fileStream = File.OpenRead(audioPath);
        StringBuilder transcription = new();
        await foreach (var result in _whisperProcessor.ProcessAsync(fileStream))
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

    public void Dispose()
    {
        _whisperProcessor?.Dispose();
        _whisperFactory?.Dispose();
        WaveIn?.Dispose();
    }
}