#region Configuration

$configFilePath = "config.ini" # Changed extension to .ini for clarity
$gameFilesMainPath = Join-Path -Path "." -ChildPath "GameFiles\Main"
$isoFileNameHint = "Simpsons Game, The (USA) ps3.iso"
$isoFilePathKey = "IsoFilePath"
$ps3GameFolderName = "PS3_GAME\USRDIR"

# Define a consistent section name for tool paths
$toolPathsSection = "ToolPaths"
$gameSettingsSection = "GameSettings"

# Define default tool locations (Windows specific - adjust as needed)
$DefaultToolPaths = @{
    "Blender"       = @(".\Tools\blender\exe\blender-4.0.2-windows-x64\blender.exe", "C:\Program Files\Blender Foundation\Blender 4.0\blender.exe", "C:\Program Files\Blender Foundation\Blender 4.1\blender.exe") # Add more versions as needed
    "Noesis"        = @(".\Tools\noesis\exe\Noesis64.exe", "C:\Noesis\noesis.exe")
    "FFmpeg"        = @("C:\ffmpeg\bin\ffmpeg.exe")
    "vgmstream-cli" = @("C:\vgmstream\vgmstream-cli.exe")
    "QuickBMS"      = @(".\Tools\quickbms\exe\quickbms.exe", "C:\QuickBMS\quickbms.exe")
}

# Define tools that support auto-install and their install commands (Windows example using winget)
$AutoInstallTools = @{
    "FFmpeg"                = "winget install ffmpeg -s winget"
    "vgmstream-cli"         = "winget install vgmstream -s winget" # May need a different package name
    "Microsoft.DotNet.SDK.9" = "winget install Microsoft.DotNet.SDK.9 -s winget"
    "dotnet-script"         = "dotnet tool install -g dotnet-script"
}

# Blender download information
$BlenderDownloadUrl = "https://download.blender.org/release/Blender4.0/blender-4.0.2-windows-x64.zip"
$BlenderDownloadPath = Join-Path -Path "." -ChildPath ".\Tools\blender\exe\blender-4.0.2-windows-x64.zip"
$BlenderExtractPath = Join-Path -Path "." -ChildPath ".\Tools\blender\exe"
$BlenderExecutableNameLocal = "blender.exe"

#endregion

#region Function: Get-ConfigValue

function Get-ConfigValue {
    param(
        [Parameter(Mandatory=$true)]
        [string]$key,
        [Parameter(Mandatory=$false)]
        [string]$configPath = $configFilePath,
        [Parameter(Mandatory=$false)]
        [string]$section = $null
    )

    if (-not (Test-Path $configPath)) {
        return $null
    }

    $content = Get-Content -Path $configPath -ErrorAction SilentlyContinue
    if ($section) {
        $inSection = $false
        foreach ($line in $content) {
            if ($line -ceq "[$section]") {
                $inSection = $true
            } elseif ($inSection -and $line -like "$key=*") {
                return $line.Split("=")[1].Trim()
            } elseif ($inSection -and $line -like "[*]") {
                break # Reached the next section
            }
        }
    } else {
        $line = $content | Where-Object { $_ -like "$key=*" } | Select-Object -First 1
        if ($line) {
            return $line.Split("=")[1].Trim()
        }
    }
    return $null
}

#endregion

#region Function: Get-ToolPath

