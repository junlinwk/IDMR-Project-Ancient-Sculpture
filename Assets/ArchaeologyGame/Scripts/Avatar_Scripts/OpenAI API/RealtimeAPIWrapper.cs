using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class RealtimeAPIWrapper : MonoBehaviour
{
    private ClientWebSocket ws;

    [SerializeField] private string apiKey = "YOUR_API_KEY";
    [TextArea(4, 10)] [SerializeField] private string systemPrompt = "You are a helpful assistant.";
    public AudioRecorder audioRecorder;
    public AudioPlayer audioPlayer;
    public AudioPlayer lipsyncAudioPlayer;
    [SerializeField] private string[] medicalRecordKeywords = {
        "\u524d\u5f8c\u77db\u76fe",
        "\u77db\u76fe"
    };
    [SerializeField] private float surprisedDuration = 3f;

    private readonly StringBuilder messageBuffer = new StringBuilder();
    private readonly StringBuilder transcriptBuffer = new StringBuilder();
    private readonly StringBuilder inputTranscriptBuffer = new StringBuilder();
    private bool isResponseInProgress = false;
    private AnimationHandler animationHandler;
    private Coroutine clearSurprisedRoutine;

    public static event Action OnWebSocketConnected;
    public static event Action OnWebSocketClosed;
    public static event Action OnSessionCreated;
    public static event Action OnConversationItemCreated;
    public static event Action OnResponseDone;
    public static event Action<string> OnTranscriptReceived;
    public static event Action OnResponseCreated;
    public static event Action OnResponseAudioDone;
    public static event Action OnResponseAudioTranscriptDone;
    public static event Action OnResponseContentPartDone;
    public static event Action OnResponseOutputItemDone;
    public static event Action OnRateLimitsUpdated;
    public static event Action OnResponseOutputItemAdded;
    public static event Action OnResponseContentPartAdded;
    public static event Action OnResponseCancelled;
    public static event Action OnConnectButtonPressed;

    private void Start()
    {
        AudioRecorder.OnAudioRecorded += SendAudioToAPI;
        animationHandler = FindObjectOfType<AnimationHandler>();
    }

    private void OnDestroy()
    {
        AudioRecorder.OnAudioRecorded -= SendAudioToAPI;
    }

    private void OnApplicationQuit() => DisposeWebSocket();

    public async void ConnectWebSocketButton()
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY")
        {
            Debug.LogError("[RealtimeAPIWrapper] apiKey is not set. Assign a valid OpenAI API key in the Inspector before connecting.");
            return;
        }

        if (ws != null)
        {
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                DisposeWebSocket();
            }
            else
            {
                ws.Dispose();
                ws = null;
            }
        }
        else
        {
            ws = new ClientWebSocket();
            Debug.Log("[RealtimeAPIWrapper] Opening websocket connection...");
            await ConnectWebSocket();
        }

        OnConnectButtonPressed?.Invoke();
    }

    private async Task ConnectWebSocket()
    {
        try
        {
            var uri = new Uri("wss://api.openai.com/v1/realtime?model=gpt-realtime-mini");
            ws.Options.SetRequestHeader("Authorization", "Bearer " + apiKey);
            // ws.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
            Debug.Log("[RealtimeAPIWrapper] Connecting to OpenAI Realtime API...");
            await ws.ConnectAsync(uri, CancellationToken.None);
            await ConfigureSession();
            OnWebSocketConnected?.Invoke();
            Debug.Log("[RealtimeAPIWrapper] WebSocket connected.");
            _ = ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError("websocket connection failed: " + e.Message);
            DisposeWebSocket();
        }
    }

    private async Task ConfigureSession()
    {
        if (ws == null || ws.State != WebSocketState.Open)
        {
            return;
        }

        var sessionUpdateMessage = new
        {
            type = "session.update",
            session = new
            {
                type = "realtime",
                instructions = systemPrompt,
                audio = new
                {
                    input = new
                    {
                        format = new
                        {
                            type = "audio/pcm",
                            rate = 24000
                        },
                        transcription = new
                        {
                            model = "gpt-4o-mini-transcribe",
                            language = "zh",
                            prompt = "Traditional Chinese conversation."
                        },
                        turn_detection = new
                        {
                            type = "server_vad"
                        }
                    },
                    output = new
                    {
                        format = new
                        {
                            type = "audio/pcm",
                            rate = 24000
                        },
                        voice = "marin"
                    }
                }
            }
        };

        string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(sessionUpdateMessage);
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
        await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async void SendCancelEvent()
    {
        if (ws.State == WebSocketState.Open && isResponseInProgress)
        {
            var cancelMessage = new { type = "response.cancel" };
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(cancelMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            OnResponseCancelled?.Invoke();
            isResponseInProgress = false;
        }
    }

    private async void SendAudioToAPI(string base64AudioData)
    {
        if (isResponseInProgress)
        {
            SendCancelEvent();
        }

        if (ws != null && ws.State == WebSocketState.Open)
        {
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new { type = "input_audio", audio = base64AudioData }
                    }
                }
            };

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(eventMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            var responseMessage = new
            {
                type = "response.create",
                response = new
                {
                    output_modalities = new[] { "audio" },
                    instructions = BuildResponseInstructions("Please provide a transcript. If the language is mandarin, please provide the transcript in traditional Chinese (TW).")
                }
            };

            string responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(responseMessage);
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async void SendTextToAPI(string text)
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new { type = "input_text", text }
                    }
                }
            };

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(eventMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            var responseMessage = new
            {
                type = "response.create",
                response = new
                {
                    output_modalities = new[] { "text" },
                    instructions = BuildResponseInstructions("Please do not provide audio for this request.")
                }
            };

            string responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(responseMessage);
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task ReceiveMessages()
    {
        var buffer = new byte[1024 * 128];
        var messageHandlers = GetMessageHandlers();

        while (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (ws.State == WebSocketState.CloseReceived)
            {
                Debug.Log("websocket close received, disposing current ws instance.");
                DisposeWebSocket();
                return;
            }

            if (result.EndOfMessage)
            {
                string fullMessage = messageBuffer.ToString();
                messageBuffer.Clear();

                if (!string.IsNullOrEmpty(fullMessage.Trim()))
                {
                    try
                    {
                        JObject eventMessage = JObject.Parse(fullMessage);
                        string messageType = eventMessage["type"]?.ToString();

                        if (messageHandlers.TryGetValue(messageType, out var handler))
                        {
                            handler(eventMessage);
                        }
                        else
                        {
                            Debug.Log("unhandled message type: " + messageType);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("error parsing json: " + ex.Message);
                    }
                }
            }
        }
    }

    private Dictionary<string, Action<JObject>> GetMessageHandlers()
    {
        return new Dictionary<string, Action<JObject>>
        {
            { "response.output_audio.delta", HandleAudioDelta },
            { "response.audio.delta", HandleAudioDelta },
            { "response.output_audio_transcript.delta", HandleTranscriptDelta },
            { "response.audio_transcript.delta", HandleTranscriptDelta },
            { "response.output_text.delta", HandleOutputTextDelta },
            { "conversation.item.input_audio_transcription.delta", HandleInputAudioTranscriptionDelta },
            { "conversation.item.input_audio_transcription.completed", HandleInputAudioTranscriptionCompleted },
            { "conversation.item.created", _ => OnConversationItemCreated?.Invoke() },
            { "conversation.item.added", _ => OnConversationItemCreated?.Invoke() },
            { "response.done", HandleResponseDone },
            { "response.created", HandleResponseCreated },
            { "session.created", _ => OnSessionCreated?.Invoke() },
            { "response.output_audio.done", _ => OnResponseAudioDone?.Invoke() },
            { "response.audio.done", _ => OnResponseAudioDone?.Invoke() },
            { "response.output_audio_transcript.done", _ => OnResponseAudioTranscriptDone?.Invoke() },
            { "response.audio_transcript.done", _ => OnResponseAudioTranscriptDone?.Invoke() },
            { "response.output_text.done", _ => OnResponseAudioTranscriptDone?.Invoke() },
            { "response.content_part.done", _ => OnResponseContentPartDone?.Invoke() },
            { "response.output_item.done", _ => OnResponseOutputItemDone?.Invoke() },
            { "response.output_item.added", _ => OnResponseOutputItemAdded?.Invoke() },
            { "response.content_part.added", _ => OnResponseContentPartAdded?.Invoke() },
            { "rate_limits.updated", _ => OnRateLimitsUpdated?.Invoke() },
            { "error", HandleError }
        };
    }

    private void HandleAudioDelta(JObject eventMessage)
    {
        string base64AudioData = eventMessage["delta"]?.ToString();
        if (!string.IsNullOrEmpty(base64AudioData))
        {
            byte[] pcmAudioData = Convert.FromBase64String(base64AudioData);
            audioPlayer.EnqueueAudioData(pcmAudioData);
            lipsyncAudioPlayer.EnqueueAudioData(pcmAudioData);
        }
    }

    private void HandleTranscriptDelta(JObject eventMessage)
    {
        string transcriptPart = eventMessage["delta"]?.ToString();
        if (!string.IsNullOrEmpty(transcriptPart))
        {
            transcriptBuffer.Append(transcriptPart);
            OnTranscriptReceived?.Invoke(transcriptPart);
            TryTriggerSurprised(transcriptBuffer.ToString());
        }
    }

    private void HandleOutputTextDelta(JObject eventMessage)
    {
        string textPart = eventMessage["delta"]?.ToString();
        if (!string.IsNullOrEmpty(textPart))
        {
            transcriptBuffer.Append(textPart);
            OnTranscriptReceived?.Invoke(textPart);
            TryTriggerSurprised(transcriptBuffer.ToString());
        }
    }

    private void HandleInputAudioTranscriptionCompleted(JObject eventMessage)
    {
        string transcript = eventMessage["transcript"]?.ToString();
        TryTriggerSurprised(transcript);
        inputTranscriptBuffer.Clear();
    }

    private void HandleInputAudioTranscriptionDelta(JObject eventMessage)
    {
        string transcriptPart = eventMessage["delta"]?.ToString();
        if (string.IsNullOrEmpty(transcriptPart))
        {
            return;
        }

        inputTranscriptBuffer.Append(transcriptPart);
        TryTriggerSurprised(inputTranscriptBuffer.ToString());
    }

    private void HandleResponseDone(JObject eventMessage)
    {
        if (!audioPlayer.IsAudioPlaying())
        {
            isResponseInProgress = false;
        }

        OnResponseDone?.Invoke();
    }

    private void HandleResponseCreated(JObject eventMessage)
    {
        transcriptBuffer.Clear();
        isResponseInProgress = true;
        OnResponseCreated?.Invoke();
    }

    private void HandleError(JObject eventMessage)
    {
        string errorMessage = eventMessage["error"]?["message"]?.ToString();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.Log("openai error: " + errorMessage);
        }
    }

    private bool TryTriggerSurprised(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string normalizedText = NormalizeRecognitionText(text);
        if (ContainsSurpriseKeyword(normalizedText))
        {
            TriggerSurprised();
            return true;
        }

        return false;
    }

    private static string NormalizeRecognitionText(string text)
    {
        return text.Replace(" ", string.Empty)
            .Replace("\u3000", string.Empty)
            .Replace("\u3001", string.Empty)
            .Replace("\u3002", string.Empty)
            .Replace("\uFF01", string.Empty)
            .Replace("\uFF1F", string.Empty)
            .Trim();
    }

    private static bool ContainsSurpriseKeyword(string text)
    {
        return text.IndexOf("\u524d\u5f8c\u77db\u76fe", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("\u77db\u76fe", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void TriggerSurprised()
    {
        if (animationHandler == null)
        {
            animationHandler = FindObjectOfType<AnimationHandler>();
        }

        if (animationHandler == null)
        {
            return;
        }

        animationHandler.SetSurprised(true);

        if (clearSurprisedRoutine != null)
        {
            StopCoroutine(clearSurprisedRoutine);
        }

        clearSurprisedRoutine = StartCoroutine(ClearSurprisedAfterDelay());
    }

    private IEnumerator ClearSurprisedAfterDelay()
    {
        yield return new WaitForSeconds(surprisedDuration);

        animationHandler?.SetSurprised(false);
        clearSurprisedRoutine = null;
    }

    private string BuildResponseInstructions(string turnInstructions)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return turnInstructions;
        }

        if (string.IsNullOrWhiteSpace(turnInstructions))
        {
            return systemPrompt;
        }

        return systemPrompt.Trim() + "\n\n" + turnInstructions.Trim();
    }

    private async void DisposeWebSocket()
    {
        if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived))
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None);
            ws.Dispose();
            ws = null;
            OnWebSocketClosed?.Invoke();
        }
    }

    private void OnDisable()
    {
        if (clearSurprisedRoutine != null)
        {
            StopCoroutine(clearSurprisedRoutine);
            clearSurprisedRoutine = null;
        }

        animationHandler?.SetSurprised(false);
    }
}
