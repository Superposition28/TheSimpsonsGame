# Directory where the files are located
$directory = "GameFiles\Main\PS3_GAME\USRDIR\"
# $directory = "GameFiles\Main\PS3_GAME\QuickBMS_STR_OUTPUT\"
# $directory = "GameFiles\Main\PS3_GAME\Flattened_OUTPUT\"

Write-Host "Processing directory: $directory"

# Mapping of old names to new names
$renameMap = @{
    "audiostreams"              = "Assets_1_Audio_Streams"
    "movies"                    = "Assets_1_Video_Movies"

    "frontend"                  = "Assets_2_Frontend"
    "simpsons_chars"            = "Assets_2_Characters_Simpsons"

    "spr_hub"                   = "Map_3-00_SprHub"                    # Springfield ?

    "loc"                       = "Map_3-01_LandOfChocolate"           # 2.1 The Land of Chocolate
    "brt"                       = "Map_3-02_BartmanBegins"             # 2.2 Bartman Begins
    "eighty_bites"              = "Map_3-03_HungryHungryHomer"         # 2.3 Around the World in 80 Bites
    "tree_hugger"               = "Map_3-04_TreeHugger"                # 2.4 Lisa the Tree Hugger
    "mob_rules"                 = "Map_3-05_MobRules"                  # 2.5 Mob Rules
    "cheater"                   = "Map_3-06_EnterTheCheatrix"          # 2.6 Enter the Cheatrix
    "dayofthedolphins"          = "Map_3-07_DayOfTheDolphin"           # 2.7 The Day of the Dolphin
    "colossaldonut"             = "Map_3-08_TheColossalDonut"          # 2.8 Shadow of the Colossal Donut
    "dayspringfieldstoodstill"  = "Map_3-09_Invasion"                  # 2.9 Invasion of the Yokel-Snatchers ?
    "bargainbin"                = "Map_3-10_BargainBin"                # 2.10 Bargain Bin

    "gamehub"                   = "Map_3-00_GameHub"                   # Game Engine ? The Game Hub is a place in The Simpsons Game after completing the level Bargain Bin. It is where you go into the new and old simpsons games that are been/being created 
    
    "neverquest"                = "Map_3-11_NeverQuest"                # 2.11 NeverQuest
    "grand_theft_scratchy"      = "Map_3-12_GrandTheftScratchy"        # 2.12 Grand Theft Scratchy
    "medal_of_homer"            = "Map_3-13_MedalOfHomer"              # 2.13 Medal of Homer
    "bigsuperhappy"             = "Map_3-14_BigSuperHappy"             # 2.14 Big Super Happy Fun Fun Game

    "rhymes"                    = "Map_3-15_Rhymes"                    # 2.15 Five Characters in Search of an Author ?
    "meetthyplayer"             = "Map_3-16_MeetThyPlayer"             # 2.16 Game Over ?
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