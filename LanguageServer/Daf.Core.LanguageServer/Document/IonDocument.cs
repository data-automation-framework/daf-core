// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Daf.Core.LanguageServer.Document
{
	//Represents a .daf/.ion file opened by the user.
	public class IonDocument
	{

		//The content of the file as a string
		public string Content { get; set; }
		public List<Location> Links { get; } = new();
		public Uri DocumentUri { get; set; }

		private readonly bool isRootFile;   //True if the file starts with three dashes (---) which indicates a root file

		private const string RootFileIndicator = "---";

		public IonDocument(string content, Uri documentUri)
		{
			Content = content ?? throw new ArgumentNullException(nameof(content));
			DocumentUri = documentUri;

			if (content.StartsWith(RootFileIndicator, StringComparison.OrdinalIgnoreCase))
			{
				isRootFile = true;
			}
			else
			{
				isRootFile = false;
			}

			Links.AddRange(GenerateLinks());
		}

		//Translates line and character position sent to the server to an absolute position within the Content string
		public int GetAbsolutePosition(int line, int character)
		{
			int absolutePosition = 0;
			int currentLine = 0;
			char currentChar;

			//Handle edge case when caret is on the very first line of the document
			if (line == 0)
			{
				if (character == 0)
					return 0;
				else
					return character - 1;
			}

			//Advance position until current line is reached, handles various line breaks
			for (; absolutePosition < Content.Length; absolutePosition++)
			{
				if (absolutePosition < Content.Length - 1)
				{
					currentChar = Content[absolutePosition];
					if (currentChar == '\r' && Content[absolutePosition + 1] == '\n')       //Windows line break
					{
						currentLine++;
						absolutePosition++;                                                 //Advance additional step
					}
					else if (currentChar is '\r' or '\n')                    //Mac/UNIX line break
					{
						currentLine++;
					}
				}
				if (currentLine == line)
				{
					break;
				}
			}

			absolutePosition += character;

			return absolutePosition;
		}

		//Determines the context of the absolutePosition within Content
		public DocumentContext GetPositionalContext(int absolutePosition)
		{
			int startLevel = GetLineLevel(absolutePosition);
			DocumentContext context = new()
			{
				ParentNodeStartPosition = GetParentNodePosition(absolutePosition, startLevel, true)
			};

			int startPosition;

			if (context.ParentNodeStartPosition != -1)
			{
				context.NodeStartFound = true;
				startPosition = context.ParentNodeStartPosition;
			}
			else
			{
				startPosition = GetStartOfLinePosition(absolutePosition);
			}


			bool inQuotedString = false;
			bool inT4Code = false;
			bool inComment = false;
			bool searchFieldName = false;
			bool nodeNameParsed = false;
			context.Context = PositionalContext.AtNodeName;

			//Parse from the node start up to caret position
			for (int i = startPosition; i <= absolutePosition; i++)
			{
				char c = Content[i];
				if (c == '"' && !inQuotedString)
					inQuotedString = true;
				else if (c == '"' && inQuotedString)
					inQuotedString = false;

				if (c == '<' && i + 1 < Content.Length - 1 && Content[i + 1] == '#')
					inT4Code = true;
				else if (c == '>' && i - 1 > 0 && Content[i - 1] == '#')
					inT4Code = false;

				if (c == '#' && !inQuotedString && !inT4Code && context.Context != PositionalContext.AtFieldValue)
					inComment = true;
				else if (inComment && (c == '\n' || c == '\r'))
					inComment = false;

				if (!inQuotedString && !inT4Code && !inComment)
				{
					if (c == ':' && !nodeNameParsed && i + 1 < absolutePosition)    //Reached past the node name
					{
						nodeNameParsed = true;
						searchFieldName = true;
						context.Context = PositionalContext.AtFieldName;
					}
					else if (c == ':' && nodeNameParsed)
					{
						context.IntermediaryNodes = true;
					}
					else if (c == '=')
					{
						context.Context = PositionalContext.AtFieldValue;
					}
					else if (Char.IsWhiteSpace(c) && context.Context == PositionalContext.AtFieldValue)
					{
						searchFieldName = true;
						context.Context = PositionalContext.AtFieldName;
					}
					else if (!Char.IsWhiteSpace(c) && searchFieldName)
					{
						searchFieldName = false;
						context.FieldNameStartPosition = i;
					}
				}
			}

			//Assume that caret is in field value if it is placed within a quoted string outside of a T4 code block
			if (inQuotedString && !inT4Code && !inComment)
				context.Context = PositionalContext.AtFieldValue;
			else if (inT4Code)
				context.Context = PositionalContext.AtT4Code;
			else if (inComment)
				context.Context = PositionalContext.AtComment;

			if (context.FieldNameStartPosition != -1)
				context.FieldNameFound = true;

			return context;
		}

		public string GetStartOfWord(int absolutePosition)
		{
			return GetStartOfWord(absolutePosition, out _);
		}

		//Gets the start of the word (alphanumeric and full stop) at absolutePosition within Content
		public string GetStartOfWord(int absolutePosition, out int lineOffset)
		{
			int pos = absolutePosition;
			int wordStartIndex = 0;
			char currentChar;

			//Search backwards to get start of word
			while (pos >= 0)
			{
				currentChar = Content[pos];
				if (!char.IsLetterOrDigit(currentChar) && currentChar != '.')
				{
					if (pos == absolutePosition)
						wordStartIndex = pos;
					else
						wordStartIndex = pos + 1;
					break;
				}
				pos--;
			}

			//Find start of line
			while (pos >= 0)
			{
				currentChar = Content[pos];
				if (pos == 0)
				{
					break;
				}
				else if (currentChar is '\n' or '\r')
				{
					pos++; //Move back to first character in line
					break;
				}
				pos--;
			}

			int wordLength = absolutePosition + 1 - wordStartIndex;
			string word = Content.Substring(wordStartIndex, wordLength);
			lineOffset = wordStartIndex - pos;

			return word;
		}

		public string GetWord(int absolutePosition)
		{
			return GetWord(absolutePosition, out _);
		}

		//Gets the word (alphanumeric) at absolutePosition within Content
		public string GetWord(int absolutePosition, out int lineOffset)
		{
			bool boundaryFound = false;
			int pos = absolutePosition;
			int wordEndIndex = 0;
			char currentChar;

			if (absolutePosition == Content.Length)
			{
				pos = absolutePosition - 1;
			}

			string startOfWord = GetStartOfWord(absolutePosition, out lineOffset);

			//Search forwards to get end of word
			while (!boundaryFound && pos < Content.Length)
			{
				currentChar = Content[pos];
				if (!char.IsLetterOrDigit(currentChar) && currentChar != '.')
				{
					boundaryFound = true;
					if (pos == absolutePosition)
						wordEndIndex = absolutePosition;
					else
						wordEndIndex = pos - 1;
				}
				pos++;
			}

			if (!boundaryFound)
			{
				wordEndIndex = absolutePosition;
			}

			int endWordLength = wordEndIndex - absolutePosition;
			string endOfWord = Content.Substring(absolutePosition + 1, endWordLength);

			return startOfWord + endOfWord;
		}

		//Generate links to files being referenced in the document
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0220:Add explicit cast", Justification = "<Pending>")]
		private List<Location> GenerateLinks()
		{
			List<Location> locs = new();

			//Match all file include tags e.g. <#@ include file="file.txt" #>
			//file.txt will be captured to group RegexConstants.FilePathGroupName
			MatchCollection includeTags = Regex.Matches(Content, RegexConstants.IncludeLinkPattern);

			string[] rows = Content.Split(Environment.NewLine);

			foreach (Match includeTag in includeTags)
			{
				//Match group contains the file path to link to
				int linkTextAbsoluteIndex = includeTag.Groups[RegexConstants.FilePathGroupName].Index;
				int linkTextLength = includeTag.Groups[RegexConstants.FilePathGroupName].Length;
				string linkedFileName = includeTag.Groups[RegexConstants.FilePathGroupName].Value;

				//Find the line in the document that the file path is on
				int line = Content.Substring(0, linkTextAbsoluteIndex).Split(Environment.NewLine).Length - 1; //-1 because first line is line 0 etc

				//Get which character in the line that the file path text starts at
				int startCharacter;
				if (rows[line].Contains(Constants.DirectiveOpeningTag, StringComparison.Ordinal))
				{
					//If the line contains the opening directive tag <#@ there is a chance that the file name also 
					//exist before that tag. It is the file name after the tag that is relevant for linking.
					//Example: File: Path="file.txt" <#@ include file="file.txt" #>
					int directiveOpeningIndex = rows[line].IndexOf(Constants.DirectiveOpeningTag, StringComparison.Ordinal);
					startCharacter = rows[line].IndexOf(linkedFileName, directiveOpeningIndex, StringComparison.Ordinal);
				}
				else
				{
					startCharacter = rows[line].IndexOf(linkedFileName, StringComparison.Ordinal);
				}

				int endCharacter = startCharacter + linkTextLength;

				Position positionStart = new(line, startCharacter);
				Position positionEnd = new(line, endCharacter);

				Location l = new()
				{
					Range = new LspRange(positionStart, positionEnd),
					Uri = new Uri(DocumentUri, linkedFileName)  //Relative path
				};

				locs.Add(l);
			}

			return locs;
		}

		internal void RegenerateLinks()
		{
			Links.Clear();
			Links.AddRange(GenerateLinks());
		}

		public string? GetRootNodeName(int absolutePosition)
		{
			bool rootNodeFound = false;
			bool readingRootNode = false;
			bool inQuote = false;
			string rootNodeName = "";
			StringBuilder sb = new();

			//Files that aren't root node files also contains nodes at first file column, these are not real root nodes however
			if (!isRootFile)
				return null;

			//TODO: Handle multi line strings

			//Check if the root node is on this line
			int startOfLine = GetStartOfLinePosition(absolutePosition);
			for (int i = startOfLine; i < Content.Length; i++)
			{
				char c = Content[i];
				sb.Append(c);
				if (c == ':')
				{
					rootNodeFound = true;
					rootNodeName = sb.ToString().TrimEnd(':');
				}
				if (Char.IsWhiteSpace(c))
					break;
			}

			//Otherwise scan backwards for the root node
			if (!rootNodeFound)
			{
				sb.Clear();
				int initialLineLevel = GetLineLevel(absolutePosition);

				for (int i = absolutePosition; i >= 0; i--)
				{
					char c = Content[i];
					sb.Append(c);
					if (c == '"')
					{
						inQuote = !inQuote; //Flip flag to indicate that the position has entered or left a quoted string
					}
					else if (c == ':' && !inQuote)
					{
						sb.Clear();
						readingRootNode = true;
					}
					else if (c == '\t' || (c == ' ' && !inQuote))
					{
						sb.Clear();
						readingRootNode = false;
					}
					else if ((c == '\n' || c == '\r' || i == 0) && readingRootNode && !inQuote)
					{
						rootNodeFound = true;
						rootNodeName = new string(sb.ToString().Reverse().ToArray());
						break;
					}
					else if ((c == '\n' || c == '\r' || i == 0) && !readingRootNode && initialLineLevel == 0 && !inQuote)
					{
						break;
					}
				}
			}

			if (!rootNodeFound)
				return null;

			return rootNodeName.Trim();
		}

		public int GetLineLevel(int absolutePosition)
		{
			int levelCount = 0;
			if (absolutePosition - 1 >= 0)
			{
				int startOfLine = GetStartOfLinePosition(absolutePosition - 1);
				for (int i = startOfLine; i < Content.Length; i++)
				{
					if (Content[i] == '\t')
						levelCount++;
					else
						break;
				}
			}
			return levelCount;
		}

		private int GetParentNodePosition(int absolutePosition, int startLevel, bool onInitialLine)
		{
			int nodeStartPosition = -1;
			int startOfLine = absolutePosition;
			int currentLineLevel = GetLineLevel(absolutePosition);
			bool nodeFound = false;
			bool readingNode = false;

			//Check if node exists on the current line
			{
				//Scan backwards to start of line
				for (int i = absolutePosition; i >= 0; i--)
				{
					char c = Content[i];
					if (c == ':' && (currentLineLevel < startLevel || onInitialLine)) //The parent node must have lower level (fewer starting tabs), unless we are currently on the initial line before recursion starts
						readingNode = true;
					else if (readingNode && (Char.IsWhiteSpace(c) || i == 0))
					{
						nodeFound = true;
						readingNode = false;
						if (Char.IsWhiteSpace(c))
							nodeStartPosition = i + 1;
						else if (i == 0)
							nodeStartPosition = i;                  //Do not offset by 1 if at the start of document
					}
					else if (nodeFound && !Char.IsWhiteSpace(c))    //Node was found but the line did not start with all whitespace
					{
						nodeFound = false;
					}
					else if (c == '\n' || c == '\r' || i == 0)      //Start of line found
					{
						startOfLine = i == 0 ? 0 : i + 1;           //Do not offset by 1 if at the start of document
						break;
					}
				}
			}

			//Search again on the line above if node position still isn't found
			if (!nodeFound && startOfLine - 1 > 0)   //Not reached start of document
			{
				//Back until non newline found
				int pos;
				for (pos = startOfLine - 1; pos >= 0; pos--)
				{
					if (Content[pos] is not '\r' and not '\n')
					{
						break;
					}
				}
				nodeStartPosition = GetParentNodePosition(pos, startLevel, false);    //Recursive
			}

			return nodeStartPosition;
		}

		private int GetStartOfLinePosition(int absolutePosition)
		{
			int pos = 0;
			for (int i = absolutePosition; i >= 0; i--)
			{
				char c = Content[i];
				if (c is '\n' or '\r')
				{
					pos = i + 1;
					break;
				}
			}
			return pos;
		}
	}
}
