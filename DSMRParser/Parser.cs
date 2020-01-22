using DSMRParser.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DSMRParser
{
    public class Parser
    {
        public delegate void TelegramParsedEventHandler(object sender, Telegram telegram);

        public static Encoding TelegramEncoding => Encoding.ASCII;

        private const char telegramStart = '/';
        private const char telegramEnd = '!';
        private const char valueStart = '(';
        private const char valueEnd = ')';
        private const string errorLogStart = "1-0:99.97.0";
        private const string obisErrorLog = "0-0:96.7.19";

        public async Task<Telegram> ParseFromString(string message)
        {
            Telegram telegram = new Telegram();
            using (StringReader reader = new StringReader(message))
            {
                await ParseFromStringReader(reader, (object sender, Telegram output) =>
                {
                    telegram = output;
                });
            }
            return telegram;
        }

        public async Task ParseFromStream(Stream stream, TelegramParsedEventHandler onParsedEvent)
        {
            Byte[] buffer = new byte[8192];
            int count;

            while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (count != 0)
                {
                    using TextReader reader = new StringReader(TelegramEncoding.GetString(buffer, 0, buffer.Length));
                    await ParseFromTextReader(reader, onParsedEvent);
                }
            }
        }

        public async Task ParseFromStringReader(StringReader reader, TelegramParsedEventHandler onParsedEvent)
        {
            await ParseFromTextReader(reader, onParsedEvent);
            return;
        }

        public async Task ParseFromStreamReader(StreamReader reader, TelegramParsedEventHandler onParsedEvent)
        {
            await ParseFromTextReader(reader, onParsedEvent);
            return;
        }

        public async Task ParseFromTextReader(TextReader reader, TelegramParsedEventHandler onParsedEvent)
        {
            Telegram? telegram = null;

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (telegram == null)
                {
                    if (line.StartsWith(telegramStart.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        telegram = new Telegram();
                        SetTelegramHeader(ref telegram, line);
                    }
                }
                else
                {
                    if (line.StartsWith(telegramEnd.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        SetTelegramCosmosProperties(ref telegram);
                        SetTelegramCRC(ref telegram, line);
                        onParsedEvent?.Invoke(this, telegram);
                        return;
                    }
                    else
                    {
                        SetTelegramContent(ref telegram, line);
                    }
                }
            }
        }

        private static void SetTelegramContent(ref Telegram telegram, string line)
        {
            var parsed = ParseContentLine(line);
            var key = parsed.ElementAtOrDefault(0);
            var properties = GetPropertiesWithKey(key);

            var values = parsed.Skip(1);
            if (parsed.Any() && parsed.Count() > 1 && properties.Any())
            {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                SetTelegramProperties(ref telegram, properties, values);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                if (key == errorLogStart)
                {

                    SetPowerFailureEvents(ref telegram, values);
                }
            }
            telegram.Lines.Add(line);
        }

        private static void SetPowerFailureEvents(ref Telegram telegram, IEnumerable<string> values)
        {

            List<PowerFailureEvent> powerFailureEvents = new List<PowerFailureEvent>();

            for (int i = 2; i <= (values.Count()-2); i += 2)
            {
                PowerFailureEvent powerFailureEvent = new PowerFailureEvent();

                SetPowerFailureEventValue(ref powerFailureEvent, "Timestamp", values.ElementAtOrDefault(i));
                SetPowerFailureEventValue(ref powerFailureEvent, "Duration", values.ElementAtOrDefault(i+1));

                powerFailureEvents.Add(powerFailureEvent);
            }

            var propName = GetPropertiesWithKey(obisErrorLog).ElementAtOrDefault(0);
#pragma warning disable CS8604 // Possible null reference argument.
            PropertyInfo propertyInfo = GetTelegramProperty(propName);
#pragma warning restore CS8604 // Possible null reference argument.
            propertyInfo.SetValue(telegram, powerFailureEvents);
        }

        private static void SetPowerFailureEventValue(ref PowerFailureEvent powerFailureEvent, string name, string value)
        {
            PropertyInfo propertyInfo;

            propertyInfo = GetPowerFailureEventProperty(name);

            var convertedValue = GetConvertedPropertyValue(propertyInfo, value);

            propertyInfo.SetValue(powerFailureEvent, convertedValue);

        }

        private static IEnumerable<string> ParseContentLine(string line)
        {
            var parts = line.Split(valueStart);
            for (var i = 0; i < parts.Length; i++)
            {
                var endIndex = parts[i].IndexOf(valueEnd);
                if (endIndex > -1)
                {
                    parts[i] = parts[i].Substring(0, endIndex);
                }
                yield return parts[i];
            }
        }

        private static IEnumerable<string?> GetPropertiesWithKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                yield return null;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            foreach (PropertyInfo property in GetTelegramProperties())
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var attr = property.GetCustomAttributes(typeof(ObisAttribute), false).Cast<ObisAttribute>().FirstOrDefault();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                if (attr != null && attr.ObisIdentifier == key)
                {
                    yield return property.Name;
                }
            }
        }

        private static void SetTelegramCosmosProperties(ref Telegram telegram)
        {
            telegram.Id = telegram.Timestamp.ToString("yyyyMMddHHmmss");
            telegram.Key = telegram.Timestamp.ToString("yyyyMMdd");
        }

        private static void SetTelegramHeader(ref Telegram telegram, string value)
        {
            var header = value.Replace(telegramStart.ToString(), string.Empty);
            var propertyInfo = GetTelegramProperty(nameof(Telegram.MessageHeader));
            SetPropertyValue(ref telegram, propertyInfo, header);
            telegram.Lines.Add(value);
        }

        private static void SetTelegramCRC(ref Telegram telegram, string value)
        {
            if (telegram.MessageVersion == ObisVersion.V42 || telegram.MessageVersion == ObisVersion.V50)
            {
                var crc = value.Replace(telegramEnd.ToString(), string.Empty);
                crc = crc.Length > 4 ? crc.Substring(0, 4) : crc;
                var propertyInfo = GetTelegramProperty(nameof(Telegram.CRC));
                SetPropertyValue(ref telegram, propertyInfo, crc);
                telegram.Lines.Add(telegramEnd + crc);
            }
            else
            {
                telegram.Lines.Add(telegramEnd.ToString());
            }
        }

        private static void SetTelegramProperties(ref Telegram telegram, IEnumerable<string> properties, IEnumerable<string> values)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            IEnumerable<PropertyInfo?> telegramProperties = GetTelegramProperties().Where(p => properties.Contains(p.Name));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            foreach (PropertyInfo propertyInfo in telegramProperties)
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            {
                var obisAttribute =
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    propertyInfo.GetCustomAttributes(typeof(ObisAttribute), false)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        .Cast<ObisAttribute>()
                        .FirstOrDefault();

                if (obisAttribute == null)
                {
                    continue;
                }

                var valueForProperty = values.ElementAtOrDefault(obisAttribute.ValueIndex);
                if (valueForProperty == null)
                {
                    continue;
                }

                SetPropertyValue(ref telegram, propertyInfo, valueForProperty, obisAttribute.ValueUnit);
            }
        }

        private static void SetPropertyValue(ref Telegram telegram, PropertyInfo propertyInfo, string value, string? obisValueUnit = null)
        {
            var convertedValue = GetConvertedPropertyValue(propertyInfo, value, obisValueUnit);
            propertyInfo.SetValue(telegram, convertedValue);
        }

        private static object GetConvertedPropertyValue(PropertyInfo propertyInfo, string value, string? obisValueUnit = null)
        {
            TypeConverter? converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
            var converterAttribute =
                propertyInfo.GetCustomAttributes(typeof(TypeConverterAttribute), false)
                    .Cast<TypeConverterAttribute>()
                    .FirstOrDefault();

            if (converterAttribute != null)
            {
                var converterType = Type.GetType(converterAttribute.ConverterTypeName);
#pragma warning disable CS8604 // Possible null reference argument.
                converter = Activator.CreateInstance(converterType) as TypeConverter;
#pragma warning restore CS8604 // Possible null reference argument.
            }

            if (!string.IsNullOrEmpty(obisValueUnit))
            {
                value = value.Replace("*" + obisValueUnit, string.Empty);
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return converter.ConvertFromInvariantString(value);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        private static IEnumerable<PropertyInfo?> GetTelegramProperties()
        {
            return typeof(Telegram).GetTypeInfo().DeclaredProperties;
        }

        private static PropertyInfo GetTelegramProperty(string propertyName)
        {
            return typeof(Telegram).GetTypeInfo().GetDeclaredProperty(propertyName);
        }

        private static PropertyInfo GetPowerFailureEventProperty(string propertyName)
        {
            return typeof(PowerFailureEvent).GetTypeInfo().GetDeclaredProperty(propertyName);
        }
    }
}