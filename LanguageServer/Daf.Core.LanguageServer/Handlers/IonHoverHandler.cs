// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServer.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Daf.Core.LanguageServer.Handlers
{
	public class IonHoverHandler : IHoverHandler
	{
		private readonly DocumentSelector _documentSelector;
		private readonly IonDocumentManager _documentManager;
		private readonly HoverService _hoverService;
		private HoverCapability? _capability;

		public IonHoverHandler(IonDocumentManager documentManager, HoverService hoverService)
		{
			_documentManager = documentManager;
			_hoverService = hoverService;
			_documentSelector = new DocumentSelector(
				new DocumentFilter() { Pattern = Constants.DafDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IonDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IntermediateDocumentFilterPattern }
			);
		}

		public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
		{
			return new HoverRegistrationOptions
			{
				DocumentSelector = _documentSelector
			};
		}

		public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			Hover h = new();
			string documentPath = request.TextDocument.Uri.ToString();
			IonDocument? document = _documentManager.GetDocument(documentPath);

			if (document == null)
			{
				return h;
			}

			int line = request.Position.Line;
			int character = request.Position.Character;
			int absolutePosition = document.GetAbsolutePosition(line, character);
			string? rootNode = document.GetRootNodeName(absolutePosition);
			DocumentContext context = document.GetPositionalContext(absolutePosition);

			string? parentNodeName = null;
			if (context.ParentNodeStartPosition != -1)
				parentNodeName = document.GetWord(context.ParentNodeStartPosition);

			string word = document.GetWord(absolutePosition, out int lineOffset);

			HoverRequest hoverRequest = new(word)
			{
				RootNodeName = rootNode,
				ParentNodeName = parentNodeName,
				SearchTextLine = line,
				SearchTextLineOffset = lineOffset
			};

			if (context.Context is PositionalContext.AtFieldName or
				PositionalContext.AtNodeName or
				PositionalContext.Unknown)
			{
				h = await _hoverService.GetDocumentationNodesAndFieldsAsync(hoverRequest);
			}
			else if (context.Context == PositionalContext.AtFieldValue)
			{
				string fieldName = "";
				if (context.FieldNameFound)
					fieldName = document.GetWord(context.FieldNameStartPosition);

				FieldValueHoverRequest hoverFieldRequest = new(hoverRequest, fieldName);

				h = await _hoverService.GetDocumentationFieldValueAsync(hoverFieldRequest);
			}

			return h;
		}

		public void SetCapability(HoverCapability capability)
		{
			_capability = capability;
		}
	}
}
