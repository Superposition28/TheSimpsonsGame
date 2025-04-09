


### Step 1: Obtain the ISO

Obtain the `The Simpsons Game` ISO:

```plaintext
A:\Dev\Games\simpsons\ps3\Simpsons Game, The (USA) ps3.iso
```

Copy `F:\PS3_GAME\USRDIR` to:

```plaintext
.\GameFiles\Main\PS3_GAME\USRDIR
```


### Step 2: Run the Main Script

Run `.\tools\process\main.ps1` and select:

#### Step 1 - Directory Structure

Then select `1 - 1 Rename Folders`. This will run the `.\tools\process\Step1_renameFolders.ps1` script, which will rename all the folders in the `.\GameFiles\Main\PS3_GAME\USRDIR` directory to more concise names.

**Current Naming Scheme (Before and After):**

```plaintext
    "audiostreams"              = "Assets_Audio_Streams_3"
    "bargainbin"                = "Maps_World_BargainBin_4-10"
    "bigsuperhappy"             = "Maps_World_BigSuperHappy_4-14"
    "brt"                       = "Maps_World_Bartman_4-2"
    "cheater"                   = "Maps_World_Cheater_4-6"
    "colossaldonut"             = "Maps_World_ColossalDonut_4-8"
    "dayofthedolphins"          = "Maps_World_DayOfTheDolphins_4-7"
    "dayspringfieldstoodstill"  = "Maps_World_DaySpringfieldStoodStill_4-99"
    "eighty_bites"              = "Maps_World_EightyBites_4-3"
    "frontend"                  = "UI_Frontend_3"
    "gamehub"                   = "Maps_World_GameHub_4-10"
    "grand_theft_scratchy"      = "Maps_World_GrandTheftScratchy_4-12"
    "loc"                       = "Maps_World_LandOfChocolate_4-1"
    "medal_of_homer"            = "Maps_World_MedalOfHomer_4-13"
    "meetthyplayer"             = "Maps_World_MeetThyPlayer_4-99"
    "mob_rules"                 = "Maps_World_MobRules_4-5"
    "movies"                    = "Assets_Video_Movies_3"
    "neverquest"                = "Maps_World_NeverQuest_4-11"
    "rhymes"                    = "Maps_World_Rhymes_4-99"
    "simpsons_chars"            = "Assets_Characters_Simpsons_3"
    "spr_hub"                   = "Maps_World_SprHub_4-5"
    "tree_hugger"               = "Maps_World_TreeHugger_4-4"
```

#### Step 2 - STR Extraction


Then select `2 - 1 QuickBMS STR`. This runs the `.\tools\process\Step2_quickbms_str.ps1` script, set up to handle the "The Simpsons STR" QuickBMS script. It extracts almost all files from the `.str` archive files used by the EA game engine "RenderWare."


It extracts the following files into `.\GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT`:

| Extension        | Purpose        | Percent | Size       | Allocated  | Files |
|------------------|----------------|---------|------------|------------|-------|
| `.vfb`          | Unknown        | 0.262    |  7,079,628 | 10,629,120 | 8,770 |
| `.preinstanced` | 3D assets      | 40.594   |  1,095,487,331 | 1,106,362,368 | 5,533 |
| `.ps3`          | Unknown        | 12.184   |  328,796,260   | 336,875,520   | 4,962 |
| `.xml`          | Unknown        | 0.065    |  1,751,756     | 2,519,040     | 2,617 |
| `.txd`          | Textures       | 26.246   |  708,266,804   | 710,103,040   | 858 |
| `.lh2`          | Unknown        | 0.211    |  5698228   | 6590464    |  594 |
| `.alb`          | Unknown        | 0.013    |  348224    | 843776     |  529 |
| `.ctb`          | Unknown        | 0.019    |  518464    | 1134592    |  528 |
| `.dat`          | Unknown        | 0.234    |  6320692   | 6537216    |  435 |
| `.graph`        | Unknown        | 0.566    |  15260938  | 15749120   |  425 |
| `.rcm_b`        | Unknown        | 0.002    |  63376     | 4096       |  346 |
| `.sbk`          | Unknown        | 15.452   |  416991600 | 417337344  |  156 |
| `.bsp`          | Unknown        | 0.073    |  1973507   | 2191360    |  113 |
| `.smb`          | Unknown        | 0.053    |  1440576   | 1560576    |   99 |
| `.mib`          | Unknown        | 0.008    |  221652    | 348160     |   89 |
| `.amb`          | Unknown        | 3.585    |  96751848  | 96849920   |   58 |
| `.uix`          | Unknown        | 0.360    |  9719256   | 9830400    |   54 |
| ``              | Unknown        | 0.000    |  9911      | 16384      |   47 |
| `.imb`          | Unknown        | 0.023    |  615440    | 720896     |   44 |
| `.toc`          | Unknown        | 0.002    |  51288     | 94208      |   25 |
| `.msb`          | Unknown        | 0.005    |  136140    | 180224     |   19 |
| `.inf`          | Unknown        | 0.003    |  75525     | 90112      |   12 |
| `.aub`          | Unknown        | 0.001    |  14664     | 16384      |    2 |
| `.bin`          | Unknown        | 0.038    |  1025512   | 1028096    |    2 |
| `.txt`          | Unknown        | 0.000    |  158       | 0          |    2 |


