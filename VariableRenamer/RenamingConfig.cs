using System.Text.Json.Serialization;

namespace VariableRenamer
{
    public class TypeSpecificRule
    {
        public string Format { get; set; } = "camelCase";
        public string Prefix { get; set; } = "";
    }

    public class TypeSpecificRules
    {
        public Dictionary<string, TypeSpecificRule>? Fields { get; set; }
        public Dictionary<string, TypeSpecificRule>? Properties { get; set; }
        public Dictionary<string, TypeSpecificRule>? Methods { get; set; }
        public TypeSpecificRule? Parameters { get; set; }
        public TypeSpecificRule? Locals { get; set; }
    }

    public class RenamingConfig
    {
        public string? FilePath { get; set; }
        public string? DirectoryPath { get; set; }
        public string? Pattern { get; set; }
        public string? Format { get; set; }
        public string? Prefix { get; set; }
        public bool IsDryRun { get; set; }
        
        [JsonInclude]
        public List<string> Targets { get; set; } = new() { "all" };
        
        [JsonInclude]
        public TypeSpecificRules? TypeSpecificRules { get; set; }
        
        [JsonInclude]
        public List<string> Exceptions { get; set; } = new();
        
        [JsonInclude]
        public Dictionary<string, string> CustomReplacements { get; set; } = new();

        public RenamingConfig()
        {
            Format = "camelCase";
            Prefix = "_";
            Pattern = "*.cs";
        }
    }
}