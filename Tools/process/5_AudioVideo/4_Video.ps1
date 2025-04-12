# Convert vp6 to ogv
# Start video conversion job

# Set the source and target directories
$MovSourceDir = "GameFiles\Main\PS3_GAME\USRDIR\Assets_1_Video_Movies"
$MovTargetDir = "GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Video_Movies"

# Get the full path of the source directory
$MovSourceDirFullPath = (Get-Item -LiteralPath $MovSourceDir).FullName

# Recursively get all .vp6 files in the source directory
$vp6Files = Get-ChildItem -Path $MovSourceDir -Recurse -Filter "*.vp6"

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Path to the C# script
$csxScriptPath = "Tools\Global\ConfigReader.csx"

# Define the parameters to pass to the C# script
$section = "ToolPaths"
$key = "FFmpegExePath"
$defaultValue = "ffmpeg"

# Execute the C# script using dotnet-script, passing the parameters
$ffmpegPath = & dotnet-script $csxScriptPath $section $key $defaultValue

# Output the result from the script
Write-Host "FFmpeg path from config: $ffmpegPath" -ForegroundColor Cyan

# Ensure ffmpeg path exists
if (-not (Test-Path -Path $ffmpegPath)) {
    Write-Error "FFmpeg executable not found at: $ffmpegPath"
    exit 1
}

# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

# Iterate through each .vp6 file
foreach ($file in $vp6Files) {
    # Get the relative path from the base of the source directory, trimming the full path of $MovSourceDirFullPath
    $MovRelativePath = $file.FullName.Substring($MovSourceDirFullPath.Length).TrimStart('\')  # Removing leading backslash
    $MovTargetPath = Join-Path -Path $MovTargetDir -ChildPath $MovRelativePath   # Combine target directory and relative path
    
    # Make sure the directory exists in the target folder
    $targetDirectory = [System.IO.Path]::GetDirectoryName($MovTargetPath)
    if (-not (Test-Path -Path $targetDirectory)) {
        Write-Host "Creating directory: $targetDirectory"
        New-Item -ItemType Directory -Path $targetDirectory -Force
    }

    # Set the target filename with .ogv extension
    $ogvFile = [System.IO.Path]::ChangeExtension($MovTargetPath, ".ogv")

    # Check if the output file already exists
    if (Test-Path -Path $ogvFile) {
        Write-Host "Skipping conversion for '$($file.FullName)' as '$($ogvFile)' already exists." -ForegroundColor Yellow
        continue
    }

    # Print the paths being used
    Write-Host "Converting '$($file.FullName)' to '$($ogvFile)'"

    # Run the ffmpeg to decode the .vp6 file to .ogv
    try {
        & $ffmpegPath -y -i $file.FullName -c:v libtheora -q:v 7 -c:a libvorbis -q:a 5 $ogvFile
        Write-Host "Conversion completed: $ogvFile"
    }
    catch {
        Write-Error "Error converting '$($file.FullName)' to '$ogvFile'. $_"
        exit 1
    }
}
