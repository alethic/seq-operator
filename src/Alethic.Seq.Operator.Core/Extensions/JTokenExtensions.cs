using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Alethic.Seq.Operator.Core.Extensions
{

    public static class JTokenExtensions
    {

        /// <summary>
        /// Copies the <see cref="JToken"/> to an anonymous hierarchy where objects are dictionaries.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static object? ToDictionary(this JToken token)
        {
            return token switch
            {
                JObject o => ToDictionary(o),
                JArray a => ToDictionary(a),
                JValue v => ToDictionary(v),
                _ => throw new InvalidOperationException(),
            };
        }

        static IDictionary<string, object?>? ToDictionary(JObject o)
        {
            if (o is null)
                return null;

            var dict = new Dictionary<string, object?>();
            foreach (var prop in o.Properties())
                dict[prop.Name] = prop.Value.ToDictionary();

            return dict;
        }

        static object?[]? ToDictionary(JArray a)
        {
            if (a is null)
                return null;

            var list = new List<object?>();
            foreach (var item in a)
                list.Add(item.ToDictionary());

            return list.ToArray();
        }

        static object? ToDictionary(JValue v)
        {
            return v.Type switch
            {
                JTokenType.Integer => v.Value,
                JTokenType.Float => v.Value,
                JTokenType.String => v.Value,
                JTokenType.Boolean => v.Value,
                JTokenType.Null => null,
                _ => throw new InvalidOperationException(),
            };
        }

    }

}
