using System.Reflection;
using Waher.Script;
using Waher.Script.Content;
using Waher.Script.Content.Functions.Encoding;
using Waher.Script.Functions.Runtime;
using Waher.Script.Model;
using Waher.Script.Operators;
using Waher.Script.Operators.Assignments;
using Waher.Script.Operators.Membership;
using Waher.Script.Persistence.Functions;
using NeuroAccessMaui.Exceptions;

namespace NeuroAccessMaui.Extensions
{
	public static class ScriptExtentions
	{
		private static readonly Assembly scriptContent = typeof(GraphEncoder).Assembly;
		private static readonly Assembly scriptPersistence = typeof(IncCounter).Assembly;
		private static readonly Assembly[] prohibitedAssemblies = new Assembly[]
		{
			scriptContent,		// Waher.Script.Content
			scriptPersistence,	// Waher.Script.Persistence
		};

		/// <summary>
		/// Parses and evaluates a script expression after validating it is safe to execute.
		/// </summary>
		/// <param name="StringExpression">Expression to parse and evaluate.</param>
		/// <param name="Variables">Variables available during evaluation.</param>
		/// <returns>Result of the evaluated expression.</returns>
		/// <exception cref="DisallowedScriptException">Thrown if the expression contains prohibited constructs.</exception>
		public static async Task<object> SafeExecute(string StringExpression, Variables Variables)
		{
			Expression Exp = new Expression(StringExpression);
			return await SafeExecute(Exp, Variables);
		}

		/// <summary>
		/// Evaluates a parsed script expression after validating it is safe to execute.
		/// </summary>
		/// <param name="Exp">Parsed expression to evaluate.</param>
		/// <param name="Variables">Variables available during evaluation.</param>
		/// <returns>Result of the evaluated expression.</returns>
		/// <exception cref="DisallowedScriptException">Thrown if the expression contains prohibited constructs.</exception>
		public static async Task<object> SafeExecute(Expression Exp, Variables Variables)
		{
			if (!CheckExpressionSafe(Exp, out ScriptNode? Prohibited))
				throw new DisallowedScriptException("Script not allowed.", Prohibited);
			return await Exp.EvaluateAsync(Variables);
		}

		/// <summary>
		/// Checks if an expression is safe to execute (if it comes from an external source).
		/// </summary>
		/// <param name="Expression">Parsed expression.</param>
		/// <param name="Prohibited">Element that is prohibited.</param>
		/// <returns>If the expression is safe to execute.</returns>
		public static bool CheckExpressionSafe(Expression Expression, out ScriptNode? Prohibited)
		{
			return CheckExpressionSafe(Expression, false, false, false, out Prohibited);
		}

		/// <summary>
		/// Checks if an expression is safe to execute (if it comes from an external source).
		/// </summary>
		/// <param name="Expression">Parsed expression.</param>
		/// <param name="AllowNamedMembers">If named members are allowed.</param>
		/// <param name="AllowError">If error function is allowed.</param>
		/// <param name="AllowCustomFunctions">If custom functions are allowed.</param>
		/// <param name="Prohibited">Element that is prohibited.</param>
		/// <returns>If the expression is safe to execute.</returns>
		private static bool CheckExpressionSafe(Expression Expression, bool AllowNamedMembers, bool AllowError,
				bool AllowCustomFunctions, out ScriptNode? Prohibited)
		{
#if DEBUG
			Prohibited = null;
			return true;
#endif // DEBUG

			ScriptNode? Prohibited2 = null;
			bool Safe = Expression.ForAll((ScriptNode Node, out ScriptNode? NewNode, object State) =>
			{
				NewNode = null;

				Assembly Assembly = Node.GetType().Assembly;

				foreach (Assembly A in prohibitedAssemblies)
				{
					if (A.FullName == Assembly.FullName)
					{
						if (A == scriptContent)
						{
							if (Node is Waher.Script.Content.Functions.Duration ||
								Node.GetType().Namespace == typeof(Utf8Encode).Namespace)
							{
								return true;
							}
						}
						else if (A == scriptPersistence)
						{
							if (Node is IncCounter ||
								Node is DecCounter ||
								Node is GetCounter ||
								Node is GetHashObject ||
								Node is PersistHash ||
								Node is VerifyHash)
							{
								return true;
							}
						}

						Prohibited2 = Node;
						return false;
					}
				}

				if ((Node is NamedMember && !AllowNamedMembers) ||
					(Node is NamedMemberAssignment && !AllowNamedMembers) ||
					(Node is LambdaDefinition && !AllowCustomFunctions) ||
					Node is NamedMethodCall ||
					Node is DynamicFunctionCall ||
					Node is DynamicMember ||
					Node is Create ||
					Node is Destroy ||
					(Node is Error && !AllowError))
				{
					Prohibited2 = Node;
					return false;
				}

				return true;

			}, null, SearchMethod.TreeOrder);

			Prohibited = Prohibited2;
			return Safe;
		}
	}
}
