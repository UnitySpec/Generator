﻿using System.Collections.Generic;
using System.Linq;
using UnitySpec.General.Configuration;
using UnitySpec.General.Extensions;
using UnitySpec.Generator.UnitTestProvider;

namespace UnitySpec.Generator.UnitTestConverter
{
    public class NonParallelizableDecorator : ITestClassDecorator
    {
        private readonly string[] nonParallelizableTags;
        private readonly ITagFilterMatcher tagFilterMatcher;

        public NonParallelizableDecorator(ITagFilterMatcher tagFilterMatcher, SpecFlowConfiguration generatorConfiguration)
        {
            this.tagFilterMatcher = tagFilterMatcher;
            nonParallelizableTags = generatorConfiguration.AddNonParallelizableMarkerForTags;
        }

        public int Priority
        {
            get { return PriorityValues.Low; }
        }

        public bool CanDecorateFrom(TestClassGenerationContext generationContext)
        {
            return ProviderSupportsParallelExecution(generationContext) && ConfiguredTagIsPresent(generationContext.Feature.Tags.Select(x => x.GetNameWithoutAt()));
        }

        public void DecorateFrom(TestClassGenerationContext generationContext)
        {
            generationContext.UnitTestGeneratorProvider.SetTestClassNonParallelizable(generationContext);
        }

        private bool ProviderSupportsParallelExecution(TestClassGenerationContext generationContext)
        {
            return generationContext.UnitTestGeneratorProvider.GetTraits()
                .HasFlag(UnitTestGeneratorTraits.ParallelExecution);
        }

        private bool ConfiguredTagIsPresent(IEnumerable<string> tagName)
        {
            return nonParallelizableTags?.Any(x => tagFilterMatcher.Match(x, tagName)) ?? false;
        }
    }
}