# Requires PowerShell 7 or higher

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Path to the C# script
$csxScriptPath = "Tools\Global\ConfigReader.csx"

# Define the parameters to pass to the C# script
$section = "test"
$key = "test"
$defaultValue = "test"

# Execute the C# script using dotnet-script, passing the parameters
#$exampleResponsePathValue = & dotnet-script $csxScriptPath $section $key $defaultValue

# Output the result from the script
#Write-Host "path from config: $exampleResponsePathValue" -ForegroundColor Cyan

# Ensure path exists
if ($null -ne $exampleResponsePathValue) {
    if (-not (Test-Path -Path $exampleResponsePathValue)) {
        #Write-Error "executable not found at: $exampleResponsePathValue"
    }
}

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #


# Define the menu options and their corresponding actions
$menuOptions = @{
    "01" = @{
        "Name" = "Initializes"
        "Action" = @{
            "Path" = ".\init.ps1"
            "Args" = ""
        }
    }
    "02" = @{
        "Name" = "Rename USRDIR Folders"
        "Action" = @{
            "Path" = ".\Tools\process\2_RenameDirs\RenameFolders.ps1"
            "Args" = ""
        }
    }
    "03" = @{
        "Name" = "QuickBMS STR"
        "Action" = @{
            "Path" = ".\Tools\process\3_QuickBMS\str.ps1"
            "Args" = "-OverwriteOption ""s"""
        }
    }
    "04" = @{
        "Name" = "Flatten Directories"
        "Action" = @{
            "Command" = "dotnet script Tools\process\4_Flat\flat.csx"
            "Args" = ".\GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT",".\GameFiles\Main\PS3_GAME\Flattened_OUTPUT"
        }
    }
    "05" = @{
        "Name" = "Video Conversion"
        "Action" = @{
            "Path" = ".\Tools\process\5_AudioVideo\4_Video.ps1"
            "Args" = ""
        }
    }
    "06" = @{
        "Name" = "Audio Conversion"
        "Action" = @{
            "Path" = ".\Tools\process\5_AudioVideo\4_Audio.ps1"
            "Args" = ""
        }
    }
    "07" = @{
        "Name" = "init Blender"
        "Action" = @{
            "Command" = "dotnet script .\Tools\process\6_Asset\init.csx"
            "Args" = ""
        }
    }
    "08" = @{
        "Name" = "Blender Conversion"
        "Action" = @{
            "Command" = "dotnet script Tools\process\6_Asset\blend.csx"
            "Args" = ""
        }
    }
    "09" = @{
        "Name" = "txd extraction initialization"
        "Action" = @{
            "Command" = "dotnet script Tools\process\9_Texture\init.csx"
            "Args" = ""
        }
    }
    "10" = @{
        "Name" = "Noesis txd directory"
        "Action" = @{
            "Path" = ".\Tools\process\9_Texture\copy.ps1"
            "Args" = "-Convert"
        }
    }
    "c" = @{
        "Name" = "Clear Terminal"
        "Action" = "ClearTerminal"
    }
    "0" = @{
        "Name" = "Run all scripts"
        "Action" = "RunAll"
    }
    "q" = @{
        "Name" = "Quit"
        "Action" = "Quit"
    }
}

