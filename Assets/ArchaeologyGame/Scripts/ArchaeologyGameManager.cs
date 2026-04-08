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

    // Events for UI and systems to listen to
    public UnityEvent<int> OnOreCountChanged = new UnityEvent<int>();
    public UnityEvent<int> OnUpgradeLevelChanged = new UnityEvent<int>();
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
        OnOreCountChanged.Invoke(oreCount);
    }

    public void RegisterFragmentDestroyed()
    {
        destroyedFragments++;
        if (destroyedFragments >= totalRockFragments)
        {
            WinGame();
        }
    }

    public void UpgradePickaxe()
    {
        if (upgradeLevel < maxUpgradeLevel)
        {
            upgradeLevel++;
            OnUpgradeLevelChanged.Invoke(upgradeLevel);

            // Notify all rock fragments about the upgrade
            RockFragment[] fragments = FindObjectsOfType<RockFragment>();
            foreach (RockFragment fragment in fragments)
            {
                fragment.OnUpgrade(upgradeLevel);
            }
        }
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
