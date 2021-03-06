// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    [Generator]
    public partial class RazorSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var razorSourceGeneratorOptionsWithDiagnostics = context.AnalyzerConfigOptionsProvider
                .Combine(context.ParseOptionsProvider)
                .Select(ComputeRazorSourceGeneratorOptions);
            var razorSourceGeneratorOptions = razorSourceGeneratorOptionsWithDiagnostics.ReportDiagnostics(context);

            var sourceItemsWithDiagnostics = context.AdditionalTextsProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Where((pair) => pair.Item1.Path.EndsWith(".razor") || pair.Item1.Path.EndsWith(".cshtml"))
                .Select(ComputeProjectItems);

            var sourceItems = sourceItemsWithDiagnostics.ReportDiagnostics(context);

            var references = context.CompilationProvider
                .WithLambdaComparer(
                    (c1, c2) => c1 != null && c2 != null && c1.References != c2.References)
                .Select((compilation, _) => compilation.References);

            var sourceItemsByName = sourceItems.Collect()
                .WithLambdaComparer((@new, old) => @new.SequenceEqual(old, new LambdaComparer<SourceGeneratorProjectItem>(
                    (l, r) => string.Equals(l?.FilePath, r?.FilePath, System.StringComparison.OrdinalIgnoreCase))));

            var discoveryProjectEngine = references
                .Combine(razorSourceGeneratorOptions)
                .Combine(sourceItemsByName)
                .Select((pair, _) =>
                {
                    var ((references, razorSourceGeneratorOptions), projectItems) = pair;
                    var tagHelperFeature = new StaticCompilationTagHelperFeature();
                    return GetDiscoveryProjectEngine(tagHelperFeature, references, projectItems, razorSourceGeneratorOptions);
                });

            var syntaxTrees = sourceItems
                .Combine(discoveryProjectEngine)
                .Combine(context.ParseOptionsProvider)
                .Select((pair, _) =>
                {
                    var (itemAndDiscoveryEngine, parseOptions) = pair;
                    var (item, discoveryProjectEngine) = itemAndDiscoveryEngine;

                    var codeGen = discoveryProjectEngine.Process(item);
                    var generatedCode = codeGen.GetCSharpDocument().GeneratedCode;
                    return CSharpSyntaxTree.ParseText(generatedCode, (CSharpParseOptions)parseOptions);
                });

            var tagHelpersFromCompilation = syntaxTrees
                .Combine(context.CompilationProvider)
                .Combine(discoveryProjectEngine)
                .SelectMany((pair, _) =>
                {
                    var ((syntaxTrees, compilation), discoveryProjectEngine) = pair;
                    var tagHelperFeature = GetFeature<StaticCompilationTagHelperFeature>(discoveryProjectEngine);
                    return GetTagHelpersFromCompilation(
                        compilation,
                        tagHelperFeature!,
                        syntaxTrees
                    );
                });

            var tagHelpersFromReferences = discoveryProjectEngine
                .Combine(context.CompilationProvider)
                .Combine(references)
                .Select((pair, _) =>
                {
                    var (engineAndCompilation, references) = pair;
                    var (discoveryProjectEngine, compilation) = engineAndCompilation;
                    var tagHelperFeature = GetFeature<StaticCompilationTagHelperFeature>(discoveryProjectEngine);
                    return GetTagHelpers(
                        references,
                        tagHelperFeature!,
                        compilation
                    );
                });

            var tagHelpers = tagHelpersFromCompilation.Collect().Combine(tagHelpersFromReferences);

            var generationProjectEngine = tagHelpers.Combine(razorSourceGeneratorOptions).Combine(sourceItems.Collect())
                .Select((pair, _) =>
                {
                    var (tagHelpersAndOptions, items) = pair;
                    var (tagHelpers, razorSourceGeneratorOptions) = tagHelpersAndOptions;
                    var (tagHelpersFromCompilation, tagHelpersFromReferences) = tagHelpers;
                    var tagHelpersCount = tagHelpersFromCompilation.Count() + tagHelpersFromReferences.Count;
                    var allTagHelpers = new List<TagHelperDescriptor>(tagHelpersCount);
                    allTagHelpers.AddRange(tagHelpersFromCompilation);
                    allTagHelpers.AddRange(tagHelpersFromReferences);

                    return GetGenerationProjectEngine(allTagHelpers, items, razorSourceGeneratorOptions);
                });

            var generationInputs = sourceItems
                .Combine(razorSourceGeneratorOptions)
                .Combine(generationProjectEngine);

            context.RegisterSourceOutput(generationInputs, (context, pair) =>
            {
                var (sourceItemsAndOptions, projectEngine) = pair;
                var (projectItem, razorSourceGeneratorOptions) = sourceItemsAndOptions;

                var codeDocument = projectEngine.Process(projectItem);
                var csharpDocument = codeDocument.GetCSharpDocument();

                for (var j = 0; j < csharpDocument.Diagnostics.Count; j++)
                {
                    var razorDiagnostic = csharpDocument.Diagnostics[j];
                    var csharpDiagnostic = razorDiagnostic.AsDiagnostic();
                    context.ReportDiagnostic(csharpDiagnostic);
                }

                if (!razorSourceGeneratorOptions.SuppressRazorSourceGenerator)
                {
                    // Add a generated suffix so tools, such as coverlet, consider the file to be generated
                    var hintName = GetIdentifierFromPath(projectItem.RelativePhysicalPath) + ".g.cs";
                    context.AddSource(hintName, csharpDocument.GeneratedCode);
                }
            });
        }
    }
}
