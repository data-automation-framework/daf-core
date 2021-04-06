// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Versioning;
using Daf.Core.Sdk;

namespace Daf.Core
{
	public static class ProjectDependencyHandler
	{
		public static List<ProjectDependency> GetProjectDependencies(string projectPath)
		{
			List<ProjectDependency> dependencies = new();

			// Add nuget references.
			List<string> frameworkPriorities = new()
			{
				"net6.0",
				"net5.0",
				"netcoreapp3.1",
				"netstandard2.2",
				"netcoreapp3.0",
				"netstandard2.1",
				"netcoreapp2.2",
				"netcoreapp2.1",
				"netcoreapp2.0",
				"netstandard2.0",
				"netcoreapp1.1",
				"netcoreapp1.0",
				"netstandard1.6",
				"netstandard1.5",
				"netstandard1.4",
				"netstandard1.3",
				"netstandard1.2",
				"netstandard1.1",
				"netstandard1.0"
			};

			string[] projectFileLines = File.ReadAllLines(projectPath);

			foreach (string line in projectFileLines)
			{
				if (line.Contains("PackageReference", StringComparison.Ordinal))
				{
					string packageName = FindStringBetweenStrings(line, "Include=\"", "\"");

					string libraryPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\.nuget\packages\{packageName.ToLower(CultureInfo.InvariantCulture)}";

					List<string> packageVersionFolders = Directory.GetDirectories(libraryPath, "*").Select(x => new DirectoryInfo(x).Name).ToList();
					List<NuGetVersion> nugetVersions = new();

					foreach (string packageVersionFolder in packageVersionFolders)
					{
						if (!NuGetVersion.TryParse(packageVersionFolder, out NuGetVersion nugetVersion))
							throw new InvalidOperationException($"Nuget package version {packageVersionFolder} for package {packageName} doesn't appear to be a valid semantic version.");
						else
							nugetVersions.Add(nugetVersion);
					}

					// Sort descending.
					nugetVersions.Sort((a, b) => b.CompareTo(a));

					string projectVersion = FindStringBetweenStrings(line, "Version=\"", "\"");

					// Attempt to figure out which version to match against, if wildcard matching is used.
					if (projectVersion.Contains('*', StringComparison.Ordinal))
					{
						foreach (NuGetVersion version in nugetVersions)
						{
							if (version.ToString().Contains(projectVersion.Replace("*", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal))
							{
								projectVersion = version.ToString();
								break;
							}
						}
					}

					// Figure out if this is a content-only library.
					bool contentOnlyLibrary = Directory.Exists($@"{libraryPath}\{projectVersion}\contentFiles") && !Directory.Exists($@"{libraryPath}\{projectVersion}\lib");

					if (contentOnlyLibrary)
					{
						// DafCoreNugets.Add(packageName, $@"{libraryPath}\{projectVersion}\contentFiles\any\any");
						ProjectDependency contentOnlyDep = new ContentOnlyProjectDependency(packageName, projectVersion, $@"{libraryPath}\{projectVersion}\contentFiles\any\any");

						dependencies.Add(contentOnlyDep);
					}

					if (!contentOnlyLibrary)
					{
						// Find all available framework targets.
						List<string> frameworksInLibrary = Directory.GetDirectories($@"{libraryPath}\{projectVersion}\lib", "net*").Select(x => new DirectoryInfo(x).Name).ToList();
						frameworksInLibrary.AddRange(Directory.GetDirectories($@"{libraryPath}\{projectVersion}\lib", "netcoreapp*").Select(x => new DirectoryInfo(x).Name).ToList());
						frameworksInLibrary.AddRange(Directory.GetDirectories($@"{libraryPath}\{projectVersion}\lib", "netstandard*").Select(x => new DirectoryInfo(x).Name).ToList());

						bool foundMatch = false;
						foreach (string priority in frameworkPriorities)
						{
							foreach (string foundFramework in frameworksInLibrary)
							{
								if (priority == foundFramework)
								{
									// Let's assume that there can only be one dll in a nuget library directory.
									string packageLocation = Directory.GetFiles($@"{libraryPath}\{projectVersion}\lib\{foundFramework}", "*.dll").ToList()[0];

									//Refs.Add(packageLocation);
									ProjectDependency refDependency = new ReferenceProjectDependency(packageName, projectVersion, foundFramework, packageLocation);
									dependencies.Add(refDependency);

									foundMatch = true;
									break;
								}
							}

							if (foundMatch)
								break;
						}
					}
				}
			}

			return dependencies;
		}

		public static List<IPlugin> ResolveConflictingPlugins(List<IPlugin> nugetPlugins, List<IPlugin> localPlugins, out List<string> warnings)
		{
			if (nugetPlugins == null)
				throw new ArgumentNullException(nameof(nugetPlugins));

			if (localPlugins == null)
				throw new ArgumentNullException(nameof(localPlugins));

			List<IPlugin> validPlugins = new(nugetPlugins);
			warnings = new();

			foreach (IPlugin localPlugin in localPlugins)
			{
				//Check if a nuget plugin already exist
				IPlugin? alreadyExists = nugetPlugins.Where(p => p.Name == localPlugin.Name).FirstOrDefault();

				if (alreadyExists == null)
					validPlugins.Add(localPlugin);
				else
					warnings.Add($"WARNING: Plugin {localPlugin.Name} has already been loaded from local nuget cache and will not be loaded. Version from nuget: {alreadyExists.Version}, local version {localPlugin.Version}.");
			}

			return validPlugins;
		}

		public static List<string> ResolveConflictingPlugins(List<string> nugetPluginPaths, List<string> localPluginPaths, out List<string> warnings)
		{
			if (nugetPluginPaths == null)
				throw new ArgumentNullException(nameof(nugetPluginPaths));

			if (localPluginPaths == null)
				throw new ArgumentNullException(nameof(localPluginPaths));

			List<string> paths = new(nugetPluginPaths); //All nuget paths are valid since they are prioritized over local plugins
			warnings = new();

			Dictionary<string, List<IPlugin>> localPluginPathPlugins = new(); //Local assembly path and the plugins loaded from it
			List<IPlugin> nugetPlugins = new();

			foreach (string localPluginPath in localPluginPaths)
			{
				localPluginPathPlugins.Add(localPluginPath, GetPlugins(localPluginPath));
			}

			foreach (string nugetPluginPath in nugetPluginPaths)
			{
				nugetPlugins.AddRange(GetPlugins(nugetPluginPath));
			}

			foreach (KeyValuePair<string, List<IPlugin>> local in localPluginPathPlugins)
			{
				List<IPlugin> localPlugins = local.Value;
				bool conflictFound = false;

				foreach (IPlugin localPlugin in localPlugins)
				{
					//Check if a nuget plugin already exist
					IPlugin? alreadyExists = nugetPlugins.Where(p => p.Name == localPlugin.Name).FirstOrDefault();

					if (alreadyExists != null)
					{
						conflictFound = true;
						warnings.Add($"WARNING: Plugin {localPlugin.Name} has already been loaded from local nuget cache and will not be loaded. Version from nuget: {alreadyExists.Version}, local version {localPlugin.Version}.");
						break;
					}
				}

				if (!conflictFound)
				{
					paths.Add(local.Key); //Add path to assembly as a valid path
				}
			}

			return paths;
		}

		public static bool IsPlugin(string assemblyPath)
		{
			Assembly asm = Assembly.LoadFrom(assemblyPath);

			//Check if the nuget dll is a plugin
			if (asm.GetTypes().Any(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
			{
				return true;
			}

			return false;
		}

		private static List<IPlugin> GetPlugins(string assemblyPath)
		{
			List<IPlugin> plugins = new();
			Assembly asm = Assembly.LoadFrom(assemblyPath);

			foreach (Type pluginType in asm.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
			{
				IPlugin plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
				plugins.Add(plugin);
			}

			return plugins;
		}

		private static string FindStringBetweenStrings(string text, string start, string end)
		{
			int p1 = text.IndexOf(start, StringComparison.Ordinal) + start.Length;
			int p2 = text.IndexOf(end, p1, StringComparison.Ordinal);

			if (string.IsNullOrEmpty(end))
				return text.Substring(p1);
			else
				return text.Substring(p1, p2 - p1);
		}
	}
}
