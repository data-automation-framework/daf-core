// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServer.Status
{
	public class PluginParsingStatus
	{
		public OperationStatus Status { get; set; }

		public string Message { get; set; }

		public PluginParsingStatus(OperationStatus status, string message)
		{
			Status = status;
			Message = message;
		}
	}
}
