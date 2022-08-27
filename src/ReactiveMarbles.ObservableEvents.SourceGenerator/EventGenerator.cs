﻿// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using ReactiveMarbles.ObservableEvents.SourceGenerator.EventGenerators;
using ReactiveMarbles.ObservableEvents.SourceGenerator.EventGenerators.Generators;
using ReactiveMarbles.PropertyChanged.SourceGenerator;

using static ReactiveMarbles.ObservableEvents.SourceGenerator.SyntaxFactoryHelpers;

namespace ReactiveMarbles.ObservableEvents.SourceGenerator
{
    /// <summary>
    /// Generates Observables from events in specified types and namespaces.
    /// </summary>
    [Generator]
    public class EventGenerator : ISourceGenerator
    {
        private static readonly InstanceEventGenerator _eventGenerator = new();
        private static readonly StaticEventGenerator _staticEventGenerator = new();

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            var compilation = context.Compilation;

            var extensionMethodInvocations = new List<MethodDeclarationSyntax>();
            var staticMethodInvocations = new List<MethodDeclarationSyntax>();

            GetAvailableTypes(compilation, receiver, out var instanceNamespaceList, out var staticNamespaceList);

            GenerateEvents(context, _staticEventGenerator, true, staticNamespaceList, staticMethodInvocations);
            GenerateEvents(context, _eventGenerator, false, instanceNamespaceList, extensionMethodInvocations);

            GenerateEventExtensionMethods(context, extensionMethodInvocations);
        }

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private static void GenerateEventExtensionMethods(GeneratorExecutionContext context, List<MethodDeclarationSyntax> methodInvocationExtensions)
        {
            var classDeclaration = ClassDeclaration("ObservableGeneratorExtensions", new[] { SyntaxKind.InternalKeyword, SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword }, methodInvocationExtensions, 1);

            var namespaceDeclaration = NamespaceDeclaration("ReactiveMarbles.ObservableEvents", new[] { classDeclaration }, true);

            var compilationUnit = GenerateCompilationUnit(namespaceDeclaration);

            if (compilationUnit == null)
            {
                return;
            }

            context.AddSource("TestExtensions.FoundEvents.SourceGenerated.cs", SourceText.From(compilationUnit.ToFullString(), Encoding.UTF8));
        }

        private static void GetAvailableTypes(
            Compilation compilation,
            SyntaxReceiver receiver,
            out List<(Location Location, INamedTypeSymbol NamedType)> instanceNamespaceList,
            out List<(Location Location, INamedTypeSymbol NamedType)> staticNamespaceList)
        {
            var observableGeneratorExtensions = compilation.GetTypeByMetadataName("ReactiveMarbles.ObservableEvents.ObservableGeneratorExtensions");

            if (observableGeneratorExtensions == null)
            {
                throw new InvalidOperationException("Cannot find ReactiveMarbles.ObservableEvents.ObservableGeneratorExtensions");
            }

            instanceNamespaceList = new List<(Location Location, INamedTypeSymbol NamedType)>();
            staticNamespaceList = new List<(Location Location, INamedTypeSymbol NamedType)>();

            foreach (var invocation in receiver.Events)
            {
                var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);

                if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, observableGeneratorExtensions))
                {
                    continue;
                }

                if (methodSymbol.TypeArguments.Length != 1)
                {
                    continue;
                }

                if (methodSymbol.TypeArguments[0] is not INamedTypeSymbol callingSymbol)
                {
                    continue;
                }

                var location = Location.Create(invocation.SyntaxTree, invocation.Span);

                instanceNamespaceList.Add((location, callingSymbol));
            }

            foreach (var attribute in compilation.Assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.ToString() != "ReactiveMarbles.ObservableEvents.GenerateStaticEventObservablesAttribute")
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length == 0)
                {
                    continue;
                }

                if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol type)
                {
                    continue;
                }

                var location = attribute.ApplicationSyntaxReference == null ? Location.None : Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span);

                staticNamespaceList.Add((location, type));
            }
        }

        private static bool GenerateEvents(
            GeneratorExecutionContext context,
            IEventSymbolGenerator symbolGenerator,
            bool isStatic,
            IReadOnlyList<(Location Location, INamedTypeSymbol NamedType)> symbols,
            List<MethodDeclarationSyntax>? methodInvocationExtensions = null)
        {
            if (symbols.Count == 0)
            {
                return true;
            }

            var processedItems = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            var fileType = isStatic ? "Static" : "Instance";

            var rootContainingSymbols = symbols.Select(x => x.NamedType).ToImmutableSortedSet(TypeDefinitionNameComparer.Default);

            bool hasEvents = false;

            foreach (var (location, item) in symbols)
            {
                if (processedItems.Contains(item))
                {
                    continue;
                }

                processedItems.Add(item);

                var namespaceItem = symbolGenerator.Generate(item);

                if (namespaceItem == null)
                {
                    continue;
                }

                hasEvents = true;

                var compilationUnit = GenerateCompilationUnit(namespaceItem);

                if (compilationUnit == null)
                {
                    continue;
                }

                var sourceText = compilationUnit.ToFullString();

                var name = $"SourceClass{item.ToDisplayString(RoslynHelpers.SymbolDisplayFormat)}-{fileType}Events.SourceGenerated.cs";

                context.AddSource(
                    name,
                    SourceText.From(sourceText, Encoding.UTF8));

                methodInvocationExtensions?.Add(item.GenerateMethod());
            }

            if (!hasEvents)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticWarnings.EventsNotFound, symbols.First().Location));
            }

            return true;
        }

        private static CompilationUnitSyntax? GenerateCompilationUnit(params NamespaceDeclarationSyntax?[] namespaceItems)
        {
            var members = new List<MemberDeclarationSyntax>(namespaceItems.Length);
            for (int i = 0; i < namespaceItems.Length; ++i)
            {
                var namespaceItem = namespaceItems[i];

                if (namespaceItem == null)
                {
                    continue;
                }

                members.Add(namespaceItem);
            }

            if (members.Count == 0)
            {
                return null;
            }

            return CompilationUnit(default, members, default)
                .WithLeadingTrivia(
                    XmlSyntaxFactory.GenerateDocumentationString(
                        "<auto-generated />"));
        }
    }
}
