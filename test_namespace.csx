#!/usr/bin/env dotnet-script
#load "src/PackageSmith.Core/Logic/PackageLogic.cs"
#load "src/PackageSmith.Data/State/PackageState.cs"
#load "src/PackageSmith.Data/Types/SubAssemblyType.cs"
#load "src/PackageSmith.Data/Types/PackageModuleType.cs"
#load "src/PackageSmith.Data/Types/TemplateType.cs"
#load "src/PackageSmith.Data/Types/LicenseType.cs"

using PackageSmith.Core.Logic;

string ns;
PackageLogic.GenerateNamespace("com.company.my-tool", out ns);
Console.WriteLine($"com.company.my-tool -> {ns}");

PackageLogic.GenerateNamespace("com.unity.entities", out ns);
Console.WriteLine($"com.unity.entities -> {ns}");
