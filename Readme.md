# The Simpsons Game (2007) Asset Extraction Tool

## Overview

This project aims to automate the process of extracting and converting assets from the game "The Simpsons Game" (2007) PS3. It provides a step-by-step, automated workflow to take the raw game files and transform them into usable formats for modding, analysis, or archival purposes.

## Features

* **Automated Initialization:** Sets up the necessary environment and tools for the extraction process.
* **Asset Conversion:** Converts extracted files (audio, video, textures, 3d models) into modern compatible formats.

"Initializes",            # gets everything ready
"QuickBMS STR",           # extracts content from STR archive files
"Rename Folders",         # renames the main output folders to be more human readable
"Flatten Directories",    # flattens the directory structure
"Audio/Video Conversion", # converts audio and video files to a usable format
"Assets to Blender",      # uses blender to convert the assets to a usable format .glb
"Noesis txd extraction"   # extracts textures from the .txd files using Noesis, broken for now

## Prerequisites

Before you begin, ensure you have the following installed and set up:

* **Operating System:** only tested on windows
* **PowerShell:** only tested on PowerShell 7.5.0
* **.NET:** only tested on .NET 9.0.201
* **dotnet-script:** ```dotnet tool install -g dotnet-script```
* **Python:** good question
* **QuickBMS:** [\[Download QuickBMS as it's a core dependency\]](https://aluigi.altervista.org/quickbms.htm)
* **Noesis:** [\[Download Noesis as required for textures\]](https://richwhitehouse.com/index.php?content=inc_projects.php&showproject=91)
* **Blender:** must Download [blender 4.3.2](https://download.blender.org/release/Blender4.3/) exactly as the plugin is old and i cannot figure out why it stopped working, and it works on my machine
* **Game Files:** You will need a copy of "The Simpsons Game" game files for the target platform.
* **ffmpeg:** this is used for conversion of proprietary video .vp6 into (.ogv, or any others), use ```winget install ffmpeg``` from https://www.gyan.dev/ffmpeg/builds/#libraries, https://www.ffmpeg.org/download.html
* **vgmstream-cli:** Download [\[r1980\]](https://github.com/vgmstream/vgmstream/releases/tag/r1980) this is used for conversion of proprietary audio .snu into (.ogg, or any others)
* **WinRar:** used by the initializer to automatically extract the iso file


## Getting Started
## Usage

The project initialization, asset extraction and conversion process is automated through a main script. To start the process, run:

```powershell
.\main.ps1
```




### `GameFiles\Main\PS3_GAME\USRDIR`

This directory mirrors the `USRDIR` directory from the PS3 game's file structure. extracted from the iso
It contains the original game assets, including audio, video, character models, and level data, all stored in various proprietary formats.

Extension       | Percent   | Size          | Allocated     | Files   | File Type
----------------|-----------|---------------|---------------|---------|----------------------------------------------
.snu            | 20.934    | 1,235,787,472 | 1,250,942,976 | 7,413   | Audio files
.str            | 20.215    | 1,193,330,688 | 1,193,910,272 | 490     | Archive files for assets, textures, and unknowns
.vp6            | 24.070    | 1,420,889,368 | 1,420,972,032 | 42      | Videos for pre-animated cutscenes
.mus            | 34.542    | 2,039,098,368 | 2,039,132,160 | 17      | Unknown
.lua            | 0.001     | 87,824        | 94,208        | 3       | Lua source file
.bin            | 0.238     | 14,073,144    | 14,073,856    | 1       | BIN file
.txt            | 0.000     | 73            | 0             | 1       | Text document



### `GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT`

After running the `[2] QuickBMS STR`, this directory will be populated with the contents extracted from the game's `.str` archive files.
These archives contain a wide variety of game assets, and extracting them is a crucial step in accessing the individual files.

Extension       | Percent  | Size          | Allocated     | Files | File Type                        |
----------------|----------|---------------|---------------|-------|----------------------------------|
.vfb            | 0.390    | 7,079,628     | 10,629,120    | 8,770 |
.preinstanced   | 40.562   | 1,095,487,331 | 1,106,362,368 | 5,533 | 3D Assets 
.ps3            | 12.351   | 328,796,260   | 336,875,520   | 4,962 |
.xml            | 0.092    | 1,751,756     | 2,519,040     | 2,617 |
.txd            | 26.034   | 708,266,804   | 710,103,040   | 858   | Texture Dictionaries 
.lh2            | 0.242    | 5,698,228     | 6,590,464     | 594   |
.alb            | 0.031    | 348,224       | 843,776       | 529   |
.ctb            | 0.042    | 518,464       | 1,134,592     | 528   |
.dat            | 0.240    | 6,320,692     | 6,537,216     | 435   |
.graph          | 0.577    | 15,260,938    | 15,749,120    | 425   |
.rcm_b          | 0.000    | 63,376        | 4,096         | 346   |
.sbk            | 15.300   | 416,991,600   | 417,337,344   | 156   |
.bsp            | 0.080    | 1,973,507     | 2,191,360     | 113   |
.smb            | 0.057    | 1,440,576     | 1,560,576     | 99    |
.mib            | 0.013    | 221,652       | 348,160       | 89    |
.amb            | 3.551    | 96,751,848    | 96,849,920    | 58    |
.uix            | 0.360    | 9,719,256     | 9,830,400     | 54    |
(No Extension)  | 0.001    | 9,911         | 16,384        | 47    |
.imb            | 0.026    | 615,440       | 720,896       | 44    |
.toc            | 0.003    | 51,288        | 94,208        | 25    |
.msb            | 0.007    | 136,140       | 180,224       | 19    |
.inf            | 0.003    | 75,525        | 90,112        | 12    |
.aub            | 0.001    | 14,664        | 16,384        | 2     |
.bin            | 0.038    | 1,025,512     | 1,028,096     | 2     |
.txt            | 0.000    | 158           | 0             | 2     |



### `GameFiles\Main\PS3_GAME\Flattened_OUTPUT`

The QuickBMS extraction process results in a deeply nested directory structure.
because i don't know how to fix it, i made this

Run `[4] Flatten Directories`

Due to issues with Windows and Blender path length limitations and to simplify tree structure, the files are copied into a "flattened" structure. The original directory hierarchy is encoded into the folder names, allowing the files to be organized as they were originally while avoiding excessive nesting.



### `GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Video_Movies_3`

This directory is the destination for converted video files. The original `.vp6` video files are converted to `.ogv` format.



### `GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3`

This directory is the destination for converted audio files. The original `.snu` audio files are converted to `.ogg` format.

The structure of the folders within the audio folder is a bit complicated, as its designed for a GameEngine 'RenderWare'.
Each folder is oddly named 
 some with its purpose others a character's name but only the last two letters of there shortened name:

* `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3\ak_xxx_0\d_shak_xxx_000639c.exa.ogg`: This is a voice line for the Shakespeare character, `shak` ending with 'ak', so it's in the `ak_xxx_0` folder.
* `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3\ak_xxx_0\d_snak_xxx_0006263.exa.ogg`: This is a voice line for the Snake character, `snak` ending with 'ak', so it's in the `ak_xxx_0` folder.

all character names are shortened and the folders are grouped by the last two letters of the shortened names
and some folders are for ambient sound
* `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3\amb_airc\moh_amb_aircraft_carrier_qd.exa.ogg`: This is a war zone ambient sound.

there are 169 folders here
**TODO**: They need to be renamed to be more clear on who they're for and regrouped, by character or purpose.

More info in [Audio.md](./Audio.md)



### `GameFiles\Main\PS3_GAME\Assets_Blender_OUTPUT`

takes about 20mins to convert 5337 of 5533 models
current success rate is 96.4%
reasons for failure are unknown at this time, could be bad plugin, bad model, or bad script, basically every step of the process



This directory contains the final output of the asset conversion pipeline. The `.preinstanced` 3D model files are imported into Blender, processed using custom scripts, and then exported as `.glb` files, a widely supported format for 3D models.



### `tools\blender`

This directory houses the Blender-related scripts and assets that automate the 3D model conversion process. The `main.py` script orchestrates the import, processing, and export of the `.preinstanced` files, while the `io_import_simpson_game_ScriptMode.py` script provides the functionality to import the `.preinstanced` file format into Blender.



### `tools\process`

This directory contains the PowerShell scripts that automate the various steps of the asset extraction and conversion pipeline. The `main.ps1` script serves as the central control point, allowing you to select and execute the individual steps. The other scripts in this directory handle specific tasks, such as renaming folders, extracting files from archives, and preparing assets for import into Blender.

---




## Legal Disclaimer

**Please read this disclaimer carefully before using this software.**

This project provides code that automates the process of extracting assets (3D models, sounds, and videos) from the PlayStation 3 version of "The Simpsons Game" (released in 2007). It is intended for personal use only, specifically for hobby projects, learning purposes, and research into the game.

**Key Points to Understand:**

* **Requires Ownership of the Game:** This tool requires you to possess your own legally obtained ISO copy of "The Simpsons Game" for the PlayStation 3. It does not provide access to the game files themselves.
* **Automation of Existing Tools:** This code automates the use of existing, publicly available software designed for file extraction and conversion. Notably, it utilizes:
    * **QuickBMS:** For extracting assets from the game's .STR archive files, using a script for "The Simpsons Game" that is publicly available and considered an official script by the creator of QuickBMS.
    * **FFmpeg and VGMStream:** Standard, open-source tools for converting extracted audio and video files.
    * **Blender and a Python Extension:** For converting extracted 3D models to the GLB format, utilizing freely available software and a random Extension.
    * **Noesis:** While texture extraction is mentioned, please note that this currently requires manual steps using the Noesis tool, which is separate from this automated process.
* **Respect Copyright:** The assets extracted from "The Simpsons Game" are copyrighted by Electronic Arts (EA) and Disney. This tool is provided solely to facilitate personal exploration and modification of assets from a game you legally own. You are solely responsible for ensuring your use of these assets complies with all applicable copyright laws and the game's End User License Agreement (EULA) or Terms of Service.
* **No Distribution of Assets:** This project does not involve the distribution of any copyrighted game assets. It only provides the code to automate the extraction process from your own game files.
* **Compliance with Takedown Requests:** The developer of this project respects the intellectual property rights of EA and Disney. If either Electronic Arts or Disney (or their legal representatives) requests the removal of this code, the developer will promptly comply.

**By using this software, you acknowledge that you have read and understood this disclaimer and agree to use it responsibly and in accordance with all applicable laws and terms of service.**

# DO NOT USE THIS FOR COMMERCIAL PURPOSES OR DISTRIBUTE ANY EXTRACTED ASSETS WITHOUT PERMISSION FROM THE COPYRIGHT HOLDERS.
# NOTE: THESE NO CHANCE THEY WILL EVER GRANT PERMISSION

The Simpsons characters themselves are primarily owned by The Walt Disney Company.

Specifically, the characters were created by Matt Groening, and the television show "The Simpsons" was originally produced by Gracie Films and 20th Century Fox Television (which is now part of Disney Television Studios). Through its acquisition of 21st Century Fox, Disney now holds the primary ownership of the copyright and trademarks associated with The Simpsons franchise, including the characters.

While Electronic Arts (EA) created the specific digital representations of these characters as assets within "The Simpsons Game, PS3 and Xbox360" the underlying intellectual property rights to the characters themselves belong to Disney. EA had a license to use these characters and the Simpsons brand to develop and publish their game

so unless you want to be sued by Disney and EA, don't distribute any extracted assets ever not for free, for monet or for fun, not even to your friends. these all constitute copyright infringement and are illegal just about everywhere in the world.

