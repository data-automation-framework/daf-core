// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Daf.Core.BuildTasks
{
	/// <summary>
	/// This is used by the .targets file in order to build key-value property pairs for MSBuild.
	/// </summary>
	public class PropertyBuilder : Task
	{
		[Required]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Expected by MSBuild.")]
		public ITaskItem[]? InputProperties { get; set; }

		[Output]
		public string? Result { get; set; }

		public override bool Execute()
		{
			List<string> keyValueStrings = new();

			foreach (ITaskItem taskItem in InputProperties!)
			{
				string key = taskItem.GetMetadata("Identity");
				string value = taskItem.GetMetadata("Value");

				keyValueStrings.Add($"{key}={value}");
			}

			Result = string.Join(';', keyValueStrings);

			return true;
		}
	}
}
