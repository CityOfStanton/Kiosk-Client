namespace KioskClient.Core.Models;

/// <summary>
/// Defines how an image should be stretched to fill its display area.
/// </summary>
public enum ImageStretch
{
    /// <summary>Content is resized to fit in the destination dimensions while preserving its native aspect ratio.</summary>
    Uniform,

    /// <summary>Content is resized to fill the destination dimensions. Aspect ratio is not preserved.</summary>
    Fill,

    /// <summary>Content is resized to fill the destination while preserving aspect ratio. Clips if necessary.</summary>
    UniformToFill,

    /// <summary>Content preserves its original size.</summary>
    None
}
