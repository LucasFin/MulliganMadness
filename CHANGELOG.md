# Changelog

## 0.4.6

- Bring **Nest Egg** back. It hatches a curse-free Take All after 3 rounds, so it belongs
  in this mod. Silver Egg stays out.
- Nest Egg full art and mini icon have no glow (flat sticker, black background).

## 0.4.5

- Thunderstore icon: original TAKE ALL card fan with the green blob in the corner.

## 0.4.4

- Nest Egg and Silver Egg moved to
  [LeanAndMeanCards](https://thunderstore.io/c/rounds/p/LJIndustries/LeanAndMeanCards/).
  Return to Sender stays here. Take All still accepts a curse-free bonus charge from other mods
  (Nest Egg uses that when both are installed).
- Take All toast and button frames match the box (no leftover 9-slice border).

## 0.4.3

- **Fix missing card-bar mini icons**, same regression as LeanAndMeanCards 1.1.4: a card
  added while a re-stamp was in flight had its request dropped instead of queued, so its
  icon was wiped by FancyCardBar's rebuild and never restored.

## 0.4.2

- Fix `PickSafetyPatch` never being installed. Harmony's `PatchClassProcessor`
  skips any class without a class-level `[HarmonyPatch]`, so the finalizers that
  log a thrown `ReplaceCards` / `Pick` / `SpawnUniqueCard` — and clear a stuck
  `isPlaying` — were silently absent.
- Add optional pick-phase spawn tracing behind `Diagnostics/LogPickPhase`
  (off by default). It reports spawned/alive card counts, `picks`, `isPlaying`
  and whether the picker resolved, which separates "the hand never spawned" from
  "it spawned and something removed it".

## 0.4.1

- Thunderstore listing matches the split: this page is Take All and curses only, with links to
  [LeanAndMeanCards](https://thunderstore.io/c/rounds/p/LJIndustries/LeanAndMeanCards/) and
  [ProMLGStats](https://thunderstore.io/c/rounds/p/LJIndustries/ProMLGStats/).

## 0.4.0

This mod is now Take All and its curses. Two pieces moved out:

- [**LeanAndMeanCards**](https://thunderstore.io/c/rounds/p/LJIndustries/LeanAndMeanCards/) —
  the 14 general cards (Confetti, Shove, Takebacksies, Bozo Shoes, Doorstop, Dynamite, Pisser, Draft Sniper, Safety Net, TASER, Yeet Cannon, Jar of Dirt, Sandbag Simulator, Thief)
- [**ProMLGStats**](https://thunderstore.io/c/rounds/p/LJIndustries/ProMLGStats/) —
  the stats HUD, Tab overlay, and card-hover previews

Nest Egg, Silver Egg, and Return to Sender stay here — they are built on Take All and curses.

Install those two mods alongside this one if you want the old 0.3.x setup back.

### Fixes

- Online picks work for non-host players again (no stuck picker, leftover cards, or taking a
  card nobody actually chose).
- Works alongside the real TabInfo mod. The old bundled stub that blocked it is gone.
- Hard Edges knockback stays consistent.
- Host settings sync more reliably when joining a lobby.

Notes below for 0.3.x still mention cards and the stats HUD. Those shipped in this package
until 0.4.0.

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
