# The Simpsons Game (2007) Asset Extraction Tool

## Overview

This project aims to automate the process of extracting and converting assets from the game "The Simpsons Game" (2007) PS3. It provides a step-by-step, automated workflow to take the raw game files and transform them into usable formats for hobby projects, learning purposes, and research.

## Features

*   **Automated Initialization:** Sets up the necessary environment and tools for the extraction process.
*   **Asset Conversion:**         Converts extracted files (audio, video, textures, 3D models) into modern compatible formats.
*   "Initializes",                gets everything ready
*   "QuickBMS STR",               extracts content from STR archive files
*   "Rename Folders",             renames the main output folders to be more human-readable
*   "Flatten Directories",        flattens the directory structure
*   "Audio/Video Conversion",     converts audio and video files to a usable format
*   "Assets to Blender",          uses Blender to convert the assets to a usable format (.glb)
*   "Noesis TXD extraction"       extracts textures from the .txd files using Noesis, broken for now

## Getting Started


## Prerequisites

Before you begin, ensure you have the following installed and set up:

*   **Operating System:** only tested on Windows, should be possible on Mac/Linux with minimal changes
*   **PowerShell:** only tested on PowerShell 7.5.0
*   **.NET:** only tested on .NET 9.0.201
*   **dotnet-script:** ```dotnet tool install -g dotnet-script```
*   **Python:** Required for some Noesis plugins. Ensure Python is installed and added to your system's PATH.
*   **QuickBMS:** [Download QuickBMS](https://aluigi.altervista.org/quickbms.htm) as it's a core dependency.
*   **Noesis:** [Download Noesis](https://richwhitehouse.com/index.php?content=inc_projects.php&showproject=91) as required for textures.
*   **Blender:** Download [Blender 4.0.2](https://download.blender.org/release/Blender4.0/). Blender 4.0 or older (down to 2.8) is required for the conversion to work.
*   **Game Files:** You will need a copy of "The Simpsons Game" game files for the target platform.
*   **ffmpeg:** This is used for conversion of proprietary video .vp6 into (.ogv, or any others). Use ```winget install ffmpeg``` from [https://www.gyan.dev/ffmpeg/builds/#libraries](https://www.gyan.dev/ffmpeg/builds/#libraries) or [https://www.ffmpeg.org/download.html](https://www.ffmpeg.org/download.html).
*   **vgmstream-cli:** Download [vgmstream-cli r1980](https://github.com/vgmstream/vgmstream/releases/tag/r1980). This is used for conversion of proprietary audio .snu into (.wav, or any others).
*   **WinRar:** Used by the initializer to automatically extract the ISO file.


The instructions assume a certain level of familiarity with tools like PowerShell, Blender, and Noesis, which might not be accessible to all users.

Recommendations for Improvement:

    Add visual aids (e.g., screenshots or diagrams) to illustrate the workflow.
    Include a troubleshooting section to address potential problems users might encounter.
    Simplify or explain technical jargon for less experienced users.
    Review and enhance formatting for consistency and readability, especially in the feature list.
    Provide example outputs or demonstrations to show what users can achieve with this tool.



## Usage

Place the QuickBMS files in `Tools\quickbms` and the Noesis files in `Tools\noesis\exe`.

The project initialization, asset extraction, and conversion process are automated through a main script. To start the process, run:

```powershell
.\main.ps1
```

Then, select each option in order:

**[0] Run all scripts**

**[1] Initialize (PowerShell, `init.ps1`)**

This step ensures all programs exist. It generates `config.ini` and the `GameFiles\Main\` directory. It registers all programs needed in the config file to be accessed by subsequent scripts (currently, only the Blender script uses this). It extracts the ISO contents using WinRar into `GameFiles\Main\`. The ISO file should contain `\PS3_GAME\USRDIR\` and other unimportant files (`\PS3_UPDATE`, `PS3_DISC.SFB`).

**[2] Rename USRDIR Folders (PowerShell, `RenameFolders.ps1`)**

This renames the directories in the game's root folder, as their original names are not descriptive. There are two kinds of folders: Levels and Assets.

*   4 asset folders: video, audio, characters, frontend
*   18 Map folders: For some reason, a number of Game Levels are separated as more than one root Map folder.

**[3] QuickBMS STR (PowerShell, `str.ps1`)**

The game files are almost all stored in a proprietary archive folder format identified by `.str`. The `.str` extension is actually meaningless, a pattern for the RenderWare game files. The 490 STR files contain over 20,000 game files, so this step extracts these many files as a massive nested structure of folders.

**[4] Flatten Directories (PowerShell, `Flatten.ps1`)**

This slightly un-nests the STR output structure by combining folders. This step was made to fix errors with Blender not finding files in long paths, but even this only reduced the number of errors significantly.

**[5] Video Conversion (PowerShell, `4_Video.ps1`)**

Converts the weird video format to a less weird one.

**[6] Audio Conversion (PowerShell, `4_Audio.ps1`)**

Converts the weird audio format to a less weird one.

**[7] init Blender (C#, `init.csx`)**

Gets the Blender conversion process ready.

Due to the length of the directory structure, Blender often fails to locate the files. I needed a fix, so I created symbolic links (advanced shortcuts) to each file's folder instead of removing unnecessary folders from the structure. Even this only reduced the reduced number of errors.

Processes `.preinstanced` files in the nested structure and generates a 'map' of asset file paths. Creates duplicate directory structures for `.blend` and `.glb` files based on `.preinstanced` files. Copies a blank `.blend` template file into the blend structure matching the `.preinstanced` files. Creates symbolic links for `.preinstanced`, `.blend`, and `.glb` files at the root of the current drive. Saves the generated asset map to `asset_mapping.json`.

**[8] Blender Conversion (C#, `blend.csx`)**

Converts each `.preinstanced` asset into `.glb` using Blender 4.0. The plugin or script stops working with 4.4. Currently, only 5337 of 5533 assets are converted successfully; the reason for the failure is unknown.

**[9] Texture Dictionary extraction (C#, `init.csx`), (PowerShell, `copy.ps1`)**

Run option `9`, then, when completed, manually perform the following:

To extract the PNG files from TXD dictionaries:

1.  Ensure a copy of `Tools\noesis\tex_TheSimpsonsGame_PS3_txd.py` is present in `Tools\noesis\exe\plugins\python\`.
2.  Open `Noesis64.exe`.
3.  Click 'Tools', then 'Batch Process'.
4.  In the batch process window, set:

    *   Input extension: `txd`
    *   Output extension: `png`
    *   Output path: `$inpath\$inname$.txd_files\$inname$out.$outext$`
5.  Enable Recursive.
6.  Click 'Folder batch' and select the `TMP_TSG_LNKS_TXD` folder located at the root of your drive, where the prior script was executed.
7.  Then click Export.

When complete, run option `10` in the main script menu, and all PNG files should now be in `TEXTURES_OUTPUT`.



## Obligatory Legal Disclaimer

**Please read this disclaimer carefully before using this software.**

This project provides code that automates the process of extracting assets (3D models, sounds, and videos) from the PlayStation 3 version of "The Simpsons Game" (released in 2007). It is intended for personal use only, specifically for hobby projects, learning purposes, and research into the game.

**Key Points to Understand:**

*   **Requires Ownership of the Game:** This tool requires you to possess your own legally obtained ISO copy of "The Simpsons Game" for the PlayStation 3. It does not provide access to the game files themselves.
*   **Respect Copyright:** The assets extracted from "The Simpsons Game" are copyrighted by Electronic Arts (EA) and Disney. This tool is provided solely to facilitate personal exploration and modification of assets from a game you legally own. You are solely responsible for ensuring your use of these assets complies with all applicable copyright laws and the game's End User License Agreement (EULA) or Terms of Service.
*   **No Distribution of Assets:** This project does not involve the distribution of any copyrighted game assets. It only provides the code to automate the extraction process from your own game files.
*   **Compliance with Takedown Requests:** The developer of this project respects the intellectual property rights of EA and Disney. If either Electronic Arts or Disney (or their legal representatives) requests the removal of this code, the developer will promptly comply.

**By using this software, you acknowledge that you have read and understood this disclaimer and agree to use it responsibly and in accordance with all applicable laws and terms of service.**

# DO NOT USE THIS FOR COMMERCIAL PURPOSES OR DISTRIBUTE ANY EXTRACTED ASSETS WITHOUT PERMISSION FROM THE COPYRIGHT HOLDERS.
# NOTE: THERE'S NO CHANCE THEY WILL EVER GRANT PERMISSION

The Simpsons characters themselves are primarily owned by The Walt Disney Company.

Specifically, the characters were created by Matt Groening, and the television show "The Simpsons" was originally produced by Gracie Films and 20th Century Fox Television (which is now part of Disney Television Studios). Through its acquisition of 21st Century Fox, Disney now holds the primary ownership of the copyright and trademarks associated with The Simpsons franchise, including the characters.

While Electronic Arts (EA) created the specific digital representations of these characters as assets within "The Simpsons Game, PS3 and Xbox360" the underlying intellectual property rights to the characters themselves belong to Disney. EA had a license to use these characters and the Simpsons brand to develop and publish their game.

So unless you want to be sued by Disney and EA, don't distribute any extracted assets ever—not for free, for money, or for fun, not even to your friends. These all constitute copyright infringement and are illegal just about everywhere in the world.

