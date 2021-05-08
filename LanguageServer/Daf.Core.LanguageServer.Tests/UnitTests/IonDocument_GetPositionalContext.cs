// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServerTests.Utility;
using Xunit;

namespace Daf.Core.LanguageServerTests.UnitTests
{
	public class IonDocument_GetPositionalContext
	{
		[Theory]
		[InlineData("Package")]
		[InlineData("FlatFileColumn")]
		[InlineData("SequenceContainer")]
		[InlineData("InputPath")]
		internal void CursorAt_NodeName_ReturnsAtNodeContext(string nodeName)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int nodePosition = DocumentUtility.GetAbsolutePositionOfNode(document, nodeName);
			int cursorPosition = nodePosition + nodeName.Length + 1;  //Move cursor one step forward to place it to the right of the ":" sign.

			DocumentContext dcActual = document.GetPositionalContext(cursorPosition);

			Assert.Equal(PositionalContext.AtNodeName, dcActual.Context);
		}

		[Theory]
		[InlineData("FlatFileConnection", "Name")]
		[InlineData("FlatFileColumn", "DataType")]
		[InlineData("SqlStatement", "Statement")]
		[InlineData("SequenceContainer", "Name")]
		internal void CursorAt_NodeAttribute_ReturnsAtAttributeContext(string nodeName, string attributeName)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int attributePosition = DocumentUtility.GetAbsolutePositionOfNodeAttribute(document, nodeName, attributeName);
			int cursorPosition = attributePosition + 1; //Move cursor one step forward to place it at the right of the first letter in the attribute.

			DocumentContext dcActual = document.GetPositionalContext(cursorPosition);

			Assert.Equal(PositionalContext.AtFieldName, dcActual.Context);
		}

		[Theory]
		[InlineData("int i = 0")]
		[InlineData("=i")]
		internal void CursorAt_Code_ReturnsAtCodeContext(string textInCode)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int codeTextPosition = DocumentUtility.GetAbsolutePositionOfTextInCode(document, textInCode);
			int cursorPosition = codeTextPosition + 1; //Move cursor one step forward to place it to the right of the first letter in the code text.

			DocumentContext dcActual = document.GetPositionalContext(cursorPosition);

			Assert.Equal(PositionalContext.AtT4Code, dcActual.Context);
		}

		[Theory]
		[InlineData("SqlParameter", "DataType", 3)]
		[InlineData("ExecuteSql", "Name", 3)]
		[InlineData("InputPath", "OutputPathName", 2)]
		[InlineData("Expression", "ExpressionValue", 7)]
		internal void CursorAt_AttributeValue_ReturnsAtAttributeValue(string nodeName, string attributeName, int offset)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int attributeValueStartPos = DocumentUtility.GetAbsolutePositionOfNodeAttributeValue(document, nodeName, attributeName);
			int cursorPosition = attributeValueStartPos + offset;

			DocumentContext dcActual = document.GetPositionalContext(cursorPosition);

			Assert.Equal(PositionalContext.AtFieldValue, dcActual.Context);
		}

		[Theory]
		[InlineData("__Comment1__")]
		[InlineData("__Comment2__")]
		[InlineData("__Comment3__")]
		internal void CursorAt_IonComment_ReturnsAtComment(string textInComment)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int commentTextPosition = DocumentUtility.GetAbsolutePositionOfTextInIonComment(document, textInComment);
			int cursorPosition = commentTextPosition + 1; //Move cursor one step forward to place it inside the Ion comment text

			DocumentContext dcActual = document.GetPositionalContext(cursorPosition);

			Assert.Equal(PositionalContext.AtComment, dcActual.Context);
		}

		[Fact]
		internal void CursorAt_NodeAtFirstLine_ReturnsAtNodename()
		{
			IonDocument document = new("RootNode:", new Uri(Constants.DummyRootNodeFileUri));
			int cursorPosition = 5; //At the root node name

			DocumentContext dcActual = document.GetPositionalContext(cursorPosition);

			Assert.Equal(PositionalContext.AtNodeName, dcActual.Context);
		}
	}
}
