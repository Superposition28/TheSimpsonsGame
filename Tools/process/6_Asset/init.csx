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
// It uses MD5 hashing to generate unique identifiers for each asset.

public static bool verbose = false; // Set to true for verbose output

public static void print(string message, ConsoleColor color = ConsoleColor.White) {
    if (verbose) {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}

// Main class containing core functionality for asset mapping and symbolic link creation
public class main {
    /// <summary>
    /// Generates a mapping of assets by processing preinstanced, blend, and optional GLB files.
    /// </summary>
    /// <param name="preinstancedRoot">Root directory containing .preinstanced files.</param>
    /// <param name="blendRoot">Root directory containing .blend files.</param>
    /// <param name="glbRoot">Optional root directory for .glb files.</param>
    /// <param name="checkExistence">If true, checks for the existence of corresponding blend files.</param>
    /// <returns>A dictionary mapping unique identifiers to asset file paths.</returns>
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

        // Process each .preinstanced file
        foreach (var preinstancedFile in Directory.EnumerateFiles(preinstancedRoot, "*.preinstanced", SearchOption.AllDirectories)) {
            var preinstancedRelativePath = Path.GetRelativePath(preinstancedRoot, preinstancedFile);
            var blendRelativePath = preinstancedRelativePath.Replace(".preinstanced", ".blend");
            var blendFullPath = Path.Combine(blendRoot, blendRelativePath);

            var glbRelativePath = preinstancedRelativePath.Replace(".preinstanced", ".glb");
            var glbFullPath = glbRoot != null ? Path.Combine(glbRoot, glbRelativePath) : null;

            // Skip if blend file doesn't exist and checkExistence is true
            if (checkExistence && !File.Exists(blendFullPath)) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Corresponding blend file not found: {blendFullPath}");
                Console.ResetColor();
                continue;
            }

            // Generate a unique identifier using MD5 hash of the blend relative path
            using (var md5 = MD5.Create()) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"blendRelativePath: {blendRelativePath}");
                Console.WriteLine($"preinstancedRelativePath: {preinstancedRelativePath}");
                Console.WriteLine($"glbRelativePath: {glbRelativePath}");
                Console.ResetColor();
                //Thread.Sleep(500000);
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(preinstancedRelativePath));
                var identifier = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                var assetInfo = new Dictionary<string, string> {
                    ["preinstanced_full"] = preinstancedFile,
                    ["blend_full"] = blendFullPath,
                    ["predicted_glb_full"] = glbFullPath
                };

                // Generate symlink paths
                assetInfo["preinstanced_symlink"] = Path.Combine(rootDrive, identifier + "_preinstanced", Path.GetFileName(preinstancedFile));
                assetInfo["blend_symlink"] = Path.Combine(rootDrive, identifier + "_blend", Path.GetFileName(blendFullPath));
                assetInfo["predicted_glb_symlink"] = glbRoot != null ? Path.Combine(rootDrive, identifier + "_glb", Path.GetFileName(glbFullPath)) : null;

                mapping[identifier] = assetInfo;
                // Optionally process existing GLB files to add them to the mapping
                if (glbRoot != null) {

                    //var glbFile = Directory.EnumerateFiles(glbRoot, "*.glb", SearchOption.AllDirectories);
                    //var glbRelativePath2 = Path.GetRelativePath(glbRoot, glbFile);
                    
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"glbRelativePath: {glbRelativePath}");
                    //Console.WriteLine($"glbRelativePath: {glbRelativePath2}");
                    Console.ResetColor();
                    //Thread.Sleep(50000);

                    //var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(blendRelativePath));
                    //var identifier = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    if (mapping.ContainsKey(identifier)) {
                        // If an entry for this identifier already exists (from a preinstanced file),
                        // update it with the actual GLB file path.
                        mapping[identifier]["glb_full"] = glbFullPath;
                        // TODO: Should we also update the predicted GLB symlink path here?
                    } else {
                        // If no corresponding preinstanced file was found, create a new entry.
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: GLB file found without a corresponding preinstanced entry: {glbFullPath}");
                        Console.ResetColor();
                        mapping[identifier] = new Dictionary<string, string> { ["glb_full"] = glbFullPath };
                        // TODO: Should we generate a predicted GLB symlink path even without a preinstanced file?
                    }

                }
            }
        }



        return mapping;
    }

    /// <summary>
    /// Creates symbolic links for the asset folders based on the provided mapping.
    /// </summary>
    /// <param name="assetMap">Mapping of asset identifiers to file paths.</param>
    /// <param name="rootDrive">Root drive where symbolic links will be created.</param>
    public static void CreateSymbolicLinks(Dictionary<string, Dictionary<string, string>> assetMap, string rootDrive) {
        foreach (var entry in assetMap) {
            var identifier = entry.Key;
            var paths = entry.Value;

            // Create symbolic link for preinstanced folder
            var preinstancedPath = paths.GetValueOrDefault("preinstanced_full");
            if (preinstancedPath != null) {
                var preinstancedSourceFolder = Path.GetDirectoryName(preinstancedPath);
                if (preinstancedSourceFolder != null && Directory.Exists(preinstancedSourceFolder)) {
                    var preinstancedLinkFolder = Path.Combine(rootDrive, $"{identifier}_preinstanced");
                    try {
                        if (!Directory.Exists(preinstancedLinkFolder)) {
                            Directory.CreateSymbolicLink(preinstancedLinkFolder, preinstancedSourceFolder);
                            Console.WriteLine($"Created symbolic link: {preinstancedLinkFolder} -> {preinstancedSourceFolder}");
                            assetMap[identifier]["preinstanced_symlink"] = preinstancedLinkFolder;
                        } else {
                            print($"Symbolic link already exists: {preinstancedLinkFolder}", ConsoleColor.Yellow);
                            assetMap[identifier]["preinstanced_symlink"] = preinstancedLinkFolder;
                        }
                    } catch (IOException e) {
                        Console.WriteLine($"Error creating symbolic link for preinstanced folder ({preinstancedSourceFolder}): {e.Message}");
                        Thread.Sleep(50000);
                    }
                } else {
                    Console.WriteLine($"Source folder not found for preinstanced file: {preinstancedPath}");
                }
            }

            // Create symbolic link for blend folder
            var blendPath = paths.GetValueOrDefault("blend_full");
            if (blendPath != null) {
                var blendSourceFolder = Path.GetDirectoryName(blendPath);
                if (blendSourceFolder != null && Directory.Exists(blendSourceFolder)) {
                    var blendLinkFolder = Path.Combine(rootDrive, $"{identifier}_blend");
                    try {
                        if (!Directory.Exists(blendLinkFolder)) {
                            Directory.CreateSymbolicLink(blendLinkFolder, blendSourceFolder);
                            Console.WriteLine($"Created symbolic link: {blendLinkFolder} -> {blendSourceFolder}");
                            assetMap[identifier]["blend_symlink"] = blendLinkFolder; // Store the symlink path
                        } else {
                            print($"Symbolic link already exists: {blendLinkFolder}", ConsoleColor.Yellow);
                            assetMap[identifier]["blend_symlink"] = blendLinkFolder; // Store the symlink path
                        }
                    } catch (IOException e) {
                        Console.WriteLine($"Error creating symbolic link for blend folder ({blendSourceFolder}): {e.Message}");
                        Thread.Sleep(50000);
                    }
                } else {
                    Console.WriteLine($"Source folder not found for blend file: {blendPath}");
                }
            }

            // Create symbolic link for predicted GLB folder
            var glbPath = paths.GetValueOrDefault("predicted_glb_full");
            if (glbPath != null) {
                var glbSourceFolder = Path.GetDirectoryName(glbPath);
                if (glbSourceFolder != null && Directory.Exists(glbSourceFolder)) {
                    var glbLinkFolder = Path.Combine(rootDrive, $"{identifier}_glb");
                    try {
                        if (!Directory.Exists(glbLinkFolder)) {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Directory.CreateSymbolicLink(glbLinkFolder, glbSourceFolder);
                            Console.WriteLine($"Created symbolic link: {glbLinkFolder} -> {glbSourceFolder}");
                            Console.ResetColor();
                            assetMap[identifier]["predicted_glb_symlink"] = glbLinkFolder; // Store the symlink path
                        } else {
                            print($"Symbolic link already exists: {glbLinkFolder}", ConsoleColor.Yellow);
                            assetMap[identifier]["predicted_glb_symlink"] = "true"; // Store the symlink path
                        }

                        //Thread.Sleep(10000); // Sleep for 10 seconds to avoid overwhelming the file system

                    } catch (IOException e) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error creating symbolic link for GLB folder ({glbSourceFolder}): {e.Message}");
                        Console.ResetColor();
                        Thread.Sleep(50000);
                        // TODO: Same question as above for this sleep.
                    }
                } else {
                    Console.WriteLine($"Source folder not found for predicted GLB file: {glbPath}");
                }
            }
        }
    }
}

