/*
https://github.com/ahmetoner/whisper-asr-webservice
Running on CPU only:
docker run -d -p 9000:9000 -e ASR_MODEL=small.en -e ASR_ENGINE=openai_whisper onerahmet/openai-whisper-asr-webservice:latest
*/
using TextCopy;
using DictationMinimal;

var voiceToAi = new VoiceToAi();
Console.WriteLine("--- Listening ---");
voiceToAi.VoiceInputRecordVoice();
Console.ReadKey(true);
Console.WriteLine("--- Processing ---");
string textDictation = await voiceToAi.VoiceProcessRecordingToTextAsync();
textDictation = textDictation.Trim();
await ClipboardService.SetTextAsync(textDictation);
Console.WriteLine("--- Raw transcription copied to clipboard ---");