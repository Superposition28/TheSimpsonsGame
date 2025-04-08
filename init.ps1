#region Configuration

$configFilePath = "a:\Dev\Games\TheSimpsonsGame\config.ini" # Changed extension to .ini for clarity
$gameFilesMainPath = Join-Path -Path "." -ChildPath "GameFiles\Main"
$isoFileNameHint = "Simpsons Game, The (USA) ps3.iso"
$isoFilePathKey = "IsoFilePath"
$ps3GameFolderName = "PS3_GAME"

# Define a consistent section name for tool paths
$toolPathsSection = "ToolPaths"
$gameSettingsSection = "GameSettings"

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
        [string]$expectedVersionPrefix = $null # Make version check optional
    )

    Write-Host "Checking for $($toolName) executable..." -ForegroundColor Cyan

    # Prompt the user for the path
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
        }
    } else {
        $keyExists = $content | Where-Object { $_ -like "$key=*" }
        if (-not $keyExists) {
            Add-Content -Path $configPath -Value "$key=$value"
            Write-Host "Configuration saved: $key=$value" -ForegroundColor Green
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

#region Function: Get-IsoFilePath

function Get-IsoFilePath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$isoFileNameHint
    )

    Write-Host "Please provide the path to the game ISO file..." -ForegroundColor Cyan
    $isoFilePath = Read-Host "Example filename: '$isoFileNameHint'"

    if (Test-Path $isoFilePath -PathType Leaf) {
        Write-Host "ISO file path confirmed: '$isoFilePath'" -ForegroundColor Green
        return $isoFilePath
    } else {
        Write-Warning "Error: The specified ISO file path is invalid or the file does not exist."
        return $null
    }
}

#endregion

#region Function: Extract-Iso

function Extract-Iso {
    param(
        [Parameter(Mandatory=$true)]
        [string]$isoFilePath,
        [Parameter(Mandatory=$true)]
        [string]$outputPath,
        [string]$ps3GameFolderNameHint = $ps3GameFolderName
    )

    Write-Host "Checking if '$($outputPath)\$($ps3GameFolderNameHint)' folder already exists..." -ForegroundColor Cyan
    if (Test-Path -Path (Join-Path -Path $outputPath -ChildPath $ps3GameFolderNameHint) -PathType Container) {
        Write-Host "'$($outputPath)\$($ps3GameFolderNameHint)' folder already exists. Skipping ISO extraction." -ForegroundColor Yellow
        return $true # Indicate that the extraction was effectively "successful" in that the content is already there
    }

    Write-Host "Attempting to extract ISO contents from '$isoFilePath' to '$outputPath' using WinRAR..." -ForegroundColor Cyan

    # Check if WinRAR is available (you might need to adjust the path)
    $winrarPath = Get-ConfigValue -key "WinRarExePath" -section $toolPathsSection
    if (-not $winrarPath) {
        Write-Warning "Warning: WinRAR executable path not found in config. Please configure it to extract ISO files."
        return $false
    }

    if (-not (Test-Path $winrarPath)) {
        Write-Warning "Error: WinRAR executable not found at '$winrarPath'."
        return $false
    }

    # Ensure the output directory exists
    try {
        New-Item -Path $outputPath -ItemType Directory -Force | Out-Null
        Write-Host "Output directory '$outputPath' ensured." -ForegroundColor DarkYellow
    } catch {
        Write-Warning "Error creating output directory '$outputPath': $($_.Exception.Message)"
        return $false
    }

    # Construct the WinRAR command
    $arguments = "x", "`"$isoFilePath`"", "`"$outputPath`"", "-y"

    Write-Host "Executing WinRAR with arguments: '$($winrarPath) $($arguments -join ' ')'" -ForegroundColor DarkYellow

    try {
        $process = Start-Process -FilePath $winrarPath -ArgumentList $arguments -Wait -PassThru
        $exitCode = $process.ExitCode

        Write-Host "WinRAR process completed with exit code: $($exitCode)" -ForegroundColor DarkYellow

        if ($exitCode -eq 0) {
            Write-Host "ISO contents extracted successfully to '$outputPath' using WinRAR." -ForegroundColor Green
            return $true
        } else {
            Write-Warning "Error during ISO extraction (WinRAR exit code: $($exitCode))."
            return $false
        }
    } catch {
        Write-Warning "Error executing WinRAR: $($_.Exception.Message)"
        return $false
    }
}

