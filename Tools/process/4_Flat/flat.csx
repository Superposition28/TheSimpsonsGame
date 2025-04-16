// flat.csx
// Applies flattening universally from RootDir's *contents* directly into DestinationDir.
// dotnet-script flat.csx -- ".\Source\RootDir" ".\Destination\Flattened"

#r "nuget: System.CommandLine, 2.0.0-beta4.22272.1" // Optional: For more advanced arg parsing if needed later.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

/**
 * --- Argument Parsing & Validation ---
 */
if (Args == null || Args.Count < 2) {
    LogError("Usage: dotnet-script flat.csx -- <RootDir> <DestinationDir>");
    Environment.Exit(1);
}
string rootDir = Args[0];
string destinationDir = Args[1];

/**
 * --- Main Script ---
 */

LogInfo("Starting universal recursive flattening copy process (root contents -> destination)...");
LogInfo($"Source Root Directory: '{rootDir}'");
LogInfo($"Destination Directory: '{destinationDir}'");

string rootDirAbs = Path.GetFullPath(rootDir);
string destinationDirAbs = Path.GetFullPath(destinationDir);


// Validate RootDir
if (!Directory.Exists(rootDirAbs)) {
    LogError($"Root directory '{rootDirAbs}' not found or is not a directory.");
    Environment.Exit(1);
}

// Ensure DestinationDir exists, creating it if necessary
if (!Directory.Exists(destinationDirAbs)) {
    LogWarning($"Destination directory '{destinationDirAbs}' not found. Creating...");
    try {
        Directory.CreateDirectory(destinationDirAbs);
        LogSuccess("Destination directory created successfully.");
    } catch (Exception ex) {
        LogError($"Failed to create destination directory '{destinationDirAbs}': {ex.Message}");
        Environment.Exit(1);
    }
}

// Start the universal recursive processing directly from the Root Directory
LogInfo("Starting processing from root directory's contents...");
Log("--------------------------------------------------", ConsoleColor.DarkGray);


try {
    // Initial call to the recursive function
    // Pass the original RootDir path explicitly to handle the special case inside
    ProcessSourceDirectory(rootDirAbs, destinationDirAbs, "", /* Start with no accumulated name */ destinationDirAbs, /* Base destination is needed for relative path logging */ rootDirAbs /* Pass the original root path */);
} catch (Exception ex) {
    LogError($"An unexpected error occurred during processing: {ex.Message}");
    LogError(ex.StackTrace);
    Environment.Exit(1);
}


Log("--------------------------------------------------", ConsoleColor.DarkGray);
LogInfo("Universal recursive flattening copy process completed.");
LogSuccess($"Source directory contents from: '{rootDirAbs}'");
LogSuccess($"Destination directory: '{destinationDirAbs}'");


/**
 * --- Helper Functions ---
 */


