// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer.Document
{
	//Context at a certain position within a document
	public enum PositionalContext
	{
		AtNodeName,
		AtFieldName,
		AtFieldValue,
		AtComment,
		AtT4Code,
		AtString,
		Unknown
	}
}
