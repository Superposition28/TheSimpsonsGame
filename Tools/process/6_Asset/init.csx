#r "System.IO.FileSystem"
#r "System.Text.Json"
#r "System.Security.Cryptography"

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// This script automates the process of mapping game assets (preinstanced, blend, and glb files)
// and creating symbolic links to these assets in a structured directory.
// It uses MD5 hashing to generate unique identifiers for each asset and organizes
// links into subdirectories based on the map/area name derived from the path.

public static void Print(string message, ConsoleColor color) {
	Console.ForegroundColor = color;
	Console.WriteLine(message);
	Console.ResetColor();
}
public static bool verbose = false; // Set to true for verbose output

// Main class containing core functionality for asset mapping and symbolic link creation
public class AssetMapper { // Renamed for clarity
    /// <summary>
    /// Extracts the map/area subdirectory name from the full preinstanced file path.
    /// Assumes the structure contains "\PS3_GAME\Flattened_OUTPUT\[MapName]\...".
    /// </summary>
    /// <param name="fullPath">The full path to the .preinstanced file.</param>
    /// <param name="preinstancedRoot">The root directory containing .preinstanced files.</param>
    /// <returns>The extracted map subdirectory name, or "_UNKNOWN_MAP" if not found.</returns>
    private static string ExtractMapSubdirectory(string fullPath, string preinstancedRoot) {
        // Ensure the path uses the correct directory separator for consistency
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        // Define the marker path segment
        string marker = Path.Combine("PS3_GAME", "Flattened_OUTPUT") + Path.DirectorySeparatorChar;

        // Find the index of the marker within the full path, case-insensitively
        int markerIndex = fullPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (markerIndex >= 0) {
            // Calculate the starting position right after the marker
            int startIndex = markerIndex + marker.Length;
            // Get the part of the path after the marker
            string remainingPath = fullPath.Substring(startIndex);
            // Split the remaining path into directory components
            string[] pathComponents = remainingPath.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            // The first component should be the map name
            if (pathComponents.Length > 0) {
                return pathComponents[0]; // Return the map directory name
            } else {
                Print($"Warning: Could not extract map subdirectory from path structure after marker: {fullPath}", ConsoleColor.Yellow);
            }
        } else {
            Print($"Warning: Marker '{marker}' not found in path: {fullPath}", ConsoleColor.Yellow);
        }

        // Return a default name if extraction fails
        return "_UNKNOWN_MAP";
    }

