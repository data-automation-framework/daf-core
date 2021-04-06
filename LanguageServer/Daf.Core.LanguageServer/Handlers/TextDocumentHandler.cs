// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServer.Model;
using Daf.Core.LanguageServer.Status;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Daf.Core.LanguageServer.Handlers
{
	//Handles syncing of files between the LSP client and server
	public sealed class DocumentHandler : ITextDocumentSyncHandler
	{
		private readonly ILanguageServerFacade _router;
		private readonly IonDocumentManager _documentManager;
		private readonly DocumentSelector _documentSelector;
		private SynchronizationCapability? _capability;

		public DocumentHandler(ILanguageServerFacade router, IonDocumentManager documentManager, LanguageServerPluginManager pluginManager)
		{
			if (pluginManager == null)
				throw new ArgumentNullException(nameof(pluginManager));

			_router = router;
			_documentManager = documentManager;
			_documentSelector = new DocumentSelector(
				new DocumentFilter() { Pattern = Constants.DafDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IonDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IntermediateDocumentFilterPattern }
			);

			foreach (PluginParsingStatus pps in pluginManager.PluginParsingStatuses)
			{
				if (pps.Status == OperationStatus.Error)
				{
					_router.Window.LogError(pps.Message);
				}
				else if (pps.Status == OperationStatus.Warning)
				{
					_router.Window.LogWarning(pps.Message);
				}
			}
		}

		//Handles text syncing when new characters are entered into the document
		Task<Unit> IRequestHandler<DidChangeTextDocumentParams, Unit>.Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
		{
			//TODO: Adapt to incremental sync
			string documentPath = request.TextDocument.Uri.ToString();
			string documentContent = request.ContentChanges.First().Text;

			_documentManager.UpdateDocument(documentPath, documentContent);

			return Unit.Task;
		}

		//Handle opening of new document
		Task<Unit> IRequestHandler<DidOpenTextDocumentParams, Unit>.Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
		{
			string documentPath = request.TextDocument.Uri.ToString();
			string documentContent = request.TextDocument.Text;
			_documentManager.UpdateDocument(documentPath, documentContent);

			return Unit.Task;
		}

		//Handle closing down of document
		Task<Unit> IRequestHandler<DidCloseTextDocumentParams, Unit>.Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
		{
			string documentPath = request.TextDocument.Uri.ToString();
			_documentManager.RemoveDocument(documentPath);

			return Unit.Task;
		}

		Task<Unit> IRequestHandler<DidSaveTextDocumentParams, Unit>.Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
		{
			return Unit.Task;
		}

		public void SetCapability(SynchronizationCapability capability)
		{
			_capability = capability;
		}

		public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
		{
			return new TextDocumentAttributes(uri, "ion");
		}

		public TextDocumentChangeRegistrationOptions GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
		{
			return new TextDocumentChangeRegistrationOptions()
			{
				DocumentSelector = _documentSelector,
				SyncKind = TextDocumentSyncKind.Full        //TODO: Change this to incremental and handle in IonDocumentManager for better performance
			};
		}

		TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
		{
			return new TextDocumentOpenRegistrationOptions()
			{
				DocumentSelector = _documentSelector
			};
		}

		TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
		{
			return new TextDocumentCloseRegistrationOptions()
			{
				DocumentSelector = _documentSelector
			};
		}

		TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
		{
			return new TextDocumentSaveRegistrationOptions()
			{
				DocumentSelector = _documentSelector,
				IncludeText = true
			};
		}
	}
}
