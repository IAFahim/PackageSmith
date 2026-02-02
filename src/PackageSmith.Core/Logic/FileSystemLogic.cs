using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PackageSmith.Core.Logic;

public static class FileSystemLogic
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DirectoryExists(string path, out bool exists)
    {
        exists = Directory.Exists(path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FileExists(string path, out bool exists)
    {
        exists = File.Exists(path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadFile(string path, out string content)
    {
        content = File.ReadAllText(path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DeleteFile(string path)
    {
        File.Delete(path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetDirectoryName(string path, out string directoryName)
    {
        directoryName = Path.GetDirectoryName(path) ?? string.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetAppDataPath(out string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            return;
        }

        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureDirectories(string[] paths)
    {
        foreach (var path in paths)
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
    }
}