using PackageSmith.Data.State;
using PackageSmith.Core.Interfaces;
using PackageSmith.Core.Extensions;

namespace PackageSmith.Core.Pipelines;

public sealed class FileSystemPipeline : IFileSystemWriter
{
	public bool TryWrite(in PackageLayoutState layout, VirtualDirectoryState[] directories, VirtualFileState[] files)
	{
		if (layout.DirectoryCount == 0 && layout.FileCount == 0) return false;

		try
		{
			directories.TryCreateAll();
			files.TryWriteAll();
			return true;
		}
		catch
		{
			return false;
		}
	}
}