function Get-ToolPath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$toolName,
        [Parameter(Mandatory=$true)]
        [string]$executableName,
        [string]$expectedVersionPrefix = $null, # Make version check optional
        [string[]]$defaultPaths = @()
    )

    Write-Host "Checking for $($toolName) executable..." -ForegroundColor Cyan

    # 1. Check default paths
    foreach ($path in $defaultPaths) {
        if (Test-Path $path -PathType Leaf) {
            Write-Host "$($toolName) found at default location: '$path'" -ForegroundColor Green
            Save-Config -key ($toolName + "ExePath") -value $path -section $toolPathsSection
            return $path
        }
    }

    # 2. Check config file
    $configuredPath = Get-ConfigValue -key ($toolName + "ExePath") -section $toolPathsSection
    if ($configuredPath) {
        if (Test-Path $configuredPath -PathType Leaf) {
            Write-Host "$($toolName) path found in config: '$configuredPath'" -ForegroundColor Green
            return $configuredPath
        } else {
            Write-Warning "Warning: Configured path for $($toolName) is invalid: '$configuredPath'. Will try other methods."
        }
    }

    # Blender specific download and extract using native PowerShell
    if ($toolName -ceq "Blender") {
        $localBlenderExePath = Join-Path -Path $BlenderExtractPath -ChildPath $BlenderExecutableNameLocal
        if (Test-Path $localBlenderExePath -PathType Leaf) {
            Write-Host "Blender found at local path: '$localBlenderExePath'" -ForegroundColor Green
            Save-Config -key ($toolName + "ExePath") -value $localBlenderExePath -section $toolPathsSection
            return $localBlenderExePath
        } else {
            Write-Host "Blender not found in default locations or config. Attempting to download and extract..." -ForegroundColor Yellow
            try {
                # Ensure the download directory exists
                $DownloadDirectory = Split-Path -Path $BlenderDownloadPath -Parent
                if (-not (Test-Path $DownloadDirectory -PathType Container)) {
                    Write-Host "Creating directory: '$DownloadDirectory'" -ForegroundColor DarkYellow
                    New-Item -Path $DownloadDirectory -ItemType Directory -Force | Out-Null
                }

                Write-Host "Downloading Blender from '$BlenderDownloadUrl' to '$BlenderDownloadPath'..." -ForegroundColor DarkYellow
                Invoke-WebRequest -Uri $BlenderDownloadUrl -OutFile $BlenderDownloadPath

                # Extract using Expand-Archive
                Write-Host "Extracting Blender to '$BlenderExtractPath' using Expand-Archive..." -ForegroundColor DarkYellow
                if (Test-Path $BlenderDownloadPath) {
                    try {
                        Expand-Archive -Path $BlenderDownloadPath -DestinationPath $BlenderExtractPath -Force
                        Write-Host "Blender extracted successfully to '$BlenderExtractPath'." -ForegroundColor Green
                        Save-Config -key ($toolName + "ExePath") -value $localBlenderExePath -section $toolPathsSection
                        return $localBlenderExePath
                    } catch {
                        Write-Warning "Error extracting Blender using Expand-Archive: $($_.Exception.Message)"
                    } finally {
                        # Clean up the downloaded zip file
                        Remove-Item -Path $BlenderDownloadPath -Force -ErrorAction SilentlyContinue
                    }
                } else {
                    Write-Warning "Error: Blender download ZIP file not found at '$BlenderDownloadPath'."
                }
            } catch {
                Write-Warning "Error during Blender download or extraction: $($_.Exception.Message)"
            }
        }
    }

    # 3. Check system path for all tools
    $pathFromEnv = Find-ToolInPath -toolName $toolName -executableName $executableName
    if ($pathFromEnv) {
        Save-Config -key ($toolName + "ExePath") -value $pathFromEnv -section $toolPathsSection
        return $pathFromEnv
    }

    # 4. Auto-install option (for supported tools)
    if ($AutoInstallTools.ContainsKey($toolName) -and $toolName -notin "Blender") {
        $autoInstallChoice = Read-Host "Executable for $($toolName) not found. Do you want to attempt auto-install? (y/N)"
        if ($autoInstallChoice -ceq "y") {
            $installCommand = $AutoInstallTools[$toolName]
            Write-Host "Attempting to auto-install $($toolName) using command: '$installCommand'" -ForegroundColor Yellow
            try {
                Invoke-Expression $installCommand
                # Check if the tool is now in the PATH (most likely for winget installs)
                $pathFromEnvAfterInstall = Find-ToolInPath -toolName $toolName -executableName $executableName
                if ($pathFromEnvAfterInstall) {
                    Write-Host "$($toolName) auto-installed and found in path: '$pathFromEnvAfterInstall'" -ForegroundColor Green
                    Save-Config -key ($toolName + "ExePath") -value $pathFromEnvAfterInstall -section $toolPathsSection
                    return $pathFromEnvAfterInstall
                } else {
                    Write-Warning "Auto-install of $($toolName) completed, but the executable was not found in the system path. You might need to add it manually or restart your terminal."
                }
            } catch {
                Write-Warning "Error during auto-install of $($toolName): $($_.Exception.Message)"
            }
        }
    }

    # 5. Prompt the user for the path (if not found by any means)
    $toolExePath = Read-Host "Please enter the path to the $($toolName) executable (e.g., '$executableName')$([string]::IsNullOrEmpty($expectedVersionPrefix) -or " (expected version prefix: $($expectedVersionPrefix))")"

    # Check if the file exists
    if (Test-Path $toolExePath) {
        if (-not [string]::IsNullOrEmpty($expectedVersionPrefix)) {
            # Perform version check if expectedVersionPrefix is provided
            $versionArgument = "--version"
            if ($toolName -ceq "Blender") {
                $versionArgument = "--version"
            }
            # Add more conditions for other tools if their version argument is different

            try {
                $versionOutput = & $toolExePath $versionArgument 2>&1 # Redirect stderr to stdout
            } catch {
                Write-Warning "Error executing $($toolName) to get version information: $($_.Exception.Message)"
                return $null
            }

            Write-Host "$($toolName) version information:" -ForegroundColor Yellow
            Write-Host $versionOutput -ForegroundColor Gray

            # Extract and display only the version number from the first line
            $firstLine = $versionOutput -split "`n" | Select-Object -First 1
            $versionMatch = $null
            if ($firstLine -match '(\d+\.\d+\.\d+)') {
                $versionMatch = $Matches[1]
                Write-Host "Extracted $($toolName) Version: $($versionMatch)"

                # Check if the version starts with the expected prefix
                if ($versionMatch -like "$($expectedVersionPrefix)*") {
                    Write-Host "$($toolName) version matches the expected prefix '$($expectedVersionPrefix)'." -ForegroundColor Green
                    Save-Config -key ($toolName + "ExePath") -value $toolExePath -section $toolPathsSection
                    return $toolExePath
                } else {
                    Write-Warning "Error: Detected $($toolName) version '$($versionMatch)' does not start with the expected prefix '$($expectedVersionPrefix)'."
                    return $null
                }
            } else {
                Write-Warning "Error: Unable to extract version number from the $($toolName) output."
                return $null
            }
        } else {
            # No version check needed, just return the path
            Write-Host "$($toolName) executable found at: $($toolExePath)" -ForegroundColor Green
            Save-Config -key ($toolName + "ExePath") -value $toolExePath -section $toolPathsSection
            return $toolExePath
        }
    } else {
        Write-Warning "Error: The specified path for $($toolName) does not exist. Please ensure the path is correct."
        return $null
    }
}

