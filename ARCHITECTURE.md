# Architecture Rules

## Folder Rules
All custom content lives under `Assets/_Project/`.
No scripts outside `Assets/_Project/Scripts/`.

## Principles
- ScriptableObject-driven data.
- Small components, no God objects.
- Systems communicate via events.
- Deterministic seed support for generation.
- Debug overlay required for all core state.

## Build Order
1) Folder structure + asmdefs
2) Core bootstrap + game state
3) Instability manager
4) Mall generator (simple)
5) Anomaly framework
6) Basic player interaction + UI