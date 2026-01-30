using PackageSmith.Core.Configuration;

namespace PackageSmith.Core.Generation;

public interface IPackageGenerator
{
    bool TryGenerate(in PackageTemplate template, in PackageSmithConfig config, out PackageLayout layout);
}
