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
    "1" = @{
        "Name" = "Initializes"
        "Action" = @{
            "Path" = ".\init.ps1"
            "Args" = ""
        }
    }
    "2" = @{
        "Name" = "Rename USRDIR Folders"
        "Action" = @{
            "Path" = ".\Tools\process\2_RenameDirs\RenameFolders.ps1"
            "Args" = ""
        }
    }
    "3" = @{
        "Name" = "QuickBMS STR"
        "Action" = @{
            "Path" = ".\Tools\process\3_QuickBMS\str.ps1"
            "Args" = "-OverwriteOption ""s"""
        }
    }
    "4" = @{
        "Name" = "Flatten Directories"
        "Action" = @{
            "Path" = ".\Tools\process\4_Flat\Flatten.ps1"
            "Args" = @("-RootDir", ".\GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT", "-DestinationDir", ".\GameFiles\Main\PS3_GAME\Flattened_OUTPUT")
        }
    }
    "5" = @{
        "Name" = "Video Conversion"
        "Action" = @{
            "Path" = ".\Tools\process\5_AudioVideo\4_Video.ps1"
            "Args" = ""
        }
    }
    "6" = @{
        "Name" = "Audio Conversion"
        "Action" = @{
            "Path" = ".\Tools\process\5_AudioVideo\4_Audio.ps1"
            "Args" = ""
        }
    }
    "7" = @{
        "Name" = "init Blender"
        "Action" = @{
            "Command" = "dotnet script Tools\process\6_Asset\init.csx"
            "Args" = ""
        }
    }
    "8" = @{
        "Name" = "Blender Conversion"
        "Action" = @{
            "Command" = "dotnet script Tools\process\6_Asset\blend.csx"
            "Args" = ""
        }
    }
    "9" = @{
        "Name" = "txd extraction initialization"
        "Action" = @{
            "Command" = "dotnet script Tools\process\7_Texture\init.csx"
            "Args" = ""
        }
    }
	"10" = @{
		"Name" = "Noesis txd directory"
		"Action" = @{
			"Path" = ".\Tools\process\7_Texture\copy.ps1"
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

# Function to display the menu with colors
function DisplayMenu {
    Write-Host "Select an option:"
    foreach ($key in $menuOptions.Keys | Sort-Object) {
        $optionName = $menuOptions[$key].Name
        if ($key -eq "c") {
            Write-Host -ForegroundColor Magenta "[$key] $optionName"
        } elseif ($key -eq "0") {
            Write-Host -ForegroundColor Yellow "[$key] $optionName"
        } elseif ($key -eq "q") {
            Write-Host -ForegroundColor Red "[$key] $optionName"
        } else {
            Write-Host -ForegroundColor Cyan "[$key] $optionName"
        }
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

# Main loop
while ($true) {
    DisplayMenu
    $selection = Read-Host "Enter your choice"

    if ($menuOptions.ContainsKey($selection)) {
        $selectedOption = $menuOptions[$selection]
        $action = $selectedOption.Action

        switch ($action) {
            "ClearTerminal" {
                Clear-Host
            }
            "RunAll" {
                Write-Host "Running all scripts..."
                foreach ($key in $menuOptions.Keys | Sort-Object) {
                    if ($key -notin "c", "0", "q") {
                        Write-Host "Running $($menuOptions[$key].Name)..."
                        Execute-Action $menuOptions[$key].Action
                        Write-Host "Finished $($menuOptions[$key].Name)."
                    }
                }
                Write-Host "All scripts have been executed."
            }
            "Quit" {
                Write-Host "Exiting script."
                exit 0
            }
            default {
                Execute-Action $action
            }
        }
    } else {
        Write-Warning "Invalid selection. Please try again."
    }
    Write-Host "" # Add an empty line for better readability
}