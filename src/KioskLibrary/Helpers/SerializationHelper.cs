﻿/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace KioskLibrary.Helpers
{
    /// <summary>
    /// Class that provides Serialization/Deserialization surfaces using a common set of options
    /// </summary>
    public class SerializationHelper
    {
        private static JsonSerializerOptions DefaultJsonOptions
        {
            get
            {
                return new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
            }
        }

        /// <summary>
        /// Deserializes the JSON string and returns it as <typeparamref name="T" />
        /// </summary>
        /// <param name="toDeserialize">The string to deserialize</param>
        /// <typeparam name="T">The expected type of the object</typeparam>
        /// <returns>The deserialized object</returns>
        public static T JSONDeserialize<T>(string toDeserialize) => JsonSerializer.Deserialize<T>(toDeserialize, DefaultJsonOptions);

        /// <summary>
        /// Serializes <paramref name="toSerialize" /> as a JSON <see cref="string" />
        /// </summary>
        /// <param name="toSerialize">The object to serialize</param>
        /// <returns>A <see cref="string" /> representation of the object</returns>
        public static string JSONSerialize(object toSerialize) => JsonSerializer.Serialize(toSerialize, DefaultJsonOptions);

        /// <summary>
        /// Deserializes the XML <see cref="Stream"/> and returns it as <typeparamref name="T" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T XMLDeserialize<T>(StringReader stringReader) => (T)new XmlSerializer(typeof(T)).Deserialize(stringReader);

        /// <summary>
        /// Serializes <paramref name="toSerialize" /> as a XML <see cref="string" />
        /// </summary>
        /// <param name="toSerialize">The object to serialize</param>
        /// <returns>A <see cref="string" /> representation of the object</returns>
        public static string XMLSerialize<T>(T toSerialize)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            new XmlSerializer(typeof(T)).Serialize(sw, toSerialize);
            sw.Close();
            return sb.ToString();
        }
    }
}
