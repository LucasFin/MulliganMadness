# Development

## Build

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build MulliganMadness/MulliganMadness.csproj -c Release
```

Copies `MulliganMadness.dll` into `package/` and your local r2modman Default profile plugins folder.

## Publish to Thunderstore

1. `thunderstore.toml` `namespace` must match the Thunderstore team (`LJIndustries`)
2. Repo secret: `THUNDERSTORE_API_KEY`
3. Bump `versionNumber` in `thunderstore.toml` and `manifest.json`
4. Commit updated `package/MulliganMadness.dll` after building
5. **Actions → Publish to Thunderstore → Run workflow**, or tag `v0.1.x` and push
