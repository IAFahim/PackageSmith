using PackageSmith.Data.State;

namespace PackageSmith.Core.Interfaces;

public interface IFileSystemWriter
{
    bool TryWrite(in PackageLayoutState layout, VirtualDirectoryState[] directories, VirtualFileState[] files);
}