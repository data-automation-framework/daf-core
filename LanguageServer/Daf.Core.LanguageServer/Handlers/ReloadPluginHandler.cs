// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Daf.Core.LanguageServer.Services;
using Daf.Core.LanguageServer.Status;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Daf.Core.LanguageServer.Handlers
{
	public class ReloadPluginHandler : IReloadPluginHandler
	{
		private readonly ILanguageServerFacade _router;
		private readonly ReloadPluginService _reloadPluginService;

		public ReloadPluginHandler(ILanguageServerFacade router, ReloadPluginService reloadPluginService)
		{
			_reloadPluginService = reloadPluginService;
			_router = router;
		}

		public Task<Unit> Handle(ReloadPluginRequest request, CancellationToken cancellationToken)
		{
			List<PluginParsingStatus> statuses = _reloadPluginService.ReloadPlugins();

			foreach (PluginParsingStatus status in statuses)
			{
				if (status.Status == OperationStatus.Error)
				{
					_router.Window.LogError(status.Message);
				}
				else if (status.Status == OperationStatus.Warning)
				{
					_router.Window.LogWarning(status.Message);
				}
			}

			return Unit.Task;
		}
	}
}
