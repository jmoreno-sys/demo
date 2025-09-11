using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public static class EnumExtensions
{
    private static Dictionary<KeyValuePair<Type, Enum>, string> _enumIndex;

    public static string GetEnumDescription(this Enum value)
    {
        try
        {
            if (value == null) return "N/A";
            if (_enumIndex == null) _enumIndex = new Dictionary<KeyValuePair<Type, Enum>, string>();
            var key = new KeyValuePair<Type, Enum>(value.GetType(), value);
            if (_enumIndex.ContainsKey(key)) return _enumIndex[key];

            var fi = value.GetType().GetField(value.ToString());
            if (fi != null)
            {
                if (!(fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes) || !attributes.Any()) return value.ToString();
                var result = attributes.First().Description;
                _enumIndex.Add(key, result);
                return result;
            }
        }
        catch (Exception)
        {
            _enumIndex = null;
        }
        return string.Empty;
    }
}
