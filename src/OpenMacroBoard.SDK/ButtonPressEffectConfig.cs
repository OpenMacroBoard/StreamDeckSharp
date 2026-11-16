using System.Diagnostics.CodeAnalysis;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Configuration for <see cref="ButtonPressEffectAdapter"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class ButtonPressEffectConfig
{
    /// <summary>
    /// Gets or sets a factor that determines how much the images gets smaller or even bigger when pressed.
    /// </summary>
    /// <remarks>
    /// <para>It's basically a scale factor, if you want the image to be half the size use 0.5. One means no change
    /// and values larger than one make the image bigger when pressed.</para>
    /// </remarks>
    public double Scale { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the relative x coordinate of the origin.
    /// </summary>
    public double OriginX { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the relative y coordinate of the origin.
    /// </summary>
    public double OriginY { get; set; } = 0.5;

    /// <summary>
    /// The background color that is used when the button is shrunk.
    /// </summary>
    public OmbColor BackgroundColor { get; set; } = OmbColor.Black;
}
