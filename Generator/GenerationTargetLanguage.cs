﻿using System;
using UnitySpec.Generator.Roslyn;

namespace UnitySpec.Generator
{
    public static class GenerationTargetLanguage
    {
        public const string CSharp = "C#";
        public const string VB = "VB";

        public static string GetExtension(string programmingLanguage)
        {
            switch (programmingLanguage)
            {
                case CSharp:
                    return ".cs";
                case VB:
                    return ".vb";
                default:
                    throw new NotSupportedException("Programming language not supported: " + programmingLanguage);
            }
        }

        public static RoslynHelper CreateCodeDomHelper(string programmingLanguage)
        {
            switch (programmingLanguage)
            {
                case CSharp:
                    return new RoslynHelper(ProviderLanguage.CSharp);
                case VB:
                    return new RoslynHelper(ProviderLanguage.VB);
                default:
                    throw new NotSupportedException("Programming language not supported: " + programmingLanguage);
            }
        }
    }
}