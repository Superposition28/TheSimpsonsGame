// Step2.2_Flatten.csx
// dotnet-script Step2.2_Flatten.csx -- ".\Source\RootDir" ".\Destination\Flattened"

#r "nuget: System.CommandLine, 2.0.0-beta4.22272.1" // Optional: For more advanced arg parsing if needed later.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// --- Argument Parsing & Validation ---

if (Args == null || Args.Count < 2) {
    LogError("Usage: dotnet-script Step2.2_Flatten.csx -- <RootDir> <DestinationDir>");
    Environment.Exit(1);
}

string rootDir = Args[0];
string destinationDir = Args[1];

// --- Helper Functions ---

// Log messages with color
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

// Recursive function to process directories based on the flattening rule
static void ProcessSourceDirectory(string sourcePath, string destinationParentPath, string accumulatedFlattenedName, string baseDestinationDir) {
    Log($"Processing Source Directory: '{sourcePath}'", ConsoleColor.Green);
    Log($" -> Destination Parent Path: '{destinationParentPath}'", ConsoleColor.DarkGreen);
    Log($" -> Accumulated Flattened Name: '{accumulatedFlattenedName}'", ConsoleColor.DarkGreen);
    // Log($" -> Base Destination Directory: '{baseDestinationDir}'", ConsoleColor.DarkGreen); // Less critical for log usually
    LogVerbose($"Processing Source: '{sourcePath}' -> Dest Parent: '{destinationParentPath}' (Accumulated Name: '{accumulatedFlattenedName}')");

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
        // The DestinationParentPath remains the same.
        ProcessSourceDirectory(singleChildDir, destinationParentPath, newAccumulatedName, baseDestinationDir);
        return; // This level's processing is complete
    }

    // --- Case 2: Branching or Terminal Condition ---
    // (More than one item, or zero items, or only files, or one file)
    else {
        // Determine the name for the directory to be created in the destination
        string finalDirName = string.IsNullOrEmpty(accumulatedFlattenedName)
            ? Path.GetFileName(sourcePath) // Use actual source directory name
            : accumulatedFlattenedName;     // Use accumulated flattened name

        // Construct the full path for the destination directory for this level
        string finalDestDirPath = Path.Combine(destinationParentPath, finalDirName);

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

        // Process Files within the current SourcePath, copying them to finalDestDirPath
        if (childFiles.Length > 0) {
            LogVerbose($"Processing {childFiles.Length} files in '{sourcePath}'.");
            foreach (string file in childFiles) {
                string fileName = Path.GetFileName(file);
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
                // The new Destination Parent is the directory we just created ($finalDestDirPath).
                // The AccumulatedFlattenedName is reset to "" because we created a concrete directory.
                ProcessSourceDirectory(dir,
                                       finalDestDirPath, // New parent is the one just created/ensured
                                       "",               // Reset accumulated name
                                       baseDestinationDir);
            }
        }

        // If the directory was empty, this point is reached without processing files/subdirs.
        if (childCount == 0) {
            LogVerbose($"Source directory '{sourcePath}' is empty.");
        }

        return; // Processing for this level complete
    } // End branching/terminal case
}


