# Define the source and destination root paths
$sourceRoot = "GameFiles\Main\PS3_GAME\Flattened_OUTPUT"
Write-Host "Source Root: $sourceRoot" -ForegroundColor Green
$destinationRoot = "GameFiles\Main\PS3_GAME\TEXTURES_OUTPUT"
Write-Host "Destination Root: $destinationRoot" -ForegroundColor Green

# Check if source root exists
if (!(Test-Path $sourceRoot)) {
    Write-Error "Source directory '$sourceRoot' does not exist. Exiting."
    exit 1
}

# Check if destination root exists, create if not
if (!(Test-Path $destinationRoot)) {
    Write-Host "Destination directory '$destinationRoot' does not exist. Creating..." -ForegroundColor Yellow
    New-Item -Path $destinationRoot -ItemType Directory -Force | Out-Null
}

# Convert sourceRoot to a full path
$sourceRoot = Convert-Path $sourceRoot

# Get all PNG files recursively from the source root
$pngFiles = Get-ChildItem -Path $sourceRoot -Recurse -Filter *.png

foreach ($file in $pngFiles) {
    
    # Get the relative path of the file from the source root
    $relativePath = $file.FullName.Substring($sourceRoot.Length).TrimStart('\')
    Write-Host "Relative Path: $relativePath" -ForegroundColor Green

    # Build the destination path
    $destinationPath = Join-Path $destinationRoot $relativePath

    # Ensure the destination directory exists
    $destinationDir = Split-Path $destinationPath
    if (!(Test-Path $destinationDir)) {
        New-Item -Path $destinationDir -ItemType Directory -Force | Out-Null
    }

    # Copy the file to the destination
    Copy-Item -Path $file.FullName -Destination $destinationPath -Force

    #Start-Sleep -Seconds 2 
}