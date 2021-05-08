// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.JsonRpc;

namespace Daf.Core.LanguageServer.Handlers
{
	public class ReloadPluginRequest : IRequest
	{
	}

	[Method("daf/reloadplugins", Direction.ClientToServer)]
	[Parallel]
	internal interface IReloadPluginHandler : IJsonRpcHandler, IRequestHandler<ReloadPluginRequest>
	{
	}
}
