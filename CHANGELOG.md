# Changelog

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
