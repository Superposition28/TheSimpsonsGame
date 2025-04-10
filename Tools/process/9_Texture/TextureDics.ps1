<#
.SYNOPSIS
Recursively extracts PNG textures from .txd files using Noesis, maintaining directory structure.

.DESCRIPTION
This script iterates through a specified input directory and its subdirectories,
finds all .txd files, and uses Noesis to extract the textures as PNG files.
The output PNG files are placed in a mirrored directory structure within the
specified output folder.

.PARAMETER InputFolder
The root directory containing the .txd files.

.PARAMETER OutputFolder
The root directory where the extracted PNG files will be saved.

.PARAMETER NoesisPath
The full path to the Noesis.exe executable.

.PARAMETER ToolName
The exact name of the Noesis tool used for batch texture extraction.

.EXAMPLE
.\ExtractTXDTextures.ps1 -InputFolder "C:\SourceTextures" -OutputFolder "C:\ExtractedTextures" -NoesisPath "C:\Noesis\Noesis.exe" -ToolName "&Batch Export"

.NOTES
Ensure that the ToolName in Noesis supports specifying an output path and format
via command-line parameters (e.g., -outpath and -format). Adjust the parameters
in the Noesis command if your tool uses different options.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$InputFolder,

    [Parameter(Mandatory=$true)]
    [string]$OutputFolder,

    [Parameter(Mandatory=$true)]
    [string]$NoesisPath,

    [Parameter(Mandatory=$true)]
    [string]$ToolName
)

# Check if the input folder exists
if (-not (Test-Path -Path $InputFolder -PathType Container)) {
    Write-Error "Input folder '$InputFolder' does not exist."
    exit 1
}

# Check if the Noesis executable exists
if (-not (Test-Path -Path $NoesisPath -PathType Leaf)) {
    Write-Error "Noesis executable '$NoesisPath' does not exist."
    exit 1
}

# Iterate through all .txd files recursively in the input folder
Get-ChildItem -Path $InputFolder -Filter "*.txd" -Recurse | ForEach-Object {
    $txdFile = $_.FullName

    # Create the relative output path based on the input folder structure
    $relativePath = $txdFile.Substring($InputFolder.Length).TrimStart("\")
    $outputPath = Join-Path -Path $OutputFolder -ChildPath (Split-Path -Parent $relativePath)

    # Create the output directory if it doesn't exist
    if (-not (Test-Path -Path $outputPath -PathType Container)) {
        Write-Verbose "Creating output directory: '$outputPath'"
        New-Item -Path $outputPath -ItemType Directory -Force | Out-Null
    }

    # Construct the Noesis command
    $noesisArguments = "?runtool `"$ToolName`" `"$txdFile`" -outpath `"$outputPath`" -format png"

    Write-Verbose "Executing Noesis: '$NoesisPath $noesisArguments'"

    # Execute Noesis
    Start-Process -FilePath $NoesisPath -ArgumentList $noesisArguments -Wait -NoNewWindow
}

Write-Host "Batch texture extraction complete."