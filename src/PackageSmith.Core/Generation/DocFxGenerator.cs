namespace PackageSmith.Core.Generation;

public static class DocFxGenerator
{
    public static VirtualDirectory[] GetDirectories()
    {
        return new[]
        {
            new VirtualDirectory("Documentation~")
        };
    }

    public static VirtualFile[] GenerateFiles(string packageName, string displayName)
    {
        var files = new List<VirtualFile>();

        // docfx.json
        files.Add(new VirtualFile(
            "Documentation~/docfx.json",
            GetDocFxJson()
        ));

        // index.md
        files.Add(new VirtualFile(
            "Documentation~/index.md",
            GetIndexMd(packageName, displayName)
        ));

        // api/.gitkeep
        files.Add(new VirtualFile(
            "Documentation~/api/.gitkeep",
            ""
        ));

        return files.ToArray();
    }

    private static string GetDocFxJson()
    {
        return """
        {
          "metadata": [
            {
              "src": [],
              "dest": "api",
              "includePrivateMembers": false,
              "disableGitFeatures": false,
              "disableDefaultFilter": false,
              "noRestore": false,
              "msbuildGenerator": {
                "name": "msbuild",
                "parameters": {
                  "customArguments": "",
                  "ignoreProject": [
                    "**/Tests/**"
                  ]
                }
              }
            }
          ],
          "build": {
            "content": [
              {
                "files": ["**/*.md"],
                "exclude": ["_site/**"]
              }
            ],
            "dest": "_site",
            "globalMetadataFiles": [],
            "fileMetadataFiles": [],
            "template": [
              "default",
              "modern"
            ],
            "postProcessors": [],
            "markdownEngineName": "markdig",
            "noLangKeyword": false,
            "keepFileLink": false,
            "cleanupCacheHistory": false,
            "disableGitFeatures": false
          }
        }
        """;
    }

    private static string GetIndexMd(string packageName, string displayName)
    {
        return $$"""
        # {{displayName}} Documentation

        Welcome to the {{displayName}} package documentation.

        ## Overview

        {{displayName}} ({{packageName}}) is a Unity package for...

        ## Installation

        Add this package to your Unity project via:

        1. Unity Package Manager
        2. Add package from git URL
        3. Enter the repository URL

        ## API Reference

        See [API Documentation](api/index.md) for detailed API documentation.

        ## Examples

        ```csharp
        using {{packageName}};

        // Your code here
        ```

        ## License

        See LICENSE.md in the package root.
        """;
    }
}
