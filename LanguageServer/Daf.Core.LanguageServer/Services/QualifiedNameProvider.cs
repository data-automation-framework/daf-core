// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
namespace Daf.Core.LanguageServer.Services
{
	public static class QualifiedNameProvider
	{
		public static QualifiedName GetQualifiedName(string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException(nameof(fullName));

			string? assemblyName = null;
			string? ns = null;                                                  //Namespace
			string name = "";
			string[] parts = fullName.Split('.');

			if (parts.Length > 1)
			{
				assemblyName = parts[0];
				if (parts.Length == 2)
				{
					name = parts[1];
				}
				if (parts.Length > 2)
				{
					ns = string.Join('.', parts, 1, parts.Length - 2);   //Get the middle list elements except the last which is the class name
					name = parts[^1];
				}
			}
			else
			{
				name = fullName;
			}

			return new QualifiedName(name)
			{
				AssemblyName = assemblyName,
				Namespace = ns
			};
		}

	}
}
