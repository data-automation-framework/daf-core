// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Daf.Core
{
	internal class NugetDependencyResolver
	{
		private readonly ReferenceProjectDependency _dependency;

		internal NugetDependencyResolver(ReferenceProjectDependency dependency)
		{
			_dependency = dependency;
		}

		/// <summary>
		/// Get all assemblies needed to load the dependency
		/// </summary>
		/// <returns>List with full paths to all assemblies that the dependency relies on</returns>
		internal List<string> GetNugetDependencyAssemblyPaths()
		{
			PackageIdentity package = new(_dependency.PackageName, NuGetVersion.Parse(_dependency.PackageVersion));
			ISettings settings = Settings.LoadDefaultSettings(null);
			string localCacheFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

			//The framework of the original dependency which will be used to get closest matching framework for it's own dependencies
			NuGetFramework framework = NuGetFramework.ParseFolder(_dependency.FrameworkVersion);

			HashSet<PackageAssemblies> dependencies = new(PackageIdentityComparer.Default);

			VersionFolderPathResolver vfpr = new(localCacheFolder); //Folder reader for the NugetV3 folder format

			PackageAssemblies depPackage = new(package);

			//Populate dependency list recursively and set assembly paths
			PopulateDependencies(depPackage, framework, vfpr, dependencies);

			List<string> assemblyPaths = new();

			foreach (PackageAssemblies dependency in dependencies)
			{
				assemblyPaths.AddRange(dependency.AssemblyPaths);
			}

			return assemblyPaths.ToList();
		}

		private void PopulateDependencies(PackageAssemblies package, NuGetFramework packageFramework, VersionFolderPathResolver vfpr, ISet<PackageAssemblies> availableDependencies)
		{
			//Avoid loading dependencies used by the core project to avoid conflicts
			if (availableDependencies.Contains(package) || package.Id == "Daf.Core.Sdk") //TODO: This should be handled in a better way
				return;

			//Get the assembly dll for the current package
			SetAssemblyPaths(package, packageFramework, vfpr);
			availableDependencies.Add(package);

			//Find where the packageh is installed and resolve its dependencies
			string installedPath = vfpr.GetInstallPath(package.Id, package.Version);

			if (installedPath != null)
			{
				using (PackageReaderBase packageReader = new PackageFolderReader(installedPath))
				{
					NuspecReader nuspec = new(packageReader.GetNuspecFile());
					FrameworkReducer frameworkReducer = new();

					//Get nearest matching framework based of the parent package to get dependencies from
					NuGetFramework nearestFramework = frameworkReducer.GetNearest(packageFramework, nuspec.GetDependencyGroups().Select(dg => dg.TargetFramework));

					//Dependencies in best match of target framework
					PackageDependencyGroup depGroup = nuspec.GetDependencyGroups().First(dg => dg.TargetFramework == nearestFramework);

					foreach (PackageDependency dep in depGroup.Packages)
					{
						PackageIdentity pi;
						if (dep.VersionRange.MaxVersion == null)
						{
							string versionsDir = vfpr.GetVersionListPath(dep.Id);
							List<NuGetVersion> versions = new();

							try
							{
								foreach (string verDir in Directory.GetDirectories(versionsDir))
								{
									string versionLiteral = new DirectoryInfo(verDir).Name;
									versions.Add(new NuGetVersion(versionLiteral));
								}
							}
							catch (DirectoryNotFoundException ex)
							{
								string exceptionMessage = $"Failed to find directories for {nameof(versionsDir)}, {package.Id}!";
								Console.WriteLine(exceptionMessage);

								throw new DirectoryNotFoundException(exceptionMessage, ex);
							}

							versions = versions.OrderByDescending(v => v).ToList();
							NuGetVersion maxVersion = versions.First();
							pi = new(dep.Id, maxVersion);
						}
						else
						{
							pi = new(dep.Id, dep.VersionRange.MaxVersion);
						}

						PackageAssemblies depPackage = new(pi);

						try
						{
							PopulateDependencies(depPackage, nearestFramework, vfpr, availableDependencies);
						}
						catch (DirectoryNotFoundException ex)
						{
							string exceptionMessage = $"Failed to load dependency for {package.Id}!";

							throw new DirectoryNotFoundException(exceptionMessage, ex);
						}
					}
				}
			}
		}

		private static void SetAssemblyPaths(PackageAssemblies package, NuGetFramework framework, VersionFolderPathResolver vfpr)
		{
			List<string> paths = new();

			string installedPath = vfpr.GetInstallPath(package.Id, package.Version);

			if (installedPath != null)
			{
				using (PackageReaderBase packageReader = new PackageFolderReader(installedPath))
				{
					NuspecReader nuspec = new(packageReader.GetNuspecFile());
					FrameworkReducer frameworkReducer = new();
					NuGetFramework nearestFramework = frameworkReducer.GetNearest(framework, nuspec.GetDependencyGroups().Select(dg => dg.TargetFramework));

					FrameworkSpecificGroup libItems = packageReader.GetLibItems().First(x => x.TargetFramework == nearestFramework);

					foreach (string assemblyRelativePath in libItems.Items)
					{
						if (Path.GetExtension(assemblyRelativePath) == ".dll") //Only add the dll files, skip any bundled documentation xml or similar
							paths.Add(Path.Combine(installedPath, assemblyRelativePath));
					}
				}
			}

			package.AssemblyPaths.AddRange(paths);
		}

		private class PackageAssemblies : PackageIdentity
		{
			public List<string> AssemblyPaths { get; } = new();

			public PackageAssemblies(PackageIdentity package) : base(package.Id, package.Version)
			{
			}
		}
	}
}
