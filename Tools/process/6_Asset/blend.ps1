param (
    [switch]$Verbose,
    [switch]$DebugSleep
)

# Example usage:
# .\run.ps1 -Debug -Verbose -noblend

$pythonScriptPath = ".\Tools\blender\main.py" # Path to the main Python script for Blender processing
$pythonextension_file = ".\Tools\blender\io_import_simpson_game_ScriptMode.py" # Path to the Python script for Blender processing

$DebugSleep = $false
$Verbose = $false

$loopCount = 0

# Path to the asset mapping JSON file
$assetMappingFile = "asset_mapping.json"

# Function to read INI file
function Read-IniFile {
    param (
        [string]$Path
    )
    try {
        # Read the file line by line
        $content = Get-Content -Path $Path -Encoding UTF8

        foreach ($line in $content) {
            # Skip lines that are not in name=value format
            if ($line -match "^\s*BlenderExePath\s*=\s*(.+)$") {
                return @{ BlenderExePath = $matches[1] }
            }
        }

        Write-Error "BlenderExePath not found in INI file: $Path"
        return $null
    } catch {
        Write-Error "Failed to read or parse INI file: $Path"
        Write-Error $_
        return $null
    }
}

# Check if the INI file exists
if (-Not (Test-Path -Path ".\config.ini")) {
	Write-Error "config.ini not found. Please ensure the file exists in the current directory."
	exit 1
} else {
	Write-Host "config.ini found." -ForegroundColor Green
}

# Read Blender path from config.ini
$config = Read-IniFile -Path ".\config.ini"

if ($config -and $config.BlenderExePath) {
    $blenderExePath = "& '$($config.BlenderExePath)'"
    Write-Host "Blender executable path from config.ini: $blenderExePath" -ForegroundColor Green
} else {
    Write-Error "BlenderExePath not found in config.ini or config.ini not found. Using default 'blender4.3'."
    $blenderExePath = "blender4.3" # Default path
}

