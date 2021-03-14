using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Tripwire;

namespace Tripwire
{
    public class LocalTripwireData : ITripwireDataProvider
    {
        private const string ConnectionString = "Default";
        public DateTime SyncTime { get => _syncTime; }
        public IList<string> SystemIds { get => new List<string> { Config["HomeSystemId"] }; }
        private DateTime _syncTime;
        IConfiguration Config { get; }

        public LocalTripwireData(IConfiguration configuration)
        {
            Config = configuration;
        }


        public async Task<IList<Wormhole>> GetHoles()
        {
            using var conn = GetConnection();
            var cmd = new MySqlCommand("select * from wormholes", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var retList = new List<Wormhole>();
            while (reader.Read())
            {

                var newHole = new Wormhole
                {
                    InitialID = reader.GetInt32("initialID").ToString(),
                    MaskID = reader.GetDecimal("maskID").ToString(),
                    SecondaryID = reader.GetInt32("secondaryID").ToString()
                };
                if (reader["parent"] != DBNull.Value)
                    newHole.Parent = reader.GetString("parent").ToString();
                retList.Add(newHole);
                
            }
            _syncTime = DateTime.Now;
            return retList;

        }
        public async Task<IList<Signature>> GetSigs()
        {
            using var conn = GetConnection();
            //get all signatures which are wormholes in systems which have wormholes
            using var cmd = new MySqlCommand("select * from signatures where systemid in (select distinct systemid from signatures s where type='wormhole') and type='wormhole';", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var retList = new List<Signature>();
            while (reader.Read())
            {
                var newSig =
                    new Signature
                    {
                        Bookmark = null,
                        CreatedByID = reader.GetInt32("createdById").ToString(),
                        ID = reader.GetInt32("id").ToString(),
                        CreatedByName = reader.GetString("createdByName"),
                        LifeLeft = reader.GetDateTime("lifeLeft").ToString(),
                        LifeLength = reader.GetInt32("lifeLength").ToString(),
                        LifeTime = reader.GetDateTime("lifeTime").ToString(),
                        MaskID = reader.GetDecimal("maskID").ToString(),
                        ModifiedByID = reader.GetInt32("modifiedByID").ToString(),
                        ModifiedByName = reader.GetString("modifiedByName"),
                        ModifiedTime = reader.GetDateTime("modifiedTime").ToString(),
                        SystemID = reader.GetInt32("systemID").ToString(),
                        Type = reader.GetString("type")
                    };
                if (reader["name"] != DBNull.Value)
                    newSig.Name = reader.GetString("name");
                if (reader["signatureID"] != DBNull.Value)
                    newSig.SignatureID = reader.GetString("signatureID");
                retList.Add(newSig);
            }
            _syncTime = DateTime.Now;
            return retList;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(Config.GetConnectionString(ConnectionString));
        }
    }
}