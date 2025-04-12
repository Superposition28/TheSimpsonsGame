function Show-Tree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Path,

        # Internal use parameters for recursion
        [string]$IndentPrefix = "",
        [switch]$IsRoot = $true
    )

    # --- Function Body ---
    $outputLines = [System.Collections.Generic.List[string]]::new()

    # Add the root path only on the initial call
    if ($IsRoot) {
        # Resolve the path to get the full absolute path
        $resolvedPath = Resolve-Path -Path $Path -ErrorAction SilentlyContinue
        if ($resolvedPath) {
             $outputLines.Add($resolvedPath.Path.ToUpper()) # Add the root path in uppercase
        } else {
             $outputLines.Add("Error: Path not found or inaccessible: $Path")
             return $outputLines
        }
    }

    # Get child items (files and directories), handle potential errors
    # Sort directories first, then files, then by name for consistent ordering
    # Sort files first, then directories, then by name
    $items = Get-ChildItem -Path $Path -ErrorAction SilentlyContinue | Sort-Object -Property @{Expression={$_.PSIsContainer}; Descending=$false}, Name

    # --- OR (equivalent, as Descending=$false is default for boolean) ---

    # $items = Get-ChildItem -Path $Path -ErrorAction SilentlyContinue | Sort-Object -Property @{Expression={$_.PSIsContainer}}, Name
    if ($null -eq $items) {
        # Path might be inaccessible or empty, just return what we have
        return $outputLines
    }

    $lastItemIndex = $items.Count - 1

    # Iterate through each item (file or directory) at the current level
    for ($i = 0; $i -lt $items.Count; $i++) {
        $item = $items[$i]
        $isLast = ($i -eq $lastItemIndex)

        # --- Determine Prefix Components ---
        # 1. Connector for the current item ('+---' or '\---')
        $itemConnector = if ($isLast) { '\---' } else { '+---' }

        # 2. What to add to the indent prefix for children of this item ('|   ' or '    ')
        $childPrefixContinuation = if ($isLast) { '    ' } else { '|   ' }

        # Construct the prefix for any children of this item
        $childItemPrefix = $IndentPrefix + $childPrefixContinuation

        # --- Output the line for the current item ---
        if ($item.PSIsContainer) {
            # It's a directory
            $outputLines.Add($IndentPrefix + $itemConnector + $item.Name)

            # Recursively call for the directory's children
            # Use @() to ensure the result is always an array (even if null, single item, or existing array)
            $childLinesResult = @(Show-Tree -Path $item.FullName -IndentPrefix $childItemPrefix -IsRoot:$false)

            # Add the results if any were returned
            # Check for null is good practice although @() usually handles it
            if ($null -ne $childLinesResult -and $childLinesResult.Count -gt 0) {
                # Explicitly cast to string[] before AddRange to handle potential Object[] return
                # and satisfy the IEnumerable<string> requirement robustly.
                $outputLines.AddRange([string[]]$childLinesResult)
            }
        } else {
            # It's a file - Files use the child prefix for alignment, no connector needed for the file itself
            $outputLines.Add($childItemPrefix + $item.Name)
        }
    }

    # Return the collected lines for this level and below
    return $outputLines
}

# --- How to Use ---

# Define the starting path
$rootPath = "GameFiles\Main\PS3_GAME\Flattened_OUTPUT\Maps_World_3-16_MeetThyPlayer"

# Define the output file path
$outputFile = "test.tree"

# Execute the function and capture the output
# Use -IsRoot switch for the initial call (or rely on the default)
$treeOutput = Show-Tree -Path $rootPath

# Save the output to a file (use UTF8 encoding for compatibility with tree symbols)
if ($treeOutput) {
    $treeOutput | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host "Tree structure saved to '$outputFile'"
} else {
    Write-Warning "Failed to generate tree structure for path: $rootPath"
}

# Optional: Display output to console as well
# $treeOutput