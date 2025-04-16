// Requires:
// #r "System.IO"
// #r "System.Text.Json"
// #r "System.Diagnostics.Process"

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;


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
string pythonExtensionFile = "Tools\\blender\\io_import_simpson_game_fork.py";
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
Print("Path from config: " + responsePathValue, ConsoleColor.Cyan);

// Ensure file exists
if (!File.Exists(responsePathValue)) {
    Print("Executable not found at: " + responsePathValue, ConsoleColor.Red);
    Environment.Exit(1);
}

// Update the path to the Blender executable
string blenderExePath = responsePathValue;

public static void Print(string message, ConsoleColor color) {
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ResetColor();
}

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
void BlenderProcessing() {
    Print("Starting Blender processing using asset mapping file...", ConsoleColor.DarkGray);

    try {
        string jsonString = File.ReadAllText(assetMappingFile);
        using JsonDocument document = JsonDocument.Parse(jsonString);
        if (document.RootElement.ValueKind == JsonValueKind.Object) {
            foreach (var property in document.RootElement.EnumerateObject()) {
                loopCount++;
                Console.WriteLine();
                Print($"Loop Count: {loopCount}", ConsoleColor.Yellow);
                Console.WriteLine();

                var assetInfo = property.Value;

				Print($"assetInfo: {assetInfo}", ConsoleColor.Cyan);

				if (assetInfo.ValueKind == JsonValueKind.Object) {
                    if (assetInfo.TryGetProperty("preinstanced_symlink", out var preinstancedSymlinkElement) &&
                        assetInfo.TryGetProperty("blend_symlink", out var blendSymlinkElement) &&
						assetInfo.TryGetProperty("filename", out var filenameElement) &&
                        assetInfo.TryGetProperty("glb_symlink", out var glbSymlinkElement)) {

						string filename = filenameElement.GetString();

						string blendSymlinkPath = blendSymlinkElement.GetString();
						string blendSymlinkFile = Path.Combine(blendSymlinkPath, filename + ".blend");
						string glbSymlinkPath = glbSymlinkElement.GetString();
                        string glbSymlinkFile = Path.Combine(glbSymlinkPath, filename + ".glb");
                        string preinstancedSymlinkPath = preinstancedSymlinkElement.GetString();
                        string preinstancedSymlinkFile = Path.Combine(preinstancedSymlinkPath, filename + ".preinstanced");

                        // Check if the corresponding .blend symlink exists
                        if (File.Exists(blendSymlinkFile)) {
                            try {
                                // Check if the .glb symlink (target) does not exist
                                if (!File.Exists(glbSymlinkFile)) {
                                    // Check if the corresponding .preinstanced symlink exists
                                    if (File.Exists(preinstancedSymlinkFile)) {
                                        // Construct the command to run Blender in background mode using symlinks
                                        string verboseStr = verbose ? "true" : "false";
                                        string debugSleepStr = debugSleep ? "true" : "false";

                                        // Execute the command
                                        Print("# Start Blender Output", ConsoleColor.Gray);
                                        ProcessStartInfo psi = new ProcessStartInfo {
                                            FileName = blenderExePath,
                                            Arguments = $"-b \"{blendSymlinkFile}\" --python \"{pythonScriptPath}\" -- \"{blendSymlinkFile}\" \"{preinstancedSymlinkFile}\" \"{glbSymlinkFile}\" \"{pythonExtensionFile}\" \"{verboseStr}\" \"{debugSleepStr}\"",
                                            RedirectStandardOutput = true,
                                            RedirectStandardError = true,
                                            UseShellExecute = false,
                                            CreateNoWindow = true
                                        };

                                        string blenderCommand = $"\"{blenderExePath}\" {psi.Arguments}";

                                        Print($"Blender command --> {blenderCommand}", ConsoleColor.Magenta);
										//Thread.Sleep(10000); // Sleep for 10 second


                                        using (Process process = new Process()) {
                                            process.StartInfo = psi;
                                            process.Start();

                                            string output = process.StandardOutput.ReadToEnd();
                                            string error = process.StandardError.ReadToEnd();

                                            process.WaitForExit();

                                            Console.WriteLine(output);
                                            Console.WriteLine(error);
                                        }
                                        Print("# End Blender Output", ConsoleColor.Gray);

                                        // Check if an error occurred
                                        if (File.Exists(glbSymlinkFile) && (File.ReadAllText(glbSymlinkFile).Contains("Error:", StringComparison.OrdinalIgnoreCase) || File.ReadAllText(glbSymlinkFile).Contains("Exception:", StringComparison.OrdinalIgnoreCase))) {
                                            Print("Blender encountered an error:", ConsoleColor.Red);
                                            foreach (var line in File.ReadAllLines(glbSymlinkFile)) {
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
                                        } else {
                                            Print("Blender executed successfully.", ConsoleColor.Green);
                                        }

                                        // Check if the output file was created successfully (target of the symlink)
                                        if (File.Exists(glbSymlinkFile)) {
                                            Print($"Output file created successfully: {glbSymlinkFile}", ConsoleColor.Green);
                                        } else {
                                            Print($"Failed to create output file: {glbSymlinkFile}", ConsoleColor.Red);
                                        }
                                    } else {
                                        Print($"Error: No corresponding .preinstanced symlink for: {blendSymlinkPath}", ConsoleColor.Red);
                                        Environment.Exit(1); // break on missing input
                                    }
                                } else {
                                    Print($"Skipping: .glb exists for: {blendSymlinkPath}", ConsoleColor.Yellow);
                                }
                            } catch (Exception ex) {
                                Print($"Error message: {ex.Message}", ConsoleColor.Red);
                                Environment.Exit(1); // Exit script on error
                            }
                        } else {
                            Print($"Error: 404 Blend symlink not found: {blendSymlinkFile}", ConsoleColor.Red);
                            Environment.Exit(1); // break on missing input
                        }
                    } else {
                        Print($"Warning: Missing required properties in asset mapping entry for key: {property.Name}", ConsoleColor.Yellow);
                    }
                } else {
                    Print($"Warning: Asset info for key: {property.Name} is not a JSON object.", ConsoleColor.Yellow);
                }
            }
        } else {
            Print("Error: Asset mapping file does not contain a JSON object at the root.", ConsoleColor.Red);
        }
    } catch (FileNotFoundException) {
        Print($"Error: Asset mapping file not found: {assetMappingFile}", ConsoleColor.Red);
        Environment.Exit(1);
    } catch (JsonException ex) {
        Print($"Error: Failed to read or parse the asset mapping JSON file: {assetMappingFile}", ConsoleColor.Red);
        Console.WriteLine(ex.Message);
        Environment.Exit(1);
    }
}

Print("Initializing...", ConsoleColor.Blue);
Print("Blender Processing using asset_mapping.json...", ConsoleColor.DarkGray);
BlenderProcessing();

Print("Processing complete.", ConsoleColor.Green);