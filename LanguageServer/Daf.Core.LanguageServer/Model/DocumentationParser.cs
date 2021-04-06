// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Exceptions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml;

namespace Daf.Core.LanguageServer.Model
{
	internal class DocumentationParser
	{
		private readonly XmlDocument xmlDoc;
		private readonly XmlNamespaceManager xnsm;

		public DocumentationParser(string documentationFilePath)
		{
			xmlDoc = new XmlDocument();
			xnsm = new XmlNamespaceManager(xmlDoc.NameTable);
			xnsm.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
			xmlDoc.Load(documentationFilePath);
		}


		public PluginDocumentation GetDocumentation()
		{
			XmlNode assemblyNode = GetNode("descendant::assembly");
			XmlNode assemblyNameNode = GetNode("descendant::name", assemblyNode);

			string assemblyName = assemblyNameNode.InnerText;

			Dictionary<string, IonDocumentation> docDictionary = new();
			XmlNodeList memberNodes = GetNodes("descendant::member");

			foreach (XmlNode member in memberNodes)
			{
				string memberName = GetNodeFieldValue("name", member);

				char typeCode = memberName[0];  //T = Type, P = Property

				memberName = assemblyName + "." + memberName[2..];  //Prefix with assembly name and strip type code

				XmlNode summaryNode = GetNode("descendant::summary", member);

				string summaryDocumentation = summaryNode.InnerText.Trim();

				IonDocumentation hd = new()
				{
					AssemblyPath = memberName,
					Summary = summaryDocumentation,
					IsValid = true,
					IsForProperty = typeCode == 'P',
					IsForType = typeCode == 'T'
				};

				docDictionary.Add(memberName, hd);
			}

			//Set parent/child relationship
			foreach (IonDocumentation parent in docDictionary.Values.ToList().Where(d => d.IsForType))
			{
				foreach (IonDocumentation child in docDictionary.Values.ToList().Where(d => d.IsForProperty))
				{
					if (parent.AssemblyPath != child.AssemblyPath && child.AssemblyPath.StartsWith(parent.AssemblyPath, StringComparison.Ordinal))
					{
						parent.Children.Add(child);
					}
				}
			}

			return new PluginDocumentation(assemblyName, docDictionary);
		}

		private XmlNode GetNode(string xpath)
		{
			XmlNode? n = xmlDoc.SelectSingleNode(xpath, xnsm);
			if (n == null)
			{
				throw new DocumentationParsingException($"No node at XPath {xpath} found in XML document");
			}

			return n;
		}

		private XmlNode GetNode(string xpath, XmlNode node)
		{
			XmlNode? n = node.SelectSingleNode(xpath, xnsm);
			if (n == null)
			{
				throw new DocumentationParsingException($"No node at XPath {xpath} found in XML node {node.Name}");
			}

			return node;
		}

		private XmlNodeList GetNodes(string xpath)
		{
			XmlNodeList? nodes = xmlDoc.SelectNodes("descendant::member", xnsm);
			if (nodes == null)
			{
				throw new DocumentationParsingException($"No node at XPath {xpath} found in XML document");
			}

			return nodes;
		}

		private static string GetNodeFieldValue(string fieldName, XmlNode node)
		{
			if (node.Attributes == null)
				throw new DocumentationParsingException($"Node {node.Name} does not have any attributes");

			XmlAttribute? attribute = node.Attributes[fieldName];

			if (attribute == null)
				throw new DocumentationParsingException($"Node {node.Name} does not have any attribute with name {fieldName}");

			return attribute.Value;
		}
	}
}