// Main processing function for a branch (.str directory)
static void CompressBranch(string branchPath, string baseRootDirAbs, string baseDestinationDirAbs) {
    Log($"Processing branch: {branchPath}", ConsoleColor.Cyan);
    LogVerbose($"Processing branch source: {branchPath}");

    string destinationBase;
    try {
        // --- Calculate Base Destination Path (corresponds to the .str directory path relative to RootDir) ---
        string branchPathAbs = Path.GetFullPath(branchPath);

        // Get the relative path from the root source dir to the current .str dir
        string relativeBranchPath = Path.GetRelativePath(baseRootDirAbs, branchPathAbs);
        if(relativeBranchPath == ".") // Handle case where RootDir itself ends in .str {
            relativeBranchPath = Path.GetFileName(branchPathAbs); // Use the name of the .str dir itself
            LogWarning($"BranchPath '{branchPath}' seems to be the BaseRootDir or directly under it. Using its name '{relativeBranchPath}' for destination structure.");
            // If RootDir itself is the .str, the destination base should still be the base destination. Adjust if needed.
            // For now, we combine it, meaning output goes into DestinationDir\<RootDirName>.str\
            destinationBase = Path.Combine(baseDestinationDirAbs, relativeBranchPath);

        } else {
            // Construct the destination path mirroring the source structure up to the .str dir
            destinationBase = Path.Combine(baseDestinationDirAbs, relativeBranchPath);
        }


        Log($" -> Destination base path for branch: '{destinationBase}'", ConsoleColor.Yellow);
        LogVerbose($"Calculated destination base for this branch: '{destinationBase}'");
    } catch (Exception ex) {
        LogError($"Error calculating destination base path for branch '{branchPath}': {ex.Message}");
        Environment.Exit(1);
        return; // Unreachable
    }

    try {
        // --- Find the primary *_str directory inside the .str directory ---
        string startPath = null;
        string firstSubDirName = null;
        string[] strDirItemsPaths = Directory.GetFileSystemEntries(branchPath); // Get all files and dirs

        try {
             // Find the first subdirectory ending in _str
            var firstSubDirInfo = Directory.EnumerateDirectories(branchPath, "*_str", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (firstSubDirInfo != null) {
                 startPath = firstSubDirInfo; // Full path to the *_str directory
                firstSubDirName = Path.GetFileName(startPath);
                LogVerbose($"Found primary '_str' directory to process: '{startPath}'");
            }
        } catch (Exception ex) {
            LogError($"Error finding '*_str' directory within '{branchPath}': {ex.Message}");
            Environment.Exit(1);
        }


        if (startPath != null) {
            // --- Determine Initial Call Parameters for Recursion ---
            string initialDestParentPath;
            string initialAccumulatedName;
            string initialSourceToProcess = startPath; // We process the *_str dir itself

            // Check for initial flattening: if .str *only* contains *_str
            if (strDirItemsPaths.Length == 1 && Path.GetFullPath(strDirItemsPaths[0]) == Path.GetFullPath(startPath)) {
                // Flatten .str and *_str together
                string branchBaseName = Path.GetFileName(branchPath);
                initialAccumulatedName = $"{branchBaseName}++{firstSubDirName}";

                // The *parent* for the flattened item is the parent of the .str directory's destination equivalent
                initialDestParentPath = Path.GetDirectoryName(destinationBase);
                // Handle edge case where destinationBase might be a root drive or direct child of root
                 if (string.IsNullOrEmpty(initialDestParentPath)) {
                    initialDestParentPath = baseDestinationDirAbs; // Fallback to base destination if no parent derived
                    LogWarning($"Initial Dest Parent derived as null/empty for '{destinationBase}', falling back to base '{baseDestinationDirAbs}'.");
                 }


                Log($"  Flattening initial '{branchBaseName}' and '{firstSubDirName}' directories into '{initialAccumulatedName}'", ConsoleColor.Magenta);
                LogVerbose($"Initial Flattening: AccumulatedName='{initialAccumulatedName}', DestParent='{initialDestParentPath}', Source='{initialSourceToProcess}'");
            }
            else {
                // No initial flattening between .str and *_str.
                // The parent for the *_str directory's output is the destination equivalent of the .str directory
                initialDestParentPath = destinationBase;
                initialAccumulatedName = ""; // No accumulation from parent level
                Log($"  No initial flattening between '{Path.GetFileName(branchPath)}' and '{firstSubDirName}'. Processing starts within '{Path.GetRelativePath(baseDestinationDirAbs, initialDestParentPath)}'.", ConsoleColor.Magenta);
                LogVerbose($"No Initial Flattening: AccumulatedName='{initialAccumulatedName}', DestParent='{initialDestParentPath}', Source='{initialSourceToProcess}'");
            }

            // Ensure the top-level destination directory structure *up to* the calculated parent path exists
             if (!Directory.Exists(initialDestParentPath))
             {
                 try
                 {
                     string relativeParentPath = Path.GetRelativePath(baseDestinationDirAbs, initialDestParentPath);
                     Log($"  Ensuring parent destination exists: '{relativeParentPath}'", ConsoleColor.DarkGray);
                     Directory.CreateDirectory(initialDestParentPath);
                 }
                 catch (Exception ex)
                 {
                     LogError($"Failed to create parent destination directory '{initialDestParentPath}': {ex.Message}");
                     Environment.Exit(1);
                 }
             }


            // --- Start Recursive Processing ---
            ProcessSourceDirectory(initialSourceToProcess,
                                   initialDestParentPath,
                                   initialAccumulatedName,
                                   baseDestinationDirAbs); // Pass root dest for relative path calculation

            Log($"  Finished processing branch content starting from '{firstSubDirName}'", ConsoleColor.Cyan);
        }
        else {
            // No *_str directory found within the .str directory
            LogWarning($"No subdirectory matching '*_str' found directly under '{branchPath}'. Skipping processing content for this branch.");
            // Add logic here if you want to copy other items from the .str directory itself.
        }
    } catch (Exception ex) {
        LogError($"An unexpected error occurred processing branch '{branchPath}': {ex.Message}");
        // Ensure script exits if an error occurs within the main try block
        Environment.Exit(1);
    }
} // End CompressBranch


// --- Main Script ---

LogInfo("Starting recursive flattening copy process...");
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

// Find all directories ending with ".str" recursively within the RootDir
string[] branchPoints;
try {
    Log("Searching for '.str' directories...", ConsoleColor.DarkGray);
    // Use SearchOption.AllDirectories for recursion
    branchPoints = Directory.GetDirectories(rootDirAbs, "*.str", SearchOption.AllDirectories);
    LogSuccess($"Found {branchPoints.Length} '.str' director(y/ies) to process.");
}
catch (Exception ex) {
    LogError($"Error finding '.str' directories under '{rootDirAbs}': {ex.Message}");
    Environment.Exit(1);
    return; // Unreachable
}


// Process each found .str directory (branch point)
int processedCount = 0;
int totalBranches = branchPoints.Length;
foreach (string branchPoint in branchPoints) {
    processedCount++;
    Log("--------------------------------------------------", ConsoleColor.DarkGray);
    Log($"Processing branch {processedCount} of {totalBranches} : {Path.GetFileName(branchPoint)}", ConsoleColor.Yellow);
    Log($"  Full Path: {branchPoint}", ConsoleColor.DarkYellow);

    // Call the branch processing function
    // Pass absolute paths for reliable relative path calculation within the function
    CompressBranch(branchPoint, rootDirAbs, destinationDirAbs);

    // Error handling is done via Environment.Exit(1) within the called functions
}

Log("--------------------------------------------------", ConsoleColor.DarkGray);
LogInfo("Recursive flattening copy process completed.");
LogSuccess($"Source directory: '{rootDirAbs}'");
LogSuccess($"Destination directory: '{destinationDirAbs}'");

// Note: Original source directories are not modified.