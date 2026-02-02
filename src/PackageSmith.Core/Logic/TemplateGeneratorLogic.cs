using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Logic;

public static class TemplateGeneratorLogic
{
    private static readonly Dictionary<string, string> AssemblyToPackageMap = new()
    {
        ["Unity.InputSystem"] = "com.unity.input.system",
        ["Unity.Physics"] = "com.unity.physics",
        ["Unity.Physics.Custom"] = "com.unity.physics",
        ["Unity.CharacterController"] = "com.unity.charactercontroller",
        ["Unity.Entities"] = "com.unity.entities",
        ["Unity.Entities.Graphics"] = "com.unity.entities.graphics",
        ["Unity.Entities.Hybrid"] = "com.unity.entities.hybrid",
        ["Unity.Scenes"] = "com.unity.entities",
        ["Unity.Collections"] = "com.unity.collections",
        ["Unity.Mathematics"] = "com.unity.mathematics",
        ["Unity.Burst"] = "com.unity.burst",
        ["Unity.Jobs"] = "com.unity.jobs",
        ["Unity.NetCode"] = "com.unity.netcode.gameobjects",
        ["Unity.NetCode.Physics"] = "com.unity.netcode.gameobjects",
        ["Unity.Networking.Transport"] = "com.unity.transport",
        ["Unity.RenderPipelines.Core"] = "com.unity.render-pipelines.core",
        ["Unity.RenderPipelines.Universal.Runtime"] = "com.unity.render-pipelines.universal",
        ["Unity.RenderPipelines.HighDefinition.Runtime"] = "com.unity.render-pipelines.high-definition",
        ["Unity.Addressables"] = "com.unity.addressables",
        ["Unity.ResourceManager"] = "com.unity.addressables",
        ["Unity.TextMeshPro"] = "com.unity.textmeshpro"
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGenerateFromTemplate(string templatePath, string outputPath, string packageName,
        out int processedFiles)
    {
        processedFiles = 0;
        if (!Directory.Exists(templatePath)) return false;

        if (Directory.Exists(outputPath))
            if (Directory.EnumerateFileSystemEntries(outputPath).Any())
                return false;
        Directory.CreateDirectory(outputPath);

        var files = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (Path.GetFileName(file).StartsWith(".")) continue;

            var relativePath = Path.GetRelativePath(templatePath, file);
            var destFile = Path.Combine(outputPath, DetokenizePath(relativePath, packageName));

            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
            {
                var content = ProcessAsmDefFile(file, packageName);
                File.WriteAllText(destFile, content);
            }
            else
            {
                var content = File.ReadAllText(file);
                var detokenizedContent = DetokenizeString(content, packageName);
                File.WriteAllText(destFile, detokenizedContent);
            }

            processedFiles++;
        }

        return true;
    }

    public static bool TryGenerateFromTemplate(
        string templatePath,
        string outputPath,
        string packageName,
        string displayName,
        string description,
        string authorName,
        string unityVersion,
        out int processedFiles)
    {
        processedFiles = 0;
        if (!Directory.Exists(templatePath)) return false;

        if (Directory.Exists(outputPath))
            if (Directory.EnumerateFileSystemEntries(outputPath).Any())
                return false;
        Directory.CreateDirectory(outputPath);

        var files = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (Path.GetFileName(file).StartsWith(".")) continue;

            var relativePath = Path.GetRelativePath(templatePath, file);
            var destFile = Path.Combine(outputPath, DetokenizePath(relativePath, packageName));

            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
            {
                var content = ProcessAsmDefFile(file, packageName, displayName, description, authorName, unityVersion);
                File.WriteAllText(destFile, content);
            }
            else
            {
                var content = File.ReadAllText(file);
                var detokenizedContent = DetokenizeString(content, packageName, displayName, description, authorName,
                    unityVersion);
                File.WriteAllText(destFile, detokenizedContent);
            }

            processedFiles++;
        }

