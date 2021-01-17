/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Action = KioskLibrary.Actions.Action;

namespace KioskLibrary.Converters
{
    /// <summary>
    /// Extension class to handle string operations
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Converts a title case string to camel case
        /// </summary>
        /// <param name="stringToConvert">The string to Convert</param>
        /// <returns>The string represented in camel case</returns>
        public static string ToCamelCase(this string stringToConvert)
        {
            if (stringToConvert.Length > 1)
                return stringToConvert[0].ToString().ToLowerInvariant() + stringToConvert.Substring(1);
            return stringToConvert;
        }

        /// <summary>
        /// Custom JSON converter for <see cref="Action" />s
        /// </summary>
        public class ActionConverter : JsonConverter<Action>
        {
            /// <summary>
            /// Whether or not we can convert
            /// </summary>
            /// <param name="typeToConvert">The type to convert</param>
            public override bool CanConvert(Type typeToConvert) =>
                typeof(Action).IsAssignableFrom(typeToConvert);

            /// <summary>
            /// The method called during deserialization
            /// </summary>
            /// <param name="reader">The current reader</param>
            /// <param name="typeToConvert">The type of the object to convert</param>
            /// <param name="options">The options to use when converting the object to JSON</param>
            public override Action Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var properties = new List<KeyValuePair<string, object>>();
                var tmpArray = new List<object>();
                var tmpArrayPropertyName = "";
                var loadToArray = false;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return DeduceAction(properties);

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString().ToUpperInvariant();

                        reader.Read();

