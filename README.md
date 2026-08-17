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

## Install
Install from Thunderstore with r2modman / Thunderstore Mod Manager once published, or drop `package/MulliganMadness.dll` into your profile `BepInEx/plugins/` folder.

## Dependencies
- BepInExPack_ROUNDS
- UnboundLib
- ModdingUtils
- CardChoiceSpawnUniqueCardPatch
- WillsWackyManagers
- MMHook (usually pulled in by UnboundLib)

## Build (local)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build MulliganMadness/MulliganMadness.csproj -c Release
```

The build copies the DLL into `package/` (for Thunderstore) and your r2modman Default profile plugins folder.

## Publish to Thunderstore

1. Ensure `thunderstore.toml` `namespace` matches your Thunderstore **team name**
2. Repo secret: `THUNDERSTORE_API_KEY` (service account token)
3. Bump `versionNumber` in `thunderstore.toml` (and `manifest.json` if you keep them aligned)
4. Commit the updated `package/MulliganMadness.dll` after building
5. Either:
   - **Actions → Publish to Thunderstore → Run workflow**, or
   - `git tag v0.1.1 && git push origin v0.1.1`

Players then update through r2modman as usual.
