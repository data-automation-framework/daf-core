// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using McMaster.NETCore.Plugins;
using Daf.Core.Sdk;

namespace Daf.Core
{
	public static class PluginManager
	{
		public static void Execute()
		{
			Stopwatch timer = Stopwatch.StartNew();

			List<IPlugin> plugins = LoadPlugins();
			ExecutePlugins(plugins);

			timer.Stop();
			string filename = Path.GetFileName(Properties.Instance.FilePath)!;
			string duration = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
			Console.WriteLine($"Finished processing {filename} in {duration}");
		}

		private static void ExecutePlugins(List<IPlugin> plugins)
		{
			// TODO: Add some kind of priority/order system.
			// TODO: Add async support? Some plugins still need to run before others, for example Sql before Ssis.
			foreach (IPlugin plugin in plugins)
			{
				int exitCode = plugin.Execute();

				if (exitCode != 0)
				{
					Console.WriteLine($"{plugin.Name} returned an error code indicating failure.");
					Environment.Exit(exitCode);
				}
			}
		}

		private static List<IPlugin> LoadPlugins()
		{
			List<IPlugin> nugetPlugins = LoadPluginsFromNuget();
			List<IPlugin> localPlugins = LoadPluginsFromCustomProjectSystems();
			List<IPlugin> validPlugins = ProjectDependencyHandler.ResolveConflictingPlugins(nugetPlugins, localPlugins, out List<string> warnings);

			foreach (string warning in warnings)
				Console.WriteLine(warning);

			return validPlugins;
		}

		private static List<IPlugin> LoadPluginsFromNuget()
		{
			List<IPlugin> nugetPlugins = new();
			List<PluginLoader> loaders = new();

			if (Properties.Instance.ProjectDirectory != null)
			{
				string[] projects = Directory.GetFiles(Properties.Instance.ProjectDirectory, "*.dafproj");
				if (projects.Length == 1)
				{
					string projectFile = projects[0];
					List<ProjectDependency> dependencies = ProjectDependencyHandler.GetProjectDependencies(projectFile);

					// Add plugin loaders
					foreach (ReferenceProjectDependency dependency in dependencies.Where(d => d is ReferenceProjectDependency))
					{
						// Check if the assembly is a plugin and add to plugin loader
						if (ProjectDependencyHandler.IsPlugin(dependency.AssemblyLocation))
						{
							PluginConfig config = new(dependency.AssemblyLocation)
							{
								PreferSharedTypes = true
							};

							PluginLoader loader = new(config);

							//Resolve any additional nuget packages referenced by the dependency
							NugetDependencyResolver packageResolver = new(dependency);

							foreach (string assemblyPath in packageResolver.GetNugetDependencyAssemblyPaths())
								loader.LoadAssemblyFromPath(assemblyPath);

							loaders.Add(loader);
						}
					}

					if (loaders.Count > 0)
						Console.WriteLine($"Found plugins in nuget cache:");

					// Create an instance of plugin types.
					foreach (PluginLoader loader in loaders)
					{
						Assembly assembly = loader.LoadDefaultAssembly();

						foreach (Type pluginType in assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
						{
							// This assumes the implementation of IPlugin has a parameterless constructor.
							IPlugin plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
							nugetPlugins.Add(plugin);

							Console.WriteLine($"  * {plugin.Name} (version {plugin.Version}, timestamp {plugin.TimeStamp}): {plugin.Description}");
						}
					}
				}
				else
				{
					throw new InvalidOperationException($"Folder {Properties.Instance.ProjectDirectory} contains more or less than 1 project file. Only 1 project file is supported.");
				}
			}
			else
			{
				throw new InvalidOperationException($"{nameof(Properties.Instance.ProjectDirectory)} is null! Provide a valid project directory.");
			}

			return nugetPlugins;
		}

		private static List<IPlugin> LoadPluginsFromCustomProjectSystems()
		{
			List<PluginLoader> loaders = new();

			// Create plugin loaders.
			string pluginsDir = Path.Combine(AppContext.BaseDirectory, "Plugins");

			if (Directory.Exists(pluginsDir))
			{
				// Ensure that we only attempt to load library files with the same file name as the plugin folder they're in.
				foreach (string directory in Directory.GetDirectories(pluginsDir))
				{
					string directoryName = Path.GetFileName(directory);
					string pluginDll = Path.Combine(directory, directoryName + ".dll");

					if (File.Exists(pluginDll))
					{
						PluginConfig config = new(pluginDll)
						{
							PreferSharedTypes = true
						};

						PluginLoader loader = new(config); // This throws an exception if the plugin folder name and the plugin dll name differ in casing.
						loader.LoadAssemblyFromPath(pluginDll);

						loaders.Add(loader);
					}
				}
			}

			if (loaders.Count != 0)
				Console.WriteLine($"Found local plugins in {pluginsDir}:");

			List<IPlugin> plugins = new();

			// Create an instance of plugin types.
			foreach (PluginLoader loader in loaders)
			{
				int count = 0;

				Assembly assembly = loader.LoadDefaultAssembly();

				foreach (Type pluginType in assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
				{
					// This assumes the implementation of IPlugin has a parameterless constructor.
					IPlugin plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
					plugins.Add(plugin);

					count++;
				}

				if (count == 0)
				{
					throw new InvalidOperationException($"Can't find any type which implements IPlugin in {assembly} from {assembly.Location}.\n" +
						$"Available types: {string.Join(",", assembly.GetTypes().Select(t => t.FullName))}");
				}
			}

			foreach (IPlugin plugin in plugins)
				Console.WriteLine($"  * {plugin.Name} (version {plugin.Version}, timestamp {plugin.TimeStamp}): {plugin.Description}");

			return plugins;
		}
	}
}
