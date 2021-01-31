/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonTestLibrary
{
    /// <summary>
    /// A loose collection of static methods to help with testing
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class TestUtils
    {
        /// <summary>
        /// Creates a random string using a subset of alphanumeric characters
        /// </summary>
        /// <param name="size">The size of string to return</param>
        /// <param name="includeUpperCase">Include uppercase characters in the list of possible characters. Defaults to 'true'.</param>
        /// <param name="includeLowerCase">Include lowercase characters in the list of possible characters. Defaults to 'true'.</param>
        /// <param name="includeNumbers">Include numeric characters in the list of possible characters. Defaults to 'true'.</param>
        /// <param name="includeSpaces">Include the space character in the list of possible characters. Defaults to 'false'.</param>
        /// <param name="includeSpecialCharacters">Include special characters in the list of possible characters. Defaults to 'false'.</param>
        /// <returns>A random string using a subset of alphanumeric characters</returns>
        public static string CreateRandomString(
            int size = 10,
            bool includeUpperCase = true,
            bool includeLowerCase = true,
            bool includeNumbers = true,
            bool includeSpaces = false,
            bool includeSpecialCharacters = false)
        {
            string possibleCharacters = "";

            if (includeUpperCase)
                possibleCharacters += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (includeLowerCase)
                possibleCharacters += "abcdefghijklmnopqrstuvwxyz";

            if (includeNumbers)
                possibleCharacters += "0123456789";

            if (includeSpaces)
                possibleCharacters += " ";

            if (includeSpecialCharacters)
                possibleCharacters += "~!@#$%^&*()_+`-=[]\\{}|;':\",./<>?";

            if (string.IsNullOrEmpty(possibleCharacters))
                throw new InvalidOperationException("Unable to generate a random string when no characters subsets have been defined.");

            return new string(Enumerable.Repeat(possibleCharacters, size)
              .Select(s => s[new Random().Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Creates a random number
        /// </summary>
        /// <param name="minimum">The inclusive maximum</param>
        /// <param name="maximum">The exclusive maximum</param>
        /// <returns>A random number</returns>
        public static int CreateRandomNumber(int minimum = int.MinValue, int maximum = int.MaxValue) { return new Random().Next(minimum, maximum); }

        /// <summary>
        /// Compare all string, int, and Guid properties of two objects to ensure their equality
        /// </summary>
        /// <param name="toCompare1">The first object</param>
        /// <param name="toCompare2">The second object</param>
        /// <param name="excludedProperties">A list of property names to exclude</param>
        /// <param name="allowEmptyGuids">When true, empty Guids are allowed. Defaults to false.</param>
        public static void TestPropertiesForEquality(object toCompare1, object toCompare2, List<string> excludedProperties = null, bool allowEmptyGuids = false)
        {
            if (null != toCompare1)
            {
                CompareObjects(toCompare1, toCompare2, excludedProperties, allowEmptyGuids, false);
            }
        }

        /// <summary>
        /// Compare all string, int, and Guid properties of two objects to ensure their inequality
        /// </summary>
        /// <param name="toCompare1">The first object</param>
        /// <param name="toCompare2">The second object</param>
        /// <param name="excludedProperties">A list of property names to exclude</param>
        /// <param name="allowEmptyGuids">When true, empty Guids are allowed. Defaults to false.</param>
        public static void TestPropertiesForInequality(object toCompare1, object toCompare2, List<string> excludedProperties = null, bool allowEmptyGuids = false)
        {
            if (null != toCompare1)
            {
                CompareObjects(toCompare1, toCompare2, excludedProperties, allowEmptyGuids, true);
            }
        }

        private static void CompareObjects(object toCompare1, object toCompare2, List<string> excludedProperties = null, bool allowEmptyGuids = false, bool executedNegativeComparison = false)
        {
            var properties = toCompare1.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo p in properties)
            {
                if (excludedProperties != null)
                {
                    if (!excludedProperties.Contains(p.Name))
                    {
                        CompareObjects(toCompare1, toCompare2, allowEmptyGuids, executedNegativeComparison, p);
                    }
                }
                else
                {
                    CompareObjects(toCompare1, toCompare2, allowEmptyGuids, executedNegativeComparison, p);
                }
            }
        }

        private static void CompareObjects(object toCompare1, object toCompare2, bool allowEmptyGuids, bool executedNegativeComparison, PropertyInfo p)
        {
            if (p.PropertyType == typeof(string) || p.PropertyType == typeof(int) || p.PropertyType == typeof(Guid))
            {
                if (p.CanRead && p.CanWrite)
                {
                    var v1 = p.GetValue(toCompare1, null);
                    var v2 = p.GetValue(toCompare2, null);

                    if (!allowEmptyGuids && p.PropertyType == typeof(Guid))
                    {
                        Assert.AreNotEqual(Guid.Empty, v1);
                        Assert.AreNotEqual(Guid.Empty, v2);
                    }

                    if (executedNegativeComparison)
                        Assert.AreNotEqual(v1, v2);
                    else
                        Assert.AreEqual(v1, v2);
                }
            }
        }

        /// <summary>
        /// Randomizes public string and int propertie values. Toggles public boolean properties.
        /// </summary>
        /// <param name="toModify">The object with properties to modify</param>
        /// <param name="excludedProperties">A list of property names to exclude</param>
        /// <returns>The object with randomized property values</returns>
        public static object RandomizePropertyValues(object toModify, List<string> excludedProperties = null)
        {
            if (toModify != null)
            {
                var properties = toModify.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var random = new Random();

                foreach (PropertyInfo p in properties)
                    if (excludedProperties == null || (excludedProperties != null && !excludedProperties.Contains(p.Name)))
                        if (p.CanRead && p.CanWrite)
                            if (p.PropertyType == typeof(string))
                                p.SetValue(toModify, CreateRandomString());
                            else if (p.PropertyType == typeof(int))
                                p.SetValue(toModify, random.Next(int.MaxValue - 1));
                            else if (p.PropertyType == typeof(bool))
                                p.SetValue(toModify, !(bool)p.GetValue(toModify));
            }
            return toModify;
        }
    }
}