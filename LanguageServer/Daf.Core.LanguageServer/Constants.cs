// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer
{
	internal static class Constants
	{
		internal const string IonDocumentFilterPattern = "**/*.daf";                //These files will be handled by the server
		internal const string DafDocumentFilterPattern = "**/*.ion";
		internal const string IntermediateDocumentFilterPattern = "**/intermediate.ion";
		internal const string MarkDownLanguage = "txt";                             //Controls style of text displayed on hover 
		internal const string DocumentationMissingMessage = "No documentation";     //Shown when no documentation is available in XSD
		internal const string DocumentLanguageId = "ion";                           //Matches the language that the client requests
		internal const string DirectiveOpeningTag = "<#@";
		internal static readonly char[] Whitespaces = { ' ', '\t', '\r', '\n' };
	}

	internal static class RegexConstants
	{
		//Regex capture group names
		internal const string FilePathGroupName = "filePath";

		//T4 include file pattern
		#region pattern explanation
		/* Pattern: <#@\s*include\s*file=\"(?<FilePathGroupName>.*)\"\s*#>
		 * 
		 * 1. <#@ matches the characters <#@ literally (case sensitive)
		 * 2. \s* matches any whitespace character (equal to [\r\n\t\f\v ])
		 *		* Quantifier — Matches between zero and unlimited times, as many times as possible, giving back as needed (greedy)
		 * 3. include matches the characters include literally (case sensitive)
		 * 4. \s* matches any whitespace character (equal to [\r\n\t\f\v ])
		 *		* Quantifier — Matches between zero and unlimited times, as many times as possible, giving back as needed (greedy)
		 * 5. file= matches the characters file= literally (case sensitive)
		 * 6. \" matches the character " literally (case sensitive)
		 * 7. Named Capture Group FilePathGroupName (?<FilePathGroupName>.*)
		 *		.* matches any character (except for line terminators)
		 *			* Quantifier — Matches between zero and unlimited times, as many times as possible, giving back as needed (greedy)
		 * 8. \" matches the character " literally (case sensitive)
		 * 9. \s* matches any whitespace character (equal to [\r\n\t\f\v ])
		 *		* Quantifier — Matches between zero and unlimited times, as many times as possible, giving back as needed (greedy)
		 *10. #> matches the characters #> literally (case sensitive)
		 */
		#endregion
		internal const string IncludeLinkPattern = @"<#@\s*include\s*file=\""(?<" + FilePathGroupName + @">.*)\""\s*#>";
	}
}
