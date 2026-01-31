using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PackageSmith.Data.Types;

namespace PackageSmith.Core.Logic;

public static class GitLogic
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateGitIgnore()
	{
		return """
		# Unity generated
		/[Ll]ibrary/
		/[Tt]emp/
		/[Oo]bj/
		/[Bb]uild/
		/[Bb]uilds/
		/[Ll]ogs/
		/[Uu]ser[Ss]ettings/

		# IDEs
		/*.csproj
		/*.sln
		/*.suo
		/*.user
		/*.userprefs
		/*.unityproj
		/.idea/
		/.vscode/

		# OS
		.DS_Store
		Thumbs.db
		""";
	}

	public static void TryInitGit(string directory, out bool success)
	{
		success = false;
		try
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "git",
				Arguments = "init",
				WorkingDirectory = directory,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = Process.Start(startInfo);
			if (proc != null)
			{
				proc.WaitForExit();
				success = proc.ExitCode == 0;
			}
		}
		catch
		{
			success = false;
		}
	}
}