// Recursive function to process directories based on the universal flattening rule
// Added originalRootDirAbs parameter to handle the root special case
static void ProcessSourceDirectory(string sourcePath, string destinationParentPath, string accumulatedFlattenedName, string baseDestinationDir, string originalRootDirAbs) {
    Log($"Processing Source Directory: '{sourcePath}'", ConsoleColor.Green);
    Log($" -> Destination Parent Path: '{destinationParentPath}'", ConsoleColor.DarkGreen);
    Log($" -> Accumulated Flattened Name: '{accumulatedFlattenedName}'", ConsoleColor.DarkGreen);
    LogVerbose($"Processing Source: '{sourcePath}' -> Dest Parent: '{destinationParentPath}' (Accumulated Name: '{accumulatedFlattenedName}')");

	if (accumulatedFlattenedName != ""){
		accumulatedFlattenedName = SanitizeName(accumulatedFlattenedName);
	}


    string[] childDirs;
    string[] childFiles;
    int childCount;

    try {
        // Get direct children
        childDirs = Directory.GetDirectories(sourcePath);
        childFiles = Directory.GetFiles(sourcePath);
        childCount = childDirs.Length + childFiles.Length;
    } catch (Exception ex) {
        LogError($"Error reading contents of '{sourcePath}': {ex.Message}.");
        Environment.Exit(1);
        return; // Should not be reached
    }

    // --- Case 1: Flattening Condition ---
    // If directory contains *exactly one item* and that item *is a directory*
    if (childCount == 1 && childDirs.Length == 1) {
        string singleChildDir = childDirs[0];
        string sourceBaseName = Path.GetFileName(sourcePath); // Name of the current source directory
        string childBaseName = Path.GetFileName(singleChildDir);

        // Append the child directory name to the current/accumulated name
        string newAccumulatedName = string.IsNullOrEmpty(accumulatedFlattenedName)
            ? $"{sourceBaseName}++{childBaseName}"
            : $"{accumulatedFlattenedName}++{childBaseName}";

        LogVerbose($"Flattening: '{sourceBaseName}' contains only '{childBaseName}'. New accumulated name: '{newAccumulatedName}'");
        LogDebug($"Flattening {sourcePath} into {singleChildDir}");

        // Recurse into the single child directory, passing the new accumulated name
        // The DestinationParentPath remains the same. Pass originalRootDirAbs along.
        ProcessSourceDirectory(singleChildDir, destinationParentPath, newAccumulatedName, baseDestinationDir, originalRootDirAbs); // Pass along
        return; // This level's processing is complete
    }
    // --- Case 2: Branching or Terminal Condition ---
    // (More than one item, or zero items, or only files, or one file)
    else {
        // Determine the name for the directory to be created in the destination
        string finalDirName = string.IsNullOrEmpty(accumulatedFlattenedName)
            ? Path.GetFileName(sourcePath) // Use actual source directory name
            : accumulatedFlattenedName;     // Use accumulated flattened name

        string finalDestDirPath;
        bool isProcessingActualRootDir = (sourcePath == originalRootDirAbs && string.IsNullOrEmpty(accumulatedFlattenedName));

        // *** Handle Root Directory ***
        if (isProcessingActualRootDir) {
            // If processing the original root dir AND it didn't meet the flattening criteria,
            // we place its contents directly into the destination parent (the main DestinationDir).
            // We do NOT create a folder named after the root dir itself.
            finalDestDirPath = destinationParentPath;
            LogVerbose($"Processing root directory's children directly into '{finalDestDirPath}'");
			Thread.Sleep(1000);
        } else {
            // For all other directories (or the root *after* some initial flattening),
            // construct the path and create the directory as usual.

            // check against creating folders without name
            if (string.IsNullOrEmpty(finalDirName)) {
                LogError($"Calculated final directory name is empty for source '{sourcePath}'. This shouldn't happen unless processing root drive. Aborting.");
                Environment.Exit(1);
            }

            finalDestDirPath = Path.Combine(destinationParentPath, finalDirName);

            // Create the destination directory if it doesn't exist
            if (!Directory.Exists(finalDestDirPath)) {
                try {
                    string relativeDestPath = Path.GetRelativePath(baseDestinationDir, finalDestDirPath);
                    Log($"  Creating directory: '{relativeDestPath}'", ConsoleColor.Green);
                    LogVerbose($"Creating concrete destination directory: '{finalDestDirPath}'");
                    Directory.CreateDirectory(finalDestDirPath);
                } catch (Exception ex) {
                    LogError($"Error creating directory '{finalDestDirPath}': {ex.Message}.");
                    Environment.Exit(1);
                }
            } else {
                LogVerbose($"Destination directory '{finalDestDirPath}' already exists.");
            }
			//Thread.Sleep(1000); // Optional: Pause for clarity in output
        }


        // Process Files within the current SourcePath, copying them to the determined finalDestDirPath
        if (childFiles.Length > 0) {
            LogVerbose($"Processing {childFiles.Length} files in '{sourcePath}'.");
            foreach (string file in childFiles) {
                string fileName = Path.GetFileName(file);
                // Use the finalDestDirPath determined above (could be destinationParentPath if processing root)
                string destinationFilePath = Path.Combine(finalDestDirPath, fileName);
                string relativeDestFilePath = Path.GetRelativePath(baseDestinationDir, destinationFilePath);

                try {
                    Log($"    Copying file: '{fileName}' -> '{relativeDestFilePath}'", ConsoleColor.Blue);
                    LogVerbose($"Copying file '{file}' to '{destinationFilePath}'");
                    File.Copy(file, destinationFilePath, true); // true = overwrite

                    // Perform hash check
                    LogVerbose($"Verifying hash for '{fileName}'...");
                    string sourceHash = GetFileSHA256(file);
                    string destinationHash = GetFileSHA256(destinationFilePath);

                    if (sourceHash != destinationHash) {
                        LogError($"Hash mismatch for file '{fileName}'. Dest: '{relativeDestFilePath}'.");
                        LogError($"  Source SHA256: {sourceHash}");
                        LogError($"  Destination SHA256: {destinationHash}");
                        Environment.Exit(1);
                    } else {
                        LogVerbose($"SHA256 hash match confirmed for '{fileName}'.");
                    }
                } catch (Exception ex) {
                    // Catch errors from Copy or GetFileSHA256
                    LogError($"Error during copy/verify for file '{file}' to '{destinationFilePath}': {ex.Message}.");
                    Environment.Exit(1);
                }
            }
        }

        // Process Subdirectories within the current SourcePath
        if (childDirs.Length > 0) {
            LogVerbose($"Processing {childDirs.Length} subdirectories in '{sourcePath}'.");
            foreach (string dir in childDirs) {
                // Recurse into this subdirectory.
                // The new Destination Parent is the directory we just identified/created ($finalDestDirPath).
                // The AccumulatedFlattenedName is reset to "" because we created a concrete directory (or are processing under one).
                // Pass originalRootDirAbs along.
                ProcessSourceDirectory(dir,
                                       finalDestDirPath, // New parent is the one just identified/ensured
                                       "",               // Reset accumulated name
                                       baseDestinationDir,
                                       originalRootDirAbs); // Pass along
            }
        }

        // If the directory was empty, this point is reached without processing files/subdirs.
        if (childCount == 0) {
            LogVerbose($"Source directory '{sourcePath}' is empty.");
        }

        return; // Processing for this level complete
    } // End branching/terminal case
}

