import * as path from 'path';
import * as fs from 'fs';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, SettingMonitor, ServerOptions, TransportKind, InitializeParams } from 'vscode-languageclient/node';
import { Trace } from 'vscode-jsonrpc';

var projectFile =
`<Project>
	<Import Sdk="Microsoft.NET.Sdk" Project="Sdk.props" />
	<PropertyGroup>
		<TargetFramework>net5.0</TargetFramework>
		<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
	</PropertyGroup>
	<PropertyGroup Label="Globals">
		<DafCoreRootPath>$(LocalAppData)\\CustomProjectSystems\\Daf.Core\\</DafCoreRootPath>
	</PropertyGroup>
	<Import Project="$(DafCoreRootPath)Daf.Core.props" />
	<ItemGroup>
		<dafFile Include="**\\*.daf" />
		<ionFile Include="**\\*.ion" />
		<ionOutput Include="**\\intermediate.ion">
			<DependentUpon>%(Filename).ion</DependentUpon>
		</ionOutput>
		<csCompile Include="**\\*.cs" />
		<sqlFile Include="**\\*.sql" Exclude="$(OutputPath)\\**\\*.sql" />
	</ItemGroup>
	<Import Sdk="Microsoft.NET.Sdk" Project="Sdk.targets" />
	<Import Project="$(DafCoreRootPath)Daf.Core.targets" />
</Project>`;

var userFile =
`<Project>
	<PropertyGroup>
		<StopBuildOnScriptErrors>true</StopBuildOnScriptErrors>
		<FailedConnectionRetryCount>0</FailedConnectionRetryCount>
		<VisualStudioEdition>Professional</VisualStudioEdition>
	</PropertyGroup>
</Project>`;

let taskProvider: vscode.Disposable | undefined;

let lspClient: LanguageClient | undefined;

export function activate(_context: vscode.ExtensionContext): void
{
	let workspaceRoot = vscode.workspace.workspaceFolders![0];
	if (!workspaceRoot)
	{
		// Abort if we don't have a workspace.
		return;
	}

	let projectFileDisposable = vscode.commands.registerCommand('daf.createProjectFile', () =>
	{
		createProjectFile();

		vscode.window.showInformationMessage('Created project file (.dafproj).');
	});

	_context.subscriptions.push(projectFileDisposable);

	let userFileDisposable = vscode.commands.registerCommand('daf.createUserFile', (name: string) =>
	{
		createUserFile();

		vscode.window.showInformationMessage(`Created .dafproj.user file, ${name}!`);
	});

	_context.subscriptions.push(userFileDisposable);
	

	let reloadPluginsDisposable = vscode.commands.registerCommand('daf.reloadPlugins', () =>
	{
		reloadPlugins();

		vscode.window.showInformationMessage('Reloaded Daf plugins.');
	});

	_context.subscriptions.push(reloadPluginsDisposable);

	let dafPromise: Thenable<vscode.Task[]> | undefined = undefined;
	
	taskProvider = vscode.tasks.registerTaskProvider('daf',
	{
		provideTasks: () =>
		{
			if (!dafPromise)
			{
				dafPromise = getDafTasks();
			}

			return dafPromise;
		},
		resolveTask(_task: vscode.Task): vscode.Task | undefined
		{
			return undefined;
		}
	});

	//Language server client 
	loadLanguageServer(_context, workspaceRoot);
}

async function loadLanguageServer(_context: vscode.ExtensionContext, workspaceRoot: vscode.WorkspaceFolder): Promise<void>
{
	if (!workspaceRoot)
	{
		return;
	}

	let filesInWorkspace = await getFiles(workspaceRoot);
	let projectFilesInWorkspace = filesInWorkspace.filter(function (str) { return str.endsWith('.dafproj'); });
	let projectFileCount = projectFilesInWorkspace.length;

	// At the moment we only support one project file per workspace root. If there are more (or less), abort.
	if (projectFileCount !== 1)
	{
		return;
	}

	// Get the project file path.
	let dafProjectFileLocation = path.join(workspaceRoot.uri.fsPath, projectFilesInWorkspace[0]);
	let languageServerLocation: string = path.join(_context.extensionPath, "Daf.Core.LanguageServer");
	let languageServerExe: string = path.join(languageServerLocation, "Daf.Core.LanguageServer.exe");
	let serverArgumentList: string[] = new Array("--projectFilePath=" + dafProjectFileLocation);

	let serverOptions: ServerOptions = {
		run: { command: languageServerExe, args: serverArgumentList },
		debug: { command: languageServerExe, args: serverArgumentList },
	};
	
	let clientOptions: LanguageClientOptions = {
		// Register the server for daf documents
		documentSelector: [
			{
				pattern: '**/*.daf',
			}
		],
		synchronize: {
			fileEvents: vscode.workspace.createFileSystemWatcher('**/*.daf')
		}
	};
	
	const client: LanguageClient = new LanguageClient('LSP', 'Daf Core Language Server', serverOptions, clientOptions);
	client.trace = Trace.Verbose;
	let disposable = client.start();

	lspClient = client;

	_context.subscriptions.push(disposable);
}