// Class responsible for processing .preinstanced files and creating corresponding .blend files
public class PreinstancedFileProcessor {
    // Root directory where .preinstanced files are located
    public string InputDirectory { get; set; }
    // Root directory where corresponding .blend files will be created
    public string BlendDirectory { get; set; }
    // Root directory where corresponding .glb files will be created (this processor only creates the directories)
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
        if (string.IsNullOrEmpty(InputDirectory)) {
            Console.WriteLine("Error: InputDirectory is not set.");
            Thread.Sleep(50000);
            return;
        }

        if (string.IsNullOrEmpty(BlendDirectory)) {
            Console.WriteLine("Error: BlendDirectory is not set.");
            // Ensure the directory exists
            if (!Directory.Exists(BlendDirectory)) {
                Directory.CreateDirectory(BlendDirectory);
                Console.WriteLine($"Created directory: {BlendDirectory}");
            }
            Thread.Sleep(50000);
        }

        if (string.IsNullOrEmpty(GLBOutputDirectory)) {
            Console.WriteLine("Error: GLBOutputDirectory is not set.");
            // Ensure the directory exists
            if (!Directory.Exists(GLBOutputDirectory)) {
                Directory.CreateDirectory(GLBOutputDirectory);
                Console.WriteLine($"Created directory: {GLBOutputDirectory}");
            }
            Thread.Sleep(50000);
        }

