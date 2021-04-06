// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daf.Core.LanguageServer.Services
{
	public class CompletionService
	{
		private readonly LanguageServerPluginManager pluginManager;

		private const bool UseExactMatch = true;
		private const bool DoNotUseExactMatch = false;

		public CompletionService(LanguageServerPluginManager manager)
		{
			pluginManager = manager;
		}

		public async Task<IReadOnlyCollection<CompletionResult>> GetAutoCompleteNodesAndFieldsAsync(CompletionRequest completionRequest, bool intermediaryNodes)
		{
			List<CompletionResult> completions = new();

			await Task.Run(() =>
			{
				string nodeCompletionStart = "";
				if (completionRequest.ParentNodeLevel == completionRequest.CurrentLevel && completionRequest.CurrentLevel > 0)
					nodeCompletionStart = Environment.NewLine + "\t";

				List<IonNode> childNodes = GetNodes(completionRequest);

				foreach (IonNode childNode in childNodes)
				{
					string completionText = GetNodeCompletionText(childNode);
					string label = "(Node) " + childNode.NodeName;
					string documentation = childNode.Documentation.Summary;

					if (childNodes.Count(cn => cn.NodeName == childNode.NodeName) > 1)
					{
						label += " (ambiguous)";
						documentation += Environment.NewLine + "(From " + childNode.Namespace + ")";
					}

					CompletionResult cr = new(label, nodeCompletionStart + completionText, documentation);

					completions.Add(cr);
				}

				if (!intermediaryNodes && completionRequest.ParentNodeSet)  //Don't allow field completion after child nodes have been defined in the document
				{
					List<IonField> fields = GetFields(completionRequest);
					foreach (IonField field in fields)
					{
						string label = "(Field) " + field.Name;
						string documentation = field.Documentation.Summary;

						if (fields.Count(a => a.Name == field.Name) > 1)
						{
							label += " (ambiguous)";
							documentation += Environment.NewLine + "(From " + field.DerivedFrom + ")";
						}

						string detail = field.TypeName;
						string completionText = field.Name + "=";
						if (field.HasDefaultValue)
						{
							detail += " - default: " + field.DefaultValue;
							completionText += field.DefaultValue;
						}

						CompletionResult cr = new(label, completionText, documentation, detail);

						completions.Add(cr);
					}
				}
			});

			return completions;
		}

		public async Task<IReadOnlyCollection<CompletionResult>> GetAutoCompleteFieldValuesAsync(FieldValueCompletionRequest completionRequest)
		{
			List<CompletionResult> completions = new();
			await Task.Run(() =>
			{
				List<IonField> fields = GetFields(completionRequest);

				foreach (IonField field in fields)
				{
					foreach (string value in field.AvailableValues.FindAll(v => v.StartsWith(completionRequest.RequestInputText, StringComparison.OrdinalIgnoreCase)))
					{
						string label = value;
						string documentation = "";
						if (fields.Count > 1)
						{
							label += " (ambiguous)";
							documentation = "(From " + field.DerivedFrom + ")";
						}

						CompletionResult xmlAttrVal = new(value, value, documentation);

						completions.Add(xmlAttrVal);
					}
				}

			});
			return completions;
		}

		private List<IonNode> GetNodes(CompletionRequest completionRequest)
		{
			List<IonNode> childNodes = new();
			QualifiedName searchNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.RequestInputText);

			if (!completionRequest.ParentNodeSet)   //No parent node, get all nodes since context can't be determined
			{
				List<IonNode> nodes = pluginManager.GetAllNodes();
				childNodes.AddRange(nodes);
			}
			else if (completionRequest.RootNodeSet)
			{
				QualifiedName rootNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.RootNodeName!);
				QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.ParentNodeName!);

				List<IonNode> nodes = pluginManager.GetNodesByRootNode(rootNodeName, parentNodeName, searchNodeName, DoNotUseExactMatch);
				childNodes.AddRange(nodes);
			}
			else
			{
				QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.ParentNodeName!);

				List<IonNode> nodes = pluginManager.GetNodes(parentNodeName, searchNodeName, DoNotUseExactMatch);
				childNodes.AddRange(nodes);
			}

			return childNodes;
		}

		private List<IonField> GetFields(CompletionRequest completionRequest)
		{
			List<IonField> fields = new();

			//Requests for field name completion must have an associated parent node
			if (!completionRequest.ParentNodeSet)
				return fields;

			QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.ParentNodeName!);

			if (completionRequest.RootNodeSet)
			{
				QualifiedName rootNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.RootNodeName!);

				List<IonNode> nodes = pluginManager.GetNodesByRootNode(rootNodeName, parentNodeName, UseExactMatch);
				foreach (IonNode node in nodes)
				{
					fields.AddRange(node.Fields.FindAll(a => a.Name.StartsWith(completionRequest.RequestInputText, StringComparison.InvariantCultureIgnoreCase)));
				}
			}
			else
			{
				List<IonNode> nodes = pluginManager.GetNodes(parentNodeName, UseExactMatch);

				foreach (IonNode node in nodes)
				{
					fields.AddRange(node.Fields.FindAll(a => a.Name.StartsWith(completionRequest.RequestInputText, StringComparison.InvariantCultureIgnoreCase)));
				}
			}
			return fields;
		}

		private List<IonField> GetFields(FieldValueCompletionRequest completionRequest)
		{
			List<IonField> fields = new();

			//Requests for field value completion must have an associated parent node
			if (!completionRequest.ParentNodeSet)
				return fields;

			QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.ParentNodeName!);

			if (completionRequest.RootNodeSet)
			{
				QualifiedName rootNodeName = QualifiedNameProvider.GetQualifiedName(completionRequest.RootNodeName!);

				List<IonNode> nodes = pluginManager.GetNodesByRootNode(rootNodeName, parentNodeName, UseExactMatch);
				foreach (IonNode node in nodes)
				{
					fields.AddRange(node.Fields.FindAll(a => a.Name.Equals(completionRequest.FieldName, StringComparison.OrdinalIgnoreCase)));
				}
			}
			else
			{
				List<IonNode> nodes = pluginManager.GetNodes(parentNodeName, UseExactMatch);
				foreach (IonNode node in nodes)
				{
					fields.AddRange(node.Fields.FindAll(a => a.Name.StartsWith(completionRequest.FieldName, StringComparison.OrdinalIgnoreCase)));
				}
			}
			return fields;
		}

		private static string GetNodeCompletionText(IonNode node)
		{
			StringBuilder completion = new();
			completion.Append(node.NodeName).Append(": ");

			//Include only required fields
			if (node != null)
			{
				foreach (IonField field in node.Fields)
				{
					if (field.IsRequired)
					{
						if (field.HasDefaultValue)
						{
							completion.Append(field.Name + "=" + field.DefaultValue);
						}
						else
						{
							completion.Append(field.Name + "=");
						}
						completion.Append(' ');
					}
				}
			}

			return completion.ToString();
		}
	}
}
