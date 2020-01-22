using System;
using System.ComponentModel;
using System.Globalization;

namespace DSMRParser.Converters
{
    public class ObisTimestampConverter : TypeConverter
    {
        //Timestamps in format: YYMMddHHmmss[W|S]
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string? stringValue = value as string;
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                stringValue = stringValue[0..^1]; //remove 'W' or 'S'
                return DateTime.ParseExact(stringValue, "yyMMddHHmmss", CultureInfo.InvariantCulture);
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
