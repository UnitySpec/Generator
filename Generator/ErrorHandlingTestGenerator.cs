﻿using Gherkin;
using System;
using System.Diagnostics;
using System.Linq;
using UnitySpec.General.Extensions;
using UnitySpec.General.GeneratorInterfaces;

namespace UnitySpec.Generator
{
    public abstract class ErrorHandlingTestGenerator : RemotableGeneratorClass
    {
        public TestGeneratorResult GenerateTestFile(FeatureFileInput featureFileInput, GenerationSettings settings)
        {
            if (featureFileInput == null) throw new ArgumentNullException("featureFileInput");
            if (settings == null) throw new ArgumentNullException("settings");

            try
            {
                return GenerateTestFileWithExceptions(featureFileInput, settings);
            }
            catch (ParserException parserException)
            {
                return new TestGeneratorResult(parserException.GetParserExceptions().Select(
                    ex => new TestGenerationError(ex.Location == null ? 0 : ex.Location.Line, ex.Location == null ? 0 : ex.Location.Column, ex.Message)));
            }
            catch (Exception exception)
            {
                return new TestGeneratorResult(new TestGenerationError(exception));
            }
        }

        public Version DetectGeneratedTestVersion(FeatureFileInput featureFileInput)
        {
            if (featureFileInput == null) throw new ArgumentNullException("featureFileInput");

            try
            {
                return DetectGeneratedTestVersionWithExceptions(featureFileInput);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception, "ErrorHandlingTestGenerator.DetectGeneratedTestVersion");
                return null;
            }
        }

        protected abstract TestGeneratorResult GenerateTestFileWithExceptions(FeatureFileInput featureFileInput, GenerationSettings settings);
        protected abstract Version DetectGeneratedTestVersionWithExceptions(FeatureFileInput featureFileInput);

        public virtual void Dispose()
        {
            //nop;
        }
    }
}