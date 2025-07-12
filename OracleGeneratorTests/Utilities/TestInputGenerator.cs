using OracleGeneratorTests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleGeneratorTests.Utilities
{
    public static class TestInputGenerator
    {
        private static Random random = new Random();

        public static Dictionary<string, object> Generate(ApiSpec spec)
        {
            var input = new Dictionary<string, object>();

            foreach (var field in spec.Inputs)
            {
                if (field.Type == "string")
                {
                    if (field.Name.ToLower().Contains("email"))
                    {
                        input[field.Name] = $"user{random.Next(1000)}@example.com";
                    }
                    else
                    {
                        input[field.Name] = GenerateRandomString(8);
                    }
                }
                else if (field.Type == "int")
                {
                    input[field.Name] = random.Next(1, 100);
                }
                else if (field.Type == "decimal")
                {
                    input[field.Name] = Math.Round(random.NextDouble() * 500, 2);
                }
            }

            return input;
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
    //public static class TestInputGenerator
    //{
    //    private static Random random = new Random();

    //    public static Dictionary<string, object> Generate(ApiSpec spec)
    //    {
    //        var input = new Dictionary<string, object>();

    //        foreach (var field in spec.Inputs)
    //        {
    //            if (field.Type == "string")
    //                input[field.Name] = GenerateRandomString(8);
    //            else if (field.Type == "int")
    //                input[field.Name] = random.Next(18, 100); // use precondition range
    //            else if (field.Type == "decimal")
    //                input[field.Name] = Math.Round(random.NextDouble() * 500, 2);
    //        }

    //        return input;
    //    }

    //    private static string GenerateRandomString(int length)
    //    {
    //        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    //        return new string(Enumerable.Repeat(chars, length)
    //            .Select(s => s[random.Next(s.Length)]).ToArray());
    //    }
    //}

}
