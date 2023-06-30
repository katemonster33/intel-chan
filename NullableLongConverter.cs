using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntelChan
{
    internal class NullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType == JsonTokenType.Null)
            {
                return 0;
            }
            else
            {
                if(reader.TryGetInt64(out long l) )
                {
                    return l;
                }
                else
                {
                    return null;
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if(value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue((decimal)value);
            }
        }
    }
}
