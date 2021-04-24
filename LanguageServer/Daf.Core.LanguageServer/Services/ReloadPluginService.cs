// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;
using Daf.Core.LanguageServer.Model;
using Daf.Core.LanguageServer.Status;

namespace Daf.Core.LanguageServer.Services
{
	public class ReloadPluginService
	{
		private readonly LanguageServerPluginManager pluginManager;

		public ReloadPluginService(LanguageServerPluginManager manager)
		{
			pluginManager = manager;
		}

		public List<PluginParsingStatus> ReloadPlugins()
		{
			pluginManager.LoadAssemblies(Properties.Instance.PluginFolder);
			return pluginManager.PluginParsingStatuses;
		}
	}
}
