# Overview

Mulligan Madness lets a picker take the **whole offered hand** instead of one card — subject to
the host's rules, and usually at the cost of a curse.

As of 0.4.0 this mod is Take All and its curses only. Two features were split out:

| Was | Now |
| --- | --- |
| Stats HUD, Tab overlay, card-hover previews | [ProMLGStats](https://github.com/3WiseStooges/ProMLGStats) |
| The 14 general cards | [LeanAndMeanCards](https://github.com/3WiseStooges/LeanAndMeanCards) |

Nest Egg, Silver Egg and Return to Sender stayed here — they are built on Take All and curses.

## Pages

- [Host settings and Take All](02-host-settings-and-take-all.md)
- [Curses](04-curses.md)

## Multiplayer

Everyone in the lobby needs this mod. It does not override how ROUNDS spawns cards: the picker's
own client builds their hand, exactly as vanilla does. Releases 0.3.27–0.3.31 did override that,
which is what caused the glitchy picker and cards appearing on the wrong player. See
`CHANGELOG.md` for the full account.
