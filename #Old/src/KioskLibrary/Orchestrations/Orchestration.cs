/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using KioskLibrary.Common;
using KioskLibrary.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Web.Http;
using Action = KioskLibrary.Actions.Action;

namespace KioskLibrary.Orchestrations
{
    /// <summary>
    /// An orchestration
    /// </summary>
    public class Orchestration
    {
        /// <summary>
        /// A name to represent this orchestration
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The version of this orchestration
        /// </summary>
        public string Version { get; set; } = Constants.Orchestrations.CurrentVersion;

        /// <summary>
        /// The interval used to check for updated versions of this <see cref="Orchestration" />
        /// </summary>
        public int PollingIntervalMinutes { get; set; }

        /// <summary>
        /// The lifecycle behavior of an <see cref="Orchestration" />
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LifecycleBehavior Lifecycle { get; set; } = LifecycleBehavior.SingleRun;

        /// <summary>
        /// The order to iterate through the set of <see cref="Orchestration.Actions" />
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public Ordering Order { get; set; } = Ordering.Sequential;

        /// <summary>
        /// The source of this <see cref="Orchestration" />
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public OrchestrationSource OrchestrationSource { get; set; }

        /// <summary>
        /// A list of <see cref="Action" />s to process
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public List<Action> Actions { get; set; }

        /// <summary>
        /// The validation results for this <see cref="Orchestration" />
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public ObservableCollection<ValidationResult> ValidationResult { get; set; }

        /// <summary>
        /// Is this <see cref="Orchestration" /> valid?
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public bool IsValid
        {
            get
            {
                return ValidationResult?.All(x => x.IsValid) ?? false;
            }
        }

        /// <summary>
        /// The count of validations that passed
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public int ValidationPassedCount
        {
            get
            {
                return CountValidationResults(ValidationResult.FirstOrDefault(), true);
            }
        }

        /// <summary>
        /// The count of validations that failed
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public int ValidationFailedCount
        {
            get
            {
                return CountValidationResults(ValidationResult.FirstOrDefault(), false);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Orchestration()
        {
            Actions = new List<Action>();
            ValidationResult = new ObservableCollection<ValidationResult>();
        }

        /// <summary>
        /// The HTTP helper
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public IHttpHelper HttpHelper { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">A name to represent this orchestration</param>
        /// <param name="actions">A list of <see cref="Action" />s to process</param>
        /// <param name="pollingInterval">The interval used to check for updated versions of this <see cref="Orchestration" /></param>
        /// <param name="orchestrationSource">The source of this <see cref="Orchestration" /></param>
        /// <param name="lifecycle">The lifecycle behavior of an <see cref="Orchestration" /></param>
        /// <param name="order">The order to iterate through the set of <see cref="Actions" /></param>
        public Orchestration(string name, List<Action> actions, int pollingInterval, OrchestrationSource orchestrationSource, LifecycleBehavior lifecycle = LifecycleBehavior.SingleRun, Ordering order = Ordering.Sequential, IHttpHelper httpHelper = null)
            : this()
        {
            Name = name;
            PollingIntervalMinutes = pollingInterval;
            Actions = actions;
            Lifecycle = lifecycle;
            Order = order;
            OrchestrationSource = orchestrationSource;
            HttpHelper = httpHelper ?? new HttpHelper();
        }

        /// <summary>
        /// Gets the <see cref="Orchestration" /> from the specified <paramref name="uri" />
        /// </summary>
        /// <param name="uri">The URI where the <see cref="Orchestration" /> is stored</param>
        /// <param name="httpHelper">The <see cref="IHttpHelper"/> to use for HTTP requests</param>
        /// <returns>An <see cref="Orchestration" /> if the it could be retrieved, else <see cref="null"/></returns>
        public async static Task<Orchestration> GetOrchestration(Uri uri, IHttpHelper httpHelper)
        {
            try
            {
                var result = await httpHelper.GetAsync(uri);
                if (result.StatusCode == HttpStatusCode.Ok)
                    return ConvertStringToOrchestration(await result.Content.ReadAsStringAsync());
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Converts a string to an <see cref="Orchestration" />
        /// </summary>
        /// <param name="orchestrationAsString">The <see cref="Orchestration" /> as a <see cref="string" /></param>
        /// <returns>An <see cref="Orchestration" /> if the it could be parsed, else <see cref="null"/></returns>
        public static Orchestration ConvertStringToOrchestration(string orchestrationAsString)
        {
            try
            {
                // Try to parse the text as JSON
                return SerializationHelper.JSONDeserialize<Orchestration>(orchestrationAsString);
            }
            catch (JsonException)
            {
                // Try to parse the text as XML
                using var sr = new StringReader(orchestrationAsString);
                try
                {
                    return SerializationHelper.XMLDeserialize<Orchestration>(sr);
                }
                catch { }
                finally
                {
                    sr.Close();
                }
            }
            catch (ArgumentNullException) { return null; }

            return null;
        }

        /// <summary>
        /// Validates this <see cref="Orchestration" />
        /// </summary>
        /// <remarks>This method stores its results in the <see cref="ValidationResult" /></remarks>
        public async Task ValidateAsync()
        {
            var result = new ValidationResult(Name);

            if (PollingIntervalMinutes < 15)
                result.Children.Add(new ValidationResult(nameof(PollingIntervalMinutes), false, Constants.Validation.Orchestration.InvalidPollingInterval, Constants.Validation.Orchestration.InvalidPollingInterval));
            else
                result.Children.Add(new ValidationResult(nameof(PollingIntervalMinutes), true, Constants.Validation.Actions.Valid, Constants.Validation.Orchestration.InvalidPollingInterval));

            if (Actions != null)
                foreach (var a in Actions)
                {
                    var actionValidationResult = await a.ValidateAsync(HttpHelper);
                    result.Children.Add(actionValidationResult);
                }

            ValidationResult?.Clear();
            ValidationResult.Add(result);
        }

        /// <summary>
        /// Gets the <see cref="ValidationResult"/>s that match the <paramref name="targetIsValidResult"/>
        /// </summary>
        /// <param name="validationResult">The <see cref="ValidationResult"/> to interrogate</param>
        /// <param name="targetIsValidResult">The target result</param>
        /// <param name="matchingResults">A list of <see cref="ValidationResult"/>s that match the <paramref name="targetIsValidResult"/></param>
        public void GetValidationResults(ValidationResult validationResult, bool targetIsValidResult, ref List<ValidationResult> matchingResults)
        {
            if (validationResult != null)
                if (validationResult.Children.Any()) // Non-leaf node
                    foreach (var child in validationResult.Children)
                        GetValidationResults(child, targetIsValidResult, ref matchingResults);
                else // Leaf node
                    if (validationResult.IsValid == targetIsValidResult)
                    matchingResults.Add(validationResult);
        }

        /// <summary>
        /// Gets the count of <see cref="ValidationResult"/>s that match the <paramref name="targetResult"/>
        /// </summary>
        /// <param name="validationResult">The <see cref="ValidationResult"/> to interrogate</param>
        /// <param name="targetResult">The target result</param>
        /// <returns>The count of <see cref="ValidationResult"/>s that match the <paramref name="targetResult"/></returns>
        public int CountValidationResults(ValidationResult validationResult, bool targetResult)
        {
            var matchingResults = new List<ValidationResult>();
            GetValidationResults(validationResult, targetResult, ref matchingResults);
            return matchingResults.Count;
        }
    }
}
