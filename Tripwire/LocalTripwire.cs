using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Tripwire;

namespace Tripwire 
{
    public class LocalTripwire : ITripwireDataProvider
    {
        private const string ConnectionString = "Default";
        public DateTime SyncTime {get => _syncTime;}
        public IList<string> SystemIds{get => new List<string>{"31000732"};}
        private DateTime _syncTime;
        IConfiguration Config { get; }

        public LocalTripwire(IConfiguration configuration)
        {
            Config = configuration;
        }


        public async Task<IList<Wormhole>> GetHoles()
        {
            using var conn = GetConnection();
            var cmd = new MySqlCommand("SELECT * FROM WORMHOLES", conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var retList = new List<Wormhole>();
            while (reader.Read())
            {
                retList.Add(
                    new Wormhole
                    {
                        InitialID = reader.GetInt32("initialID").ToString(),
                        MaskID = reader.GetDecimal("maskID").ToString(),
                        Parent = reader.GetString("parent").ToString(),
                        SecondaryID = reader.GetInt32("secondaryID").ToString()
                    }
                );
            }
            _syncTime=DateTime.Now;
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
                retList.Add(
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
                        Name = reader.GetString("name"),
                        SignatureID = reader.GetString("signatureID"),
                        SystemID = reader.GetString("systemID"),
                        Type = reader.GetString("type")
                    });
            }
            _syncTime=DateTime.Now;
            return retList;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(Config.GetConnectionString(ConnectionString));
        }
    }
}