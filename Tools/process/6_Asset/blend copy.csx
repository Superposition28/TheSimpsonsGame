// Requires:
// #r "System.IO"
// #r "System.Text.Json"
// #r "System.Diagnostics.Process"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;

// Add this line to define args for the script
string[] args = Environment.GetCommandLineArgs();

bool verbose = false;
bool debugSleep = false;

// Parse command-line arguments
foreach (string arg in args) {
    if (arg.Equals("-Verbose", StringComparison.OrdinalIgnoreCase)) {
        verbose = true;
    } else if (arg.Equals("-DebugSleep", StringComparison.OrdinalIgnoreCase)) {
        debugSleep = true;
    }
}

string pythonScriptPath = "./Tools/blender/main.py";
string pythonExtensionFile = "./Tools/blender/io_import_simpson_game_ScriptMode.py";
string assetMappingFile = "asset_mapping.json";
string configFile = "config.ini";
string blenderExePath = "blender4.3"; // Default path

int loopCount = 0;

// Ensure blendFilePath is defined in the appropriate scope
string blendFilePath = ""; // Initialize with an empty string or appropriate default value

// Function to read INI file
static Dictionary<string, string> ReadIniFile(string path) {
    var config = new Dictionary<string, string>();
    try {
        foreach (var line in File.ReadLines(path)) {
            if (line.Contains('=')) {
                var parts = line.Split('=', 2);
                if (parts.Length == 2 && parts[0].Trim().Equals("BlenderExePath", StringComparison.OrdinalIgnoreCase)) {
                    config["BlenderExePath"] = parts[1].Trim();
                    return config;
                }
            }
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: BlenderExePath not found in INI file: {path}");
        Console.ResetColor();
        return null;
    } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Failed to read or parse INI file: {path}");
        Console.WriteLine(ex.Message);
        Console.ResetColor();
        return null;
    }
}

// Check if the INI file exists
if (!File.Exists(configFile)) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {configFile} not found. Please ensure the file exists in the current directory.");
    Console.ResetColor();
    Environment.Exit(1);
} else {
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"{configFile} found.");
    Console.ResetColor();
}

// Read Blender path from config.ini
var config = ReadIniFile(configFile);
if (config != null && config.ContainsKey("BlenderExePath")) {
    blenderExePath = config["BlenderExePath"];
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Blender executable path from {configFile}: {blenderExePath}");
    Console.ResetColor();
} else {
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Error: BlenderExePath not found in config.ini or config.ini not found. Using default 'blender4.3'.");
    Console.ResetColor();
}

