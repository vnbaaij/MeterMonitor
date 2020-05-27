using System.IO;
using System.Text.Json;
using Azure.Cosmos.Serialization;

namespace MeterMonitor
{
    internal class CosmosJsonSerializer : CosmosSerializer
    {

        public override T FromStream<T>(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            stream.Close();
            var item = JsonSerializer.Deserialize<T>(memoryStream.ToArray());
            return item;
        }

        public override Stream ToStream<T>(T input)
        {
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(input,options));
        }

    }
}
