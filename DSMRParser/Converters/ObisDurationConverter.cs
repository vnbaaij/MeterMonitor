using System;
using System.ComponentModel;
using System.Globalization;

namespace DSMRParser.Converters
{
    public class ObisDurationConverter : TypeConverter
    {
        //Duration in format: 000000xxxx*s
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string? stringValue = value as string;
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                stringValue = stringValue[0..^2]; //remove '*s'
                return int.Parse(stringValue);
            }
            else
            {
                return 0;
            }
        }
    }
}
