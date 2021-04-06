// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer.Document
{
	public class DocumentContext
	{
		public PositionalContext Context { get; set; }

		public int ParentNodeStartPosition { get; set; }

		public int FieldNameStartPosition { get; set; }

		public bool NodeStartFound { get; set; }

		public bool FieldNameFound { get; set; }

		public bool IntermediaryNodes { get; set; }

		public DocumentContext()
		{
			Context = PositionalContext.Unknown;
			NodeStartFound = false;
			FieldNameFound = false;
			ParentNodeStartPosition = -1;
			FieldNameStartPosition = -1;
			IntermediaryNodes = false;
		}
	}
}
