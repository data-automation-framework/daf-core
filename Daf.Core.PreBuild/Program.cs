// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace Daf.Core.PreBuild
{
	public static class Program
	{
		private const string OutFileName = "intermediate.ion";

		public static int Main(string[] args)
		{
			string projectPath = string.Empty;
			IEnumerable<string>? parameters = null;

			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(o =>
				{
					projectPath = o.ProjectFullPath!;
					parameters = o.Parameters;
				});

			string? projectFolder = Path.GetDirectoryName(projectPath);
			string rootFile = GetRootFile(projectFolder!);
			string outFile = Path.Combine(Path.GetDirectoryName(rootFile)!, OutFileName);

			T4Generator generator = new(projectPath, rootFile, outFile);

#pragma warning disable CA1508 // Avoid dead conditional code. This isn't actually null though...
			foreach (string parameter in parameters!)
#pragma warning restore CA1508 // Avoid dead conditional code
			{
				string[] keysAndValues = parameter.Split('=');
				Tuple<string, string> property = new(keysAndValues[0], keysAndValues[1]);

				generator.Properties.Add(property);
			}

			if (generator.Execute())
				return 0;
			else
				return 1;
		}

		private static string GetRootFile(string rootDirectory)
		{
			string[] files = Directory.GetFiles(rootDirectory, "*.daf", SearchOption.AllDirectories);

			List<string> rootFiles = new();

			foreach (string file in files)
			{
				if (RootFileHandler.IsRootFile(file))
					rootFiles.Add(file);
			}

			if (rootFiles.Count == 0)
				throw new InvalidOperationException($"No root .daf file found. The root .daf file needs to start with a line containing exactly these characters: {RootFileHandler.RootFileIndicator}");

			if (rootFiles.Count > 1)
			{
				string allRootFiles = Environment.NewLine + string.Join(Environment.NewLine, rootFiles);
				throw new InvalidOperationException($"More than one root file found. Only a single root file is allowed per project. The following files have been identified as root files: ${allRootFiles}");
			}

			return rootFiles[0];
		}
	}
}
