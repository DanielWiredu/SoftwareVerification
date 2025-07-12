using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleGeneratorTests.Utilities
{
    public class SpecTestCaseData
    {
        public static IEnumerable<object[]> GetTestCases()
        {
            var specs = new[] { "Specs/register_user.yaml", "Specs/transfer_funds.yaml" };

            foreach (var specPath in specs)
            {
                var spec = SpecLoader.LoadFromYaml(specPath);
                for (int i = 0; i < 10; i++)
                {
                    var input = TestInputGenerator.Generate(spec);
                    yield return new object[] { specPath, input, i + 1 };
                }
            }
        }
    }
}