public class SanitizationRule {
	public string Pattern { get; set; }
	public string Replacement { get; set; }
	public bool IsRegex { get; set; }
}

public static string SanitizeName(string inputName) {
	LogVerbose($"Sanitizing name: '{inputName}'");

	List<SanitizationRule> rules = new List<SanitizationRule>() {
		new SanitizationRule { Pattern = Regex.Escape("build++PS3++ntsc_en"), Replacement = "US_AU", IsRegex = true },
		new SanitizationRule { Pattern = Regex.Escape("story_mode++story_mode_design.str++story_mode_design_str"), Replacement = "story_mode_design_STR", IsRegex = true },
		new SanitizationRule { Pattern = Regex.Escape("challenge_mode++challenge_mode_designSTR"), Replacement = "challenge_mode_design_STR", IsRegex = true },
		new SanitizationRule { Pattern = @"^texture_dictionary\+\+.*?\+\+chars$", Replacement = "Textures", IsRegex = true }, // texture_dictionary++dayofthedolphins++story_mode
		new SanitizationRule { Pattern = Regex.Escape("ASSET_RWS++texture_dictionary++GlobalFolder++costumes"), Replacement = "RWS+Textures", IsRegex = true },
		new SanitizationRule { Pattern = @"^texture_dictionary\+\+(.*?)\+\+design$", Replacement = "$1_Textures", IsRegex = true }, // e.g., "texture_dictionary++spr_hub++design" -> "spr_hub_Textures"
		new SanitizationRule { Pattern = @"^.*?_Textures\+\+Act_.*_folderstream$", Replacement = "Textures", IsRegex = true }, // {mapname}_Textures++Act_{number}_folderstream -> Textures 
		new SanitizationRule { Pattern = @"^(.*)\.str\+\+(.*)_str$", Replacement = "$1STR", IsRegex = true }, // e.g., "spr_hub.str++spr_hub_str" -> "spr_hubSTR"
		new SanitizationRule { Pattern = @"^assets_rws\+\+(.*?)\+\+\1$", Replacement = "ASSET_RWS", IsRegex = true }, // e.g., "assets_rws++spr_hub++spr_hub" -> "ASSET_RWS"
		new SanitizationRule { Pattern = "^audio\\+\\+", Replacement = "", IsRegex = true }
	};

	string outputName = inputName;

	foreach (var rule in rules) {
		string before = outputName;
		if (rule.IsRegex) {
			outputName = Regex.Replace(outputName, rule.Pattern, rule.Replacement);
		} else {
			outputName = outputName.Replace(rule.Pattern, rule.Replacement);
		}
		if (before != outputName) {
			LogVerbose($"Rule applied: Pattern='{rule.Pattern}', Replacement='{rule.Replacement}'");
			LogVerbose($"  Before: '{before}'");
			LogVerbose($"  After:  '{outputName}'");
		}
	}

	LogVerbose($"Sanitized name result: '{outputName}'");
	return outputName;
}


/**
 * Log Functions
 */
static void Log(string message, ConsoleColor color = ConsoleColor.Gray) {
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ResetColor();
}
static void LogInfo(string message) => Log(message, ConsoleColor.Cyan);
static void LogSuccess(string message) => Log(message, ConsoleColor.Green);
static void LogWarning(string message) => Log(message, ConsoleColor.Yellow);
static void LogError(string message) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ResetColor();
}
static void LogVerbose(string message) => Log($"VERBOSE: {message}", ConsoleColor.DarkGray);
static void LogDebug(string message) => Log($"DEBUG: {message}", ConsoleColor.Magenta); // For extra debugging

// Function to calculate the SHA256 hash of a file
static string GetFileSHA256(string filePath) {
    try {
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException($"File not found at '{filePath}'.");
        }
        using (var sha256 = SHA256.Create()) {
            using (var stream = File.OpenRead(filePath)) {
                byte[] hashBytes = sha256.ComputeHash(stream);
                // Convert byte array to a lowercase hex string
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    } catch (Exception ex) {
        LogError($"Error calculating SHA256 hash for file '{filePath}': {ex.Message}");
        Environment.Exit(1);
        return null; // Should not be reached due to Exit
    }
}
