// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;
using CommandLine;

namespace Daf.Core
{
	public class Options
	{
		[Option('m', "intermediate", Required = true, HelpText = "The intermediate ion file to process.")]
		public string IntermediateFile { get; init; } = string.Empty;

		[Option('o', "output", Required = true, HelpText = "The build output directory.")]
		public string OutputDirectory { get; init; } = string.Empty;

		[Option('p', "parameters", Separator = ';', Required = false, HelpText = "Optional parameters.")]
		public IEnumerable<string>? Parameters { get; init; }
	}
}
