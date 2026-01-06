using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace VariableRenamer
{
    public class RenamingRewriter : CSharpSyntaxRewriter
    {
        private readonly RenamingConfig _config;
        public List<VariableChange> Changes { get; } = new();
        
        public class VariableInfo
        {
            public string OriginalName { get; set; } = "";
            public SyntaxToken Token { get; set; }
            public VariableType Type { get; set; }
            public Accessibility Accessibility { get; set; }
        }
        
        private readonly List<VariableInfo> _variables = new();
        private readonly Dictionary<string, string> _renameMap = new();

        public RenamingRewriter(RenamingConfig config)
        {
            _config = config;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _variables.Clear();
            _renameMap.Clear();
            
            var collector = new VariableCollector();
            collector.Visit(node);
            _variables.AddRange(collector.Variables);
            
            CreateRenameMap();
            
            return base.VisitClassDeclaration(node);
        }

        private void CreateRenameMap()
        {
            foreach (var variable in _variables)
            {
                if (ShouldRename(variable))
                {
                    var newName = GenerateNewName(variable);
                    if (newName != variable.OriginalName)
                    {
                        // ? ПРОВЕРКА ИСКЛЮЧЕНИЙ: если старое или новое имя в исключениях
                        bool isException = _config.Exceptions.Contains(variable.OriginalName.ToUpper()) ||
                                          _config.Exceptions.Contains(newName.ToUpper()) ||
                                          _config.Exceptions.Any(e => 
                                              variable.OriginalName.Equals(e, StringComparison.OrdinalIgnoreCase));
                        
                        if (!isException)
                        {
                            _renameMap[variable.OriginalName] = newName;
                            Changes.Add(new VariableChange
                            {
                                OldName = variable.OriginalName,
                                NewName = newName,
                                Type = variable.Type.ToString(),
                                Line = variable.Token.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                            });
                        }
                        else
                        {
                            Console.WriteLine($"SKIP: '{variable.OriginalName}' is in exceptions list");
                        }
                    }
                }
            }
        }

        private bool ShouldRename(VariableInfo variable)
        {
            if (_config.Targets.Contains("all")) return true;
            
            return variable.Type switch
            {
                VariableType.Field => _config.Targets.Contains("fields"),
                VariableType.Property => _config.Targets.Contains("properties"),
                VariableType.Local => _config.Targets.Contains("locals"),
                VariableType.Parameter => _config.Targets.Contains("parameters"),
                _ => false
            };
        }

        private string GenerateNewName(VariableInfo variable)
        {
            string name = variable.OriginalName;
            
            Console.WriteLine($"DEBUG: Processing '{name}' (Type: {variable.Type})");
            
            // ? ШАГ 1: Проверяем полные совпадения в кастомных заменах
            if (_config.CustomReplacements.ContainsKey(name.ToLower()))
            {
                string replacement = _config.CustomReplacements[name.ToLower()];
                Console.WriteLine($"DEBUG: Full replacement '{name}' ? '{replacement}'");
                name = replacement;
            }
            
            // ? ШАГ 2: Нормализация БЕЗ удаления "m"!
            name = SafeNormalizeName(name);
            
            // ? ШАГ 3: Получаем формат и префикс
            string format = GetFormatForVariable(variable);
            string prefix = GetPrefixForVariable(variable);
            
            // ? ШАГ 4: Применяем форматирование
            name = ApplyFormat(name, variable, format);
            
            // ? ШАГ 5: Добавляем префикс если нужно
            if (ShouldAddPrefix(variable, prefix) && !string.IsNullOrEmpty(prefix))
            {
                if (!name.StartsWith(prefix))
                {
                    name = prefix + name;
                }
            }
            
            Console.WriteLine($"DEBUG: Final '{variable.OriginalName}' ? '{name}'");
            return name;
        }

        private string SafeNormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            string original = name;
            
            // ? КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Удаляем ТОЛЬКО подчеркивания в начале
            name = name.TrimStart('_');
            
            // ? Убираем только явные венгерские префиксы с подчеркиванием
            // НИКОГДА не удаляем одиночные буквы!
            string[] safePrefixes = { "m_", "s_", "g_", "this_" };
            
            foreach (var prefix in safePrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length);
                    Console.WriteLine($"DEBUG: Removed prefix '{prefix}' from '{original}' ? '{name}'");
                    break;
                }
            }
            
            // ? ОСТОРОЖНО с "m" - это может быть часть слова!
            if (name.StartsWith("m", StringComparison.OrdinalIgnoreCase) && 
                name.Length > 1 && 
                char.IsLower(name[1]))
            {
                // Это НЕ префиксы типа "m_" - это часть слова (max, move, etc.)
                // НЕ УДАЛЯЕМ!
            }
            
            return name;
        }

        private string GetFormatForVariable(VariableInfo variable)
        {
            if (_config.TypeSpecificRules == null)
                return _config.Format ?? "camelCase";
            
            string accessibility = variable.Accessibility.ToString().ToLower();
            
            return variable.Type switch
            {
                VariableType.Field when _config.TypeSpecificRules.Fields != null &&
                                       _config.TypeSpecificRules.Fields.ContainsKey(accessibility) =>
                    _config.TypeSpecificRules.Fields[accessibility].Format,
                
                VariableType.Property when _config.TypeSpecificRules.Properties != null &&
                                          _config.TypeSpecificRules.Properties.ContainsKey(accessibility) =>
                    _config.TypeSpecificRules.Properties[accessibility].Format,
                
                VariableType.Parameter when _config.TypeSpecificRules.Parameters != null =>
                    _config.TypeSpecificRules.Parameters.Format,
                
                VariableType.Local when _config.TypeSpecificRules.Locals != null =>
                    _config.TypeSpecificRules.Locals.Format,
                
                _ => _config.Format ?? "camelCase"
            };
        }

        private string GetPrefixForVariable(VariableInfo variable)
        {
            if (_config.TypeSpecificRules == null)
                return _config.Prefix ?? "_";
            
            string accessibility = variable.Accessibility.ToString().ToLower();
            
            return variable.Type switch
            {
                VariableType.Field when _config.TypeSpecificRules.Fields != null &&
                                       _config.TypeSpecificRules.Fields.ContainsKey(accessibility) =>
                    _config.TypeSpecificRules.Fields[accessibility].Prefix,
                
                VariableType.Property when _config.TypeSpecificRules.Properties != null &&
                                          _config.TypeSpecificRules.Properties.ContainsKey(accessibility) =>
                    _config.TypeSpecificRules.Properties[accessibility].Prefix,
                
                VariableType.Parameter when _config.TypeSpecificRules.Parameters != null =>
                    _config.TypeSpecificRules.Parameters.Prefix,
                
                VariableType.Local when _config.TypeSpecificRules.Locals != null =>
                    _config.TypeSpecificRules.Locals.Prefix,
                
                _ => _config.Prefix ?? "_"
            };
        }

        private bool ShouldAddPrefix(VariableInfo variable, string prefix)
        {
            if (variable.Type == VariableType.Field)
            {
                return variable.Accessibility == Accessibility.Private && 
                       !string.IsNullOrEmpty(prefix);
            }
            return false;
        }

        private string ApplyFormat(string name, VariableInfo variable, string format)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Сохраняем цифровые суффиксы
            var numberMatch = Regex.Match(name, @"(\d+)$");
            string numberSuffix = numberMatch.Success ? numberMatch.Value : "";
            name = Regex.Replace(name, @"\d+$", "");

            // Разделяем на слова
            var words = SplitIntoWords(name);
            
            if (words.Count == 0) return name + numberSuffix;
            
            string result = format.ToLower() switch
            {
                "pascalcase" => ToPascalCase(words),
                "snake_case" => ToSnakeCase(words),
                "screaming_snake" => ToScreamingSnakeCase(words),
                "kebab-case" => ToKebabCase(words),
                _ => ToCamelCase(words, variable, format)
            };

            return result + numberSuffix;
        }

        private List<string> SplitIntoWords(string input)
        {
            var words = new List<string>();
            var currentWord = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                if (i > 0 && char.IsUpper(c))
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    currentWord.Append(c);
                }
                else if (c == '_' || c == '-')
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                }
                else
                {
                    currentWord.Append(c);
                }
            }
            
            if (currentWord.Length > 0)
                words.Add(currentWord.ToString());
                
            return words;
        }

        private string ToCamelCase(List<string> words, VariableInfo variable, string format)
        {
            if (words.Count == 0) return "";
            
            var result = words[0].ToLower();
            for (int i = 1; i < words.Count; i++)
            {
                result += Capitalize(words[i]);
            }
            
            if (format.Equals("camelCase", StringComparison.OrdinalIgnoreCase) &&
                (variable.Type == VariableType.Property || 
                 (variable.Type == VariableType.Field && 
                  variable.Accessibility == Accessibility.Public)))
            {
                result = Capitalize(result);
            }
            
            return result;
        }

        private string ToPascalCase(List<string> words)
        {
            return string.Join("", words.Select(Capitalize));
        }

        private string ToSnakeCase(List<string> words)
        {
            return string.Join("_", words.Select(w => w.ToLower()));
        }

        private string ToScreamingSnakeCase(List<string> words)
        {
            return string.Join("_", words.Select(w => w.ToUpper()));
        }

        private string ToKebabCase(List<string> words)
        {
            return string.Join("-", words.Select(w => w.ToLower()));
        }

        private string Capitalize(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken) && 
                _renameMap.ContainsKey(token.ValueText))
            {
                return SyntaxFactory.Identifier(
                    token.LeadingTrivia,
                    _renameMap[token.ValueText],
                    token.TrailingTrivia);
            }
            
            return base.VisitToken(token);
        }
    }
}