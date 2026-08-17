# Mulligan Madness

Once-per-game **Take All** during card picks, auto-pick curses, three manipulation cards, and a built-in **stats suite** that replaces Infoholic + TabInfo.

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

## Stats (v0.3.0 — replaces Infoholic + TabInfo)

All stats UI is client-side and works for host and regular players.

| Feature | Default | Controls |
| --- | --- | --- |
| **Always-on HUD** | On | Bottom-left panel with your live stats. Toggle with **O**. |
| **Tab overlay** | On | Press **Tab** for full stats on every player. |
| **Compact compare** | On | Top-right panel shows up to **4 players**. **Pin** locks a baseline; **Reset** clears it. Deltas show in green/red. |
| **Card hover preview** | On | During **your** pick, hover a card to see green `[+/-]` deltas on your HUD for how it would change your build. |

Configure under **Mods → Mulligan Madness**. Simple HUD mode shows fewer stats (Infoholic-style).

## Install
Install with **r2modman** / Thunderstore Mod Manager. Dependencies are pulled in automatically.

Works best with Pick N Cards, PickPhaseImprovements, Genie, Root Curses, RarityLib, and WillsWackyManagers.

**Replacing Infoholic + TabInfo:** Mulligan Madness ships its own `TabInfo.dll` compatibility shim so Root/NullManager mods that register extra stats keep working. In r2modman, choose **Disable TabInfo only** (not "Disable all") — do not disable Root mods.
