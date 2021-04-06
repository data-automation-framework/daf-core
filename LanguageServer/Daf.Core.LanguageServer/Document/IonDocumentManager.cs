// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Daf.Core.LanguageServer.Document
{
	//Holds the documents that the user is currently editing
	public class IonDocumentManager
	{
		//Allows thread safe access to the open documents
		private readonly ConcurrentDictionary<string, IonDocument> documents = new();

		//Inserts or updates a document
		public void UpdateDocument(string documentPath, string content)
		{
			IonDocument? hd = documents.GetValueOrDefault(documentPath);
			if (hd != null)
			{
				hd.Content = content;
				hd.RegenerateLinks();
			}
			else
			{
				documents.AddOrUpdate(documentPath, new IonDocument(content, new Uri(documentPath)), (key, value) => new IonDocument(content, new Uri(documentPath)));
			}
		}

		public void RemoveDocument(string documentPath)
		{
			documents.TryRemove(documentPath, out _);   //Discard the value returned from the dictionary at removal
		}

		public IonDocument? GetDocument(string documentPath)
		{
			return documents.GetValueOrDefault(documentPath);
		}
	}
}