        if (string.IsNullOrEmpty(BlankBlendSource)) {
            Console.WriteLine("Error: BlankBlendSource is not set.");
            Thread.Sleep(50000);
            return;
        }

        // Find all .preinstanced files in the input directory and its subdirectories
        var preinstancedFiles = Directory.EnumerateFiles(InputDirectory, "*.preinstanced", SearchOption.AllDirectories).ToList();

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"Found {preinstancedFiles.Count} .preinstanced files in {InputDirectory} and its subdirectories.");
        Console.ResetColor();

        // Process each .preinstanced file
        foreach (var preinstancedFile in preinstancedFiles) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Processing preinstanced file: {preinstancedFile}");
            Console.ResetColor();

            string relativePath;
            // Determine the relative path of the current .preinstanced file with respect to the InputDirectory
            if (preinstancedFile.StartsWith(InputDirectory, StringComparison.OrdinalIgnoreCase)) {
                relativePath = preinstancedFile.Substring(InputDirectory.Length).TrimStart('\\', '/');
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Preinstanced file '{preinstancedFile}' is not within the input directory '{InputDirectory}'. Skipping.");
                Console.ResetColor();
                Thread.Sleep(50000);
                continue;
            }

            // Debug logging for relative path components
            if (DebugSleep) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Relative Path: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(relativePath);
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("inputDirectory Path Length: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(InputDirectory.Length);
                Console.ResetColor();

                var relativePathComponents = relativePath.Split('\\');
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Relative Path Components: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(string.Join(", ", relativePathComponents));
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Relative Path Components Count: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(relativePathComponents.Length);
                Console.ResetColor();
                for (int i = 0; i < Math.Min(4, relativePathComponents.Length); i++) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"Relative Path Components: ");
                }
            }

            // Construct the destination directory path for the .blend file
            var destinationDirectory = Path.Combine(BlendDirectory, Path.GetDirectoryName(relativePath));
            // Construct the destination directory path for the .glb file
            var glbDestinationDirectory = Path.Combine(GLBOutputDirectory, Path.GetDirectoryName(relativePath));

            // Debug logging for destination directories
            if (DebugSleep) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Destination Directory: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(destinationDirectory);
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("GLB Destination Directory: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(glbDestinationDirectory);
                Console.ResetColor();
            }

            // Create the destination directory for the .blend file if it doesn't exist
            if (!Directory.Exists(destinationDirectory)) {
                Directory.CreateDirectory(destinationDirectory);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Created directory: {destinationDirectory}");
                Console.ResetColor();
            }

            // Create the destination directory for the .glb file if it doesn't exist
            if (!Directory.Exists(glbDestinationDirectory)) {
                Directory.CreateDirectory(glbDestinationDirectory);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Created directory: {glbDestinationDirectory}");
                Console.ResetColor();
            }

            // Construct the full path for the destination .blend file
            var blankBlendDestination = Path.Combine(destinationDirectory, Path.GetFileNameWithoutExtension(preinstancedFile) + ".blend");

            // Copy the blank .blend file to the destination and rename it
            if (!File.Exists(blankBlendDestination)) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Copying {Path.GetFileName(BlankBlendSource)} to {blankBlendDestination}");
                Console.ResetColor();
                File.Copy(BlankBlendSource, blankBlendDestination, true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Copied and renamed {Path.GetFileName(BlankBlendSource)} to {blankBlendDestination}");
                Console.ResetColor();
            } else {
                if (DebugSleep) {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"{blankBlendDestination} already exists, skipping.");
                    Console.ResetColor();
                }
            }

            // Optional sleep for debugging purposes
            if (DebugSleep) {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Debug mode enabled. Sleeping for 15 seconds.");
                Console.ResetColor();
                Thread.Sleep(5000);
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"Total .preinstanced files processed: {preinstancedFiles.Count}");
        Console.ResetColor();
    }
}

