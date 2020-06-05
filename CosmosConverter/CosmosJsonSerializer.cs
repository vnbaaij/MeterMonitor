using System.IO;
using System.Text.Json;
using Azure.Cosmos.Serialization;

namespace CosmosConverter
{

    internal class CosmosJsonSerializer : CosmosSerializer
    {

        public override T FromStream<T>(Stream stream)
        {
            var options = new JsonSerializerOptions
            {
                //IgnoreNullValues = true,
                //PropertyNamingPolicy = JsonNamingPolicy.CamelCase

            };

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            stream.Close();
            var item = JsonSerializer.Deserialize<T>(memoryStream.ToArray(), options);
            return item;
        }

        public override Stream ToStream<T>(T input)
        {
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(input, options));
        }

    }
}
