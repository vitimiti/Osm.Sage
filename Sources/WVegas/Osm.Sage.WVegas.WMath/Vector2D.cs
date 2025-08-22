using JetBrains.Annotations;
using Osm.Sage.WVegas.WMath.Extensions;
using SysPure = System.Diagnostics.Contracts.PureAttribute;

namespace Osm.Sage.WVegas.WMath;

/// <summary>
/// Represents a 2D vector with X and Y components and provides various utility methods for vector operations.
/// </summary>
/// <param name="X">The X component of the vector.</param>
/// <param name="Y">The Y component of the vector.</param>
[PublicAPI]
public record Vector2D(float X, float Y)
{
    /// <summary>
    /// Gets the squared length (magnitude) of the vector.
    /// </summary>
    /// <remarks>
    /// This property calculates the sum of the squares of the X and Y components of the vector.
    /// It is more efficient than calculating the actual length using <see cref="Length"/>
    /// as it avoids computing the square root.
    /// </remarks>
    /// <value>The squared length of the vector.</value>
    public float LengthSquared => X * X + Y * Y;

    /// <summary>
    /// Gets the length (magnitude) of the vector.
    /// </summary>
    /// <remarks>
    /// This property calculates the square root of the squared length of the vector.
    /// It represents the Euclidean distance of the vector from the origin.
    /// For performance-critical operations where the exact length is not required, use <see cref="LengthSquared"/>.
    /// </remarks>
    /// <value>The length of the vector.</value>
    public float Length => float.Sqrt(LengthSquared);

    /// <summary>
    /// Indicates whether the vector is normalized.
    /// </summary>
    /// <remarks>
    /// A normalized vector has a length of 1. This property is set to <c>true</c>
    /// when the vector is explicitly normalized using appropriate methods, such as
    /// <see cref="Normalize()"/> or <see cref="NormalizeFast()"/>. It does not
    /// automatically verify the length of the vector but relies on the context in
    /// which the vector was created.
    /// </remarks>
    /// <value><c>true</c> if the vector is normalized; otherwise, <c>false</c>.</value>
    public bool IsNormalized { get; private init; }

    /// <summary>
    /// Determines whether both components of the vector are valid numeric values.
    /// </summary>
    /// <remarks>
    /// This property evaluates the X and Y components of the vector to ensure they are not <see cref="float.NaN"/>.
    /// A vector with any component as <see cref="float.NaN"/> is considered invalid.
    /// </remarks>
    /// <value>
    /// true if neither the X nor Y component is <see cref="float.NaN"/>; otherwise, false.
    /// </value>
    public bool IsValid => !float.IsNaN(X) && !float.IsNaN(Y);

    /// <summary>
    /// Calculates the dot product of the current vector and the specified vector.
    /// </summary>
    /// <param name="other">The vector to compute the dot product with.</param>
    /// <returns>The dot product of the current vector and the specified vector.</returns>
    [SysPure]
    public float DotProduct(Vector2D other) => this * other;

    /// <summary>
    /// Calculates the perpendicular dot product of the current vector and the specified vector.
    /// </summary>
    /// <param name="other">The vector to compute the perpendicular dot product with.</param>
    /// <returns>The perpendicular dot product of the current vector and the specified vector.</returns>
    [SysPure]
    public float PerpendicularDotProduct(Vector2D other) => X * (-other.Y) + Y * other.X;

    /// <summary>
    /// Normalizes the vector, ensuring its length becomes 1.
    /// </summary>
    /// <returns>A normalized vector with a length of 1.</returns>
    [SysPure]
    public Vector2D Normalize() =>
        (this * LengthSquared.InverseSqrt()) with
        {
            IsNormalized = true,
        };

    /// <summary>
    /// Normalizes the current vector, returning a unit vector in the same direction.
    /// </summary>
    /// <returns>A normalized vector with a magnitude of 1.</returns>
    [SysPure]
    public Vector2D Normalize(Vector2D other) =>
        (other / LengthSquared.InverseSqrt()) with
        {
            IsNormalized = true,
        };

    /// <summary>
    /// Normalizes the vector to have a length of approximately 1 using a fast inverse square root method.
    /// </summary>
    /// <returns>A new normalized vector with the same direction as the current vector.</returns>
    [SysPure]
    public Vector2D NormalizeFast() =>
        (this * LengthSquared.FastInverseSqrt()) with
        {
            IsNormalized = true,
        };

    /// <summary>
    /// Quickly normalizes the current vector using an approximate inverse square root for performance optimization.
    /// </summary>
    /// <returns>A normalized vector with a magnitude of approximately 1.</returns>
    [SysPure]
    public Vector2D NormalizeFast(Vector2D other) =>
        (other / LengthSquared.FastInverseSqrt()) with
        {
            IsNormalized = true,
        };

