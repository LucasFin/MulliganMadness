# Mulligan Madness

Adds a **Take All** button during the pick phase, a pack of MulliganMadness cards and curses, and a stats HUD with a Tab menu. Built to replace **Infoholic** and **TabInfo**.

Host match rules live in **Mods → Mulligan Madness** and sync to the lobby. A one-line summary appears at the first card pick.

## Host session settings

| Setting | What it does |
| --- | --- |
| **Take All** | Off, once per game, multi-use, or **vote** |
| **Take All uses** | How many times each player can Take All (0–3) |
| **Vote timeout** | Seconds the lobby has to accept |
| **Take All inflicts a curse** | Take All also gives a random MulliganMadness curse |
| **Mercy vote** | Auto-offers a Take All vote when you are far behind |
| **Panic Pick timer** | Seconds before Panic Pick chooses for you |

Cards and curses are not toggled here. Use **Toggle Cards** under **MulliganMadness**.

## Take All

On your pick, **Take All** grabs every card in the current offer.

- **Off / once / multi** - nobody uses it, one use each, or a few uses per player
- **Vote** - you ask the lobby first. If they say yes, Take All is unlocked for that pick. It is not forced: you can still take a single card as usual.
- **Mercy vote** - if you are far behind on rounds, the lobby is asked automatically. Same as Vote: a yes only unlocks Take All, it does not take the hand for you.
- **Take All inflicts a curse** - grabbing the whole hand also gives you a random MulliganMadness curse

Nest Egg adds an extra curse-free Take All after it hatches. Silver Egg hatches into a small random card haul instead (weaker / faster cousin of KeysCards' The Golden Egg).

## Stats

- Bottom-left live HUD (ammo, bounces, attack speed, and so on), with card-hover previews during picks
- **O** hides the HUD
- **Tab** opens a full panel for every player (drag to move, compare with **C**)

## Curses

If Take All inflicts a curse, you get one of these. Take All still only leaves you with one; Return to Sender can stack a second onto someone.

### Common

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/forcedchoice_mini.png" width="72" alt="Forced Choice"> | **Forced Choice** - instantly takes a random offered card |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/leftmostluck_mini.png" width="72" alt="Leftmost Luck"> | **Leftmost Luck** - always takes the leftmost card |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/shorthand_mini.png" width="72" alt="Short Hand"> | **Short Hand** - one fewer card in each of your offers (needs Pick N Cards) |

### Uncommon

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/blinddraft_mini.png" width="72" alt="Blind Draft"> | **Blind Draft** - your offers are face-down |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/fumble_mini.png" width="72" alt="Fumble"> | **Fumble** - 50% chance the card you confirm is swapped for a neighbor |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/hardedges_mini.png" width="72" alt="Hard Edges"> | **Hard Edges** - map edges bounce you 60% harder |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/kickback_mini.png" width="72" alt="Kickback"> | **Kickback** - +25% damage, and your shots strongly kick you away from your gun |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/panicpick_mini.png" width="72" alt="Panic Pick"> | **Panic Pick** - short timer, then a random pick |

## Cards

### Common

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/confetti_mini.png" width="72" alt="Confetti"> | **Confetti** - +2 ammo, 25% faster fire, 10% less damage |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/shove_mini.png" width="72" alt="Shove"> | **Shove** - +40% bullet knockback and +25% damage |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/takebacksies_mini.png" width="72" alt="Takebacksies"> | **Takebacksies** - after you are stolen from, yoink that card back |

### Uncommon

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/bozoshoes_mini.png" width="72" alt="Bozo Shoes"> | **Bozo Shoes** - players you hit wear clown shoes and take +50% knockback for the rest of the round |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/doorstop_mini.png" width="72" alt="Doorstop"> | **Doorstop** - +1 block, block cooldown 20% longer |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/dynamite_mini.png" width="72" alt="Dynamite"> | **Dynamite** - +20% damage. Bullets plant a small delayed blast on hit (including bounces). Weak boom, huge knockback. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/pisser_mini.png" width="72" alt="Pisser"> | **Pisser** - +4 ammo, 40% faster fire, no spread, 20% less damage |

### Rare

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/draftsniper_mini.png" width="72" alt="Draft Sniper"> | **Draft Sniper** - during someone else's pick, click a card to replace it. Extra copies stack. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/safetynet_mini.png" width="72" alt="Safety Net"> | **Safety Net** - map edges no longer deal damage. If you soft-lock outside the map, you die after a few seconds. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/silveregg_mini.png" width="72" alt="Silver Egg"> | **Silver Egg** - after 2 rounds, hatches into a small random card haul (weaker and faster than The Golden Egg). Extra copies each hatch another. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/tasertasertaser_mini.png" width="72" alt="TASER TASER TASER"> | **TASER TASER TASER** - hits stun for +0.5s, 15% faster fire, -1 ammo |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/yeetcannon_mini.png" width="72" alt="Yeet Cannon"> | **Yeet Cannon** - +100% bullet knockback, +15% damage, and your shots strongly kick you away from your gun |

### Legendary

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/nestegg_mini.png" width="72" alt="Nest Egg"> | **Nest Egg** - after 3 rounds, gain 1 curse-free Take All. Extra copies each hatch another. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/sandbag_mini.png" width="72" alt="Sandbag Simulator"> | **Sandbag Simulator** - reroll someone's current pick hand (once per game) |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/thief_mini.png" width="72" alt="Thief"> | **Thief** - steal one card from another player (once per game) |

### Unique

| | |
| :---: | --- |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/jarofdirt_mini.png" width="72" alt="Jar of Dirt"> | **Jar of Dirt** - only offered if you have Nulls. Converts those Nulls into treasures. Disabled Nulls stay Nulls. |
| <img src="https://raw.githubusercontent.com/LucasFin/MulliganMadness/main/package/Art/returntosender_mini.png" width="72" alt="Return to Sender"> | **Return to Sender** - only offered if you have a MulliganMadness curse. Give that curse to another player. They keep any curse they already have. |

## Works well with

Pick N Cards, PickPhaseImprovements, Genie, Root Curses / Root Nulled Cards, NullManager, KeysCards, RarityLib, WillsWackyManagers.
