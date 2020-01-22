using DSMRParser.Models;
using System;
using System.ComponentModel;
using System.Globalization;

namespace DSMRParser.Converters
{
    public class ObisTariffConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string? stringValue = value as string;
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue switch
                {
                    "0001" => PowerTariff.Low,
                    "0002" => PowerTariff.Normal,
                    _ => throw new NotSupportedException($"Value {stringValue} is not a recognized ObisTariff"),
                };
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