#endregion

#region Function: Save-Config

function Save-Config {
    param(
        [Parameter(Mandatory=$true)]
        [string]$key,
        [Parameter(Mandatory=$true)]
        [string]$value,
        [Parameter(Mandatory=$false)]
        [string]$configPath = $configFilePath,
        [Parameter(Mandatory=$false)]
        [string]$section = $null # Optional section parameter
    )

    $content = Get-Content -Path $configPath -ErrorAction SilentlyContinue
    $keyExists = $false
    $sectionExists = $false

    if ($section) {
        foreach ($line in $content) {
            if ($line -ceq "[$section]") {
                $sectionExists = $true
            } elseif ($sectionExists -and $line -like "$key=*") {
                $keyExists = $true
                break
            } elseif ($sectionExists -and $line -like "[*]") {
                break # Reached the next section
            }
        }

        if (-not $sectionExists) {
            # Add the section if it doesn't exist
            Add-Content -Path $configPath -Value "[$section]"
        }

        if (-not $keyExists) {
            Add-Content -Path $configPath -Value "$key=$value"
            Write-Host "Configuration saved: [$section] $key=$value" -ForegroundColor Green
        } elseif ($keyExists -and $content -like "*`n$key=*") {
            # Key exists, find and replace the value
            for ($i = 0; $i -lt $content.Count; $i++) {
                if ($content[$i] -like "$key=*") {
                    $content[$i] = "$key=$value"
                    break
                }
            }
            Set-Content -Path $configPath -Value $content
            Write-Host "Configuration updated: [$section] $key=$value" -ForegroundColor Green
        }
    } else {
        $keyExists = $content | Where-Object { $_ -like "$key=*" }
        if (-not $keyExists) {
            Add-Content -Path $configPath -Value "$key=$value"
            Write-Host "Configuration saved: $key=$value" -ForegroundColor Green
        } elseif ($keyExists) {
            # Key exists, find and replace the value
            for ($i = 0; $i -lt $content.Count; $i++) {
                if ($content[$i] -like "$key=*") {
                    $content[$i] = "$key=$value"
                    break
                }
            }
            Set-Content -Path $configPath -Value $content
            Write-Host "Configuration updated: $key=$value" -ForegroundColor Green
        }
    }
}

