// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;

namespace Daf.Core.LanguageServer.Model
{
	public class IonField
	{
		public string AssemblyName { get; set; }

		public string Namespace { get; set; }

		public string Name { get; set; }

		public bool IsValid { get; set; } = true;

		public string TypeName { get; set; }

		public string DerivedFrom { get; set; }

		public bool IsRequired { get; set; }

		public string DefaultValue { get; set; } = string.Empty;

		public bool HasDefaultValue
		{
			get { return !string.IsNullOrEmpty(DefaultValue); }
		}

		public List<string> AvailableValues { get; } = new();

		public IonDocumentation Documentation { get; set; } = new();

		public IonField(string assemblyName, string ns, string name, string typeName, string derivedFrom)
		{
			AssemblyName = assemblyName;
			Namespace = ns;
			Name = name;
			TypeName = typeName;
			DerivedFrom = derivedFrom;
		}
	}
}
