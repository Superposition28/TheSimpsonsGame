# Define the source and destination root paths
$sourceRoot = "GameFiles\Main\PS3_GAME\Flattened_OUTPUT"
$destinationRoot = "GameFiles\Main\PS3_GAME\TEXTURES_OUTPUT"

# Get all PNG files recursively from the source root
$pngFiles = Get-ChildItem -Path $sourceRoot -Recurse -Filter *.png

foreach ($file in $pngFiles) {
    # Get the relative path of the file from the source root
    $relativePath = $file.FullName.Substring($sourceRoot.Length).TrimStart('\')

    # Build the destination path
    $destinationPath = Join-Path $destinationRoot $relativePath

    # Ensure the destination directory exists
    $destinationDir = Split-Path $destinationPath
    if (!(Test-Path $destinationDir)) {
        New-Item -Path $destinationDir -ItemType Directory -Force | Out-Null
    }

    # Copy the file to the destination
    Copy-Item -Path $file.FullName -Destination $destinationPath -Force
}
