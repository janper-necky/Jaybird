using Grasshopper.Kernel.Types;

namespace Jaybird;

public class GH_Hello : GH_Goo<string>
{
    public GH_Hello()
    {
        Value = string.Empty;
    }

    public GH_Hello(string value)
    {
        Value = value;
    }

    public GH_Hello(GH_Hello other)
    {
        Value = other.Value;
    }

    public override bool IsValid => !string.IsNullOrEmpty(Value);

    public override string TypeName => "Hello";

    public override string TypeDescription => "A hello message";

    public override IGH_Goo Duplicate()
    {
        return new GH_Hello(this);
    }

    public override string ToString()
    {
        return IsValid ? Value : "<invalid>";
    }

    public override bool CastFrom(object source)
    {
        if (source is string text)
        {
            Value = text;
            return true;
        }
        if (source is GH_String ghString)
        {
            Value = ghString.Value;
            return true;
        }
        return false;
    }

    public override bool CastTo<T>(ref T target)
    {
        if (typeof(T) == typeof(string))
        {
            target = (T)(object)Value;
            return true;
        }
        if (typeof(T) == typeof(GH_String))
        {
            target = (T)(object)new GH_String(Value);
            return true;
        }
        return false;
    }
}
