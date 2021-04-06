// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Generic;
using System.IO;
using Mono.TextTemplating;

namespace Daf.Core.PreBuild
{
	/// <summary>
	/// Inherits T4's default TemplateGenerator in order to handle includes referring to content in
	/// nuget packages as well as setting up the required indentation handling for ion files.
	/// </summary>
	public class DafTemplateGenerator : TemplateGenerator
	{
		internal Dictionary<string, string> DafCoreNugets { get; } = new();

		protected override bool LoadIncludeText(string requestFileName, out string content, out string location)
		{
			if (requestFileName == null)
				throw new ArgumentNullException(nameof(requestFileName));

			// Handle nuget includes.
			if (requestFileName.StartsWith(@"Nuget\", StringComparison.Ordinal))
			{
				string[] pathParts = requestFileName.Split(@"\");

				requestFileName = DafCoreNugets[pathParts[1]] + requestFileName.Replace(@$"{pathParts[0]}\{pathParts[1]}", string.Empty, StringComparison.Ordinal);
			}

			bool result = base.LoadIncludeText(requestFileName, out content, out location);

			// Handle indentation for daf/ion files used in include directives (potentially containing ion script nodes/attributes).
			if (Path.GetExtension(requestFileName) is ".daf" or ".ion")
				content = "--- IndentationPush ---" + Environment.NewLine + T4Generator.RemoveComments(content) + Environment.NewLine + "--- IndentationPop ---" + Environment.NewLine;

			return result;
		}

		internal void AddReferences(string projectPath)
		{
			Refs.Add(typeof(Utility.Utility).Assembly.Location);

			foreach (ProjectDependency dependency in ProjectDependencyHandler.GetProjectDependencies(projectPath))
			{
				switch (dependency)
				{
					case ContentOnlyProjectDependency contentDependency:
						DafCoreNugets.Add(contentDependency.PackageName, contentDependency.ContentPath);
						break;
					case ReferenceProjectDependency referenceDependency:
						Refs.Add(referenceDependency.AssemblyLocation);
						break;
				}
			}
		}

		internal void AddImports()
		{
			Imports.Add("System.Collections.Generic");
			Imports.Add("System.Data");
			Imports.Add("System.Linq");

			Imports.Add("Daf.Core.Utility");
		}

		internal void AddParameters(List<Tuple<string, string>> properties)
		{
			foreach (Tuple<string, string> propertyItem in properties)
			{
				string propertyName = propertyItem.Item1;
				string propertyValue = propertyItem.Item2;

				AddParameter(null, null, propertyName, propertyValue);
			}
		}
	}
}
