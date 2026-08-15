namespace ViciOne.Communication.Vse.Data;

internal static class FloatExtensions
{
    private const float Epsilon = 0.0000001f;

    /// <summary>
    /// Compares two device-reported floats for the purpose of deciding whether a value "actually changed" (and
    /// a Changed event should fire). Unlike <c>==</c>, <see cref="float.Equals(float)"/> treats NaN as equal to
    /// itself, so a value that stays NaN (e.g. while monitoring is disabled) doesn't spuriously look changed.
    /// </summary>
    public static bool IsNearlyEqualTo(this float value, float other)
        => value.Equals(other) || MathF.Abs(value - other) < Epsilon;
}
