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


# Set the paths for the BMS script and QuickBMS tool
$quickBMS = "A:\Dev\Games\The_Simpsons_Game\tools\quickbms\quickbms.exe"
$bmsScript = "A:\Dev\Games\The_Simpsons_Game\tools\quickbms\simpsons_str.bms"
$sourceDirectory = $StrDirectory  # Directory containing .str files

Write-Host "Starting QuickBMS processing..." -ForegroundColor Green
Write-Host "QuickBMS Path: $quickBMS" -ForegroundColor Cyan
Write-Host "BMS Script Path: $bmsScript" -ForegroundColor Cyan
Write-Host "Source Directory: $sourceDirectory" -ForegroundColor Cyan

# Get all .str files in the source directory and its subdirectories
$strFiles = Get-ChildItem -Path $sourceDirectory -Filter "*.str" -Recurse

Write-Host "Found $($strFiles.Count) .str files to process." -ForegroundColor Yellow

function ProcessPreinstancedFiles {
    param (
        [string]$outputDirectory
    )

    # Check for the presence of .preinstanced files in the output directory and its subdirectories
    $preinstancedFiles = Get-ChildItem -Path $outputDirectory -Filter "*.preinstanced" -Recurse

    Write-Host "Found $($preinstancedFiles.Count) .preinstanced files in $outputDirectory and its subdirectories." -ForegroundColor Yellow

    foreach ($preinstancedFile in $preinstancedFiles) {
        Write-Host "Processing preinstanced file: $($preinstancedFile.FullName)" -ForegroundColor Green
        # Define the source and destination paths for the blank.blend file
        $blankBlendSource = "A:\Dev\Games\The_Simpsons_Game\blank.blend"
        $blankBlendDestination = Join-Path $preinstancedFile.DirectoryName "$($preinstancedFile.BaseName).blend"

        Write-Host "Copying $blankBlendSource to $blankBlendDestination" -ForegroundColor Cyan

        # Copy and rename the blank.blend file
        Copy-Item -Path $blankBlendSource -Destination $blankBlendDestination -Force

        Write-Host "Copied and renamed blank.blend to $blankBlendDestination" -ForegroundColor Green
    }
}

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
        $command = "& `"$quickBMS`" -o `"$bmsScript`" `"$($file.FullName)`" `"$outputDirectory`""
    } elseif ($OverwriteOption -eq "r") {
        # Construct the command to run with the rename all option
        $command = "& `"$quickBMS`" -K `"$bmsScript`" `"$($file.FullName)`" `"$outputDirectory`""
    } elseif ($OverwriteOption -eq "s") {
        # Construct the command to run with the skip all option
        $command = "& `"$quickBMS`" -k `"$bmsScript`" `"$($file.FullName)`" `"$outputDirectory`""
    } else {
        # Construct the command to run with manual handling
        $command = "& `"$quickBMS`" `"$bmsScript`" `"$($file.FullName)`" `"$outputDirectory`""
    }

    Write-Host "Executing command: $command" -ForegroundColor Green

    # Execute the command
    Invoke-Expression $command

    Write-Host "Processed $($file.Name) -> Output Directory: $outputDirectory" -ForegroundColor Green

    #Start-Sleep -Seconds 15

}

Write-Host "QuickBMS processing completed." -ForegroundColor Green