// Blender Processing function
void BlenderProcessing() {
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("Starting Blender processing using asset mapping file...");
    Console.ResetColor();

    try {
        string jsonString = File.ReadAllText(assetMappingFile);
        using JsonDocument document = JsonDocument.Parse(jsonString);
        if (document.RootElement.ValueKind == JsonValueKind.Object) {
            foreach (var property in document.RootElement.EnumerateObject()) {
                loopCount++;
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Loop Count: {loopCount}");
                Console.ResetColor();
                Console.WriteLine();

                var assetInfo = property.Value;
                if (assetInfo.ValueKind == JsonValueKind.Object) {
                    if (assetInfo.TryGetProperty("preinstanced_symlink", out var preinstancedSymlinkElement) &&
                        assetInfo.TryGetProperty("blend_symlink", out var blendSymlinkElement) &&
                        assetInfo.TryGetProperty("predicted_glb_symlink", out var predictedGlbSymlinkElement)) {

                        string preinstancedSymlink = preinstancedSymlinkElement.GetString();
                        string blendSymlink = blendSymlinkElement.GetString();
                        string predictedGlbSymlink = predictedGlbSymlinkElement.GetString();

                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine($"Preinstanced Symlink: {preinstancedSymlink}");
                        Console.WriteLine($"Blend Symlink: {blendSymlink}");
                        Console.WriteLine($"Predicted GLB Symlink: {predictedGlbSymlink}");
                        Console.ResetColor();
                        Console.WriteLine();

                        // Check if the corresponding .blend symlink exists
                        if (File.Exists(blendSymlink)) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Processing: {preinstancedSymlink}");
                            Console.WriteLine($"Blend File (Symlink): {blendSymlink}");
                            Console.ResetColor();
                            Console.WriteLine();

                            // Construct the output .glb file path (using the predicted symlink)
                            string glbFilePath = predictedGlbSymlink;
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"Output GLB File (Symlink): {glbFilePath}");
                            Console.ResetColor();
                            Console.WriteLine();

                            try {
                                // Construct the paths for Blender arguments (using symlinks)
                                blendFilePath = blendSymlink;
                                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                Console.WriteLine($"Blend File Path (Symlink): {blendFilePath}");
                                Console.WriteLine($"Preinstanced File Path (Symlink): {preinstancedSymlink}");
                                Console.ResetColor();
                                Console.WriteLine();

                                // Extract the directory path from preinstancedSymlink (using symlinks)
                                Console.ForegroundColor = ConsoleColor.DarkBlue;
                                Console.WriteLine($"Preinstanced Directory Path (Full): {preinstancedSymlink}");
                                Console.ResetColor();
                                Console.WriteLine();

                                // Extract the directory path from glb file path (using symlinks)
                                string glbDirectoryPath = Path.GetDirectoryName(glbFilePath);
                                Console.ForegroundColor = ConsoleColor.DarkBlue;
                                Console.WriteLine($"GLB Directory Path: {glbDirectoryPath}");
                                Console.ResetColor();
                                Console.WriteLine();

                                // Check if the .glb symlink (target) does not exist
                                if (!File.Exists(glbFilePath)) {
                                    // Check if the corresponding .preinstanced symlink exists
                                    if (File.Exists(preinstancedSymlink)) {
                                        // Construct the command to run Blender in background mode using symlinks
                                        string verboseStr = verbose ? "true" : "false";
                                        string debugSleepStr = debugSleep ? "true" : "false";
                                        string blenderCommand = $"\"{blenderExePath}\" -b \"{blendFilePath}\" --python \"{pythonScriptPath}\" -- \"{blendFilePath}\" \"{preinstancedSymlink}\" \"{glbFilePath}\" \"{pythonExtensionFile}\" \"{verboseStr}\" \"{debugSleepStr}\"";

                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Console.WriteLine($"Blender command --> {blenderCommand}");
                                        Console.ResetColor();

                                        // Execute the command
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        Console.WriteLine("# Start Blender Output");
                                        Console.ResetColor();
                                        ProcessStartInfo psi = new ProcessStartInfo {
                                            FileName = blenderExePath,
                                            Arguments = $"-b \"{blendFilePath}\" --python \"{pythonScriptPath}\" -- \"{blendFilePath}\" \"{preinstancedSymlink}\" \"{glbFilePath}\" \"{pythonExtensionFile}\" \"{verboseStr}\" \"{debugSleepStr}\"",
                                            RedirectStandardOutput = true,
                                            RedirectStandardError = true,
                                            UseShellExecute = false,
                                            CreateNoWindow = true
                                        };

                                        using (Process process = new Process()) {
                                            process.StartInfo = psi;
                                            process.Start();

                                            string output = process.StandardOutput.ReadToEnd();
                                            string error = process.StandardError.ReadToEnd();

                                            process.WaitForExit();

                                            Console.WriteLine(output);
                                            Console.WriteLine(error);
                                        }
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        Console.WriteLine("# End Blender Output");
                                        Console.ResetColor();

                                        // Check if an error occurred
                                        if (File.Exists(glbFilePath) && (File.ReadAllText(glbFilePath).Contains("Error:", StringComparison.OrdinalIgnoreCase) || File.ReadAllText(glbFilePath).Contains("Exception:", StringComparison.OrdinalIgnoreCase))) {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("Blender encountered an error:");
                                            foreach (var line in File.ReadAllLines(glbFilePath)) {
                                                if (line.Contains("Error:", StringComparison.OrdinalIgnoreCase) || line.Contains("Exception:", StringComparison.OrdinalIgnoreCase)) {
                                                    Console.WriteLine($"  {line}");
                                                }
                                            }
                                            string blankBlenderFile = "blank.blend"; // Assuming this is a relevant path
                                            if (File.Exists(blankBlenderFile)) {
                                                Console.WriteLine($"Blank Blender file exists: {blankBlenderFile}");
                                            } else {
                                                Console.WriteLine($"Blank Blender file does not exist: {blankBlenderFile}");
                                            }
                                            Console.ResetColor();
                                        } else {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine("Blender executed successfully.");
                                            Console.ResetColor();
                                        }

                                        // Check if the output file was created successfully (target of the symlink)
                                        if (File.Exists(glbFilePath)) {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"Output file created successfully: {glbFilePath}");
                                            Console.ResetColor();
                                        } else {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"Failed to create output file: {glbFilePath}");
                                            Console.ResetColor();
                                        }
                                    } else {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"Error: No corresponding .preinstanced symlink for: {blendFilePath}");
                                        Console.ResetColor();
                                        Environment.Exit(1); // break on missing input
                                    }
                                } else {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Skipping: .glb exists for: {blendFilePath}");
                                    Console.ResetColor();
                                }
                            } catch (Exception ex) {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error processing file: {blendFilePath}");
                                Console.WriteLine($"Error message: {ex.Message}");
                                Console.ResetColor();
                                Environment.Exit(1); // Exit script on error
                            }
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Finished processing for: {blendFilePath}");
                            Console.ResetColor();
                        } else {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error: 404 Blend symlink not found: {blendSymlink}");
                            Console.ResetColor();
                            Environment.Exit(1); // break on missing input
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Missing required properties in asset mapping entry for key: {property.Name}");
                        Console.ResetColor();
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Asset info for key: {property.Name} is not a JSON object.");
                    Console.ResetColor();
                }
            }
        } else {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Asset mapping file does not contain a JSON object at the root.");
            Console.ResetColor();
        }
    } catch (FileNotFoundException) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Asset mapping file not found: {assetMappingFile}");
        Console.ResetColor();
        Environment.Exit(1);
    } catch (JsonException ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Failed to read or parse the asset mapping JSON file: {assetMappingFile}");
        Console.WriteLine(ex.Message);
        Console.ResetColor();
        Environment.Exit(1);
    }
}

Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("Initializing...");
Console.ResetColor();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Blender Processing using asset_mapping.json...");
Console.ResetColor();
BlenderProcessing();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Processing complete.");
Console.ResetColor();