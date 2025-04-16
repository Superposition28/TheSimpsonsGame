#r "System.IO.FileSystem"
#r "System.Text.Json"
#r "System.Security.Cryptography"

// using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// This script automates the process of mapping game assets (txd, and png files)
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
    /// Extracts the map/area subdirectory name from the full txd file path.
    /// Assumes the structure contains "\PS3_GAME\Flattened_OUTPUT\[MapName]\...".
    /// </summary>
    /// <param name="fullPath">The full path to the .txd file.</param>
    /// <param name="txdRoot">The root directory containing .txd files.</param>
    /// <returns>The extracted map subdirectory name, or "_UNKNOWN_MAP" if not found.</returns>
    private static string ExtractMapSubdirectory(string fullPath, string txdRoot) {
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
    /// Generates a mapping of assets by processing txd, png.
    /// </summary>
    /// <param name="rootDrive">The root drive where symbolic links will eventually be created.</param>
    /// <param name="txdRoot">Root directory containing .txd files.</param>
    /// <param name="pngRoot">root directory for .png files.</param>
    /// <returns>A dictionary mapping unique identifiers to asset file paths and metadata.</returns>
    public static Dictionary<string, Dictionary<string, string>> GenerateAssetMapping(string rootDrive, string txdRoot, string pngRoot = null) {
        // Validate input directories
        if (!Directory.Exists(txdRoot)) {
            throw new DirectoryNotFoundException($"txd root directory not found: {txdRoot}");
        }
        if (pngRoot != null && !Directory.Exists(pngRoot)) {
            Directory.CreateDirectory(pngRoot); // Create PNG directory if it doesn't exist
        }

        var mapping = new Dictionary<string, Dictionary<string, string>>();
        string normalizedtxdRoot = Path.GetFullPath(txdRoot); // Normalize for reliable relative path calculation

        // Process each .txd file
        foreach (var txdFile in Directory.EnumerateFiles(normalizedtxdRoot, "*.txd", SearchOption.AllDirectories)) {
            var txdRelativePath = Path.GetRelativePath(normalizedtxdRoot, txdFile);
			var txdFullPath = Path.Combine(txdRoot, txdRelativePath); // Full path for txd file

            var pngRelativePath = Path.ChangeExtension(txdRelativePath, ".png");
            var pngFullPath = pngRoot != null ? Path.Combine(pngRoot, pngRelativePath) : null;


            // *** NEW: Extract the map subdirectory name ***
            string mapSubdirectory = ExtractMapSubdirectory(txdFile, normalizedtxdRoot);
            if (verbose) {
                Print($"Extracted Map Subdirectory: {mapSubdirectory} for {txdFile}", ConsoleColor.DarkCyan);
            }

            // Generate a unique identifier using MD5 hash of the *relative txd path* for consistency
            using (var md5 = MD5.Create()) {
                // Use txd relative path for hashing to ensure uniqueness based on source structure
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(txdRelativePath.Replace('\\', '/'))); // Normalize separators for hash consistency
                var identifier = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                if (verbose) {
                    Print($"Hashed Path (for ID {identifier}): {txdRelativePath}", ConsoleColor.Cyan);
                }

                var assetInfo = new Dictionary<string, string> {
                    ["map_subdirectory"] = mapSubdirectory, // Store the extracted map name
					["filename"] = Path.GetFileNameWithoutExtension(txdFile),
                    ["txd_full"] = txdFile,
                    ["png_full"] = pngFullPath
                    // Symlink paths will be calculated and added later during link creation
                };

                // Add or update the mapping
                if (mapping.ContainsKey(identifier)) {
                    // Handle potential hash collisions or duplicates if necessary
                    Print($"Warning: Identifier collision detected for {identifier}. Overwriting entry. Check source files: {txdFile} and existing.", ConsoleColor.Yellow);
                }
                mapping[identifier] = assetInfo;


                // Optionally process existing PNG files if pngRoot is provided
                // Note: This part might need adjustment depending on how PNG files are definitively matched.
                // The current logic assumes a PNG exists if a txd file exists.
                if (pngRoot != null && File.Exists(pngFullPath)) {
                    if (mapping.ContainsKey(identifier)) {
                        mapping[identifier]["existing_png_full"] = pngFullPath; // Update key for existing PNG
                    } else {
                        // This case should ideally not happen if GenerateAssetMapping logic is correct
                        Print($"Warning: PNG file found without a corresponding txd entry (this shouldn't happen here): {pngFullPath}", ConsoleColor.Yellow);
                        // mapping[identifier] = new Dictionary<string, string> { ["existing_png_full"] = pngFullPath, ["map_subdirectory"] = mapSubdirectory };
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

            // --- Create symbolic link for txd folder ---
            var txdPath = paths.GetValueOrDefault("txd_full");
            if (txdPath != null && File.Exists(txdPath)) { // Check File exists, link Directory
                var txdSourceFolder = Path.GetDirectoryName(txdPath);
                if (txdSourceFolder != null && Directory.Exists(txdSourceFolder)) {
                    // *** Update link path to include map subdirectory ***
                    var txdLinkFolder = Path.Combine(targetBaseDir, $"{identifier}_txd");
                    try {
                        if (!Directory.Exists(txdLinkFolder)) {
                            Directory.CreateSymbolicLink(txdLinkFolder, txdSourceFolder);
                            Print($"Created symbolic link: {txdLinkFolder} -> {txdSourceFolder}", ConsoleColor.Green);
                            assetMap[identifier]["txd_symlink"] = txdLinkFolder; // Store the created symlink path
                        } else {
                            if (verbose) {
                                Print($"Symbolic link already exists: {txdLinkFolder}", ConsoleColor.Yellow);
                            }
                            assetMap[identifier]["txd_symlink"] = txdLinkFolder; // Still store the path
                        }
                    } catch (IOException e) {
                        Print($"Error creating symbolic link for txd folder ('{txdLinkFolder}' -> '{txdSourceFolder}'): {e.Message}", ConsoleColor.Red);
                        Thread.Sleep(5000); // Pause on error
                    }
                } else {
                    Print($"Source folder not found for txd file: {txdSourceFolder ?? "N/A"} (from: {txdPath})", ConsoleColor.Red);
                }
            } else if (txdPath != null) {
                Print($"Source txd file not found: {txdPath}", ConsoleColor.Yellow);
            }

            // --- Create symbolic link for predicted PNG folder ---
            // Link the predicted location's directory, even if the file doesn't exist yet.
            var pngPredictedPath = paths.GetValueOrDefault("png_full");
            if (pngPredictedPath != null) {
                var pngSourceFolder = Path.GetDirectoryName(pngPredictedPath);
                // We link the *predicted* directory, which should exist due to txdFileProcessor
                if (pngSourceFolder != null && Directory.Exists(pngSourceFolder)) {
                    // *** Update link path to include map subdirectory ***
                    var pngLinkFolder = Path.Combine(targetBaseDir, $"{identifier}_png");
                    try {
                        if (!Directory.Exists(pngLinkFolder)) {
                            Directory.CreateSymbolicLink(pngLinkFolder, pngSourceFolder);
                            Print($"Created symbolic link: {pngLinkFolder} -> {pngSourceFolder}", ConsoleColor.Magenta);
                            assetMap[identifier]["png_symlink"] = pngLinkFolder; // Store the symlink path
                        } else {
                            if (verbose) {
                                Print($"Symbolic link already exists: {pngLinkFolder}", ConsoleColor.Yellow);
                            }
                            // Ensure the path is stored even if it exists
                            assetMap[identifier]["png_symlink"] = pngLinkFolder;
                        }
                    } catch (IOException e) {
                        Print($"Error creating symbolic link for PNG folder ('{pngLinkFolder}' -> '{pngSourceFolder}'): {e.Message}", ConsoleColor.Red);
                        Thread.Sleep(5000); // Pause on error
                    }
                } else {
                    Print($"Source folder not found for predicted PNG file: {pngSourceFolder ?? "N/A"} (from: {pngPredictedPath})", ConsoleColor.Red);
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


// --- Main Execution ---

try { // Wrap main logic in a try-catch block

    // Determine the root path to GameFiles
    string workingDirRoot = Directory.GetCurrentDirectory();
    Print($"Working Directory: {workingDirRoot}", ConsoleColor.Cyan);

    // Define the root directories for asset files
    var txdDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Flattened_OUTPUT");
    var pngDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Flattened_OUTPUT");
    // Ensure Tools\process\9_Texture directory exists before writing the file
    var outputDir = Path.Combine(workingDirRoot, "Tools", "process", "9_Texture");
    Directory.CreateDirectory(outputDir);
    var outputFile = Path.Combine(outputDir, "texture_mapping.json");

    // Determine the root drive letter for symbolic links
    var rootDriveLetter = Path.GetPathRoot(Directory.GetCurrentDirectory()); // Get the drive letter (e.g., "A:\")
    if (string.IsNullOrEmpty(rootDriveLetter)){
        throw new InvalidOperationException("Could not determine root drive letter from working directory.");
    }
    var rootDrive = Path.Combine(rootDriveLetter, "TMP_TSG_LNKS_TXD");

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
    var assetMap = AssetMapper.GenerateAssetMapping(rootDrive, txdDir, pngDir); // Pass rootDrive for context if needed later

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

// Helper to log to main.log with timestamp
void LogToFile(string message) {
	string logPath = Path.Combine(Directory.GetCurrentDirectory(), "main.log");
	string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
	File.AppendAllText(logPath, $"[{timestamp}] {message}{Environment.NewLine}");
}

// Open Noesis executable and provide batch instructions
var noesisPath = "./Tools/noesis/exe/Noesis64.exe";
if (File.Exists(noesisPath)) {
	Print($"Opening Noesis: {noesisPath}", ConsoleColor.Cyan);
	
	// Launch Noesis
	System.Diagnostics.Process.Start(noesisPath);
	Print("Noesis launched. Please complete the batch export process manually.", ConsoleColor.Green);

	// Print instructions for batch processing in Noesis
	Print("=== Noesis Batch Processing Instructions ===", ConsoleColor.DarkCyan);
	Print("1. Click 'Tools' > 'Batch Process'.", ConsoleColor.DarkCyan);
	Print("2. In the batch process window, set the following:", ConsoleColor.DarkCyan);
	Print("   - Input extension:      txd", ConsoleColor.DarkCyan);
	Print("   - Output extension:     png", ConsoleColor.DarkCyan);
	Print("   - Output path:          $inpath$\\$inname$.txd_files\\$inname$out.$outext$", ConsoleColor.DarkCyan);
	Print("   - Check 'Recursive'.", ConsoleColor.DarkCyan);
	Print("3. Click 'Folder Batch' and select the folder:", ConsoleColor.DarkCyan);
	Print($"   {Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "TMP_TSG_LNKS_TXD")}", ConsoleColor.Cyan);
	Print("4. Click 'Export' to begin the conversion process.", ConsoleColor.DarkCyan);
	Print("============================================", ConsoleColor.DarkCyan);

	// Prompt: Wait for user to confirm they're ready to start batch process
	Print("Press ENTER once you've configured the settings and are ready to start the batch process in Noesis...", ConsoleColor.White);
	Console.ReadLine(); // Wait for confirmation to begin
	LogToFile("User confirmed start of batch process in Noesis.");

	// Prompt: Wait for user to confirm batch process is finished
	Print("Press ENTER once the batch processing in Noesis is completed...", ConsoleColor.White);
	Console.ReadLine(); // Wait for confirmation that batch export is done
	LogToFile("User confirmed completion of batch process in Noesis.");

	Print("Batch processing confirmed complete.", ConsoleColor.Green);

} else {
	Print($"Noesis executable not found at: {noesisPath}", ConsoleColor.Red);
	LogToFile($"ERROR: Noesis executable not found at: {noesisPath}");
}
