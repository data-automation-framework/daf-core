// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServer.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;    //Avoid conflict with System.Range

namespace Daf.Core.LanguageServer.Handlers
{
	public class IonCompletionHandler : ICompletionHandler
	{
		private readonly IonDocumentManager _documentManager;
		private readonly CompletionService _completionService;
		private readonly DocumentSelector _documentSelector;
		private CompletionCapability? _capability;

		public IonCompletionHandler(IonDocumentManager documentManager, CompletionService completionService)
		{
			_documentManager = documentManager;
			_completionService = completionService;
			_documentSelector = new DocumentSelector(
				new DocumentFilter() { Pattern = Constants.DafDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IonDocumentFilterPattern },
				new DocumentFilter() { Pattern = Constants.IntermediateDocumentFilterPattern }
			);
		}

		public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
		{
			return new CompletionRegistrationOptions
			{
				DocumentSelector = _documentSelector,
				ResolveProvider = false
			};
		}

		//Handle autocompletion requests
		public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			CompletionList completionList = new();
			string documentPath = request.TextDocument.Uri.ToString();
			IonDocument? document = _documentManager.GetDocument(documentPath);

			if (document == null)
			{
				return completionList;
			}

			int line = request.Position.Line;
			int character = request.Position.Character;
			int absolutePosition = document.GetAbsolutePosition(line, character);      //Calculate absolute caret position in the document
			string? rootNodeName = document.GetRootNodeName(absolutePosition);

			CompletionInput completionInput = GetCompletionInput(document, absolutePosition, line, character);  //Figure out what to auto complete
			int currentLevel = document.GetLineLevel(absolutePosition);

			int parentNodeLevel = 0;
			string? parentNodeName = null;
			if (completionInput.DocumentContext.ParentNodeStartPosition != -1)
			{
				parentNodeLevel = document.GetLineLevel(completionInput.DocumentContext.ParentNodeStartPosition);
				parentNodeName = document.GetWord(completionInput.DocumentContext.ParentNodeStartPosition);
			}

			//Build completion request to the completion service
			CompletionRequest completionRequest = new(completionInput.InputText)
			{
				RootNodeName = rootNodeName,
				ParentNodeName = parentNodeName,
				CurrentLevel = currentLevel,
				ParentNodeLevel = parentNodeLevel
			};

			if (completionInput.IsValid)
			{
				IReadOnlyCollection<CompletionResult> completionResults = new List<CompletionResult>();
				string textToReplace = GetReplacementText(document, completionInput.DocumentContext, absolutePosition);  //Calculate the text to be replaced after autocompletion finishes

				if (completionInput is FieldValueCompletionInput)
				{
					string fieldName = document.GetWord(completionInput.DocumentContext.FieldNameStartPosition);
					FieldValueCompletionRequest avcr = new(completionRequest, fieldName);
					completionResults = await _completionService.GetAutoCompleteFieldValuesAsync(avcr);
				}
				else if (completionInput is NodeOrFieldCompletionInput)
				{
					bool intermediaryNodes = completionInput.DocumentContext.IntermediaryNodes;
					completionResults = await _completionService.GetAutoCompleteNodesAndFieldsAsync(completionRequest, intermediaryNodes);
				}

				if (completionResults.Count > 0)
				{
					int diff;
					if (completionInput.InputText.Contains(".", StringComparison.OrdinalIgnoreCase))
					{
						diff = character - (completionInput.InputText.LastIndexOf(".", StringComparison.OrdinalIgnoreCase) + 1 + completionInput.StartCharacter);
					}
					else
					{
						diff = character - completionInput.StartCharacter;      //Difference between caret position and start of input word
					}
					List<CompletionItem> completions = new();

					foreach (CompletionResult result in completionResults)
					{
						TextEdit te = new()
						{
							NewText = result.CompletionText,
							Range = new Range(
								new Position    //Start position of text to be replaced
								{
									Line = request.Position.Line,
									Character = request.Position.Character - diff
								},
								new Position    //End position of text to be replaced
								{
									Line = request.Position.Line,
									Character = request.Position.Character + textToReplace.Length
								}
							)
						};

						CompletionItem completionItem = new()
						{
							Label = result.Label,
							Kind = CompletionItemKind.Reference,    //Determines completion symbol
							Detail = result.Detail,
							Documentation = result.Documentation,
							TextEdit = te
						};

						completions.Add(completionItem);
					}
					completionList = new CompletionList(completions, isIncomplete: completions.Count > 1);
				}
			}

			return completionList;
		}

		public void SetCapability(CompletionCapability capability)
		{
			_capability = capability;
		}

