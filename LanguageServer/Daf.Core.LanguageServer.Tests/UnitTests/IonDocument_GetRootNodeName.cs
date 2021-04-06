// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServerTests.Utility;
using Xunit;

namespace Daf.Core.LanguageServerTests.UnitTests
{
	public class IonDocument_GetRootNodeName
	{
		[Theory]
		[InlineData("Package", "IntegrationTestPlugin")]
		[InlineData("FlatFileColumn", "IntegrationTestPlugin")]
		[InlineData("Database", "SsdtProjects")]
		[InlineData("Table", "SsdtProjects")]
		internal void CursorOn_NodeUnderRootNode_ReturnsRootNodeName(string nodeName, string expectedRootNodeName)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int nodePosition = DocumentUtility.GetAbsolutePositionOfNode(document, nodeName);

			string? rootNodeName = document.GetRootNodeName(nodePosition);

			Assert.NotNull(rootNodeName);
			Assert.Equal(expectedRootNodeName, rootNodeName!);
		}
	}
}
