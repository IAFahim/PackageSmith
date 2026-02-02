using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace PackageSmith.Core.Logic;

public static class TemplateHarvesterLogic
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", ".vscode", "Library", "Temp", "Logs", "obj", "bin", "Build", "Builds", "UserSettings",
        "MemoryCaptures"
    };

    private static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csproj", ".sln", ".suo", ".user", ".userprefs", ".pidb", ".booproj", ".unityproj", ".pdb", ".db"
    };

    public static bool TryHarvest(string sourcePath, string outputTemplatePath, string sourcePackageName,
        out int processedFiles, bool keepMeta = false)
    {
        processedFiles = 0;
        if (!Directory.Exists(sourcePath)) return false;

        var rootPath = LocateEffectiveRoot(sourcePath);

        if (Directory.Exists(outputTemplatePath)) Directory.Delete(outputTemplatePath, true);
        Directory.CreateDirectory(outputTemplatePath);

        var metadata = ExtractMetadata(rootPath);

        if (metadata.PackageName == "{{PACKAGE_NAME}}")
        {
            var dirName = new DirectoryInfo(rootPath).Name;
            metadata.PackageName = ToPackageName(dirName);
            metadata.DisplayName = ToDisplayName(dirName);
            metadata.CompanyName = DeriveCompanyName(metadata.PackageName);
        }

        var rootAsmName = ExtractRootAssemblyName(rootPath);

        var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (ShouldSkip(file, rootPath)) continue;

            var action = AnalyzeFile(file, keepMeta);

            if (action == FileAction.Keep || action == FileAction.Tokenize)
            {
                var relativePath = Path.GetRelativePath(rootPath, file);

                var normalizedPath = NormalizeStructure(relativePath);

                var tokenizedPath = TokenizePath(normalizedPath, in metadata, rootAsmName);
                var destFile = Path.Combine(outputTemplatePath, tokenizedPath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                if (action == FileAction.Tokenize)
                {
                    var content = File.ReadAllText(file);
                    var tokenizedContent = TokenizeContent(content, in metadata, rootAsmName, file);
                    File.WriteAllText(destFile, tokenizedContent);
                }
                else
                {
                    File.Copy(file, destFile);
                }

                processedFiles++;
            }
        }

        PostProcessPackageJson(outputTemplatePath, metadata);

        return true;
    }

    private static string LocateEffectiveRoot(string startPath)
    {
        if (File.Exists(Path.Combine(startPath, "package.json"))) return startPath;

        var pkgFiles = Directory.GetFiles(startPath, "package.json", SearchOption.AllDirectories);
        if (pkgFiles.Length > 0) return Path.GetDirectoryName(pkgFiles.OrderBy(f => f.Length).First())!;

        var asmFiles = Directory.GetFiles(startPath, "*.asmdef", SearchOption.AllDirectories);
        if (asmFiles.Length > 0) return startPath;

        return startPath;
    }

    private static string NormalizeStructure(string relativePath)
    {
        var parts = relativePath.Replace('\\', '/').Split('/');
        if (parts.Length == 0) return relativePath;

        if (parts[0].Equals("Assets", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length == 1) return "";
            parts = parts.Skip(1).ToArray();
        }

        if (parts.Length > 0 && parts[0].Equals("Scripts", StringComparison.OrdinalIgnoreCase)) parts[0] = "Runtime";

        if (parts.Length == 1)
        {
            var ext = Path.GetExtension(parts[0]).ToLowerInvariant();
            if (ext == ".cs" || ext == ".asmdef")
            {
                if (parts[0].Contains("Editor", StringComparison.OrdinalIgnoreCase)) return "Editor/" + parts[0];

                return "Runtime/" + parts[0];
            }
        }

        return string.Join('/', parts);
    }

    private static void PostProcessPackageJson(string outputRoot, PackageMetadata metadata)
    {
        var path = Path.Combine(outputRoot, "package.json");
        string jsonContent;

        if (!File.Exists(path))
            jsonContent = """
                          {
                            "name": "{{PACKAGE_NAME}}",
                            "version": "{{VERSION}}",
                            "displayName": "{{DISPLAY_NAME}}",
                            "description": "{{DESCRIPTION}}",
                            "unity": "{{UNITY_VERSION}}",
                            "author": {
                              "name": "{{AUTHOR_NAME}}",
                              "email": "{{AUTHOR_EMAIL}}"
                            }
                          }
                          """;
        else
            jsonContent = File.ReadAllText(path);

        var samplesDir = Path.Combine(outputRoot, "Samples~");
        if (Directory.Exists(samplesDir))
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                if (!doc.RootElement.TryGetProperty("samples", out _))
                {
                    var subDirs = Directory.GetDirectories(samplesDir);
                    if (subDirs.Length > 0)
                    {
                        var samplesList = new List<object>();
                        foreach (var dir in subDirs)
                        {
                            var dirName = Path.GetFileName(dir);
                            samplesList.Add(new
                            {
                                displayName = ToDisplayName(dirName),
                                description = $"Samples for {dirName}",
                                path = $"Samples~/{dirName}"
                            });
                        }

                        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                        if (dict != null)
                        {
                            dict["samples"] = samplesList;
                            jsonContent = JsonSerializer.Serialize(dict,
                                new JsonSerializerOptions { WriteIndented = true });
                        }
                    }
                }
            }
            catch
            {
            }

        File.WriteAllText(path, jsonContent);
    }

    private static bool ShouldSkip(string filePath, string rootPath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.StartsWith(".")) return true;

        if (IgnoredExtensions.Contains(Path.GetExtension(filePath))) return true;

        var relative = Path.GetRelativePath(rootPath, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var part in parts)
            if (IgnoredDirectories.Contains(part))
                return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FileAction AnalyzeFile(string filePath, bool keepMeta)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".cs" => FileAction.Tokenize,
            ".asmdef" => FileAction.Tokenize,
            ".asmref" => FileAction.Keep,
            ".json" => FileAction.Tokenize,
            ".md" => FileAction.Tokenize,
            ".meta" => keepMeta ? FileAction.Keep : FileAction.Drop,
            ".yml" => FileAction.Tokenize,
            ".yaml" => FileAction.Tokenize,
            ".txt" => FileAction.Tokenize,
            ".xml" => FileAction.Tokenize,
            ".uss" => FileAction.Tokenize,
            ".uxml" => FileAction.Tokenize,
            ".shader" => FileAction.Tokenize,
            ".hlsl" => FileAction.Tokenize,
            ".cginc" => FileAction.Tokenize,
            _ => FileAction.Keep
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PackageMetadata ExtractMetadata(string sourcePath)
    {
        var metadata = new PackageMetadata
        {
            PackageName = "{{PACKAGE_NAME}}",
            DisplayName = "{{DISPLAY_NAME}}",
            Description = "{{DESCRIPTION}}",
            AuthorName = "{{AUTHOR_NAME}}",
            AuthorEmail = "{{AUTHOR_EMAIL}}",
            Version = "{{VERSION}}",
            UnityVersion = "{{UNITY_VERSION}}",
            CompanyName = "{{COMPANY_NAME}}"
        };

        try
        {
            var packageJsonPath = Path.Combine(sourcePath, "package.json");
            if (!File.Exists(packageJsonPath)) return metadata;

            var content = File.ReadAllText(packageJsonPath);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("name", out var nameProp))
                metadata.PackageName = nameProp.GetString() ?? metadata.PackageName;

            if (root.TryGetProperty("displayName", out var displayProp))
                metadata.DisplayName = displayProp.GetString() ?? metadata.DisplayName;

            if (root.TryGetProperty("description", out var descProp))
                metadata.Description = descProp.GetString() ?? metadata.Description;

            if (root.TryGetProperty("version", out var versionProp))
                metadata.Version = versionProp.GetString() ?? metadata.Version;

            if (root.TryGetProperty("unity", out var unityProp))
                metadata.UnityVersion = unityProp.GetString() ?? metadata.UnityVersion;

            if (root.TryGetProperty("author", out var authorProp))
            {
                if (authorProp.ValueKind == JsonValueKind.Object)
                {
                    if (authorProp.TryGetProperty("name", out var authorNameProp))
                        metadata.AuthorName = authorNameProp.GetString() ?? metadata.AuthorName;

                    if (authorProp.TryGetProperty("email", out var authorEmailProp))
                        metadata.AuthorEmail = authorEmailProp.GetString() ?? metadata.AuthorEmail;
                }
                else if (authorProp.ValueKind == JsonValueKind.String)
                {
                    metadata.AuthorName = authorProp.GetString() ?? metadata.AuthorName;
                }
            }

            if (metadata.AuthorName == "{{AUTHOR_NAME}}")
                metadata.CompanyName = DeriveCompanyName(metadata.PackageName);
            else
                metadata.CompanyName = metadata.AuthorName;
        }
        catch
        {
        }

        return metadata;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string DeriveCompanyName(string packageName)
    {
        var parts = packageName.Split('.');
        if (parts.Length >= 2 && (parts[0] == "com" || parts[0] == "org" || parts[0] == "io" || parts[0] == "net"))
            return ToPascalCase(parts[1]);
        return "{{COMPANY_NAME}}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
                result.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                result.Append(part.Substring(1).ToLowerInvariant());
        }

        return result.Length > 0 ? result.ToString() : input;
    }

    private static string ToPackageName(string dirName)
    {
        return dirName;
    }

    private static string ToDisplayName(string dirName)
    {
        return dirName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ExtractRootAssemblyName(string sourcePath)
    {
        try
        {
            var asmdefs = Directory.GetFiles(sourcePath, "*.asmdef", SearchOption.AllDirectories);
            if (asmdefs.Length == 0) return string.Empty;

            var candidates = new List<string>();

            foreach (var asm in asmdefs)
                try
                {
                    var content = File.ReadAllText(asm);
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                    {
                        var name = nameProp.GetString();
                        if (!string.IsNullOrEmpty(name)) candidates.Add(name);
                    }
                }
                catch
                {
                }

            if (candidates.Count > 0) return candidates.OrderBy(x => x.Length).ThenBy(x => x).First();
        }
        catch
        {
        }

        return string.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TokenizeContent(string input, in PackageMetadata metadata, string rootAsmName,
        string filePath)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input;

        if (filePath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
        {
            result = TokenizePackageJson(result, in metadata);
            return result;
        }

        if (!string.IsNullOrEmpty(metadata.PackageName) && metadata.PackageName != "{{PACKAGE_NAME}}")
            result = result.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");
        if (!string.IsNullOrEmpty(metadata.DisplayName) && metadata.DisplayName != "{{DISPLAY_NAME}}")
            result = result.Replace(metadata.DisplayName, "{{DISPLAY_NAME}}");
        if (!string.IsNullOrEmpty(metadata.Description) && metadata.Description != "{{DESCRIPTION}}" &&
            metadata.Description.Length > 5) result = result.Replace(metadata.Description, "{{DESCRIPTION}}");

        if (!string.IsNullOrEmpty(metadata.AuthorName) && metadata.AuthorName != "{{AUTHOR_NAME}}")
            result = result.Replace(metadata.AuthorName, "{{AUTHOR_NAME}}");
        if (!string.IsNullOrEmpty(metadata.AuthorEmail) && metadata.AuthorEmail != "{{AUTHOR_EMAIL}}")
            result = result.Replace(metadata.AuthorEmail, "{{AUTHOR_EMAIL}}");
        if (!string.IsNullOrEmpty(metadata.CompanyName) && metadata.CompanyName != "{{COMPANY_NAME}}")
            result = result.Replace(metadata.CompanyName, "{{COMPANY_NAME}}");

        if (!string.IsNullOrEmpty(rootAsmName))
        {
            result = result.Replace(rootAsmName, "{{ASM_NAME}}");

            var parts = rootAsmName.Split('.');
            if (parts.Length > 0)
            {
                var shortName = parts[^1];
                result = result.Replace(shortName, "{{ASM_SHORT_NAME}}");
            }
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TokenizePackageJson(string json, in PackageMetadata metadata)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name;

                switch (name)
                {
                    case "name":
                        writer.WriteString(name, "{{PACKAGE_NAME}}");
                        break;

                    case "displayName":
                        writer.WriteString(name, "{{DISPLAY_NAME}}");
                        break;

                    case "description":
                        writer.WriteString(name, "{{DESCRIPTION}}");
                        break;

                    case "version":
                        writer.WriteString(name, "{{VERSION}}");
                        break;

                    case "unity":
                        writer.WriteString(name, "{{UNITY_VERSION}}");
                        break;

                    case "author":
                        writer.WritePropertyName(name);
                        writer.WriteStartObject();
                        writer.WriteString("name", "{{AUTHOR_NAME}}");
                        writer.WriteString("email", "{{AUTHOR_EMAIL}}");
                        writer.WriteEndObject();
                        break;

                    case "keywords":
                        writer.WritePropertyName(name);
                        writer.WriteStartArray();
                        writer.WriteEndArray();
                        break;

                    case "samples":
                        writer.WritePropertyName(name);
                        writer.WriteStartArray();
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                            foreach (var sample in prop.Value.EnumerateArray())
                                if (sample.ValueKind == JsonValueKind.Object)
                                {
                                    writer.WriteStartObject();
                                    foreach (var sProp in sample.EnumerateObject())
                                    {
                                        var sVal = sProp.Value.ToString();
                                        if (!string.IsNullOrEmpty(metadata.PackageName) &&
                                            metadata.PackageName != "{{PACKAGE_NAME}}")
                                            sVal = sVal.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");
                                        writer.WriteString(sProp.Name, sVal);
                                    }

                                    writer.WriteEndObject();
                                }

                        writer.WriteEndArray();
                        break;

                    case "dependencies":
                        prop.WriteTo(writer);
                        break;

                    default:
                        prop.WriteTo(writer);
                        break;
                }
            }

            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return json;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TokenizePath(string path, in PackageMetadata metadata, string rootAsmName)
    {
        var result = path;
        if (!string.IsNullOrEmpty(metadata.PackageName) && metadata.PackageName != "{{PACKAGE_NAME}}")
            result = result.Replace(metadata.PackageName, "{{PACKAGE_NAME}}");

        if (!string.IsNullOrEmpty(rootAsmName))
        {
            result = result.Replace(rootAsmName, "{{ASM_NAME}}");

            var parts = rootAsmName.Split('.');
            if (parts.Length > 0)
            {
                var shortName = parts[^1];
                if (!string.IsNullOrEmpty(shortName)) result = result.Replace(shortName, "{{ASM_SHORT_NAME}}");
            }
        }

        return result;
    }

    private struct PackageMetadata
    {
        public string PackageName;
        public string DisplayName;
        public string Description;
        public string AuthorName;
        public string AuthorEmail;
        public string Version;
        public string UnityVersion;
        public string CompanyName;
    }

    private enum FileAction
    {
        Drop,
        Keep,
        Tokenize
    }
}