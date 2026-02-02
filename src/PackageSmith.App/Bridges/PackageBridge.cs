using System.IO;
using PackageSmith.Core.Extensions;
using PackageSmith.Core.Logic;
using PackageSmith.Core.Pipelines;
using PackageSmith.Data.State;
using PackageSmith.Data.Types;

namespace PackageSmith.App.Bridges;

public sealed class PackageBridge : IPackageBridge
{
    private readonly BuildPipeline _buildPipeline;

    public PackageBridge()
    {
        _buildPipeline = new BuildPipeline();
    }

    public bool TryCreate(in PackageState package)
    {
        var configBridge = new ConfigBridge();
        var config = configBridge.TryLoad(out var c) ? c : configBridge.GetDefault();

        VirtualFileState[] files;
        bool success;

        if (package.SelectedTemplate != TemplateType.None && !string.IsNullOrEmpty(package.TemplatePath))
            success = TemplateGeneratorLogic.TryGenerateState(
                package.TemplatePath,
                package.PackageName,
                package.DisplayName,
                package.Description,
                package.CompanyName,
                package.UnityVersion,
                out files);
        else
            success = _buildPipeline.TryGenerate(in package, in config, out _, out files);

        if (!success) return false;

        TransactionLogic.CreateTransaction(package.OutputPath, out var tx);
        if (!tx.TryBegin()) return false;

        foreach (var file in files)
        {
            var relative = Path.IsPathRooted(file.Path)
                ? Path.GetRelativePath(package.OutputPath, file.Path)
                : file.Path;

            if (!tx.TryWriteFile(relative, file.Content))
            {
                tx.TryRollback();
                return false;
            }
        }

        return tx.TryCommit();
    }

    public bool TryCreateFromTemplate(string templatePath, in PackageState package)
    {
        if (!TemplateGeneratorLogic.TryGenerateState(
                templatePath,
                package.PackageName,
                package.DisplayName,
                package.Description,
                package.CompanyName,
                package.UnityVersion,
                out var files))
            return false;

        TransactionLogic.CreateTransaction(package.OutputPath, out var tx);
        if (!tx.TryBegin()) return false;

        foreach (var file in files)
            if (!tx.TryWriteFile(file.Path, file.Content))
            {
                tx.TryRollback();
                return false;
            }

        return tx.TryCommit();
    }
}