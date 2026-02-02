using System.IO;
using PackageSmith.Core.Logic;
using PackageSmith.Data.State;

namespace PackageSmith.Core.Extensions;

public static class FileSystemExtensions
{
    public static bool TryEnsureDirectory(this string path)
    {
        FileSystemLogic.DirectoryExists(path, out var exists);
        if (!exists)
        {
            FileSystemLogic.CreateDirectory(path);
            return true;
        }

        return true;
    }

    public static bool TryWrite(in this VirtualFileState file)
    {
        try
        {
            var dir = Path.GetDirectoryName(file.Path);
            if (!string.IsNullOrEmpty(dir)) dir.TryEnsureDirectory();

            FileSystemLogic.WriteFile(file.Path, file.Content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryWriteAll(this VirtualFileState[] files)
    {
        foreach (var file in files)
        {
            var temp = file;
            if (!temp.TryWrite()) return false;
        }

        return true;
    }

    public static bool TryCreate(in this VirtualDirectoryState directory)
    {
        try
        {
            FileSystemLogic.CreateDirectory(directory.Path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryCreateAll(this VirtualDirectoryState[] directories)
    {
        foreach (var dir in directories)
        {
            var temp = dir;
            if (!temp.TryCreate()) return false;
        }

        return true;
    }
}