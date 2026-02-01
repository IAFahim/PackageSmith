using System;
using System.IO;
using PackageSmith.Data.State;
using PackageSmith.Core.Logic;

namespace PackageSmith.Core.Extensions;

public static class TransactionExtensions
{
	public static bool TryBegin(ref this TransactionState state)
	{
		if (Directory.Exists(state.TempPath)) return false;

		try
		{
			Directory.CreateDirectory(state.TempPath);
			return true;
		}
		catch { return false; }
	}

	public static bool TryWriteFile(ref this TransactionState state, string relativePath, string content)
	{
		TransactionLogic.GetShadowPath(in state, relativePath, out var fullPath);

		try
		{
			var dir = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir!);
			File.WriteAllText(fullPath, content);
			return true;
		}
		catch { return false; }
	}

	public static bool TryCommit(ref this TransactionState state)
	{
		if (state.IsCommitted) return false;

		try
		{
			if (!Directory.Exists(state.TargetPath)) Directory.CreateDirectory(state.TargetPath);

			// Move temp files to target
			CopyDirectory(state.TempPath, state.TargetPath);

			state.IsCommitted = true;
			TryRollback(ref state); // Cleanup temp
			return true;
		}
		catch
		{
			TryRollback(ref state); // Fail safe
			return false;
		}
	}

	public static bool TryRollback(ref this TransactionState state)
	{
		try
		{
			if (Directory.Exists(state.TempPath)) Directory.Delete(state.TempPath, true);
			return true;
		}
		catch { return false; }
	}

	private static void CopyDirectory(string sourceDir, string destDir)
	{
		var dir = new DirectoryInfo(sourceDir);
		foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
		{
			var rel = Path.GetRelativePath(sourceDir, file.FullName);
			var dest = Path.Combine(destDir, rel);
			Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
			file.CopyTo(dest, true);
		}

		foreach (var subDir in dir.GetDirectories("*", SearchOption.AllDirectories))
		{
			var rel = Path.GetRelativePath(sourceDir, subDir.FullName);
			var dest = Path.Combine(destDir, rel);
			Directory.CreateDirectory(dest);
		}
	}
}
