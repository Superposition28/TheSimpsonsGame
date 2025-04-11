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
    $items = Get-ChildItem -Path $Path -ErrorAction SilentlyContinue | Sort-Object -Property @{Expression={$_.PSIsContainer}; Descending=$false}, Name

    if ($null -eq $items) {
        # Path might be inaccessible or empty, just return what we have
        return $outputLines
    }

    $lastItemIndex = $items.Count - 1

    # Iterate through each item (file or directory) at the current level
    for ($i = 0; $i -lt $items.Count; $i++) {
        $item = $items[$i]
        $isLast = ($i -eq $lastItemIndex)

        # Determine branch connector for directories
        $itemConnector = if ($isLast) { '\---' } else { '+---' }

        # Determine prefix continuation for children IF the PARENT ($item) is a directory
        $recursiveChildPrefixContinuation = if ($isLast) { '    ' } else { '|   ' }
        $recursiveChildPrefix = $IndentPrefix + $recursiveChildPrefixContinuation

        if ($item.PSIsContainer) {
            # Print directory line
            $outputLines.Add($IndentPrefix + $itemConnector + $item.Name)
            # Recurse using the standard child prefix
            $childLinesResult = @(Show-Tree -Path $item.FullName -IndentPrefix $recursiveChildPrefix -IsRoot:$false)
            if ($null -ne $childLinesResult -and $childLinesResult.Count -gt 0) {
                $outputLines.AddRange([string[]]$childLinesResult)
            }
        } else {
            # It's a file.
            # Construct the specific prefix for file lines using spaces '    ' for the last segment.
            $fileLinePrefix = $IndentPrefix + '    '
            $outputLines.Add($fileLinePrefix + $item.Name)
        }
    }

    # Return the collected lines for this level and below
    return $outputLines
}

# --- How to Use ---

# Get the path from the command line arguments, or use current directory if none provided
if ($args.Count -gt 0) {
    $rootPath = $args[0]
} else {
    $rootPath = (Get-Location).Path # Use current location
}

# Execute the function and capture the output
$treeOutput = Show-Tree -Path $rootPath

# Display output to console
if ($treeOutput) {
    $treeOutput
} else {
    Write-Warning "Failed to generate tree structure for path: $rootPath"
}