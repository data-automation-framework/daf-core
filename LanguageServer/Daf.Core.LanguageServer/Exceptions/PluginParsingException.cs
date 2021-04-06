// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;

namespace Daf.Core.LanguageServer.Exceptions
{
	public class PluginParsingException : Exception
	{
		public PluginParsingException()
		{
		}

		public PluginParsingException(string message)
			: base(message)
		{
		}

		public PluginParsingException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
