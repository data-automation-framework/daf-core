// SPDX-License-Identifier: MIT
// Copyright © 2021 Oscar Björhn, Petter Löfgren and contributors

using Daf.Core.Sdk;
using System.Collections.Generic;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
#pragma warning disable CA1720 // Identifier contains type name
#pragma warning disable CA1724 // Type names should not match namespaces
namespace Daf.Core.LanguageServer.Tests.Plugins
{
	/// <summary>
	/// Root node for the IntegrationTest plugin
	/// </summary>
	[IsRootNode]
	public class IntegrationTestPlugin
	{
		/// <summary>
		/// Collection of SsisProject
		/// </summary>
		public List<SsisProject> SsisProjectList { get; } = new();
	}

	/// <summary>
	/// Element in collection of all the SQL Server Integration Services (SSIS) projects.
	/// </summary>
	public class SsisProject
	{
		/// <summary>
		/// Collection of all the SQL Server Integration Services (SSIS) packages in the project.
		/// </summary>
		public List<Package> Packages { get; } = new();
	}

	/// <summary>
	/// Element in collection of all the SQL Server Integration Services (SSIS) packages in the project.
	/// </summary>
	public class Package
	{
		/// <summary>
		/// Collection of all the connections in the package.
		/// </summary>
		public List<Connection> Connections { get; } = new();

		/// <summary>
		/// Collection of all the tasks in the package.
		/// </summary>
		public List<Task> Tasks { get; } = new();

		/// <summary>
		/// The name of the object.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// The locale ID for the package. Uses the building operating system's regional settings if left empty, or the project-level LocaleId if it's set.
		/// </summary>
		[DefaultValue(0)]
		public int LocaleId { get; set; }

		/// <summary>
		/// Validation should be delayed until runtime.
		/// </summary>
		[DefaultValue(false)]
		public bool DelayValidation { get; set; }
	}

	/// <summary>
	/// Element in collection of child tasks.
	/// </summary>
	public abstract class Task
	{
		/// <summary>
		/// Collection of precedence constraints.
		/// </summary>
		public PrecedenceConstraintList? PrecedenceConstraints { get; set; }

		/// <summary>
		/// The name of the object.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Validation should be delayed until runtime.
		/// </summary>
		[DefaultValue(false)]
		public bool DelayValidation { get; set; }

		/// <summary>
		/// If false, an OnError EventHandler with Propagate = false is created for this task.
		/// </summary>
		[DefaultValue(true)]
		public bool PropagateErrors { get; set; }
	}

	/// <summary>
	/// Element in collection of precedence constraints.
	/// </summary>
	public class PrecedenceConstraintList
	{
		/// <summary>
		/// Collection of precedence constraint input paths.
		/// </summary>
		public List<InputPath> Inputs { get; } = new();

		/// <summary>
		/// Is this an "And" or an "Or" precedence constraint list?
		/// </summary>
		[DefaultValue(LogicalOperationEnum.And)]
		public LogicalOperationEnum LogicalType { get; set; }
	}

	/// <summary>
	/// Element in collection of precedence constraint input paths.
	/// </summary>
	public class InputPath
	{
		public string? OutputPathName { get; set; }

		/// <summary>
		/// The evaluation operation.
		/// </summary>
		[DefaultValue(TaskEvaluationOperationTypeEnum.Constraint)]
		public TaskEvaluationOperationTypeEnum EvaluationOperation { get; set; }

		/// <summary>
		/// The evaluation value.
		/// </summary>
		[DefaultValue(TaskEvaluationOperationValueEnum.Success)]
		public TaskEvaluationOperationValueEnum EvaluationValue { get; set; }

		/// <summary>
		/// The SSIS expression that must evaluate to "True" for execution to continue along this path
		/// </summary>
		public string? Expression { get; set; }
	}

	/// <summary>
	/// Element in collection of all the connections in the project.
	/// </summary>
	public class Connection
	{
		/// <summary>
		/// The connection string to use.
		/// </summary>
		public string? ConnectionString { get; set; }

