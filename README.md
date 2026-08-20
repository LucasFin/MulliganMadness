# Mulligan Madness

Once-per-game **Take All** during card picks, auto-pick curses, four manipulation cards, and a built-in **stats suite** that replaces Infoholic + TabInfo.

## Host session settings (Mods → Mulligan Madness)

Match rules are set by the **host** and sync to everyone in the lobby when players connect. A one-line summary appears at the **first card pick** of each game.

| Setting | Description |
| --- | --- |
| **Take All mode** | Off, once per game, multi-use (0–3), or **vote** (others accept/decline during your pick) |
| **Take All curse cost** | Take All works but you receive a random MM auto-pick curse afterward |
| **Mercy vote** | Auto-offers a Take All vote when you're down ≥ N round wins vs the leader |
| **Presets** | **Chaos** (vote Take All, curse cost, fast Panic, unlimited Sandbag) · **Competitive** (Take All/curses off) |
| **Card / curse toggles** | Thief, Takebacksies, Sandbag, Jar of Dirt, auto-pick curses |
| **Panic Pick timer** | Seconds before auto-pick |
| **Sandbag limit** | Once per game toggle |

Stats HUD / Tab / compare settings remain **personal** (not synced).

## Take All
When enabled (Mods → Mulligan Madness), a **TAKE ALL** button appears on your pick turn. Each player can use it **once per game**.

Take All grabs every card in the current offer, including **Reroll** and **Table Flip** — you get them and their normal consequences still run.

- **Distill Knowledge** grants the rest of the hand twice and pays Distill’s Nulls twice, without starting the redraw loop.
- **Distill Power** still grants its bonus rares; extra Nulls wait until the **next** pick.
- If Distill already started showing **Nulls**, Take All cashes out the real cards and closes that ritual.

## Curses
Works with WillsWackyManagers curses. You can only have one of these at a time:

| Curse | Effect |
| --- | --- |
| **Forced Choice** | Instantly takes a random offered card |
| **Panic Pick** | Short timer, then auto-picks at random |
| **Leftmost Luck** | Always takes the leftmost card |

## New cards (0.2.0)

| Card | Rarity | Effect |
| --- | --- | --- |
| **Thief** | Legendary | Once per game, steal one card from another player |
| **Takebacksies** | Common | Appears in your pick pool after being stolen from; yoinks your card back from whoever holds it |
| **Sandbag Simulator** | Legendary | Once per game, reroll any player’s current pick hand (including yourself) |
| **Jar of Dirt** | Unique | Replaces every Null you currently own with a treasure (disabled Nulls stay) |

## Default appearance (Mods → Mulligan Madness)

Save your face and color once, then optionally **apply each game** at spawn. You can still change them in character select whenever you want.

- **Save current face & color** — capture from lobby/character select
- **Default color index** — palette slot on the color wheel
- **Apply saved appearance now** — test without restarting

## Stats (v0.3.7 — replaces Infoholic + TabInfo)

All stats UI is client-side. Two surfaces only — no overlapping panels.

### Quick controls

| Key | What it does |
| --- | --- |
| **O** | Show/hide bottom-left live stats |
| **Tab** | Open/close the stats panel (left side by default) |
| **Esc** | Close Tab panel |
| **C** | In Tab: toggle compare vs another player |
| **[ / ]** | In compare mode: switch opponent |

**Bottom-left HUD:** transparent text, always on unless hidden. In battle shows your stats. During card picks it switches to **whoever is picking** — hover a card to preview how it would change their build `[+/-]`.

**Tab panel:** scroll through every player. **Compare mode (C)** shows your stats with green/red `(delta)` vs one opponent, plus their full build below for reference.

Adjust panel size, position, and opacity under **Mods → Mulligan Madness**.

| Feature | Default | Notes |
| --- | --- | --- |
| **Always-on HUD** | On | Bottom-left · **O** toggles · pick phase follows active picker |
| **Tab overlay** | On | Left-side scroll panel · **C** compare · auto-closes on pick |
| **Compare** | In Tab | One opponent at a time with stat deltas — no extra overlay |

Configure under **Mods → Mulligan Madness**. Host rules vs personal UI are labeled in the menu.

## Install
Install with **r2modman** / Thunderstore Mod Manager. Dependencies are pulled in automatically.

Works best with Pick N Cards, PickPhaseImprovements, Genie, Root Curses, RarityLib, and WillsWackyManagers.

**Replacing Infoholic + TabInfo:** Mulligan Madness ships its own `TabInfo.dll` compatibility shim so Root/NullManager mods that register extra stats keep working. In r2modman, choose **Disable TabInfo only** (not "Disable all") — do not disable Root mods.
