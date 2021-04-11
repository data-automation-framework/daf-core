// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.Exceptions;
using Daf.Core.LanguageServer.Exceptions;
using Daf.Core.LanguageServer.Services;
using Daf.Core.LanguageServer.Status;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Daf.Core.LanguageServer.Model
{
	public class LanguageServerPluginManager
	{

		public Dictionary<string, PluginAssembly> Assemblies { get; } = new();   //Key = Root node name

		private const bool UseExactMatch = true;

		public List<PluginParsingStatus> PluginParsingStatuses { get; } = new();

		public List<IonNode> GetNodesByRootNode(QualifiedName rootNodeName, QualifiedName parentNodeName, QualifiedName searchNodeName, bool exactMatch)
		{
			if (rootNodeName == null)
				throw new ArgumentNullException(nameof(rootNodeName));

			if (parentNodeName == null)
				throw new ArgumentNullException(nameof(parentNodeName));

			if (searchNodeName == null)
				throw new ArgumentNullException(nameof(searchNodeName));

			List<IonNode> nodes = new();

			if (TryGetAssembly(rootNodeName, out PluginAssembly? assembly))
			{
				nodes.AddRange(assembly!.GetNodes(parentNodeName, searchNodeName, exactMatch));
			}

			return EliminateDuplicates(nodes);
		}

		public List<IonNode> GetNodesByRootNode(QualifiedName rootNodeName, QualifiedName searchNodeName, bool exactMatch)
		{
			List<IonNode> nodes = new();

			if (TryGetAssembly(rootNodeName, out PluginAssembly? assembly))
			{
				nodes.AddRange(assembly!.GetNodes(searchNodeName, exactMatch));
			}

			return EliminateDuplicates(nodes);
		}

		public List<IonNode> GetNodes(QualifiedName searchNodeName, bool exactMatch)
		{
			if (searchNodeName == null)
				throw new ArgumentNullException(nameof(searchNodeName));

			List<IonNode> nodes = new();

			if (searchNodeName.AssemblyNameSet)
			{
				if (TryGetAssemblyByAssemblyName(searchNodeName.AssemblyName!, out PluginAssembly? assembly))
				{
					nodes.AddRange(assembly!.GetNodes(searchNodeName, exactMatch));
				}
			}
			else
			{
				foreach (KeyValuePair<string, PluginAssembly> assemblyEntry in Assemblies)
				{
					nodes.AddRange(assemblyEntry.Value.GetNodes(searchNodeName, exactMatch));
				}
			}

			return EliminateDuplicates(nodes);
		}

		public List<IonNode> GetNodes(QualifiedName parentNodeName, QualifiedName searchNodeName, bool exactMatch)
		{
			if (parentNodeName == null)
				throw new ArgumentNullException(nameof(parentNodeName));

			if (searchNodeName == null)
				throw new ArgumentNullException(nameof(searchNodeName));

			List<IonNode> nodes = new();

			if (parentNodeName.AssemblyNameSet)
			{
				if (TryGetAssemblyByAssemblyName(parentNodeName.AssemblyName!, out PluginAssembly? pa_parent))
				{
					nodes.AddRange(pa_parent!.GetNodes(parentNodeName, searchNodeName, exactMatch));
				}
			}
			else if (searchNodeName.AssemblyNameSet)
			{
				if (TryGetAssemblyByAssemblyName(searchNodeName.AssemblyName!, out PluginAssembly? pa_node))
				{
					nodes.AddRange(pa_node!.GetNodes(searchNodeName, exactMatch));
				}
			}
			else
			{
				foreach (KeyValuePair<string, PluginAssembly> assemblyEntry in Assemblies)
				{
					nodes.AddRange(assemblyEntry.Value.GetNodes(parentNodeName, searchNodeName, exactMatch));
				}
			}

			return EliminateDuplicates(nodes);
		}

		public List<IonNode> GetAllNodes()
		{
			List<IonNode> nodes = new();

			foreach (KeyValuePair<string, PluginAssembly> assemblyEntry in Assemblies)
			{
				nodes.AddRange(assemblyEntry.Value.GetAllNodes());
			}

			return nodes;
		}

		public List<IonDocumentation> GetDocumentation(QualifiedName rootNode, QualifiedName parentNodeName, QualifiedName searchWordName)
		{
			if (parentNodeName == null)
				throw new ArgumentNullException(nameof(parentNodeName));

			if (searchWordName == null)
				throw new ArgumentNullException(nameof(searchWordName));

			List<IonDocumentation> docs = new();

			//Get the assembly by root node name
			if (TryGetAssembly(rootNode, out PluginAssembly? assembly))
			{
				docs.AddRange(GetDocumentation(assembly!, parentNodeName, searchWordName));
			}


			return EliminateDuplicates(docs);
		}

		public List<IonDocumentation> GetDocumentation(QualifiedName parentNodeName, QualifiedName searchWord)
		{
			if (parentNodeName == null)
				throw new ArgumentNullException(nameof(parentNodeName));

			if (searchWord == null)
				throw new ArgumentNullException(nameof(searchWord));

			List<IonDocumentation> docs = new();

			//User has specified a namespace
			if (parentNodeName.AssemblyNameSet || searchWord.AssemblyNameSet)
			{
				if (searchWord.AssemblyNameSet && TryGetAssemblyByAssemblyName(searchWord.AssemblyName!, out PluginAssembly? assembly))
				{
					docs.AddRange(GetDocumentation(assembly!, parentNodeName, searchWord));
				}
				else if (parentNodeName.AssemblyNameSet && TryGetAssemblyByAssemblyName(parentNodeName.AssemblyName!, out PluginAssembly? parentAssembly))
				{
					docs.AddRange(GetDocumentation(parentAssembly!, parentNodeName, searchWord));
				}
			}
			//User has not specified a namespace
			else
			{
				foreach (KeyValuePair<string, PluginAssembly> assemblyEntry in Assemblies)
				{
					docs.AddRange(GetDocumentation(assemblyEntry.Value, parentNodeName, searchWord));
				}
			}

			return EliminateDuplicates(docs);
		}

		public List<IonDocumentation> GetDocumentation(QualifiedName searchNodeName)
		{
			if (searchNodeName == null)
				throw new ArgumentNullException(nameof(searchNodeName));

			List<IonDocumentation> docs = new();

			if (searchNodeName.AssemblyNameSet)
			{
				if (searchNodeName.AssemblyNameSet && TryGetAssemblyByAssemblyName(searchNodeName.AssemblyName!, out PluginAssembly? assembly))
				{
					docs.AddRange(GetDocumentation(assembly!, searchNodeName));
				}
			}
			//User has not specified a namespace
			else
			{
				foreach (KeyValuePair<string, PluginAssembly> assemblyEntry in Assemblies)
				{
					docs.AddRange(GetDocumentation(assemblyEntry.Value, searchNodeName));
				}
			}

			return EliminateDuplicates(docs);
		}

		private static List<IonDocumentation> GetDocumentation(PluginAssembly assembly, QualifiedName parentNodeName, QualifiedName searchWordName)
		{
			List<IonDocumentation> docs = new();
			//Try to get field
			List<IonField> fields = assembly.GetFields(parentNodeName, searchWordName.Name, UseExactMatch);
			foreach (IonField field in fields)
			{
				if (field.IsValid)
					docs.Add(field.Documentation);
			}

			List<IonNode> nodes = assembly.GetNodes(parentNodeName, searchWordName, UseExactMatch);

			foreach (IonNode node in nodes)
			{
				if (node.IsValid)
					docs.Add(node.Documentation);
			}

			return EliminateDuplicates(docs);
		}

		private static List<IonDocumentation> GetDocumentation(PluginAssembly assembly, QualifiedName searchWordName)
		{
			List<IonDocumentation> docs = new();

			List<IonNode> nodes = assembly.GetNodes(searchWordName, UseExactMatch);

			foreach (IonNode node in nodes)
			{
				if (node.IsValid)
					docs.Add(node.Documentation);
			}

			return EliminateDuplicates(docs);
		}

		private static List<IonDocumentation> EliminateDuplicates(List<IonDocumentation> documentation)
		{
			List<IonDocumentation> uniqueDocs = new();

			foreach (IonDocumentation doc in documentation)
			{
				if (!uniqueDocs.Any(d => d.AssemblyPath == doc.AssemblyPath))
				{
					uniqueDocs.Add(doc);
				}
			}
			return uniqueDocs;
		}

		private static List<IonNode> EliminateDuplicates(List<IonNode> nodes)
		{
			List<IonNode> uniqueNodes = new();

			foreach (IonNode node in nodes)
			{
				if (!uniqueNodes.Any(d => d.ClassNameFull == node.ClassNameFull && d.NodeName == node.NodeName))
				{
					uniqueNodes.Add(node);
				}
			}
			return uniqueNodes;
		}

		private bool TryGetAssemblyByAssemblyName(string assemblyName, out PluginAssembly? assembly)
		{
			assembly = null;

			foreach (KeyValuePair<string, PluginAssembly> kvp in Assemblies)
			{
				if (kvp.Value.AssemblyName == assemblyName)
				{
					assembly = kvp.Value;
					return true;
				}
			}
			return false;
		}

		private bool TryGetAssembly(QualifiedName rootNodeName, out PluginAssembly? assembly)
		{
			bool found = false;
			assembly = null;

			if (rootNodeName == null || string.IsNullOrEmpty(rootNodeName.Name))
			{
				return false;
			}
			else if (rootNodeName.AssemblyNameSet)
			{
				if (TryGetAssemblyByAssemblyName(rootNodeName.AssemblyName!, out PluginAssembly? assemblyByAssemblyName))
				{
					assembly = assemblyByAssemblyName!;
					found = true;
				}
			}
			else
			{
				List<string> keys = new();
				foreach (string key in Assemblies.Keys)
				{
					if (key.EndsWith(rootNodeName.Name, StringComparison.Ordinal))
					{
						keys.Add(key);
					}
				}
				if (keys.Count == 1)
				{
					assembly = Assemblies[keys[0]];
					found = true;
				}
			}

			return found;
		}

		public List<PluginParsingStatus> LoadAssemblies(string localFolder)
		{
			List<PluginParsingStatus> statuses = new();

			List<string> nugetPluginPaths = GetNugetPlugins();
			List<string> localPluginPaths = new();

			if (Directory.Exists(localFolder))
			{
				List<string> allLocalAssemblyPaths = new(Directory.GetFiles(localFolder, "*.dll", SearchOption.AllDirectories));

				//Ensure that we only attempt to load library files with the same file name as the plugin folder they're in
				foreach (string plugin in allLocalAssemblyPaths)
				{
					string? pluginPath = Path.GetDirectoryName(plugin);

					if (pluginPath != null)
					{
						string pluginDirectory = pluginPath.Split('\\').Last();
						string pluginName = Path.GetFileNameWithoutExtension(plugin);

						if (string.Equals(pluginDirectory, pluginName, StringComparison.OrdinalIgnoreCase))
							localPluginPaths.Add(plugin);
					}
				}
			}

			List<string> validPluginPaths = ProjectDependencyHandler.ResolveConflictingPlugins(nugetPluginPaths, localPluginPaths, out List<string> conflictWarnings);
			foreach (string warning in conflictWarnings)
				statuses.Add(new PluginParsingStatus(OperationStatus.Warning, warning));

			foreach (string file in validPluginPaths)
			{
				if (Path.GetFileNameWithoutExtension(file) != "Daf.Core.Sdk")
				{
					try
					{
						PluginAssembly pa = new(Assembly.LoadFile(file));
						Assemblies.Add(pa.RootNodeNameFull, pa);

						string docFile = Path.ChangeExtension(file, ".xml");

						if (File.Exists(docFile))
						{
							DocumentationParser dp = new(docFile);
							try
							{
								PluginDocumentation documentation = dp.GetDocumentation();
								pa.AddDocumentation(documentation);
								statuses.Add(new PluginParsingStatus(OperationStatus.Success, Path.GetFileName(file) + " loaded successfully"));
							}
							catch (DocumentationParsingException dpe)
							{
								statuses.Add(new PluginParsingStatus(OperationStatus.Warning, "Failed to parse documentation for plugin " + Path.GetFileName(docFile) + ". Message: " + dpe.Message));
							}
						}
						else
						{
							statuses.Add(new PluginParsingStatus(OperationStatus.Warning, "No documentation found for plugin " + Path.GetFileName(file)));
						}
					}
					catch (PluginParsingException ppe)
					{
						string message = ppe.Message + $" File: {file}.";
						statuses.Add(new PluginParsingStatus(OperationStatus.Error, message));
					}
				}
			}

			return statuses;
		}

		private List<string> GetNugetPlugins()
		{
			List<string> pluginPaths = new();
			string projectFile = Properties.Instance.ProjectFilePath;
			List<ProjectDependency> projectDependencies = new();

			try
			{
				projectDependencies.AddRange(ProjectDependencyHandler.GetProjectDependencies(projectFile));
			}
			catch (NugetDependencyNotFoundException ndnfe)
			{
				PluginParsingStatuses.Add(new PluginParsingStatus(OperationStatus.Warning, ndnfe.Message));
			}

			foreach (ReferenceProjectDependency dep in projectDependencies.Where(d => d is ReferenceProjectDependency))
			{
				if (ProjectDependencyHandler.IsPlugin(dep.AssemblyLocation))
					pluginPaths.Add(dep.AssemblyLocation);
			}

			return pluginPaths;
		}
	}
}
