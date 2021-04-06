// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer.Services
{
	//Holds completion data to be returned to the client
	public class CompletionResult
	{
		public string Label { get; set; }           //Text to display in auto complete list
		public string? Detail { get; set; }          //Brief information
		public string CompletionText { get; set; }  //Full text that will replace the search word
		public string Documentation { get; set; }   //Detailed documentation

		public CompletionResult(string label, string completionText, string documentation)
		{
			Label = label;
			CompletionText = completionText;
			Documentation = documentation;
		}

		public CompletionResult(string label, string completionText, string documentation, string detail)
		{
			Label = label;
			CompletionText = completionText;
			Documentation = documentation;
			Detail = detail;
		}
	}
}
