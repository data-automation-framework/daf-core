// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;

namespace Daf.Core.LanguageServer.Model
{
	public class IonDocumentation
	{
		public string AssemblyPath { get; set; }

		public string TypeName { get; set; }

		public string Summary { get; set; }

		public bool IsValid { get; set; }

		public bool IsForProperty { get; set; }

		public bool IsForType { get; set; }

		public List<IonDocumentation> Children { get; }

		public IonDocumentation()
		{
			AssemblyPath = "";
			Summary = "No documentation";
			TypeName = "Unknown";
			IsValid = false;
			Children = new List<IonDocumentation>();
		}

		public IonDocumentation Clone()
		{
			IonDocumentation clone = new()
			{
				AssemblyPath = AssemblyPath,
				TypeName = TypeName,
				Summary = Summary,
				IsValid = IsValid
			};
			clone.Children.AddRange(Children);

			return clone;
		}
	}
}