    /// <summary>
    /// Rotates the vector by the specified angle.
    /// </summary>
    /// <param name="theta">The angle, in radians, by which to rotate the vector.</param>
    /// <returns>A new vector obtained by rotating the current vector by the specified angle.</returns>
    [SysPure]
    public Vector2D Rotate(float theta) => Rotate(float.Sin(theta), float.Cos(theta));

    /// <summary>
    /// Rotates the vector by the specified sine and cosine values.
    /// </summary>
    /// <param name="sine">The sine of the angle by which to rotate the vector.</param>
    /// <param name="cosine">The cosine of the angle by which to rotate the vector.</param>
    /// <returns>A new vector representing the rotated vector.</returns>
    [SysPure]
    public Vector2D Rotate(float sine, float cosine) =>
        new(X * cosine + Y * (-sine), X * sine + Y * cosine);

    /// <summary>
    /// Rotates the current vector towards the target vector by a maximum angle, constrained by the provided sine and cosine values.
    /// </summary>
    /// <param name="target">The target vector to rotate towards. Must be normalized.</param>
    /// <param name="maxSine">The sine of the maximum angle to rotate by.</param>
    /// <param name="maxCosine">The cosine of the maximum angle to rotate by.</param>
    /// <param name="positiveTurn">Indicates whether to enforce a positive rotation direction.</param>
    /// <returns>A new vector rotated towards the target vector.</returns>
    [SysPure]
    public Vector2D RotateTowards(
        Vector2D target,
        float maxSine,
        float maxCosine,
        bool positiveTurn
    )
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(IsNormalized, true);
        ArgumentOutOfRangeException.ThrowIfNotEqual(target.IsNormalized, true);

        var posTurn = target.PerpendicularDotProduct(this) > 0F;
        if (DotProduct(target) >= maxCosine)
        {
            return target;
        }

