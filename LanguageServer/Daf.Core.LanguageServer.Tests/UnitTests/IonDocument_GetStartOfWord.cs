// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServerTests.Utility;
using Xunit;

namespace Daf.Core.LanguageServerTests.UnitTests
{
	public class IonDocument_GetStartOfWord
	{
		[Theory]
		[InlineData("Connections", 4, "Conne")]
		[InlineData("Unicode", 1, "Un")]
		[InlineData("Statement", 2, "Sta")]
		[InlineData("VariableName", 3, "Vari")]
		internal void CursorAt_WordOffset_ReturnsStartOfWord(string word, int cursorOffset, string expectedStartOfWord)
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int wordAbsolutePosition = DocumentUtility.GetAbsolutePositionOfWord(document, word);
			int docIndex = wordAbsolutePosition + cursorOffset;

			string actualStartOfWord = document.GetStartOfWord(docIndex);

			Assert.Equal(expectedStartOfWord, actualStartOfWord);
		}
	}
}
