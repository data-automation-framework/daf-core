// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Daf.Core.LanguageServer.Document;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Daf.Core.LanguageServerTests.Utility
{
	public static class DocumentUtility
	{
		internal static IonDocument GetRootIonDocument()
		{
			return new IonDocument(IonDocuments.SampleRootDocumentText, new Uri(Constants.DummyRootNodeFileUri));
		}
		internal static IonDocument GetRootIonDocument(string documentPath)
		{
			return new IonDocument(IonDocuments.SampleRootDocumentText, new Uri(documentPath));
		}
		internal static IonDocument GetNonRootIonDocument()
		{
			return new IonDocument(IonDocuments.SampleNonRootDocumentText, new Uri(Constants.DummyRootNodeFileUri));
		}

		internal static IonDocument GetMixedLineBreaksIonDocument()
		{
			return new IonDocument(IonDocuments.MixedLineBreaksDocumentText, new Uri(Constants.DummyRootNodeFileUri));
		}

		internal static Position GetPositionOfNode(IonDocument document, string nodeName)
		{
			DocumentMatch dm = MatchNode(document, nodeName);
			string start = document.Content.Substring(0, dm.Index);
			string[] lines = start.Split(Environment.NewLine);
			int lineNumber = lines.Length - 1;
			int character = lines[lineNumber].Length;

			return new Position(lineNumber, character + 1);
		}

		internal static Position GetPositionOfAttribute(IonDocument document, string nodeName, string attributeName)
		{
			DocumentMatch dm = MatchNode(document, nodeName);
			if (dm.Value.Contains(attributeName + "=", StringComparison.Ordinal))
			{
				string start = document.Content.Substring(0, dm.Index + dm.Value.IndexOf(attributeName + "=", StringComparison.Ordinal));
				string[] lines = start.Split(Environment.NewLine);
				int lineNumber = lines.Length - 1;
				int character = lines[lineNumber].Length + 1;

				return new Position(lineNumber, character);
			}
			else
				throw new ArgumentException($"Attribute {attributeName} could not be found for node {nodeName} in the provided IonDocument.");
		}

		internal static int GetAbsolutePositionAfterNode(IonDocument document, string nodeName)
		{
			DocumentMatch dm = MatchNode(document, nodeName);
			return dm.Index + dm.Value.Length + 1;
		}

		internal static int GetAbsolutePositionOfNode(IonDocument document, string nodeName)
		{
			DocumentMatch dm = MatchNode(document, nodeName);
			return dm.Index;
		}

		internal static int GetAbsolutePositionOfNodeAttribute(IonDocument document, string nodeName, string attributeName)
		{
			DocumentMatch dm = MatchNode(document, nodeName);
			if (dm.Value.Contains(attributeName + "=", StringComparison.Ordinal))
				return dm.Index + dm.Value.IndexOf(attributeName + "=", StringComparison.Ordinal);
			else
				throw new ArgumentException($"Attribute {attributeName} could not be found for node {nodeName} in the provided IonDocument.");
		}

		internal static int GetAbsolutePositionOfNodeAttributeValue(IonDocument document, string nodeName, string attributeName)
		{
			string attributeValueAssignment = "=";    //Assignment of attribute value
			int attributePosition = GetAbsolutePositionOfNodeAttribute(document, nodeName, attributeName);
			return attributePosition + attributeName.Length + attributeValueAssignment.Length;  //Start of attribute value
		}

		internal static int GetAbsolutePositionOfTextInIonComment(IonDocument document, string text)
		{
			MatchCollection mc = Regex.Matches(document.Content, "#.+"); //# starts comments
			foreach (Match m in mc)
			{
				if (m.Value.Contains(text, StringComparison.Ordinal))
					return m.Index + m.Value.IndexOf(text, StringComparison.Ordinal);
			}
			throw new ArgumentException($"Text {text} not found in any ion (#) comment in the provided IonDocument");
		}

		internal static int GetAbsolutePositionOfTextInCode(IonDocument document, string text)
		{
			foreach (Match m in Regex.Matches(document.Content, "<#.*?#>", RegexOptions.Singleline))   //Multiline C# code blocks
			{
				if (m.Value.Contains(text, StringComparison.Ordinal))
				{
					return m.Index + m.Value.IndexOf(text, StringComparison.Ordinal);
				}
			}
			throw new ArgumentException($"Text {text} not found in any C# code block in the provided IonDocument");
		}

		internal static int GetAbsolutePositionOfWord(IonDocument document, string word)
		{
			Match m = Regex.Match(document.Content, @"\b" + word + @"\b");
			if (m.Success)
				return m.Index;
			else
				throw new ArgumentException($"Word {word} not found in the provided IonDocument");
		}


		private static DocumentMatch MatchNode(IonDocument document, string nodeName)
		{
			string matchPattern = @"\t*" + nodeName + ":"; //Node name + starting tabs
			Match m = Regex.Match(document.Content, matchPattern);
			if (m.Success)
			{
				StringBuilder sb = new();
				int level = GetLineLevel(document.Content.Substring(m.Index));
				string[] lines = document.Content.Substring(m.Index).Split(Constants.NewLines, StringSplitOptions.RemoveEmptyEntries);
				sb.Append(lines[0]).Append(Environment.NewLine);
				for (int i = 1; i < lines.Length; i++)
				{
					string line = lines[i];
					int currentLineLevel = GetLineLevel(line);
					if (currentLineLevel < level)
						break;
					sb.Append(line).Append(Environment.NewLine);
				}

				string value = sb.ToString().TrimStart();   //Remove leading tabs
				int index = m.Index - 1 + level;            //Offset leading tabs
				DocumentMatch dm = new(index, value);

				return dm;
			}
			else
				throw new ArgumentException($"Node {nodeName} could not be found in the provided IonDocument.");
		}

		private static int GetLineLevel(string line)
		{
			int level = 0;
			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];
				if (c != '\t')
					break;
				level++;
			}
			return level;
		}
	}
}