                        if (reader.TokenType == JsonTokenType.String
                            || reader.TokenType == JsonTokenType.Number
                            || reader.TokenType == JsonTokenType.False
                            || reader.TokenType == JsonTokenType.Null
                            || reader.TokenType == JsonTokenType.True)
                        {
                            if (loadToArray)
                                tmpArray.Add(propertyName);
                            else
                            {
                                object value = null;

                                if (reader.TokenType == JsonTokenType.Number)
                                    value = reader.GetDouble();
                                else if (reader.TokenType == JsonTokenType.String)
                                    value = reader.GetString();
                                else if (reader.TokenType == JsonTokenType.False)
                                    value = false;
                                else if (reader.TokenType == JsonTokenType.True)
                                    value = true;

                                properties.Add(new KeyValuePair<string, object>(propertyName, value));
                            }
                        }

                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            loadToArray = true;
                            tmpArrayPropertyName = propertyName;
                        }
                    }

                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        loadToArray = false;

                        var tmp = new List<object>();
                        foreach (var c in tmpArray)
                            tmp.Add(c);

                        properties.Add(new KeyValuePair<string, object>(tmpArrayPropertyName, tmp));

                        tmpArrayPropertyName = "";
                    }

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        var childAction = Read(ref reader, typeToConvert, options);
                        tmpArray.Add(childAction);
                    }
                }

                throw new JsonException();
            }

            /// <summary>
            /// The method called during serialization of an <see cref="Action" />
            /// </summary>
            /// <param name="writer">The current writer</param>
            /// <param name="action">The action to serialize</param>
            /// <param name="options">The options to use when converting the object to JSON</param>
            public override void Write(Utf8JsonWriter writer, Action action, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString(nameof(action.Name).ToCamelCase(), action.Name);

                if (action.Duration.HasValue)
                    writer.WriteNumber(nameof(action.Duration).ToCamelCase(), action.Duration.Value);

                if (action.GetType() == typeof(ImageAction))
                {
                    var a = action as ImageAction;
                    writer.WriteString(nameof(a.Path).ToCamelCase(), a.Path);
                    writer.WriteString(nameof(a.Stretch).ToCamelCase(), a.Stretch.ToString());
                }
                else if (action.GetType() == typeof(WebsiteAction))
                {
                    var a = action as WebsiteAction;
                    writer.WriteString(nameof(a.Path).ToCamelCase(), a.Path);
                    writer.WriteBoolean(nameof(a.AutoScroll).ToCamelCase(), a.AutoScroll);

                    if (a.ScrollDuration.HasValue)
                        writer.WriteNumber(nameof(a.ScrollDuration).ToCamelCase(), a.ScrollDuration.Value);

                    if (a.ScrollInterval.HasValue)
                        writer.WriteNumber(nameof(a.ScrollInterval).ToCamelCase(), a.ScrollInterval.Value);

                    if (a.ScrollResetDelay.HasValue)
                        writer.WriteNumber(nameof(a.ScrollResetDelay).ToCamelCase(), a.ScrollResetDelay.Value);
                }

                writer.WriteEndObject();
            }

            /// <summary>
            /// Deduces the derrived type of the Action based on a count of matching property names.
            /// </summary>
            /// <remarks>It's possible that this method will not be able to deduce the type due to a lack of unique properties. In that case, the first matching type will be returned.</remarks>
            /// <param name="objectProperties">A map of property values to their corresponding property names</param>
            /// <returns>The derrived <see cref="Action" /> that was deduced from <paramref name="objectProperties" /></returns>
            private static Action DeduceAction(List<KeyValuePair<string, object>> objectProperties)
            {
                var TypeToProperties = new Dictionary<Type, List<string>>(); // A map of a Type to a list of its properties
                var TypeToScore = new Dictionary<Type, int>(); // A map of a Type to the number of properties that exist on the object to test

                // Gather the classes that derrive from Action and populate the dictionaries
                foreach (var t in Assembly.GetExecutingAssembly().GetTypes()
                    .Where(x => x.IsSubclassOf(typeof(Action)) && !x.IsAbstract)
                    .Select(x => x))
                {
                    // Add a new type and populate the list with all public properties
                    TypeToProperties.Add(t, t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToList());
                    // Add a new type and set the default score to 0
                    TypeToScore.Add(t, 0);
                }

                // Look at each type
                foreach (var type in TypeToProperties.Keys)
                {
                    // Look at each property
                    foreach (var property in TypeToProperties[type])
                    {
                        // Add 1 to the score if the property exists, -1 if it does not
                        TypeToScore[type] += objectProperties.Any(x => x.Key.Equals(property, StringComparison.OrdinalIgnoreCase)) ? 1 : -1;
                    }
                }

                // Get the highest scoring type
                var likelyType = TypeToScore.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

                // Create a default constructed instance of the highest scoring type
                var toReturn = Activator.CreateInstance(likelyType) as Action;

                foreach (var p in toReturn.GetType().GetProperties())
                {
                    if (objectProperties.Select(x => x.Key).Contains(p.Name.ToUpperInvariant()))
                    {
                        var property = objectProperties.First(x => x.Key == p.Name.ToUpperInvariant());
                        try
                        {
                            if (property.Value is List<object>)
                            {
                                System.Collections.IList listProperty = Activator.CreateInstance(p.PropertyType) as System.Collections.IList;

                                foreach (var c in (property.Value as List<object>))
                                    listProperty.Add(c);

                                p.SetValue(toReturn, listProperty, null);
                            }
                            else
                            {
                                // This method may fail on system objects (such as TaskResults).
                                p.SetValue(toReturn, ConvertStringToType(p.PropertyType, property.Value?.ToString()), null);
                            }
                        }
                        catch { }
                    }
                }

                return toReturn;
            }

            /// <summary>
            /// Converts <paramref name="value" /> to the <paramref name="propertyType" />
            /// </summary>
            /// <param name="propertyType">The target type</param>
            /// <param name="value">The string to convert</param>
            /// <returns>An <see cref="object" /> that represents the type <paramref name="propertyType" /></returns>
            private static object ConvertStringToType(Type propertyType, string value)
            {
                if (value == null) return null;

                if (propertyType == typeof(Guid))
                    return Guid.Parse(value);

                if (propertyType == typeof(DateTime))
                    return DateTime.Parse(value);

                if (propertyType == typeof(bool))
                    return bool.Parse(value);

                if (propertyType == typeof(int))
                    return int.Parse(value);

                if (propertyType == typeof(int?))
                    if (value != null)
                        return int.Parse(value);

                if (propertyType == typeof(double?))
                    if (value != null)
                        return double.Parse(value);

                if (propertyType.IsEnum)
                    return Enum.Parse(propertyType, value);

                return value;
            }
        }
    }
}