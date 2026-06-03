using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class ArchaeologyGameManager : MonoBehaviour
{
    public static ArchaeologyGameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private int oreCount = 0;
    [SerializeField] private int upgradeLevel = 0; // 0 = base, 1-3 = upgrades
    [SerializeField] private int totalRockFragments = 0;
    [SerializeField] private int destroyedFragments = 0;

    [Header("References")]
    [SerializeField] private GameObject statueObject; // Gauss, to reveal on win
    [SerializeField] private int maxUpgradeLevel = 3;

    [Header("Upgrade Economy")]
    [Tooltip("Iron ore cost for the first upgrade (Lv0 -> Lv1).")]
    [SerializeField] private int baseUpgradeCost = 5;
    [Tooltip("Extra ore required per subsequent upgrade. Cost = base + (level * increment).")]
    [SerializeField] private int upgradeCostIncrement = 5;

    [Header("Victory UI")]
    [SerializeField] private bool autoCreateVictoryUI = true;
    [SerializeField] private string victoryMessage = "Congratulations!";
    [SerializeField] private Color victoryTextColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private float victoryTextSize = 72f;
    [SerializeField] private Vector2 victoryCanvasSize = new Vector2(1200f, 300f);
    [SerializeField] private Vector3 victoryLocalOffset = new Vector3(0f, 0f, 2.1f);
    [SerializeField] private float victoryCanvasScale = 0.01f;
    [SerializeField] private float victoryFadeDuration = 0.45f;
    [SerializeField] private float victoryPulseAmplitude = 0.06f;
    [SerializeField] private float victoryPulseSpeed = 3.5f;
    [SerializeField] private float glowOffsetX = 320f;
    [SerializeField] private float glowParticleSize = 14f;
    [SerializeField] private float glowParticleRate = 32f;

    [Header("Debug")]
    [SerializeField] private bool logManager = true;

    // Events for UI and systems to listen to
    public UnityEvent<int> OnOreCountChanged = new UnityEvent<int>();
    public UnityEvent<int> OnUpgradeLevelChanged = new UnityEvent<int>();
    public UnityEvent OnUpgradeFailed = new UnityEvent();
    public UnityEvent OnGameWon = new UnityEvent();

    private bool hasWon;
    private GameObject victoryCanvasObject;
    private CanvasGroup victoryCanvasGroup;
    private RectTransform victoryTextRect;
    private TextMeshProUGUI victoryText;
    private ParticleSystem victoryGlowParticles;
    private Vector3 victoryTextBaseScale;
    private Coroutine victoryRevealRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Count total rock fragments in scene on startup
        RockFragment[] fragments = FindObjectsOfType<RockFragment>();
        totalRockFragments = fragments.Length;

        // Register all fragments with this manager
        foreach (RockFragment fragment in fragments)
        {
            fragment.Initialize(this);
        }

        if (statueObject != null)
        {
            statueObject.SetActive(false);
        }

        if (autoCreateVictoryUI)
        {
            BuildVictoryUI();
        }
    }

    public void AddOre(int amount = 1)
    {
        oreCount += amount;
        if (logManager) Debug.Log($"[GameManager] AddOre(+{amount}) -> total={oreCount}");
        OnOreCountChanged.Invoke(oreCount);
    }

    public int GetOreCount()
    {
        return oreCount;
    }

    public bool IsAtMaxUpgrade()
    {
        return upgradeLevel >= maxUpgradeLevel;
    }

    /// <summary>
    /// Ore cost for the *next* upgrade. Returns int.MaxValue if already maxed.
    /// </summary>
    public int GetCurrentUpgradeCost()
    {
        if (IsAtMaxUpgrade())
        {
            return int.MaxValue;
        }
        return baseUpgradeCost + upgradeCostIncrement * upgradeLevel;
    }

    public bool HasEnoughOreForUpgrade()
    {
        return !IsAtMaxUpgrade() && oreCount >= GetCurrentUpgradeCost();
    }

    /// <summary>
    /// Deduct ore if the player has enough. Returns true on success.
    /// </summary>
    public bool TrySpendOre(int amount)
    {
        if (amount <= 0 || oreCount < amount)
        {
            if (logManager) Debug.Log($"[GameManager] TrySpendOre({amount}) FAILED — have {oreCount}");
            return false;
        }
        oreCount -= amount;
        if (logManager) Debug.Log($"[GameManager] TrySpendOre({amount}) OK — remaining={oreCount}");
        OnOreCountChanged.Invoke(oreCount);
        return true;
    }

    public void RegisterFragmentDestroyed()
    {
        if (hasWon)
        {
            return;
        }

        destroyedFragments++;
        if (destroyedFragments >= totalRockFragments)
        {
            WinGame();
        }
    }

    /// <summary>
    /// Attempt to upgrade the pickaxe. Charges ore based on current level.
    /// Returns true on success; false if maxed out or not enough ore.
    /// </summary>
    public bool UpgradePickaxe()
    {
        if (IsAtMaxUpgrade())
        {
            if (logManager) Debug.Log($"[GameManager] UpgradePickaxe FAILED — already at max level {upgradeLevel}/{maxUpgradeLevel}");
            OnUpgradeFailed.Invoke();
            return false;
        }

        int cost = GetCurrentUpgradeCost();
        if (!TrySpendOre(cost))
        {
            if (logManager) Debug.Log($"[GameManager] UpgradePickaxe FAILED — could not spend {cost} ore");
            OnUpgradeFailed.Invoke();
            return false;
        }

        upgradeLevel++;
        if (logManager) Debug.Log($"[GameManager] UpgradePickaxe OK — now level {upgradeLevel}, next cost={GetCurrentUpgradeCost()}");
        OnUpgradeLevelChanged.Invoke(upgradeLevel);

        // Notify all rock fragments about the upgrade
        RockFragment[] fragments = FindObjectsOfType<RockFragment>();
        foreach (RockFragment fragment in fragments)
        {
            fragment.OnUpgrade(upgradeLevel);
        }
        return true;
    }

    public int GetUpgradeLevel()
    {
        return upgradeLevel;
    }

    private void WinGame()
    {
        if (hasWon)
        {
            return;
        }

        hasWon = true;

        if (statueObject != null)
        {
            statueObject.SetActive(true);
            // Optional: add particles/sound effects here
        }

        ShowVictoryBanner();
        OnGameWon.Invoke();
    }

    private void BuildVictoryUI()
    {
        if (victoryCanvasObject != null)
        {
            return;
        }

        Transform anchor = GetVictoryAnchor();
        victoryCanvasObject = new GameObject(
            "VictoryCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        victoryCanvasObject.transform.SetParent(anchor, false);
        victoryCanvasObject.transform.localPosition = victoryLocalOffset;
        victoryCanvasObject.transform.localRotation = Quaternion.identity;
        victoryCanvasObject.transform.localScale = Vector3.one * victoryCanvasScale;

        Canvas canvas = victoryCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = FindMainCamera();
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = victoryCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        victoryCanvasGroup = victoryCanvasObject.GetComponent<CanvasGroup>();
        victoryCanvasGroup.alpha = 0f;
        victoryCanvasGroup.interactable = false;
        victoryCanvasGroup.blocksRaycasts = false;

        RectTransform canvasRect = victoryCanvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = victoryCanvasSize;

        GameObject textObject = new GameObject(
            "CongratulationsText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(victoryCanvasObject.transform, false);

        victoryTextRect = textObject.GetComponent<RectTransform>();
        victoryTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        victoryTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        victoryTextRect.pivot = new Vector2(0.5f, 0.5f);
        victoryTextRect.anchoredPosition = Vector2.zero;
        victoryTextRect.sizeDelta = new Vector2(1000f, 220f);

        victoryText = textObject.GetComponent<TextMeshProUGUI>();
        victoryText.text = victoryMessage;
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (fontAsset != null)
        {
            victoryText.font = fontAsset;
        }
        victoryText.fontSize = victoryTextSize;
        victoryText.color = victoryTextColor;
        victoryText.alignment = TextAlignmentOptions.Center;
        victoryText.enableWordWrapping = false;
        victoryText.fontStyle = FontStyles.Bold;
        victoryText.raycastTarget = false;
        victoryTextBaseScale = Vector3.one;
        victoryTextRect.localScale = victoryTextBaseScale;

        GameObject glowObject = new GameObject("VictoryGlow", typeof(ParticleSystem));
        glowObject.transform.SetParent(victoryCanvasObject.transform, false);
        glowObject.transform.localPosition = new Vector3(glowOffsetX, 0f, 0f);
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one;

        victoryGlowParticles = glowObject.GetComponent<ParticleSystem>();
        ConfigureVictoryParticles(victoryGlowParticles);

        // Keep the victory UI fully disabled until the win condition is met.
        victoryCanvasObject.SetActive(false);

        if (logManager)
        {
            Debug.Log("[GameManager] Victory UI created.");
        }
    }

    private void ShowVictoryBanner()
    {
        if (victoryCanvasObject == null)
        {
            BuildVictoryUI();
        }

        if (victoryCanvasObject == null)
        {
            return;
        }

        if (victoryRevealRoutine != null)
        {
            StopCoroutine(victoryRevealRoutine);
        }

        victoryCanvasGroup.alpha = 0f;
        victoryCanvasObject.transform.localScale = Vector3.one * victoryCanvasScale;
        victoryCanvasObject.SetActive(true);
        victoryRevealRoutine = StartCoroutine(VictoryRevealRoutine());
    }

    private System.Collections.IEnumerator VictoryRevealRoutine()
    {
        if (victoryGlowParticles != null)
        {
            victoryGlowParticles.Play(true);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, victoryFadeDuration);
        Vector3 baseScale = victoryTextBaseScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (victoryCanvasGroup != null)
            {
                victoryCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            }

            if (victoryTextRect != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * victoryPulseSpeed) * victoryPulseAmplitude;
                victoryTextRect.localScale = baseScale * Mathf.Lerp(0.92f, pulse, t);
            }

            yield return null;
        }

        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.alpha = 1f;
        }

        if (victoryTextRect != null)
        {
            victoryTextRect.localScale = baseScale * (1f + victoryPulseAmplitude);
        }

        victoryRevealRoutine = null;
    }

    private void ConfigureVictoryParticles(ParticleSystem particles)
    {
        if (particles == null)
        {
            return;
        }

        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(glowParticleSize * 0.6f, glowParticleSize * 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.96f, 0.55f, 0.92f),
            new Color(1f, 0.82f, 0.2f, 0.75f));
        main.maxParticles = 200;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = glowParticleRate;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)16, (short)24)
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 18f;
        shape.radiusThickness = 1f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.15f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.15f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                renderer.material = new Material(shader)
                {
                    color = Color.white
                };
            }
        }
    }

    private Transform GetVictoryAnchor()
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            return rig.centerEyeAnchor;
        }

        Camera cam = FindMainCamera();
        if (cam != null)
        {
            return cam.transform;
        }

        return transform;
    }

    private Camera FindMainCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam;
        }

        return FindFirstObjectByType<Camera>();
    }
}
