### `GameFiles\Main\PS3_GAME\USRDIR`

This directory mirrors the `USRDIR` directory from the PS3 game's file structure, extracted from the ISO. It contains the original game assets, including audio, video, character models, and level data, all stored in various proprietary formats.

Extension       | Percent   | Size          | Allocated     | Files   | File Type
----------------|-----------|---------------|---------------|---------|----------------------------------------------
.exa.snu        | 20.934    | 1,235,787,472 | 1,250,942,976 | 7,413   | Audio files
.str            | 20.215    | 1,193,330,688 | 1,193,910,272 | 490     | Archive files for assets, textures, and unknowns
.vp6            | 24.070    | 1,420,889,368 | 1,420,972,032 | 42      | Videos for pre-animated cutscenes
.mus            | 34.542    | 2,039,098,368 | 2,039,132,160 | 17      | Unknown
.lua            | 0.001     | 87,824        | 94,208        | 3       | Lua source file
.bin            | 0.238     | 14,073,144    | 14,073,856    | 1       | BIN file
.txt            | 0.000     | 73            | 0             | 1       | Text document


### `GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT`

After running the `[2] QuickBMS STR`, this directory will be populated with the contents extracted from the game's `.str` archive files.
These archives contain a wide variety of game assets, and extracting them is a crucial step in accessing the individual files.

|Extension|        | Percent | Files | Initial Label / Hypothesis | Known Purpose                 | Summary                                                        |
| --------------- | -------  | ------------- | ----- | -------------------------- | ----------------------------- | ---------------------------------------------------------------|-|
| .vfb            || 0.390   | 8,770 | RenderWare Visual Effects  | —                             | Examine headers, consult RenderWare/PS3 communities.           |
| .preinstanced   || 40.562  | 5,533 | Compressed 3D Assets       | 3D Assets                     | Investigate RenderWare compression methods, use Blender plugin.|
|| .rws.ps3.preinstanced |   | 3,499 | Compressed 3D Assets       | 3D Assets                     | RenderWare Scene, static assets ie: world chunks or props      |
|| .dff.ps3.preinstanced |   | 2,034 | Compressed 3D Assets       | 3D Assets                     | Dynamic Fragment Format, dynamic assets ie: Character models or complex props |
| .ps3            || 12.351  | 4,962 | Unknown (Asset/Metadata)   | —                             | Examine headers, compare content.                              |
|| .rcb.ps3       |          | 1226  |                            |                               |                                                                |
|| .bnk.ps3       |          | 1213  |                            |                               |                                                                |
|| .hko.ps3       |          | 1021  |                            |                               |                                                                |
|| .hkt.ps3       |          | 878   |                            |                               |                                                                |
|| .acs.ps3       |          | 267   |                            |                               |                                                                |
|| .xml.ps3       |          | 153   |                            |                               |                                                                |
|| .bbn.ps3       |          | 110   |                            |                               |                                                                |
|| .tox.ps3       |          | 77    |                            |                               |                                                                |
|| .shk.ps3       |          | 16    |                            |                               |                                                                |
|| .cec.ps3       |          | 1     |                            |                               |                                                                |
| .xml            || 0.092   | 2,617 | Configuration Data         | —                             | Inspect content for recognizable structures.                   |
|| .meta.xml      |          | 2,616 |                            |                               |                                                                |
| .txd            || 26.034  | 858   | Texture Dictionaries       | Texture Dictionaries          | Use Noesis with Python plugin.                                 |
| .lh2            || 0.242   | 594   | Game Text Strings          | —                             | Try LHA decompression tools.                                   |
|| .en.lh2        |          | 20    |                            |                               |                                                                |
|| .ss.lh2        |          | 20    |                            |                               |                                                                |
| .alb            || 0.031   | 529   | Unknown (Likely Audio)     | —                             | Broaden search for PS3 audio formats.                          |
| .ctb            || 0.042   | 528   | Unknown                    | —                             | Consider RenderWare plugins or custom data.                    |
| .dat            || 0.240   | 435   | Unknown (Generic Data)     | —                             | Examine headers, try generic archive tools.                    |
| .graph          || 0.577   | 425   | Unknown (Graph Data)       | —                             | Explore RenderWare scene graph documentation.                  |
| .rcm_b          || 0.000   | 346   | Unknown                    | —                             | Continue searching PS3 file format databases.                  |
| .sbk            || 15.300  | 156   | Unknown (Likely Audio)     | —                             | Research sound data formats in PS3 games.                      |
| .bsp            || 0.080   | 113   | Unknown (Level Geometry?)  | —                             | Investigate RenderWare support for .bsp or similar formats.    |
| .smb            || 0.057   | 99    | Unknown (Model Data?)      | —                             | Research game model formats on PS3.                            |
| .mib            || 0.013   | 89    | Unknown (Likely Audio)     | —                             | Research audio formats recognized by VGMStream.                |
| .amb            || 3.551   | 58    | Unknown (Likely Audio)     | —                             | Research ambient sound formats.                                |
| .uix            || 0.360   | 54    | Unknown (UI Related)       | —                             | Investigate UI middleware used in PS3 games.                   |
| (No Extension)  || 0.001   | 47    | Unknown                    | —                             | Examine headers, use file identification tools.                |
| .imb            || 0.026   | 44    | Unknown                    | —                             | Broaden search for PS3 game asset formats.                     |
| .toc            || 0.003   | 25    | Unknown (Index)            | —                             | Analyze content for file offsets or names.                     |
| .str.occ.toc    |          | 5     |                            |                               |                                                                |
| .msb            || 0.007   | 19    | Unknown (Likely Audio)     | —                             | Confirm association with EA Redwood Shores' audio format.      |
| .inf            || 0.003   | 12    | Unknown (Information)      | —                             | Examine content for metadata or setup instructions.            |
| .aub            || 0.001   | 2     | Unknown                    | —                             | Broaden search for PS3 game asset formats.                     |
| .bin            || 0.038   | 2     | Unknown                    | —                             | Explore generic binary format tools.                           |
| .hud.bin        |          | 2     |                            |                               |                                                                |
| .txt            || 0.000   | 2     | Unknown                    | —                             | Likely placeholder or leftover debug text.                     |