    /// <summary>
    /// Generates a mapping of assets by processing preinstanced, blend, and optional GLB files.
    /// </summary>
    /// <param name="rootDrive">The root drive where symbolic links will eventually be created.</param>
    /// <param name="preinstancedRoot">Root directory containing .preinstanced files.</param>
    /// <param name="blendRoot">Root directory containing .blend files.</param>
    /// <param name="glbRoot">Optional root directory for .glb files.</param>
    /// <param name="checkExistence">If true, checks for the existence of corresponding blend files.</param>
    /// <returns>A dictionary mapping unique identifiers to asset file paths and metadata.</returns>
    public static Dictionary<string, Dictionary<string, string>> GenerateAssetMapping(string rootDrive, string preinstancedRoot, string blendRoot, string glbRoot = null, bool checkExistence = false) {
        // Validate input directories
        if (!Directory.Exists(preinstancedRoot)) {
            throw new DirectoryNotFoundException($"Preinstanced root directory not found: {preinstancedRoot}");
        }
        if (!Directory.Exists(blendRoot)) {
            throw new DirectoryNotFoundException($"Blend root directory not found: {blendRoot}");
        }
        if (glbRoot != null && !Directory.Exists(glbRoot)) {
            Directory.CreateDirectory(glbRoot); // Create GLB directory if it doesn't exist
        }

        var mapping = new Dictionary<string, Dictionary<string, string>>();
        string normalizedPreinstancedRoot = Path.GetFullPath(preinstancedRoot); // Normalize for reliable relative path calculation

        // Process each .preinstanced file
        foreach (var preinstancedFile in Directory.EnumerateFiles(normalizedPreinstancedRoot, "*.preinstanced", SearchOption.AllDirectories)) {
            var preinstancedRelativePath = Path.GetRelativePath(normalizedPreinstancedRoot, preinstancedFile);
            var blendRelativePath = Path.ChangeExtension(preinstancedRelativePath, ".blend"); // More robust way to change extension
            var blendFullPath = Path.Combine(blendRoot, blendRelativePath);

            var glbRelativePath = Path.ChangeExtension(preinstancedRelativePath, ".glb");
            var glbFullPath = glbRoot != null ? Path.Combine(glbRoot, glbRelativePath) : null;

            // Skip if blend file doesn't exist and checkExistence is true
            if (checkExistence && !File.Exists(blendFullPath)) {
                Print($"Warning: Corresponding blend file not found: {blendFullPath}", ConsoleColor.Yellow);
                continue;
            }

            // *** NEW: Extract the map subdirectory name ***
            string mapSubdirectory = ExtractMapSubdirectory(preinstancedFile, normalizedPreinstancedRoot);
            if (verbose) {
                Print($"Extracted Map Subdirectory: {mapSubdirectory} for {preinstancedFile}", ConsoleColor.DarkCyan);
            }

            // Generate a unique identifier using MD5 hash of the *relative preinstanced path* for consistency
            using (var md5 = MD5.Create()) {
                // Use preinstanced relative path for hashing to ensure uniqueness based on source structure
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(preinstancedRelativePath.Replace('\\', '/'))); // Normalize separators for hash consistency
                var identifier = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                if (verbose) {
                    Print($"Hashed Path (for ID {identifier}): {preinstancedRelativePath}", ConsoleColor.Cyan);
                }

                var assetInfo = new Dictionary<string, string> {
                    ["map_subdirectory"] = mapSubdirectory, // Store the extracted map name
					["filename"] = Path.GetFileNameWithoutExtension(preinstancedFile),
                    ["preinstanced_full"] = preinstancedFile,
                    ["blend_full"] = blendFullPath,
                    ["glb_full"] = glbFullPath
                    // Symlink paths will be calculated and added later during link creation
                };

                // Add or update the mapping
                if (mapping.ContainsKey(identifier)) {
                    // Handle potential hash collisions or duplicates if necessary
                    Print($"Warning: Identifier collision detected for {identifier}. Overwriting entry. Check source files: {preinstancedFile} and existing.", ConsoleColor.Yellow);
                }
                mapping[identifier] = assetInfo;


                // Optionally process existing GLB files if glbRoot is provided
                // Note: This part might need adjustment depending on how GLB files are definitively matched.
                // The current logic assumes a GLB exists if a preinstanced file exists.
                if (glbRoot != null && File.Exists(glbFullPath)) {
                    if (mapping.ContainsKey(identifier)) {
                        mapping[identifier]["existing_glb_full"] = glbFullPath; // Update key for existing GLB
                    } else {
                        // This case should ideally not happen if GenerateAssetMapping logic is correct
                        Print($"Warning: GLB file found without a corresponding preinstanced entry (this shouldn't happen here): {glbFullPath}", ConsoleColor.Yellow);
                        // mapping[identifier] = new Dictionary<string, string> { ["existing_glb_full"] = glbFullPath, ["map_subdirectory"] = mapSubdirectory };
                    }
                }
            }
        }

