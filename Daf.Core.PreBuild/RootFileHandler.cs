// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.IO;

namespace Daf.Core.PreBuild
{
	internal static class RootFileHandler
	{
		internal const string RootFileIndicator = "---";

		/// <summary>
		/// Opens a file and reads the first bunch of characters in order to check if it's a root file.
		/// </summary>
		/// <param name="filePath">The path of the file to check</param>
		/// <returns>True if it's a root file, otherwise false</returns>
		public static bool IsRootFile(string filePath)
		{
			char[] buffer = new char[RootFileIndicator.Length];

			using (StreamReader streamReader = new(filePath))
			{
				streamReader.ReadBlock(buffer, 0, RootFileIndicator.Length);
			}

			string fileStart = new(buffer);

			if (fileStart == RootFileIndicator)
				return true;

			return false;
		}
	}
}