# Function to log messages to main.log
function Log-Message {
    param (
        [string]$Message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Add-Content -Path ".\main.log" -Value $logMessage
}

# Function to execute a command or script with logging
function Execute-Action ($action) {
    if ($action.ContainsKey("Path")) {
        $startTime = Get-Date

        # Create a hashtable to hold named parameters
        $params = @{}
        if ($action.Args -is [array]) {
            # Convert array to hashtable
            for ($i = 0; $i -lt $action.Args.Count; $i += 2) {
                $params[$($action.Args[$i].Trim("-"))] = $action.Args[$i+1]
            }
        }

        Log-Message "Starting execution of $($action.Path) $($params)"
        Write-Host "Executing $($action.Path) $($params)"
        & $action.Path @params # Pass arguments as named parameters
        $endTime = Get-Date
        $duration = $endTime - $startTime
        Log-Message "Finished execution of $($action.Path). Time taken: $($duration.TotalSeconds) seconds."
    } elseif ($action.ContainsKey("Command")) {
        $startTime = Get-Date
        Log-Message "Starting execution of $($action.Command) $($action.Args)"
        Write-Host "Executing $($action.Command) $($action.Args)"
        Invoke-Expression "$($action.Command) $($action.Args)"
        $endTime = Get-Date
        $duration = $endTime - $startTime
        Log-Message "Finished execution of $($action.Command). Time taken: $($duration.TotalSeconds) seconds."
    } else {
        Write-Warning "No executable path or command defined for this action."
    }
}

function Show-InteractiveMenu {
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable]$Options,
        [string]$Title = "Select an Option"
    )

    $OptionKeys = $Options.Keys | Sort-Object
    $SelectedIndex = 0

    function Draw-Menu {
        Clear-Host # Always clear the screen before drawing the menu
        Write-Host $Title -ForegroundColor Cyan
        Write-Host "" # Add an initial empty line for spacing

        for ($i = 0; $i -lt $OptionKeys.Count; $i++) {
            $Key = $OptionKeys[$i]
            $Option = $Options[$Key]
            $Box = "[ ]"

            if ($Key -eq "c") {
                $ForegroundColor = "Magenta"
            } elseif ($Key -eq "0") {
                $ForegroundColor = "Yellow"
            } elseif ($Key -eq "q") {
                $ForegroundColor = "Red"
            } else {
                $ForegroundColor = "Cyan"
            }

            if ($i -eq $SelectedIndex) {
                $Box = "[X]"
            }

            Write-Host "  $Box $($Option.Name)" -ForegroundColor $ForegroundColor
        }
        Write-Host "" # Add an empty newline after drawing the options
    }

    Draw-Menu

    while ($true) {
        $Key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

        if ($Key.VirtualKeyCode -eq 38) { # Up Arrow
            $SelectedIndex--
            if ($SelectedIndex -lt 0) {
                $SelectedIndex = $OptionKeys.Count - 1
            }
            Draw-Menu
        } elseif ($Key.VirtualKeyCode -eq 40) { # Down Arrow
            $SelectedIndex++
            if ($SelectedIndex -ge $OptionKeys.Count) {
                $SelectedIndex = 0
            }
            Draw-Menu
        } elseif ($Key.VirtualKeyCode -eq 13) { # Enter Key
            return $OptionKeys[$SelectedIndex]
            break
        } elseif ($Key.VirtualKeyCode -eq 27) { # Escape Key (optional exit)
            Write-Host "`nMenu cancelled." -ForegroundColor Yellow
            return $null
            break
        }
    }
}

# Main loop
while ($true) {
    $selection = Show-InteractiveMenu -Options $menuOptions -Title "Select an option:"

    if ($selection) {
        if ($menuOptions.ContainsKey($selection)) {
            $selectedOption = $menuOptions[$selection]
            $action = $selectedOption.Action

            switch ($action) {
                "ClearTerminal" {
                    Clear-Host
                }
                "RunAll" {
                    Clear-Host
                    Write-Host "Running all scripts..."
                    foreach ($key in $menuOptions.Keys | Sort-Object) {
                        if ($key -notin "c", "0", "q") {
                            Write-Host "Running $($menuOptions[$key].Name)..."
                            Execute-Action $menuOptions[$key].Action
                            Write-Host "Finished $($menuOptions[$key].Name)."
                        }
                    }
                    Write-Host "All scripts have been executed."
                    Write-Host "`nPress any key to return to the menu..."
                    $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
                }
                "Quit" {
                    Write-Host "Exiting script."
                    exit 0
                }
                default {
                    Clear-Host # Clear screen before executing the selected action
                    Execute-Action $action
                    Write-Host "`nPress any key to return to the menu..."
                    $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
                }
            }
        } else {
            Write-Warning "Invalid selection."
        }
    } else {
        Write-Host "No option selected." -ForegroundColor Yellow
    }
}