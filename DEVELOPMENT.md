# Development

## Build

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build MulliganMadness/MulliganMadness.csproj -c Release
```

Copies `MulliganMadness.dll` and `TabInfo.dll` into `package/` only. Do not copy into r2modman profiles; install and test through r2modman / Thunderstore.

## Publish to Thunderstore

Pushes to `main` publish automatically. The workflow bumps the patch version if Thunderstore already has the repo version.

You can still **Actions → Publish to Thunderstore → Run workflow**, or tag `v0.1.x` and push.

1. `thunderstore.toml` `namespace` must match the Thunderstore team (`LJIndustries`)
2. Repo secret: `THUNDERSTORE_API_KEY`
3. Commit updated `package/MulliganMadness.dll` after building
