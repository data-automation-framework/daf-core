// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.LanguageServer.Exceptions;
using Daf.Core.LanguageServer.Services;
using Daf.Core.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Daf.Core.LanguageServer.Model
{
	public class PluginAssembly
	{
		private readonly Assembly asm;

		private readonly Type rootNodeType;

		private readonly List<Type> parsingTypes;   //Used to keep track of circular dependencies

		private readonly List<PendingChild> pendingChildren;           //Used to set child nodes that were skipped due to circular dependencies

		private readonly Dictionary<string, IonNode> nodeDictionary;      //Key = IonNode.AssemblyFullName

		private readonly List<IonNode> nodes;

		public string RootNodeNameFull { get; }

		public string AssemblyName { get; set; }

		public PluginAssembly(Assembly assembly)
		{
			asm = assembly;
			parsingTypes = new List<Type>();
			pendingChildren = new List<PendingChild>();
			nodeDictionary = new Dictionary<string, IonNode>();
			nodes = new List<IonNode>();

			foreach (Type t in asm.GetTypes())
			{
				IsRootNodeAttribute? rootNode = (IsRootNodeAttribute?)Attribute.GetCustomAttribute(t, typeof(IsRootNodeAttribute));
				if (rootNode != null)
				{
					RootNodeNameFull = asm.GetName().Name + "." + t.FullName;
					rootNodeType = t;
					break;
				}
			}

			if (rootNodeType == null || RootNodeNameFull == null)
				throw new PluginParsingException($"A root node could not be found in assembly {asm.FullName}");

			if (asm.GetName().Name == null)
				throw new PluginParsingException($"Assembly does not specify a name.");

			AssemblyName = asm.GetName().Name!;

			ParseAssembly();

			nodeDictionary = GetNodeDictionary();
		}

		internal List<IonNode> GetNodes(QualifiedName parentNodeName, QualifiedName searchNodeName, bool exactMatch)
		{
			List<IonNode> nodes = new();
			List<string> parentNodeKeys = new();

			foreach (string key in nodeDictionary.Keys.ToList())
			{
				if (parentNodeName.NamespaceSet && key.EndsWith(parentNodeName.Namespace + "." + parentNodeName.Name, StringComparison.OrdinalIgnoreCase))
				{
					parentNodeKeys.Add(key);
				}
				else if (key.EndsWith(parentNodeName.Name, StringComparison.OrdinalIgnoreCase))
				{
					parentNodeKeys.Add(key);
				}
			}

			//Get child nodes that matches the searched for node within the parent node
			foreach (string parentNodeKey in parentNodeKeys)
			{
				List<string> nodeKeys = new();

				//If parent can be identified then it is enough to know the name of the child node since a class cannot have multiple properties with the same name
				IonNode parentNode = nodeDictionary[parentNodeKey];
				foreach (IonNode childNode in parentNode.Children)
				{
					if (exactMatch && childNode.NodeName == searchNodeName.Name)
					{
						nodes.Add(childNode);
					}
					else if (!exactMatch && childNode.NodeName.StartsWith(searchNodeName.Name, StringComparison.OrdinalIgnoreCase))
					{
						nodes.Add(childNode);
					}
				}
			}

			return nodes.Where(n => n.IsAbstract == false).ToList();
		}

		internal List<IonNode> GetNodes(QualifiedName searchNodeName, bool exactMatch)
		{
			if (searchNodeName == null)
				throw new ArgumentNullException(nameof(searchNodeName));

			List<IonNode> nodes = new();
			List<string> nodeKeys = new();

			foreach (string key in nodeDictionary.Keys.ToList())
			{
				string[] keyParts = key.Split('.');
				if (searchNodeName.NamespaceSet && key.StartsWith(searchNodeName.AssemblyName + "." + searchNodeName.Namespace, StringComparison.OrdinalIgnoreCase))
				{
					if (exactMatch && keyParts[^1] == searchNodeName.Name)
					{
						nodeKeys.Add(key);
					}
					else if (!exactMatch && keyParts[^1].StartsWith(searchNodeName.Name, StringComparison.OrdinalIgnoreCase))
					{
						nodeKeys.Add(key);
					}
				}
				else
				{
					if (exactMatch && keyParts[^1] == searchNodeName.Name)
					{
						nodeKeys.Add(key);
					}
					else if (!exactMatch && keyParts[^1].StartsWith(searchNodeName.Name, StringComparison.OrdinalIgnoreCase))
					{
						nodeKeys.Add(key);
					}
				}
			}

			foreach (string nodeKey in nodeKeys)
			{
				nodes.Add(nodeDictionary[nodeKey]);
			}

			return nodes.Where(n => n.IsAbstract == false).ToList();
		}

		internal List<IonNode> GetAllNodes()
		{
			return nodeDictionary.Values.Where(n => n.IsAbstract == false).ToList();
		}

		internal List<IonField> GetFields(QualifiedName parentNodeName, string fieldName, bool exactMatch)
		{
			List<IonField> fields = new();

			if (parentNodeName != null)
			{
				List<IonNode> nodes = GetNodes(parentNodeName, exactMatch);

				foreach (IonNode node in nodes)
				{
					foreach (IonField field in node.Fields)
					{
						if (exactMatch && field.Name == fieldName)
						{
							fields.Add(field);
						}
						else if (!exactMatch && field.Name.StartsWith(fieldName, StringComparison.OrdinalIgnoreCase))
						{
							fields.Add(field);
						}
					}
				}
			}

			return fields;
		}
		internal void AddDocumentation(PluginDocumentation documentation)
		{

			foreach (IonNode node in nodeDictionary.Values.ToList())
			{
				foreach (IonDocumentation doc in documentation.Documentation.Values.ToList())
				{
					bool documentationFound = false;
					if (node.ParentClassNameFull + "." + node.NodeName == doc.AssemblyPath) //Get documentation by property name if not list element
						documentationFound = true;
					else if (node.ParentNode != null && node.ParentNode.IsContainer         //Get documentation by class name if list element
								&& node.AssemblyName + "." + node.ClassNameFull == doc.AssemblyPath)
						documentationFound = true;
					else if (node.ParentNode == null && node.AssemblyName + "." + node.ClassNameFull == doc.AssemblyPath) //Get root node documentation
						documentationFound = true;

					if (documentationFound)
					{
						//If the node has not already received documentation via a parent/child relationship (such as base classes and list elements)
						if (!node.Documentation.IsValid)
						{
							IonDocumentation docCopy = doc.Clone();
							docCopy.TypeName = "Node";
							node.Documentation = docCopy;
						}

						//Set documentation based on child nodes
						foreach (IonNode childNode in node.Children)
						{
							documentation.Documentation.TryGetValue(childNode.AssemblyName + "." + childNode.DerivedFrom, out IonDocumentation? childDoc);
							if (childDoc != null)
							{
								IonDocumentation childDocCopy = childDoc.Clone();
								childDocCopy.TypeName = "Node";
								childNode.Documentation = childDocCopy;
							}
						}

						//Set documentation on fields
						foreach (IonField field in node.Fields)
						{
							documentation.Documentation.TryGetValue(field.AssemblyName + "." + field.DerivedFrom, out IonDocumentation? attrDoc);
							if (attrDoc != null)
							{
								IonDocumentation attrDocCopy = attrDoc.Clone();
								attrDocCopy.TypeName = "Field";
								field.Documentation = attrDocCopy;
							}
						}

						break;
					}
				}
			}
		}

		private void ParseAssembly()
		{

			ParseTypeToNode(rootNodeType, rootNodeType.Name, null);

			SetPendingChildren();
		}

		private IonNode ParseTypeToNode(Type t, string nodeName, IonNode? parentNode)
		{
			parsingTypes.Add(t);

			bool isContainerType = IsContainerType(t);

			if (t.Namespace == null)
				throw new PluginParsingException($"Type {t.Name} does not have a namespace in assembly {asm.FullName}.");

			string className = isContainerType ? GetContainerTypeName(t) : t.Name;
			string ns = isContainerType ? parentNode!.Namespace : t.Namespace; //Use namespace from parent if list (system namespaces are avoided)

			IonNode node = new(AssemblyName, className, nodeName, ns, isContainerType, t.IsAbstract, parentNode);

			if (isContainerType)
			{
				//Consider the list element type to be the child type
				Type elementType = t.GenericTypeArguments[0];  //TODO: Fix hard coded 0

				List<Type> baseAndDerivedTypes = new()
				{
					elementType
				};
				baseAndDerivedTypes.AddRange(GetAllInheritingTypes(elementType));

				foreach (Type badt in baseAndDerivedTypes)
				{
					if (!IsAlreadyBeingParsed(badt))
					{
						IonNode childNode = ParseTypeToNode(badt, badt.Name, node);   //Parse recursively
						node.Children.Add(childNode);
					}
					else
					{
						AddPendingListChild(node, badt); //Current node should have a child node which will be added later to avoid circular references
					}
				}
			}
			else
			{
				foreach (PropertyInfo p in t.GetProperties())
				{
					string derivedFrom = p.DeclaringType + "." + p.Name;

					bool propertyIsContainerType = IsContainerType(p.PropertyType);

					if (p.PropertyType.IsEnum)
					{
						IonField field = ConstructField(p);
						field.AvailableValues.AddRange(p.PropertyType.GetEnumNames());
						node.Fields.Add(field);
					}
					else if (propertyIsContainerType)
					{
						IonNode childNode = ParseTypeToNode(p.PropertyType, p.Name, node);    //Parse recursively
						childNode.DerivedFrom = derivedFrom;
						node.Children.Add(childNode);
					}
					else if (p.PropertyType.Module != t.Module)  //Type defined outside of plugin, assume simple type (string, int etc)
					{
						IonField field = ConstructField(p);
						node.Fields.Add(field);
					}
					else
					{
						List<Type> inheritingTypes = GetAllInheritingTypes(p.PropertyType);
						if (inheritingTypes.Count > 0)
						{
							throw new PluginParsingException($"Property {p.Name} is of a type that has one or more derived types. Derived classes are only allowed as element types in lists.");
						}
						else
						{
							if (!IsAlreadyBeingParsed(p.PropertyType))
							{
								IonNode childNode = ParseTypeToNode(p.PropertyType, p.Name, node);   //Parse recursively, normal type
								childNode.DerivedFrom = derivedFrom;
								node.Children.Add(childNode);
							}
							else
							{
								AddPendingChild(node, p);   //Current node should have a child node which will be added later to avoid circular references
							}
						}
					}
				}
			}

			parsingTypes.Remove(t);

			nodes.Add(node);
			return node;
		}

		private void AddPendingChild(IonNode node, PropertyInfo property)
		{
			if (property.PropertyType.FullName == null)
				throw new PluginParsingException($"Property {property.Name} of type {property.PropertyType.Name} does not specify a full name or is missing a namespace.");

			PendingChild pc = new(node, property.Name, property.PropertyType.FullName);
			pendingChildren.Add(pc);
		}

		private void AddPendingListChild(IonNode node, Type elementType)
		{
			if (elementType.FullName == null)
				throw new PluginParsingException($"Type {elementType.Name} does not specify a full name or is missing a namespace.");

			PendingChild pc = new(node, elementType.Name, elementType.FullName);
			pendingChildren.Add(pc);
		}

		private IonField ConstructField(PropertyInfo property)
		{
			string typeName = property.PropertyType.Name;
			if (Nullable.GetUnderlyingType(property.PropertyType) != null)
				typeName = Nullable.GetUnderlyingType(property.PropertyType)!.Name;

			if (property.PropertyType.Namespace == null)
				throw new PluginParsingException($"Property {property.Name} of type {property.PropertyType.Name} is missing a namespace.");

			string ns = property.PropertyType.Namespace;
			string derivedFrom = property.DeclaringType + "." + property.Name;

			IonField field = new(AssemblyName, ns, property.Name, typeName, derivedFrom);

			//Check for .NET attributes on the property
			IsRequiredAttribute? isRequired = (IsRequiredAttribute?)Attribute.GetCustomAttribute(property, typeof(IsRequiredAttribute));
			if (isRequired != null)
				field.IsRequired = true;

			DefaultValueAttribute? defaultValue = (DefaultValueAttribute?)Attribute.GetCustomAttribute(property, typeof(DefaultValueAttribute));
			if (defaultValue != null)
			{
				field.DefaultValue = defaultValue!.Value.ToString() ?? "";
			}

			return field;
		}

		//Get all types that inherits from baseType
		private List<Type> GetAllInheritingTypes(Type baseType)
		{
			List<Type> immediateInheritingTypes = new();
			List<Type> allInheritingTypes = new();

			foreach (Type t in asm.GetTypes())
			{
				if (t.BaseType == baseType)
				{
					immediateInheritingTypes.Add(t);
				}
			}
			allInheritingTypes.AddRange(immediateInheritingTypes);

			foreach (Type t in immediateInheritingTypes)
			{
				allInheritingTypes.AddRange(GetAllInheritingTypes(t)); //Recursion, get inheriting types of immediate inheriting types
			}

			return allInheritingTypes;
		}

		private static string GetContainerTypeName(Type t)
		{
			Type elementType = t.GenericTypeArguments[0];
			return t.Name + "(" + elementType.Name + ")";
		}

		private bool IsAlreadyBeingParsed(Type type)
		{
			if (parsingTypes.Contains(type))
				return true;
			return false;
		}

		private static bool IsContainerType(Type t)
		{
			bool isContainerType = t.GetInterface("IList") != null;
			isContainerType = isContainerType || t.IsArray;
			return isContainerType;
		}

		private void SetPendingChildren()
		{
			foreach (PendingChild pc in pendingChildren)
			{
				IonNode parentNode = nodes.Where(n => n == pc.ParentNode).First();
				IonNode childNode = nodes.Where(n => n.ClassNameFull == pc.ClassNameFull).First().Clone();
				childNode.NodeName = pc.ChildNodeName;
				childNode.ParentNode = pc.ParentNode;
				parentNode.Children.Add(childNode);
			}
			pendingChildren.Clear();
		}

		private Dictionary<string, IonNode> GetNodeDictionary()
		{
			Dictionary<string, IonNode> nodeLookup = new();

			foreach (IonNode node in nodes)
			{
				if (node.ParentNode != null)
				{
					nodeLookup.TryAdd(node.AssemblyName + "." + node.ParentNode.Namespace + "." + node.ParentNode.NodeName + "." + node.NodeName, node);
				}
				else
					nodeLookup.TryAdd(node.AssemblyName + "." + node.ClassNameFull, node);
			}

			return nodeLookup;
		}

		private class PendingChild
		{
			public IonNode ParentNode { get; set; }
			public string ChildNodeName { get; set; }
			public string ClassNameFull { get; set; }

			internal PendingChild(IonNode parentNode, string childNodeName, string classNameFull)
			{
				ParentNode = parentNode;
				ChildNodeName = childNodeName;
				ClassNameFull = classNameFull;
			}
		}
	}
}
