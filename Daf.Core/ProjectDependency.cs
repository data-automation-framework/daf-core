// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core
{
	public abstract class ProjectDependency
	{
		public string PackageName { get; init; }

		public string PackageVersion { get; init; }

		protected ProjectDependency(string packageName, string packageVersion)
		{
			PackageName = packageName;
			PackageVersion = packageVersion;
		}
	}

	public class ContentOnlyProjectDependency : ProjectDependency
	{
		public string ContentPath { get; init; }

		public ContentOnlyProjectDependency(string packageName, string packageVersion, string contentPath) : base(packageName, packageVersion)
		{
			ContentPath = contentPath;
		}
	}

	public class ReferenceProjectDependency : ProjectDependency
	{
		public string FrameworkVersion { get; init; }

		public string AssemblyLocation { get; init; }

		public ReferenceProjectDependency(string packageName, string packageVersion, string frameworkVersion, string assemblyLocation) : base(packageName, packageVersion)
		{
			FrameworkVersion = frameworkVersion;
			AssemblyLocation = assemblyLocation;
		}
	}
}
