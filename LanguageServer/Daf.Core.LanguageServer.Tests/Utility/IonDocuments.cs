// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

namespace Daf.Core.LanguageServerTests.Utility
{
	public static class IonDocuments
	{
		public const string SampleRootDocumentText =
@"---
IntegrationTestPlugin:
	SsisProjects:	#__Comment1__
		SsisProject: Name=Staging
			Packages:
				<#
				for (int i = 0; i < 10; i++) {
				#>
				Package: Name=SsisPackage_<#=i#>
					Connections:
						FlatFileConnection: Name=Export_FileConnection ConnectionString=""C:\Export\SystemName\ImportName"" Unicode=true
							FlatFileColumns:
								FlatFileColumn: Name=Column1
												DataType=DateTimeOffset
												Delimiter=;
								FlatFileColumn: Name=Column2 DataType=String Delimiter=;
								FlatFileColumn: Name=Column3
												DataType=String
												Delimiter=;
								FlatFileColumn: Name=Column4 DataType=String Delimiter=CRLF
					Tasks:	#__Comment2__
						SequenceContainer: Name=Export	#__Comment3__
							Tasks:
								ExecuteSql: Name=""Test"" ConnectionName=Test_DW
									SqlParameters:
										SqlParameter: Direction=Input Size=-1 DataType=AnsiString variablename=CaseInsensitiveTest ParameterName=0
										SqlParameter: Direction=Input Size=-1 DataType=AnsiString VariableName=User::LoadDate ParameterName=1
									SqlStatement: Statement=""
															SELECT Column1,
															Column2,
															Column3,
															CAST(Column4 AS nvarchar(20)) AS Col4 FROM schema.table;
															""
								Expression: Name=""Set FileName"" ExpressionValue=""@[User::FileName] = @[$Project::RootFilePath] + @[User::FileName] + &quot;_&quot; +  REPLACE(LEFT( @[$Package::LoadDate] , 10), &quot;-&quot;, &quot;&quot;) + &quot;.csv&quot;""
									PrecedenceConstraints:
										Inputs:
											InputPath: OutputPathName=""1""
								executesql: Name=case_insensitive_test ConnectionName=test
				<#
				}
				#>
SsdtProjects:
	SsdtProject: Name=TestProject
		Database: Name=TestDB
			Tables:
				Table: Name=TestTable Schema=test
";

		public static readonly string MixedLineBreaksDocumentText =
"A" + (char)10 +                //LF	- LineFeed
"B" + (char)13 +                //CR	- CarriageReturn
"C" + (char)13 + (char)10 +     //CRLF	- CarriageReturn + LineFeed
"D";

		public const string NamespaceWithRootNodeDocumentText =
@"---
SsisProjects:	
	SsisProject:
		DummyClass: Name=""ASD""
			DafCoreSsisPlugin.DafCoreSsisPlugin.DummyComponent: DummyEnum=SsisEnumVal1
			DummyComponent: DummyEnum=SsisEnumVal1
";

		public const string SampleNonRootDocumentText =
@"Tasks:
	SequenceContainer: Name=Container1
		Tasks:
			ExecuteSql: Name=""ExecSqlTest"" ConnectionName=Test_DW
				SqlParameters:
					SqlParameter: Direction=Input Size=-1 DataType=AnsiString Variablename=TestParameter ParameterName=0
				SqlStatement: Statement=<!
										SELECT Column1,
										Column2,
										Column3,
										CAST(Column4 AS nvarchar(20)) AS Col4 FROM schema.table;
										!>
			Expression: Name=""ExpressionTest"" ExpressionValue=""ExpressionTestValue""
				PrecedenceConstraints:
					Inputs:
						InputPath: OutputPathName=""1""
";
	}

}
