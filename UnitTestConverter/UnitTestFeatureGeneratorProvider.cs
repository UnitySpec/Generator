using UnitySpec.General.Parser;
using UnitySpec.Generator.Generation;

namespace UnitySpec.Generator.UnitTestConverter
{
    public class UnitTestFeatureGeneratorProvider : IFeatureGeneratorProvider
    {
        private readonly UnitTestFeatureGenerator _unitTestFeatureGenerator;

        public UnitTestFeatureGeneratorProvider(UnitTestFeatureGenerator unitTestFeatureGenerator)
        {
            _unitTestFeatureGenerator = unitTestFeatureGenerator;
        }

        public int Priority => PriorityValues.Lowest;

        public bool CanGenerate(SpecFlowDocument document)
        {
            return true;
        }

        public IFeatureGenerator CreateGenerator(SpecFlowDocument document)
        {
            return _unitTestFeatureGenerator;
        }
    }
}