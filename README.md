# Data Automation Framework - Core (Daf Core)
**Note: This project is currently in an alpha state and should be considered unstable. Breaking changes to the public API will occur.**

Daf is a plugin-based data and integration automation framework primarily designed to facilitate data warehouse and ETL processes. Developers use this framework to programatically generate data integration objects using the Daf templating language.

This core library provides the following:
* A Visual Studio Code (VSCode) extension for the Daf templating language. 
* A language server for the Daf templating language for auto completion when developing Daf templates.
* Executing Daf plugins to generate files used in the data integration process, such as definitions of database tables, SSIS packages and Azure Data Factory scripts. 

## Installation
The core library is built using .NET and is cross platform. 

Currently, the easiest way to grab the core library, along with the VSCode extension is from the VSCode marketplace. There is also an archive containing just the build system in the Github release section, which is recommended when running Daf on a build server that doesn't have VSCode installed. The extension has the following requirements:
* .NET 5.0 runtime
* Visual Studio Code (VSCode)

Steps: 
* Go to https://marketplace.visualstudio.com/items?itemName=data-automation-framework.daf-core
* Install the extension.


## Example usage
Create new Daf project after having installed the extension: 
* In a empty workspace in VSCode, open the command pallete (Ctrl+Shift+P) and select option _Create a Daf project file_.
* In the project file add a new ItemGroup containing a nuget package reference to a Daf plugin:
  ```
  <ItemGroup>
    <PackageReference Include="Daf.Core.Ssis" Version="*" />
  <ItemGroup>
  ```
* Add a new root file with extension .daf. The first line in this file must contain three dashes (---) to indicate that it is the root file of the solution.
* Start coding:
  ![demo](https://user-images.githubusercontent.com/1073539/112722549-93613b80-8f0a-11eb-9495-2786007df9ff.gif)
* Build the project (Ctrl+Shift+B) and select an appropiate build action, e.g. _Build Dev (Full)_.
* For the standard plugins, the output is generated in the underlying bin folder.

Also see the sample projects in this repository.

## Links
[Daf organization](https://github.com/data-automation-framework)
[Documentation](https://data-automation-framework.com)
