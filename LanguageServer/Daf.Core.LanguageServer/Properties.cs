// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using System;
using System.Collections.Generic;

namespace Daf.Core.LanguageServer
{
	public class Properties
	{
		private static Properties? _instance;

		private Properties()
		{
			PluginFolder = "";
			ProjectFilePath = "";
			OtherProperties = new Dictionary<string, string>();
		}

		public static Properties Instance
		{
			get
			{
				_instance ??= new Properties();

				return _instance;
			}
		}

		public string PluginFolder { get; set; }

		public string ProjectFilePath { get; set; }

		public Dictionary<string, string> OtherProperties { get; private set; }

		public void SetOtherProperties(List<string> args)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			Dictionary<string, string> properties = new();

			if (args.Count != 0)
			{
				foreach (string property in args)
				{
					string[] tokens = property.Split('=');

					properties[tokens[0]] = tokens[1];
				}
			}

			OtherProperties = properties;
		}

	}
}
