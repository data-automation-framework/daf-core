// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
namespace Daf.Core.LanguageServer.Services
{
	public class CompletionRequest
	{
		public string? RootNodeName { get; set; }
		public bool RootNodeSet { get { return RootNodeName != null; } }
		public string? ParentNodeName { get; set; }
		public int ParentNodeLevel { get; set; }
		public bool ParentNodeSet { get { return ParentNodeName != null; } }
		public string RequestInputText { get; set; }
		public int CurrentLevel { get; set; }

		public CompletionRequest(string requestInputText)
		{
			RequestInputText = requestInputText;
		}

		public CompletionRequest(CompletionRequest fromRequest)
		{
			if (fromRequest == null)
				throw new ArgumentNullException(nameof(fromRequest));

			RequestInputText = fromRequest.RequestInputText;
		}
	}

	public class FieldValueCompletionRequest : CompletionRequest
	{
		public string FieldName { get; set; }

		public FieldValueCompletionRequest(CompletionRequest completionRequest, string fieldName) : base(completionRequest)
		{
			RootNodeName = completionRequest.RootNodeName;
			ParentNodeName = completionRequest.ParentNodeName;
			RequestInputText = completionRequest.RequestInputText;
			ParentNodeLevel = completionRequest.ParentNodeLevel;
			CurrentLevel = completionRequest.CurrentLevel;
			FieldName = fieldName;
		}
	}
}
