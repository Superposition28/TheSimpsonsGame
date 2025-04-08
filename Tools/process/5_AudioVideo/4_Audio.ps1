#
# convert snu to ogg
#
# Start audio conversion job

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

    # Set the target filename with .ogg extension
    $oggFile = [System.IO.Path]::ChangeExtension($AudioTargetPath, ".ogg")

    # Print the paths being used
    Write-Output "Converting '$($file.FullName)' to '$($oggFile)'"

    # Run the vgmstream-cli to decode the .snu file to .ogg
    #& vgmstream-cli -o $oggFile $file.FullName
    & vgmstream-cli -f 1 -l 10 -o $oggFile $file.FullName
    #test & vgmstream-cli -f 1 -l 10 -o "OUTPUT_d_as01_xxx_0003bb5.ogg" "d_as01_xxx_0003bb5.exa.snu"
}
