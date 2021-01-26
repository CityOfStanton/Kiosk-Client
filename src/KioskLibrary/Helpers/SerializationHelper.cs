/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KioskLibrary.Helpers
{
    /// <summary>
    /// Class that provides Serialization/Deserialization surfaces using a common set of options
    /// </summary>
    public class SerializationHelper
    {
        private static JsonSerializerSettings DefaultJsonOptions
        {
            get
            {
                return new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                };
            }
        }

        /// <summary>
        /// Deserializes the string and returns it as <typeparamref name="T" />
        /// </summary>
        /// <param name="toDeserialize">The string to deserialize</param>
        /// <typeparam name="T">The expected type of the object</typeparam>
        /// <returns>The deserialized object</returns>
        public static T Deserialize<T>(string toDeserialize) => JsonConvert.DeserializeObject<T>(toDeserialize, DefaultJsonOptions);

        /// <summary>
        /// Serializes <paramref name="toSerialize" /> as a <see cref="string" />
        /// </summary>
        /// <param name="toSerialize">The object to serialize</param>
        /// <returns>A <see cref="string" /> representation of the object</returns>
        public static string Serialize(object toSerialize) => JsonConvert.SerializeObject(toSerialize, DefaultJsonOptions);
    }
}
