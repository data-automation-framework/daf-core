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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Daf.Core.LanguageServer.Tests.IntegrationTests
{
	public class IonCompletionHandler_Handle
	{
		private readonly string pluginFolder = Path.GetFullPath(@"Plugins");

		[Theory]
		[InlineData("SqlParameters", 1, 2)]
		[InlineData("Connections", 0, 5)]
		[InlineData("executesql", 3, 1)]
		internal async Task CursorAt_NodeName_ReturnsNodeAndAttributeCompletions(string nodeName, int cursorOffset, int expectedCompletions)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			CompletionService completionService = new(pluginManager);
			IonCompletionHandler completionHandler = new(documentManager, completionService);
			Position pos = DocumentUtility.GetPositionOfNode(document, nodeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			CompletionParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			CompletionList completions = await completionHandler.Handle(requestParams, cancellationToken);

			Assert.Equal(expectedCompletions, completions.Items.Count());
		}

		[Theory]
		[InlineData("FlatFileColumn", "DataType", 1, 2)]
		[InlineData("FlatFileColumn", "DataType", 2, 1)]
		[InlineData("SqlParameter", "variablename", 2, 1)]
		internal async Task CursorAt_AttributeName_ReturnsNodeAndAttributeCompletions(string nodeName, string attributeName, int cursorOffset, int expectedCompletions)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			CompletionService completionService = new(pluginManager);
			IonCompletionHandler completionHandler = new(documentManager, completionService);
			Position pos = DocumentUtility.GetPositionOfAttribute(document, nodeName, attributeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			CompletionParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			CompletionList completions = await completionHandler.Handle(requestParams, cancellationToken);

			Assert.Equal(expectedCompletions, completions.Items.Count());
		}

		[Theory]
		[InlineData("FlatFileColumn", "DataType", 13, 4)]
		[InlineData("FlatFileColumn", "DataType", 18, 1)]
		internal async Task CursorAt_AttributeEnumValue_ReturnsEnumValueCompletions(string nodeName, string attributeName, int cursorOffset, int expectedCompletions)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			CompletionService completionService = new(pluginManager);
			IonCompletionHandler completionHandler = new(documentManager, completionService);
			Position pos = DocumentUtility.GetPositionOfAttribute(document, nodeName, attributeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			CompletionParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			CompletionList completions = await completionHandler.Handle(requestParams, cancellationToken);

			Assert.Equal(expectedCompletions, completions.Items.Count());
		}

		[Theory]
		[InlineData("SqlParameters", 0, 13)]
		[InlineData("SequenceContainer", 3, 1)]
		internal async Task CursorAt_NonRootFileNodeName_ReturnsNodeAndAttributeCompletions(string nodeName, int cursorOffset, int expectedCompletions)
		{
			LanguageServer.Properties.Instance.ProjectFilePath = Path.GetFullPath("..\\..\\..\\Resources\\EmptyProjectFile.dafproj");
			IonDocument document = DocumentUtility.GetNonRootIonDocument();
			IonDocumentManager documentManager = new();
			string documentPath = Constants.DummyRootNodeFileUri;
			documentManager.UpdateDocument(documentPath, document.Content);
			LanguageServerPluginManager pluginManager = new();
			pluginManager.LoadAssemblies(pluginFolder);
			CompletionService completionService = new(pluginManager);
			IonCompletionHandler completionHandler = new(documentManager, completionService);
			Position pos = DocumentUtility.GetPositionOfNode(document, nodeName);
			pos.Character += cursorOffset;
			TextDocumentIdentifier textDocument = new(new Uri(documentPath));
			CompletionParams requestParams = new()
			{
				TextDocument = textDocument,
				Position = pos
			};
			CancellationToken cancellationToken = new();

			CompletionList completions = await completionHandler.Handle(requestParams, cancellationToken);

			Assert.Equal(expectedCompletions, completions.Items.Count());
		}
	}
}
