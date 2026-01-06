using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Text.Json;

namespace VariableRenamer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return;
            }

            try
            {
                var config = ParseArguments(args);
                
                if (config.FilePath != null)
                {
                    await ProcessFileAsync(config);
                }
                else if (config.DirectoryPath != null)
                {
                    await ProcessDirectoryAsync(config);
                }
                else
                {
                    Console.WriteLine("Error: Specify --file or --dir option");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("C# Variable Renamer - Rename variables to specific format");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rename-vars --file <path> [options]");
            Console.WriteLine("  rename-vars --dir <path> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --file <path>        Process single file");
            Console.WriteLine("  --dir <path>         Process directory (default: current)");
            Console.WriteLine("  --pattern <pattern>  File pattern (default: *.cs)");
            Console.WriteLine("  --format <format>    Naming format: camelCase, PascalCase,");
            Console.WriteLine("                       snake_case, SCREAMING_SNAKE, kebab-case");
            Console.WriteLine("                       (default: camelCase)");
            Console.WriteLine("  --targets <list>     Comma-separated list: fields,properties,");
            Console.WriteLine("                       locals,parameters,all (default: all)");
            Console.WriteLine("  --prefix <prefix>    Prefix for private fields (default: _)");
            Console.WriteLine("  --dry-run           Show changes without applying");
            Console.WriteLine("  --config <path>     JSON config file path");
            Console.WriteLine("  --help, -h          Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  rename-vars --file MyClass.cs --format camelCase");
            Console.WriteLine("  rename-vars --dir ./Scripts --format PascalCase --targets fields,properties");
            Console.WriteLine("  rename-vars --file Test.cs --dry-run");
        }

        static RenamingConfig ParseArguments(string[] args)
        {
            var config = new RenamingConfig();
            string configPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--file":
                        if (i + 1 < args.Length) config.FilePath = args[++i];
                        break;
                    case "--dir":
                        if (i + 1 < args.Length) config.DirectoryPath = args[++i];
                        break;
                    case "--pattern":
                        if (i + 1 < args.Length) config.Pattern = args[++i];
                        break;
                    case "--format":
                        if (i + 1 < args.Length) config.Format = args[++i];
                        break;
                    case "--targets":
                        if (i + 1 < args.Length)
                        {
                            var targets = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries);
                            config.Targets = targets.ToList();
                        }
                        break;
                    case "--prefix":
                        if (i + 1 < args.Length) config.Prefix = args[++i];
                        break;
                    case "--dry-run":
                        config.IsDryRun = true;
                        break;
                    case "--config":
                        if (i + 1 < args.Length) configPath = args[++i];
                        break;
                }
            }

            // Загружаем конфиг из файла если указан
            if (!string.IsNullOrEmpty(configPath))
            {
                var fileConfig = LoadConfig(configPath).Result;
                // Объединяем с аргументами командной строки
                if (string.IsNullOrEmpty(config.Format)) config.Format = fileConfig.Format;
                if (string.IsNullOrEmpty(config.Prefix)) config.Prefix = fileConfig.Prefix;
                if (config.Targets.Count == 0 || (config.Targets.Count == 1 && config.Targets[0] == "all"))
                    config.Targets = fileConfig.Targets;
                
                config.Exceptions = fileConfig.Exceptions;
                config.CustomReplacements = fileConfig.CustomReplacements;
            }

            return config;
        }

        static async Task<RenamingConfig> LoadConfig(string path)
        {
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<RenamingConfig>(json) ?? new RenamingConfig();
            }
            return new RenamingConfig();
        }

        static async Task ProcessFileAsync(RenamingConfig config)
        {
            Console.WriteLine($"Processing: {config.FilePath}");
            
            var content = await File.ReadAllTextAsync(config.FilePath!);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();
            
            var renamer = new RenamingRewriter(config);
            var newRoot = renamer.Visit(root);
            
            if (!newRoot.IsEquivalentTo(root))
            {
                var changes = renamer.Changes;
                
                Console.WriteLine($"Found {changes.Count} changes:");
                foreach (var change in changes)
                {
                    Console.WriteLine($"  {change.OldName} → {change.NewName} ({change.Type}, line {change.Line})");
                }
                
                if (!config.IsDryRun)
                {
                    var workspace = new AdhocWorkspace();
                    newRoot = Formatter.Format(newRoot, workspace);
                    
                    await File.WriteAllTextAsync(config.FilePath!, newRoot.ToFullString());
                    Console.WriteLine("✅ File updated");
                }
                else
                {
                    Console.WriteLine("🔍 Dry run - no changes applied");
                }
            }
            else
            {
                Console.WriteLine("✅ No changes needed");
            }
        }

        static async Task ProcessDirectoryAsync(RenamingConfig config)
        {
            var dir = config.DirectoryPath ?? Directory.GetCurrentDirectory();
            var pattern = config.Pattern ?? "*.cs";
            
            var files = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            Console.WriteLine($"Found {files.Count} C# files in {dir}");
            
            int totalChanged = 0;
            int totalRenamed = 0;
            var allChanges = new List<VariableChange>();
            
            foreach (var file in files)
            {
                try
                {
                    Console.WriteLine($"\n--- {Path.GetRelativePath(dir, file)} ---");
                    
                    var content = await File.ReadAllTextAsync(file);
                    var tree = CSharpSyntaxTree.ParseText(content);
                    var root = await tree.GetRootAsync();
                    
                    var renamer = new RenamingRewriter(config);
                    var newRoot = renamer.Visit(root);
                    
                    if (!newRoot.IsEquivalentTo(root))
                    {
                        totalChanged++;
                        totalRenamed += renamer.Changes.Count;
                        allChanges.AddRange(renamer.Changes);
                        
                        if (renamer.Changes.Count > 0)
                        {
                            Console.WriteLine($"Renamed {renamer.Changes.Count} variables:");
                            foreach (var change in renamer.Changes.Take(3))
                            {
                                Console.WriteLine($"  {change.OldName} → {change.NewName}");
                            }
                            if (renamer.Changes.Count > 3)
                                Console.WriteLine($"  ... and {renamer.Changes.Count - 3} more");
                        }
                        
                        if (!config.IsDryRun)
                        {
                            var workspace = new AdhocWorkspace();
                            newRoot = Formatter.Format(newRoot, workspace);
                            await File.WriteAllTextAsync(file, newRoot.ToFullString());
                            Console.WriteLine("✅ Updated");
                        }
                        else
                        {
                            Console.WriteLine("🔍 Would update (dry run)");
                        }
                    }
                    else
                    {
                        Console.WriteLine("✅ No changes");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }
            
            // Выводим сводку
            Console.WriteLine($"\n{new string('=', 50)}");
            Console.WriteLine("📊 SUMMARY");
            Console.WriteLine($"{new string('=', 50)}");
            Console.WriteLine($"Files processed: {files.Count}");
            Console.WriteLine($"Files changed: {totalChanged}");
            Console.WriteLine($"Total variables renamed: {totalRenamed}");
            
            if (allChanges.Count > 0)
            {
                Console.WriteLine($"\nMost common renames:");
                var topRenames = allChanges
                    .GroupBy(c => $"{c.OldName} → {c.NewName}")
                    .OrderByDescending(g => g.Count())
                    .Take(10);
                
                foreach (var group in topRenames)
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()} times");
                }
            }
        }
    }
}