#endregion

#region Function: Find-ToolInPath

function Find-ToolInPath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$toolName,
        [Parameter(Mandatory=$true)]
        [string]$executableName
    )

    Write-Host "Checking if $($toolName) is in the system path..." -ForegroundColor Cyan
    $command = Get-Command $executableName -ErrorAction SilentlyContinue
    if ($command) {
        Write-Host "$($toolName) found in path: $($command.Source)" -ForegroundColor Green
        return $command.Source
    } else {
        Write-Host "$($toolName) not found in system path." -ForegroundColor Yellow
        return $null
    }
}

#endregion

#region Function: Initialize-Workspace

function Initialize-Workspace {
    param(
        [Parameter(Mandatory=$true)]
        [string]$workspacePath
    )

    Write-Host "Initializing workspace at '$workspacePath'..." -ForegroundColor Cyan

    # Check if the main folders exist
    if (-not (Test-Path -Path "$workspacePath\GameFiles\Main" -PathType Container)) {
        Write-Host "Creating '$workspacePath\GameFiles\Main' folder structure..." -ForegroundColor Yellow
        try {
            New-Item -Path "$workspacePath\GameFiles\Main" -ItemType Directory -Force | Out-Null
            Write-Host "'$workspacePath\GameFiles\Main' folders created successfully." -ForegroundColor Green
        } catch {
            Write-Warning "Error creating '$workspacePath\GameFiles\Main' folders: $($_.Exception.Message)"
        }
    } else {
        Write-Host "'$workspacePath\GameFiles\Main' folders already exist." -ForegroundColor Green
    }
}

#endregion

#region Function: Get-IsoInputFolder

function Get-IsoInputFolder {
    Write-Host "Please provide the path to the folder containing the game ISO file(s)..." -ForegroundColor Cyan
    $inputFolderPath = Read-Host "Example: C:\ISOs"

    if (Test-Path $inputFolderPath -PathType Container) {
        Write-Host "Input folder path confirmed: '$inputFolderPath'" -ForegroundColor Green
        return $inputFolderPath
    } else {
        Write-Warning "Error: The specified input folder path is invalid or does not exist."
        return $null
    }
}

#endregion

#region Function: Extract-Iso

