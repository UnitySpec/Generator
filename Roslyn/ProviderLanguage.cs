namespace UnitySpec.Generator.Roslyn
{
    public enum ProviderLanguage
    {
        CSharp,
        VB,
        Other
    }

    public static class ProviderLanguageExtensions
    {
        public static string GetLanguage(this ProviderLanguage language)
        {
            switch (language)
            {
                case ProviderLanguage.CSharp:
                    return "ProgrammingLanguage.CSharp";
                case ProviderLanguage.VB:
                    return "ProgrammingLanguage.VB";
                case ProviderLanguage.Other:
                    return "ProgrammingLanguage.Other";
                default:
                    return language.ToString();
            };
        }
    }
}