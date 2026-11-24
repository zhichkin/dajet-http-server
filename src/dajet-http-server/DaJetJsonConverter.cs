using DaJet.Json;
using DaJet.Scripting.Model;
using DaJet.TypeSystem;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaJet.Http.Server
{
    public sealed class DaJetJsonConverter : JsonConverter<DataObject>
    {
        private readonly DefineStatement _schema;
        private readonly DataObjectJsonConverter _default = new();
        private readonly static string[] DATETIME_FORMATS = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss"];
        public DaJetJsonConverter(in DefineStatement schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            _schema = schema;
        }
        public override void Write(Utf8JsonWriter writer, DataObject source, JsonSerializerOptions options)
        {
            _default.Write(writer, source, options);
        }

        public override DataObject Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return ParseObject(ref reader);
        }
        private DataObject ParseObject(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected token 'StartObject', but got '{reader.TokenType}'.");
            }

            DataObject target = new();

            string name = null;
            DefineProperty property = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break; // end of target object - return result
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    name = reader.GetString();

                    if (!_schema.TryGet(in name, out property))
                    {
                        reader.Skip();
                    }

                    continue;
                }

                if (reader.TokenType == JsonTokenType.Null)
                {
                    target.SetValue(name, null); // declared.DefaultValue()
                }
                else if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                {
                    ReadBoolean(ref reader, in target, in name, property.Type);
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    ReadNumber(ref reader, in target, in name, property.Type);
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    ReadString(ref reader, in target, in name, property.Type);
                }
                else if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseArray(ref reader, in target, in name, property.Type);
                }
                else //TODO: JsonTokenType.StartObject
                {
                    throw new JsonException($"Unexpected token '{reader.TokenType}'.");
                }
            }

            return target;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadBoolean(ref Utf8JsonReader reader, in DataObject target, in string name, DataType declared)
        {
            if (declared.IsArray || !declared.IsBoolean)
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid boolean value.");
            }

            if (reader.TokenType == JsonTokenType.True)
            {
                target.SetValue(name, true);
            }
            else if (reader.TokenType == JsonTokenType.False)
            {
                target.SetValue(name, false);
            }
            else
            {
                throw new JsonException($"Reading boolean value got unexpected token '{reader.TokenType}'.");
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadNumber(ref Utf8JsonReader reader, in DataObject target, in string name, DataType declared)
        {
            if (declared.IsArray)
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid number value.");
            }

            if (declared.IsDecimal)
            {
                target.SetValue(name, reader.GetDecimal());
            }
            else if (declared.IsInteger)
            {
                if (declared.Size == 8)
                {
                    if (!reader.TryGetInt64(out long int64))
                    {
                        throw new JsonException($"Property '{name}' of type 'integer(8)' got invalid value.");
                    }

                    target.SetValue(name, int64);
                }
                else
                {
                    if (!reader.TryGetInt32(out int integer))
                    {
                        throw new JsonException($"Property '{name}' of type 'integer' got invalid value.");
                    }

                    target.SetValue(name, integer);
                }
            }
            else
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid number value.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadString(ref Utf8JsonReader reader, in DataObject target, in string name, DataType declared)
        {
            if (declared.IsArray)
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid string value.");
            }

            string value = reader.GetString();

            if (declared.IsString)
            {
                target.SetValue(name, value);
            }
            else if (declared.IsDateTime)
            {
                if (DateTime.TryParseExact(value, DATETIME_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime datetime))
                {
                    target.SetValue(name, datetime);
                }
                else
                {
                    throw new JsonException($"Property '{name}' of type 'datetime' got invalid value.");
                }
            }
            else if (declared.IsUuid)
            {
                if (Guid.TryParse(value, out Guid uuid))
                {
                    target.SetValue(name, uuid);
                }
                else
                {
                    throw new JsonException($"Property '{name}' of type 'uuid' got invalid value.");
                }
            }
            else if (declared.IsEntity)
            {
                if (Entity.TryParse(value, out Entity entity))
                {
                    target.SetValue(name, entity);
                }
                else
                {
                    throw new JsonException($"Property '{name}' of type 'entity' got invalid value.");
                }
            }
            else if (declared.IsBinary)
            {
                byte[] binary;

                if (value.StartsWith("0x"))
                {
                    binary = Convert.FromHexString(value[2..]);
                }
                else
                {
                    binary = Convert.FromBase64String(value);
                }

                target.SetValue(name, binary);
            }
            else
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid string value.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ParseArray(ref Utf8JsonReader reader, in DataObject target, in string name, DataType declared)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException($"Expected token 'StartArray', but got '{reader.TokenType}'.");
            }

            if (!declared.IsArray)
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid array value.");
            }

            IList array;

            if (declared.IsBoolean) { array = new List<bool>(); }
            else if (declared.IsDecimal) { array = new List<decimal>(); }
            else if (declared.IsInteger)
            {
                if (declared.Size == 8)
                {
                    array = new List<long>();
                }
                else
                {
                    array = new List<int>();
                }
            }
            else if (declared.IsDateTime) { array = new List<DateTime>(); }
            else if (declared.IsString) { array = new List<string>(); }
            else if (declared.IsBinary) { array = new List<byte[]>(); }
            else if (declared.IsUuid) { array = new List<Guid>(); }
            else if (declared.IsEntity) { array = new List<Entity>(); }
            else //TODO: declared.IsObject
            {
                throw new JsonException($"Property '{name}' of type '{declared}' got invalid array value.");
            }
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break; // end of current array - return result
                }

                if (reader.TokenType == JsonTokenType.True)
                {
                    if (declared.IsBoolean)
                    {
                        array.Add(true);
                    }
                    else
                    {
                        throw new JsonException($"Property '{name}' of type '{declared}' got invalid boolean value.");
                    }
                }
                else if (reader.TokenType == JsonTokenType.False)
                {
                    if (declared.IsBoolean)
                    {
                        array.Add(false);
                    }
                    else
                    {
                        throw new JsonException($"Property '{name}' of type '{declared}' got invalid boolean value.");
                    }
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    if (declared.IsDecimal)
                    {
                        array.Add(reader.GetDecimal());
                    }
                    else if (declared.IsInteger)
                    {
                        if (declared.Size == 8)
                        {
                            if (!reader.TryGetInt64(out long int64))
                            {
                                throw new JsonException($"Property '{name}' of type 'array(integer(8))' got invalid value.");
                            }

                            array.Add(int64);
                        }
                        else
                        {
                            if (!reader.TryGetInt32(out int integer))
                            {
                                throw new JsonException($"Property '{name}' of type 'array(integer)' got invalid value.");
                            }

                            array.Add(integer);
                        }
                    }
                    else
                    {
                        throw new JsonException($"Property '{name}' of type '{declared}' got invalid number value.");
                    }
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    string value = reader.GetString();

                    if (declared.IsString)
                    {
                        array.Add(value);
                    }
                    else if (declared.IsDateTime)
                    {
                        if (DateTime.TryParseExact(value, DATETIME_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime datetime))
                        {
                            array.Add(datetime);
                        }
                        else
                        {
                            throw new JsonException($"Property '{name}' of type 'array(datetime)' got invalid value.");
                        }
                    }
                    else if (declared.IsUuid)
                    {
                        if (Guid.TryParse(value, out Guid uuid))
                        {
                            array.Add(uuid);
                        }
                        else
                        {
                            throw new JsonException($"Property '{name}' of type 'array(uuid)' got invalid value.");
                        }
                    }
                    else if (declared.IsEntity)
                    {
                        if (Entity.TryParse(value, out Entity entity))
                        {
                            array.Add(entity);
                        }
                        else
                        {
                            throw new JsonException($"Property '{name}' of type 'array(entity)' got invalid value.");
                        }
                    }
                    else if (declared.IsBinary)
                    {
                        byte[] binary;

                        try
                        {
                            if (value.StartsWith("0x"))
                            {
                                binary = Convert.FromHexString(value[2..]);
                            }
                            else
                            {
                                binary = Convert.FromBase64String(value);
                            }
                        }
                        catch
                        {
                            throw new JsonException($"Property '{name}' of type 'array(binary)' got invalid value.");
                        }

                        array.Add(binary);
                    }
                    else
                    {
                        throw new JsonException($"Property '{name}' of type '{declared}' got invalid string value.");
                    }
                }
                else //TODO: JsonTokenType.StartObject
                {
                    throw new JsonException($"Reading array value got unexpected token '{reader.TokenType}'.");
                }
            }

            target.SetValue(name, array);
        }
    }
}