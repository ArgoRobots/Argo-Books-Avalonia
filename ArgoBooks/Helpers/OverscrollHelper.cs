using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Helpers;

/// <summary>
/// Helper class for implementing rubber band overscroll effects.
/// Provides consistent overscroll behavior across scrollable content.
/// </summary>
public class OverscrollHelper
{
    /// <summary>
    /// How much resistance when overscrolling (0-1, lower = more resistance).
    /// </summary>
    public const double DefaultResistance = 0.3;

    /// <summary>
    /// Maximum overscroll distance in pixels.
    /// </summary>
    public const double DefaultMaxDistance = 100;

    private readonly LayoutTransformControl _transformControl;
    private readonly double _resistance;
    private readonly double _maxDistance;

    private Vector _overscroll;

    /// <summary>
    /// Gets the current overscroll vector.
    /// </summary>
    public Vector Overscroll => _overscroll;

    /// <summary>
    /// Gets whether there is any current overscroll.
    /// </summary>
    public bool HasOverscroll => _overscroll.X != 0 || _overscroll.Y != 0;

    /// <summary>
    /// Creates a new OverscrollHelper for the specified transform control.
    /// </summary>
    /// <param name="transformControl">The LayoutTransformControl to apply overscroll transforms to.</param>
    /// <param name="resistance">How much resistance when overscrolling (0-1).</param>
    /// <param name="maxDistance">Maximum overscroll distance in pixels.</param>
    public OverscrollHelper(
        LayoutTransformControl transformControl,
        double resistance = DefaultResistance,
        double maxDistance = DefaultMaxDistance)
    {
        _transformControl = transformControl;
        _resistance = resistance;
        _maxDistance = maxDistance;
    }

    /// <summary>
    /// Calculates overscroll values based on desired scroll position and bounds.
    /// </summary>
    /// <param name="desiredX">Desired X offset.</param>
    /// <param name="desiredY">Desired Y offset.</param>
    /// <param name="maxX">Maximum X scroll offset.</param>
    /// <param name="maxY">Maximum Y scroll offset.</param>
    /// <returns>Tuple of (clampedX, clampedY, overscrollX, overscrollY).</returns>
    public (double ClampedX, double ClampedY, double OverscrollX, double OverscrollY) CalculateOverscroll(
        double desiredX, double desiredY, double maxX, double maxY)
    {
        double overscrollX = 0;
        double overscrollY = 0;
        double clampedX = desiredX;
        double clampedY = desiredY;

        if (desiredX < 0)
        {
            overscrollX = desiredX * _resistance;
            overscrollX = Math.Max(overscrollX, -_maxDistance);
            clampedX = 0;
        }
        else if (desiredX > maxX)
        {
            overscrollX = (desiredX - maxX) * _resistance;
            overscrollX = Math.Min(overscrollX, _maxDistance);
            clampedX = maxX;
        }

        if (desiredY < 0)
        {
            overscrollY = desiredY * _resistance;
            overscrollY = Math.Max(overscrollY, -_maxDistance);
            clampedY = 0;
        }
        else if (desiredY > maxY)
        {
            overscrollY = (desiredY - maxY) * _resistance;
            overscrollY = Math.Min(overscrollY, _maxDistance);
            clampedY = maxY;
        }

        return (clampedX, clampedY, overscrollX, overscrollY);
    }

    /// <summary>
    /// Applies overscroll and updates the visual transform.
    /// </summary>
    /// <param name="overscrollX">Overscroll X value.</param>
    /// <param name="overscrollY">Overscroll Y value.</param>
    public void ApplyOverscroll(double overscrollX, double overscrollY)
    {
        _overscroll = new Vector(overscrollX, overscrollY);
        ApplyTransform();
    }

    /// <summary>
    /// Applies the current overscroll as a visual transform.
    /// The overscroll is inverted because dragging right should show content from left.
    /// </summary>
    private void ApplyTransform()
    {
        var translateTransform = new TranslateTransform(-_overscroll.X, -_overscroll.Y);
        _transformControl.RenderTransform = translateTransform;
    }

    /// <summary>
    /// Animates the overscroll back to zero with a spring-like effect.
    /// </summary>
    public async Task AnimateSnapBackAsync()
    {
        const int steps = 12;
        const int delayMs = 16; // ~60fps

        var startOverscroll = _overscroll;

        for (int i = 1; i <= steps; i++)
        {
            // Ease-out curve for smooth deceleration (cubic ease-out)
            double t = i / (double)steps;
            double easeOut = 1 - Math.Pow(1 - t, 3);

            _overscroll = new Vector(
                startOverscroll.X * (1 - easeOut),
                startOverscroll.Y * (1 - easeOut)
            );

            ApplyTransform();
            await Task.Delay(delayMs);
        }

        // Ensure we end at exactly zero
        _overscroll = new Vector(0, 0);
        ApplyTransform();
    }

    /// <summary>
    /// Resets the overscroll to zero without animation.
    /// </summary>
    public void Reset()
    {
        _overscroll = new Vector(0, 0);
        ApplyTransform();
    }
}
