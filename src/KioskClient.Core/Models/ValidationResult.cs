namespace KioskClient.Core.Models;

/// <summary>
/// Represents the result of a validation check. Supports hierarchical validation
/// via child results.
/// </summary>
public class ValidationResult
{
    /// <summary>Unique identifier for this validation result.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable name of the item being validated.</summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>Whether this specific validation passed.</summary>
    public bool? IsValid { get; set; }

    /// <summary>Message describing the validation result.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Guidance on how to fix a validation failure.</summary>
    public string Guidance { get; set; } = string.Empty;

    /// <summary>Child validation results for hierarchical validation.</summary>
    public List<ValidationResult> Children { get; set; } = [];

    /// <summary>
    /// Returns true only if this result and all children are valid.
    /// </summary>
    public bool IsFullyValid =>
        IsValid == true && Children.All(c => c.IsFullyValid);

    /// <summary>
    /// Gets the total count of passed validations (this + descendants).
    /// </summary>
    public int PassedCount =>
        (IsValid == true ? 1 : 0) + Children.Sum(c => c.PassedCount);

    /// <summary>
    /// Gets the total count of failed validations (this + descendants).
    /// </summary>
    public int FailedCount =>
        (IsValid == false ? 1 : 0) + Children.Sum(c => c.FailedCount);
}