        return mapping;
    }

    /// <summary>
    /// Creates symbolic links for the asset folders based on the provided mapping, organizing them by map subdirectory.
    /// </summary>
    /// <param name="assetMap">Mapping of asset identifiers to file paths and metadata.</param>
    /// <param name="rootDrive">Root drive where symbolic links will be created (e.g., "A:\TMP_TSG_LNKS").</param>
    public static void CreateSymbolicLinks(Dictionary<string, Dictionary<string, string>> assetMap, string rootDrive) {
        foreach (var entry in assetMap) {
            var identifier = entry.Key;
            var paths = entry.Value;

            // *** Get the map subdirectory name ***
            var mapSubdirectory = paths.GetValueOrDefault("map_subdirectory");
            if (string.IsNullOrEmpty(mapSubdirectory)) {
                Print($"Error: Missing map subdirectory for identifier {identifier}. Skipping link creation.", ConsoleColor.Red);
                continue; // Skip if map subdirectory is missing
            }

            // *** Construct the base target directory including the map name ***
            var targetBaseDir = Path.Combine(rootDrive, mapSubdirectory);

            // *** Ensure the map-specific subdirectory exists ***
            if (!Directory.Exists(targetBaseDir)) {
                try {
                    Directory.CreateDirectory(targetBaseDir);
                    Print($"Created map subdirectory: {targetBaseDir}", ConsoleColor.DarkCyan);
                } catch (IOException ex) {
                    Print($"Error creating map subdirectory '{targetBaseDir}': {ex.Message}", ConsoleColor.Red);
                    Thread.Sleep(5000); // Pause on error
                    continue; // Skip this asset if the subdirectory cannot be created
                }
            }

            // --- Create symbolic link for preinstanced folder ---
            var preinstancedPath = paths.GetValueOrDefault("preinstanced_full");
            if (preinstancedPath != null && File.Exists(preinstancedPath)) { // Check File exists, link Directory
                var preinstancedSourceFolder = Path.GetDirectoryName(preinstancedPath);
                if (preinstancedSourceFolder != null && Directory.Exists(preinstancedSourceFolder)) {
                    // *** Update link path to include map subdirectory ***
                    var preinstancedLinkFolder = Path.Combine(targetBaseDir, $"{identifier}_preinstanced");
                    try {
                        if (!Directory.Exists(preinstancedLinkFolder)) {
                            Directory.CreateSymbolicLink(preinstancedLinkFolder, preinstancedSourceFolder);
                            Print($"Created symbolic link: {preinstancedLinkFolder} -> {preinstancedSourceFolder}", ConsoleColor.Green);
                            assetMap[identifier]["preinstanced_symlink"] = preinstancedLinkFolder; // Store the created symlink path
                        } else {
                            if (verbose) {
                                Print($"Symbolic link already exists: {preinstancedLinkFolder}", ConsoleColor.Yellow);
                            }
                            assetMap[identifier]["preinstanced_symlink"] = preinstancedLinkFolder; // Still store the path
                        }
                    } catch (IOException e) {
                        Print($"Error creating symbolic link for preinstanced folder ('{preinstancedLinkFolder}' -> '{preinstancedSourceFolder}'): {e.Message}", ConsoleColor.Red);
                        Thread.Sleep(5000); // Pause on error
                    }
                } else {
                    Print($"Source folder not found for preinstanced file: {preinstancedSourceFolder ?? "N/A"} (from: {preinstancedPath})", ConsoleColor.Red);
                }
            } else if (preinstancedPath != null) {
                Print($"Source preinstanced file not found: {preinstancedPath}", ConsoleColor.Yellow);
            }

            // --- Create symbolic link for blend folder ---
            var blendPath = paths.GetValueOrDefault("blend_full");
            // Check if the source blend *file* exists before linking its directory
            if (blendPath != null && File.Exists(blendPath)) {
                var blendSourceFolder = Path.GetDirectoryName(blendPath);
                if (blendSourceFolder != null && Directory.Exists(blendSourceFolder)) {
                    // *** Update link path to include map subdirectory ***
                    var blendLinkFolder = Path.Combine(targetBaseDir, $"{identifier}_blend");
                    try {
                        if (!Directory.Exists(blendLinkFolder)) {
                            Directory.CreateSymbolicLink(blendLinkFolder, blendSourceFolder);
                            Print($"Created symbolic link: {blendLinkFolder} -> {blendSourceFolder}", ConsoleColor.Green);
                            assetMap[identifier]["blend_symlink"] = blendLinkFolder; // Store the symlink path
                        } else {
                            if (verbose) {
                                Print($"Symbolic link already exists: {blendLinkFolder}", ConsoleColor.Yellow);
                            }
                            assetMap[identifier]["blend_symlink"] = blendLinkFolder; // Store the symlink path
                        }
                    } catch (IOException e) {
                        Print($"Error creating symbolic link for blend folder ('{blendLinkFolder}' -> '{blendSourceFolder}'): {e.Message}", ConsoleColor.Red);
                        Thread.Sleep(5000); // Pause on error
                    }
                } else {
                    Print($"Source folder not found for blend file: {blendSourceFolder ?? "N/A"} (from: {blendPath})", ConsoleColor.Red);
                }
            } else if (blendPath != null) {
                Print($"Source blend file not found (expected): {blendPath}", ConsoleColor.Yellow);
            }

            // --- Create symbolic link for predicted GLB folder ---
            // Link the predicted location's directory, even if the file doesn't exist yet.
            var glbPredictedPath = paths.GetValueOrDefault("glb_full");
            if (glbPredictedPath != null) {
                var glbSourceFolder = Path.GetDirectoryName(glbPredictedPath);
                // We link the *predicted* directory, which should exist due to PreinstancedFileProcessor
                if (glbSourceFolder != null && Directory.Exists(glbSourceFolder)) {
                    // *** Update link path to include map subdirectory ***
                    var glbLinkFolder = Path.Combine(targetBaseDir, $"{identifier}_glb");
                    try {
                        if (!Directory.Exists(glbLinkFolder)) {
                            Directory.CreateSymbolicLink(glbLinkFolder, glbSourceFolder);
                            Print($"Created symbolic link: {glbLinkFolder} -> {glbSourceFolder}", ConsoleColor.Magenta);
                            assetMap[identifier]["glb_symlink"] = glbLinkFolder; // Store the symlink path
                        } else {
                            if (verbose) {
                                Print($"Symbolic link already exists: {glbLinkFolder}", ConsoleColor.Yellow);
                            }
                            // Ensure the path is stored even if it exists
                            assetMap[identifier]["glb_symlink"] = glbLinkFolder;
                        }
                    } catch (IOException e) {
                        Print($"Error creating symbolic link for GLB folder ('{glbLinkFolder}' -> '{glbSourceFolder}'): {e.Message}", ConsoleColor.Red);
                        Thread.Sleep(5000); // Pause on error
                    }
                } else {
                    Print($"Source folder not found for predicted GLB file: {glbSourceFolder ?? "N/A"} (from: {glbPredictedPath})", ConsoleColor.Red);
                }
            }

            Print($"Created symbolic links for {identifier} in {mapSubdirectory}", ConsoleColor.Green);
            // Optional sleep for debugging purposes - reduce duration or remove for production
            if (verbose) {
                Print("Verbose mode sleep.", ConsoleColor.Blue);
                Thread.Sleep(100); // Shorter sleep in verbose mode
            }
            // Sleep for a very short time to potentially ease I/O, adjust if needed
            // Thread.Sleep(50); // Reduced sleep
        }
    }
}


