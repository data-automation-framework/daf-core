// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using Daf.Core.LanguageServer.Document;
using Daf.Core.LanguageServer.Handlers;
using Daf.Core.LanguageServer.Model;
using Daf.Core.LanguageServer.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Daf.Core.LanguageServer
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			Parser.Default.ParseArguments<IonLanguageServerOptions>(args).WithParsed(options => SetProperties(options));

			await StartUpAsync();
		}

		private static async Task StartUpAsync()
		{
			LanguageServerOptions lsOptions = new();

			using Stream input = Console.OpenStandardInput();
			using Stream output = Console.OpenStandardOutput();
			lsOptions.WithInput(input);
			lsOptions.WithOutput(output);
			lsOptions.ConfigureLogging(x => x.AddLanguageProtocolLogging().SetMinimumLevel(LogLevel.Trace));
			lsOptions.WithServices(ConfigureServices);
			lsOptions.WithHandler<DocumentHandler>();
			lsOptions.WithHandler<IonHoverHandler>();
			lsOptions.WithHandler<IonCompletionHandler>();
			lsOptions.WithHandler<IonLinkHandler>();
			lsOptions.WithHandler<ReloadPluginHandler>();
			ILanguageServer ls = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(lsOptions);

			await ls.WaitForExit;
			ls.Dispose();
		}
		private static void SetProperties(IonLanguageServerOptions ionOptions)
		{
			Properties props = Properties.Instance;
			props.ProjectFilePath = ionOptions.ProjectFilePath;
			props.PluginFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"CustomProjectSystems\Daf.Core\Dependencies\Plugins");
		}

		private static void ConfigureServices(IServiceCollection services)
		{
			LanguageServerPluginManager pm = new();
			pm.LoadAssemblies(Properties.Instance.PluginFolder);

			services.AddSingleton<IonDocumentManager>();
			services.AddSingleton(m => pm);
			services.AddSingleton<CompletionService>();
			services.AddSingleton<HoverService>();
			services.AddSingleton<ReloadPluginService>();
		}
	}
}
