# Mulligan Madness

Take the **whole hand** instead of one card, if the lobby lets you, and if you can live with
the curse it costs you.

Host rules live in **Mods > Mulligan Madness** and sync to the lobby. A one-line summary of the
active rules appears at the first card pick.

Everyone in the lobby needs this mod.

## Split in 0.4.0

Mulligan Madness used to also ship a stats HUD and a general card pack. Those now live in
their own mods. Install them alongside this one if you want the old setup back:

- [**LeanAndMeanCards**](https://thunderstore.io/c/rounds/p/LJIndustries/LeanAndMeanCards/):
  the extra cards (Confetti, Shove, Takebacksies, Dynamite, Thief, Nest Egg, Silver Egg, and the rest)
- [**ProMLGStats**](https://thunderstore.io/c/rounds/p/LJIndustries/ProMLGStats/):
  live stats HUD, Tab overlay, and card-hover previews

This mod is Take All, its curses, and **Return to Sender**. Nest Egg and Silver Egg live in
LeanAndMeanCards.

## Host session settings

| Setting | What it does |
| --- | --- |
| **Take All** | Off, once per game, multi-use, or **vote** |
| **Take All uses** | How many times each player can Take All (0-3) |
| **Vote timeout** | Seconds the lobby has to accept |
| **Take All inflicts a curse** | Take All also gives a random Mulligan Madness curse |
| **Mercy vote** | Auto-offers a Take All vote when you are far behind |
| **Panic Pick timer** | Seconds before Panic Pick chooses for you |

Curses are not toggled here. Use **Toggle Cards > MulliganMadness**.

## Take All

On your pick, **Take All** grabs every card in the current offer.

- **Off / once / multi**: nobody uses it, one use each, or a few uses per player.
- **Vote**: you ask the lobby first. A yes only *unlocks* Take All for that pick; you can
  still take a single card as usual.
- **Mercy vote**: if you are far behind on rounds, the lobby is asked automatically. Same
  rules as a normal vote.
- **Take All inflicts a curse**: grabbing the whole hand also gives you a random curse.

A curse-free bonus Take All (from **Nest Egg** in LeanAndMeanCards) does not spend a session
use and does not inflict a curse.

## Curses

### Common

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/forcedchoice_mini.png" width="72" alt="Forced Choice"> | **Forced Choice**: instantly takes a random offered card |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/leftmostluck_mini.png" width="72" alt="Leftmost Luck"> | **Leftmost Luck**: always takes the leftmost card |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/shorthand_mini.png" width="72" alt="Short Hand"> | **Short Hand**: one fewer card in each of your offers (needs Pick N Cards) |

### Uncommon

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/blinddraft_mini.png" width="72" alt="Blind Draft"> | **Blind Draft**: your offers are face-down |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/fumble_mini.png" width="72" alt="Fumble"> | **Fumble**: 50% chance the card you confirm is swapped for a neighbour |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/hardedges_mini.png" width="72" alt="Hard Edges"> | **Hard Edges**: map edges bounce you 60% harder |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/kickback_mini.png" width="72" alt="Kickback"> | **Kickback**: +25% damage, and your shots strongly kick you away from your gun |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/panicpick_mini.png" width="72" alt="Panic Pick"> | **Panic Pick**: short timer, then a random pick |

## Cards

**Return to Sender** stays here because it is built around curses. Nest Egg and Silver Egg
are in [LeanAndMeanCards](https://thunderstore.io/c/rounds/p/LJIndustries/LeanAndMeanCards/).

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/returntosender_mini.png" width="72" alt="Return to Sender"> | **Return to Sender** (Unique): only offered if you have a curse. Give that curse to another player. |
