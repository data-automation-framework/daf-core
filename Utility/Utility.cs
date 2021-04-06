// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.IO;

namespace Daf.Core.Utility
{
	// Methods contained in this class can be called from within T4.
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "TODO fix this later.")]
	public static class Utility
	{
		/// <summary>
		/// Launches a debugger.
		/// </summary>
		/// <remarks>
		/// The T4 template needs to have debugging enabled in order for this to work.
		/// For example: <#@ template hostspecific="true" debug="true" #>
		/// </remarks>
		public static void LaunchDebugger()
		{
			System.Diagnostics.Debugger.Launch();
		}

		/// <summary>
		/// Injects the text contents of a file.
		/// </summary>
		/// <param name="filePath">The path to the file containing the text to inject</param>
		/// <returns>The text contents of the file</returns>
		public static string LoadScript(string filePath)
		{
			return File.ReadAllText(filePath);
		}

		/// <summary>
		/// Writes a line to the console during the build process.
		/// </summary>
		/// <param name="text">The text to print to console</param>
		public static void WriteLine(string text)
		{
			Console.WriteLine(text);
		}
	}
}