		private static CompletionInput GetCompletionInput(IonDocument document, int absolutePosition, int line, int col)
		{
			DocumentContext dc = document.GetPositionalContext(absolutePosition);

			//It is not possible to distinguish fields from child nodes when doing auto completion
			if (dc.Context is PositionalContext.AtFieldName
				or PositionalContext.AtNodeName
				or PositionalContext.Unknown) //Unknown can be at the root node
			{
				string autoCompleteStartText = document.GetStartOfWord(absolutePosition).Trim();

				NodeOrFieldCompletionInput aci = new(autoCompleteStartText, dc)
				{
					StartLine = line,
					StartCharacter = col - autoCompleteStartText.Length,
					IsValid = true
				};

				return aci;
			}
			else if (dc.Context == PositionalContext.AtFieldValue)
			{
				string autoCompleteStartText = document.GetStartOfWord(absolutePosition).Trim();

				if (autoCompleteStartText is "=" or "\"")  //Dont search with "=" or " when the cursor is right after the value indicator (=) or starting string indicator
					autoCompleteStartText = "";     //Use empty string instead to return all values

				if (dc.ParentNodeStartPosition != -1 && dc.FieldNameFound)
				{
					FieldValueCompletionInput avci = new(autoCompleteStartText, dc)
					{
						StartLine = line,
						StartCharacter = col - autoCompleteStartText.Length,
						IsValid = true
					};

					return avci;
				}
			}

			return new CompletionInput("", dc)
			{
				IsValid = false
			};
		}

		//Calculate the text to be replaced after autocompletion finishes
		private static string GetReplacementText(IonDocument document, DocumentContext context, int absolutePosition)
		{
			string replacement = "";

			if (context.Context == PositionalContext.AtFieldName)
			{
				replacement = GetFieldReplacementText(document.Content, absolutePosition);
			}
			else if (context.Context == PositionalContext.AtNodeName)
			{
				replacement = GetNodeReplacementText(document.Content, absolutePosition);
			}
			else if (context.Context == PositionalContext.AtFieldValue)
			{
				string startOfWord = document.GetStartOfWord(absolutePosition);

				if ((startOfWord == "\"" || startOfWord == "=") && absolutePosition + 1 < document.Content.Length)
				{
					replacement = document.GetWord(absolutePosition + 1);
					if (replacement == "\"")
						replacement = "";
				}
				else
				{
					string wholeWord = document.GetWord(absolutePosition);
					replacement = wholeWord[startOfWord.Length..];
				}
			}

			return replacement;
		}

		private static string GetFieldReplacementText(string content, int absolutePosition)
		{
			//Check if the field being replaced has any value (e.g. Name="FieldName" would return "=\"FieldName\"")
			int length = content.IndexOfAny(new char[] { '\r', '\n' }, absolutePosition) - absolutePosition;
			string lineRemainder;
			if (length < -1) //No newline, last line of document
			{
				lineRemainder = content[absolutePosition..];
			}
			else
			{
				lineRemainder = content.Substring(absolutePosition + 1, length);
			}
			int endPos = 0;
			bool inQuotedString = false;
			for (int i = 0; i < lineRemainder.Length; i++)
			{
				if (lineRemainder[i] == '"')
				{
					inQuotedString = !inQuotedString; //Flip bool to keep track of whether the current position is inside or outside a string
				}
				else if (!inQuotedString)          //Allow any character except newline inside string
				{
					if (Char.IsWhiteSpace(lineRemainder[i]))
					{
						endPos = i;
						break;
					}
				}
				if (lineRemainder[i] == '\r' || lineRemainder[i] == '\n' || i == lineRemainder.Length - 1)  //End of line found, break even if in quoted string (multi line replacements are not supported)
				{
					endPos = i;
					break;
				}
			}

			return lineRemainder[..endPos];
		}

		private static string GetNodeReplacementText(string content, int absolutePosition)
		{
			int length = content.IndexOfAny(new char[] { '\r', '\n' }, absolutePosition) - absolutePosition;
			string lineRemainder;
			if (length < -1)        //No newline, last line of document
			{
				lineRemainder = content[absolutePosition..];
			}
			else
			{
				lineRemainder = content.Substring(absolutePosition, length);
			}

			return lineRemainder;
		}

		//Holds the data that the user is typing to get auto complete by
		private class CompletionInput
		{
			public string InputText { get; set; }
			public int StartLine { get; set; }
			public int StartCharacter { get; set; }
			public bool IsValid { get; set; }
			public DocumentContext DocumentContext { get; set; }

			public CompletionInput(string inputText, DocumentContext docContext)
			{
				InputText = inputText;
				DocumentContext = docContext;
			}
		}

		private class NodeOrFieldCompletionInput : CompletionInput
		{
			public NodeOrFieldCompletionInput(string inputText, DocumentContext docContext) : base(inputText, docContext) { }
		}

		private class FieldValueCompletionInput : NodeOrFieldCompletionInput
		{
			public FieldValueCompletionInput(string inputText, DocumentContext docContext) : base(inputText, docContext) { }
		}
	}
}
