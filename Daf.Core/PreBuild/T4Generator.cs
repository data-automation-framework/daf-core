// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace Daf.Core.PreBuild
{
	public class T4Generator
	{
		public T4Generator(string projectPath, string inFile, string outFile)
		{
			ProjectPath = projectPath;
			InFile = Path.GetFullPath(inFile);
			this.outFile = outFile;
		}

		public string ProjectPath { get; }

		public string InFile { get; }

		private string outFile;
		public string OutFile
		{
			get { return outFile; }
		}

		public string OutPath
		{
			get { return Path.GetFullPath(OutFile); }
		}

		public List<Tuple<string, string>> Properties { get; } = new();

		/// <summary>
		/// Processes the project directory and generates a raw intermediate.ion file for further processing.
		/// </summary>
		/// <returns>True if successful, otherwise false</returns>
		public bool Execute()
		{
			DafTemplateGenerator generator = new();

			generator.AddReferences(ProjectPath);
			generator.AddImports();
			generator.AddParameters(Properties);

			string ion = File.ReadAllText(InFile);
			bool success = generator.ProcessTemplate(InFile, ion, ref outFile, out string outputContent);

			if (!success)
			{
				Console.WriteLine($"Failed to process template file {InFile}.");

				foreach (CompilerError error in generator.Errors)
				{
					if (!error.IsWarning)
						Console.WriteLine(error.ToString());
				}

				return false;
			}

			// Strip root file indicator before writing output.
			if (outputContent.StartsWith(RootFileHandler.RootFileIndicator + Environment.NewLine, StringComparison.Ordinal))
				outputContent = outputContent.Substring(RootFileHandler.RootFileIndicator.Length + Environment.NewLine.Length);
			else if (outputContent.StartsWith(RootFileHandler.RootFileIndicator, StringComparison.Ordinal))
				outputContent = outputContent.Substring(RootFileHandler.RootFileIndicator.Length);

			File.WriteAllText(OutPath, outputContent);

			//Console.WriteLine($"Pre-build step completed successfully. Generated {OutFile}");
			Console.WriteLine(OutFile); // TODO: Figure out a better way of passing the intermediate file path to the next step in the build process.

			return true;
		}

		// This is broken, doesn't work properly. Probably due to both the 2nd if and the final handling inside the loop.
		// It doesn't matter at the moment, the premise is faulty since we can't know whether we're in a "text block" when including a file,
		// which in turn means we don't know whether we can treat all # as comments (such as temp tables).
		// Hardcoded to only remove the standard Daf comment header, for now.
		internal static string RemoveComments(string ion)
		{
			string licenseHeader = "# SPDX-License-Identifier: MIT\n# Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors\n";

			return ion.Replace(licenseHeader, string.Empty, StringComparison.InvariantCulture);

			//StringBuilder ionWithoutCommentsBuilder = new StringBuilder();
			//int pos = 0;

			//while (pos != -1)
			//{
			//	int commentStart = ion.IndexOf('#', pos);
			//	int t4Start = ion.IndexOf("<#", pos);
			//	int t4End = ion.IndexOf("#>", pos);

			//	// No comments (#) were found in the document, may as well break early.
			//	if (commentStart == -1)
			//		break;

			//	// A comment character (#) was found, but it was part of a t4 start or end tag. Keep looping.
			//	if (commentStart == t4Start + 1 || commentStart == t4End)
			//	{
			//		ionWithoutCommentsBuilder.Append(ion.Substring(pos, commentStart - pos));
			//		pos = commentStart + 1;
			//		continue;
			//	}

			//	// A valid comment character (#) + line was found. Add everything leading up to it, then skip until the end of the line. Keep looping.
			//	ionWithoutCommentsBuilder.Append(ion.Substring(pos, commentStart - pos));

			//	int commentEnd = ion.IndexOf('\n', commentStart);
			//	pos = commentEnd + 1;
			//}

			//ionWithoutCommentsBuilder.Append(ion.Substring(pos));

			//return ionWithoutCommentsBuilder.ToString();
		}
	}
}