#### Step 2 - Flatten Directories

Run `main` script and select `Step 2 - STR Extraction`, then option `2 - 2 Flatten Directories`. This will create:

```plaintext
.\GameFiles\Main\PS3_GAME\Flattened_OUTPUT
```
it is a compresses version of the file structure with a copy of all files
example this structure
```plaintext
+---Assets_Characters_Simpsons_3
|   +---GlobalFolder
|   |   +---chars
|   |   |   +---bart.str
|   |   |   |   \---bart_str
|   |   |   |       |   0000001d.dat
|   |   |   |       |   
|   |   |   |       \---build
|   |   |   |           \---PS3
|   |   |   |               \---ntsc_en
|   |   |   |                   +---assets
|   |   |   |                   |   +---chars
|   |   |   |                   |   |   \---bart
|   |   |   |                   |   |       \---cct
|   |   |   |                   |   |           \---export
|   |   |   |                   |   |                   bart.bnk.PS3
|   |   |   |                   |   |                   bartman.bnk.PS3
```
is compressed into
```plaintext
+---Assets_Characters_Simpsons_3
|   +---GlobalFolder
|   |   +---chars
|   |   |   +---bart.str_-_bart_str
|   |   |   |   |   0000001d.dat
|   |   |   |   |   
|   |   |   |   \---build_-_PS3_-_ntsc_en
|   |   |   |       +---assets
|   |   |   |       |   +---chars_-_bart_-_cct_-_export
|   |   |   |       |   |       bart.bnk.PS3
|   |   |   |       |   |       bartman.bnk.PS3
```
this is an important change as blender and windows cannot handle the massively nested folder structures generated by the quickBMS script
windows may still have some issues, ensure long paths are enabled


next run `main` 'Step 3 - Audio/Video Extraction' option '3 - 1 Audio/Video Extraction'

this gets Audio .snu from `.\GameFiles\Main\PS3_GAME\USRDIR\Assets_Audio_Streams_3`
and converts into .ogg at `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3`
and gets Video .vp6 from `.\GameFiles\Main\PS3_GAME\USRDIR\Assets_Video_Movies_3`
and converts into .ogv at `.\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Video_Movies_3`

using ffmpeg for .vp6 tp .ogv
and vgmstream-cli for .snu tp .ogg


next run `main` 'Step 4 - preinstanced Assets Extraction' option '4 - 1 Assets to Blender'

this copies a blank .blend file `.\blank.blend` into a duplicate folder structure for every folder with a .preinstanced file at `.\GameFiles\Main\PS3_GAME\Flattened_OUTPUT\blender` from `.\GameFiles\Main\PS3_GAME\Flattened_OUTPUT\`
it then runs blender4.3 with the `.\tools\blender\main.py`
it imports the .preinstanced assets extension `.\tools\blender\io_import_simpson_game_ScriptMode.py`
to then import each .preinstanced file into its corrisponding .blend file, and exports them to .glb at
`.\GameFiles\Main\PS3_GAME\Assets_Blender_OUTPUT`
example
`.\GameFiles\Main\PS3_GAME\Flattened_OUTPUT\Assets_Characters_Simpsons_3\GlobalFolder\chars\bart.str_-_bart_str\build_-_PS3_-_ntsc_en\assets\weapons\controller\bound_-_export\controller.dff.PS3.preinstanced`
is imported into
`.\GameFiles\Main\PS3_GAME\Flattened_OUTPUT\blender\Assets_Characters_Simpsons_3\GlobalFolder\chars\bart.str_-_bart_str\build_-_PS3_-_ntsc_en\assets\weapons\controller\bound_-_export\controller.dff.PS3.blend`
exported as 
`.\GameFiles\Main\PS3_GAME\Assets_Blender_OUTPUT\Assets_Characters_Simpsons_3\GlobalFolder\chars\bart.str_-_bart_str\build_-_PS3_-_ntsc_en\assets\weapons\controller\bound_-_export\controller.dff.PS3.glb`

the process repeates this making all .preinstanced files into the .glb files in 
`.\GameFiles\Main\PS3_GAME\Assets_Blender_OUTPUT`



