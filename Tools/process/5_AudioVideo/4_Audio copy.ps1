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
$defaultValue = "error"

$vgkey = "vgmstreamExePath"
$ffkey = "FFmpegExePath"

# Execute the C# script using dotnet-script, passing the parameters
$vgmstreamCliPath = & dotnet-script $csxScriptPath $section $vgkey $defaultValue
$ffmpegPath = & dotnet-script $csxScriptPath $section $ffkey $defaultValue

# Output the result from the script
Write-Host "vgmstream-cli path from config: $vgmstreamCliPath" -ForegroundColor Cyan
# Output the result from the script
Write-Host "ffmpeg path from config: $ffmpegPath" -ForegroundColor Cyan

# Ensure vgmstream-cli path exists
if (-not (Test-Path -Path $vgmstreamCliPath)) {
    Write-Error "vgmstream-cli executable not found at: $vgmstreamCliPath"
    exit 1
}
# Ensure ffmpeg path exists
if (-not (Test-Path -Path $ffmpegPath)) {
    Write-Error "ffmpeg executable not found at: $ffmpegPath"
    exit 1
}


# # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

function ProcessAudioFile {
    param (
        [string]$fileFullName,
        [string]$AudioSourceDirFullPath,
        [string]$AudioTargetDir,
        [string]$vgmstreamCliPath,
        [string]$ffmpegPath
    )
    $relativePath = $fileFullName.Substring($AudioSourceDirFullPath.Length).TrimStart('\')
    $AudioTargetPath = Join-Path -Path $AudioTargetDir -ChildPath $relativePath
    $AudioTargetDirectory = [System.IO.Path]::GetDirectoryName($AudioTargetPath)
    
    if (-not (Test-Path -Path $AudioTargetDirectory)) {
        New-Item -ItemType Directory -Path $AudioTargetDirectory -Force
    }

    # Step 1: Temporary wav file (from vgmstream)
    $oggTempFile = [System.IO.Path]::ChangeExtension($AudioTargetPath, ".temp.wav")

    # Step 2: Final wav file (Vorbis re-encoded)
    $oggFinalFile = [System.IO.Path]::ChangeExtension($AudioTargetPath, ".wav")

    Write-Host "Converting '$fileFullName' to temp wav: '$oggTempFile'"
    & $vgmstreamCliPath -f 1 -l 10 -o $oggTempFile $fileFullName

    Write-Host "Re-encoding '$oggTempFile' to Vorbis: '$oggFinalFile'" -ForegroundColor Yellow
    & $ffmpegPath -y -i $oggTempFile -c:a libvorbis $oggFinalFile

    # Optionally delete temp file
    Remove-Item $oggTempFile -Force

    Write-Host "Converted '$fileFullName' to '$oggFinalFile'" -ForegroundColor Magenta
}

foreach ($file in $snuFiles) {
    ProcessAudioFile -fileFullName $file.FullName -AudioSourceDirFullPath $AudioSourceDirFullPath -AudioTargetDir $AudioTargetDir -vgmstreamCliPath $vgmstreamCliPath  -ffmpegPath $ffmpegPath
}
