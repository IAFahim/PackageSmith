namespace PackageSmith.Core.Generation;

public interface IFileSystemWriter
{
    bool TryWrite(in PackageLayout layout);
}
