using DSMRParser.Models;
using System;
using System.ComponentModel;
using System.Globalization;

namespace DSMRParser.Converters
{
    public class ObisVersionConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue = value as string;
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue switch
                {
                    "20" => ObisVersion.V20,
                    "42" => ObisVersion.V42,
                    "50" => ObisVersion.V50,
                    _ => throw new NotSupportedException($"Value {stringValue} is not a recognized ObisVersion"),
                };
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
