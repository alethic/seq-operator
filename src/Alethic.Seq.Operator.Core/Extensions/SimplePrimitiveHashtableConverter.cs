using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alethic.Seq.Operator.Core.Extensions
{

    /// <summary>
    /// Serializers or deserializes a JSON structure to a hierarchy of <see cref="Hashtable"/> and primitive types.
    /// </summary>
    public class SimplePrimitiveHashtableConverter : JsonConverter<Hashtable>
    {

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Hashtable value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType());
        }

        public override Hashtable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (Hashtable?)ReadToken(ref reader, options);
        }

        /// <inheritdoc />
        static object? ReadToken(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    {
                        if (reader.TryGetInt32(out var i))
                            return i;
                        if (reader.TryGetInt64(out var l))
                            return l;
                        if (reader.TryGetDouble(out var d))
                            return d;
                        if (reader.TryGetDecimal(out var m))
                            return m;

                        throw new JsonException("Cannot parse number.");
                    }
                case JsonTokenType.StartArray:
                    {
                        var list = new List<object?>();

                        while (reader.Read())
                        {
                            switch (reader.TokenType)
                            {
                                default:
                                    list.Add(ReadToken(ref reader, options));
                                    break;
                                case JsonTokenType.EndArray:
                                    return list.ToArray();
                            }
                        }

                        throw new JsonException();
                    }
                case JsonTokenType.StartObject:
                    var dict = new Hashtable();

                    while (reader.Read())
                    {
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.EndObject:
                                return dict;
                            case JsonTokenType.PropertyName:
                                var key = reader.GetString();
                                if (key is null)
                                    throw new JsonException("Null keys not supported.");

                                reader.Read();
                                dict[key] = ReadToken(ref reader, options);
                                break;
                            default:
                                throw new JsonException();
                        }
                    }

                    throw new JsonException();
                default:
                    throw new JsonException($"Unknown token {reader.TokenType}.");
            }
        }

    }

}
