using PackageSmith.Data.Config;
using PackageSmith.Data.State;

namespace PackageSmith.App.Bridges;

public interface IConfigBridge
{
    bool TryLoad(out AppConfig config);
    bool TrySave(in AppConfig config);
    bool TryDelete();
    AppConfig GetDefault();
}

public interface IPackageBridge
{
    bool TryCreate(in PackageState package);
}