using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class ElevenLabsVoiceSettings
{
    public float stability = .5f;
    public float similarity_boost = .75f;
    public float style;
    public bool use_speaker_boost = true;
    public float speed = 1;
}
[Serializable]
public class ElevenLabsTTSRequest
{
    public string text;
    public string model_id;
    public ElevenLabsVoiceSettings voice_settings;
}
[Serializable]
public class SpeechServiceConfig
{
    public string apiKey;
    public string voiceId;
}

public class ElevenLabsClient : MonoBehaviour
{
    public static ElevenLabsClient Instance { get; private set; }
    [SerializeField] private Button recordButton;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private string apiKey;
    [SerializeField] private string voiceId;
    [SerializeField] private TeacherController teacherController;
    [SerializeField] private string speechToTextModelId = "scribe_v2";
    [SerializeField] private string languageCode = "deu";
    [SerializeField] private bool diarize;
    [SerializeField] private bool tagAudioEvents;
    [SerializeField] private string textToSpeechModelId = "eleven_flash_v2_5";
    [SerializeField] private ElevenLabsVoiceSettings voiceSettings = new ElevenLabsVoiceSettings();
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int maxRecordingLengthSeconds = 60;
    [SerializeField] private int sampleRate = 16000;
    private UnityWebRequest activeTTSRequest;
    private UnityWebRequest activeSTTRequest;
    private int operationVersion;
    private AudioClip recordedClip;
    private AudioClip speechClip;
    private string microphoneDevice;
    private bool isRecording;
    private bool isBusy;
    private float recordingStarted;
    [Serializable] private class SpeechToTextResponse { public string text; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadSettings();
        if (recordButton != null) recordButton.onClick.AddListener(OnRecordButtonClicked);
        if (teacherController == null) teacherController = FindAnyObjectByType<TeacherController>();
        SetIdleUi();
    }
    private void LoadSettings()
    {
        var settings = RuntimeServiceConfig.Read<SpeechServiceConfig>("speech-config.json",
            "Scripts/SpeechTransformation/Config/speech-config.json");
        apiKey = RuntimeServiceConfig.EnvironmentOr("SECCITY_ELEVENLABS_API_KEY", settings?.apiKey ?? apiKey);
        voiceId = RuntimeServiceConfig.EnvironmentOr("SECCITY_ELEVENLABS_VOICE_ID", settings?.voiceId ?? voiceId);
    }
    private void Update()
    {
        if (isRecording && Time.realtimeSinceStartup - recordingStarted >= maxRecordingLengthSeconds)
            StopRecordingAndTranscribe(true);
    }
    private void OnDisable() { CancelSpeech(false); }
    private void OnDestroy()
    {
        if (recordButton != null) recordButton.onClick.RemoveListener(OnRecordButtonClicked);
        if (Instance == this) Instance = null;
    }

    public void ToggleRecording()
    {
        OnRecordButtonClicked();
    }

