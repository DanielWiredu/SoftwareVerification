using OracleGeneratorTests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleGeneratorTests.Utilities
{
    public static class PreconditionValidator
    {
        public static bool Validate(ApiSpec spec, Dictionary<string, object> input, out List<string> failures)
        {
            failures = new List<string>();

            foreach (var condition in spec.Preconditions)
            {
                var result = Evaluate(condition.Condition, input);
                if (!result)
                    failures.Add(condition.Message);
            }

            return !failures.Any();
        }

        private static bool Evaluate(string expression, Dictionary<string, object> input)
        {
            if (expression.Contains(">="))
            {
                var parts = expression.Split(">=");
                var key = parts[0].Trim();
                var val = decimal.Parse(parts[1].Trim());
                return Convert.ToDecimal(input[key]) >= val;
            }
            else if (expression.Contains(">"))
            {
                var parts = expression.Split(">");
                var key = parts[0].Trim();
                var val = decimal.Parse(parts[1].Trim());
                return Convert.ToDecimal(input[key]) > val;
            }
            else if (expression.Contains("<"))
            {
                var parts = expression.Split("<");
                var key = parts[0].Trim();
                var val = decimal.Parse(parts[1].Trim());
                return Convert.ToDecimal(input[key]) < val;
            }
            else if (expression.Contains("=="))
            {
                var parts = expression.Split("==");
                var key = parts[0].Trim();
                var val = parts[1].Trim(' ', '"');
                return input[key].ToString() == val;
            }
            else if (expression.Contains("!="))
            {
                var parts = expression.Split("!=");
                var key = parts[0].Trim();
                var val = parts[1].Trim(' ', '"');
                return input[key].ToString() != val;
            }
            else if (expression.Contains("contains"))
            {
                var parts = expression.Split("contains");
                var key = parts[0].Trim();
                var substr = parts[1].Trim(' ', '\"', '\'');
                return input[key].ToString().Contains(substr);
            }
            return true;
        }
    }
}
