# DictationMinimal

DictationMinimal is a lightweight C# application that records audio from your microphone, transcribes the recording via an external Whisper ASR API, and copies the transcription to your clipboard.

## Features

- **Voice Recording:** Uses NAudio to capture audio and saves a temporary WAV file
- **Speech-To-Text:** Sends the recorded audio to a Whisper ASR API for transcription
- **Clipboard Integration:** Automatically copies the transcribed text to your clipboard

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (for running the Whisper ASR API server)

## Instant Set up

For CPU based systems, run docker command

```sh
docker run -d -p 9000:9000 -e ASR_MODEL=small.en -e ASR_ENGINE=openai_whisper onerahmet/openai-whisper-asr-webservice:latest
```

Then run the published executable from [this folder]("./published%20executable/DictationMinimal.exe").

## Setup

1. **Clone the repository:**
    ```sh
    git clone https://github.com/MrSimonC/DictationMinimal.git
    cd DictationMinimal
    ```

2. **Build:**
    ```sh
    dotnet restore
    dotnet build
    ```

3. **Run the Whisper ASR API Server:**

   The easiest way to run the program is to first start the Whisper ASR API server using Docker:
    ```sh
    docker run -d -p 9000:9000 -e ASR_MODEL=small.en -e ASR_ENGINE=openai_whisper onerahmet/openai-whisper-asr-webservice:latest
    ```

4. **Run the Application:**

   Instead of running the code using `dotnet run`, you can now simply access the published executable:
    - On Windows, run [DictationMinimal.exe](http://_vscodecontentref_/0) from the output directory.
    
## Using the Whisper ASR API

By default, the application connects to `http://localhost:9000/asr`.

## Code Structure

- **Program.cs**: Entry point that initializes recording, processes audio input, and handles clipboard integration.
- **VoiceToAI.cs**: Contains the logic for recording audio using NAudio and Whisper API interaction.