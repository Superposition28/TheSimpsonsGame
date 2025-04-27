### `PS3_GAME\USRDIR`

The GameFiles
stored as:

| Extension       | Percent   | Files   | Assumed Purpose                                   | Known Purpose                                     | Summary                                                    |
| --------------- | --------- | ------- | ------------------------------------------------- | ------------------------------------------------- | ---------------------------------------------------------- |
| .exa.snu        | 20.778    | 29,430  | Audio files                                       | Audio files                                       | Size: 2,379,597,360 bytes (Allocated: 2,439,372,800 bytes) |
| .str            | 10.459    | 550     | Archive format                                    | archive containing main game assets/files         | Size: 1,197,807,616 bytes (Allocated: 1,198,415,872 bytes) |
| .vp6            | 50.835    | 172     | Videos for pre-animated cutscenes                 | "Movies"                                          | Size: 5,821,886,960 bytes (Allocated: 5,822,251,008 bytes) |
| .mus            | 17.805    | 17      | Unknown                                           |                                                   | Size: 2,039,098,368 bytes (Allocated: 2,039,132,160 bytes) |
| .lua            | 0.001     | 3       | Lua source file                                   |                                                   | Size: 87,824 bytes (Allocated: 94,208 bytes)               |
| .bin            | 0.123     | 1       | BIN file                                          |                                                   | Size: 14,073,144 bytes (Allocated: 14,073,856 bytes)       |
| .txt            | 0.000     | 1       |                                                   |                                                   | Size: 128 bytes (Allocated: 0 bytes)                       |


### `GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT`

After running the `[2] QuickBMS STR`, this directory will be populated with the contents extracted from the game's `.str` archive files.
These archives contain a wide variety of game assets, and extracting them is a crucial step in accessing the individual files.

|Extension        || Percent | Files | Assumed Purpose            | Known Purpose                 | Summary                                                        |
| --------------- | -------  | ------| -------------------------- | ----------------------------- | -------------------------------------------------------------- |-|
| .vfb            || 0.390   | 8,770 | RenderWare Visual Effects  | —                             | Examine headers, consult RenderWare/PS3 communities.           |
| .preinstanced   || 40.562  | 5,533 | Compressed 3D Assets       | 3D Assets                     | Investigate RenderWare compression methods, use Blender plugin.|
|| .rws.ps3.preinstanced |   | 3,499 | Compressed 3D Assets       | 3D Assets                     | RenderWare Scene, static assets ie: world chunks or props      |
|| .dff.ps3.preinstanced |   | 2,034 | Compressed 3D Assets       | 3D Assets                     | Dynamic Fragment Format, dynamic assets ie: Character models or complex props |
| .ps3            || 12.351  | 4,962 | Unknown (Asset/Metadata)   | —                             | Examine headers, compare content.                              |
|| .rcb.ps3       |          | 1,226 |                            |                               |                                                                |
|| .bnk.ps3       |          | 1,213 |                            |                               |                                                                |
|| .hko.ps3       |          | 1,021 |                            |                               | Havok Physics file                                             |
|| .hkt.ps3       |          | 878   |                            |                               |                                                                |
|| .acs.ps3       |          | 267   |                            |                               |                                                                |
|| .xml.ps3       |          | 153   |                            |                               |                                                                |
|| .bbn.ps3       |          | 110   |                            |                               |                                                                |
|| .tox.ps3       |          | 77    |                            |                               |                                                                |
|| .shk.ps3       |          | 16    |                            |                               |                                                                |
|| .cec.ps3       |          | 1     |                            |                               |                                                                |
| .xml            || 0.092   | 1     | Configuration Data         | —                             | Inspect content for recognizable structures.                   |
|| .meta.xml      |          | 2,616 |                            |                               |                                                                |
| .txd            || 26.034  | 858   | Texture Dictionaries       | Texture Dictionaries          | Use Noesis with Python plugin.                                 |
| .lh2            || 0.242   | 554   | Game Text Strings          | —                             | Try LHA decompression tools.                                   |
|| .en.lh2        |          | 20    |                            |                               |                                                                |
|| .ss.lh2        |          | 20    |                            |                               |                                                                |
|| .fr.lh2        |          | 20    |                            |                               |                                                                |
|| .es.lh2        |          | 20    |                            |                               |                                                                |
|| .it.lh2        |          | 20    |                            |                               |                                                                |
| .alb            || 0.031   | 529   | Unknown (Likely Audio)     | —                             | Broaden search for PS3 audio formats.                          |
| .ctb            || 0.042   | 528   | Unknown                    | —                             | Consider RenderWare plugins or custom data.                    |
| .dat            || 0.240   | 434   | Unknown (Generic Data)     | —                             | Examine headers, try generic archive tools.                    |
| .graph          || 0.577   | 425   | Unknown (Graph Data)       | —                             | Explore RenderWare scene graph documentation.                  |
| .rcm_b          || 0.000   | 346   | Unknown                    | —                             | Continue searching PS3 file format databases.                  |
| .sbk            || 15.300  | 157   | Unknown (Likely Audio)     | —                             | Research sound data formats in PS3 games.                      |
| .bsp            || 0.080   | 113   | Unknown (Level Geometry?)  | —                             | Investigate RenderWare support for .bsp or similar formats.    |
| .smb            || 0.057   | 99    | Unknown (Model Data?)      | —                             | Research game model formats on PS3.                            |
| .mib            || 0.013   | 89    | Unknown (Likely Audio)     | —                             | Research audio formats recognized by VGMStream.                |
| .amb            || 3.551   | 58    | Unknown (Likely Audio)     | —                             | Research ambient sound formats.                                |
| .uix            || 0.360   | 54    | Unknown (UI Related)       | —                             | Investigate UI middleware used in PS3 games.                   |
| (No Extension)  || 0.001   | 47    | Unknown                    | —                             | Examine headers, use file identification tools.                |
| .imb            || 0.026   | 44    | Unknown                    | —                             | Broaden search for PS3 game asset formats.                     |
| .toc            || 0.003   | 20    | Unknown (Index)            | —                             | Analyze content for file offsets or names.                     |
| .str.occ.toc    |          | 5     |                            |                               |                                                                |
| .msb            || 0.007   | 19    | Unknown (Likely Audio)     | —                             | Confirm association with EA Redwood Shores' audio format.      |
| .inf            || 0.003   | 12    | Unknown (Information)      | —                             | Examine content for metadata or setup instructions.            |
| .aub            || 0.001   | 2     | Unknown                    | —                             | Broaden search for PS3 game asset formats.                     |
| .bin            || 0.038   | 2     | Unknown                    | —                             | Explore generic binary format tools.                           |
| .hud.bin        |          | 2     |                            |                               |                                                                |
| .txt            || 0.000   | 2     | Unknown                    | —                             | Likely placeholder or leftover debug text.                     |



