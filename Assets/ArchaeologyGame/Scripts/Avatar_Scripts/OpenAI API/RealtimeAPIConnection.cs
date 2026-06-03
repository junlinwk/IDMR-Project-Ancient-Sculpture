using UnityEngine;

public class RealtimeAPIConnection : MonoBehaviour
{
    public static RealtimeAPIConnection instance;
    [HideInInspector] public bool isConnected = false;

    [Header("Input")]
    [SerializeField] private bool useEditorKeyFallback = true;
    [SerializeField] private KeyCode editorToggleKey = KeyCode.Y;
    [SerializeField] private bool logConnection = true;

    public string connectionStatus => isConnected ? "Connected" : "Disconnected";
    public string connectionButtonString => isConnected ? "Disconnect" : "Connect";

    private RealtimeAPIWrapper realtimeWrapper;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        realtimeWrapper = GetComponent<RealtimeAPIWrapper>();
        if (realtimeWrapper == null)
        {
            realtimeWrapper = GetComponentInChildren<RealtimeAPIWrapper>();
        }

        if (realtimeWrapper == null)
        {
            Debug.LogError("[RealtimeAPIConnection] No RealtimeAPIWrapper found on this object or its children.");
        }
    }

    void Update()
    {
        if (!TogglePressed())
        {
            return;
        }

        if (realtimeWrapper == null)
        {
            Debug.LogError("[RealtimeAPIConnection] Toggle pressed, but no RealtimeAPIWrapper is assigned.");
            return;
        }

        isConnected = !isConnected;

        if (logConnection)
        {
            Debug.Log($"[RealtimeAPIConnection] Toggle pressed. Requested state: {(isConnected ? "connect" : "disconnect")}.");
        }

        realtimeWrapper.ConnectWebSocketButton();
    }

    private bool TogglePressed()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            return true;
        }

        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            return true;
        }

        if (useEditorKeyFallback && Application.isEditor && Input.GetKeyDown(editorToggleKey))
        {
            return true;
        }

        return false;
    }

    public void sceneGUIButtonPressed()
    {
        if (realtimeWrapper == null)
        {
            Debug.LogError("[RealtimeAPIConnection] Scene GUI button pressed, but no RealtimeAPIWrapper is assigned.");
            return;
        }

        isConnected = !isConnected;
        if (logConnection)
        {
            Debug.Log($"[RealtimeAPIConnection] Scene GUI button pressed. Requested state: {(isConnected ? "connect" : "disconnect")}.");
        }

        realtimeWrapper.ConnectWebSocketButton();
    }
}
