# Mulligan Madness

ROUNDS mod for r2modman / Thunderstore.

## Features

### Take All
- Toggle in **Mods → Mulligan Madness**
- During your card pick, a **TAKE ALL** button appears
- Labeled as **once per game**
- Each player can use it once per match (not per round)

### Auto-pick curses (WillsWackyManagers)
Mutually exclusive (sharing one blocks the others):

| Curse | Effect |
| --- | --- |
| **Forced Choice** | Immediately takes a random offered card on your pick |
| **Panic Pick** | Gives you a short timer, then auto-picks at random |
| **Leftmost Luck** | Always takes the leftmost card |

## Dependencies
- BepInExPack_ROUNDS
- UnboundLib
- ModdingUtils
- CardChoiceSpawnUniqueCardPatch
- WillsWackyManagers
- MMHook (usually pulled in by UnboundLib)

## Build / install (local)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build MulliganMadness/MulliganMadness.csproj -c Release
```

The build copies `MulliganMadness.dll` into your r2modman Default profile:

`.../ROUNDS/profiles/Default/BepInEx/plugins/local-MulliganMadness/`

Then launch ROUNDS via r2modman (**Start modded**).
