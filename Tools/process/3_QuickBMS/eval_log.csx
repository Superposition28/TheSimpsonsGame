#r "nuget: System.Linq, 4.3.0"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

var logPath = @"Tools\\process\\3_QuickBMS\\QBMS.log";

if (!File.Exists(logPath)) {
    Console.WriteLine($"Error: Log file not found at path '{logPath}'");
    return;
}

var input = File.ReadAllText(logPath);

var pattern = new Regex(@"File\s*=\s*""(-?\d+)""\s*,\s*Percentage\s*=\s*""(\d+)%""", RegexOptions.Compiled);
var filePercentages = new Dictionary<int, List<int>>();
var allValues = new List<int>();

foreach (Match match in pattern.Matches(input)) {
    int fileNum = int.Parse(match.Groups[1].Value);
    int percent = int.Parse(match.Groups[2].Value);

    allValues.Add(percent);

    if (!filePercentages.ContainsKey(fileNum))
        filePercentages[fileNum] = new List<int>();

    filePercentages[fileNum].Add(percent);
}

double CalcMedian(List<int> values) {
    var sorted = values.OrderBy(v => v).ToList();
    int count = sorted.Count;
    return count % 2 == 0
        ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0
        : sorted[count / 2];
}

double CalcStdDev(List<int> values) {
    double mean = values.Average();
    return Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
}

Console.WriteLine("📁 Statistics per File number:\n");

foreach (var kvp in filePercentages.OrderBy(k => k.Key)) {
    var values = kvp.Value;
    Console.WriteLine($"File {kvp.Key}");
    Console.WriteLine($"  Count:   {values.Count}");
    Console.WriteLine($"  Mean:    {values.Average():F2}%");
    Console.WriteLine($"  Median:  {CalcMedian(values):F2}%");
    Console.WriteLine($"  Min:     {values.Min()}%");
    Console.WriteLine($"  Max:     {values.Max()}%");
    Console.WriteLine($"  Std Dev: {CalcStdDev(values):F2}%\n");
}

Console.WriteLine("📊 Combined Stats (All Files):\n");
Console.WriteLine($"  Total Count: {allValues.Count}");
Console.WriteLine($"  Mean:        {allValues.Average():F2}%");
Console.WriteLine($"  Median:      {CalcMedian(allValues):F2}%");
Console.WriteLine($"  Min:         {allValues.Min()}%");
Console.WriteLine($"  Max:         {allValues.Max()}%");
Console.WriteLine($"  Std Dev:     {CalcStdDev(allValues):F2}%");

// Thresholds to evaluate
int[] thresholds = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 101 };

// Method to print breakdown for a given set
void PrintThresholdStats(List<int> values, string label) {
    Console.WriteLine($"\n📉 Values Below Thresholds for {label}:");

    foreach (var threshold in thresholds) {
        int count = values.Count(v => v < threshold);
        double percent = (double)count / values.Count * 100;
        Console.WriteLine($"  Below {threshold}%: {count} entries ({percent:F2}%)");
    }
}

// Per file threshold stats
foreach (var kvp in filePercentages.OrderBy(k => k.Key)) {
    PrintThresholdStats(kvp.Value, $"File {kvp.Key}");
}

// Combined threshold stats
PrintThresholdStats(allValues, "All Files");
