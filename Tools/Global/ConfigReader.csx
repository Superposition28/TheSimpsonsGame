#r "System.IO"
#r "System.Collections"

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class ConfigReader {
    public string IniPath { get; set; }
    public Dictionary<string, Dictionary<string, string>> Config { get; set; }

    public ConfigReader(string iniPath) {
        IniPath = iniPath;
        Config = new Dictionary<string, Dictionary<string, string>>();
        ParseIni(IniPath);
    }

    private void ParseIni(string iniPath) {
        // Read the content of the INI file
        string iniContent = File.ReadAllText(iniPath);

        // Split the content into lines (by newline characters)
        var lines = iniContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        string currentSection = null;
        var sectionDict = new Dictionary<string, string>();

        foreach (var line in lines) {
            // Ignore empty lines
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check if the line is a section header (starts with '[' and ends with ']')
            if (line.StartsWith("[") && line.EndsWith("]")) {
                // If we were processing a previous section, store it
                if (currentSection != null) {
                    Config[currentSection] = sectionDict;
                }

                // Update the current section name (trim the '[' and ']')
                currentSection = line.Trim('[', ']');
                sectionDict = new Dictionary<string, string>(); // Initialize a new section dictionary
            } else {
                // Parse the key-value pairs within the current section
                var match = Regex.Match(line, @"^([^=]+)=(.+)$");
                if (match.Success && currentSection != null) {
                    string key = match.Groups[1].Value.Trim();
                    string value = match.Groups[2].Value.Trim();

                    // Add the key-value pair to the section dictionary
                    sectionDict[key] = value;
                }
            }
        }

        // Ensure the last section is added to the Config dictionary
        if (currentSection != null) {
            Config[currentSection] = sectionDict;
        }
    }

    public string GetConfigValue(string section, string key, string defaultValue = "") {
        if (Config.ContainsKey(section)) {
            if (Config[section].ContainsKey(key)) {
                return Config[section][key];
            } else {
                //Console.WriteLine($"[ConfigReader] Key not found. Section: {section}, Key: {key}, Returning DefaultValue: {defaultValue}");
                return defaultValue;
            }
        } else {
            // Only log the error if section/key is not found, but still return the default value
            //Console.WriteLine($"[ConfigReader] Section not found. Section: {section}, Key: {key}, Returning DefaultValue: {defaultValue}");
            return defaultValue;
        }
    }
}

// Expecting 3 arguments: Section, Key, DefaultValue
string[] args = Environment.GetCommandLineArgs();
if (args.Length < 4) {
    //Console.WriteLine("Usage: dotnet-script Tools\\Global\\ConfigReader.csx <Section> <Key> <DefaultValue>");
    return;
}

// Get the section, key, and default value from the arguments
string section = args[2];  // Fixed index for section
//Console.WriteLine($"Section: {section}");
string key = args[3];      // Fixed index for key
//Console.WriteLine($"Key: {key}");
string defaultValue = args[4]; // Fixed index for default value
//Console.WriteLine($"DefaultValue: {defaultValue}");

// Define the path to the config file
string iniFilePath = "./config.ini";

// Create an instance of the ConfigReader class
var configReader = new ConfigReader(iniFilePath);

// Get the value from the config
string value = configReader.GetConfigValue(section, key, defaultValue);

// return the value
Console.WriteLine(value);