async function createProjectFile(): Promise<void>
{
	let workspaceRoot = vscode.workspace.workspaceFolders![0];

	if (!workspaceRoot)
	{
		return;
	}

	let filesInWorkspace = await getFiles(workspaceRoot);
	let projectFilesInWorkspace = filesInWorkspace.filter(function (str) { return str.endsWith('.dafproj'); });
	let projectFileCount = projectFilesInWorkspace.length;

	// Don't let the user create a project file if one already exists in the workspace.
	if (projectFileCount !== 0)
	{
		return;
	}

	// Get the project file path.
	let workspaceName = workspaceRoot.name;

	// Set the project file name based on the current workspace folder.
	const newFile = await vscode.Uri.parse('untitled:' + path.join(vscode.workspace.workspaceFolders![0].uri.fsPath, `${workspaceName}.dafproj`));

	vscode.workspace.openTextDocument(newFile).then(document =>
	{
		const edit = new vscode.WorkspaceEdit();
		edit.insert(newFile, new vscode.Position(0, 0), projectFile);

		vscode.workspace.applyEdit(edit).then(success =>
		{
			if (success)
			{
				vscode.window.showTextDocument(document);
			}
			else
			{
				vscode.window.showInformationMessage('Error!');
			}
		});
	});
}

async function createUserFile(): Promise<void>
{
	let workspaceRoot = vscode.workspace.workspaceFolders![0];

	if (!workspaceRoot)
	{
		return;
	}

	let filesInWorkspace = await getFiles(workspaceRoot);
	let projectFilesInWorkspace = filesInWorkspace.filter(function (str) { return str.endsWith('.dafproj'); });
	let projectFileCount = projectFilesInWorkspace.length;

	// At the moment we only support one project file per workspace root. If there are more (or less), abort.
	if (projectFileCount !== 1)
	{
		return;
	}

	// Get the project file path.
	let dafProjFile = path.join(workspaceRoot.uri.fsPath, projectFilesInWorkspace[0]);
	let taskName = dafProjFile.split('\\').pop()!.split('/').pop()!;

	const newFile = await vscode.Uri.parse('untitled:' + path.join(workspaceRoot.uri.fsPath, `${taskName}.user`));

	vscode.workspace.openTextDocument(newFile).then(document =>
	{
		const edit = new vscode.WorkspaceEdit();
		edit.insert(newFile, new vscode.Position(0, 0), userFile);

		vscode.workspace.applyEdit(edit).then(success =>
		{
			if (success)
			{
				vscode.window.showTextDocument(document);
			}
			else
			{
				vscode.window.showInformationMessage('Error!');
			}
		});
	});
}

async function reloadPlugins(): Promise<void>
{
	if (lspClient != undefined)
	{
		lspClient.sendRequest('daf/reloadplugins');
	}
	else
	{
		vscode.window.showInformationMessage('Cannot reload Daf plugins, language server has not been initiated.');
	}
}

export function deactivate(): void
{
	if (taskProvider)
	{
		taskProvider.dispose();
	}
}

function exists(file: string): Promise<boolean>
{
	return new Promise<boolean>((resolve, _reject) =>
	{
		fs.exists(file, (value) =>
		{
			resolve(value);
		});
	});
}

function getFiles(path: vscode.WorkspaceFolder): Promise<string[]>
{
	return new Promise<string[]>((resolve, _reject) =>
	{
		fs.readdir(path.uri.fsPath, (err, value) =>
		{
			resolve(value);
		});
	});
}

let _channel: vscode.OutputChannel;
function getOutputChannel(): vscode.OutputChannel
{
	if (!_channel)
	{
		_channel = vscode.window.createOutputChannel('Daf Core Auto Detection');
	}

	return _channel;
}

interface DafTaskDefinition extends vscode.TaskDefinition
{
	// The project file name
	projectFile: string;

	// The build configuration (prod, dev, etc)
	configuration: string;

	// The build type (full, sql, etc)
	build: string;

	// Additional parameters
	parameters: string;
}

