# Mulligan Madness

**Take All** during card picks, auto-pick curses, four manipulation cards, and a built-in **stats suite** that replaces Infoholic + TabInfo.

## Host session settings (Mods → Mulligan Madness)

Match rules are set by the **host** and sync to the lobby. A one-line summary appears at the first card pick.

| Setting | Description |
| --- | --- |
| **Take All** | Off, once per game, multi-use (0–3), or **vote** |
| **Take All curse cost** | Take All also gives a random MM auto-pick curse |
| **Mercy vote** | Auto-offers a Take All vote when you're far behind |
| **Panic Pick timer** | Seconds before Panic Pick chooses for you |
| **Presets** | **Chaos** (vote Take All, curse cost, mercy) · **Competitive** (Take All off) |

**Cards and curses** are not toggled here — use **Toggle Cards** (and your curse manager) like any other custom card.

Default look (save / apply face & color) is personal.

## Take All

A **TAKE ALL** button appears on your pick turn when the host has it on:

- **Once per game** — one Take All each
- **Multi-use** — up to N uses per player (0–3)
- **Vote** — other players accept or decline during your pick

Take All grabs every card in the current offer, including **Reroll** and **Table Flip**.

- **Distill Knowledge** grants the rest of the hand twice and pays Distill’s Nulls twice, without starting the redraw loop.
- **Distill Power** still grants its bonus rares; extra Nulls wait until the **next** pick.
- If Distill already started showing **Nulls**, Take All cashes out the real cards and closes that ritual.

## Curses

Works with WillsWackyManagers. You can only have one of these at a time:

| Curse | Effect |
| --- | --- |
| **Forced Choice** | Instantly takes a random offered card |
| **Panic Pick** | Short timer, then auto-picks at random |
| **Leftmost Luck** | Always takes the leftmost card |

## Cards

| Card | Rarity | Effect |
| --- | --- | --- |
| **Thief** | Legendary | Once per game, steal one card from another player |
| **Takebacksies** | Common | Appears in your pick pool after being stolen from; yoinks your card back |
| **Sandbag Simulator** | Legendary | Reroll any player’s current pick hand (once per game) |
| **Jar of Dirt** | Unique | Replaces every Null you currently own with a treasure |

## Default appearance

Save your face and color once, then optionally apply it each game. You can still change in character select.

## Stats

All stats UI is client-side.

| Key | What it does |
| --- | --- |
| **O** | Show/hide bottom-left live stats |
| **Tab** | Open/close the stats panel |
| **Esc** | Close Tab panel |
| **C** | In Tab: compare vs another player |
| **[ / ]** | In compare mode: switch opponent |

**Tab panel:** drag the **top bar** to move it, drag the **left edge** to resize. Position is saved. Online games show ping next to names.

**Bottom-left HUD:** follows whoever is picking and shows `(delta)` for the hovered card. After picks, `(delta)` is vs the start of your last pick (HP damage is ignored in fights).

## Install

Install with **r2modman** / Thunderstore Mod Manager. Dependencies are pulled in automatically.

Works best with Pick N Cards, PickPhaseImprovements, Genie, Root Curses, RarityLib, and WillsWackyManagers.

**Replacing Infoholic + TabInfo:** Mulligan Madness ships its own `TabInfo.dll` compatibility shim so Root/NullManager mods that register extra stats keep working. In r2modman, choose **Disable TabInfo only** (not "Disable all") — do not disable Root mods.
