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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void TryGetGitConfig(out string userName, out string userEmail)
	{
		userName = "Unknown";
		userEmail = "unknown@local";

		try
		{
			userName = RunGitConfig("user.name") ?? "Unknown";
			userEmail = RunGitConfig("user.email") ?? "unknown@local";
		}
		catch
		{
			// Fallback to defaults
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string? RunGitConfig(string key)
	{
		try
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "git",
				Arguments = $"config --get {key}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = Process.Start(startInfo);
			if (proc != null)
			{
				var result = proc.StandardOutput.ReadToEnd()?.Trim();
				proc.WaitForExit();
				return proc.ExitCode == 0 && !string.IsNullOrEmpty(result) ? result : null;
			}
		}
		catch
		{
			// Ignore
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateLicense(LicenseType license, string year, string copyrightHolder)
	{
		var y = string.IsNullOrEmpty(year) ? DateTime.Now.Year.ToString() : year;
		var holder = string.IsNullOrEmpty(copyrightHolder) ? "Your Company" : copyrightHolder;

		return license switch
		{
			LicenseType.Mit => $$"""
			MIT License

			Copyright (c) {{y}} {{holder}}

			Permission is hereby granted, free of charge, to any person obtaining a copy
			of this software and associated documentation files (the "Software"), to deal
			in the Software without restriction, including without limitation the rights
			to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
			copies of the Software, and to permit persons to whom the Software is
			furnished to do so, subject to the following conditions:

			The above copyright notice and this permission notice shall be included in all
			copies or substantial portions of the Software.

			THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
			IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
			FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
			AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
			LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
			OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
			SOFTWARE.
			""",
			LicenseType.Apache20 => $$"""
			Apache License
			Version 2.0, January 2004
			http://www.apache.org/licenses/

			TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

			Licensed under the Apache License, Version 2.0 (the "License");
			you may not use this file except in compliance with the License.
			You may obtain a copy of the License at

			    http://www.apache.org/licenses/LICENSE-2.0

			Unless required by applicable law or agreed to in writing, software
			distributed under the License is distributed on an "AS IS" BASIS,
			WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
			See the License for the specific language governing permissions and
			limitations under the License.

			Copyright {{y}} {{holder}}
			""",
			LicenseType.Proprietary => $$"""
			PROPRIETARY LICENSE

			Copyright (c) {{y}} {{holder}}

			All rights reserved.

			This software and associated documentation files (the "Software") are the
			proprietary and confidential information of {{holder}}. The Software is
			licensed, not sold, and may only be used in accordance with the terms of
			a separate license agreement between you and {{holder}}.

			RESTRICTIONS:

			- You may not copy, modify, or distribute the Software without express
			  written permission from {{holder}}.
			- You may not use the Software for any purpose outside of the terms
			  of your license agreement.
			- All title and copyright in and to the Software and any copies thereof
			  are owned by {{holder}}.

			For information about obtaining a license, contact:
			{{holder}}
			""",
			LicenseType.UnityCompanion => $$"""
			{{holder}} copyright © {{y}}

			Licensed under the Unity Companion License for Unity-dependent projects--see [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License).

			Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.
			""",
			_ => string.Empty
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GenerateChangelog(string packageName, string version = "1.0.0")
	{
		var today = DateTime.Now.ToString("yyyy-MM-dd");
		return $"# Changelog\n\n" +
			"All notable changes to this project will be documented in this file.\n\n" +
			"The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),\n" +
			"and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).\n\n" +
			$"## [{version}] - {today}\n\n" +
			"### Added\n" +
			$"- Initial release of {packageName}\n";
	}
}
