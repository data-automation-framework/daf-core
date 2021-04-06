// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.IO;
using System.Linq;
using CommandLine;
using Daf.Core.Sdk;

namespace Daf.Core
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args).WithParsed(options => Execute(options));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We actually want to catch any exception here.")]
		private static void Execute(Options options)
		{
			SetProperties(options);

			try
			{
				PluginManager.Execute();
			}
			catch (Exception exception)
			{
				Console.WriteLine("An unhandled exception was thrown by Daf.Core!");
				Console.WriteLine(exception.GetType().Name);
				Console.WriteLine(exception.StackTrace);
				Console.WriteLine($"Exception message: {exception.Message}");

				Environment.Exit(1);
			}
		}

		private static void SetProperties(Options options)
		{
			Properties singleton = Properties.Instance;
			singleton.ProjectDirectory = Path.GetDirectoryName(options.IntermediateFile);
			singleton.FilePath = Path.GetFileName(options.IntermediateFile);
			singleton.OutputDirectory = options.OutputDirectory;
			singleton.CommandLine = Parser.Default.FormatCommandLine(options);
			singleton.SetOtherProperties(options.Parameters!.ToList());
		}
	}
}
