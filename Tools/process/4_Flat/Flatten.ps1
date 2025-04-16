<#
.SYNOPSIS
Recursively processes directories starting from a '*_str' subdirectory within each '.str' directory found under a RootDir. Flattens sequences where a directory solely contains another directory, combining their names with '++'. Copies files to the resulting structure in DestinationDir with hash checks and error handling.

.DESCRIPTION
This script searches for directories ending in ".str" within the RootDir. For each, it looks for a subdirectory ending in "_str". It then recursively processes the contents of the "_str" directory.
The core rule: If a source directory being processed contains exactly one item, and that item is also a directory, their names are combined (e.g., "parent++child") and the recursion continues into the child directory without creating a physical directory for "parent".
If a source directory contains multiple items, or only files, or is empty, a physical directory is created in the destination using either the source directory's name or the accumulated flattened name from previous steps. Files are copied into this destination directory, and the script recurses into any subdirectories found (resetting the accumulated name).
The script provides live output, performs SHA256 hash checks, and exits on any error. The original RootDir remains unchanged.

.PARAMETER RootDir
The root directory where the script should start searching for .str directories. Must exist.

.PARAMETER DestinationDir
The directory where the recursively flattened structure should be copied. Will be created if it doesn't exist.

.EXAMPLE
.\Tools\process\Step2.2_Flatten.ps1 -RootDir ".\GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT" -DestinationDir ".\GameFiles\Main\PS3_GAME\Flattened_OUTPUT"

.NOTES
Author: Based on user script and refinement iterations.
Date: 2025-04-07
Requires: PowerShell 4.0 or later (for Get-FileHash)
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$RootDir,

    [Parameter(Mandatory=$true)]
    [string]$DestinationDir
)

Write-Host "Starting recursive flattening copy process..." -ForegroundColor Cyan
Write-Host "Source Root Directory: '$RootDir'" -ForegroundColor Cyan
Write-Host "Destination Directory: '$DestinationDir'" -ForegroundColor Cyan

# --- Helper Functions ---

# Function to calculate the SHA256 hash of a file
function Get-FileSHA256 {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$FilePath
    )
    try {
        # Ensure the file exists before hashing
        if (-not (Test-Path -Path $FilePath -PathType Leaf)) {
            Write-Error "Error calculating SHA256 hash: File not found at '$($FilePath)'."
            exit 1
        }
        $fileHash = Get-FileHash -Algorithm SHA256 -Path $FilePath
        # $PSCmdlet.WriteVerbose("SHA256 hash for '$($FilePath)': $($fileHash.Hash)") # Optional verbose output
        return $fileHash.Hash
    } catch {
        Write-Error "Error calculating SHA256 hash for file '$($FilePath)': $($_.Exception.Message)"
        exit 1
    }
}

