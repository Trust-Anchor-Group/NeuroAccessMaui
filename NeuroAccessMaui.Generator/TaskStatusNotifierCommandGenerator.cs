using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeuroAccessMaui.Generator
{
	[Generator(LanguageNames.CSharp)]
	public class TaskStatusNotifierCommandGenerator : IIncrementalGenerator
	{
		// Diagnostic for successfully generating a command.
		private static readonly DiagnosticDescriptor debugGeneratedCommand = new DiagnosticDescriptor(
			  id: "NTSCG001",
			  title: "Command Generated",
			  messageFormat: "Generated command property '{0}' for method '{1}' in class '{2}'",
			  category: "TaskStatusNotifierCommandGenerator",
			  defaultSeverity: DiagnosticSeverity.Info,
			  isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor unsupportedSignature = new DiagnosticDescriptor(
			 id: "NTSCG002",
			 title: "Unsupported Method Signature",
			 messageFormat: "Method '{0}' in class '{1}' has an unsupported signature for command generation",
			 category: "TaskStatusNotifierCommandGenerator",
			 defaultSeverity: DiagnosticSeverity.Error,
			 isEnabledByDefault: true);

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			// Create a provider for all method declarations with any attribute.
			IncrementalValuesProvider<MethodDeclarationSyntax> MethodDeclarations =
				  context.SyntaxProvider.CreateSyntaxProvider(
						 predicate: static (node, _) =>
							 node is MethodDeclarationSyntax M && M.AttributeLists.Count > 0,
						 transform: static (ctx, _) => (MethodDeclarationSyntax)ctx.Node)
				  .Where(method => method != null);

			// Combine the methods with the Compilation.
			IncrementalValueProvider<(Compilation, ImmutableArray<MethodDeclarationSyntax>)> CompilationAndMethods =
				  context.CompilationProvider.Combine(MethodDeclarations.Collect());

			// Register the source output.
			context.RegisterSourceOutput(CompilationAndMethods, (spc, source) =>
			{
				(Compilation Compilation, ImmutableArray<MethodDeclarationSyntax> Methods) = source;
				// Get the attribute symbol.
				INamedTypeSymbol? AttributeSymbol = Compilation.GetTypeByMetadataName("NeuroAccessMaui.UI.MVVM.TaskStatusNotifierCommandAttribute");
				if (AttributeSymbol == null)
				{
					// If the attribute isn’t found, report an informational diagnostic.
					spc.ReportDiagnostic(Diagnostic.Create(
						  new DiagnosticDescriptor(
								 id: "NTSCG000",
								 title: "Attribute Not Found",
								 messageFormat: "The attribute 'TaskStatusNotifierCommandAttribute' was not found.",
								 category: "TaskStatusNotifierCommandGenerator",
								 defaultSeverity: DiagnosticSeverity.Info,
								 isEnabledByDefault: true),
						  Location.None));
					return;
				}

				foreach (MethodDeclarationSyntax MethodDeclaration in Methods)
				{
					SemanticModel SemanticModel = Compilation.GetSemanticModel(MethodDeclaration.SyntaxTree);
					if (SemanticModel.GetDeclaredSymbol(MethodDeclaration) is not IMethodSymbol MethodSymbol)
						continue;

					// Check if the method is decorated with our attribute.
					var attrData = MethodSymbol.GetAttributes()
						  .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, AttributeSymbol));
					if (attrData == null)
						continue;

					// Get containing type info.
					INamedTypeSymbol? ContainingType = MethodSymbol.ContainingType;
					string NamespaceName = ContainingType.ContainingNamespace.ToDisplayString();
					string ClassName = ContainingType.Name;
					string MethodName = MethodSymbol.Name;

					// Generate command property name (remove "Async" suffix if present).
					string CommandName = MethodName.EndsWith("Async")
						  ? MethodName.Substring(0, MethodName.Length - 5) + "Command"
						  : MethodName + "Command";

					// Determine the result type.
					string ResultType;
					bool ReturnsGenericTask = false;
					if (MethodSymbol.ReturnType is INamedTypeSymbol NamedType)
					{
						if (NamedType.ConstructedFrom.ToDisplayString() == "System.Threading.Tasks.Task<TResult>")
						{
							ReturnsGenericTask = true;
							ResultType = NamedType.TypeArguments[0].ToDisplayString();
						}
						else if (NamedType.ToDisplayString() == "System.Threading.Tasks.Task")
						{
							ResultType = "object";
						}
						else
						{
							// Not a Task type; skip generation.
							spc.ReportDiagnostic(Diagnostic.Create(unsupportedSignature, MethodDeclaration.GetLocation(), MethodName, ClassName));
							continue;
						}
					}
					else
					{
						spc.ReportDiagnostic(Diagnostic.Create(unsupportedSignature, MethodDeclaration.GetLocation(), MethodName, ClassName));
						continue;
					}

					// Determine the progress type.
					// Now we support either a single parameter of type TaskContext<TProgress> or no parameters.
					string ProgressType = "object"; // default fallback
					if (MethodSymbol.Parameters.Length == 1)
					{
						IParameterSymbol P0 = MethodSymbol.Parameters[0];
						if (P0.Type is INamedTypeSymbol ContextSymbol &&
							 ContextSymbol is { Name: "TaskContext", TypeArguments.Length: 1 })
						{
							ProgressType = ContextSymbol.TypeArguments[0].ToDisplayString();
						}
						else
						{
							spc.ReportDiagnostic(Diagnostic.Create(unsupportedSignature, MethodDeclaration.GetLocation(), MethodName, ClassName));
							continue;
						}
					}
					else if (MethodSymbol.Parameters.Length == 0)
					{
						// Parameterless method: we fall back to object.
						ProgressType = "object";
					}
					else
					{
						spc.ReportDiagnostic(Diagnostic.Create(unsupportedSignature, MethodDeclaration.GetLocation(), MethodName, ClassName));
						continue;
					}

					// Get the options value from the attribute.
					// Default is "TaskStatusNotifierCommandOptions.None".
					string OptionsValue = "TaskStatusNotifierCommandOptions.None";
					if (attrData.ConstructorArguments.Length > 0 && attrData.ConstructorArguments[0].Value is int value)
					{
						// We output the integer value as a cast to the enum.
						OptionsValue = $"((TaskStatusNotifierCommandOptions){value})";
					}
					else
					{
						// Also check for named arguments.
						foreach (var namedArg in attrData.NamedArguments)
						{
							if (namedArg.Key == "Options" && namedArg.Value.Value is int n)
							{
								OptionsValue = $"((TaskStatusNotifierCommandOptions){n})";
								break;
							}
						}
					}

					// Generate the lambda body.
					string LambdaBody = "";
					if (MethodSymbol.Parameters.Length == 0)
					{
						// Parameterless method.
						LambdaBody = ReturnsGenericTask
							  ? $"return await {MethodName}();"
							  : $"await {MethodName}(); return default!;";
					}
					else if (MethodSymbol.Parameters.Length == 1)
					{
						// Method with TaskContext<TProgress> parameter.
						LambdaBody = ReturnsGenericTask
							  ? $"return await {MethodName}(context);"
							  : $"await {MethodName}(context); return default!;";
					}
					else
					{
						spc.ReportDiagnostic(Diagnostic.Create(unsupportedSignature, MethodDeclaration.GetLocation(), MethodName, ClassName));
						continue;
					}

					// Build the generated source code using the extracted progress type and options.
					string GeneratedSource = $@"
using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.MVVM;

namespace {NamespaceName}
{{
    public partial class {ClassName}
    {{
        private TaskStatusNotifierCommand<{ResultType}, {ProgressType}>? _{CommandName};
        public TaskStatusNotifierCommand<{ResultType}, {ProgressType}> {CommandName} => _{CommandName} ??= new TaskStatusNotifierCommand<{ResultType}, {ProgressType}>(
            async (context) =>
            {{
                {LambdaBody}
            }},
            null,
            {OptionsValue});
    }}
}}
";
					// Add the generated source.
					spc.AddSource($"{ClassName}_{CommandName}_generated.cs", GeneratedSource);
					// Report diagnostic that command generation succeeded.
					spc.ReportDiagnostic(Diagnostic.Create(debugGeneratedCommand, MethodDeclaration.GetLocation(), CommandName, MethodName, ClassName));
				}
			});
		}
	}
}
