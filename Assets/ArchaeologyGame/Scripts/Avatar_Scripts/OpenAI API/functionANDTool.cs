using System.Threading.Tasks;
using UnityEngine;

public class functionANDTool : MonoBehaviour
{
    [Header("OpenAI")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";
    [SerializeField] private string model = "gpt-4o-mini";
    [SerializeField] private bool logWarnings = true;

    private void Start()
    {
        if (logWarnings)
        {
            Debug.LogWarning(
                "[functionANDTool] OpenAI support is disabled in this project build. " +
                "The component remains as a safe placeholder."
            );
        }
    }

    public Task SendToGPT(string userMsg)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[functionANDTool] SendToGPT(\"{userMsg}\") skipped because the OpenAI package is not installed.");
        }

        return Task.CompletedTask;
    }
}
