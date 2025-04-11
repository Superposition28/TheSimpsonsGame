#
# convert snu to wav
#
# Start audio conversion

# Execute two PowerShell scripts in their own visible terminal windows
#Start-Process -FilePath "powershell.exe" -ArgumentList " . '.\tools\step3.1_Audio.ps1'" -WindowStyle Normal

# Set the source and target directories
$AudioSourceDir = ".\GameFiles\Main\PS3_GAME\USRDIR\Assets_Audio_Streams_3"
$AudioTargetDir = ".\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Audio_Streams_3"

# Get the full path of the source directory
$AudioSourceDirFullPath = (Get-Item -LiteralPath $AudioSourceDir).FullName

# Recursively get all .snu files in the source directory
$snuFiles = Get-ChildItem -Path $AudioSourceDir -Recurse -Filter "*.snu"

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
    $oggFile = [System.IO.Path]::ChangeExtension($AudioTargetPath, ".wav")

    # Print the paths being used
    Write-Host "Converting '$($file.FullName)' to '$($oggFile)'"

    # Run the vgmstream-cli to decode the .snu file to .wav
    #& vgmstream-cli -o $oggFile $file.FullName
    & vgmstream-cli -f 1 -l 10 -o $oggFile $file.FullName
    #test & vgmstream-cli -f 1 -l 10 -o "OUTPUT_d_as01_xxx_0003bb5.wav" "d_as01_xxx_0003bb5.exa.snu"
}

#
# convert vp6 to ogv
#
# Start video conversion

#Start-Process -FilePath "powershell.exe" -ArgumentList ". '.\tools\step3.2_Video.ps1'" -WindowStyle Normal

# Set the source and target directories
$MovSourceDir = ".\GameFiles\Main\PS3_GAME\USRDIR\Assets_Video_Movies_3"
$MovTargetDir = ".\GameFiles\Main\PS3_GAME\AudioVideo_OUTPUT\Assets_Video_Movies_3"

# Get the full path of the source directory
$MovSourceDirFullPath = (Get-Item -LiteralPath $MovSourceDir).FullName

# Recursively get all .vp6 files in the source directory
$vp6Files = Get-ChildItem -Path $MovSourceDir -Recurse -Filter "*.vp6"

# Iterate through each .vp6 file
foreach ($file in $vp6Files) {
    # Get the relative path from the base of the source directory, trimming the full path of $MovSourceDirFullPath
    $MovRelativePath = $file.FullName.Substring($MovSourceDirFullPath.Length).TrimStart('\')  # Removing leading backslash
    $MovTargetPath = Join-Path -Path $MovTargetDir -ChildPath $MovRelativePath   # Combine target directory and relative path
    
    # Make sure the directory exists in the target folder
    $targetDirectory = [System.IO.Path]::GetDirectoryName($MovTargetPath)
    if (-not (Test-Path -Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force
    }

    # Set the target filename with .ogv extension
    $ogvFile = [System.IO.Path]::ChangeExtension($MovTargetPath, ".ogv")

    # Print the paths being used
    Write-Host "Converting '$($file.FullName)' to '$($ogvFile)'"

    # Run the ffmpeg to decode the .vp6 file to .ogv
    #& ffmpeg -i $file.FullName -c:v ffv1 -c:a copy $ogvFile
    #& ffmpeg -i $file.FullName -c:v libvpx-vp9 -lossless 1 -c:a libopus $ogvFile
    & ffmpeg -y -i $file.FullName -c:v libtheora -q:v 7 -c:a libvorbis -q:a 5 $ogvFile
    #test & ffmpeg -i 80b_igc01.vp6 -c:v libvpx-vp9 -lossless 1 -c:a libopus 80b_igc01.ogv

}

