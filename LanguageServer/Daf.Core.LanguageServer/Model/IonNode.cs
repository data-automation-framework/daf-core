// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System.Collections.Generic;

namespace Daf.Core.LanguageServer.Model
{
	public class IonNode
	{
		public string AssemblyName { get; set; }

		public string Namespace { get; set; }

		public string ClassName { get; set; }

		public string ClassNameFull
		{
			get
			{
				return Namespace + "." + ClassName;
			}
		}

		public string ParentClassNameFull //Class that declares this node including its assembly name and namespace
		{
			get
			{   //Special case for root node
				if (ParentNode == null)
					return ClassNameFull;

				//Otherwise get full name by parent
				return ParentNode.AssemblyName + "." + ParentNode.ClassNameFull;
			}
		}

		public string NodeName { get; set; }

		public string NodeNameFull
		{
			get
			{
				//Special case for root node
				if (ParentNode == null)
					return AssemblyName + "." + Namespace + "." + NodeName;

				//Otherwise get full name by parent
				return AssemblyName + "." + ParentNode.Namespace + "." + ParentNode.NodeName + "." + NodeName;
			}
		}

		public string? DerivedFrom { get; set; }

		public bool IsValid { get; set; }

		public IonNode? ParentNode { get; set; }

		public List<IonField> Fields { get; } = new();

		public List<IonNode> Children { get; } = new();

		public IonDocumentation Documentation { get; set; } = new();

		public bool IsContainer { get; set; }

		public bool IsAbstract { get; set; }

		public IonNode(string assemblyName, string className, string nodeName, string ns, bool isContainer, bool isAbstract, IonNode? parentNode)
		{
			AssemblyName = assemblyName;
			ClassName = className;
			NodeName = nodeName;
			Namespace = ns;
			IsContainer = isContainer;
			IsAbstract = isAbstract;
			ParentNode = parentNode;
			IsValid = true;
		}

		public IonNode Clone()
		{
			IonNode clone = new(AssemblyName, ClassName, NodeName, Namespace, IsContainer, IsAbstract, ParentNode);
			clone.Fields.AddRange(Fields);
			clone.Children.AddRange(Children);

			return clone;
		}
	}
}