        return true;
    }

    public static bool TryGenerateState(
        string templatePath,
        string packageName,
        string displayName,
        string description,
        string authorName,
        string unityVersion,
        out VirtualFileState[] virtualFiles)
    {
        virtualFiles = Array.Empty<VirtualFileState>();
        if (!Directory.Exists(templatePath)) return false;

        var filesList = new List<VirtualFileState>();
        var sourceFiles = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories);

        foreach (var file in sourceFiles)
        {
            if (Path.GetFileName(file).StartsWith(".")) continue;

            var relativePath = Path.GetRelativePath(templatePath, file);
            var destPath = DetokenizePath(relativePath, packageName);
            string content;

            if (Path.GetExtension(file).Equals(".asmdef", StringComparison.OrdinalIgnoreCase))
            {
                content = ProcessAsmDefFile(file, packageName, displayName, description, authorName, unityVersion);
            }
            else
            {
                var raw = File.ReadAllText(file);
                content = DetokenizeString(raw, packageName, displayName, description, authorName, unityVersion);
            }

            filesList.Add(new VirtualFileState
            {
                Path = destPath,
                Content = content,
                ContentLength = content.Length
            });
        }

        virtualFiles = filesList.ToArray();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ProcessAsmDefFile(string asmdefPath, string packageName)
    {
        var content = File.ReadAllText(asmdefPath);
        var detokenized = DetokenizeString(content, packageName);

        using var doc = JsonDocument.Parse(detokenized);
        var root = doc.RootElement;

        var references = new List<string>();
        if (root.TryGetProperty("references", out var refsProp) && refsProp.ValueKind == JsonValueKind.Array)
            foreach (var refElem in refsProp.EnumerateArray())
                if (refElem.ValueKind == JsonValueKind.String)
                    references.Add(refElem.GetString() ?? string.Empty);

        var versionDefines = new Dictionary<string, (string expression, string define)>();
        if (root.TryGetProperty("versionDefines", out var existingProp) &&
            existingProp.ValueKind == JsonValueKind.Array)
            foreach (var vd in existingProp.EnumerateArray())
                if (vd.TryGetProperty("name", out var nameProp))
                {
                    var name = nameProp.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        var exp = vd.TryGetProperty("expression", out var e) ? e.GetString() : "";
                        var def = vd.TryGetProperty("define", out var d) ? d.GetString() : "";
                        versionDefines[name] = (exp ?? "", def ?? "");
                    }
                }

        if (!versionDefines.ContainsKey(packageName))
        {
            var selfDefine =
                AsmDefGenerationLogic.DeriveDefineSymbol(packageName.Contains('.')
                    ? packageName.Split('.').Last()
                    : packageName);
            versionDefines[packageName] = ("0.0.1", selfDefine);
        }

        foreach (var refName in references)
        {
            var packageId = AsmDefGenerationLogic.DerivePackageId(refName);

            if (versionDefines.ContainsKey(packageId)) continue;

            var defineSymbol = AsmDefGenerationLogic.DeriveDefineSymbol(refName);

            versionDefines[packageId] = ("0.0.1", defineSymbol);
        }

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.NameEquals("versionDefines")) continue;
            if (prop.NameEquals("references"))
            {
                var asmName = GetAssemblyNameFromPackage(packageName);
                writer.WritePropertyName(prop.Name);
                writer.WriteStartArray();
                foreach (var refName in references)
                {
                    var finalRef = refName.Replace("{{ASM_NAME}}", asmName);
                    writer.WriteStringValue(finalRef);
                }

                writer.WriteEndArray();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }

        if (versionDefines.Count > 0)
        {
            writer.WritePropertyName("versionDefines");
            writer.WriteStartArray();
            foreach (var (name, (expression, define)) in versionDefines)
            {
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteString("expression", expression);
                writer.WriteString("define", define);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ProcessAsmDefFile(string asmdefPath, string packageName, string displayName,
        string description, string authorName, string unityVersion)
    {
        var content = File.ReadAllText(asmdefPath);
        var detokenized = DetokenizeString(content, packageName, displayName, description, authorName, unityVersion);

        using var doc = JsonDocument.Parse(detokenized);
        var root = doc.RootElement;

        var references = new List<string>();
        if (root.TryGetProperty("references", out var refsProp) && refsProp.ValueKind == JsonValueKind.Array)
            foreach (var refElem in refsProp.EnumerateArray())
                if (refElem.ValueKind == JsonValueKind.String)
                    references.Add(refElem.GetString() ?? string.Empty);

        var versionDefines = new Dictionary<string, (string expression, string define)>();
        if (root.TryGetProperty("versionDefines", out var existingProp) &&
            existingProp.ValueKind == JsonValueKind.Array)
            foreach (var vd in existingProp.EnumerateArray())
                if (vd.TryGetProperty("name", out var nameProp))
                {
                    var name = nameProp.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        var exp = vd.TryGetProperty("expression", out var e) ? e.GetString() : "";
                        var def = vd.TryGetProperty("define", out var d) ? d.GetString() : "";
                        versionDefines[name] = (exp ?? "", def ?? "");
                    }
                }

        if (!versionDefines.ContainsKey(packageName))
        {
            var selfDefine =
                AsmDefGenerationLogic.DeriveDefineSymbol(packageName.Contains('.')
                    ? packageName.Split('.').Last()
                    : packageName);
            versionDefines[packageName] = ("0.0.1", selfDefine);
        }

        foreach (var refName in references)
        {
            var packageId = AsmDefGenerationLogic.DerivePackageId(refName);

            if (versionDefines.ContainsKey(packageId)) continue;

            var defineSymbol = AsmDefGenerationLogic.DeriveDefineSymbol(refName);

            versionDefines[packageId] = ("0.0.1", defineSymbol);
        }

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.NameEquals("versionDefines")) continue;
            if (prop.NameEquals("references"))
            {
                var asmName = GetAssemblyNameFromPackage(packageName);
                writer.WritePropertyName(prop.Name);
                writer.WriteStartArray();
                foreach (var refName in references)
                {
                    var finalRef = refName.Replace("{{ASM_NAME}}", asmName);
                    writer.WriteStringValue(finalRef);
                }

                writer.WriteEndArray();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }

        if (versionDefines.Count > 0)
        {
            writer.WritePropertyName("versionDefines");
            writer.WriteStartArray();
            foreach (var (name, (expression, define)) in versionDefines)
            {
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteString("expression", expression);
                writer.WriteString("define", define);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string DetokenizeString(string input, string packageName, string displayName, string description,
        string authorName, string unityVersion)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var asmName = GetAssemblyNameFromPackage(packageName);
        var asmShortName = GetShortName(asmName);
        var companyName = GetCompanyNameFromPackage(packageName);

        displayName = string.IsNullOrEmpty(displayName) ? GetDisplayNameFromPackage(packageName) : displayName;
        description = string.IsNullOrEmpty(description) ? $"Generated by {displayName}" : description;
        authorName = string.IsNullOrEmpty(authorName) ? companyName : authorName;
        unityVersion = string.IsNullOrEmpty(unityVersion) ? "2022.3" : unityVersion;

        return input
            .Replace("{{PACKAGE_NAME}}", packageName)
            .Replace("{{DISPLAY_NAME}}", displayName)
            .Replace("{{DESCRIPTION}}", description)
            .Replace("{{AUTHOR_NAME}}", authorName)
            .Replace("{{AUTHOR_EMAIL}}", string.Empty)
            .Replace("{{COMPANY_NAME}}", companyName)
            .Replace("{{VERSION}}", "1.0.0")
            .Replace("{{UNITY_VERSION}}", unityVersion)
            .Replace("{{ASM_NAME}}", asmName)
            .Replace("{{ASM_SHORT_NAME}}", asmShortName)
            .Replace("{{PACKAGE_PASCAL_NAME}}", asmName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string DetokenizeString(string input, string packageName)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var asmName = GetAssemblyNameFromPackage(packageName);
        var asmShortName = GetShortName(asmName);
        var companyName = GetCompanyNameFromPackage(packageName);
        var displayName = GetDisplayNameFromPackage(packageName);

        return input
            .Replace("{{PACKAGE_NAME}}", packageName)
            .Replace("{{DISPLAY_NAME}}", displayName)
            .Replace("{{DESCRIPTION}}", $"Generated by {displayName}")
            .Replace("{{AUTHOR_NAME}}", companyName)
            .Replace("{{AUTHOR_EMAIL}}", string.Empty)
            .Replace("{{COMPANY_NAME}}", companyName)
            .Replace("{{VERSION}}", "1.0.0")
            .Replace("{{UNITY_VERSION}}", "2022.3")
            .Replace("{{ASM_NAME}}", asmName)
            .Replace("{{ASM_SHORT_NAME}}", asmShortName)
            .Replace("{{PACKAGE_PASCAL_NAME}}", asmName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string DetokenizePath(string path, string packageName)
    {
        var asmName = GetAssemblyNameFromPackage(packageName);
        return path
            .Replace("{{PACKAGE_NAME}}", asmName)
            .Replace("{{ASM_NAME}}", asmName)
            .Replace("{{ASM_SHORT_NAME}}", asmName)
            .Replace("{{PACKAGE_PASCAL_NAME}}", asmName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetAssemblyNameFromPackage(string packageName)
    {
        PackageLogic.GetAsmDefRoot(packageName, out var asmName);
        return asmName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetShortName(string asmName)
    {
        var parts = asmName.Split('.');
        return parts.Length > 0 ? parts[^1] : asmName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetCompanyNameFromPackage(string packageName)
    {
        var parts = packageName.Split('.');
        if (parts.Length >= 2 && parts[0] is "com" or "org" or "io" or "net") return ToPascalCase(parts[1]);
        return "YourCompany";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetDisplayNameFromPackage(string packageName)
    {
        var parts = packageName.Split('.');
        if (parts.Length >= 3)
        {
            var lastPart = ToPascalCase(parts[^1]);
            return $"{GetCompanyNameFromPackage(packageName)} {lastPart}";
        }

        return ToPascalCase(packageName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split('-', '_');
        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (part.Length > 0)
                result.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
                result.Append(part.Substring(1).ToLowerInvariant());
        }

        return result.Length > 0 ? result.ToString() : input;
    }
}