    private void OnRecordButtonClicked()
    {
        if (isBusy || (teacherController != null && !teacherController.CanRecord)) return;
        if (isRecording) StopRecordingAndTranscribe(false);
        else StartRecording();
    }
    private void StartRecording()
    {
        LoadSettings();
        if (string.IsNullOrWhiteSpace(apiKey)) { SetStatus("ElevenLabs API-Schluessel fehlt."); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            SetStatus("Mikrofonzugriff erlauben und Aufnahme erneut starten.");
            return;
        }
#endif
        if (Microphone.devices.Length == 0) { SetStatus("Kein Mikrofon gefunden."); return; }
        microphoneDevice = Microphone.devices[0];
        maxRecordingLengthSeconds = Mathf.Clamp(maxRecordingLengthSeconds, 1, 120);
        try { recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingLengthSeconds, sampleRate); }
        catch (Exception) { SetStatus("Mikrofon konnte nicht gestartet werden."); return; }
        if (recordedClip == null) { SetStatus("Keine Mikrofonaufnahme verfuegbar."); return; }
        isRecording = true;
        recordingStarted = Time.realtimeSinceStartup;
        if (buttonLabel != null) buttonLabel.text = "Stop";
        SetStatus("Aufnahme laeuft ...");
    }
    private void StopRecordingAndTranscribe(bool reachedLimit)
    {
        int samplePosition = recordedClip == null ? 0 :
            (reachedLimit ? recordedClip.samples : Microphone.GetPosition(microphoneDevice));
        Microphone.End(microphoneDevice);
        isRecording = false;
        SetIdleUi();
        if (recordedClip == null || samplePosition <= 0)
        {
            ReleaseRecording();
            SetStatus("Keine Audiodaten aufgenommen.");
            return;
        }
        samplePosition = Mathf.Min(samplePosition, recordedClip.samples);
        var samples = new float[samplePosition * recordedClip.channels];
        recordedClip.GetData(samples, 0);
        var trimmed = AudioClip.Create("Question", samplePosition, recordedClip.channels, recordedClip.frequency, false);
        trimmed.SetData(samples, 0);
        byte[] wav = WavUtility.FromAudioClip(trimmed);
        Destroy(trimmed);
        ReleaseRecording();
        isBusy = true;
        if (recordButton != null) recordButton.interactable = false;
        int version = ++operationVersion;
        StartCoroutine(UploadAudio(wav, version));
    }
    private IEnumerator UploadAudio(byte[] wav, int version)
    {
        SetStatus("Transkribiere ...");
        var form = new WWWForm();
        form.AddField("model_id", speechToTextModelId);
        if (!string.IsNullOrWhiteSpace(languageCode)) form.AddField("language_code", languageCode);
        form.AddField("diarize", diarize ? "true" : "false");
        form.AddField("tag_audio_events", tagAudioEvents ? "true" : "false");
        form.AddBinaryData("file", wav, "recording.wav", "audio/wav");
        using (var request = UnityWebRequest.Post("https://api.elevenlabs.io/v1/speech-to-text", form))
        {
            activeSTTRequest = request;
            request.timeout = 60;
            request.SetRequestHeader("xi-api-key", apiKey);
            try
            {
                yield return request.SendWebRequest();
                if (version != operationVersion) yield break;
                string transcript = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try { transcript = JsonUtility.FromJson<SpeechToTextResponse>(request.downloadHandler.text)?.text; }
                    catch (ArgumentException) { }
                }
                // Release STT before handing over: the teacher owns the next busy state.
                activeSTTRequest = null;
                isBusy = false;
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    SetStatus("Transkription fehlgeschlagen oder leer. Bitte erneut versuchen.");
                    yield break;
                }
                if (resultLabel != null) { resultLabel.richText = false; resultLabel.text = transcript; }
                SetStatus("Frage wird beantwortet ...");
                if (teacherController != null) teacherController.Talk(transcript);
                else { SetStatus("TeacherController fehlt."); }
            }
            finally
            {
                if (ReferenceEquals(activeSTTRequest, request)) activeSTTRequest = null;
            }
        }
    }
    public void Speak(string text, Action onFinished = null)
    {
        CancelSpeech(false);
        LoadSettings();
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(voiceId) || audioSource == null)
        {
            SetStatus("Textansicht aktiv; Sprachausgabe nicht verfuegbar.");
            onFinished?.Invoke();
            return;
        }
        isBusy = true;
        StartCoroutine(SendSpeech(text, operationVersion, onFinished));
    }
    private IEnumerator SendSpeech(string text, int version, Action onFinished)
    {
        string url = "https://api.elevenlabs.io/v1/text-to-speech/" + UnityWebRequest.EscapeURL(voiceId) + "?output_format=mp3_44100_128";
        var payload = new ElevenLabsTTSRequest { text = text, model_id = textToSpeechModelId, voice_settings = voiceSettings };
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            activeTTSRequest = request;
            request.timeout = 60;
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);
            request.SetRequestHeader("Accept", "audio/mpeg");
            try
            {
                SetStatus("Erzeuge Sprache ...");
                yield return request.SendWebRequest();
                if (version != operationVersion) yield break;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus("Sprachausgabe fehlgeschlagen. Die Textantwort bleibt sichtbar.");
                    yield break;
                }
                speechClip = DownloadHandlerAudioClip.GetContent(request);
                if (speechClip == null || audioSource == null) yield break;
                audioSource.loop = false;
                audioSource.clip = speechClip;
                audioSource.Play();
                SetStatus("Avatar spricht ...");
                yield return null;
                while (version == operationVersion && audioSource != null && audioSource.isPlaying) yield return null;
            }
            finally
            {
                if (ReferenceEquals(activeTTSRequest, request)) activeTTSRequest = null;
                if (version == operationVersion)
                {
                    isBusy = false;
                    ReleaseSpeech();
                    onFinished?.Invoke();
                }
            }
        }
    }
    private void SetIdleUi() { if (buttonLabel != null) buttonLabel.text = "Record"; }
    private void SetStatus(string value) { if (statusLabel != null) statusLabel.text = value; }
    private void ReleaseRecording() { if (recordedClip != null) Destroy(recordedClip); recordedClip = null; }
    private void ReleaseSpeech()
    {
        if (audioSource != null) { audioSource.Stop(); audioSource.clip = null; }
        if (speechClip != null) Destroy(speechClip);
        speechClip = null;
    }
    public void CancelSpeech(bool restoreRecordButton = false)
    {
        operationVersion++;
        var tts = activeTTSRequest; activeTTSRequest = null; tts?.Abort();
        var stt = activeSTTRequest; activeSTTRequest = null; stt?.Abort();
        if (isRecording) Microphone.End(microphoneDevice);
        isRecording = false;
        isBusy = false;
        ReleaseRecording();
        ReleaseSpeech();
        SetIdleUi();
        if (recordButton != null) recordButton.interactable = restoreRecordButton;
    }
}
public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int channels = clip.channels;
            int sampleRate = clip.frequency;
            int sampleCount = clip.samples;

            float[] samples = new float[sampleCount * channels];
            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];
            const float rescaleFactor = 32767f;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)Mathf.Clamp(samples[i] * rescaleFactor, short.MinValue, short.MaxValue);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            WriteHeader(stream, bytesData.Length, channels, sampleRate);
            stream.Write(bytesData, 0, bytesData.Length);
            return stream.ToArray();
        }
    }

    private static void WriteHeader(Stream stream, int dataLength, int channels, int sampleRate)
    {
        int fileSize = 36 + dataLength;

        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);
        }
    }
}