using YamlDotNet.Serialization;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;

namespace EveSde
{
    public class EveSdeClient : IEveSdeClient
    {
        IConfiguration Config { get; }

        Dictionary<uint, string> idsToNames = new Dictionary<uint, string>();

        public EveSdeClient(IConfiguration config)
        {
            Config = config;
        }

        public bool Start()
        {
            string invNamesPath = Config["invNames-path"];
            if(string.IsNullOrEmpty(invNamesPath))
            {
                return false;
            }
            if (File.Exists(invNamesPath))
            {
                var deserializer = new DeserializerBuilder().Build();
                List<IdNamePair> idNamePairs = deserializer.Deserialize<List<IdNamePair>>(File.OpenText(invNamesPath));
                foreach (var idNamePair in idNamePairs)
                {
                    idsToNames[(uint)idNamePair.itemID] = idNamePair.itemName;
                }
            }
            return true;
        }

        public void Dispose()
        {
        }
        
        public string GetName(uint id)
        {
            string? output = string.Empty;
            if(!idsToNames.TryGetValue(id, out output))
            {
                output = "(BAD ID)";
            }
            return output;
        }
    }
}