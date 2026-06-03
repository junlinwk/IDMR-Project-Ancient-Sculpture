using UnityEngine;

public class GPTRequest : MonoBehaviour
{
    [Header("OpenAI")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";
    [SerializeField] private string model = "gpt-5-nano";
    [SerializeField] private bool logWarnings = true;

    private void Start()
    {
        if (logWarnings)
        {
            Debug.LogWarning(
                "[GPTRequest] OpenAI support is disabled in this project build. " +
                "The component remains as a safe placeholder."
            );
        }
    }

    public void CreateConversationClient()
    {
        if (logWarnings)
        {
            Debug.LogWarning("[GPTRequest] CreateConversationClient() skipped because the OpenAI package is not installed.");
        }
    }

    public void GetReplyFromChat(string newMsg)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[GPTRequest] GetReplyFromChat(\"{newMsg}\") skipped because the OpenAI package is not installed.");
        }
    }

    public void GetStructuredReplyFromChat(string newMsg)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[GPTRequest] GetStructuredReplyFromChat(\"{newMsg}\") skipped because the OpenAI package is not installed.");
        }
    }
}
