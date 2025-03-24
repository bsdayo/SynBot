using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SynBot.Copilot.Utilities;

namespace SynBot.Copilot.Generators;

[Generator(LanguageNames.CSharp)]
public class CommandIndexGenerator : IIncrementalGenerator
{
    private const string CommandAttributeFullName = "SynBot.Attributes.CommandAttribute";
    private const string OptionAttributeFullName = "SynBot.Attributes.OptionAttribute";
    private const string ArgumentAttributeFullName = "SynBot.Attributes.ArgumentAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            CommandAttributeFullName,
            (syntax, _) => syntax is ClassDeclarationSyntax,
            (ctx, _) => ctx);

        context.RegisterSourceOutput(provider.Collect(), GenerateCommandSource);
    }

    private void GenerateCommandSource(SourceProductionContext sources,
        ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
    {
        var propDefsSb = new StringBuilder();
        var propInitSb = new StringBuilder();
        var factoriesSb = new StringBuilder();
        var validatorsSb = new StringBuilder();
        var definedCommands = new HashSet<string>();

        // For every class with [Command]
        foreach (var context in contexts)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
            var classPropertyDeclarations = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();

            var commandPath = context.Attributes[0].ConstructorArguments[0].Values
                .Select(val => val.Value)
                .OfType<string>()
                .ToArray();
            var commandClassName = classDeclaration.Identifier.Text;

            propInitSb.AppendIndent(2).AppendLine($"// {commandClassName} => #{string.Join(" ", commandPath)}");

            var currentCommandIdentifier = "";
            for (var i = 0; i < commandPath.Length; i++)
            {
                var parentCommandExpr = i == 0 ? "_rootCommand" : $"_{currentCommandIdentifier}Command";
                if (i != 0)
                    currentCommandIdentifier += '_';
                currentCommandIdentifier += commandPath[i];

                // If this path of command is not defined previously
                if (!definedCommands.Contains(currentCommandIdentifier))
                {
                    propDefsSb.AppendIndent(1).AppendLine($"private Command _{currentCommandIdentifier}Command;");
                    propInitSb.AppendIndent(2)
                        .AppendLine($"_{currentCommandIdentifier}Command = new Command(\"{commandPath[i]}\");");
                    propInitSb.AppendIndent(2)
                        .AppendLine($"{parentCommandExpr}.AddCommand(_{currentCommandIdentifier}Command);");
                }

                definedCommands.Add(currentCommandIdentifier);
            }

            factoriesSb.AppendIndent(2).AppendLine(
                $"_factories[_{currentCommandIdentifier}Command] = (services, result) => {{");
            factoriesSb.AppendIndent(3).AppendLine(
                $"var {currentCommandIdentifier}Instance = ActivatorUtilities.CreateInstance<{commandClassName}>(services);");

            // For every property of the class with [Command]
            foreach (var propertyDeclaration in classPropertyDeclarations)
            {
                // Get property symbol
                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration)!;

                string? symbolType = null;
                var propertyName = propertySymbol.Name;
                var propertyType = propertySymbol.Type.Name;
                var mainArg = "\"<unknown>\"";
                string? descriptionExpr = null;

                var propertyInitializer = propertyDeclaration.Initializer;
                var defaultValueIsNull = propertyInitializer?.Value.DescendantNodesAndSelf()
                    .Any(node => node is LiteralExpressionSyntax literal &&
                                 literal.IsKind(SyntaxKind.NullLiteralExpression));
                var defaultValueExpr = defaultValueIsNull == true ? null : propertyInitializer?.Value.ToString();

                var attributes = propertyDeclaration.AttributeLists
                    .SelectMany(attributeList => attributeList.Attributes)
                    .Select(attribute => (Node: attribute,
                        FullName: context.SemanticModel.GetSymbolInfo(attribute).Symbol!.ContainingSymbol.ToString()));

                foreach (var attribute in attributes)
                {
                    if (attribute.Node.ArgumentList?.Arguments is not { } attributeArgs)
                        continue;

                    switch (attribute.FullName)
                    {
                        case OptionAttributeFullName:
                            mainArg = attributeArgs[0].ToString();
                            if (attributeArgs.Count > 1)
                                descriptionExpr = attributeArgs[1].ToString();
                            symbolType = "Option";
                            break;

                        case ArgumentAttributeFullName:
                            mainArg = attributeArgs[0].ToString();
                            if (attributeArgs.Count > 1)
                                descriptionExpr = attributeArgs[1].ToString();
                            symbolType = "Argument";
                            break;
                    }
                }

                if (symbolType is not null)
                {
                    var generatedProp = $"_{currentCommandIdentifier}Command_{propertyName}{symbolType}";

                    propDefsSb.AppendIndent(1).AppendLine(
                        $"private {symbolType}<{propertyType}> {generatedProp};");

                    propInitSb.AppendIndent(2).AppendLine(
                        $"{generatedProp} = new {symbolType}<{propertyType}>({mainArg});");
                    if (descriptionExpr is not null)
                        propInitSb.AppendIndent(2).AppendLine(
                            $"{generatedProp}.Description = {descriptionExpr};");
                    if (defaultValueExpr is not null)
                        propInitSb.AppendIndent(2).AppendLine(
                            $"{generatedProp}.SetDefaultValue({defaultValueExpr});");
                    propInitSb.AppendIndent(2).AppendLine(
                        $"_{currentCommandIdentifier}Command.Add{symbolType}({generatedProp});");

                    factoriesSb.AppendIndent(3).AppendLine(
                        $"{currentCommandIdentifier}Instance.{propertyName} = result.GetValueFor{symbolType}({generatedProp});");
                }
            }

            factoriesSb.AppendIndent(3).AppendLine($"return {currentCommandIdentifier}Instance;");
            factoriesSb.AppendIndent(2).AppendLine("};");

            validatorsSb.AppendIndent(2).AppendLine(
                $"_validators[typeof({commandClassName})] = (services, instance) => {{");
            validatorsSb.AppendIndent(3).AppendLine(
                $"return services.GetService<IValidator<{commandClassName}>>()?.Validate(({commandClassName})instance);");
            validatorsSb.AppendIndent(2).AppendLine("};");
        }

        var source = // lang=csharp
            $$"""
              /// <auto-generated />

              using System.CommandLine;
              using System.CommandLine.Parsing;
              using FluentValidation;
              using Microsoft.Extensions.DependencyInjection;
              using SynBot.Commands;

              namespace SynBot.Infrastructure;

              partial class CommandIndex
              {
              {{propDefsSb}}
                  public CommandIndex()
                  {
              {{propInitSb}}
              {{factoriesSb}}
              {{validatorsSb}}
                  }
              }
              """;
        sources.AddSource("CommandIndex.g.cs", source);
    }
}