#
# convert snu to wav
#
# Start audio conversion job

# Set the source and target directories
$AudioSourceDir = ".\GameFiles\Main\PS3_GAME\USRDIR\Assets_1_Audio_Streams"
$AudioTargetDir = ".\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_1_Audio_Streams"

# Get the full path of the source directory
$AudioSourceDirFullPath = (Get-Item -LiteralPath $AudioSourceDir).FullName

# Recursively get all .snu files in the source directory
$snuFiles = Get-ChildItem -Path $AudioSourceDir -Recurse -Filter "*.snu"

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Path to the C# script
$csxScriptPath = "Tools\Global\ConfigReader.csx"

# Define the parameters to pass to the C# script
$section = "ToolPaths"
$key = "vgmstream-cliExePath"
$defaultValue = "vgmstream-cli"

# Execute the C# script using dotnet-script, passing the parameters
$vgmstreamCliPath = & dotnet-script $csxScriptPath $section $key $defaultValue

# Output the result from the script
Write-Host "vgmstream-cli path from config: $vgmstreamCliPath" -ForegroundColor Cyan

# Ensure vgmstream-cli path exists
if (-not (Test-Path -Path $vgmstreamCliPath)) {
    Write-Error "vgmstream-cli executable not found at: $vgmstreamCliPath"
    exit 1
}

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Iterate through each .snu file
foreach ($file in $snuFiles) {
    # Get the relative path from the base of the source directory, trimming the full path of $AudioSourceDirFullPath
    $relativePath = $file.FullName.Substring($AudioSourceDirFullPath.Length).TrimStart('\')  # Removing leading backslash
    $AudioTargetPath = Join-Path -Path $AudioTargetDir -ChildPath $relativePath   # Combine target directory and relative path
    
    # Make sure the directory exists in the target folder
    $AudioTargetDirectory = [System.IO.Path]::GetDirectoryName($AudioTargetPath)
    if (-not (Test-Path -Path $AudioTargetDirectory)) {
        New-Item -ItemType Directory -Path $AudioTargetDirectory -Force
    }

    # Set the target filename with .wav extension
    $wavFile = [System.IO.Path]::ChangeExtension($AudioTargetPath, ".wav")

    # Check if the output file already exists
    if (Test-Path -Path $wavFile) {
        Write-Host "Skipping conversion for '$($file.FullName)' as '$($wavFile)' already exists." -ForegroundColor Yellow
        continue
    }

    # Print the paths being used
    Write-Host "Converting '$($file.FullName)' to '$($wavFile)'"

    Write-Host "& $vgmstreamCliPath -f 1 -l 10 -o $wavFile $file.FullName" 

    # Run the vgmstream-cli to decode the .snu file to .wav
    & $vgmstreamCliPath -f 1 -l 10 -o $wavFile $file.FullName
    #test & vgmstream-cli -f 1 -l 10 -o "OUTPUT_d_as01_xxx_0003bb5.wav" "d_as01_xxx_0003bb5.exa.snu"

    #Start-Sleep -Seconds 10
}
