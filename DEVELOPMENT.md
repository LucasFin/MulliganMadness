# Development

## Repos

Mulligan Madness was split into three mods in 0.4.0. Each is standalone — no compile-time
reference between them, and no shared assembly.

| Repo | What it is |
| --- | --- |
| `MulliganMadness` | Take All, curses, Nest Egg / Silver Egg / Return to Sender |
| [`ProMLGStats`](https://thunderstore.io/c/rounds/p/LJIndustries/ProMLGStats/) | Stats HUD, Tab overlay, card-hover previews (client-side only) |
| [`LeanAndMeanCards`](https://thunderstore.io/c/rounds/p/LJIndustries/LeanAndMeanCards/) | The 14 general cards |

Where they need to know about each other they use reflection (`AccessTools.TypeByName` + null
checks), so each works with the others absent.

## Build

```bash
dotnet build MulliganMadness/MulliganMadness.csproj -c Release
```

The project auto-detects a standard Windows Steam + r2modman layout. Override either path:

```bash
dotnet build MulliganMadness/MulliganMadness.csproj -c Release -p:RoundsFolder="D:\Steam\steamapps\common\ROUNDS" -p:R2ProfileName="MyProfile"
```

The build fails with a clear message if the reference assemblies are not where it looked.

Output goes to `package/` only. Do not copy into an r2modman profile by hand — install and test
through r2modman so you exercise the real load order.

## Before touching the pick phase

Read `.cursor/rules/online-pick-safety.mdc`. The short version: the picker's own client spawns
their hand, `IsMine == false` on the host is correct, and nothing here may write
`CardChoice.spawnedCards`, `children` or `picks`.

## Testing multiplayer

`PhotonNetwork.OfflineMode` does not exercise the paths that break. Test with a second client,
with Pick Phase Improvements and Pick N Cards installed, and with a **non-host** doing the
picking — that is the case every 0.3.2x regression showed up in.

Set `Diagnostics/LogPickPhase = true` in the config to log one line per pick start.

## Publish to Thunderstore

Pushes to `main` publish automatically. The workflow bumps the patch version if Thunderstore
already has the repo version.

You can also use **Actions → Publish to Thunderstore → Run workflow**, or tag `v0.x.y` and push.

1. `thunderstore.toml` `namespace` must match the Thunderstore team (`LJIndustries`)
2. Repo secret: `THUNDERSTORE_API_KEY`
3. Commit the rebuilt `package/MulliganMadness.dll`
