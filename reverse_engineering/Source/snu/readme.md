# Handling .snu Audio Files

Modules\Audio\Tools\process\Main.py file contains tools for processing `.snu` audio files found in The Simpsons Game.

## Conversion

The recommended tool for converting `.snu` files is **vgmstream-cli**. It can convert `.snu` files to more common formats like `.wav` nativly.

```pwsh
vgmstream-cli.exe -o "audio.wav" "audio.snu"
```

**File Extension:** `.snu`

**Origin:** Electronic Arts (EA), particularly associated with titles from EA Redwood Shores/Visceral Games studios (e.g., Dead Space, Dante's Inferno, The Godfather 2) and other EA games like The Simpsons Game.

**Purpose:**
The SNU format acts as a container or wrapper for Electronic Arts audio streams. It typically encapsulates audio data structured according to either the older EA SNR/SNS format or the newer EA SPS format, both of which fall under the broader EA EAAC (Electronic Arts Audio Compression) family.

**Structure Overview:**
An SNU file consists of a small header followed by the main audio data block. The header provides basic information and, critically, the offset to the start of the actual audio stream data (or a secondary header).

**Header Format:**
The initial bytes of the SNU file form its header. The exact interpretation can vary slightly, but the structure observed by vgmstream is as follows:

*   **Endianness:** The 4-byte integer fields within the header (offsets `0x04`, `0x08`, `0x0C`) can be either Big Endian (BE) or Little Endian (LE), depending on the original platform of the game (e.g., PS3/Xbox 360 often use BE, PC often uses LE). The vgmstream implementation often uses the value at offset `0x08` to deduce the correct endianness.

*   **Fields:**
    *   `0x00` (1 byte): Unknown. Potentially related to sample rate (e.g., observed value `0x03` might correlate with 48000 Hz).
    *   `0x01` (1 byte): Unknown. Possibly flags or a count. Its value might influence whether the field at `0x0C` is used or if extra data exists before the main audio block.
    *   `0x02` (1 byte): Unknown. Often observed to be `0x00`.
    *   `0x03` (1 byte): Usually indicates the number of audio channels. However, it might sometimes be `0`, requiring the channel count to be determined from the contained SNR/SPS header.
    *   `0x04` (4 bytes, BE/LE): A size value. The exact meaning is unclear, but vgmstream notes it might relate to the number of audio frames (`size >> 2 ~= number of frames`).
    *   `0x08` (4 bytes, BE/LE): **Start Offset**. This is a crucial field indicating the byte offset within the SNU file where the main audio data block (or its header) begins.
    *   `0x0C` (4 bytes, BE/LE): Optional Sub-offset. Used in some cases (potentially indicated by the byte at `0x01`). Its exact purpose isn't fully detailed but might point to secondary data (e.g., observed value `0x20`).

**Internal Data Structure Variants:**
The SNU header primarily serves to locate the main audio stream. The format of that stream depends on the specific game/version. vgmstream identifies two main variants based on the first byte found at the Start Offset (read from `0x08`):

*   **Contained SPS Structure:**
    *   **Identifier:** The byte at Start Offset is `0x48` (ASCII 'H'). This 'H' likely signifies the start of an EA SPS header (`EAAC_BLOCKID1_HEADER`).
    *   **Parsing:** The SNU header is used to find this location. The subsequent data starting from Start Offset is then parsed as an EA SPS stream.
    *   **Example:** Observed in Dead Space 3 (PC).

*   **Contained SNR/SNS Structure:**
    *   **Identifier:** The byte at Start Offset is not `0x48`.
    *   **Parsing:** This indicates an older structure. vgmstream assumes:
        *   An SNR header is located at a fixed offset within the SNU file (typically `0x10`).
        *   The SNS audio body begins at the Start Offset read from the SNU header at `0x08`.
        *   The data is then parsed according to the EA SNR/SNS format rules.
    *   **Example:** Observed in The Simpsons Game (as shown in the vgmstream-cli output).

**Audio Codec:**
The SNU file itself is just a container. The actual audio encoding is determined by the contained SNR/SNS or SPS data. Common codecs include:

*   **EA-XAS ADPCM:** (Electronic Arts Extended Audio Scheme) Often 4-bit ADPCM, as seen in the provided examples (Electronic Arts EA-XAS 4-bit ADPCM v1).
*   Other EAAC codecs (potentially EA-XA, EA Layer 3, etc., depending on what the specific SNR/SPS version supports).

**Metadata:**
Metadata such as sample rate, channels, duration, total samples, layout type (e.g., blocked (EA SNS)), and encoding format are derived by parsing both the SNU header and, more importantly, the embedded SNR/SNS or SPS header and data.

**Example Usage (vgmstream-cli):**
The provided examples show vgmstream-cli successfully parsing `.snu` files from The Simpsons Game:

```
# Example 1
> vgmstream-cli.exe -m .\d_as01_xxx_0003bb5.exa.snu
metadata for .\d_as01_xxx_0003bb5.exa.snu
sample rate: 48000 Hz
channels: 1
stream total samples: 54901 (...)
encoding: Electronic Arts EA-XAS 4-bit ADPCM v1
layout: blocked (EA SNS)
metadata from: Electronic Arts SNU header
...

# Example 2
> vgmstream-cli.exe -m .\d_as01_xxx_000580c.exa.snu
metadata for .\d_as01_xxx_000580c.exa.snu
sample rate: 48000 Hz
channels: 1
stream total samples: 67200 (...)
encoding: Electronic Arts EA-XAS 4-bit ADPCM v1
layout: blocked (EA SNS)
metadata from: Electronic Arts SNU header
...
```

These examples confirm the use of the SNR/SNS variant within the SNU container for this game, using 48kHz mono EA-XAS ADPCM audio.