// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;

namespace Daf.Core.LanguageServer.Exceptions
{
	public class DocumentationParsingException : Exception
	{
		public DocumentationParsingException()
		{
		}

		public DocumentationParsingException(string message)
			: base(message)
		{
		}

		public DocumentationParsingException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
