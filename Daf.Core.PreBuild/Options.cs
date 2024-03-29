﻿// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;
using CommandLine;

namespace Daf.Core.PreBuild
{
	public class Options
	{
		[Option('f', "projectfullpath", Required = true, HelpText = "The project's full path.")]
		public string ProjectFullPath { get; init; } = string.Empty;

		[Option('p', "parameters", Separator = ';', Required = false, HelpText = "Optional parameters.")]
		public IEnumerable<string>? Parameters { get; init; }
	}
}
