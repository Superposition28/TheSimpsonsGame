#
#
# The following output file already exists:
#   StreamSetConfig
#   Do you want to overwrite it?
#     y = overwrite (you can use also the 'o' key), manually
#     n = skip (default, just press ENTER), manually
#     a = overwrite all the files without asking, command line argument -o
#     r = automatically rename the files with the same name, command line argument -K
#     s = skip all the existent files without asking, command line argument -k


param (
    [string]$OverwriteOption = "s"  # Default to 's' (skip all)
)

# example: .\Step2_quickbms_str.ps1 -OverwriteOption "s"


# Directory where the files are located
$StrDirectory = ".\GameFiles\Main\PS3_GAME\USRDIR\"
$OutDirectory = ".\GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT\"

# Log file path
$logFilePath = ".\Tools\process\3_QuickBMS\QBMS.log"

# Create log file if it doesn't exist
if (-not (Test-Path -Path $logFilePath)) {
    Write-Host "Creating log file at $logFilePath" -ForegroundColor Yellow
    try {
        New-Item -Path $logFilePath -ItemType File -Force | Out-Null
        Write-Host "Log file created successfully." -ForegroundColor Green
    } catch {
        Write-Host "Error creating log file: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "Log file already exists at $logFilePath" -ForegroundColor Yellow
}

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Path to the C# script
$csxScriptPath = "Tools\Global\ConfigReader.csx"

# Define the parameters to pass to the C# script
$section = "ToolPaths"
$key = "QuickBMSExePath"
$defaultValue = "error"

# Execute the C# script using dotnet-script, passing the parameters
$PathValue = & dotnet-script $csxScriptPath $section $key $defaultValue

# Output the result from the script
Write-Host "path from config: $PathValue" -ForegroundColor Cyan

# Ensure path exists
if ($null -ne $PathValue) {
    if (-not (Test-Path -Path $PathValue)) {
        Write-Error "QuickBMSExePath not found at: $PathValue"
		exit 1
	}
}

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Set the paths for the BMS script and QuickBMS tool
$quickBMS = $PathValue  # Path to the QuickBMS executable
$bmsScript = "Tools\quickbms\simpsons_str.bms"
$sourceDirectory = $StrDirectory  # Directory containing .str files

Write-Host "Starting QuickBMS processing..." -ForegroundColor Green
Write-Host "QuickBMS Path: $quickBMS" -ForegroundColor Cyan
Write-Host "BMS Script Path: $bmsScript" -ForegroundColor Cyan
Write-Host "Source Directory: $sourceDirectory" -ForegroundColor Cyan

# Get all .str files in the source directory and its subdirectories
$strFiles = Get-ChildItem -Path $sourceDirectory -Filter "*.str" -Recurse

Write-Host "Found $($strFiles.Count) .str files to process." -ForegroundColor Yellow

# Loop through each .str file and run the QuickBMS command
foreach ($file in $strFiles) {
    Write-Host "Processing file: $($file.FullName)" -ForegroundColor Green
    # Check if the file exists
    if (-not (Test-Path -Path $file.FullName)) {
        Write-Host "Error: File $($file.FullName) does not exist." -ForegroundColor Red
        continue
    }

    # Construct the output directory based on the file's name with _str suffix
    #$relativePath = $file.FullName.Substring($sourceDirectory.Length).TrimStart('\')
    $relativePath = $file.FullName.Substring($file.FullName.IndexOf("USRDIR") + 7).TrimStart('\')
    #Write-Host "Relative Path: $relativePath" -ForegroundColor Cyan
    $outputDirectory = Join-Path $OutDirectory (Join-Path $relativePath "$($file.BaseName)_str")
    
    Write-Host "Output Directory: $outputDirectory" -ForegroundColor Cyan

    # Ensure the output directory exists
    if (-not (Test-Path -Path $outputDirectory)) {
        Write-Host "Creating output directory: $outputDirectory" -ForegroundColor Yellow
        New-Item -Path $outputDirectory -ItemType Directory -Force
    }

    # Construct the command to run
    if ($OverwriteOption -eq "a") {
        # Construct the command to run with the overwrite all option
        $args = @(
            "-o"
            "$bmsScript"
            "$($file.FullName)"
            "$outputDirectory"
        )
        $quickBMSCommand = "& `"$quickBMS`" $args"
        Write-Host "QuickBMS Command: $quickBMSCommand" -ForegroundColor Green
    } elseif ($OverwriteOption -eq "r") {
        # Construct the command to run with the rename all option
        $args = @(
            "-K"
            "$bmsScript"
            "$($file.FullName)"
            "$outputDirectory"
        )
        $quickBMSCommand = "& `"$quickBMS`" $args"
        Write-Host "QuickBMS Command: $quickBMSCommand" -ForegroundColor Green
    } elseif ($OverwriteOption -eq "s") {
        $args = @(
            "-k"
            "$bmsScript"
            "$($file.FullName)"
            "$outputDirectory"
        )
        $quickBMSCommand = "& `"$quickBMS`" $args"
        Write-Host "QuickBMS Command: $quickBMSCommand" -ForegroundColor Green
    } else {
        # Construct the command to run with manual handling
        $args = @(
            "$bmsScript"
            "$($file.FullName)"
            "$outputDirectory"
        )
        $quickBMSCommand = "& `"$quickBMS`" $args"
        Write-Host "QuickBMS Command: $quickBMSCommand" -ForegroundColor Green
    }    

    # Execute the quickBMSCommand and stream the output directly to the console
    Write-Host "# Start quickBMS Output" -ForegroundColor Magenta

    # Capture standard output and standard error separately
    $quickBMSOutput = & $quickBMS @args 2>&1

    Write-Host "catching output" -ForegroundColor Magenta

    # Write the output to the console
    Write-Host ""
    Write-Host $quickBMSOutput -ForegroundColor Green
    Write-Host ""

    Write-Host "# End quickBMS Output" -ForegroundColor Magenta

    # Extract all coverage percentages
    [regex]$coverageRegex = 'coverage file\s+(?<filenumber>-?\d+)\s+([\d.]+)%\s+(\d+)\s+(\d+)\s+\.\s+offset\s+(?<offset>[0-9a-fA-F]+)'
    $matches = $quickBMSOutput | Select-String -Pattern $coverageRegex -AllMatches | ForEach-Object {$_.Matches}

    if ($matches) {
        Write-Host "Coverage Percentages:" -ForegroundColor Cyan
        foreach ($match in $matches) {
            $fileNumber = $match.Groups["filenumber"].Value
            $percentage = $match.Groups[1].Value
            $offset = $match.Groups["offset"].Value
            Write-Host "  File: $fileNumber, Percentage: $percentage%, Offset: 0x$offset" -ForegroundColor Cyan

            # disabled logging
            if (1 -eq 2) {
                # Log the file name and percentage to the log file
                try {
                    $logEntry = "Time = ""[{0}]"", Path = ""$($file.FullName)"", File = ""{1}"", Percentage = ""{2}%"", Offset = ""0x{3}""" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $fileNumber, $percentage, $offset
                    Add-Content -Path $logFilePath -Value $logEntry
                }
                catch {
                    Write-Host "Error writing to log file: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "No coverage information found." -ForegroundColor Yellow
    }

    Write-Host "Processed $($file.Name) -> Output Directory: $outputDirectory" -ForegroundColor Green

    #Start-Sleep -Seconds 5

}

Write-Host "QuickBMS processing completed." -ForegroundColor Green

