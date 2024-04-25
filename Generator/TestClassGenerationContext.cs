using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using UnitySpec.General.Parser;
using UnitySpec.Generator.UnitTestProvider;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace UnitySpec.Generator
{
    public class TestClassGenerationContext
    {
        public IUnitTestGeneratorProvider UnitTestGeneratorProvider { get; private set; }
        public SpecFlowDocument Document { get; private set; }

        public SpecFlowFeature Feature => Document.SpecFlowFeature;

        public NamespaceDeclarationSyntax Namespace { get; private set; }
        public ClassDeclarationSyntax TestClass { get; set; }
        public MethodDeclarationSyntax TestClassInitializeMethod { get; set; }
        public MethodDeclarationSyntax TestClassUnitySetupMethod { get; set; }
        public MethodDeclarationSyntax TestInitializeMethod { get; set; }
        public MethodDeclarationSyntax TestCleanupMethod { get; set; }
        public MethodDeclarationSyntax ScenarioInitializeMethod { get; set; }
        public MethodDeclarationSyntax ScenarioStartMethod { get; set; }
        public MethodDeclarationSyntax ScenarioCleanupMethod { get; set; }
        public MethodDeclarationSyntax FeatureBackgroundMethod { get; set; }
        public FieldDeclarationSyntax TestRunnerField { get; set; }
        public List<FieldDeclarationSyntax> ExtraFields { get; set; }

        public bool GenerateRowTests { get; private set; }

        public IDictionary<string, object> CustomData { get; private set; }

        public TestClassGenerationContext(
            IUnitTestGeneratorProvider unitTestGeneratorProvider,
            SpecFlowDocument document,
            NamespaceDeclarationSyntax ns,
            ClassDeclarationSyntax testClass,
            FieldDeclarationSyntax testRunnerField,
            MethodDeclarationSyntax testClassInitializeMethod,
            MethodDeclarationSyntax testClassCleanupMethod,
            MethodDeclarationSyntax testInitializeMethod,
            MethodDeclarationSyntax testCleanupMethod,
            MethodDeclarationSyntax scenarioInitializeMethod,
            MethodDeclarationSyntax scenarioStartMethod,
            MethodDeclarationSyntax scenarioCleanupMethod,
            MethodDeclarationSyntax featureBackgroundMethod,
            bool generateRowTests)
        {
            UnitTestGeneratorProvider = unitTestGeneratorProvider;
            Document = document;
            Namespace = ns;
            TestClass = testClass;
            TestRunnerField = testRunnerField;
            TestClassInitializeMethod = testClassInitializeMethod;
            TestClassUnitySetupMethod = testClassCleanupMethod;
            TestInitializeMethod = testInitializeMethod;
            TestCleanupMethod = testCleanupMethod;
            ScenarioInitializeMethod = scenarioInitializeMethod;
            ScenarioStartMethod = scenarioStartMethod;
            ScenarioCleanupMethod = scenarioCleanupMethod;
            FeatureBackgroundMethod = featureBackgroundMethod;
            GenerateRowTests = generateRowTests;

            ExtraFields = new List<FieldDeclarationSyntax>();
            CustomData = new Dictionary<string, object>();
        }

        public NamespaceDeclarationSyntax BuildClass()
        {
            return Namespace.WithMembers(SingletonList<MemberDeclarationSyntax>(BuildTestClass()));
        }

        private ClassDeclarationSyntax BuildTestClass()
        {
            var members = new List<MemberDeclarationSyntax>()
                {
                    TestRunnerField,
                    TestClassInitializeMethod,
                    TestClassUnitySetupMethod,
                    TestInitializeMethod,
                    TestCleanupMethod,
                    ScenarioInitializeMethod,
                    ScenarioStartMethod,
                    ScenarioCleanupMethod
                };
            members.AddRange(ExtraFields);
            if (FeatureBackgroundMethod != null) { members.Add(FeatureBackgroundMethod); }
            var allMembers = TestClass.Members.AddRange(members);
            return TestClass.WithMembers(List<MemberDeclarationSyntax>(allMembers));
        }
    }
}


