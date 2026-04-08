using UnityEngine;
using TMPro;

/// <summary>
/// Ore HUD display.
/// Subscribes to GameManager ore count events and updates UI text.
/// </summary>
public class OreHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI oreCountText;
    [SerializeField] private TextMeshProUGUI upgradeLevelText;

    private void Start()
    {
        if (ArchaeologyGameManager.Instance != null)
        {
            // Subscribe to ore count changes
            ArchaeologyGameManager.Instance.OnOreCountChanged.AddListener(UpdateOreCount);
            ArchaeologyGameManager.Instance.OnUpgradeLevelChanged.AddListener(UpdateUpgradeLevel);

            // Initialize display
            UpdateOreCount(0);
            UpdateUpgradeLevel(0);
        }
    }

    private void OnDestroy()
    {
        if (ArchaeologyGameManager.Instance != null)
        {
            ArchaeologyGameManager.Instance.OnOreCountChanged.RemoveListener(UpdateOreCount);
            ArchaeologyGameManager.Instance.OnUpgradeLevelChanged.RemoveListener(UpdateUpgradeLevel);
        }
    }

    private void UpdateOreCount(int count)
    {
        if (oreCountText != null)
        {
            oreCountText.text = $"Iron Ore: {count}";
        }
    }

    private void UpdateUpgradeLevel(int level)
    {
        if (upgradeLevelText != null)
        {
            upgradeLevelText.text = $"Upgrade Level: {level}";
        }
    }
}
