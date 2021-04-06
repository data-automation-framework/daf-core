// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;

namespace Daf.Core.LanguageServer.Model
{
	public class PluginDocumentation
	{
		public string AssemblyName { get; set; }
		public Dictionary<string, IonDocumentation> Documentation { get; }

		public PluginDocumentation(string assemblyName, Dictionary<string, IonDocumentation> documentation)
		{
			AssemblyName = assemblyName;
			Documentation = documentation;
		}
	}
}
