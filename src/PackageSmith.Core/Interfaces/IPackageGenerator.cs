using PackageSmith.Data.Config;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Interfaces;

public interface IPackageGenerator
{
	bool TryGenerate(in PackageState package, in AppConfig config, out PackageLayoutState layout);
}
