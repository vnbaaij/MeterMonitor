using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DSMRParser.Models;

namespace DSMRParser
{
    public static class TelegramExtensions
    {
        public static Telegram GetDelta(this Telegram current, Telegram previous)
        {
            Telegram result = new Telegram();

            Type type = typeof(Telegram);

            if (previous == null)
                return current;

            //Loop through each properties inside class and get values for the property from both objects and compare
            foreach (System.Reflection.PropertyInfo property in type.GetProperties())
            {

                string value1 = GetValueAsString(current, type, property);
                string value2 = GetValueAsString(previous, type, property);

                if (value1.Trim() != value2.Trim())
                {
                    property.SetValue(result, type.GetProperty(property.Name)!.GetValue(current, null));
                }
            }

            return result;
        }

        private static string GetValueAsString(Telegram telegram, Type type, PropertyInfo property)
        {
            if (type.GetProperty(property.Name)!.GetValue(telegram, null) != null)
                return type.GetProperty(property.Name)!.GetValue(telegram, null)!.ToString();
            else
                return string.Empty;
        }
    }
}
