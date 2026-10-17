using Waher.Script;
using Waher.Script.Model;
using Waher.Script.Operators.Assignments;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Checks the scripts of a token's note commands before the app runs them. A token's author writes these scripts, so
	/// only pure calculations are allowed: constants, the command's parameters, variables the scripts assign themselves,
	/// operators, conditionals, XML literals and a list of functions without side effects. Anything that could reach a
	/// database, files, the network or .NET objects, or that could loop, is refused.
	/// </summary>
	public static class NoteScriptValidator
	{
		private static readonly HashSet<string> allowedAssemblies = new(StringComparer.Ordinal)
		{
			"Waher.Script",
			"Waher.Script.Content",
			"Waher.Script.Xml"
		};

		/// <summary>
		/// Namespaces whose script nodes are all allowed.
		/// </summary>
		private static readonly HashSet<string> allowedNamespaces = new(StringComparer.Ordinal)
		{
			"Waher.Script.Operators.Arithmetics",
			"Waher.Script.Operators.Binary",
			"Waher.Script.Operators.Comparisons",
			"Waher.Script.Operators.Dual",
			"Waher.Script.Operators.Logical",
			"Waher.Script.Operators.Assignments.Pre",
			"Waher.Script.Operators.Assignments.Post",
			"Waher.Script.Operators.Assignments.WithSelf",
			"Waher.Script.Functions.Analytic",
			"Waher.Script.Functions.ComplexNumbers",
			"Waher.Script.Functions.DateAndTime",
			"Waher.Script.Functions.Scalar",
			"Waher.Script.Functions.Vectors",
			"Waher.Script.Functions.Matrices",
			"Waher.Script.Functions.Strings",
			"Waher.Script.Xml.Model",
			"Waher.Script.Xml.Functions"
		};

		/// <summary>
		/// Script nodes allowed one by one, from namespaces where other nodes are refused.
		/// </summary>
		private static readonly HashSet<string> allowedTypes = new(StringComparer.Ordinal)
		{
			"Waher.Script.Model.ConstantElement",
			"Waher.Script.Model.VariableReference",
			"Waher.Script.Operators.ElementList",
			"Waher.Script.Operators.Sequence",
			"Waher.Script.Operators.ObjectExNihilo",
			"Waher.Script.Operators.DynamicIndex",
			"Waher.Script.Operators.SetUnit",
			"Waher.Script.Operators.CreateMeasurement",
			"Waher.Script.Operators.Assignments.Assignment",
			"Waher.Script.Operators.Assignments.VectorIndexAssignment",
			"Waher.Script.Operators.Assignments.MatrixIndexAssignment",
			"Waher.Script.Operators.Assignments.MatrixColumnAssignment",
			"Waher.Script.Operators.Assignments.MatrixRowAssignment",
			"Waher.Script.Operators.Assignments.DynamicIndexAssignment",
			"Waher.Script.Operators.Conditional.If",
			"Waher.Script.Operators.Conditional.NullCheck",
			"Waher.Script.Operators.Conditional.TryCatch",
			"Waher.Script.Operators.Conditional.TryCatchFinally",
			"Waher.Script.Operators.Conditional.TryFinally",
			"Waher.Script.Operators.Membership.In",
			"Waher.Script.Operators.Membership.NotIn",
			"Waher.Script.Operators.Membership.Is",
			"Waher.Script.Operators.Membership.IsNot",
			"Waher.Script.Operators.Membership.Matches",
			"Waher.Script.Operators.Vectors.VectorDefinition",
			"Waher.Script.Operators.Vectors.VectorIndex",
			"Waher.Script.Operators.Matrices.MatrixDefinition",
			"Waher.Script.Operators.Matrices.MatrixIndex",
			"Waher.Script.Operators.Matrices.ColumnVector",
			"Waher.Script.Operators.Matrices.RowVector",
			"Waher.Script.Operators.Matrices.Transpose",
			"Waher.Script.Operators.Sets.SetDefinition",
			"Waher.Script.Operators.Sets.Union",
			"Waher.Script.Operators.Sets.Intersection",
			"Waher.Script.Functions.Runtime.Error",
			"Waher.Script.Functions.Runtime.Exists",
			"Waher.Script.Functions.Runtime.Return",
			"Waher.Script.Content.Functions.Duration",
			"Waher.Script.Content.Functions.PrettyJson",
			"Waher.Script.Content.Functions.Encoding.Base32Decode",
			"Waher.Script.Content.Functions.Encoding.Base32Encode",
			"Waher.Script.Content.Functions.Encoding.Base64Decode",
			"Waher.Script.Content.Functions.Encoding.Base64Encode",
			"Waher.Script.Content.Functions.Encoding.Base64UrlDecode",
			"Waher.Script.Content.Functions.Encoding.Base64UrlEncode",
			"Waher.Script.Content.Functions.Encoding.HtmlAttributeEncode",
			"Waher.Script.Content.Functions.Encoding.HtmlValueEncode",
			"Waher.Script.Content.Functions.Encoding.JsonDecode",
			"Waher.Script.Content.Functions.Encoding.JsonEncode",
			"Waher.Script.Content.Functions.Encoding.UrlDecode",
			"Waher.Script.Content.Functions.Encoding.UrlEncode",
			"Waher.Script.Content.Functions.Encoding.Utf8Decode",
			"Waher.Script.Content.Functions.Encoding.Utf8Encode",
			"Waher.Script.Content.Functions.Encoding.XmlDecode",
			"Waher.Script.Content.Functions.Encoding.XmlEncode"
		};

		/// <summary>
		/// Script nodes refused even though their namespace is allowed: they run text as script, parse text as XML, or
		/// create vectors and matrices of any size.
		/// </summary>
		private static readonly HashSet<string> refusedTypes = new(StringComparer.Ordinal)
		{
			"Waher.Script.Functions.Strings.Evaluate",
			"Waher.Script.Functions.Strings.Parse",
			"Waher.Script.Functions.Vectors.Ones",
			"Waher.Script.Functions.Vectors.Zeroes",
			"Waher.Script.Functions.Matrices.Identity",
			"Waher.Script.Functions.Matrices.Ones",
			"Waher.Script.Functions.Matrices.Zeroes",
			"Waher.Script.Xml.Functions.Xml"
		};

		/// <summary>
		/// Constants a script may refer to by name. Other names that aren't parameters or assigned by the scripts are
		/// refused, since a name can also refer to a function, a namespace or a type.
		/// </summary>
		private static readonly HashSet<string> allowedConstants = new(StringComparer.Ordinal)
		{
			"e", "π", "pi", "i", "∞", "infinity", "inf", "ε", "eps", "epsilon", "∅", "EmptySet",
			"Now", "NowUtc", "Today", "TodayUtc", "Time", "TimeUtc"
		};

		/// <summary>
		/// Checks if a set of scripts may run.
		/// </summary>
		/// <param name="Scripts">The parsed scripts. Null entries are skipped.</param>
		/// <param name="ParameterNames">Names of the command's parameters.</param>
		/// <param name="AssignedNames">Names the scripts assign that aren't parameters. Set them to null before running the
		/// scripts, so they can't refer to anything else.</param>
		/// <returns>If every node of every script is allowed.</returns>
		public static bool IsAllowed(IEnumerable<Expression?> Scripts, IEnumerable<string> ParameterNames, out string[] AssignedNames)
		{
			AssignedNames = [];

			Expression[] Parsed = [.. Scripts.Where(Script => Script is not null).Cast<Expression>()];
			HashSet<string> Parameters = new(ParameterNames, StringComparer.Ordinal);
			ValidationState State = new(Parameters);

			// The first pass collects the names that are assigned, so they can be used anywhere.
			foreach (Expression Script in Parsed)
			{
				if (!Script.ForAll(CollectAssignedName, State, SearchMethod.DepthFirst))
					return false;
			}

			foreach (Expression Script in Parsed)
			{
				if (!Script.ForAll(CheckNode, State, SearchMethod.DepthFirst) || !State.Allowed)
					return false;
			}

			AssignedNames = [.. State.Assigned.Where(Name => !Parameters.Contains(Name))];
			return true;
		}

		/// <summary>
		/// Collects the name of a variable that a node assigns.
		/// </summary>
		/// <param name="Node">The script node.</param>
		/// <param name="NewNode">Always null, since the script isn't changed.</param>
		/// <param name="State">The validation state.</param>
		/// <returns>If the walk should continue.</returns>
		private static bool CollectAssignedName(ScriptNode Node, out ScriptNode? NewNode, object? State)
		{
			NewNode = null;

			if (Node is Assignment Assign && State is ValidationState Validation && !string.IsNullOrEmpty(Assign.VariableName))
				Validation.Assigned.Add(Assign.VariableName);

			return true;
		}

		/// <summary>
		/// Checks if a node is allowed.
		/// </summary>
		/// <param name="Node">The script node.</param>
		/// <param name="NewNode">Always null, since the script isn't changed.</param>
		/// <param name="State">The validation state.</param>
		/// <returns>If the walk should continue: only while every node is allowed.</returns>
		private static bool CheckNode(ScriptNode Node, out ScriptNode? NewNode, object? State)
		{
			NewNode = null;

			if (State is not ValidationState Validation)
				return false;

			if (!IsAllowedNode(Node, Validation))
			{
				Validation.Allowed = false;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if a node is of an allowed type, and if the variable it refers to, if any, is allowed.
		/// </summary>
		/// <param name="Node">The script node.</param>
		/// <param name="Validation">The validation state.</param>
		/// <returns>If the node is allowed.</returns>
		private static bool IsAllowedNode(ScriptNode? Node, ValidationState Validation)
		{
			if (Node is null)
				return true;

			Type T = Node.GetType();
			string? AssemblyName = T.Assembly.GetName().Name;
			string? FullName = T.FullName;

			if (AssemblyName is null || FullName is null || !allowedAssemblies.Contains(AssemblyName) || refusedTypes.Contains(FullName))
				return false;

			if (!allowedTypes.Contains(FullName) && (T.Namespace is null || !allowedNamespaces.Contains(T.Namespace)))
				return false;

			if (Node is VariableReference Reference)
			{
				string Name = Reference.VariableName;
				return Validation.Parameters.Contains(Name) || Validation.Assigned.Contains(Name) || allowedConstants.Contains(Name);
			}

			if (Node is ScriptLeafNodeVariableReference LeafReference)
			{
				string Name = LeafReference.VariableName;
				return Validation.Parameters.Contains(Name) || Validation.Assigned.Contains(Name);
			}

			return true;
		}

		/// <summary>
		/// What is known while scripts are checked.
		/// </summary>
		/// <param name="Parameters">Names of the command's parameters.</param>
		private sealed class ValidationState(HashSet<string> Parameters)
		{
			/// <summary>
			/// Names of the command's parameters.
			/// </summary>
			public HashSet<string> Parameters { get; } = Parameters;

			/// <summary>
			/// Names the scripts assign.
			/// </summary>
			public HashSet<string> Assigned { get; } = new(StringComparer.Ordinal);

			/// <summary>
			/// If every node checked so far is allowed.
			/// </summary>
			public bool Allowed { get; set; } = true;
		}
	}
}
