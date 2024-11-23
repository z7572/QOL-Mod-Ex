using System;
using System.Reflection;

namespace QOL
{
    class Utils
    {
        public static T GetFieldValue<T>(object obj, string fieldName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new ArgumentException("fieldName", "No such field was found.");
            }
            if (!typeof(T).IsAssignableFrom(field.FieldType))
            {
                throw new InvalidOperationException($"Field type {typeof(T)} and requested type {field.FieldType} are not compatible.");
            }
            return (T)((object)field.GetValue(obj));
        }

        public static void SetFieldValue<T>(object obj, string fieldName, object value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new ArgumentException("fieldName", "No such field was found.");
            }
            if (!typeof(T).IsAssignableFrom(field.FieldType))
            {
                throw new InvalidOperationException($"Field type {typeof(T)} and requested type {field.FieldType} are not compatible.");
            }
            field.SetValue(obj, (T)((object)value));
        }
    }
}
