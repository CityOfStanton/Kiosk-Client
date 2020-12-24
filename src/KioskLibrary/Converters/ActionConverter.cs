/*  
 * Copyright 2020
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KioskLibrary.Converters
{
    public class ActionConverter : JsonConverter<Action>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(Action).IsAssignableFrom(typeToConvert);

        public override Action Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var properties = new List<KeyValuePair<string, string>>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return DeduceAction(properties);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString().ToUpperInvariant();

                    reader.Read();
                    var value = reader.GetString();

                    properties.Add(new KeyValuePair<string, string>(propertyName, value));
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Action Action, JsonSerializerOptions options)
        {
            // No-op. Required to implement, but unused as of now.
        }

        private static Action DeduceAction(List<KeyValuePair<string, string>> objectProperties)
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
                    { // This method may fail on system objects (such as TaskResults).
                        p.SetValue(toReturn, ConvertStringToType(p.PropertyType, property.Value), null);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }

            return toReturn;
        }

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

            if (propertyType == typeof(int))
                return int.Parse(value);

            return value;
        }
    }
}