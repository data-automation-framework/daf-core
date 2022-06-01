// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer.Tests.Utility
{
	public class DocumentMatch
	{
		public int Index { get; set; }
		public string Value { get; set; }

		public DocumentMatch(int index, string value)
		{
			Index = index;
			Value = value;
		}
	}
}
