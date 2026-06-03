using UnityEngine;

public class teststructure : MonoBehaviour
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
                "[teststructure] OpenAI support is disabled in this project build. " +
                "The component remains as a safe placeholder."
            );
        }
    }

    public void RunTest()
    {
        if (logWarnings)
        {
            Debug.LogWarning("[teststructure] RunTest() skipped because the OpenAI package is not installed.");
        }
    }
}
