/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KioskLibrary.DataSerialization
{
    public class Serialization
    {
        private static JsonSerializerOptions DefaultJsonOptions
        {
            get
            {
                return new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                };
            }
        }

        public static T Deserialize<T>(string toDeserialize) => JsonSerializer.Deserialize<T>(toDeserialize, DefaultJsonOptions);

        public static string Serialize(object toSerialize) => JsonSerializer.Serialize(toSerialize, DefaultJsonOptions);
    }
}
