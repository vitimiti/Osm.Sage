namespace Osm.Sage.WVegas.WMath.Extensions;

/// <summary>
/// Provides extension methods for operations involving single-precision floating-point numbers (float).
/// </summary>
public static class FloatExtensions
{
    /// <summary>
    /// Calculates the inverse square root of the given single-precision floating-point number.
    /// </summary>
    /// <param name="value">The value for which the inverse square root is to be calculated. Must be greater than or equal to <see cref="float.Epsilon"/>.</param>
    /// <returns>The inverse square root of the input value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than or equal to <see cref="float.Epsilon"/> (if <paramref name="value"/> is essentially 0).</exception>
    public static float InverseSqrt(this float value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, float.Epsilon);

        return 1F / (float)Math.Sqrt(value);
    }

    /// <summary>
    /// Computes an approximation of the inverse square root of the given single-precision floating-point number using a fast algorithm.
    /// </summary>
    /// <param name="value">The value for which the approximate inverse square root is to be calculated. Must be greater than or equal to <see cref="float.Epsilon"/>.</param>
    /// <returns>An approximation of the inverse square root of the input value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than <see cref="float.Epsilon"/> (if <paramref name="value"/> is essentially 0).</exception>
    public static float FastInverseSqrt(this float value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, float.Epsilon);

        // Yes, this is a rip-off from the fast inverse square root algorithm from ID software's source code.
        // Yes, it's adapted to C# and therefore it's technically different.
        // No, you probably won't find it much faster than the standard one, but it's here if you need it.
        // This may prove to give better results in AoT compilation.
        const int magic = 0x5F3759DF;
        var half = 0.5f * value;
        var i = BitConverter.SingleToInt32Bits(value);

        i = magic - (i >> 1);
        value = BitConverter.Int32BitsToSingle(i);
        value *= (1.5f - half * value * value);

        return value;
    }
}