#endregion

#region Main Script

Write-Host "Starting initialization..." -ForegroundColor Cyan

# Initialize Workspace
Initialize-Workspace -workspacePath "."

# Check for existing ISO file path
$isoFilePathConfigured = Get-ConfigValue -key $isoFilePathKey -section $gameSettingsSection
if (-not $isoFilePathConfigured) {
    # Ask for ISO file path
    $isoFilePath = Get-IsoFilePath -isoFileNameHint $isoFileNameHint
    if ($isoFilePath) {
        Save-Config -key $isoFilePathKey -value $isoFilePath -section $gameSettingsSection
        # Attempt to extract ISO immediately after getting the path
        Extract-Iso -isoFilePath $isoFilePath -outputPath $gameFilesMainPath
    }
} else {
    Write-Host "ISO file path found in config: $($isoFilePathConfigured)" -ForegroundColor Green
    # Attempt to extract ISO if the path is already configured
    Extract-Iso -isoFilePath $isoFilePathConfigured -outputPath $gameFilesMainPath
}

# Check for existing configuration values for tools
$winrarPathConfigured = Get-ConfigValue -key "WinRarExePath" -section $toolPathsSection
$blenderExePathConfigured = Get-ConfigValue -key "BlenderExePath" -section $toolPathsSection
$noesisExePathConfigured = Get-ConfigValue -key "NoesisExePath" -section $toolPathsSection
$ffmpegExePathConfigured = Get-ConfigValue -key "FFmpegExePath" -section $toolPathsSection
$vgmstreamExePathConfigured = Get-ConfigValue -key "vgmstreamExePath" -section $toolPathsSection

# Initialize winrar path
if (-not $winrarPathConfigured) {
    $winrarExe = Get-ToolPath -toolName "WinRAR" -executableName "WinRAR.exe"
    if ($winrarExe) {
        Save-Config -key "WinRarExePath" -value $winrarExe -section $toolPathsSection
    }
} else {
    Write-Host "WinRAR path found in config: $($winrarPathConfigured)" -ForegroundColor Green
}

# Initialize Blender path
if (-not $blenderExePathConfigured) {
    $blenderExe = Get-ToolPath -toolName "Blender" -executableName "blender.exe" -expectedVersionPrefix "4.3"
    if ($blenderExe) {
        Save-Config -key "BlenderExePath" -value $blenderExe -section $toolPathsSection
    }
} else {
    Write-Host "Blender path found in config: $($blenderExePathConfigured)" -ForegroundColor Green
}

# Initialize Noesis path
if (-not $noesisExePathConfigured) {
    $noesisExe = Get-ToolPath -toolName "Noesis" -executableName "noesis.exe"
    if ($noesisExe) {
        Save-Config -key "NoesisExePath" -value $noesisExe -section $toolPathsSection
    }
} else {
    Write-Host "Noesis path found in config: $($noesisExePathConfigured)" -ForegroundColor Green
}

# Initialize FFmpeg path
if (-not $ffmpegExePathConfigured) {
    $ffmpegExe = Find-ToolInPath -toolName "FFmpeg" -executableName "ffmpeg.exe"
    if (-not $ffmpegExe) {
        $ffmpegExe = Get-ToolPath -toolName "FFmpeg" -executableName "ffmpeg.exe"
    }
    if ($ffmpegExe) {
        Save-Config -key "FFmpegExePath" -value $ffmpegExe -section $toolPathsSection
    }
} else {
    Write-Host "FFmpeg path found in config: $($ffmpegExePathConfigured)" -ForegroundColor Green
}

# Initialize vgmstream-cli path
if (-not $vgmstreamExePathConfigured) {
    $vgmstreamExe = Find-ToolInPath -toolName "vgmstream-cli" -executableName "vgmstream-cli.exe"
    if (-not $vgmstreamExe) {
        $vgmstreamExe = Get-ToolPath -toolName "vgmstream-cli" -executableName "vgmstream-cli.exe"
    }
    if ($vgmstreamExe) {
        Save-Config -key "vgmstreamExePath" -value $vgmstreamExe -section $toolPathsSection
    }
} else {
    Write-Host "vgmstream-cli path found in config: $($vgmstreamExePathConfigured)" -ForegroundColor Green
}

# Add more tool initializations here as needed

Write-Host "Initialization complete." -ForegroundColor Cyan

#endregion