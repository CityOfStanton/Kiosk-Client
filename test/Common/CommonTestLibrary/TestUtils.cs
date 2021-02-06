/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Actions;
using KioskLibrary.Common;
using KioskLibrary.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Action = KioskLibrary.Actions.Action;

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

        public static OrchestrationInstance CreateRandomOrchestrationInstance()
        {
            var orchestrationSourceOptions = Enum.GetValues(typeof(OrchestrationSource));
            var lifecycleBehaviorOptions = Enum.GetValues(typeof(LifecycleBehavior));
            var orderingOptions = Enum.GetValues(typeof(Ordering));
            var r = new Random();

            return new OrchestrationInstance(
                new List<Action>()
                {
                    new ImageAction()
                    {
                        Path = $"http://{CreateRandomString()}"
                    },
                    new WebsiteAction()
                    {
                        Path = $"http://{CreateRandomString()}"
                    }
                },
                CreateRandomNumber(15),
                (OrchestrationSource)orchestrationSourceOptions.GetValue(r.Next(orchestrationSourceOptions.Length)),
                (LifecycleBehavior)lifecycleBehaviorOptions.GetValue(r.Next(lifecycleBehaviorOptions.Length)),
                (Ordering)orderingOptions.GetValue(r.Next(orderingOptions.Length)));
        }

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
                CompareObjects(toCompare1, toCompare2, excludedProperties, allowEmptyGuids, false);
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
                CompareObjects(toCompare1, toCompare2, excludedProperties, allowEmptyGuids, true);
        }

        private static void CompareObjects(object toCompare1, object toCompare2, List<string> excludedProperties = null, bool allowEmptyGuids = false, bool executedNegativeComparison = false)
        {
            var properties = toCompare1.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo p in properties)
            {
                var doComparrison = false;

                if (excludedProperties == null)
                    doComparrison = true;
                else if (!excludedProperties.Contains(p.Name))
                    doComparrison = true;

                if (doComparrison && p.CanRead && p.CanWrite)
                {
                    var v1 = p.GetValue(toCompare1, null);
                    var v2 = p.GetValue(toCompare2, null);

                    if (p.PropertyType.IsPrimitive)
                    {
                        if (!allowEmptyGuids && p.PropertyType == typeof(Guid))
                        {
                            Assert.AreNotEqual(Guid.Empty, v1, "First property is not an empty GUID.");
                            Assert.AreNotEqual(Guid.Empty, v2, "Second property is not an empty GUID.");
                        }

                        if (executedNegativeComparison)
                            Assert.AreNotEqual(v1, v2, "Both properties are not equal.");
                        else
                            Assert.AreEqual(v1, v2, "Both properties are equal.");
                    }
                    else if (p.PropertyType.IsArray)
                    {
                        Assert.AreEqual((v1 as object[]).Length, (v2 as object[]).Length, "Both arrays are of the same length");

                        for (int i = 0; i < (v1 as object[]).Length; i++)
                            CompareObjects((v1 as object[])[i], (v2 as object[])[i], excludedProperties, allowEmptyGuids, executedNegativeComparison);
                    }
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