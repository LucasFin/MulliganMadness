# Mulligan Madness

Take the whole card offer in one go, with host rules for how often and whether the lobby has to agree. Includes a stats HUD/tab that replaces **Infoholic** and **TabInfo**, plus optional curses when Take All is expensive.

Host match rules live in **Mods → Mulligan Madness** and apply to everyone in the lobby.

## Take All

On your pick, a **Take All** button grabs every card in the current offer.

The host chooses how it works:

- **Off / once / multi** - nobody uses it, one use each, or a few uses per player
- **Vote** - you ask the lobby; if they accept, you can still pick a single card instead of taking everything
- **Mercy vote** - if you're far behind on rounds, the lobby is asked automatically so you can catch up (still optional if they say yes)
- **Take All inflicts a curse** - grabbing the whole hand also gives you a random Mulligan Madness curse

## Stats

Built-in HUD and Tab overlay, meant to replace Infoholic and TabInfo.

- Bottom-left live stats (ammo, bounces, attack speed, and so on), with card-hover previews during picks
- **O** hides the HUD
- **Tab** opens a full panel for every player (drag to move, compare with **C**)

## Curses

If the host enables **Take All inflicts a curse**, taking the whole hand also applies one of these (Take All still only leaves you with one; Return to Sender can stack a second onto someone):

- **Forced Choice** - instantly takes a random offered card
- **Panic Pick** - short timer, then a random pick
- **Leftmost Luck** - always takes the leftmost card
- **Blind Draft** - your offers are face-down. Only you see the backs; everyone else can still read the cards
- **Short Hand** - one fewer card in each of your offers (uses Pick N Cards' draw count, so keep that mod)
- **Fumble** - **50%** chance that confirming a card takes a neighbor from the offer instead
- **Kickback** - **+25% damage**, and your own shots knock you backward
- **Hard Edges** - map edges bounce you **60%** harder

Turn cards and curses on or off in **Toggle Cards** under **MulliganMadness**.

## Cards

- **Thief** - steal one card from another player (once per game)
- **Takebacksies** - after you're stolen from, yoink that card back
- **Sandbag Simulator** - reroll someone's current pick hand (once per game)
- **Jar of Dirt** - Unique card (not a Null). Only offered if you already have Nulls; converts those Nulls into treasures. Disabled Nulls stay Nulls.
- **Confetti** - **+2 ammo**, **25%** faster fire, **10%** less damage
- **Shove** - **+40%** bullet knockback and **+25%** damage
- **Pisser** - **+4 ammo**, **40%** faster fire, **no spread**, **20%** less damage
- **Doorstop** - **+1 block**, block cooldown **20%** longer
- **Bozo Shoes** - players you hit wear clown shoes and take **+50%** knockback from everyone for the rest of the round
- **Draft Sniper** - during someone else's pick, click a card in their offer to replace it for everyone. Extra copies stack as extra snipes.
- **Yeet Cannon** - **+100%** bullet knockback, **+15%** damage, and your shots kick you backward
- **Dynamite** - **+20%** damage. Bullets plant a small delayed blast on hit (same idea as Timed Detonation, including bounces and Drop Grenade landings). Weak boom, huge knockback.
- **TASER TASER TASER** - hits stun for **+0.5s**, **15%** faster fire, **-1 ammo**
- **Safety Net** - map edges (top, bottom, sides) no longer deal damage
- **Nest Egg** - Legendary. After **3 rounds**, gain **1 curse-free Take All**. If you already have a Take All left, this adds another. Extra copies each hatch another.
- **Silver Egg** - Rare. After **2 rounds**, gain **1 curse-free Take All of half the offer** (rounded up). Stacks the same way as Nest Egg. Extra copies each hatch another.
- **Return to Sender** - Unique. Only offered if you have a Mulligan Madness curse. Give that curse to another player. If they already have one, they keep theirs and still get yours.

## Works well with

Compatibility is built in for:

- Pick N Cards
- PickPhaseImprovements
- Genie
- Root Curses / Root Nulled Cards
- NullManager
- KeysCards
- RarityLib
- WillsWackyManagers