async function getDafTasks(): Promise<vscode.Task[]>
{
	let workspaceRoot = vscode.workspace.workspaceFolders![0];
	let emptyTasks: vscode.Task[] = [];

	if (!workspaceRoot)
	{
		return emptyTasks;
	}

	let filesInWorkspace = await getFiles(workspaceRoot);
	let projectFilesInWorkspace = filesInWorkspace.filter(function (str) { return str.endsWith('.dafproj'); });
	let projectFileCount = projectFilesInWorkspace.length;

	// At the moment we only support one project file per workspace root. If there are more (or less), abort.
	if (projectFileCount !== 1)
	{
		return emptyTasks;
	}

	// Get the project file path.
	let dafProjFile = path.join(workspaceRoot.uri.fsPath, projectFilesInWorkspace[0]);

	if (!await exists(dafProjFile))
	{
		return emptyTasks;
	}

	try
	{
		let projectFileName = dafProjFile.split('\\').pop()!.split('/').pop()!;

		// This only gets read on the first build (when the task is being resolved).
		// How can we avoid forcing the user to restart VSCode to make a changed configuration have an effect?
		const config = vscode.workspace.getConfiguration('daf-core');
		let additionalParameters = `-p:DafCoreRootPath='${vscode.extensions.getExtension("data-automation-framework.daf-core")!.extensionPath}\\Daf.Core\\' `;

		let stop1 = config.inspect('build.stopBuildOnScriptErrors')!.globalValue;
		let stop2 = config.inspect('build.stopBuildOnScriptErrors')!.workspaceValue;

		if (typeof stop1 !== 'undefined' || typeof stop2 !== 'undefined')
		{
			let stopBuildOnScriptErrors = config.get('build.stopBuildOnScriptErrors');
			additionalParameters += `-p:StopBuildOnScriptErrors=${stopBuildOnScriptErrors} `;
		}

		let failed1 = config.inspect('build.failedConnectionRetryCount')!.globalValue;
		let failed2 = config.inspect('build.failedConnectionRetryCount')!.workspaceValue;

		if (typeof failed1 !== 'undefined' || typeof failed2 !== 'undefined')
		{
			let failedConnectionRetryCount = config.get('build.failedConnectionRetryCount');
			additionalParameters += `-p:failedConnectionRetryCount=${failedConnectionRetryCount} `;
		}

		let vsEdition1 = config.inspect('build.visualStudioEdition')!.globalValue;
		let vsEdition2 = config.inspect('build.visualStudioEdition')!.workspaceValue;

		if (typeof vsEdition1 !== 'undefined' || typeof vsEdition2 !== 'undefined')
		{
			let vsEdition = config.get('build.visualStudioEdition');
			additionalParameters += `-p:visualStudioEdition=${vsEdition} `;
		}

		let devFullTask: DafTaskDefinition =
		{
			type: 'daf',
			projectFile: projectFileName,
			configuration: 'Dev',
			build: 'Full',
			parameters: additionalParameters
		};

		let devSqlTask: DafTaskDefinition =
		{
			type: 'daf',
			projectFile: projectFileName,
			configuration: 'Dev',
			build: 'SQL',
			parameters: additionalParameters
		};

		let prodFullTask: DafTaskDefinition =
		{
			type: 'daf',
			projectFile: projectFileName,
			configuration: 'Prod',
			build: 'Full',
			parameters: additionalParameters
		};

		let prodSqlTask: DafTaskDefinition =
		{
			type: 'daf',
			projectFile: projectFileName,
			configuration: 'Prod',
			build: 'SQL',
			parameters: additionalParameters
		};

		let result: vscode.Task[] = [];

		result.push(createTask(devFullTask));
		result.push(createTask(devSqlTask));
		result.push(createTask(prodFullTask));
		result.push(createTask(prodSqlTask));

		return result;
	} catch (err)
	{
		let channel = getOutputChannel();

		if (err.stderr)
		{
			channel.appendLine(err.stderr);
		}
		if (err.stdout)
		{
			channel.appendLine(err.stdout);
		}

		channel.appendLine('Auto detecting Daf Core tasks failed.');
		channel.show(true);

		return emptyTasks;
	}

	function createTask(definition:DafTaskDefinition):vscode.Task
	{
		if (definition.build === "SQL")
		{
			definition.parameters += "-p:BuildSqlOnly=true ";
		}

		// This can be set to the ShellExecution as a second parameter, but currently only works with tasks.json, not extension-provided build tasks.
		//let args: string[] = ['${input:buildConfiguration}'];

		// -v:n could be used for more verbose output.
		var execution = new vscode.ShellExecution(`dotnet build ${definition.projectFile} --configuration ${definition.configuration} ${definition.parameters}--no-incremental --nologo`);
		var task = new vscode.Task(definition, vscode.TaskScope.Workspace, definition.projectFile + ' - Build ' + definition.configuration + ' (' + definition.build + ')', 'daf', execution, '$msCompile');

		task.group = vscode.TaskGroup.Build;

		//task.runOptions.reevaluateOnRerun = true; // Doesn't seem to do what I hoped it would do.

		task.presentationOptions.clear = true;

		return task;
	}
}