# Recursive function to process directories based on the flattening rule
function Process-SourceDirectory {
    [CmdletBinding()]
    param(
        # The current directory in the source structure being processed
        [Parameter(Mandatory=$true)]
        [string]$SourcePath,

        # The path in the destination where this SourcePath's corresponding output should be placed
        [Parameter(Mandatory=$true)]
        [string]$DestinationParentPath,

        # Optional: Carries the flattened name from parent levels if flattening occurred
        [string]$AccumulatedFlattenedName = "",

        # Pass the overall destination root for display/substring purposes
        [Parameter(Mandatory=$true)]
        [string]$BaseDestinationDir
    )

    Write-Host "Processing Source Directory: '$SourcePath'" -ForegroundColor Green
    Write-Host "Destination Parent Path: '$DestinationParentPath'" -ForegroundColor Green
    Write-Host "Accumulated Flattened Name: '$AccumulatedFlattenedName'" -ForegroundColor Green
    Write-Host "Base Destination Directory: '$BaseDestinationDir'" -ForegroundColor Green

    $PSCmdlet.WriteVerbose("Processing Source: '$SourcePath' -> Dest Parent: '$DestinationParentPath' (Accumulated Name: '$AccumulatedFlattenedName')")

    try {
        # Get direct children; -Force includes hidden items if needed, adjust if necessary
        $children = Get-ChildItem -Path $SourcePath -Force
        $childDirs = $children | Where-Object {$_.PSIsContainer}
        $childFiles = $children | Where-Object {-not $_.PSIsContainer}
        $childCount = $children.Count
        $childDirCount = $childDirs.Count
    } catch {
        Write-Error "Error reading contents of '$SourcePath': $($_.Exception.Message)."
        exit 1 # Cannot proceed if directory contents are unreadable
    }

    # --- Case 1: Flattening Condition ---
    # If directory contains *exactly one item* and that item *is a directory*
    if ($childCount -eq 1 -and $childDirCount -eq 1) {
        $singleChildDir = $childDirs[0]
        $sourceBaseName = Split-Path -Path $SourcePath -Leaf # Get the name of the current source directory

        # Append the child directory name to the current/accumulated name
        $newAccumulatedName = if ([string]::IsNullOrEmpty($AccumulatedFlattenedName)) {
            # Start a new flattened sequence using current source dir name and its single child dir name
            $sourceBaseName + "+" + $singleChildDir.Name
        } else {
            # Continue an existing flattened sequence by appending the single child dir name
            $AccumulatedFlattenedName + "+" + $singleChildDir.Name
        }

        $PSCmdlet.WriteVerbose("Flattening: '$($sourceBaseName)' contains only '$($singleChildDir.Name)'. New accumulated name: '$newAccumulatedName'")

        # Recurse into the single child directory, passing the new accumulated name
        # The DestinationParentPath remains the same, as we haven't created a concrete directory yet.
        Process-SourceDirectory -SourcePath $singleChildDir.FullName `
                                -DestinationParentPath $DestinationParentPath `
                                -AccumulatedFlattenedName $newAccumulatedName `
                                -BaseDestinationDir $BaseDestinationDir
        return # This level's processing is complete, handled by the recursive call
    }

    # --- Case 2: Branching or Terminal Condition ---
    # (More than one item, or zero items, or only files, or one file)
    else {
        # Determine the name for the directory to be created in the destination
        $finalDirName = if ([string]::IsNullOrEmpty($AccumulatedFlattenedName)) {
            # No prior flattening, use the actual name of the source directory
            Split-Path -Path $SourcePath -Leaf
        } else {
            # Use the name accumulated from previous flattening steps
            $AccumulatedFlattenedName
        }

        # Construct the full path for the destination directory for this level
        $finalDestDirPath = Join-Path -Path $DestinationParentPath -ChildPath $finalDirName

        # Create the destination directory if it doesn't exist
        if (-not (Test-Path -Path $finalDestDirPath -PathType Container)) {
            try {
                # Display path relative to the overall destination dir for clarity
                $relativeDestPath = $finalDestDirPath.Substring($BaseDestinationDir.Length).TrimStart('\')
                Write-Host "  Creating directory: '$relativeDestPath'" -ForegroundColor Green
                $PSCmdlet.WriteVerbose("Creating concrete destination directory: '$finalDestDirPath'")
                New-Item -Path $finalDestDirPath -ItemType Directory -Force -ErrorAction Stop | Out-Null
            } catch {
                Write-Error "Error creating directory '$finalDestDirPath': $($_.Exception.Message)."
                exit 1
            }
        } else {
            $PSCmdlet.WriteVerbose("Destination directory '$finalDestDirPath' already exists.")
        }

        # Process Files within the current SourcePath, copying them to finalDestDirPath
        if ($childFiles.Count -gt 0) {
            $PSCmdlet.WriteVerbose("Processing $($childFiles.Count) files in '$SourcePath'.")
            foreach ($file in $childFiles) {
                $destinationFilePath = Join-Path -Path $finalDestDirPath -ChildPath $file.Name
                Write-Host "Destination file path: '$destinationFilePath'"
                Write-Host "finalDestDirPath: '$finalDestDirPath'"
                Write-Host "DestinationParentPath: $DestinationParentPath"
                Write-Host "finalDirName: $finalDirName"

                Write-Host "Source file path: '$($file.FullName)'"

                #Write-Host "sleep 10 seconds before copying file"
                #Start-Sleep -Seconds 10
                #exit 1 # Uncomment this line to exit after the sleep for testing purposes


                try {
                    # Display target path relative to the overall destination dir
                    $relativeDestFilePath = $destinationFilePath.Substring($BaseDestinationDir.Length).TrimStart('\')
                    Write-Host "    Copying file: '$($file.Name)' -> '$relativeDestFilePath'" -ForegroundColor Blue
                    $PSCmdlet.WriteVerbose("Copying file '$($file.FullName)' to '$($destinationFilePath)'")
                    Copy-Item -Path $file.FullName -Destination $destinationFilePath -Force -ErrorAction Stop

                    # Perform hash check
                    $sourceHash = Get-FileSHA256 -FilePath $file.FullName -ErrorAction Stop
                    $destinationHash = Get-FileSHA256 -FilePath $destinationFilePath -ErrorAction Stop

                    if ($sourceHash -ne $destinationHash) {
                        Write-Error "Hash mismatch for file '$($file.Name)'. Dest: '$relativeDestFilePath'."
                        exit 1
                    } else {
                        $PSCmdlet.WriteVerbose("SHA256 hash match confirmed for '$($file.Name)'.")
                    }
                } catch {
                    # Catch errors from Copy-Item or Get-FileSHA256
                    Write-Error "Error during copy/verify for file '$($file.FullName)' to '$($destinationFilePath)': $($_.Exception.Message)."
                    exit 1
                }
            }
        }

        # Process Subdirectories within the current SourcePath
        if ($childDirCount -gt 0) {
            $PSCmdlet.WriteVerbose("Processing $($childDirCount) subdirectories in '$SourcePath'.")
            foreach ($dir in $childDirs) {
                # Recurse into this subdirectory.
                # The new Destination Parent is the directory we just created ($finalDestDirPath).
                # The AccumulatedFlattenedName is reset to "" because we created a concrete directory, starting a new potential flattening sequence.
                Process-SourceDirectory -SourcePath $dir.FullName `
                                        -DestinationParentPath $finalDestDirPath `
                                        -AccumulatedFlattenedName "" `
                                        -BaseDestinationDir $BaseDestinationDir
            }
        }

        # If the directory was empty, this point is reached without processing files/subdirs.
        if ($childCount -eq 0) {
            $PSCmdlet.WriteVerbose("Source directory '$SourcePath' is empty.")
        }

        return # Processing for this level (and its children) is complete
    } # End branching/terminal case
}


