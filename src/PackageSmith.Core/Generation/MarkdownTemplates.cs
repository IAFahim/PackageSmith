using PackageSmith.Core.Configuration;

namespace PackageSmith.Core.Generation;

public static class MarkdownTemplates
{
    public static string Readme(in PackageTemplate template)
    {
        var moduleName = template.PackageName.Split('.').Last();
        var year = DateTime.UtcNow.Year;

        return $$"""
        # {{template.DisplayName}}

        {{template.Description}}

        ## Installation

        1. Open Unity Package Manager
        2. Click the "+" button
        3. Select "Add package from git URL"
        4. Enter the package URL

        ## Features

        - Feature 1
        - Feature 2
        - Feature 3

        ## Usage

        ```csharp
        using {{template.PackageName}};

        // Your code here
        ```

        ## Requirements

        - Unity {{template.UnityVersion ?? "2022.3"}}

        ## License

        Copyright (c) {{year}} {{template.CompanyName}}. All rights reserved.

        ## Changelog

        See [CHANGELOG.md](CHANGELOG.md) for version history.
        """;
    }

    public static string License(string companyName, int year)
    {
        return $$"""
        MIT License

        Copyright (c) {{year}} {{companyName}}

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        """;
    }

    public static string Changelog(string packageName)
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        return $$"""
        # Changelog

        All notable changes to this project will be documented in this file.

        The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
        and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

        ## [Unreleased]

        ## [1.0.0] - {{date}}

        ### Added
        - Initial release of {{packageName}}
        - Core functionality implemented
        """;
    }

    public static string RuntimeAsmDef(string packageName)
    {
        var asmdefName = packageName;
        return $$"""
        {
            "name": "{{asmdefName}}",
            "references": [],
            "includePlatforms": [],
            "excludePlatforms": [],
            "allowUnsafeCode": false,
            "overrideReferences": false,
            "precompiledReferences": [],
            "autoReferenced": true,
            "defineConstraints": [],
            "versionDefines": [],
            "noEngineReferences": false
        }
        """;
    }

    public static string EditorAsmDef(string packageName)
    {
        var asmdefName = $"{packageName}.Editor";
        var runtimeAsmdef = packageName;
        return $$"""
        {
            "name": "{{asmdefName}}",
            "references": [
                "{{runtimeAsmdef}}"
            ],
            "includePlatforms": [
                "Editor"
            ],
            "excludePlatforms": [],
            "allowUnsafeCode": false,
            "overrideReferences": false,
            "precompiledReferences": [],
            "autoReferenced": true,
            "defineConstraints": [
                "UNITY_EDITOR"
            ],
            "versionDefines": [],
            "noEngineReferences": false
        }
        """;
    }

    public static string TestsAsmDef(string packageName)
    {
        var asmdefName = $"{packageName}.Tests";
        var runtimeAsmdef = packageName;
        return $$"""
        {
            "name": "{{asmdefName}}",
            "references": [
                "UnityEngine.TestRunner",
                "UnityEditor.TestRunner",
                "{{runtimeAsmdef}}"
            ],
            "includePlatforms": [],
            "excludePlatforms": [],
            "allowUnsafeCode": false,
            "overrideReferences": true,
            "precompiledReferences": [
                "nunit.framework.dll"
            ],
            "autoReferenced": false,
            "defineConstraints": [
                "UNITY_INCLUDE_TESTS"
            ],
            "versionDefines": [],
            "noEngineReferences": false
        }
        """;
    }
}
