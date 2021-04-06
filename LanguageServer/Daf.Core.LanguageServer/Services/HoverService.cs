// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Daf.Core.LanguageServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;    //Avoid System.Range ambiguity

namespace Daf.Core.LanguageServer.Services
{
	public class HoverService
	{
		private readonly LanguageServerPluginManager pluginManager;

		public HoverService(LanguageServerPluginManager manager)
		{
			pluginManager = manager;
		}

		internal async Task<Hover> GetDocumentationNodesAndFieldsAsync(HoverRequest hoverRequest)
		{
			Position start = new(hoverRequest.SearchTextLine, hoverRequest.SearchTextLineOffset);
			Position end = new(hoverRequest.SearchTextLine, hoverRequest.SearchTextLineOffset + hoverRequest.SearchText.Length);
			Range range = new(start, end);
			MarkedString content = new(string.Empty);
			List<IonDocumentation> documentation = new();

			await Task.Run(() =>
			{
				documentation.AddRange(GetDocumentation(hoverRequest));

				if (documentation.Count == 1)
				{
					string displayDoc = documentation[0].TypeName + " - " + documentation[0].Summary;
					content = new(displayDoc);
				}
				else if (documentation.Count > 1)
				{
					string displayDoc = "Ambiguous - either:  " + Environment.NewLine;

					for (int i = 0; i < documentation.Count; i++)
					{
						IonDocumentation doc = documentation[i];
						displayDoc += doc.TypeName + " - " + doc.Summary + "  " + Environment.NewLine + "(from " + doc.AssemblyPath + ")";
						if (i < documentation.Count - 1)
						{
							displayDoc += "  " + Environment.NewLine + "or  " + Environment.NewLine;
						}
					}
					content = new(displayDoc);
				}
			});

			return new Hover()
			{
				Range = range,
				Contents = new MarkedStringsOrMarkupContent(content)
			};
		}

		public async Task<Hover> GetDocumentationFieldValueAsync(FieldValueHoverRequest request)
		{
			MarkedString content = new(string.Empty);

			await Task.Run(() =>
			{
				List<IonField> fields = GetFields(request);

				if (fields.Count == 1)
				{
					if (fields[0].AvailableValues.Count > 0)
					{
						string values = "Available values  " + Environment.NewLine + string.Join("  " + Environment.NewLine, fields[0].AvailableValues);

						content = new(values);
					}
				}
				else if (fields.Count > 1)
				{
					List<IonField> enumFields = fields.Where(a => a.AvailableValues.Count > 0).ToList();
					if (enumFields.Count > 0)
					{
						string displayDoc = "Ambiguous:  " + Environment.NewLine;
						foreach (IonField enumAttr in enumFields)
						{
							displayDoc += "**" + enumAttr.DerivedFrom + "**  " + Environment.NewLine;
							displayDoc += "Available values:  " + Environment.NewLine;
							displayDoc += string.Join("  " + Environment.NewLine, enumAttr.AvailableValues);
							displayDoc += "  " + Environment.NewLine;
						}
						content = new(displayDoc);
					}
				}
			});

			return new Hover()
			{
				Contents = new MarkedStringsOrMarkupContent(content)
			};
		}

		private List<IonDocumentation> GetDocumentation(HoverRequest hoverRequest)
		{
			List<IonDocumentation> docs = new();
			QualifiedName searcWordName = QualifiedNameProvider.GetQualifiedName(hoverRequest.SearchText);

			if (!string.IsNullOrEmpty(hoverRequest.SearchText))
			{
				if (hoverRequest.RootNodeSet && hoverRequest.ParentNodeSet)
				{
					QualifiedName rootNodeName = QualifiedNameProvider.GetQualifiedName(hoverRequest.RootNodeName!);
					QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(hoverRequest.ParentNodeName!);

					docs.AddRange(pluginManager.GetDocumentation(rootNodeName, parentNodeName, searcWordName));
				}
				else if (hoverRequest.ParentNodeSet)
				{
					QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(hoverRequest.ParentNodeName!);

					docs.AddRange(pluginManager.GetDocumentation(parentNodeName, searcWordName));
				}
				else
				{
					docs.AddRange(pluginManager.GetDocumentation(searcWordName));
				}
			}

			return docs.Where(d => d.IsValid).ToList();
		}


		private List<IonField> GetFields(FieldValueHoverRequest request)
		{
			List<IonField> fields = new();

			if (!request.ParentNodeSet)
				return fields;

			QualifiedName parentNodeName = QualifiedNameProvider.GetQualifiedName(request.ParentNodeName!);

			if (request.RootNodeSet)
			{
				QualifiedName rootNodeName = QualifiedNameProvider.GetQualifiedName(request.RootNodeName!);

				List<IonNode> nodes = pluginManager.GetNodesByRootNode(rootNodeName, parentNodeName, true);
				foreach (IonNode node in nodes)
				{
					fields.AddRange(node.Fields.FindAll(a => a.Name == request.FieldName));
				}
			}
			else
			{

				List<IonNode> nodes = pluginManager.GetNodes(parentNodeName, true);
				foreach (IonNode node in nodes)
				{
					fields.AddRange(node.Fields.FindAll(a => a.Name == request.FieldName));
				}
			}

			return fields;
		}
	}
}
