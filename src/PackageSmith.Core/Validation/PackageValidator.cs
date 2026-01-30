using System.Text.Json;
using PackageSmith.Core.Configuration;

namespace PackageSmith.Core.Validation;

public readonly struct ValidationCheck
{
    public readonly string Name;
    public readonly bool Passed;
    public readonly string Message;
    public readonly ValidationSeverity Severity;

    public ValidationCheck(string name, bool passed, string message, ValidationSeverity severity = ValidationSeverity.Info)
    {
        Name = name;
        Passed = passed;
        Message = message;
        Severity = severity;
    }
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public readonly struct ValidationResult
{
    public readonly ValidationCheck[] Checks;
    public readonly bool IsValid;

    public ValidationResult(ValidationCheck[] checks)
    {
        Checks = checks;
        IsValid = checks.All(c => c.Passed || c.Severity != ValidationSeverity.Error && c.Severity != ValidationSeverity.Critical);
    }
}

public sealed class PackageValidator
{
    public ValidationResult Validate(string packagePath)
    {
        var checks = new List<ValidationCheck>();

        // Check 1: package.json exists
        var packageJsonPath = Path.Combine(packagePath, "package.json");
        checks.Add(new ValidationCheck(
            "package.json exists",
            File.Exists(packageJsonPath),
            File.Exists(packageJsonPath) ? "Found" : "Missing package.json",
            ValidationSeverity.Critical
        ));

        // Check 2: package.json is valid JSON
        if (File.Exists(packageJsonPath))
        {
            try
            {
                var json = File.ReadAllText(packageJsonPath);
                JsonDocument.Parse(json);
                checks.Add(new ValidationCheck(
                    "package.json is valid JSON",
                    true,
                    "Valid JSON structure"
                ));

                // Check 3: Required fields
                var root = JsonDocument.Parse(json);
                var hasName = root.RootElement.TryGetProperty("name", out var name) && name.ValueKind != JsonValueKind.Undefined;
                var hasVersion = root.RootElement.TryGetProperty("version", out var version) && version.ValueKind != JsonValueKind.Undefined;
                var hasDisplayName = root.RootElement.TryGetProperty("displayName", out var displayName) && displayName.ValueKind != JsonValueKind.Undefined;

                checks.Add(new ValidationCheck(
                    "package.json has required fields",
                    hasName && hasVersion && hasDisplayName,
                    $"Name: {(hasName ? name.GetString() : "missing")}, Version: {(hasVersion ? version.GetString() : "missing")}, DisplayName: {(hasDisplayName ? displayName.GetString() : "missing")}",
                    ValidationSeverity.Error
                ));
            }
            catch (JsonException ex)
            {
                checks.Add(new ValidationCheck(
                    "package.json is valid JSON",
                    false,
                    ex.Message,
                    ValidationSeverity.Critical
                ));
            }
        }

        // Check 4: README.md exists
        var readmePath = Path.Combine(packagePath, "README.md");
        checks.Add(new ValidationCheck(
            "README.md exists",
            File.Exists(readmePath),
            File.Exists(readmePath) ? "Documentation found" : "Consider adding README.md",
            ValidationSeverity.Warning
        ));

        // Check 5: Assembly definitions
        var asmdefFiles = Directory.GetFiles(packagePath, "*.asmdef", SearchOption.AllDirectories);
        checks.Add(new ValidationCheck(
            "Assembly definitions found",
            asmdefFiles.Length > 0,
            $"{asmdefFiles.Length} .asmdef file(s) found",
            ValidationSeverity.Info
        ));

        // Check 6: .asmdef reference validation
        foreach (var asmdefFile in asmdefFiles)
        {
            checks.Add(new ValidationCheck(
                $"Validate {Path.GetFileName(asmdefFile)}",
                ValidateAsmDef(asmdefFile, packagePath),
                Path.GetFileName(asmdefFile)
            ));
        }

        // Check 7: Runtime folder exists
        var runtimePath = Path.Combine(packagePath, "Runtime");
        checks.Add(new ValidationCheck(
            "Runtime folder exists",
            Directory.Exists(runtimePath),
            Directory.Exists(runtimePath) ? "Standard structure" : "Missing Runtime folder",
            ValidationSeverity.Warning
        ));

        // Check 8: .meta files (for Unity packages)
        var metaFiles = Directory.GetFiles(packagePath, "*.meta", SearchOption.AllDirectories);
        checks.Add(new ValidationCheck(
            ".meta files present",
            metaFiles.Length > 0,
            $"{metaFiles.Length} .meta file(s) found (Unity packages should include .meta files)",
            ValidationSeverity.Info
        ));

        return new ValidationResult(checks.ToArray());
    }

    private static bool ValidateAsmDef(string asmdefPath, string packagePath)
    {
        try
        {
            var json = File.ReadAllText(asmdefPath);
            var doc = JsonDocument.Parse(json);

            // Check if references are valid
            if (doc.RootElement.TryGetProperty("references", out var references))
            {
                foreach (var reference in references.EnumerateArray())
                {
                    var refName = reference.GetString();
                    if (refName.StartsWith("Unity.") || refName.StartsWith("UnityEngine.") || refName.StartsWith("UnityEditor."))
                    {
                        continue; // Unity references are always valid
                    }

                    // Check if local reference exists
                    var referencedAsmdef = Path.Combine(packagePath, $"{refName}.asmdef");
                    if (!File.Exists(referencedAsmdef))
                    {
                        var inSubfolder = Path.GetFileName(asmdefPath).Replace(".asmdef", "");
                        var possiblePath = Path.Combine(packagePath, "Runtime", inSubfolder, $"{refName}.asmdef");
                        if (!File.Exists(possiblePath))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
