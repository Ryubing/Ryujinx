#nullable enable
using System;
using System.Reflection;
using System.Text;

namespace Ryujinx.Common.Logging.Formatters
{
    internal static class DynamicObjectFormatter
    {
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool = SharedPools.Default<StringBuilder>();

        public static string? Format(object? dynamicObject)
        {
            if (dynamicObject is null)
            {
                return null;
            }

            StringBuilder sb = _stringBuilderPool.Allocate();

            try
            {
                Format(sb, dynamicObject);

                return sb.ToString();
            }
            finally
            {
                _stringBuilderPool.Release(sb);
            }
        }

        public static void Format(StringBuilder sb, object? dynamicObject)
        {
            if (dynamicObject is null)
            {
                return;
            }

            PropertyInfo[] props = dynamicObject.GetType().GetProperties();

            sb.Append('{');

            foreach (PropertyInfo prop in props)
            {
                sb.Append(prop.Name);
                sb.Append(": ");

                if (typeof(Array).IsAssignableFrom(prop.PropertyType))
                {
                    Array? array = (Array?)prop.GetValue(dynamicObject);

                    if (array is not null)
                    {
                        foreach (object? item in array)
                        {
                            sb.Append(item);
                            sb.Append(", ");
                        }

                        if (array.Length > 0)
                        {
                            sb.Remove(sb.Length - 2, 2);
                        }
                    }
                }
                else
                {
                    sb.Append(prop.GetValue(dynamicObject));
                }

                sb.Append(" ; ");
            }

            // We remove the final ';' from the string
            if (props.Length > 0)
            {
                sb.Remove(sb.Length - 3, 3);
            }

            sb.Append('}');
        }
    }
}