        return posTurn ? Rotate(maxSine, maxCosine) : Rotate(-maxSine, maxCosine);
    }

    /// <summary>
    /// Rotates the current vector towards the target vector by a maximum angle,
    /// specified by the sine and cosine of the angle, and considering the direction of rotation.
    /// </summary>
    /// <param name="target">The vector to rotate towards.</param>
    /// <param name="maxTheta">The maximum angle, in radians, that the vector can be rotated.</param>
    /// <param name="positiveTurn">
    /// A boolean indicating whether the rotation should prioritize the positive (counter-clockwise) direction.
    /// If false, the rotation prioritizes the negative (clockwise) direction.
    /// </param>
    /// <returns>
    /// A new vector that is rotated towards the target vector by the specified constraints.
    /// </returns>
    [SysPure]
    public Vector2D RotateTowards(Vector2D target, float maxTheta, bool positiveTurn) =>
        RotateTowards(target, float.Sin(maxTheta), float.Cos(maxTheta), positiveTurn);

    /// <summary>
    /// Returns a new vector where each component is the minimum of the corresponding components of the current vector and the specified vector.
    /// </summary>
    /// <param name="other">The vector to compare component values with.</param>
    /// <returns>A new vector with the minimum component values from both vectors.</returns>
    [SysPure]
    public Vector2D WithMinimum(Vector2D other) =>
        new(float.Min(X, other.X), float.Min(Y, other.Y));

    /// <summary>
    /// Returns a vector composed of the maximum X and Y values between the current vector and the specified vector.
    /// </summary>
    /// <param name="other">The vector to compare against for maximum values.</param>
    /// <returns>A new vector comprising the maximum X and Y values from both vectors.</returns>
    [SysPure]
    public Vector2D WithMaximum(Vector2D other) =>
        new(float.Max(X, other.X), float.Max(Y, other.Y));

    /// <summary>
    /// Scales the current vector by the specified scaling factors for the X and Y components.
    /// </summary>
    /// <param name="x">The scaling factor for the X component.</param>
    /// <param name="y">The scaling factor for the Y component.</param>
    /// <returns>A new vector that is the result of scaling the current vector by the specified factors.</returns>
    [SysPure]
    public Vector2D Scale(float x, float y) => new(X * x, Y * y);

    /// <summary>
    /// Scales the current vector by the specified vector.
    /// </summary>
    /// <param name="other">The vector containing the scaling factors for each component.</param>
    /// <returns>A new vector obtained by scaling the current vector by the specified vector.</returns>
    [SysPure]
    public Vector2D Scale(Vector2D other) => Scale(other.X, other.Y);

    /// <summary>
    /// Approximates the distance between the current vector and the specified coordinates in 2D space.
    /// </summary>
    /// <param name="otherX">The X-coordinate of the other point.</param>
    /// <param name="otherY">The Y-coordinate of the other point.</param>
    /// <returns>An approximation of the distance between the current vector and the specified coordinates.</returns>
    [SysPure]
    public float QuickDistance(float otherX, float otherY)
    {
        float xDiff = float.Abs(X - otherX);
        float yDiff = float.Abs(Y - otherY);

        return xDiff > yDiff ? (yDiff / 2F) + xDiff : (xDiff / 2F) + yDiff;
    }

    /// <summary>
    /// Calculates a quick approximation of the Euclidean distance squared between the current vector and the specified vector.
    /// </summary>
    /// <param name="other">The vector to compute the quick distance squared to.</param>
    /// <returns>An approximate squared distance between the current vector and the specified vector.</returns>
    [SysPure]
    public float QuickDistance(Vector2D other) => QuickDistance(other.X, other.Y);

    /// <summary>
    /// Calculates the Euclidean distance between the current vector and the specified point with given coordinates.
    /// </summary>
    /// <param name="otherX">The x-coordinate of the other point.</param>
    /// <param name="otherY">The y-coordinate of the other point.</param>
    /// <returns>The distance between the current vector and the specified point.</returns>
    [SysPure]
    public float Distance(float otherX, float otherY)
    {
        var xDiff = X - otherX;
        var yDiff = Y - otherY;

        return float.Sqrt((xDiff * xDiff) + (yDiff * yDiff));
    }

    /// <summary>
    /// Calculates the Euclidean distance between the current vector and the specified vector.
    /// </summary>
    /// <param name="other">The vector to compute the distance to.</param>
    /// <returns>The distance between the current vector and the specified vector.</returns>
    [SysPure]
    public float Distance(Vector2D other)
    {
        var temp = this - other;
        return temp.Length;
    }

    /// <summary>
    /// Linearly interpolates between the current vector and the specified vector based on the given parameter.
    /// </summary>
    /// <param name="other">The target vector to interpolate towards.</param>
    /// <param name="t">The interpolation factor, where 0 represents the current vector and 1 represents the target vector.</param>
    /// <returns>A vector that is the linear interpolation between the current vector and the specified vector.</returns>
    [SysPure]
    public Vector2D Lerp(Vector2D other, float t) =>
        new(X + (other.X - X) * t, (Y + (other.Y - Y) * t));

    /// <summary>
    /// Adds two Vector2D instances component-wise.
    /// </summary>
    /// <param name="left">The first Vector2D instance.</param>
    /// <param name="right">The second Vector2D instance.</param>
    /// <returns>A new Vector2D representing the sum of the two vectors.</returns>
    public static Vector2D operator +(Vector2D left, Vector2D right) =>
        new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Subtracts one vector from another vector component-wise.
    /// </summary>
    /// <param name="left">The vector operand on the left-hand side of the subtraction.</param>
    /// <param name="right">The vector operand on the right-hand side of the subtraction.</param>
    /// <returns>A new vector representing the difference of the two vectors.</returns>
    public static Vector2D operator -(Vector2D left, Vector2D right) =>
        new(left.X - right.X, left.Y - right.Y);

    /// <summary>
    /// Defines the multiplication operation between two Vector2D instances,
    /// resulting in the dot product of the two vectors.
    /// </summary>
    /// <param name="left">The first vector in the operation.</param>
    /// <param name="right">The second vector in the operation.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static float operator *(Vector2D left, Vector2D right) =>
        left.X * right.X + left.Y * right.Y;

    /// <summary>
    /// Performs a scalar multiplication on a vector by multiplying each component of the vector by the scalar value.
    /// </summary>
    /// <param name="left">The vector to be multiplied.</param>
    /// <param name="right">The scalar value to multiply the vector by.</param>
    /// <returns>A new vector resulting from the scalar multiplication.</returns>
    public static Vector2D operator *(Vector2D left, float right) =>
        new(left.X * right, left.Y * right);

    /// <summary>
    /// Multiplies the specified scalar value with the given vector.
    /// </summary>
    /// <param name="left">The scalar value to multiply with.</param>
    /// <param name="right">The vector to be scaled.</param>
    /// <returns>A new vector representing the result of the scalar multiplication.</returns>
    public static Vector2D operator *(float left, Vector2D right) => right * left;

    /// <summary>
    /// Divides the components of the vector by the given scalar value.
    /// </summary>
    /// <param name="left">The vector whose components will be divided.</param>
    /// <param name="right">The scalar value by which the vector's components will be divided. Must be greater than or equal to the smallest positive float value.</param>
    /// <returns>A new vector representing the components of the input vector divided by the scalar value.</returns>
    public static Vector2D operator /(Vector2D left, float right)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(right, float.Epsilon);
        return new Vector2D(left.X / right, left.Y / right);
    }
}
