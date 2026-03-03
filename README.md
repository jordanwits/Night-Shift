# Night Shift: Mall Security

Phase 1 playable prototype. Unity 6.3 LTS, URP, Windows Steam target.

## Quick Start

1. Open `Assets/_Project/Scenes/Bootstrap.unity`
2. Add the scene to Build Settings (File > Build Settings > Add Open Scenes) if needed
3. Ensure the scene has a Main Camera
4. Run **Night Shift > Create Default Anomalies** (creates 5 anomaly definitions in `Resources/Anomalies`)
5. Press Play

GameBootstrap auto-creates managers if missing. Player is a capsule with WASD + mouse. Press **E** to fix anomalies in range.

---

## Folder Structure

```
Assets/_Project/
  Art/
    Materials/
    Models/
    Prefabs/
      MallSections/
      Anomalies/
      Props/
  Audio/
  Scenes/
  ScriptableObjects/
    Anomalies/
    Systems/
  Scripts/
    Core/          # Events, interfaces, data definitions
    Systems/       # GameState, Instability, Anomaly, Time
    Generation/    # Mall generator
    Player/
    UI/
    Debug/
    Utils/
    Bootstrap/     # GameBootstrap (no asmdef)
  Settings/
  Tests/
  Resources/
    Anomalies/     # AnomalyDefinition assets (auto-loaded)
  Editor/          # Night Shift menu tools
```

No scripts outside `_Project/Scripts`. No prefabs outside `_Project/Art/Prefabs`.

---

## How to Add New Anomalies

1. Create asset: Right-click in Project → **Create > Night Shift > Anomaly Definition**
2. Set:
   - `id` (unique, e.g. `anomaly_vending_machine`)
   - `displayName`, `description`
   - `severity` (1–5)
   - `baseInstabilityPenalty` (when unfixed / failed fix)
   - `rewardValue` (instability reduced on correct fix)
   - `spawnRules` (min/max instability, maxConcurrent, minCooldownSeconds)
   - `fixMethod` (Interact or MultiChoice)
   - `anomalyPrefab` (optional; if null, a placeholder cube is spawned)
3. Put in `Assets/_Project/Resources/Anomalies/` to auto-load, or assign to AnomalyManager in the scene.

---

## How to Add New Mall Sections

1. Create asset: Right-click → **Create > Mall Section Data** (when available) or duplicate existing
2. Set `sectionId`, `displayName`, `sectionType` (Corridor/Store/Plaza)
3. Assign a section prefab (low-poly, flat-shaded)
4. Add to MallGenerator's `Section Types` array in the scene

If no section types are assigned, the generator spawns placeholder cubes (Corridor/Store/Plaza).

---

## How to Test Instability Thresholds

**Tiers:**
- 0: Stable
- 1 (≥30%): Light Flicker
- 2 (≥60%): Radio Distortion
- 3 (≥80%): Hallucination

**Debug tools (F2 toggles panel):**
- **Instability slider**: Set 0–100 directly
- **Spawn Anomaly**: Add anomaly (increases instability when unfixed)
- **Quick Restart (F5)**: Reload scene

**Debug overlay (F1 toggles):**
- Current time
- Instability %
- Active anomaly count
- Current tier name
- Mall seed

---

## Assembly Definitions

| Assembly      | Folder   | Purpose                    |
|--------------|----------|----------------------------|
| NightShift.Core      | Scripts/Core      | Events, interfaces, SOs   |
| NightShift.Systems  | Scripts/Systems  | Managers                  |
| NightShift.Generation | Scripts/Generation | Mall generation        |
| NightShift.Player   | Scripts/Player   | Player controls           |
| NightShift.UI      | Scripts/UI       | End screen, etc.          |
| NightShift.Debug   | Scripts/Debug    | Overlay, tools            |
| NightShift.Editor  | Editor/           | Menu tools                |

No circular dependencies. Event-driven communication.
