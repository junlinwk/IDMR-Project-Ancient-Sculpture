using UnityEngine;
using UnityEngine.Events;

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

    [Header("Debug")]
    [SerializeField] private bool logManager = true;

    // Events for UI and systems to listen to
    public UnityEvent<int> OnOreCountChanged = new UnityEvent<int>();
    public UnityEvent<int> OnUpgradeLevelChanged = new UnityEvent<int>();
    public UnityEvent OnUpgradeFailed = new UnityEvent();
    public UnityEvent OnGameWon = new UnityEvent();

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
        if (statueObject != null)
        {
            statueObject.SetActive(true);
            // Optional: add particles/sound effects here
        }
        OnGameWon.Invoke();
    }
}