# Main processing function for a branch (.str directory)
function Compress-Branch {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$BranchPath, # Full path to the .str directory

        [Parameter(Mandatory=$true)]
        [string]$BaseRootDir, # Root of the entire source scan

        [Parameter(Mandatory=$true)]
        [string]$BaseDestinationDir # Root for all destination output
    )

    try {
        Write-Host "Processing branch: $($BranchPath)" -ForegroundColor Cyan
        $PSCmdlet.WriteVerbose("Processing branch source: $($BranchPath)")

        # --- Calculate Base Destination Path (corresponds to the .str directory path) ---
        $relativeBranchPath = ""
        # Convert both paths to absolute paths for reliable comparison and trim trailing slashes
        $absoluteBranchPath = (Resolve-Path -Path $BranchPath).ToString().TrimEnd('\')
        $absoluteBaseRootDir = (Resolve-Path -Path $BaseRootDir).ToString().TrimEnd('\')

        if ($absoluteBranchPath.Length -gt $absoluteBaseRootDir.Length) {
            if ($absoluteBranchPath.StartsWith($absoluteBaseRootDir)) {
                $relativeBranchPath = $absoluteBranchPath.Substring($absoluteBaseRootDir.Length).TrimStart('\')
            } else {
                Write-Error "Logic Error: BranchPath '$BranchPath' does not start with BaseRootDir '$BaseRootDir'."
                exit 1
            }
        } elseif ($absoluteBranchPath -eq $absoluteBaseRootDir) {
            # This case might occur if RootDir itself ends in .str
            Write-Warning "BranchPath '$BranchPath' is the same as BaseRootDir '$absoluteBaseRootDir'." -ForegroundColor Yellow
            # Relative path is empty, destination base is just BaseDestinationDir
        } else {
            # This indicates an unexpected state
            Write-Error "Logic Error: BranchPath '$BranchPath' does not appear to be under BaseRootDir '$absoluteBaseRootDir'."
            exit 1
        }
        # Destination base preserves the structure *up to* the .str dir
        $destinationBase = Join-Path -Path $BaseDestinationDir -ChildPath $relativeBranchPath
        Write-Host "Destination base path: '$destinationBase'" -ForegroundColor Yellow
        Write-Host "BaseDestinationDir: $BaseDestinationDir" -ForegroundColor Yellow
        Write-Host "relativeBranchPath: $relativeBranchPath" -ForegroundColor Yellow


        $PSCmdlet.WriteVerbose("Calculated destination base for this branch: '$destinationBase'")

        # --- Find the primary *_str directory inside the .str directory ---
        try {
            # Get items directly inside the .str directory
            $strDirItems = Get-ChildItem -Path $BranchPath -Force -ErrorAction Stop
        } catch {
            Write-Error "Error reading contents of '$BranchPath': $($_.Exception.Message)."
            exit 1
        }
        # Find the first subdirectory ending in _str
        $firstSubDir = $strDirItems | Where-Object {$_.PSIsContainer -and $_.Name -like "*_str"} | Select-Object -First 1

        if ($firstSubDir) {
            $startPath = $firstSubDir.FullName # Path to the *_str directory
            $PSCmdlet.WriteVerbose("Found primary '_str' directory to process: '$startPath'")

            # --- Determine Initial Call Parameters for Recursion ---
            $initialDestParentPath = $null
            $initialAccumulatedName = ""
            $initialSourceToProcess = $startPath # We process the *_str dir itself

            # Check for initial flattening: if .str *only* contains *_str
            if ($strDirItems.Count -eq 1 -and $strDirItems[0].FullName -eq $startPath) {
                # Flatten .str and *_str together
                $initialAccumulatedName = (Split-Path -Path $BranchPath -Leaf) + "+" + $firstSubDir.Name
                # The *parent* for the flattened item is the parent of the .str directory's destination equivalent
                $initialDestParentPath = Split-Path -Path $destinationBase -Parent

                Write-Host "initialDestParentPath: $initialDestParentPath" -ForegroundColor Yellow
                Write-Host "destinationBase: $destinationBase" -ForegroundColor Yellow


                # Ensure the parent path exists (it should normally, but safety check)
                if(-not $initialDestParentPath){ $initialDestParentPath = $BaseDestinationDir} # Handle edge case where .str is directly under root
                Write-Host "  Flattening initial '.str' and '_str' directories into '$initialAccumulatedName'" -ForegroundColor Magenta
                $PSCmdlet.WriteVerbose("Initial Flattening: AccumulatedName='$initialAccumulatedName', DestParent='$initialDestParentPath', Source='$initialSourceToProcess'")
            } else {
                # No initial flattening between .str and *_str.
                # The parent for the *_str directory's output is the destination equivalent of the .str directory
                $initialDestParentPath = $destinationBase

                $initialAccumulatedName = "" # No accumulation from parent level
                Write-Host "  No initial flattening between '$($BranchPath | Split-Path -Leaf)' and '$($firstSubDir.Name)'." -ForegroundColor Magenta
                $PSCmdlet.WriteVerbose("No Initial Flattening: AccumulatedName='$initialAccumulatedName', DestParent='$initialDestParentPath', Source='$initialSourceToProcess'")
            }

             # Create the top-level destination directory structure *up to* the calculated parent path if it doesn't exist
            # Example: Ensure ".../dest/Assets_Characters_Simpsons_3/GlobalFolder/chars/" exists before processing starts inside it.
            if ($initialDestParentPath -ne $BaseDestinationDir -and (-not (Test-Path -Path $initialDestParentPath -PathType Container))) {
                try {
                    $relativeParentPath = $initialDestParentPath.Substring($BaseDestinationDir.Length).TrimStart('\')
                    Write-Host "  Ensuring parent destination exists: '$relativeParentPath'" -ForegroundColor DarkGray
                    New-Item -Path $initialDestParentPath -ItemType Directory -Force -ErrorAction Stop | Out-Null
                } catch {
                    Write-Error "Failed to create parent destination directory '$initialDestParentPath': $($_.Exception.Message)"
                    exit 1
                }
            }

            Write-Host "SourcePath: $initialSourceToProcess"
            Write-Host "DestinationParentPath: $initialDestParentPath"
            Write-Host "AccumulatedFlattenedName: $initialAccumulatedName"
            Write-Host "BaseDestinationDir: $BaseDestinationDir"


            # --- Start Recursive Processing ---
            # Call the recursive function to process the *_str directory ($initialSourceToProcess)
            # It will create its own directory structure relative to $initialDestParentPath,
            # potentially using $initialAccumulatedName as its starting point.
            Process-SourceDirectory -SourcePath $initialSourceToProcess `
                                    -DestinationParentPath $initialDestParentPath `
                                    -AccumulatedFlattenedName $initialAccumulatedName `
                                    -BaseDestinationDir $BaseDestinationDir # Pass root dest for display/relative path calculation

            Write-Host "  Finished processing branch content starting from '$($firstSubDir.Name)'" -ForegroundColor Cyan

        } else {
            # No *_str directory found within the .str directory
            Write-Warning "No subdirectory matching '*_str' found directly under '$($BranchPath)'. Skipping processing content for this branch." -ForegroundColor Yellow
            # If you wanted to copy other files/folders found directly inside the .str directory,
            # you would add logic here to iterate through $strDirItems (excluding *_str dirs)
            # and copy them to $destinationBase. Currently, it skips.
        }
    } catch {
        Write-Error "An unexpected error occurred processing branch '$($BranchPath)': $($_.Exception.Message)."
        # Ensure script exits if an error occurs within the main try block of the function
        exit 1
    }
} # End Compress-Branch


# --- Main Script ---

Write-Host "Starting recursive flattening copy process..." -ForegroundColor Cyan
$PSCmdlet.WriteVerbose("Source Root Directory: '$RootDir'")
$PSCmdlet.WriteVerbose("Destination Directory: '$DestinationDir'")

# Validate RootDir
if (-not (Test-Path -Path $RootDir -PathType Container)) {
    Write-Error "Root directory '$RootDir' not found or is not a directory."
    exit 1
}

# Ensure DestinationDir exists, creating it if necessary
if (-not (Test-Path -Path $DestinationDir -PathType Container)) {
    Write-Host "Destination directory '$DestinationDir' not found. Creating..." -ForegroundColor Yellow
    try {
        New-Item -Path $DestinationDir -ItemType Directory -Force -ErrorAction Stop | Out-Null
        Write-Host "Destination directory created successfully." -ForegroundColor Green
    } catch {
        Write-Error "Failed to create destination directory '$DestinationDir': $($_.Exception.Message)"
        exit 1
    }
}

# Find all directories ending with ".str" recursively within the RootDir
try {
    Write-Host "Searching for '.str' directories under '$RootDir'..." -ForegroundColor DarkGray
    $branchPoints = Get-ChildItem -Path $RootDir -Directory -Recurse -Filter "*.str" -ErrorAction Stop
    Write-Host "Found $($branchPoints.Count) '.str' director(y/ies) to process." -ForegroundColor Green
} catch {
    Write-Error "Error finding '.str' directories under '$RootDir': $($_.Exception.Message)"
    exit 1
}


# Process each found .str directory (branch point)
$processedCount = 0
$totalBranches = $branchPoints.Count
foreach ($branchPoint in $branchPoints) {
    $processedCount++
    Write-Host "--------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "Processing branch $processedCount of $totalBranches : ${branchPoint.Name}" -ForegroundColor Yellow
    # Call the branch processing function, which handles finding *_str and starting recursion
    Compress-Branch -BranchPath $branchPoint.FullName -BaseRootDir $RootDir -BaseDestinationDir $DestinationDir
    # Check if the function call resulted in an error (non-zero exit code or $? being false)
    if ($LASTEXITCODE -ne 0 -or $? -eq $false) {
        Write-Error "Compress-Branch function reported an error for '$($branchPoint.FullName)'. Exiting script."
        # Attempt to exit with the specific error code if available and non-zero
        exit ($LASTEXITCODE | Where-Object {$_ -ne 0} | Select-Object -First 1 | ForEach-Object {$_} -Else 1)
    }
}

Write-Host "--------------------------------------------------" -ForegroundColor DarkGray
Write-Host "Recursive flattening copy process completed." -ForegroundColor Cyan
Write-Host "Source directory: '$RootDir'" -ForegroundColor Green
Write-Host "Destination directory: '$DestinationDir'" -ForegroundColor Green

# Note: Original source directories are not removed or modified.