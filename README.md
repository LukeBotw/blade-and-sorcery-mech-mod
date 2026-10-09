# Mech Suit — a Blade & Sorcery mod

Climb into a mech suit and fight as a walking war machine.

| Ability | How to use it |
|---|---|
| **Suit toggle** | Hold **both Alt-Use buttons** for 1 second, or flip *Mech Suit Active* in the book's Mod Options. Both controllers buzz when the suit powers on or off. |
| **Servo legs** | Faster movement and much higher jumps. No fall damage (optional). |
| **Armor plating** | Absorbs 60% of incoming damage by default, and the hull repairs itself over time. |
| **Thrusters** | Point an **empty hand palm-down** and hold **Trigger** to blast upward. Use both hands to hover or fly. |
| **Ground slam** | Fall fast (jump off something high, or cut your thrusters mid-air) and land. The shockwave knocks down and damages everything nearby. Harder landings hit harder. |
| **Power fists** | Fast **empty-handed** punches send enemies flying. |

You can tune every value in game under **Book → Mods → Mech Suit**: speed, jump, armor, regen, thrust, slam radius and damage, punch damage. Each ability can also be switched off on its own.

## Requirements

- Blade & Sorcery **1.0+** (U12 or newer). The mod is a pure C# `ThunderScript`, so it needs no Unity asset bundles or catalog JSON.
- PCVR or Nomad. On Nomad, the DLL has to be built against the Nomad `ThunderRoad.dll`.

## Building

1. Install the .NET SDK (any version that can build `net472`, for example .NET 8 SDK on Windows).
2. Build, pointing `GameDir` at your game install:

   ```sh
   dotnet build src/MechMod/MechMod.csproj -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Blade & Sorcery"
   ```

   The project references `ThunderRoad.dll` and the Unity modules from `BladeAndSorcery_Data/Managed`. After a successful build, `MechSuit.dll` is copied into `Mod/MechSuit/`.

## Installing

Copy the `Mod/MechSuit` folder (with `manifest.json` and `MechSuit.dll`) into:

```
<Blade & Sorcery>/BladeAndSorcery_Data/StreamingAssets/Mods/
```

Then start the game and check the Mods page in the book for **Mech Suit**.

## Project layout

```
Mod/MechSuit/manifest.json      mod metadata the game reads
src/MechMod/MechMod.csproj      build + auto-copy into Mod/MechSuit
src/MechMod/MechOptions.cs      in-game Mod Options (all tunables)
src/MechMod/MechSuitScript.cs   suit state + thrusters, slam, power fists
```

## Ideas for later

- A custom mech armor model or cockpit HUD (needs the B&S Unity SDK and an addressable bundle).
- Sound and particle effects for the thrusters, slam, and punches.
- A shoulder-mounted rocket launcher spell.
