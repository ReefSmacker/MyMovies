using System;
using System.Reflection;

/// <summary>
/// Attribute class to allow string to be associated with enum values.
/// </summary>
public class StringEnum : System.Attribute
{

    private string _value;

    public StringEnum(string value)
    {
        _value = value;
    }

    public string Value
    {
        get { return _value; }
    }

    public static string Get(Enum value)
    {
        string output = string.Empty;
        Type type = value.GetType();

        //Look for our 'StringValueAttribute' 
        //in the field's custom attributes
        FieldInfo fi = type.GetField(value.ToString());
        StringEnum[] attrs = fi.GetCustomAttributes(typeof(StringEnum), false) as StringEnum[];
        if (attrs.Length > 0)
        {
            output = attrs[0].Value;
        }

        return output;
    }
}
