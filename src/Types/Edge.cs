using System;
using System.Diagnostics.CodeAnalysis;

namespace Jaybird;

public struct Edge
{
    public int ToNodeIdx;
    public double Length;

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        if (!base.Equals(obj))
        {
            return false;
        }

        var other = (Edge)obj;

        return other.ToNodeIdx == ToNodeIdx && other.Length == Length;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(ToNodeIdx, Length);
    }
}
