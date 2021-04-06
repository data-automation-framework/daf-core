// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
namespace Daf.Core.LanguageServer.Services
{
	public class HoverRequest
	{
		public string? RootNodeName { get; set; }
		public bool RootNodeSet { get { return RootNodeName != null; } }
		public string? ParentNodeName { get; set; }
		public bool ParentNodeSet { get { return ParentNodeName != null; } }
		public string SearchText { get; set; }
		public int SearchTextLine { get; set; }
		public int SearchTextLineOffset { get; set; }

		public HoverRequest(string searchText)
		{
			SearchText = searchText;
		}

		public HoverRequest(HoverRequest fromRequest)
		{
			if (fromRequest == null)
				throw new ArgumentNullException(nameof(fromRequest));

			SearchText = fromRequest.SearchText;
		}
	}

	public class FieldValueHoverRequest : HoverRequest
	{
		public string FieldName { get; set; }

		public FieldValueHoverRequest(string searchText, string fieldName) : base(searchText)
		{
			FieldName = fieldName;
		}

		public FieldValueHoverRequest(HoverRequest hoverRequest, string fieldName) : base(hoverRequest)
		{
			RootNodeName = hoverRequest.RootNodeName;
			ParentNodeName = hoverRequest.ParentNodeName;
			FieldName = fieldName;
		}
	}
}
