using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Squinject
{
    internal static class Extensions
    {
        internal static bool IsValueTypeOrString( this Type type )
        {
            return type.IsValueType || type == typeof( string );
        }

        internal static string ToStringValueType( this object value )
        {
            if ( value is DateTime dtv )
            {
                return dtv.ToString( "o" );
            }
            if ( value is bool dtb )
            {
                return dtb.ToStringLowerCase();
            }
            return value.ToString();
        }

        internal static string ToStringLowerCase( this bool boolean )
        {
            return boolean ? "true" : "false";
        }
    }

    public class DataFlatten
    {
        public Dictionary<string, object> Execute( object @object, string prefix = "" )
        {
            var dictionary = new Dictionary<string, object>();
            Flatten( dictionary, @object, prefix );
            return dictionary;
        }

        private static void Flatten(
            IDictionary<string, object> dictionary,
            object source,
            string name )
        {
            var properties = source.GetType().GetProperties().Where(x => x.CanRead);
            foreach ( var property in properties )
            {
                var key = string.IsNullOrWhiteSpace(name) ? property.Name : $"{name}.{property.Name}";
                var value = property.GetValue(source, null);

                if ( value == null )
                {
                    dictionary[key] = null;
                    continue;
                }

                if ( property.PropertyType.IsValueTypeOrString() )
                {
                    dictionary[key] = value.ToStringValueType();
                }
                else
                {
                    dictionary[key] = JsonConvert.DeserializeObject( JsonConvert.SerializeObject( value ) );
                }
            }
        }
    }
}
