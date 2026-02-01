using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PackageSmith.Core.Logic;

public static class OsLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void TryOpenFolder(string path, out bool success)
	{
		success = false;
		if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
		{
			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Process.Start(new ProcessStartInfo("explorer", $"\"{path}\"") { UseShellExecute = true });
					success = true;
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", $"\"{path}\"");
					success = true;
				}
				else
				{
					Process.Start("xdg-open", $"\"{path}\"");
					success = true;
				}
			}
			catch
			{
				success = false;
			}
		}
	}
}
