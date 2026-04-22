using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Ore HUD display.
/// Subscribes to GameManager ore count events and updates UI text.
/// Also shows the current upgrade cost and flashes a warning on failed upgrades.
/// </summary>
public class OreHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI oreCountText;
    [SerializeField] private TextMeshProUGUI upgradeLevelText;
    [Tooltip("Optional: shows the ore cost for the next upgrade.")]
    [SerializeField] private TextMeshProUGUI upgradeCostText;

    [Header("Failure Flash")]
    [SerializeField] private Color failFlashColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private float failFlashDuration = 0.6f;

    private Color upgradeCostDefaultColor;
    private Coroutine flashRoutine;

    private void Start()
    {
        if (upgradeCostText != null)
        {
            upgradeCostDefaultColor = upgradeCostText.color;
        }

        ArchaeologyGameManager manager = ArchaeologyGameManager.Instance;
        if (manager != null)
        {
            manager.OnOreCountChanged.AddListener(HandleOreCountChanged);
            manager.OnUpgradeLevelChanged.AddListener(HandleUpgradeLevelChanged);
            manager.OnUpgradeFailed.AddListener(HandleUpgradeFailed);

            // Initialize display with current manager state.
            HandleOreCountChanged(manager.GetOreCount());
            HandleUpgradeLevelChanged(manager.GetUpgradeLevel());
        }
    }

    private void OnDestroy()
    {
        ArchaeologyGameManager manager = ArchaeologyGameManager.Instance;
        if (manager != null)
        {
            manager.OnOreCountChanged.RemoveListener(HandleOreCountChanged);
            manager.OnUpgradeLevelChanged.RemoveListener(HandleUpgradeLevelChanged);
            manager.OnUpgradeFailed.RemoveListener(HandleUpgradeFailed);
        }
    }

    private void HandleOreCountChanged(int count)
    {
        if (oreCountText != null)
        {
            oreCountText.text = $"Iron Ore: {count}";
        }
        RefreshUpgradeCostText();
    }

    private void HandleUpgradeLevelChanged(int level)
    {
        if (upgradeLevelText != null)
        {
            upgradeLevelText.text = $"Upgrade Level: {level}";
        }
        RefreshUpgradeCostText();
    }

    private void RefreshUpgradeCostText()
    {
        if (upgradeCostText == null)
        {
            return;
        }

        ArchaeologyGameManager manager = ArchaeologyGameManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (manager.IsAtMaxUpgrade())
        {
            upgradeCostText.text = "Pickaxe: MAX";
            return;
        }

        int cost = manager.GetCurrentUpgradeCost();
        int owned = manager.GetOreCount();
        upgradeCostText.text = $"Next Upgrade: {owned}/{cost} Ore";
    }

    private void HandleUpgradeFailed()
    {
        if (upgradeCostText == null)
        {
            return;
        }
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashFailCoroutine());
    }

    private IEnumerator FlashFailCoroutine()
    {
        upgradeCostText.color = failFlashColor;
        yield return new WaitForSeconds(failFlashDuration);
        upgradeCostText.color = upgradeCostDefaultColor;
        flashRoutine = null;
    }
}
