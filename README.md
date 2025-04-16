# DeadlockDemoResearch

Prototyping automated high-level analysis of **"demo files"**: full records of events that occur during an online multiplayer match of Valve's new MOBA-genre video game, ***Deadlock*** (2024).

> Not familiar with MOBAs or gaming jargon? Check out my short explainer! ["What is *Deadlock*?"](https://github.com/cmorley191/SchemaGenDemofileNet-cm191/blob/main/what_is_deadlock.md)

## Usage

This is currently the early stages of a research project, so it is subject to rapid changes and experimental code. The goal right now is to get a strong foothold in the file format - not write an efficient API for it.

Nevertheless, the main C# program is a usable interactive CLI for exploring any .dem files you have downloaded.

- Clone the project with submodules:
```
git clone --recurse-submodules <url>
```

- Open it in Visual Studio 2022, and run it. 

- Select a demo file; parsing it will take a few minutes.

- The current commands (explained by the program's output) dump parsed data in json format, which you can explore with your favorite json tool.

#### demo files

You can download .dem files for input either by
- selecting your profile in Deadlock's main menu, and selecting a recently played match,
- or by entering a specific match id (e.g. for a Twitch streamer's match) in Deadlock's main menu "Watch" tab

Find your downloaded demos in `C:\Program Files\Steam\steamapps\common\Deadlock\game\citadel\replays`

## Novel Features

(...beyond the capabilities of the underlying parser: [saul/demofile-net](https://github.com/saul/demofile-net))

- making sense of Deadlock's confusing event timestamps ([Frame.cs](DeadlockDemoResearch/DataModels/Frame.cs)), e.g. handling in-game time != replay time != replay file time, handling pauses, etc.
- correlating separate entities into a cohesive model, e.g. unified Troopers/minions (`dump_troopers`) and soul-urns (`dump_urns`)
- identifying interpretable concepts, e.g. turning a tower's x,y,z coordinates into location enums like "Left-hand Tier 3 Tower for Sapphire Flame team"

### Heavy **Defensive Programming**
The project incorporates *extremely frequent* assertions of properties in the data, both related to the project's analysis/output, and unrelated to it! 

The purpose of this philosophy is to prevent small edge cases and undetected changes in Deadlock's game logic (which are created by Valve all the time) from remaining undetected/unconsidered and turning into complex problems in a deep layer of the analysis. You'll find exceptions being thrown for nit-picky conditions *everywhere* in this project; the point is to identify and address invalidated or incomplete assumptions as soon as possible every time a new concept is encountered (either due to a new game update, or a never-before-seen edge case).

## Acknowledgements

This research project is only possible due to the generously shared work of the *Deadlock* and wider Source 2 game engine communities.

- The ever-helpful fellow analysts of the Deadlock Dev Community Discord server
- [saul/demofile-net](https://github.com/saul/demofile-net) -- an amazing C# lower-level demo parser that is the essential foundation of this project
- [blukai/haste-inspector](https://github.com/blukai/haste-inspector) -- an incredible UI for blukai's C++ demo parser that has been indispensable in exploring the complexities of the demo format
- [neverlosecc/source2sdk](https://github.com/neverlosecc/source2gen) -- a C++ game file reader that helps my demofile-net fork stay current with the latest *Deadlock* game code updates
- The countless other projects and community members listed in *those* projects' Acknowledgement sections.