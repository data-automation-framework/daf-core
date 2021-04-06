// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using CommandLine;

namespace Daf.Core.LanguageServer
{
	public class IonLanguageServerOptions
	{

		[Option('f', "projectFilePath", Required = true, HelpText = "The full path to the project file for the current project.")]
		public string ProjectFilePath { get; set; } = string.Empty;
	}
}
