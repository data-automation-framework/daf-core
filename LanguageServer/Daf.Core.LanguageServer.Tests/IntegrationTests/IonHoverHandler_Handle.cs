// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServer.Handlers;
using Daf.Core.LanguageServer.Model;
using Daf.Core.LanguageServer.Services;
using Daf.Core.LanguageServer.Tests.Utility;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Daf.Core.LanguageServer.Tests.IntegrationTests
{
	public class IonHoverHandler_Handle
	{
		private readonly string pluginFolder = Path.GetFullPath(@"Plugins");

		[Theory]
		[InlineData("Package", 1)]
		[InlineData("SequenceContainer", 1)]
		[InlineData("SqlStatement", 1)]
		[InlineData("PrecedenceConstraints", 1)]
		[InlineData("IntegrationTestPlugin", 1)]
		internal async Task CursorAt_NodeNameWithDocumentation_ReturnsHoverContents(string nodeName, int cursorOffset)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			HoverService hoverService = new(pluginManager);
			IonHoverHandler hoverHandler = new(documentManager, hoverService);
			Position pos = DocumentUtility.GetPositionOfNode(document, nodeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			HoverParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			Hover? hover = await hoverHandler.Handle(requestParams, cancellationToken);

			Assert.NotNull(hover);
			Assert.NotNull(hover!.Contents);
		}

		[Theory]
		[InlineData("Package", "Name", 1)]
		[InlineData("FlatFileColumn", "DataType", 1)]
		[InlineData("ExecuteSql", "ConnectionName", 1)]
		[InlineData("Expression", "ExpressionValue", 1)]
		internal async Task CursorAt_AttributeNameWithDocumentation_ReturnsHoverContents(string nodeName, string attributeName, int cursorOffset)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			HoverService hoverService = new(pluginManager);
			IonHoverHandler hoverHandler = new(documentManager, hoverService);
			Position pos = DocumentUtility.GetPositionOfAttribute(document, nodeName, attributeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			HoverParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			Hover? hover = await hoverHandler.Handle(requestParams, cancellationToken);

			Assert.NotNull(hover);
			Assert.NotNull(hover!.Contents);
		}

		[Theory]
		[InlineData("FlatFileColumn", "DataType", 14)]
		internal async Task CursorAt_AttributeEnumeratedValue_ReturnsHoverContents(string nodeName, string attributeName, int cursorOffset)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			HoverService hoverService = new(pluginManager);
			IonHoverHandler hoverHandler = new(documentManager, hoverService);
			Position pos = DocumentUtility.GetPositionOfAttribute(document, nodeName, attributeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			HoverParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			Hover? hover = await hoverHandler.Handle(requestParams, cancellationToken);

			Assert.NotNull(hover);
			Assert.NotNull(hover!.Contents);
		}

		[Theory]
		[InlineData("FlatFileConnection", "Name", 6)]
		internal async Task CursorAt_AttributeWithoutEnumeratedValue_ReturnsEmptyHoverContents(string nodeName, string attributeName, int cursorOffset)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			HoverService hoverService = new(pluginManager);
			IonHoverHandler hoverHandler = new(documentManager, hoverService);
			Position pos = DocumentUtility.GetPositionOfAttribute(document, nodeName, attributeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			HoverParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			Hover? hover = await hoverHandler.Handle(requestParams, cancellationToken);

			Assert.NotNull(hover);
			if (hover!.Contents.HasMarkedStrings)
			{
				foreach (MarkedString? x in hover!.Contents.MarkedStrings!)
				{
					Assert.Equal(string.Empty, x);
				}
			}
			if (hover!.Contents.HasMarkupContent)
			{
				Assert.Equal(string.Empty, hover!.Contents.MarkupContent!.Value);
			}
		}

		[Theory]
		[InlineData("InputPath", "OutputPathName", 1)]
		internal async Task CursorAt_AttributeNameWithoutDocumentation_ReturnsEmptyHoverContents(string nodeName, string attributeName, int cursorOffset)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			HoverService hoverService = new(pluginManager);
			IonHoverHandler hoverHandler = new(documentManager, hoverService);
			Position pos = DocumentUtility.GetPositionOfAttribute(document, nodeName, attributeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			HoverParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			Hover? hover = await hoverHandler.Handle(requestParams, cancellationToken);

			Assert.NotNull(hover);
			if (hover!.Contents.HasMarkedStrings)
			{
				foreach (MarkedString? x in hover!.Contents.MarkedStrings!)
				{
					Assert.Equal(string.Empty, x);
				}
			}
			if (hover!.Contents.HasMarkupContent)
			{
				Assert.Equal(string.Empty, hover!.Contents.MarkupContent!.Value);
			}
		}

	}
}