// Class responsible for processing .preinstanced files and creating corresponding .blend files
public class PreinstancedFileProcessor {
    // Root directory where .preinstanced files are located
    public string InputDirectory { get; set; }
    // Root directory where corresponding .blend files will be created
    public string BlendDirectory { get; set; }
    // Root directory where corresponding .glb file directories will be created
    public string GLBOutputDirectory { get; set; }
    // Path to a blank .blend file used as a template
    public string BlankBlendSource { get; set; }
    // Flag to enable debug mode with extra logging and sleep delays
    public bool DebugSleep { get; set; } = false;

    /// <summary>
    /// Processes all .preinstanced files in the InputDirectory and its subdirectories.
    /// For each .preinstanced file, it creates a corresponding directory structure in the BlendDirectory
    /// and copies the BlankBlendSource file to the destination, renaming it to match the .preinstanced file.
    /// It also creates the corresponding directory structure in the GLBOutputDirectory.
    /// </summary>
    public void ProcessFiles() {
        if (string.IsNullOrEmpty(InputDirectory) || !Directory.Exists(InputDirectory)) {
            Print($"Error: InputDirectory '{InputDirectory}' is not set or does not exist.", ConsoleColor.Red);
            Thread.Sleep(5000);
            return;
        }

        if (string.IsNullOrEmpty(BlendDirectory)) {
            Print("Error: BlendDirectory is not set.", ConsoleColor.Red);
            Thread.Sleep(5000);
            // Let's not automatically create it here, should be pre-defined.
            return;
        }
        // Ensure Blend Directory exists
        Directory.CreateDirectory(BlendDirectory);


        if (string.IsNullOrEmpty(GLBOutputDirectory)) {
            Print("Error: GLBOutputDirectory is not set.", ConsoleColor.Red);
            Thread.Sleep(5000);
            // Let's not automatically create it here, should be pre-defined.
            return;
        }
        // Ensure GLB Directory exists
        Directory.CreateDirectory(GLBOutputDirectory);


        if (string.IsNullOrEmpty(BlankBlendSource) || !File.Exists(BlankBlendSource)) {
            Print($"Error: BlankBlendSource '{BlankBlendSource}' is not set or does not exist.", ConsoleColor.Red);
            Thread.Sleep(5000);
            return;
        }

        // Find all .preinstanced files in the input directory and its subdirectories
        var preinstancedFiles = Directory.EnumerateFiles(InputDirectory, "*.preinstanced", SearchOption.AllDirectories).ToList();

        Print($"Found {preinstancedFiles.Count} .preinstanced files in {InputDirectory} and its subdirectories.", ConsoleColor.Magenta);

        string normalizedInputDirectory = Path.GetFullPath(InputDirectory); // Normalize for reliable relative path calculation

        // Process each .preinstanced file
        foreach (var preinstancedFile in preinstancedFiles) {
             Print($"Processing preinstanced file: {preinstancedFile}", ConsoleColor.DarkGray); // Less prominent color for individual files

            // Use Path.GetRelativePath for robustness
            string relativePath = Path.GetRelativePath(normalizedInputDirectory, preinstancedFile);

            // Construct the destination directory path for the .blend file
            var destinationDirectory = Path.Combine(BlendDirectory, Path.GetDirectoryName(relativePath));
            // Construct the destination directory path for the .glb file
            var glbDestinationDirectory = Path.Combine(GLBOutputDirectory, Path.GetDirectoryName(relativePath));

            // Create the destination directory for the .blend file if it doesn't exist
            Directory.CreateDirectory(destinationDirectory); // Creates all intermediate directories if needed
            // Create the destination directory for the .glb file if it doesn't exist
            Directory.CreateDirectory(glbDestinationDirectory);

            // Construct the full path for the destination .blend file
            var blankBlendDestination = Path.Combine(destinationDirectory, Path.GetFileNameWithoutExtension(preinstancedFile) + ".blend");

            // Copy the blank .blend file to the destination and rename it
            if (!File.Exists(blankBlendDestination)) {
                if (verbose) {
                    Print($"Copying {Path.GetFileName(BlankBlendSource)} to {blankBlendDestination}", ConsoleColor.Cyan);
                }
                try {
                    File.Copy(BlankBlendSource, blankBlendDestination, true); // Overwrite if exists? Set to false if not desired.
                    if (verbose) Print($"Copied {Path.GetFileName(BlankBlendSource)} to {blankBlendDestination}", ConsoleColor.Green);
                } catch (IOException ex) {
                    Print($"Error copying blank blend file to '{blankBlendDestination}': {ex.Message}", ConsoleColor.Red);
                    // Decide if processing should continue or stop
                    Thread.Sleep(2000); // Pause briefly on error
                }
            } else {
                if (DebugSleep || verbose) { // Show message if verbose or debug sleeping
                    Print($"{Path.GetFileName(blankBlendDestination)} already exists, skipping copy.", ConsoleColor.DarkYellow);
                }
            }

            // Optional sleep for debugging purposes
            if (DebugSleep) {
                Print("Debug mode enabled. Sleeping.", ConsoleColor.Blue);
                Thread.Sleep(500); // Shorter sleep
            }
        }

        Print($"Total .preinstanced files processed: {preinstancedFiles.Count}", ConsoleColor.DarkYellow);
    }
}

