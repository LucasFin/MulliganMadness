# Changelog

## 0.4.0

Split into three mods and fixed the pick-phase desync that 0.3.27-0.3.31 were chasing.

### Split

MulliganMadness is now Take All + curses only. Two features moved to their own mods:

- **[ProMLGStats](https://github.com/3WiseStooges/ProMLGStats)** - the stats HUD, Tab overlay and
  card-hover previews.
- **[LeanAndMeanCards](https://github.com/3WiseStooges/LeanAndMeanCards)** - the 14 general cards
  (Confetti, Shove, Takebacksies, Bozo Shoes, Doorstop, Dynamite, Pisser, Draft Sniper,
  Safety Net, TASER, Yeet Cannon, Jar of Dirt, Sandbag Simulator, Thief).

Nest Egg, Silver Egg and Return to Sender stay here - they are built on Take All and curses.

### Fixed: the pick phase no longer overrides who spawns cards

0.3.27 read `IsMine=false` on the host as a stall and re-gated card spawning on
`IsMasterClient`. That reading was wrong. Vanilla `CardChoice.Pick` is:

```csharp
else if (PlayerManager.instance.GetPlayerWithID(pickrID).data.view.IsMine)
    StartCoroutine(ReplaceCards(pickedCard, clear));
```

The *picker* spawns their own hand, and `SpawnUniqueCard` uses `PhotonNetwork.Instantiate` so
every client receives it. `IsMine == false` on the host is correct whenever a non-host is
picking. The override caused all of the following, and all of it is now gone:

- **Glitchy card picker.** A non-host picker's `spawnedCards` list stayed empty, and
  `CardChoice.DoPlayerSelect` returns early on `spawnedCards.Count == 0`. They could not move
  the selection or confirm a card until `RPCA_SyncOfferedHand` back-filled the list, which
  polled at 0.35s / 0.9s / 1.6s / 2.5s.
- **Cards the picker never took.** The host Photon-owned cards for someone else's pick,
  leaving orphans whose ViewIDs got reused - the "friend sees Sneaky the host never took" bug
  that 0.3.31 tried to paper over.
- **`Ev Destroy Failed` spam.** `DestroyOrphanOfferCards` called `PhotonNetwork.Destroy` on
  objects other clients still referenced.
- **Diverging pick counts.** `CardChoice.picks` was written on remotes by reflection from an
  RPC payload.

The empty-offer bug those releases were chasing was really the `readonly struct` Harmony abort
(fixed in 0.3.23) and destroying `CardChoice.children` on StartPick (fixed in 0.3.24). Both
were already fixed by then.

`PickDiagnosticsPatch` (754 lines) is replaced by `PickSafetyPatch` (~120 lines) which is
Finalizers and one optional log line - it never changes what spawns or who spawns it.

### Fixed: no more TabInfo GUID collision

MulliganMadness shipped a stub `TabInfo.dll` that declared willuwontu's own plugin GUID,
`com.willuwontu.rounds.tabinfo`. Two plugins cannot share a GUID, so users had to disable the
real TabInfo to run this mod. The stub is deleted. TabInfo integration is reflection-only and
entirely optional.

### Other fixes

- Hard Edges no longer risks a knockback desync. The out-of-bounds flag is set only on the
  owning client (the only one that raises the force RPC) and is cleared by a Finalizer, so a
  throw mid-LateUpdate can no longer leave it stuck.
- The settings handshake no longer fires `RaiseEvent` outside a room, which logged
  `RaiseEvent(69) failed` and dropped the payload.
- Card bar icon stamping is coalesced. Five hooks reported the same card addition, so adding
  one card could trigger up to fifteen full-bar sweeps.
- `AssemblyVersion` is pinned. UnboundLib resolves RPC targets via `Type.AssemblyQualifiedName`,
  which embeds the assembly version, so letting it drift silently breaks RPCs between clients
  on adjacent releases.
- The build no longer hardcodes one machine's Linux paths. It auto-detects a standard Windows
  Steam + r2modman layout and accepts `-p:RoundsFolder` / `-p:R2ProfileName` overrides.

## 0.3.31

- Dynamite plants and explodes on every client (the fuse used to exist only on the shooter's machine, so non-hosts saw nothing). Knockback still applies once, on the host.
- TASER stuns on a hit, including catching your own bullet (that path used to skip self).
- Silver Egg hatch loot is rolled once on the host and synced; remotes no longer each roll a different hand.
- Stop leftover Photon pick cards from stacking behind the offer. Those ghosts reused ViewIDs so a friend could see/apply **Sneaky** (and wall-pierce) the host never took.
- Draft Sniper **locks** a card in someone else's offer (they pick another). No Photon spawn/destroy — that replace path was what stacked ghost cards and applied Sneaky on the wrong client. Can't lock the last remaining card.
- Take All now grants every offered Null card (NullManager `NullCard` visuals + Photon spawn name), instead of skipping ones that failed to encode.
- Turn off MulliganMadness pick-card particle glow entirely.

## 0.3.30

- Fix stuck face-down picks: after the master Instantiates the offer hand, sync `spawnedCards` ViewIDs to all clients so the picker can flip and select (0.3.28–0.3.29 left remotes with empty local lists).

## 0.3.29

- Dial back pick-card glow for **all** MulliganMadness cards (not just Nest/Silver Egg): weaker vanilla particles, moving-bg off by default, hard glow cap; eggs stay lowest.
- Also block non-master `ReplaceCards` when it would Instantiates (IDoEndPick bypasses Pick) so orphan card backs cannot stick into the fight.

## 0.3.28

- Fix stacked pick cards / extra flip sounds: online, only the master starts ReplaceCards (vanilla IsMine spawn on every client was doubling Photon Instantiates after the 0.3.27 stall force).

## 0.3.27

- Fix online empty offers: when pick stalls with zero cards because vanilla skipped ReplaceCards (`IsMine=false` on host), force-start ReplaceCards on the master instead of calling Pick() again (which hits the same gate).

## 0.3.26

- Add pick-phase diagnostics (ReplaceCards / spawn watch logs) and stricter online hand-ready checks so Draft Sniper / AutoPick cannot touch a half-built PPI hand.
- If a pick starts but no cards appear within ~1.6s, clear stuck `isPlaying` and retry `Pick()` once (vanilla only spawns when the picker's view IsMine; a thrown spawn coroutine softlocks empty).
- Remove unused default face/color save-apply feature (did not work reliably with other face mods).

## 0.3.25

- Harden pick phase: clear sticky Take All collect flag and Draft Sniper bans each pick; never-throw guards on card spawn / hand-build hooks so one bad art or allow-card path cannot empty online offers.

## 0.3.24

- Fix online empty card offers: stop destroying CardChoice children on StartPick (raced Pick Phase Improvements' hand rebuild). Take All button needs a spawned hand, so it stayed hidden too.

## 0.3.23

- Fix Harmony startup: a `readonly struct` in card-art FX made PatchAll abort on Unity Mono, so MM pick/combat patches never applied (empty/broken online picks).

## 0.3.22

- Dynamite planted charge is the flashing circle again (no mini-art sticker overlay).
- Dynamite boom uses Timed Detonation's explosion sound.

## 0.3.21

- Cleaner card text: drop backwards stat lines, Pisser spread row, and egg "per copy" clutter.
- Bozo Shoes: yellow BOZO label over the head plus small shoes (no more giant card-mini squares).

## 0.3.20

- Stop rewriting other mods' card-bar icons (FancyCardBar silhouettes).
- Stats category headers are spaced and labeled so Combat / Mobility / Projectile read as groups.
- Dynamite blast now shoves boxes, hanging props, and other loose physics objects.
- TASER TASER TASER actually stuns on hit.

## 0.3.19

- Clearer card-bar minis: transparent cropped icons, FancyCardBar `FancyIcon` soft-dep, strip RGB rainbow overlays.
- HUD labels more readable on neon maps (brighter labels, stronger outline, darker scrim).
- Bozo Shoes: visible clown-shoe markers on marked players.
- Dynamite: longer fuse with bomb flicker, stronger knockback yeet.

## 0.3.18

- Add Thunderstore `CHANGELOG.md` (this tab) with a backfilled history from prior releases.

## 0.3.17

- Fix card art in Toggle Cards (SpriteRenderer again; UI Images were blank).
- Stamp mini icons over FancyCardBar placeholders in the top-right card bar.
- Theme borders by rarity; curses stay purple so they read as curses.
- Fix Dynamite: delayed blast + knockback on hit.
- Strengthen Yeet Cannon and Kickback; kick opposite gun aim (aim down to hop).
- Remove Mulligan Madness effect rows from the bottom-left HUD (Tab only).
- Soft HUD scrim/outline so white maps do not wash out the text.
- Safety Net: if you soft-lock outside the map, die after a few seconds.
- Draft Sniper moved to Rare.

## 0.3.16

- Rework Silver Egg into a weaker Golden Egg-style hatch: after 2 rounds, small common/uncommon haul.
- Nest Egg stays the curse-free Take All path.
- README / wiki updates to match.

## 0.3.15

- Clearer Thunderstore listing and short description.

## 0.3.14

- New cards and curses, card-bar minis, and TDM pick / Take All fixes.

## 0.3.13

- Reload and Bullets on the bottom-left stats HUD.

## 0.3.12

- Simpler host settings, Tab ping/drag, and Take All curse button copy.

## 0.3.11

- Online TDM multiplayer fixes for Take All, steals, and feedback.
- Publish CI: stop guessing Thunderstore versions when the API fails; User-Agent for version lookup.

## 0.3.9

- Stats HUD/tab layout fixes, online compare, and Forced Choice auto-pick.

## 0.3.7

- Unified stats UI, default appearance (save/apply face and color), and Tab compare.
- Bottom-left pick HUD.

## 0.3.5

- Host session settings synced to the lobby.
- Vote / mercy Take All modes and UI polish.
- Curse art and card-art cover fixes.

## 0.3.2

- TabInfo shim fixes (Root/NullManager compatibility).
- Stats overlays only during matches; compare is Tab-only.

## 0.3.1

- Version bump so Thunderstore can publish past a reused 0.3.0 upload.
- CI increments patch when the listing already has the repo version.

## 0.3.0

- Built-in stats HUD, Tab overlay, compare, and card-hover previews (Infoholic/TabInfo replacement).
- Manipulation cards: Thief, Takebacksies, Sandbag Simulator.
- TabInfo.dll shim for Root-style stat registration.
- Jar of Dirt (Unique): convert owned Nulls into treasures; live Nulls on the HUD.

## 0.1.x

- Take All pick UI (once / multi / later vote modes).
- Auto-pick curses via WillsWackyManagers.
- Take All multiplayer sync, Pick N Cards hardening, Distill/Reroll/Table Flip/Null handling.
- Thunderstore packaging under LJIndustries and publish-on-main CI.
