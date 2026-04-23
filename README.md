<div align="center">

# 🎮 VR 考古遊戲

## VR Archaeology Game

![Unity 6000.0.53f1](https://img.shields.io/badge/Unity-6000.0.53f1-lightgrey?logo=unity)
![Meta XR SDK](https://img.shields.io/badge/Meta%20XR%20SDK-85.0.0-000000?logo=meta)
![License MIT](https://img.shields.io/badge/License-MIT-green)
![Version 0.2.0](https://img.shields.io/badge/Version-0.2.0--alpha-blue)

**Meta Quest 3 VR 考古遊戲原型**
*使用 Apple LiDAR 掃描環境，沉浸式挖掘體驗*

---

</div>

## 📋 專案概述

Meta Quest 3 VR 考古遊戲。玩家在沉浸式 MR 場景中：

1. **🔨 敲擊石頭** — 用鐵鎬敲碎包裹雕像的石頭外殼。每顆石頭有兩條擊破路徑：
   - **石頭本體** — 需要多下敲擊
   - **弱點（高亮子物件）** — 較少敲擊即可破
2. **⛏️ 收集鐵礦** — 敲碎石頭掉落鐵礦，噴散落地，短暫靜置後自動吸附拾取
3. **⬆️ 升級鐵鎬** — 在鐵砧前以鐵鎚敲打，**消耗鐵礦**升級工具，減少敲擊次數
4. **🏆 挖出雕像** — 摧毀所有石頭後揭露 Gauss 雕像

**環境資源：** Apple LiDAR 掃描模型（Gauss 雕像 + 周圍環境）

---

## ✨ 功能完成度

| 系統 | 狀態 | 說明 |
|------|:----:|------|
| 工具抓取（OVRGrabbable） | ✅ | 雙手支援、Grip 鍵控制 |
| 鐵鎬敲擊本體 | ✅ | 每級敲擊次數可設 |
| 弱點敲擊系統 | ✅ | 可視化變色、升級同步 |
| 鐵礦掉落 + 散射 | ✅ | 散落範圍、高度、速度可調 |
| 鐵礦自動拾取 | ✅ | 距離閾值 + 寬容期 |
| 掉落防呆（Rescue） | ✅ | 掉出世界 / 卡住自動召回玩家 |
| 鐵礦付費升級 | ✅ | 費用遞增、HUD 顯示剩餘需求 |
| 升級失敗回饋 | ✅ | 音效 + HUD 紅字閃爍 |
| 鐵鎚 Anvil 打擊 | ✅ | 速度+距離判定（繞過 Kinematic 限制）|
| HUD 顯示 | ✅ | 世界空間 UI，跟隨玩家頭部 |
| 手柄震動 | ✅ | Haptics |
| Layer 碰撞隔離 | ✅ | Tools vs PlayerController 分離 |
| 掉物理穩定（位置鎖） | ✅ | LateUpdate 強制回位 |
| Debug log 開關 | ✅ | 每個系統各自可切換 |

---

## 📁 專案結構

```
IDMR-Project-Ancient-Sculpture/
├── Assets/
│   ├── ArchaeologyGame/
│   │   └── Scripts/                    ← 18 個遊戲腳本（見下方清單）
│   ├── Scenes/
│   │   └── ArchaeologyGame.unity       ← 主遊戲場景
│   ├── Prefabs/
│   │   ├── Fluorite.prefab             ← 鐵礦 Prefab（IronOre.cs）
│   │   ├── broke.wav / dig.wav         ← 音效
│   │   └── IDMR_midterm_intro.mp4      ← 片頭影片
│   ├── Materials/
│   │   └── Ironore.mat                 ← 鐵礦材質
│   ├── Models/
│   │   ├── Gauss/                      ← 雕像（LiDAR 掃描）
│   │   ├── 3_18_2026/                  ← 環境掃描
│   │   ├── Fluorite/                   ← 螢石模型（鐵礦視覺）
│   │   ├── hammer/                     ← 鐵鎚
│   │   ├── old-pickaxe/                ← 鐵鎬
│   │   ├── rock/                       ← 石頭
│   │   └── dirty-stones-pile/          ← 石堆
│   ├── Oculus/                         ← Meta SDK 配置
│   └── Settings/                       ← 品質、渲染設定
├── ProjectSettings/
├── Packages/                           ← Meta XR SDK 85.0.0
├── README.md                           ← 本檔案
└── UPGRADE_SYSTEM_SETUP.md             ← 升級系統 Inspector 設定指南
```

---

## 🔧 核心腳本清單

總共 18 個 C# 腳本，按系統分組：

### 🎮 核心遊戲系統
| 腳本 | 職責 |
|------|------|
| `ArchaeologyGameManager.cs` | 中央管理器：礦數、升級等級、鐵礦經濟、勝利判定、事件廣播 |
| `WeaponBase.cs` | 工具基底類：OVRGrabbable 抓取狀態、Haptic 震動、控制器辨識 |
| `Pickaxe.cs` | 鐵鎬 — 碰撞偵測、敲擊反饋、抓取狀態 log |
| `Hammer.cs` | 鐵鎚 — **速度+距離**判定撞擊鐵砧（非碰撞事件） |
| `RockFragment.cs` | 石頭本體 — 敲擊計數、掉礦、破碎流程、位置鎖定 |
| `WeakPointCollisionBox.cs` | 弱點 — Trigger 偵測、顏色指示、升級同步、呼叫本體 `TriggerDestruction()` |
| `AnvilStation.cs` | 鐵砧 — 鐵鎚敲擊計數、礦石檢查、觸發升級 |
| `IronOre.cs` | 鐵礦 — 自動吸附、寬容期、掉出世界救援 |

### 🎨 視覺與回饋
| 腳本 | 職責 |
|------|------|
| `FeedbackManager.cs` | 全域音效 + 粒子 + 震動中樞，含程序化音效生成 |
| `Debries.cs` | 石頭破碎後的散落碎屑 |
| `ScreenShake.cs` | 相機震動 |
| `SparkleGlowField.cs` / `SparkleMovement.cs` | 礦物閃光粒子 |
| `RockLandingFeedback.cs` | 石頭掉落著地回饋 |

### 🖥️ UI 與輔助
| 腳本 | 職責 |
|------|------|
| `OreHUD.cs` | HUD — 礦數、升級等級、升級費用顯示 + 失敗閃爍 |
| `StatueHideOnRightB.cs` | 雕像 debug 隱藏切換 |
| `FullscreenVideoOnLeftY.cs` | 片頭影片切換 |

### 🔌 系統基礎
| 腳本 | 職責 |
|------|------|
| `OvrGrabberBootstrap.cs` | 自動配置 ControllerAnchor 的 OVRGrabber（Grab Volume 0.25m） |

---

## 🎯 核心系統詳解

### 1. 鐵礦經濟與升級系統（ArchaeologyGameManager）

**升級公式：** `cost = baseUpgradeCost + upgradeCostIncrement × upgradeLevel`

預設值：
- `Base Upgrade Cost` = 5
- `Upgrade Cost Increment` = 5
- 於是：Lv0→Lv1 需 5 礦，Lv1→Lv2 需 10 礦，Lv2→Lv3 需 15 礦
- 滿級共需 **30 顆礦**

**公開 API：**
```csharp
bool TrySpendOre(int amount)                // 扣礦，成功回傳 true
bool UpgradePickaxe()                       // 嘗試升級，自動扣礦
int GetCurrentUpgradeCost()                 // 下次升級費用
bool HasEnoughOreForUpgrade()               // 礦數夠不夠
bool IsAtMaxUpgrade()                       // 是否滿級

UnityEvent<int> OnOreCountChanged           // HUD 用
UnityEvent<int> OnUpgradeLevelChanged
UnityEvent OnUpgradeFailed                  // HUD 紅字閃爍
UnityEvent OnGameWon
```

### 2. 石頭雙擊破路徑（RockFragment + WeakPointCollisionBox）

每顆石頭兩條 kill path，任一達成都觸發摧毀：

| 部位 | 腳本 | Inspector 欄位 | 預設每級 |
|------|------|----------------|:--------:|
| 本體 | `RockFragment` | `Hits Per Level` | `[5, 4, 2, 1]` |
| 弱點 | `WeakPointCollisionBox` | `Hits Per Level Weak` | `[3, 2, 1, 1]` |

**升級同步：** `WeakPointCollisionBox` 訂閱 `OnUpgradeLevelChanged`，升級瞬間自動縮減閾值。

**摧毀流程：** 兩條路徑最終都呼叫 `RockFragment.TriggerDestruction()`，共用掉礦 / 升級計數邏輯。

### 3. 鐵礦掉落與拾取（RockFragment.SpawnOre + IronOre）

**噴散參數（RockFragment）：**
| 欄位 | 預設 | 說明 |
|------|:---:|------|
| `Ore Drop Count` | 3 | 每顆石頭掉幾顆礦 |
| `Ore Spawn Height Offset` | 2.0m | 石頭上方 N 公尺生成（避免鑽地板） |
| `Ore Upward Burst` | 1.5 m/s | 垂直彈射速度 |
| `Ore Scatter Speed` | 1.2 m/s | 水平隨機方向散射 |
| `Ore Spawn Jitter` | 0.15m | 隨機位置偏移（避免疊在一起） |

**物理保險：** 礦石生成時自動 `Physics.IgnoreCollision` 與源石頭，避免卡 Collider 亂飛。

**拾取流程（IronOre）：**
```
生成 → 噴散 → 落地滾一下（寬容期 4 秒，不吸附）
      ↓
寬容期結束 → 距離玩家 < 1m → 飛向玩家
      ↓
距離 < 0.3m → 自動拾取 → AddOre(+1)
```

**防呆機制：**
- `Fall Threshold` = -3m → 掉出世界自動飛向玩家
- `Rescue After Seconds` = 8s → 卡住太久強制召回

### 4. 鐵鎚擊打 Anvil（Hammer）

因 OVRGrabbable 抓取時 Rigidbody 變 Kinematic，`OnCollisionEnter` 不觸發。改用**速度 + 距離**主動判定：

```csharp
每幀檢查：
  IsHeld                         → 必須被握著
  距離 anvil < strikeRadius      → 預設 0.5m
  手部速度 > minStrikeSpeed      → 預設 0.1 m/s（簡易模擬器友善）
  上次擊打 > strikeCooldown      → 預設 0.3 秒
  ↓
呼叫 AnvilStation.Hit()
```

**Inspector 可調：**
| 欄位 | 預設 | 說明 |
|------|:---:|------|
| `Strike Radius` | 1.0m | 判定距離，寬鬆設定 |
| `Min Strike Speed` | 0.05 m/s | 最低揮動速度 |
| `Strike Cooldown` | 0.3s | 連擊冷卻 |

### 5. Anvil 升級互動（AnvilStation）

敲打流程：
```
Hammer.Hit() 呼叫 anvil.Hit()
  ↓
檢查：是否已滿級？ → 是 → 播放 Fail 音效，返回
  ↓
檢查：鐵礦夠不夠？ → 不夠 → 播放 Fail 音效 + OnUpgradeFailed（HUD 紅字閃）
  ↓
hammerHits++ → 到達 hitsToUpgrade（預設 5）
  ↓
呼叫 gameManager.UpgradePickaxe()
  ↓
TrySpendOre(cost) → 成功 → upgradeLevel++ → 廣播所有 RockFragment
```

### 6. HUD 系統（OreHUD）

**World Space Canvas，parent 到 `CenterEyeAnchor`**，永遠在玩家視線前方 2m。

**三行文字：**
```
Iron Ore: 12
Upgrade Level: 1
Next Upgrade: 5/10 Ore
```

**升級失敗時：** `UpgradeCostText` 紅字閃爍 0.6 秒。

### 7. Layer 隔離系統

解決 PlayerController 的 Capsule Collider 把工具/礦石撞飛的問題。

**Layer 配置：**
- `Tools`（User Layer 6）：Pickaxe、Hammer、PickaxeHead、HammerHead、Fluorite
- `Default`：石頭、Anvil、地板
- `Water`：PlayerController 所在 Layer

**關鍵設定：** PlayerController 的 **Capsule Collider** → `Layer Overrides` → `Exclude Layers = Tools`。這樣 Capsule 不和 Tools 碰撞，但 OVRGrabber 的 Grab Volume Trigger 仍能偵測工具（Layer Collision Matrix 保持 Tools × Water = ✅）。

### 8. 位置鎖（RockFragment.LateUpdate）

石頭每幀自動回彈到出生位置：

```csharp
LateUpdate:
  if transform.position != lockedPosition:
    transform.position = lockedPosition       ← 強制拉回
    rb.linearVelocity = Vector3.zero
```

**目的：** 即使有其他系統（或物理 bug）試圖推動石頭，`LateUpdate` 會立刻拉回。敲到 5/5 呼叫 `TriggerDestruction()` 時才解鎖。

---

## 🎛️ Inspector 參數速查表

### 核心玩法平衡

| 物件 | 元件 | 欄位 | 預設 | 建議範圍 |
|------|------|-------|:----:|:--------:|
| ArchaeologyGameManager | ArchaeologyGameManager | `Base Upgrade Cost` | 5 | 3-10 |
| ArchaeologyGameManager | ArchaeologyGameManager | `Upgrade Cost Increment` | 5 | 0-10 |
| ArchaeologyGameManager | ArchaeologyGameManager | `Max Upgrade Level` | 3 | 2-5 |
| 每顆 Rock | RockFragment | `Hits Per Level` | `[5,4,2,1]` | 自訂 |
| 每顆 Rock | RockFragment | `Ore Drop Count` | 3 | 1-10 |
| WeakPoint（每顆石頭底下） | WeakPointCollisionBox | `Hits Per Level Weak` | `[3,2,1,1]` | 自訂 |
| Anvil | AnvilStation | `Hits To Upgrade` | 5 | 3-10 |
| Pickaxe | Pickaxe | `Pickaxe Haptic Amplitude` | 0.7 | 0.3-1.0 |
| Hammer | Hammer | `Strike Radius` | 1.0 | 0.3-1.5 |
| Hammer | Hammer | `Min Strike Speed` | 0.05 | 0.05-2.0 |
| Fluorite Prefab | IronOre | `Pickup Range` | 1.0 | 0.5-2.0 |
| Fluorite Prefab | IronOre | `Pickup Delay` | 4s | 1-8s |
| Fluorite Prefab | IronOre | `Rescue After Seconds` | 8s | 5-20s |

### Debug Log 開關

所有腳本的 log 都可在 Inspector 關閉：
- `ArchaeologyGameManager.Log Manager`
- `AnvilStation.Log Anvil`
- `RockFragment.Log Rock Hits`
- `IronOre.Log Pickup`
- `Pickaxe.Log Pickaxe Hits`
- `Hammer.Log Hammer`
- `WeakPointCollisionBox.Log Weak Point Hits`

---

## 🎮 遊戲流程

```
遊戲開始
  ↓
玩家抓鐵鎬（按住 Grip 鍵）
  ↓
敲碎石頭（本體 × 5 或 弱點 × 3）
  ├─ 播放擊中粒子 + 音效 + 震動
  └─ hitCount 達到閾值 → TriggerDestruction()
  ↓
摧毀石頭 → 噴出 N 顆鐵礦（上方 2m 空中噴散）
  ↓
鐵礦落地 → 在地上躺 4 秒（可見）
  ↓
玩家走近 < 1m → 礦石自動飛向玩家（< 0.3m 拾取）
  ↓
AddOre → HUD 更新 → [GameManager] AddOre(+1) -> total=X
  ↓
（收集夠 5 顆後）玩家抓鐵鎚 → 走到 Anvil
  ↓
揮動鐵鎚距離 < 1m、速度 > 0.05 m/s → 觸發 Hit()
  ├─ 礦數不夠：播放 Fail 音效 + HUD 紅字閃爍
  └─ 礦數夠：hammerHits++ → 到 5/5 → UpgradePickaxe()
  ↓
扣 5 礦 → upgradeLevel = 1 → 廣播所有石頭
  ↓
升級後敲石頭：本體 5→4 下、弱點 3→2 下
  ↓
重複挖礦 → 升到 Lv3（扣 5+10+15=30 礦）
  ↓
摧毀所有石頭 → Gauss_Statue.SetActive(true) → OnGameWon
```

---

## 🐛 常見問題與排除

### ❌ 鐵鎬抓了就秒放

**原因：** PlayerController 的 Capsule Collider 與鐵鎬碰撞衝突。
**解法：** 確認鐵鎬 Layer = Tools、PlayerController 的 Capsule Collider 的 `Layer Overrides → Exclude Layers` 包含 Tools。

### ❌ 敲石頭沒反應

**檢查：**
1. Pickaxe GameObject Tag = `Pickaxe`
2. PickaxeHead 子物件有 Collider 且 Layer = Tools
3. RockFragment Inspector 的 `Hits Per Level` 陣列有值
4. Layer Collision Matrix 的 Tools × Default = ✅

### ❌ 敲碎石頭但沒掉礦

**檢查：**
1. RockFragment Inspector 的 `Iron Ore Prefab` 欄位有拖入 `Fluorite.prefab`
2. Fluorite Prefab 有掛 `IronOre.cs`
3. Fluorite Prefab 有 Collider（非 Trigger）

### ❌ 礦石看不到 / 瞬間消失

**原因：** 礦石掉地板下（穿透），或被太快吸附。
**解法：**
- 調高 `Ore Spawn Height Offset`（如 3.0）
- 調長 `Pickup Delay`（如 5-6 秒）
- Console 看有沒有 `RESCUE triggered` 日誌（代表穿地板被救回）

### ❌ 鐵鎚敲 Anvil 沒反應

**原因：** 新版 Hammer.cs 用速度+距離判定，參數太嚴。
**解法：**
- `Strike Radius` = 1.0
- `Min Strike Speed` = 0.05
- 確認 `anvil` GameObject 有掛 `AnvilStation.cs`

### ❌ HUD 不顯示

**檢查：**
1. Canvas Render Mode = **World Space**（不是 Screen Space）
2. Canvas Scale = `(0.005, 0.005, 0.005)`
3. Canvas parent 到 `OVRCameraRig/TrackingSpace/CenterEyeAnchor`
4. 每個 TextMeshPro 都指派 `LiberationSans SDF` Font Asset
5. OreHUD 的三個 Text 欄位都拖入對應 TMP

### ❌ 石頭進場全飛走

**原因：** 石頭 Collider 互相重疊，物理互相推擠。
**解法：** `RockFragment.Awake()` 自動設 `FreezeAll + LateUpdate` 位置鎖（已內建），應該不會發生。若仍發生，檢查 Rigidbody 設定是否被手動改過。

---

## 🔌 相依性與環境

### Unity 版本
- **Unity 6000.0.53f1** (Unity 6)

### 主要套件
- **Meta XR SDK 85.0.0** — OVRGrabbable、OVRGrabber、OVRCameraRig、Haptics
- **TextMeshPro** — HUD 文字
- **OpenXR** — XR 運行時

### 目標平台
- **Meta Quest 3**（主要目標）
- Meta XR Simulator（編輯器測試）

### 複用外部
- `WeaponBase.cs` — 繼承自 `Ref/Space Invader MR 3` 專案的武器基底類

---

## 📝 修改程式碼的最佳實踐

### 添加新功能時

**Singleton 互動：**
```csharp
// ✅ 推薦：事件系統
ArchaeologyGameManager.Instance.OnOreCountChanged.AddListener(MyHandler);

// ❌ 避免：緊耦合
UpdateUI(ArchaeologyGameManager.Instance.GetOreCount());
```

**新增配置參數：**
```csharp
// ✅ 使用 SerializeField 暴露給 Inspector
[SerializeField] private int myConfig = 5;

// ❌ 不要硬編碼
private int myConfig = 5;
```

**擴展 RockFragment / WeakPointCollisionBox：**
兩個腳本都通過 `RockFragment.TriggerDestruction()` 統一進入破壞流程。新增第三種摧毀方式時，同樣呼叫這個 public 方法。

---

## 📖 相關文件

| 文件 | 內容 |
|------|------|
| `README.md` | 本檔案 — 系統概覽 |
| `UPGRADE_SYSTEM_SETUP.md` | Inspector 設定、測試流程、Debug log 指南 |

---

<div align="center">

**版本：** 0.2.0-alpha
**最後更新：** 2026-04-23
**授權：** MIT

---

**準備好了嗎？** 打開 Unity 進入 `Assets/Scenes/ArchaeologyGame.unity` 開始挖礦！

</div>
