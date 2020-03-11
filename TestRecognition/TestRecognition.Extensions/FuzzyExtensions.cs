using DuoVia.FuzzyStrings;
using System;
using System.Collections.Generic;

namespace TestRecognition.Extensions
{
    public static class FuzzyExtensions
    {
        /// <summary>
        /// Compare string vs collection of strings using FuzzyEquals, case insensitive.
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="collStrB"></param>
        /// <param name="requiredProbabilityScore"></param>
        /// <returns>Returns true if any member of collection is equal to string A</returns>
        public static bool FuzzyEqualsCollection(this string strA, IEnumerable<string> collStrB, double requiredProbabilityScore = 0.75)
        {
            var result = false;

            if (collStrB != null)
            {
                foreach (var strB in collStrB)
                {
                    if (strA.ToLower().FuzzyEquals(strB.ToLower(), requiredProbabilityScore))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
