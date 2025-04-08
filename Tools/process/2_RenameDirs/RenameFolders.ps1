# Directory where the files are located
$directory = "GameFiles\Main\PS3_GAME\USRDIR\"

Write-Host "Processing directory: $directory"

# Mapping of old names to new names
$renameMap = @{
    "audiostreams"              = "Assets_Audio_Streams_3"
    "bargainbin"                = "Maps_World_BargainBin_4-10"
    "bigsuperhappy"             = "Maps_World_BigSuperHappy_4-14"
    "brt"                       = "Maps_World_Bartman_4-2"
    "cheater"                   = "Maps_World_Cheater_4-6"
    "colossaldonut"             = "Maps_World_ColossalDonut_4-8"
    "dayofthedolphins"          = "Maps_World_DayOfTheDolphins_4-7"
    "dayspringfieldstoodstill"  = "Maps_World_DaySpringfieldStoodStill_4-99"
    "eighty_bites"              = "Maps_World_EightyBites_4-3"
    "frontend"                  = "Assets_Frontend_3"
    "gamehub"                   = "Maps_World_GameHub_4-10"
    "grand_theft_scratchy"      = "Maps_World_GrandTheftScratchy_4-12"
    "loc"                       = "Maps_World_LandOfChocolate_4-1"
    "medal_of_homer"            = "Maps_World_MedalOfHomer_4-13"
    "meetthyplayer"             = "Maps_World_MeetThyPlayer_4-99"
    "mob_rules"                 = "Maps_World_MobRules_4-5"
    "movies"                    = "Assets_Video_Movies_3"
    "neverquest"                = "Maps_World_NeverQuest_4-11"
    "rhymes"                    = "Maps_World_Rhymes_4-99"
    "simpsons_chars"            = "Assets_Characters_Simpsons_3"
    "spr_hub"                   = "Maps_World_SprHub_4-5"
    "tree_hugger"               = "Maps_World_TreeHugger_4-4"
}

# Initialize counters for debugging
$totalItems = 0
$renamedItems = 0
$skippedItems = 0

# Get all directories in the directory
Get-ChildItem -Path $directory -Directory | ForEach-Object {
    $totalItems++
    $oldName = $_.Name
    Write-Host "Processing item: $oldName"

    # Check if the old name exists in the mapping
    if ($renameMap.ContainsKey($oldName)) {
        $newName = $renameMap[$oldName]
        $oldPath = $_.FullName
        $newPath = Join-Path $directory $newName
        Write-Host "Old path: $oldPath"
        Write-Host "New path: $newPath"
        
        # Perform the renaming (use only the new folder name)
        Rename-Item -Path $oldPath -NewName $newName
        Write-Host "Renamed '$oldName' to '$newName'"
        $renamedItems++
    } else {
        Write-Host "Skipped '$oldName' - no matching key in rename map"
        $skippedItems++
    }
}

# Log summary
Write-Host "Processing complete. Total items: $totalItems, Renamed: $renamedItems, Skipped: $skippedItems"

# Output all variables for debugging
Write-Host "`n--- Debugging Outputs ---"
Write-Host "Directory: $directory"
Write-Host "Rename Map: $($renameMap.GetEnumerator() | ForEach-Object { "$($_.Key) = $($_.Value)" } | Out-String)"
Write-Host "Total Items Processed: $totalItems"
Write-Host "Items Renamed: $renamedItems"
Write-Host "Items Skipped: $skippedItems"