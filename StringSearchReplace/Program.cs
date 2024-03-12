using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

public class Program
{
    public class Configuration
    {
        public Replacement[] Replacements { get; set; }
    }

    public class Replacement
    {
        public string Key { get; set; }
        public string SearchValue { get; set; }
        public string ReplacementValue { get; set; }
    }

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a directory path.");
            return;
        }

        string path = args[0];
        if (!Directory.Exists(path))
        {
            Console.WriteLine("The provided path does not exist.");
            return;
        }

        string configFilePath = "config.json";
        if (!File.Exists(configFilePath))
        {
            Console.WriteLine("Configuration file not found.");
            return;
        }

        string jsonString = File.ReadAllText(configFilePath);
        Configuration config = JsonSerializer.Deserialize<Configuration>(jsonString);

        string backupFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempBackUp");
        Directory.CreateDirectory(backupFolderPath);
        

        string backupFileName = $"{Path.GetFileName(path)}_Backup_{DateTime.Now:yyyyMMddHHmmss}.zip";
        string backupFilePath = Path.Combine(Path.GetDirectoryName(path), backupFileName);
        ZipFile.CreateFromDirectory(path, backupFilePath);
        Console.WriteLine($"Backup created: {backupFilePath}");

        try
        {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".cs") || s.EndsWith(".csv"));

            foreach (var file in files)
            {
                string content = File.ReadAllText(file);
                string modifiedContent = content;

                foreach (var replacement in config.Replacements)
                {
                    if (file.EndsWith(".cs") && replacement.Key != "*")
                    {
                        string searchPattern = $"{Regex.Escape(replacement.Key)}\\s*=\\s*\"{Regex.Escape(replacement.SearchValue)}\"";
                        string replacementPattern = $"{replacement.Key} = \"{replacement.ReplacementValue}\"";
                        modifiedContent = Regex.Replace(modifiedContent, searchPattern, replacementPattern);
                    }
                    else if (file.EndsWith(".csv") || replacement.Key == "*")
                    {
                        modifiedContent = modifiedContent.Replace(replacement.SearchValue, replacement.ReplacementValue);
                    }
                }

                if (content != modifiedContent)
                {
                    File.WriteAllText(file, modifiedContent);
                    Console.WriteLine($"Modified: {file}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}