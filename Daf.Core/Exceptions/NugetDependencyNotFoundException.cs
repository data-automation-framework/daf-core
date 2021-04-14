// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;

namespace Daf.Core.Exceptions
{
	public class NugetDependencyNotFoundException : Exception
	{
		public NugetDependencyNotFoundException()
		{
		}

		public NugetDependencyNotFoundException(string message) : base(message)
		{
		}

		public NugetDependencyNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}

	}
}
