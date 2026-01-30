namespace PackageSmith.Core.Generation;

public sealed class FileSystemWriter : IFileSystemWriter
{
    public bool TryWrite(in PackageLayout layout)
    {
        if (!layout.IsValid)
        {
            return false;
        }

        try
        {
            // Create all directories first
            foreach (var dir in layout.Directories)
            {
                if (!Directory.Exists(dir.Path))
                {
                    Directory.CreateDirectory(dir.Path);
                }
            }

            // Write all files
            foreach (var file in layout.Files)
            {
                var directory = Path.GetDirectoryName(file.Path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(file.Path, file.Content);
            }

            return true;
        }
        catch
        {
            // In production, you might want to clean up partial writes here
            return false;
        }
    }
}
