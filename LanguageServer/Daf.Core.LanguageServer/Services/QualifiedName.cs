// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer.Services
{
	public class QualifiedName
	{
		public string? AssemblyName { get; set; }
		public string? Namespace { get; set; }
		public bool AssemblyNameSet { get { return AssemblyName != null; } }
		public bool NamespaceSet { get { return Namespace != null; } }
		public string Name { get; set; }

		public QualifiedName(string name)
		{
			Name = name;
		}
	}
}
