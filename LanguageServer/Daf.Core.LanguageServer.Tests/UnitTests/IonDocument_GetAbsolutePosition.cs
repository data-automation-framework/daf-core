// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServerTests.Utility;
using Xunit;

namespace Daf.Core.LanguageServerTests.UnitTests
{
	public class IonDocument_GetAbsolutePosition
	{
		[Fact]
		internal void MixedLineBreakDocument_LineFeed_HandledAsSingleChar()
		{
			IonDocument document = DocumentUtility.GetMixedLineBreaksIonDocument();
			int line = 1;
			int character = 1;

			int absolutePosition = document.GetAbsolutePosition(line, character);

			Assert.Equal(2, absolutePosition);
		}

		[Fact]
		internal void MixedLineBreakDocument_CarriageReturn_HandledAsSingleChar()
		{
			IonDocument document = DocumentUtility.GetMixedLineBreaksIonDocument();
			int line = 2;
			int character = 1;

			int absolutePosition = document.GetAbsolutePosition(line, character);

			Assert.Equal(4, absolutePosition);
		}

		[Fact]
		internal void MixedLineBreakDocument_CarriageReturnLineFeed_HandledAsTwoChars()
		{
			IonDocument document = DocumentUtility.GetMixedLineBreaksIonDocument();
			int line = 3;
			int character = 1;

			int absolutePosition = document.GetAbsolutePosition(line, character);

			Assert.Equal(7, absolutePosition);
		}

		[Fact]
		internal void CursorAt_FirstLine_ReturnsCharacterPositionMinusOne()
		{
			IonDocument document = DocumentUtility.GetRootIonDocument();
			int line = 0;
			int character = 2;

			int absolutePosition = document.GetAbsolutePosition(line, character);

			Assert.Equal(1, absolutePosition);
		}
	}
}