function Extract-Iso {
    param(
        [Parameter(Mandatory=$true)]
        [string]$inputFolderPath,
        [Parameter(Mandatory=$true)]
        [string]$outputPath,
        [string]$ps3GameFolderNameHint = $ps3GameFolderName
    )

    Write-Host "Searching for ISO files in '$inputFolderPath'..." -ForegroundColor Cyan
    $isoFiles = Get-ChildItem -Path $inputFolderPath -Filter "*.iso" -File

    if ($isoFiles.Count -eq 0) {
        Write-Warning "Warning: No ISO files found in the specified folder '$inputFolderPath'."
        return $false
    }

    $selectedIsoFile = $null

    if ($isoFiles.Count -eq 1) {
        $selectedIsoFile = $isoFiles[0]
        Write-Host "Found one ISO file: '$($selectedIsoFile.Name)'" -ForegroundColor Green
    } else {
        Write-Host "Found multiple ISO files:" -ForegroundColor Yellow
        for ($i = 0; $i -lt $isoFiles.Count; $i++) {
            Write-Host "$($i + 1). $($isoFiles[$i].Name)" -ForegroundColor Yellow
        }

        while ($true) {
            $selection = Read-Host "Please enter the number of the ISO file to extract"
            if ($selection -match '^\d+$' -and $selection -ge 1 -and $selection -le $isoFiles.Count) {
                $selectedIsoFile = $isoFiles[$selection - 1]
                break
            } else {
                Write-Warning "Invalid selection. Please enter a number between 1 and $($isoFiles.Count)."
            }
        }
    }

    $extractedFolderName = Join-Path -Path $outputPath -ChildPath $ps3GameFolderNameHint
    Write-Host "Checking if '$($extractedFolderName)' folder already exists for '$($selectedIsoFile.Name)'..." -ForegroundColor Cyan
    if (Test-Path -Path $extractedFolderName -PathType Container) {
        Write-Host "'$($extractedFolderName)' folder already exists. Skipping ISO extraction for '$($selectedIsoFile.Name)'." -ForegroundColor Yellow
        return $true # Indicate that the extraction was effectively "successful"
    }

    Write-Host "Attempting to extract ISO contents from '$($selectedIsoFile.FullName)' to '$outputPath' using drive mounting..." -ForegroundColor Cyan

    $mountedDisk = $null
    $mountedDriveLetter = $null

    try {
        $mountedDisk = Mount-DiskImage -ImagePath $selectedIsoFile.FullName -StorageType ISO -ErrorAction Stop
        $mountedDriveLetter = $null

        if ($mountedDisk) {
            $volumeInfo = Get-DiskImage -ImagePath $selectedIsoFile.FullName | Get-Volume
            if ($volumeInfo -and $volumeInfo.DriveLetter) {
                $mountedDriveLetter = $volumeInfo.DriveLetter
                Write-Host "ISO mounted successfully to drive '$($mountedDriveLetter):'." -ForegroundColor Green
            } else {
                Write-Warning "Error: Failed to retrieve drive letter for the mounted ISO."
                return $false
            }
        }

        $sourcePath = "$($mountedDriveLetter):\"
        if (Test-Path $sourcePath -PathType Container) {
            Write-Host "Copying contents from '$sourcePath' to '$outputPath'..." -ForegroundColor DarkYellow
            try {
                Copy-Item -Path "$sourcePath\*" -Destination $outputPath -Recurse -Force -ErrorAction Stop
                Write-Host "Contents copied successfully." -ForegroundColor Green
                return $true
            } catch {
                Write-Warning "Error copying contents from mounted ISO: $($_.Exception.Message)"
                return $false
            }
        } else {
            Write-Warning "Warning: Folder '$ps3GameFolderNameHint' not found in the mounted ISO."
            # Attempt to copy the entire ISO content if the specific folder is not found
            Write-Host "Attempting to copy the entire ISO content to '$outputPath'..." -ForegroundColor DarkYellow
            try {
                Copy-Item -Path "$($mountedDriveLetter):\*" -Destination $outputPath -Recurse -Force -ErrorAction Stop
                Write-Host "Entire ISO content copied successfully." -ForegroundColor Green
                return $true
            } catch {
                Write-Warning "Error copying entire ISO content: $($_.Exception.Message)"
                return $false
            }
        }
    } catch {
        Write-Warning "Error mounting ISO image: $($_.Exception.Message)"
        return $false
    } finally {
        if ($mountedDisk) {
            Write-Host "Dismounting ISO image..." -ForegroundColor DarkYellow
            Dismount-DiskImage -ImagePath $selectedIsoFile.FullName -ErrorAction SilentlyContinue | Out-Null
            Write-Host "ISO image dismounted." -ForegroundColor DarkYellow
        }
    }
}

#endregion

#region Main Script

Write-Host "Starting initialization..." -ForegroundColor Cyan

# Initialize Workspace
Initialize-Workspace -workspacePath "."