### `GameFiles\Main\PS3_GAME\Flattened_OUTPUT`

The QuickBMS extraction process results in a deeply nested directory structure.
Because I don't know how to fix it, I made this over-engineered script because I wanted to maintain the original structure as much as possible.

Run `[4] Flatten Directories`

Due to issues with Windows and Blender path length limitations and to simplify tree structure, the files are copied into a "flattened" structure. The original directory hierarchy is encoded into the folder names, allowing the files to be organized as they were originally while avoiding excessive nesting.



### `GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Video_Movies_3`

This directory is the destination for converted video files. The original `.vp6` video files are converted to `.ogv` format.



### `GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3`

This directory is the destination for converted audio files. The original `.snu` audio files are converted to `.wav` format.

The structure of the folders within the audio folder is a bit complicated, as it's designed for a GameEngine 'RenderWare'.
Each folder is oddly named, some with its purpose, others a character's name but only the last two letters of their shortened name:

* `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3\ak_xxx_0\d_shak_xxx_000639c.exa.wav`: This is a voice line for the Shakespeare character, `shak` ending with 'ak', so it's in the `ak_xxx_0` folder.
* `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3\ak_xxx_0\d_snak_xxx_0006263.exa.wav`: This is a voice line for the Snake character, `snak` ending with 'ak', so it's in the `ak_xxx_0` folder.

All character names are shortened, and the folders are grouped by the last two letters of the shortened names. Some folders are for ambient sound:
* `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3\amb_airc\moh_amb_aircraft_carrier_qd.exa.wav`: This is a war zone ambient sound.

There are 169 folders here.
**TODO**: They need to be renamed to be more clear on who they're for and regrouped by character or purpose.

More info in [Audio.md](./Audio.md)



### `GameFiles\Main\PS3_GAME\Assets_Blender_OUTPUT`

Takes about 20 minutes to convert 5337 of 5533 models.
Current success rate is 96.4%.
Reasons for failure are unknown at this time; could be bad plugin, bad model, or bad script—basically every step of the process.



This directory contains the final output of the asset conversion pipeline. The `.preinstanced` 3D model files are imported into Blender, processed using custom scripts, and then exported as `.glb` files, a widely supported format for 3D models.



### `Tools\blender`

This directory houses the Blender-related Python scripts that automate the 3D model conversion process.
The `main.py` script manages the import, processing, and export of a `.preinstanced` file as `.glb` file.
The `io_import_simpson_game_ScriptMode.py` script provides the functionality to import the `.preinstanced` file format into Blender.



### `Tools\process`

This directory contains the PowerShell scripts that automate the various steps of the asset extraction and conversion pipeline. The `main.ps1` script serves as the central control point, allowing you to select and execute the individual steps. The other scripts in this directory handle specific tasks, such as renaming folders, extracting files from archives, and preparing assets for import into Blender.



### `Tools\noesis\tex_TheSimpsonsGame_PS3_txd.py`

This Python plugin enables Noesis to extract the textures from the `.txd` files. I don't know who made it, as the original host site is gone, and this may be one of the few copies left.



## `Tools\blender\io_import_simpson_game_ScriptMode.py`

This Python extension for Blender enables the import of `.preinstanced` assets.

I got this from [\[Turk645\]](https://github.com/Turk645/Simpsons-Game-PS3-Blender-Plugin)
and the _fork version from [\[misternebula\]](https://github.com/misternebula/Simpsons-Game-PS3-Blender-Plugin)
made for Blender 2.8
but works up to 4.0

---