// --- Main Execution ---

try { // Wrap main logic in a try-catch block

    // Determine the root path to GameFiles
    string workingDirRoot = Directory.GetCurrentDirectory();
    Print($"Working Directory: {workingDirRoot}", ConsoleColor.Cyan);

    // Define the root directories for asset files
    var preinstancedDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Flattened_OUTPUT");
    var blendDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Blender_TMP_OUTPUT");
    var glbDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Assets_Blender_OUTPUT");
    // Ensure Tools\process\6_Asset directory exists before writing the file
    var outputDir = Path.Combine(workingDirRoot, "Tools", "process", "6_Asset");
    Directory.CreateDirectory(outputDir);
    var outputFile = Path.Combine(outputDir, "asset_mapping.json");

    // Determine the root drive letter for symbolic links
    var rootDriveLetter = Path.GetPathRoot(Directory.GetCurrentDirectory()); // Get the drive letter (e.g., "A:\")
    if (string.IsNullOrEmpty(rootDriveLetter)){
        throw new InvalidOperationException("Could not determine root drive letter from working directory.");
    }
    var rootDrive = Path.Combine(rootDriveLetter, "TMP_TSG_LNKS");

    // --- Step 1: Process Preinstanced Files ---
    Print("--- Step 1: Processing Preinstanced Files ---", ConsoleColor.Yellow);
    // Instantiate the PreinstancedFileProcessor and configure its properties
    var processor = new PreinstancedFileProcessor {
        InputDirectory = preinstancedDir,
        BlendDirectory = blendDir,
        GLBOutputDirectory = glbDir,
        BlankBlendSource = Path.Combine(workingDirRoot, "blank.blend"), // Assuming blank.blend is in the working directory
        DebugSleep = false // Set to true for debug mode with pauses
    };
    Print($"Starting to process files in: {preinstancedDir}", ConsoleColor.Cyan);
    Thread.Sleep(1000); // Short pause before starting
    processor.ProcessFiles();
    Print("--- Step 1: Completed ---", ConsoleColor.Yellow);
    Thread.Sleep(1000);


    // --- Step 2: Setup Symbolic Link Root Directory ---
    Print($"--- Step 2: Preparing Symbolic Link Root Directory: {rootDrive} ---", ConsoleColor.Yellow);
    // Create the root directory for symbolic links, clearing if it exists
    try {
        if (Directory.Exists(rootDrive)) {
            Print($"Deleting existing root directory for symbolic links: {rootDrive}", ConsoleColor.Yellow);
            Directory.Delete(rootDrive, true);
             Thread.Sleep(1000); // Give system time to delete
        }
        Directory.CreateDirectory(rootDrive);
        Print($"Created root directory for symbolic links: {rootDrive}", ConsoleColor.Green);
        Thread.Sleep(500); // Pause after creation
    } catch (IOException ex) {
        Print($"Error managing root symbolic link directory '{rootDrive}': {ex.Message}. Check permissions or locks.", ConsoleColor.Red);
        Thread.Sleep(5000);
        return; // Exit if root link directory fails
    } catch (UnauthorizedAccessException ex) {
        Print($"Error managing root symbolic link directory '{rootDrive}': {ex.Message}. Run as Administrator?", ConsoleColor.Red);
        Thread.Sleep(5000);
        return; // Exit if root link directory fails
    }

    // --- Step 3: Generate Asset Mapping ---
    Print("--- Step 3: Generating Asset Map ---", ConsoleColor.Yellow);
    Thread.Sleep(1000);
    // Generate the asset mapping, including the new map subdirectory info
    var assetMap = AssetMapper.GenerateAssetMapping(rootDrive, preinstancedDir, blendDir, glbDir, checkExistence: false); // Pass rootDrive for context if needed later

    Print($"Generated map for {assetMap.Count} assets.", ConsoleColor.Cyan);
    Thread.Sleep(1000);

    // Save the initial asset map to a JSON file (optional, could be removed if only final map is needed)
    Print($"--- Saving initial asset map to: {outputFile} ---", ConsoleColor.DarkYellow);
    var options = new JsonSerializerOptions { WriteIndented = true }; // Define options here
    try {
        var initialJsonString = JsonSerializer.Serialize(assetMap, options);
        File.WriteAllText(outputFile, initialJsonString);
        Print($"Initial asset mapping saved to: {outputFile}", ConsoleColor.DarkYellow);
    } catch (Exception ex) {
        Print($"ERROR saving initial asset map: {ex.Message}", ConsoleColor.Red);
        Thread.Sleep(5000);
        // Decide if you want to continue if initial save fails
    }
    Thread.Sleep(1000);

    // --- Step 4: Create Symbolic Links ---
    Print($"--- Step 4: Creating Symbolic Links in: {rootDrive} ---", ConsoleColor.Yellow);
    Thread.Sleep(1000);
    // Create the symbolic links based on the generated asset map
    // This function MODIFIES the assetMap IN MEMORY by adding _symlink keys
    AssetMapper.CreateSymbolicLinks(assetMap, rootDrive);
    Print("--- Step 4: Symbolic links creation process completed. ---", ConsoleColor.Green);
    Thread.Sleep(500); // Short pause after creation


    // *** NEW: Save the UPDATED asset map (including symlink paths) ***
    Print($"--- Saving updated asset map with symlink paths to: {outputFile} ---", ConsoleColor.Yellow);
    try {
        // Reuse the options variable defined in Step 3
        var updatedJsonString = JsonSerializer.Serialize(assetMap, options); // Serialize the UPDATED map
        File.WriteAllText(outputFile, updatedJsonString); // Overwrite the file with the new data
        Print($"Successfully saved updated asset mapping to: {outputFile}", ConsoleColor.Green);
    } catch (Exception ex) {
        Print($"ERROR saving updated asset map: {ex.Message}", ConsoleColor.Red);
        Thread.Sleep(5000); // Pause on error
    }
    Thread.Sleep(1000); // Optional short pause after saving


} catch (DirectoryNotFoundException e) {
    Print($"ERROR: Directory not found - {e.Message}", ConsoleColor.Red);
    Thread.Sleep(10000); // Pause longer on critical errors
} catch (UnauthorizedAccessException e) {
    Print($"ERROR: Access Denied - {e.Message}. Try running as Administrator.", ConsoleColor.Red);
    Thread.Sleep(10000);
} catch (Exception e) {
    Print($"An unexpected ERROR occurred: {e.GetType().Name} - {e.Message}", ConsoleColor.Red);
    Print($"Stack Trace: {e.StackTrace}", ConsoleColor.DarkRed); // Print stack trace for debugging
    Thread.Sleep(10000);
}

Print("Script finished.", ConsoleColor.White);
