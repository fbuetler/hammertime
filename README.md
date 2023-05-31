![teaser image](game_teaser.png){width=200px}
# Hammer Time!!!
# :video_game: Game Programming Lab 2023 - House Rapture - Team4

Our game is a 2.5-dimensional, top-down game. The players find themselves on a playing field of limited size made up of square tiles. If a player walks across a tile, the tile will take damage. Do this often enough, and the tile breaks and disappears, leaving a gap in the field. If a player walks across such a gap or off the side of the field, they fall to their deaths. 

Players can throw hammers to knock each other back into unfavorable positions and potentially push people off the map. You can charge your throw so that the hammer flies further away from you, the longer you charge. The hammers come back to their owner once they reach maximum distance, be careful not to be hit by a hammer you just dodged.

In case you are stuck on a little island of your making, or in case you really need to dodge a hammer, don't be afraid! You can always dash to cross gaps or evade. It is a risky move as you cannot move for a brief time after dashing, but if you use it carefully it will be your key to victory!

To spice the game a bit, there are walls on the map that can't be crossed and will block hammers.
They are breakable however by throwing your hammer against them a few times.

To win a round, one must be the last player to not have fallen in the arena (a draw is possible but is extraordinary rare as two players must fall at the exact same time)

The result is a game which feels slightly chaotic and surprising, but contains multiple strategic elements. 

Members: Frederic Necker, Florian Buetler, Lasse Meinen, Deniz Yildiz, and Shiran Sun 

## Controls:

* Left thumbstick: move player
* Right thumbstick: aim with hammer
* Left shoulder button: dash
* Right shoulder button: press to begin charging the hammer, relase to throw.
* Start button: pause

## Description

Hammers are flying everywhere, the ground is breaking beneath your feet, and everyone is screaming at you to stop camping in a corner. Hammer Time is a fast-paced game that allows you to prove to your friends that you could best them in combat once and for all! Simple controls, short rounds and fast-paced gameplay make Hammer Time perfect for any party. The light-hearted graphics give the game a fun feel and allow you to enjoy some eye-candy, too. So hook up your controllers to the nearest computer and let the chaos commence!

The game is available for free on [itch.io](https://lonely-hermit.itch.io/hammertime)

## Development

All commands have to be run from within `src/hammertime`.

For more information see the [MonoGame Documentation](https://docs.monogame.net/) and the [MonoGame Source Code](https://github.com/MonoGame/MonoGame).

### Run

Run the development build:

```bash
dotnet run Program.cs
```

Run the release build:

```bash
dotnet run --configuration Release Program.cs
```

### Publish

Release for Linux:

```bash
dotnet publish -c Release -r linux-x64 /p:PublishReadyToRun=false /p:TieredCompilation=false --self-contained
```

The binary can be found at `bin/Release/net6.0/linux-x64/publish/hammertime`.

Release for Windows:

```bash
dotnet publish -c Release -r win-x64 /p:PublishReadyToRun=false /p:TieredCompilation=false --self-contained
```

The exe can be found at `bin/Release/net6.0/win-x64/publish/hammertime.exe`.