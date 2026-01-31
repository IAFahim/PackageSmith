using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PackageSmith.Core.Logic;

public static class FileSystemLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DirectoryExists(string path, out bool exists)
	{
		exists = System.IO.Directory.Exists(path);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void FileExists(string path, out bool exists)
	{
		exists = System.IO.File.Exists(path);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CreateDirectory(string path)
	{
		System.IO.Directory.CreateDirectory(path);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteFile(string path, string content)
	{
		System.IO.File.WriteAllText(path, content);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ReadFile(string path, out string content)
	{
		content = System.IO.File.ReadAllText(path);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DeleteFile(string path)
	{
		System.IO.File.Delete(path);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetDirectoryName(string path, out string directoryName)
	{
		directoryName = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
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
			path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
			return;
		}

		path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void EnsureDirectories(string[] paths)
	{
		foreach (var path in paths)
		{
			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}
		}
	}
}