function BlenderProcessing {
    Write-Host "Starting Blender processing using asset mapping file..." -ForegroundColor Green

    # Read the asset mapping JSON file
    try {
        $assetMapping = Get-Content -Path $assetMappingFile -Raw | ConvertFrom-Json
    } catch {
        Write-Error "Failed to read or parse the asset mapping JSON file: $assetMappingFile"
        Write-Error $_
        exit 1
    }

    # Iterate through each entry in the asset mapping
    foreach ($entryKey in $assetMapping.PSObject.Properties.Name) {
        $assetInfo = $assetMapping.$entryKey

        $loopCount += 1
        Write-Host ""
        Write-Host "Loop Count: $loopCount" -ForegroundColor Green
        Write-Host ""

        $preinstancedSymlink = $assetInfo.preinstanced_symlink
        $blendSymlink = $assetInfo.blend_symlink
        $predictedGlbSymlink = $assetInfo.predicted_glb_symlink

		Write-Host ""
        Write-Host "Preinstanced Symlink: " -ForegroundColor Magenta -NoNewline; Write-Host "$preinstancedSymlink" -ForegroundColor Blue
        Write-Host "Blend Symlink: " -ForegroundColor Magenta -NoNewline; Write-Host "$blendSymlink" -ForegroundColor Blue
        Write-Host "Predicted GLB Symlink: " -ForegroundColor Magenta -NoNewline; Write-Host "$predictedGlbSymlink" -ForegroundColor Blue
		Write-Host ""

        # Check if the corresponding .blend symlink exists
        if (Test-Path -Path $blendSymlink) {
            Write-Host "Processing: " -ForegroundColor Cyan -NoNewline; Write-Host "$preinstancedSymlink" -ForegroundColor Blue
            Write-Host "Blend File (Symlink): " -ForegroundColor Magenta -NoNewline; Write-Host "$blendSymlink" -ForegroundColor Blue
            Write-Host ""

            # Construct the output .glb file path (using the predicted symlink)
            $glbFilePath = $predictedGlbSymlink
            Write-Host "Output GLB File (Symlink): " -ForegroundColor Magenta -NoNewline; Write-Host "$glbFilePath" -ForegroundColor Blue
            Write-Host ""

            try {
                # Construct the paths for Blender arguments (using symlinks)
                $blendFilePath = $blendSymlink
                Write-Host "Blend File Path (Symlink): " -ForegroundColor Green -NoNewline; Write-Host "$blendFilePath" -ForegroundColor Blue
                Write-Host "Preinstanced File Path (Symlink): " -ForegroundColor Green -NoNewline; Write-Host "$preinstancedSymlink" -ForegroundColor Blue
				Write-Host ""

                # Extract the directory path from preinstancedSymlink  (using symlinks)
                Write-Host "Preinstanced Directory Path (Full): " -ForegroundColor Green -NoNewline; Write-Host "$preinstancedSymlink" -ForegroundColor Blue
				Write-Host ""

				# Extract the directory path from glb file path (using symlinks)
				$predictedGlbSymlink = Split-Path -Path $glbFilePath -Parent
				Write-Host "GLB Directory Path: " -ForegroundColor Green -NoNewline; Write-Host "$predictedGlbSymlink" -ForegroundColor Blue
				Write-Host ""

                # Check if the .glb symlink (target) does not exist
                if (!(Test-Path $glbFilePath)) {
                    # Check if the corresponding .preinstanced symlink exists
                    if (Test-Path $preinstancedSymlink) {
                        # Construct the command to run Blender in background mode using symlinks
                        # Convert boolean values to strings "true" or "false"
                        $verboseStr = if ($Verbose) { "true" } else { "false" }
                        $debugSleepStr = if ($DebugSleep) { "true" } else { "false" }
                        $blenderCommand = "$blenderExePath -b `"$blendFilePath`" --python `"$pythonScriptPath`" -- `"$blendFilePath`" `"$preinstancedSymlink`" `"$glbFilePath`" `"$pythonextension_file`" `"$verboseStr`" `"$debugSleepStr`""

                        Write-Host "Blender command  -->  $blenderCommand" -ForegroundColor Blue

						# Execute the command
						Write-Host "# Start Blender Output"
						$blenderOutput = Invoke-Expression $blenderCommand 2>&1
						Write-Host $blenderOutput
						Write-Host "# End Blender Output"

						# Check if an error occurred
						$errorOccurred = $blenderOutput -like "*Error:*"

						if ($errorOccurred) {
							Write-Host "Blender encountered an error:"
							# Split the output into lines
							$outputLines = $blenderOutput -split "`n"
							# Find the line containing the error message
							foreach ($line in $outputLines) {
								if ($line -like "Error:*") {
									Write-Host "  $line"
								}
							}
							# check if the blankBlenderFile exists
							if (Test-Path $blankBlenderFile) {
								Write-Host "Blank Blender file exists: $blankBlenderFile" -ForegroundColor Green
							} else {
								Write-Host "Blank Blender file does not exist: $blankBlenderFile" -ForegroundColor Red
							}
						} else {
							Write-Host "Blender executed successfully." -ForegroundColor Green
						}
						
                        #check if the output file was created successfully (target of the symlink)
                        if (Test-Path $glbFilePath) {
                            Write-Host "Output file created successfully: $glbFilePath" -ForegroundColor Green
                        } else {
                            Write-Host "Failed to create output file: $glbFilePath" -ForegroundColor Red
                        }

                    } else {
                        Write-Host "No corresponding .preinstanced symlink for: $blendFilePath" -ForegroundColor Red
                        exit 1 # break on missing input
                    }
                } else {
                    Write-Host "`e[33mSkipping: .glb exists for: $blendFilePath" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "Error processing file: $blendFilePath" -ForegroundColor Red
                Write-Host "Error message: $_" -ForegroundColor Red
                exit 1 # Exit script on error
            }
            Write-Host "Finished processing for: $blendFilePath" -ForegroundColor Blue
        } else {
            Write-Error "404 Blend symlink not found: $blendSymlink"
            exit 1 # break on missing input
        }
    }
}

Write-Host "Initializing..." -ForegroundColor Green
#ProcessPreinstancedFiles // made redundant by the new C# script
Write-Host "Blender Processing using asset_mapping.json..." -ForegroundColor Green
BlenderProcessing