#### `.hko.ps3` — Havok Physics Files (PS3 Version)

**Purpose:**

The `.hko.ps3` files define physics setups for characters, props, and other objects in the game using the Havok Physics Engine. They contain all necessary data for how an object interacts physically within the world — including collision detection, joint constraints, movement behaviors, and material properties.

These files are specifically formatted for the PlayStation 3 architecture to optimize performance (e.g., memory layout, endian formats).

**Context:**

This game uses RenderWare for graphics and world rendering.

It integrates Havok 4.1.0-r1 as its physics simulation middleware.

`.hko.ps3` files serve as the bridge between RenderWare-rendered objects and Havok-simulated physical behavior.

**Example path:**

...\chars++lisa_hog\bound++export\lisa_hog.hko.PS3

Here, `lisa_hog.hko.ps3` likely contains the complete physics setup for the character Lisa, defining how her body reacts to forces, impacts, and movement.

**Contents:**

Analysis of `.hko.ps3` file internals shows it contains:

*   **Physics Bodies**

	`hkRigidBody`, `hkCollidable`, `hkShape`, `hkSphereShape`, `hkConvexShape`

	Define the physical shapes and collision volumes for parts of the object (e.g., limbs, torso, props).
*   **Constraints & Joints**

	`hkConstraintData`, `hkConstraintAtom`, `hkModifierConstraintAtom`, `hkConstraintInstance`

	Define joints (shoulders, elbows, knees) and rules for how connected bodies can move relative to each other.
*   **Motion & Movement**

	`hkMotion`, `hkKeyframedRigidMotion`, `hkMaxSizeMotion`, `hkMotionState`

	Describe how each body part moves, including keyframed (animated) motion or fully simulated physics motion (e.g., ragdolls).
*   **Physics Systems**

	`hkPhysicsSystem`, `EAPhysicsSystem`

	Group multiple bodies and constraints into complete physical "systems" (e.g., a character's full body or a destructible object).

	`EAPhysicsSystem` suggests custom EA extensions to base Havok functionality.
*   **Materials**

	`hkMaterial`, `EAMaterial`

	Define physical properties like friction and bounciness (important for realism when surfaces interact).
*   **Properties & Metadata**

	`hkProperty`, `hkPropertyValue`

	Store additional settings for rigid bodies, shapes, and materials.
*   **Naming & Tagging**

	Found strings like `CHARACTER`, `MODEL`, `reference`, `AITrajectory`, `JO_shoulder`, `JO_elbow`, `JO_wrist`, and `ignore_DynamicObject`

	These link physics parts to animation bones or AI systems and control behaviors like collision ignoring.

**Key Takeaways:**

*   `.hko.ps3` files are essential for physics simulation in the game.
*   Without them, characters and objects would either not react physically or behave incorrectly (e.g., no ragdoll on death, no proper collision).
*   Files are tied closely to RenderWare models, character skeletons, and game-specific animations.
*   They ensure physical realism and gameplay interactions are accurate and efficient on PS3 hardware.

**Additional Notes:**

The `.ps3` suffix means the data is already optimized for PlayStation 3: possibly endian-corrected, tightly memory-aligned, and tailored for the console's SPUs (co-processors).

Full parsing of `.hko.ps3` files would require either:

*   Havok 4.1.0-r1 SDK tools (very rare now)
*   Custom reverse engineering based on public Havok 4.x documentation
