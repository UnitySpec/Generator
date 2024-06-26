﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnitySpec.General.Configuration;
using UnitySpec.General.Extensions;
using UnitySpec.General.GeneratorInterfaces;
using UnitySpec.General.Parser;
using UnitySpec.Generator.Roslyn;
using UnitySpec.Generator.UnitTestConverter;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace UnitySpec.Generator
{
    public class TestGenerator : ErrorHandlingTestGenerator, ITestGenerator
    {
        protected readonly SpecFlowConfiguration specFlowConfiguration;
        protected readonly ProjectSettings projectSettings;
        protected readonly ITestHeaderWriter testHeaderWriter;
        protected readonly ITestUpToDateChecker testUpToDateChecker;
        protected readonly RoslynHelper roslynHelper;
        private readonly IFeatureGeneratorRegistry featureGeneratorRegistry;
        private readonly IGherkinParserFactory gherkinParserFactory;


        public TestGenerator(SpecFlowConfiguration specFlowConfiguration,
            ProjectSettings projectSettings,
            ITestHeaderWriter testHeaderWriter,
            ITestUpToDateChecker testUpToDateChecker,
            IFeatureGeneratorRegistry featureGeneratorRegistry,
            RoslynHelper roslynHelper,
            IGherkinParserFactory gherkinParserFactory)
        {
            if (specFlowConfiguration == null) throw new ArgumentNullException(nameof(specFlowConfiguration));
            if (projectSettings == null) throw new ArgumentNullException(nameof(projectSettings));
            if (testHeaderWriter == null) throw new ArgumentNullException(nameof(testHeaderWriter));
            if (testUpToDateChecker == null) throw new ArgumentNullException(nameof(testUpToDateChecker));
            if (featureGeneratorRegistry == null) throw new ArgumentNullException(nameof(featureGeneratorRegistry));
            if (gherkinParserFactory == null) throw new ArgumentNullException(nameof(gherkinParserFactory));

            this.specFlowConfiguration = specFlowConfiguration;
            this.testUpToDateChecker = testUpToDateChecker;
            this.featureGeneratorRegistry = featureGeneratorRegistry;
            this.roslynHelper = roslynHelper;
            this.testHeaderWriter = testHeaderWriter;
            this.projectSettings = projectSettings;
            this.gherkinParserFactory = gherkinParserFactory;
        }

        protected override TestGeneratorResult GenerateTestFileWithExceptions(FeatureFileInput featureFileInput, GenerationSettings settings)
        {
            if (featureFileInput == null) throw new ArgumentNullException("featureFileInput");
            if (settings == null) throw new ArgumentNullException("settings");

            var generatedTestFullPath = GetTestFullPath(featureFileInput);
            bool? preliminaryUpToDateCheckResult = null;
            if (settings.CheckUpToDate)
            {
                preliminaryUpToDateCheckResult = testUpToDateChecker.IsUpToDatePreliminary(featureFileInput, generatedTestFullPath, settings.UpToDateCheckingMethod);
                if (preliminaryUpToDateCheckResult == true)
                    return new TestGeneratorResult(null, true);
            }

            string generatedTestCode = GetGeneratedTestCode(featureFileInput);
            if (string.IsNullOrEmpty(generatedTestCode))
                return new TestGeneratorResult(null, true);

            if (settings.CheckUpToDate && preliminaryUpToDateCheckResult != false)
            {
                var isUpToDate = testUpToDateChecker.IsUpToDate(featureFileInput, generatedTestFullPath, generatedTestCode, settings.UpToDateCheckingMethod);
                if (isUpToDate)
                    return new TestGeneratorResult(null, true);
            }

            if (settings.WriteResultToFile)
            {
                File.WriteAllText(generatedTestFullPath, generatedTestCode, Encoding.UTF8);
            }

            return new TestGeneratorResult(generatedTestCode, false);
        }

        protected string GetGeneratedTestCode(FeatureFileInput featureFileInput)
        {
            var classDeclaration = GenerateTestFileCode(featureFileInput);
            var header = GetUnitySpecHeader();
            var footer = GetUnitySpecFooter();

            CompilationUnitSyntax unit =
                CompilationUnit()
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        classDeclaration
                        .WithNamespaceKeyword(header)
                        )
                    )
                .WithEndOfFileToken(footer)
                .NormalizeWhitespace()
                ;

            return unit.ToFullString();
        }

        private NamespaceDeclarationSyntax GenerateTestFileCode(FeatureFileInput featureFileInput)
        {
            string targetNamespace = GetTargetNamespace(featureFileInput) ?? "UnitySpec.GeneratedTests";

            var parser = gherkinParserFactory.Create(specFlowConfiguration.FeatureLanguage);
            SpecFlowDocument specFlowDocument;
            using (var contentReader = featureFileInput.GetFeatureFileContentReader(projectSettings))
            {
                specFlowDocument = ParseContent(parser, contentReader, GetSpecFlowDocumentLocation(featureFileInput));
            }

            if (specFlowDocument.SpecFlowFeature == null) return null;

            var featureGenerator = featureGeneratorRegistry.CreateGenerator(specFlowDocument);

            var codeNamespace = featureGenerator.GenerateUnitTestFixture(specFlowDocument, null, targetNamespace);
            return codeNamespace;
        }

        private SpecFlowDocumentLocation GetSpecFlowDocumentLocation(FeatureFileInput featureFileInput)
        {
            return new SpecFlowDocumentLocation(
                featureFileInput.GetFullPath(projectSettings),
                GetFeatureFolderPath(featureFileInput.ProjectRelativePath));
        }

        private string GetFeatureFolderPath(string projectRelativeFilePath)
        {
            string directoryName = Path.GetDirectoryName(projectRelativeFilePath);
            if (string.IsNullOrWhiteSpace(directoryName)) return null;

            return string.Join("/", directoryName.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
        }

        protected virtual SpecFlowDocument ParseContent(IGherkinParser parser, TextReader contentReader, SpecFlowDocumentLocation documentLocation)
        {
            return parser.Parse(contentReader, documentLocation);
        }

        protected string GetTargetNamespace(FeatureFileInput featureFileInput)
        {
            if (!string.IsNullOrEmpty(featureFileInput.CustomNamespace))
                return featureFileInput.CustomNamespace;

            string targetNamespace = projectSettings == null || string.IsNullOrEmpty(projectSettings.DefaultNamespace)
                ? null
                : projectSettings.DefaultNamespace;

            string directoryName = Path.GetDirectoryName(featureFileInput.ProjectRelativePath);
            if (directoryName.StartsWith(projectSettings.ProjectFolder, StringComparison.OrdinalIgnoreCase))
            {
                directoryName = directoryName.Substring(projectSettings.ProjectFolder.Length + "/Assets".Length);
            }
            string namespaceExtension = string.IsNullOrEmpty(directoryName) ? null :
                string.Join(".", directoryName.TrimStart('\\', '/', '.').Split('\\', '/').Select(f => f.ToIdentifier()).ToArray());
            if (!string.IsNullOrEmpty(namespaceExtension))
                targetNamespace = targetNamespace == null
                    ? namespaceExtension
                    : targetNamespace + "." + namespaceExtension;
            return targetNamespace;
        }

        public string GetTestFullPath(FeatureFileInput featureFileInput)
        {
            var path = featureFileInput.GetGeneratedTestFullPath(projectSettings);
            if (path != null)
                return path;

            return featureFileInput.GetFullPath(projectSettings) + GenerationTargetLanguage.GetExtension(projectSettings.ProjectPlatformSettings.Language);
        }

        #region Header & Footer
        protected override Version DetectGeneratedTestVersionWithExceptions(FeatureFileInput featureFileInput)
        {
            var generatedTestFullPath = GetTestFullPath(featureFileInput);
            return testHeaderWriter.DetectGeneratedTestVersion(featureFileInput.GetGeneratedTestContent(generatedTestFullPath));
        }

        protected SyntaxToken GetUnitySpecHeader()
        {
            return Token(
                        TriviaList(
                            new[]{
                                Comment("// ------------------------------------------------------------------------------"),
                                Comment("//  <auto-generated>"),
                                Comment("//      This code was generated by UnitySpec (https://github.com/UnitySpec)."),
                                Comment("// "),
                                Comment("//      Changes to this file may cause incorrect behavior and will be lost if"),
                                Comment("//      the code is regenerated."),
                                Comment("//  </auto-generated>"),
                                Comment("// ------------------------------------------------------------------------------"),

                                roslynHelper.GetStartRegionStatement("Designer generated code"),
                                roslynHelper.GetDisableWarningsPragma()

                            }
                        ),
                        SyntaxKind.NamespaceKeyword,
                        TriviaList()
                );
        }

        protected SyntaxToken GetUnitySpecFooter()
        {
            return Token(
                    TriviaList(
                        new[]{
                            Trivia(
                                PragmaWarningDirectiveTrivia(
                                    Token(SyntaxKind.RestoreKeyword),
                                    true
                                )
                            ),
                            Trivia(
                                BadDirectiveTrivia(
                                    Token(SyntaxKind.EndRegionKeyword),
                                    true
                                )
                            )
                        }
                    ),
                    SyntaxKind.EndOfFileToken,
                    TriviaList()
                );
        }
        #endregion
    }
}