# Check for existing ISO input folder path
$isoInputFolderPathConfigured = Get-ConfigValue -key "IsoInputFolderPath" -section $gameSettingsSection
if (-not $isoInputFolderPathConfigured) {
    # Ask for ISO input folder path
    $isoInputFolderPath = Get-IsoInputFolder
    if ($isoInputFolderPath) {
        Save-Config -key "IsoInputFolderPath" -value $isoInputFolderPath -section $gameSettingsSection
        # Attempt to extract ISO immediately after getting the path
        Extract-Iso -inputFolderPath $isoInputFolderPath -outputPath $gameFilesMainPath
    } else {
        Write-Warning "No valid ISO input folder provided. Skipping ISO extraction."
    }
} else {
    Write-Host "ISO input folder path found in config: $($isoInputFolderPathConfigured)" -ForegroundColor Green
    # Attempt to extract ISO if the path is already configured
    Extract-Iso -inputFolderPath $isoInputFolderPathConfigured -outputPath $gameFilesMainPath
}

# Initialize Blender path with download and extract
$blenderExe = Get-ToolPath -toolName "Blender" -executableName "blender.exe" -expectedVersionPrefix "4.0" -defaultPaths $DefaultToolPaths["Blender"]
if ($blenderExe) {
    Write-Host "Blender path found: $($blenderExe)" -ForegroundColor Green
} else {
    Write-Host "Blender executable not found through default paths or config. Attempting download and extraction..." -ForegroundColor Yellow
    # The download and extract logic is now within Get-ToolPath for Blender
}

# Initialize Noesis path
$noesisExe = Get-ToolPath -toolName "Noesis" -executableName "noesis.exe" -defaultPaths $DefaultToolPaths["Noesis"]
if ($noesisExe) {
    Write-Host "Noesis path found: $($noesisExe)" -ForegroundColor Green
} else {
    Write-Host "Noesis executable not found through default paths or config. Please provide the path manually when prompted." -ForegroundColor Yellow
}

# Initialize FFmpeg path with auto-install
$ffmpegExe = Get-ToolPath -toolName "FFmpeg" -executableName "ffmpeg.exe" -defaultPaths $DefaultToolPaths["FFmpeg"]
if ($ffmpegExe) {
    Write-Host "FFmpeg path found: $($ffmpegExe)" -ForegroundColor Green
} else {
    Write-Host "FFmpeg executable not found through default paths, config, or system path." -ForegroundColor Yellow
}

# Initialize vgmstream-cli path with auto-install
$vgmstreamExe = Get-ToolPath -toolName "vgmstream-cli" -executableName "vgmstream-cli.exe" -defaultPaths $DefaultToolPaths["vgmstream-cli"]
if ($vgmstreamExe) {
    Write-Host "vgmstream-cli path found: $($vgmstreamExe)" -ForegroundColor Green
} else {
    Write-Host "vgmstream-cli executable not found through default paths, config, or system path." -ForegroundColor Yellow
}

# Initialize QuickBMS path
$quickbmsExe = Get-ToolPath -toolName "QuickBMS" -executableName "quickbms.exe" -defaultPaths $DefaultToolPaths["QuickBMS"]
if ($quickbmsExe) {
    Write-Host "QuickBMS path found: $($quickbmsExe)" -ForegroundColor Green
} else {
    Write-Host "QuickBMS executable not found through default paths or config. Please provide the path manually when prompted." -ForegroundColor Yellow
}

# Initialize Microsoft.DotNet.SDK.9 path (this might not be an executable path but rather ensures the SDK is installed)
$dotnetSDK9Installed = Get-ToolPath -toolName "Microsoft.DotNet.SDK.9" -executableName "dotnet.exe" # Using dotnet.exe as a check
if ($dotnetSDK9Installed) {
    Write-Host "Microsoft.DotNet.SDK.9 seems to be installed." -ForegroundColor Green
} else {
    Write-Host "Microsoft.DotNet.SDK.9 not found. Auto-install will be attempted." -ForegroundColor Yellow
}

# Initialize dotnet-script path
$dotnetScriptInstalled = Get-ToolPath -toolName "dotnet-script" -executableName "dotnet-script"
if ($dotnetScriptInstalled) {
    Write-Host "dotnet-script seems to be installed." -ForegroundColor Green
} else {
    Write-Host "dotnet-script not found. Auto-install will be attempted." -ForegroundColor Yellow
}

# Add more tool initializations here as needed

Write-Host "Initialization complete." -ForegroundColor Cyan

#endregion