// Determine the root path to GameFiles
string workingDirRoot = Directory.GetCurrentDirectory();
Console.WriteLine($"Working Directory: {workingDirRoot}");

// Define the root directories for asset files
var preinstancedDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Flattened_OUTPUT");
var blendDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Blender_TMP_OUTPUT");
var glbDir = Path.Combine(workingDirRoot, "GameFiles", "Main", "PS3_GAME", "Assets_Blender_OUTPUT");
var outputFile = "Tools\\process\\6_Asset\\asset_mapping.json"; // Choose a name for your output file
// Determine the root drive letter for symbolic links
var rootDriveLetter = Path.GetPathRoot(Directory.GetCurrentDirectory()); // Get the drive letter of the current directory
var rootDrive = Path.Combine(rootDriveLetter, "TMP_TSG_LNKS");

// Instantiate the PreinstancedFileProcessor and configure its properties
var processor = new PreinstancedFileProcessor {
    InputDirectory = preinstancedDir,
    BlendDirectory = blendDir,
    GLBOutputDirectory = glbDir,
    BlankBlendSource = Path.Combine(workingDirRoot, "blank.blend"),
    DebugSleep = false // Set to true for debug mode
};
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"Starting to process files in: {preinstancedDir}");
Console.ResetColor();
Thread.Sleep(3333);
// Start processing the .preinstanced files
processor.ProcessFiles();

try {

    // Create the root directory for symbolic links if it doesn't exist
    if (!Directory.Exists(rootDrive)) {
        Directory.CreateDirectory(rootDrive);
        Console.WriteLine($"Created root directory for symbolic links: {rootDrive}");
        Thread.Sleep(5000);
    } else {
        // delete directory if it exists
        Directory.Delete(rootDrive, true);
        Console.WriteLine($"Deleted existing root directory for symbolic links: {rootDrive}");
        if (!Directory.Exists(rootDrive)) {
            Directory.CreateDirectory(rootDrive);
            Console.WriteLine($"Created root directory for symbolic links: {rootDrive}");
        } else {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Failed to create root directory for symbolic links: {rootDrive}");
            Console.ResetColor();
            Thread.Sleep(5000);
            return;
        }
        Thread.Sleep(5000);
    }

    // Check if the root drive is valid
    if (!Directory.Exists(rootDrive)) {
        throw new DirectoryNotFoundException($"Root drive not found: {rootDrive}");
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Generating asset map");
    Console.ResetColor();
    Thread.Sleep(3333);
    // Generate the asset mapping
    var assetMap = main.GenerateAssetMapping(rootDrive, preinstancedDir, blendDir, glbDir, checkExistence: false);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Generated map for {assetMap.Count} assets.");
    Console.ResetColor();
    Thread.Sleep(3333);

    // Save the asset map to a JSON file
    var options = new JsonSerializerOptions { WriteIndented = true };
    var jsonString = JsonSerializer.Serialize(assetMap, options);
    File.WriteAllText(outputFile, jsonString);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Asset mapping saved to: {outputFile}");
    Console.ResetColor();
    Thread.Sleep(2112);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Starting process to create symbolic links in: {rootDrive}");
    Console.ResetColor();
    Thread.Sleep(3333);
    // Create the symbolic links based on the generated asset map
    main.CreateSymbolicLinks(assetMap, rootDrive);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Symbolic links creation process completed.");
    Console.ResetColor();
} catch (DirectoryNotFoundException e) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {e.Message}");
    Console.ResetColor();
    Thread.Sleep(50000);
} catch (Exception e) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An unexpected error occurred: {e.Message}");
    Console.ResetColor();
    Thread.Sleep(50000);
}