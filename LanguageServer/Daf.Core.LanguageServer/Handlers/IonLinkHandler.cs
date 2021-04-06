// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Daf.Core.LanguageServer.Document;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Daf.Core.LanguageServer.Handlers
{
	public class IonLinkHandler : IDocumentLinkHandler
	{
		private readonly IonDocumentManager _documentManager;
		private readonly DocumentSelector _documentSelector;
		private DocumentLinkCapability? _capability;

		public IonLinkHandler(IonDocumentManager documentManager)
		{
			_documentManager = documentManager;
			_documentSelector = new DocumentSelector(
				new DocumentFilter() { Pattern = Constants.DafDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IonDocumentFilterPattern }
			);
		}

		public DocumentLinkRegistrationOptions GetRegistrationOptions(DocumentLinkCapability capability, ClientCapabilities clientCapabilities)
		{
			return new DocumentLinkRegistrationOptions
			{
				DocumentSelector = _documentSelector,
				ResolveProvider = true
			};
		}

		public async Task<DocumentLinkContainer> Handle(DocumentLinkParams request, CancellationToken cancellationToken)
		{
			List<DocumentLink> l = new();
			await Task.Run(() =>
			{
				IonDocument? document = _documentManager.GetDocument(request.TextDocument.Uri.ToString());

				if (document != null)
				{
					List<Location> links = document.Links;
					foreach (Location link in links)
					{
						DocumentLink dl = new()
						{
							Target = link.Uri,
							Range = link.Range
						};
						l.Add(dl);
					}
				}
			}, cancellationToken);
			return new DocumentLinkContainer(l);
		}

		public void SetCapability(DocumentLinkCapability capability)
		{
			_capability = capability;
		}
	}
}
