# Mulligan Madness

Take the **whole hand** instead of one card — if the lobby lets you, and if you can live with
the curse it costs you.

Host rules live in **Mods → Mulligan Madness** and sync to the lobby. A one-line summary of the
active rules appears at the first card pick.

> **Split in 0.4.0.** The stats HUD is now [ProMLGStats](https://github.com/3WiseStooges/ProMLGStats)
> and the general card pack is now [LeanAndMeanCards](https://github.com/3WiseStooges/LeanAndMeanCards).
> Install those separately if you want them. This mod is Take All and its curses.

## Host session settings

| Setting | What it does |
| --- | --- |
| **Take All** | Off, once per game, multi-use, or **vote** |
| **Take All uses** | How many times each player can Take All (0–3) |
| **Vote timeout** | Seconds the lobby has to accept |
| **Take All inflicts a curse** | Take All also gives a random Mulligan Madness curse |
| **Mercy vote** | Auto-offers a Take All vote when you are far behind |
| **Panic Pick timer** | Seconds before Panic Pick chooses for you |

Curses are not toggled here. Use **Toggle Cards → MulliganMadness**.

## Take All

On your pick, **Take All** grabs every card in the current offer.

- **Off / once / multi** — nobody uses it, one use each, or a few uses per player.
- **Vote** — you ask the lobby first. A yes only *unlocks* Take All for that pick; you can
  still take a single card as usual.
- **Mercy vote** — if you are far behind on rounds, the lobby is asked automatically. Same
  rules as a normal vote.
- **Take All inflicts a curse** — grabbing the whole hand also gives you a random curse.

**Nest Egg** adds an extra curse-free Take All after it hatches.

## Curses

### Common

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/forcedchoice_mini.png" width="72" alt="Forced Choice"> | **Forced Choice** — instantly takes a random offered card |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/leftmostluck_mini.png" width="72" alt="Leftmost Luck"> | **Leftmost Luck** — always takes the leftmost card |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/shorthand_mini.png" width="72" alt="Short Hand"> | **Short Hand** — one fewer card in each of your offers (needs Pick N Cards) |

### Uncommon

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/blinddraft_mini.png" width="72" alt="Blind Draft"> | **Blind Draft** — your offers are face-down |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/fumble_mini.png" width="72" alt="Fumble"> | **Fumble** — 50% chance the card you confirm is swapped for a neighbour |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/hardedges_mini.png" width="72" alt="Hard Edges"> | **Hard Edges** — map edges bounce you 60% harder |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/kickback_mini.png" width="72" alt="Kickback"> | **Kickback** — +25% damage, and your shots strongly kick you away from your gun |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/panicpick_mini.png" width="72" alt="Panic Pick"> | **Panic Pick** — short timer, then a random pick |

## Cards

The three cards that stayed here are built on Take All and curses.

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/nestegg_mini.png" width="72" alt="Nest Egg"> | **Nest Egg** (Legendary) — after 3 rounds, gain 1 curse-free Take All. Extra copies each hatch another. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/silveregg_mini.png" width="72" alt="Silver Egg"> | **Silver Egg** (Rare) — after 2 rounds, hatches into a small random card haul. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/returntosender_mini.png" width="72" alt="Return to Sender"> | **Return to Sender** (Unique) — only offered if you have a curse. Give that curse to another player. |

## Multiplayer

Everyone in the lobby needs this mod. It does not override how the game spawns cards — the
picker's own client builds their hand, exactly as vanilla does. See `CHANGELOG.md` for why
that sentence is worth writing down.

## Works well with

Pick N Cards, PickPhaseImprovements, Genie, Root Curses / Root Nulled Cards, NullManager,
KeysCards, RarityLib, WillsWackyManagers, and the two mods split out of this one.

## Build

```bash
dotnet build MulliganMadness/MulliganMadness.csproj -c Release
```

Override the paths if your install differs:

```bash
dotnet build MulliganMadness/MulliganMadness.csproj -c Release -p:RoundsFolder="D:\Steam\steamapps\common\ROUNDS" -p:R2ProfileName="MyProfile"
```

The DLL and `Art/` land in `package/`. Install and test through r2modman rather than copying
by hand.
