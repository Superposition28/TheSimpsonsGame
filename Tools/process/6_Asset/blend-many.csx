// Requires:
// #r "System.IO"
// #r "System.Text.Json"
// #r "System.Diagnostics.Process"
// #r "System.Threading.Tasks" // Add this for parallel processing

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks; // Add this for parallel processing

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

string pythonScriptPath = "Tools\\blender\\main.py";
string pythonExtensionFile = "Tools\\blender\\io_import_simpson_game_ScriptMode.py";
string assetMappingFile = "Tools\\process\\6_Asset\\asset_mapping.json";
string configFile = "config.ini";

int loopCount = 0;



// # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

// Path to the C# script
string csxScriptPath = "Tools\\Global\\ConfigReader.csx";

// Define the parameters to pass to the C# script
string section = "ToolPaths";
string key = "BlenderExePath";
string defaultValue = "err";

// Execute the C# script using dotnet-script, passing the parameters
string responsePathValue = RunDotnetScript(csxScriptPath, section, key, defaultValue);
    
// Output the result from the script
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("Path from config: " + responsePathValue);

// Ensure file exists
if (!File.Exists(responsePathValue)) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Executable not found at: " + responsePathValue);
    Environment.Exit(1);
}

// Update the path to the Blender executable
string blenderExePath = responsePathValue;

// Helper function to run the dotnet-script and get the output
static string RunDotnetScript(string scriptPath, string section, string key, string defaultValue) {
    var startInfo = new ProcessStartInfo {
        FileName = "dotnet-script",
        Arguments = $"{scriptPath} {section} {key} {defaultValue}",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using (var process = Process.Start(startInfo)) {
        using (var reader = process.StandardOutput) {
            string result = reader.ReadToEnd();
            process.WaitForExit();
            return result.Trim(); // Return the output from the script
        }
    }
}
// # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #


// Blender Processing function
void BlenderProcessingParallel() {
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("Starting Blender processing using asset mapping file (Parallel)...");
    Console.ResetColor();

    try {
        string jsonString = File.ReadAllText(assetMappingFile);
        using JsonDocument document = JsonDocument.Parse(jsonString);
        if (document.RootElement.ValueKind == JsonValueKind.Object) {
            var assetEntries = new List<JsonProperty>();
            foreach (var property in document.RootElement.EnumerateObject()) {
                assetEntries.Add(property);
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Found {assetEntries.Count} files in the asset mapping.");
            Console.ResetColor();

            // Configure parallel options (optional, but can be useful)
            var parallelOptions = new ParallelOptions {
                // You can set the maximum degree of parallelism here.
                // For example, to use all available cores:
                // MaxDegreeOfParallelism = Environment.ProcessorCount
                // Or limit it to a specific number, e.g., 4:
                MaxDegreeOfParallelism = 4
            };

            Parallel.ForEach(assetEntries, parallelOptions, (property) => {
                int currentLoopCount = System.Threading.Interlocked.Increment(ref loopCount);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Loop Count (Parallel): {currentLoopCount}");
                Console.ResetColor();
                Console.WriteLine();

                var assetInfo = property.Value;
                if (assetInfo.ValueKind == JsonValueKind.Object) {
                    if (assetInfo.TryGetProperty("preinstanced_symlink", out var preinstancedSymlinkElement) &&
                        assetInfo.TryGetProperty("blend_symlink", out var blendSymlinkElement) &&
                        assetInfo.TryGetProperty("glb_symlink", out var predictedGlbSymlinkElement)) {

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
                            Console.WriteLine($"Processing (Parallel): {preinstancedSymlink}");
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
                                string currentBlendFilePath = blendSymlink;
                                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                Console.WriteLine($"Blend File Path (Symlink): {currentBlendFilePath}");
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
                                        string blenderCommand = $"\"{blenderExePath}\" -b \"{currentBlendFilePath}\" --python \"{pythonScriptPath}\" -- \"{currentBlendFilePath}\" \"{preinstancedSymlink}\" \"{glbFilePath}\" \"{pythonExtensionFile}\" \"{verboseStr}\" \"{debugSleepStr}\"";

                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Console.WriteLine($"Blender command (Parallel) --> {blenderCommand}");
                                        Console.ResetColor();

                                        // Execute the command
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        Console.WriteLine("# Start Blender Output (Parallel)");
                                        Console.ResetColor();
                                        ProcessStartInfo psi = new ProcessStartInfo {
                                            FileName = blenderExePath,
                                            Arguments = $"-b \"{currentBlendFilePath}\" --python \"{pythonScriptPath}\" -- \"{currentBlendFilePath}\" \"{preinstancedSymlink}\" \"{glbFilePath}\" \"{pythonExtensionFile}\" \"{verboseStr}\" \"{debugSleepStr}\"",
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
                                        Console.WriteLine("# End Blender Output (Parallel)");
                                        Console.ResetColor();

                                        // Check if an error occurred
                                        if (File.Exists(glbFilePath) && (File.ReadAllText(glbFilePath).Contains("Error:", StringComparison.OrdinalIgnoreCase) || File.ReadAllText(glbFilePath).Contains("Exception:", StringComparison.OrdinalIgnoreCase))) {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("Blender encountered an error (Parallel):");
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
                                            Console.WriteLine("Blender executed successfully (Parallel).");
                                            Console.ResetColor();
                                        }

                                        // Check if the output file was created successfully (target of the symlink)
                                        if (File.Exists(glbFilePath)) {
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"Output file created successfully (Parallel): {glbFilePath}");
                                            Console.ResetColor();
                                        } else {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine($"Failed to create output file (Parallel): {glbFilePath}");
                                            Console.ResetColor();
                                            
                                            Environment.Exit(1); // Exit the script if the file creation fails
                                        }
                                    } else {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"Error (Parallel): No corresponding .preinstanced symlink for: {currentBlendFilePath}");
                                        Console.ResetColor();
                                        // Optionally, you might want to log this instead of exiting in a parallel context
                                        // Environment.Exit(1);
                                    }
                                } else {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Skipping (Parallel): .glb exists for: {currentBlendFilePath}");
                                    Console.ResetColor();
                                }
                            } catch (Exception ex) {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error processing file (Parallel)");
                                Console.WriteLine($"Error message: {ex.Message}");
                                Console.ResetColor();
                                // Optionally, you might want to log this instead of exiting in a parallel context
                                // Environment.Exit(1);
                            }
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Finished processing for (Parallel)");
                            Console.ResetColor();
                        } else {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error (Parallel): 404 Blend symlink not found: {blendSymlink}");
                            Console.ResetColor();
                            // Optionally, you might want to log this instead of exiting in a parallel context
                            // Environment.Exit(1);
                        }
                    } else {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning (Parallel): Missing required properties in asset mapping entry for key: {property.Name}");
                        Console.ResetColor();
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning (Parallel): Asset info for key: {property.Name} is not a JSON object.");
                    Console.ResetColor();
                }
            });
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
Console.WriteLine("Blender Processing using asset_mapping.json (Parallel)..."); // Updated output message
Console.ResetColor();
BlenderProcessingParallel(); // Call the parallel processing function

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Processing complete (Parallel)."); // Updated output message
Console.ResetColor();


Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("deleting temp files...");
Console.ResetColor();

// Delete TMP_TSG_LNKS
// reading the asset_mapping.json file to get the TMP_TSG_LNKS path



