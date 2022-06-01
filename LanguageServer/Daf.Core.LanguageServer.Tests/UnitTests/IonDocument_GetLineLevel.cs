// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServer.Tests.Utility;
using Xunit;

namespace Daf.Core.LanguageServer.Tests.UnitTests
{
	public class IonDocument_GetLineLevel
	{
		[Theory]
		[InlineData("Package")]
		[InlineData("FlatFileColumn")]
		[InlineData("SequenceContainer")]
		[InlineData("InputPath")]
		internal void GetLineLevel_OnLineWithNodeName_ReturnsStartingNumberOfTabs(string nodeName)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int nodePosition = DocumentUtility.GetAbsolutePositionOfNode(document, nodeName);
			int tabs = CountTabsToStartOfLine(document.Content, nodePosition);

			int lineLevel = document.GetLineLevel(nodePosition);

			Assert.Equal(tabs, lineLevel);
		}

		private static int CountTabsToStartOfLine(string documentContent, int startIndex)
		{
			int tabs = 0;
			for (int i = startIndex; i >= 0; i--)
			{
				char c = documentContent[i];
				if (c == '\t')
					tabs++;
				else if (c is '\n' or '\r')
					break;
			}
			return tabs;
		}
	}
}
