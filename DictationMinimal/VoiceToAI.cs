using System.Net.Http.Headers;
using NAudio.Wave;

namespace DictationMinimal;

public class VoiceToAi(string? whisperServerIp = "localhost")
{
    private const string OutputWaveFilePath = "output.wav";
    public WaveInEvent WaveIn { get; set; } = new WaveInEvent();

    private TaskCompletionSource<bool>? _recordingStoppedTcs;

    public void VoiceInputRecordVoice()
    {
        // Dispose previous WaveIn if exists
        WaveIn?.Dispose();
        WaveIn = new WaveInEvent();
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

    private async Task<string?> CallWhisperApiAsync(string outputWaveFilePath, string? initialPrompt = null)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using MultipartFormDataContent content = [];
        ByteArrayContent fileContent = new(await File.ReadAllBytesAsync(outputWaveFilePath));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

        content.Add(fileContent, "audio_file", Path.GetFileName(outputWaveFilePath));
        if (!string.IsNullOrEmpty(initialPrompt))
        {
            content.Add(new StringContent(initialPrompt), "initial_prompt");
        }

        string whisperApiUrl = $"http://{whisperServerIp}:9000/asr?encode=true&task=transcribe&language=en&word_timestamps=false&output=txt";
        HttpResponseMessage response = await client.PostAsync(whisperApiUrl, content);

        string responseString = await response.Content.ReadAsStringAsync();
        return responseString;
    }
}