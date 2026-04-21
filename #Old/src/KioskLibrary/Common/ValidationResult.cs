/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace KioskLibrary.Common
{
    /// <summary>
    /// A message that represents a threaded message stack
    /// </summary>
    public class ValidationResult
    {
        private bool _isValid;

        /// <summary>
        /// The id
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The identifier
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Whether or not the validation succeeded
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (Children != null && Children.Count == 0)
                    return _isValid;
                else
                    return Children.All(x => x.IsValid);
            }
            set
            {
                _isValid = value;
            }
        }

        /// <summary>
        /// The description
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The guidance
        /// </summary>
        public string Guidance{ get; set; }

        /// <summary>
        /// The child results
        /// </summary>
        public List<ValidationResult> Children { get; set; }

        /// <summary>
        /// The validation message
        /// </summary>
        public string ValidationMessage
        {
            get
            {
                if(string.IsNullOrEmpty(Message))
                    return Identifier;
                else
                    return $"{Identifier} - {Message}";
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ValidationResult()
        {
            Children = new List<ValidationResult>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifier">The identifier</param>
        /// <param name="isValid">Is the current result valid</param>
        /// <param name="message">The message</param>
        /// <param name="guidance">The guidance</param>
        public ValidationResult(string identifier, bool isValid = false, string message = null, string guidance = null)
            : this()
        {
            Identifier = identifier;
            IsValid = isValid;
            Message = message;
            Guidance = guidance;
        }

        /// <summary>
        /// Gets a validation summary of the children
        /// </summary>
        public string GetValidationSummaryOfChildren()
        {
            if (Children.Any())
                return $"{Constants.ValidationResult.FailedProperties} {(string.Join(", ", Children.Select(x => x.Identifier)))}";
            else
                return Constants.ValidationResult.InsufficientInformation;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Identifier}{(string.IsNullOrEmpty(Message) ? "" : $": {Message}")}";
    }
}