		/// <summary>
		/// The name of the object.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Pre-defined GUID for the connection. Use this if you want the connection to work with incremental builds, or if you want to be able to deploy packages individually.
		/// </summary>
		public string? GUID { get; set; }

		/// <summary>
		/// Validation should be delayed until runtime.
		/// </summary>
		[DefaultValue(false)]
		public bool DelayValidation { get; set; }
	}

	public class CustomConnection : Connection
	{
		/// <summary>
		/// The CreationName of the custom connection.
		/// </summary>
		public string? CreationName { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class OleDbConnection : Connection
	{
	}

	/// <summary>
	/// 
	/// </summary>
	public class FlatFileConnection : Connection
	{
		/// <summary>
		/// Collection of flat file columns.
		/// </summary>
		public List<FlatFileColumn> FlatFileColumns { get; } = new();

		/// <summary>
		/// The file's format (FixedWidth, Delimited or RaggedRight).
		/// </summary>
		[DefaultValue(FlatFileFormatEnum.Delimited)]
		public FlatFileFormatEnum Format { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DefaultValue(true)]
		public bool ColumnNamesInFirstDataRow { get; set; }

		/// <summary>
		/// The number of rows to skip before reading data.
		/// </summary>
		[DefaultValue(0)]
		public int HeaderRowsToSkip { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DefaultValue(true)]
		public bool Unicode { get; set; }

		/// <summary>
		/// The encoding of the file.
		/// </summary>
		[DefaultValue(65001)]
		public int CodePage { get; set; }

		/// <summary>
		/// The locale ID of the file. Uses the building operating system's regional settings if left empty.
		/// </summary>
		[DefaultValue(0)]
		public int LocaleId { get; set; }

		/// <summary>
		/// The text qualifier of the file.
		/// </summary>
		public string? TextQualifier { get; set; }
	}
	/// <summary>
	/// Element in collection of flat file columns.
	/// </summary>
	public class FlatFileColumn
	{
		/// <summary>
		/// The name of the object.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// The column's data type.
		/// </summary>
		[DefaultValue(DatabaseTypeEnum.Int32)]
		public DatabaseTypeEnum DataType { get; set; }

		/// <summary>
		/// The width of the column in the source file.
		/// </summary>
		[DefaultValue(0)]
		public int InputWidth { get; set; }

		/// <summary>
		/// The width of the column in the data flow.
		/// </summary>
		[DefaultValue(0)]
		public int OutputWidth { get; set; }

		/// <summary>
		/// The precision of this column's data type. This only applies to data type definitions that accept a precision parameter, such as Decimal.
		/// </summary>
		[DefaultValue(0)]
		public int Precision { get; set; }

		/// <summary>
		/// The scale of this column's data type. This only applies to data type definitions that accept a scale parameter, such as Decimal.
		/// </summary>
		[DefaultValue(0)]
		public int Scale { get; set; }

		/// <summary>
		/// The delimiter of the column.
		/// </summary>
		[DefaultValue(",")]
		public string? Delimiter { get; set; }

		/// <summary>
		/// The codepage of the column. This only applies to string-type columns.
		/// </summary>
		[DefaultValue(0)]
		public int CodePage { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DefaultValue(false)]
		public bool TextQualified { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[DefaultValue(false)]
		public bool FastParse { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class SequenceContainer : Task
	{
		/// <summary>
		/// Collection of child tasks.
		/// </summary>
		public List<Task> Tasks { get; } = new();
	}

	/// <summary>
	/// 
	/// </summary>
	public class ExecuteSql : Task
	{
		/// <summary>
		/// The SQL statement to execute.
		/// </summary>
		public SqlStatement? SqlStatement { get; set; }

		/// <summary>
		/// Collection of result-to-variable mappings for SQL query results.
		/// </summary>
		public List<Result> Results { get; } = new();

		/// <summary>
		/// Collection of variable-to-parameter mappings for SQL query parameters.
		/// </summary>
		public List<SqlParameter> SqlParameters { get; } = new();

		/// <summary>
		/// Specifies the connection used to access the data.
		/// </summary>
		public string? ConnectionName { get; set; }

		/// <summary>
		/// Specifies the format of the query results.
		/// </summary>
		[DefaultValue(ExecuteSqlResultSetEnum.None)]
		public ExecuteSqlResultSetEnum ResultSet { get; set; }

		/// <summary>
		/// Specifies how the task will perform value to variable type conversions.
		/// </summary>
		[DefaultValue(ExecuteSqlTypeConversionModeEnum.Allowed)]
		public ExecuteSqlTypeConversionModeEnum TypeConversionMode { get; set; }

		/// <summary>
		/// Indicates wheter the task should prepare the query before executing it.
		/// </summary>
		[DefaultValue(true)]
		public bool BypassPrepare { get; set; }

		/// <summary>
		/// Specifies the time-out value.
		/// </summary>
		[DefaultValue(0U)]
		public uint TimeOut { get; set; }

		/// <summary>
		/// Specifies the code page value.
		/// </summary>
		[DefaultValue(1252U)]
		public uint CodePage { get; set; }
	}

	/// <summary>
	/// Element in collection of result-to-variable mappings for SQL query results.
	/// </summary>
	public class Result
	{
		/// <summary>
		/// The name of the result to map into a variable.
		/// </summary>
		public string? ResultName { get; set; }

		/// <summary>
		/// The variable to map the result into.
		/// </summary>
		public string? VariableName { get; set; }
	}

	public class SqlStatement
	{
		public string? Value { get; set; }
	}

	/// <summary>
	/// Element in collection of variable-to-parameter mappings for SQL query parameters.
	/// </summary>
	public class SqlParameter
	{
		/// <summary>
		/// The variable to map into the specified query parameter.
		/// </summary>
		public string? VariableName { get; set; }

		/// <summary>
		/// The name of the query parameter to map the variable name against.
		/// </summary>
		public string? ParameterName { get; set; }

		/// <summary>
		/// The data type of the query parameter.
		/// </summary>
		public DatabaseTypeEnum DataType { get; set; }

		/// <summary>
		/// The direction of the query parameter.
		/// </summary>
		[DefaultValue(ParameterDirectionEnum.Input)]
		public ParameterDirectionEnum Direction { get; set; }

		/// <summary>
		/// The size (length) of the mapped parameter value. Only relevant when used with an applicable data type (string, binary etc).
		/// </summary>
		[DefaultValue("0")]
		public string? Size { get; set; }
	}

	public enum ExecuteSqlTypeConversionModeEnum
	{
		None,
		Allowed,
	}

	public enum ExecuteSqlResultSetEnum
	{
		None,
		SingleRow,
		Full,
		Xml,
	}

	/// <summary>
	/// 
	/// </summary>
	public class Expression : Task
	{
		/// <summary>
		/// Expression.
		/// </summary>
		public string? ExpressionValue { get; set; }
	}

	public enum ParameterDirectionEnum
	{
		Input,
		Output,
		ReturnValue,
	}


	public enum TaskEvaluationOperationTypeEnum
	{
		Constraint,
		Expression,
		ExpressionAndConstraint,
		ExpressionOrConstraint,
	}

	public enum TaskEvaluationOperationValueEnum
	{
		Success,
		Failure,
		Completion,
	}

	public enum LogicalOperationEnum
	{
		And,
		Or,
	}

	public enum FlatFileFormatEnum
	{
		Delimited,
		FixedWidth,
		RaggedRight,
	}

	public enum DatabaseTypeEnum
	{
		AnsiString,
		AnsiStringFixedLength,
		Binary,
		Byte,
		Boolean,
		Currency,
		Date,
		DateTime,
		DateTime2,
		DateTimeOffset,
		Decimal,
		Double,
		Guid,
		Int16,
		Int32,
		Int64,
		Object,
		SByte,
		Single,
		String,
		StringFixedLength,
		Time,
		UInt16,
		UInt32,
		UInt64,
		VarNumeric,
		Xml,
	}
}
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
#pragma warning restore CA1720 // Identifier contains type name
#pragma warning restore CA1724 // Type names should not match namespaces
