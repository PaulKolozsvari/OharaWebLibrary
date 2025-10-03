namespace Ohara.Library.Base.Methods
{
    #region Using Directives

    using Ohara.Library.Session.Classes;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion //Using Directives

    public static class OharaUtilities
    {

        public static T GetEntityFromDictionary<T>(this Dictionary<string, string> dictionary, T originalEntity) where T : class, new()
        {
            var entity = originalEntity ?? new T(); //If the original entity is not null, use it to update its properties , otherwise create a new entity.
            var entityType = entity.GetType();
            var entityProperties = entityType.GetProperties().ToList();

            foreach (KeyValuePair<string, string> htmlField in dictionary)
            {
                var entityProperty = entityType.GetProperty(htmlField.Key); //Check if the entity has a property with the html field name.
                if (entityProperty == null || string.IsNullOrEmpty(htmlField.Value))
                {
                    continue; //There is not property on the entity with the given html field name.
                }

                var entityPropertyType = entityProperty.PropertyType;
                object entityPropertyValue = Convert.ChangeType(htmlField.Value, entityPropertyType);
                entityProperty.SetValue(entity, entityPropertyValue, null);
            }
            return entity;
        }

        public static string ToSentence(this string value)
        {
            int length = value.Length;
            string output = value;
            List<char> capitalLetters = output.Where(x => char.IsUpper(x)).Distinct().ToList();
            foreach (char item in capitalLetters)
            {
                if (output.Contains(item))
                {
                    output = output.Replace(item.ToString(), $" {item}");
                }
            }
            output = output.TrimStart();
            return output;
        }

        public static void Add(this Dictionary<string, string> dictionary, Dictionary<string, string> keyValuePairs)
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<string, string>();
            }
            foreach (var pair in keyValuePairs)
            {
                dictionary.Add(pair.Key, pair.Value);
            }
        }
        public static T GetProperty<T>(this OharaPageState session, string propertyName) {
            return session.GetProperty<T>(propertyName, default(T));
        }

        public static T? GetProperty<T>(this OharaPageState session, string propertyName, object defaultValue)
        {
            object? output = null;
            Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (session == null || session.Properties == null)
            {
                return (T)defaultValue;
            }

            KeyValuePair<string, string> pair = session.Properties.FirstOrDefault(x => x.Key == propertyName);
            if (pair.Value == null)
            {
                return (T)defaultValue;
            }

            output = ConvertObjectToType(pair.Value, type) ?? defaultValue;
            return (T)output;
        }

        public static object? ConvertObjectToType(object? input, Type type) {
            
            if(input == null || type == null)
            {
                return default;
            }

            object output = null;
            string value = input.ToString(); 
            
            switch (type)
            {
                case Type t when t == typeof(Guid): output = Convert.ChangeType(value.ToGuid(), type); break;
                case Type t when t == typeof(DateTime): output = Convert.ChangeType(value.ToDateTime(), type); break;
                case Type t when t == typeof(string): output = value.ToString(); break;
                case Type t when t == typeof(int): output = value.ToNullableInt(); break;
                case Type t when t == typeof(bool): output = value.ToNullableBool(); break;
            };

            return output;
        }

        public static Guid? ToGuid(this string value)
        {
            Guid.TryParse(value.ToString(), out Guid result);
            return result;
        }
        public static DateTime? ToDateTime(this string value)
        {
            DateTime.TryParse(value, out DateTime result);
            return result;
        }

        public static bool KeyValuePairExists(Dictionary<string, string> dictionary, string propertyName)
        {
            bool output = dictionary != null && dictionary.Any(x => x.Key == propertyName);
            return output;
        }

        public static int? ToNullableInt(this string input)
        {
            int output;
            if (int.TryParse(input, out output))
            {
                return output;
            }
            return null;
        }
        public static bool? ToNullableBool(this string input)
        {
            bool output;
            if (bool.TryParse(input, out output))
            {
                return output;
            }
            return null;
        }
    }
}
