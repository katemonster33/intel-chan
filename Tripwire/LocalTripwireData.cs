using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        ILogger<LocalTripwireData> Logger { get; }

        public LocalTripwireData(IConfiguration configuration, ILogger<LocalTripwireData> logger)
        {
            Config = configuration;
            Logger=logger;
        }

        public async Task<bool> Start(CancellationToken token)
        {
            
            bool connected = false;
            while (!connected)
            {
                if(token.IsCancellationRequested)
                    return false;
                
                using var conn = GetConnection();
                try
                {
                    await conn.OpenAsync(token);
                    connected = true;
                    Logger.LogInformation("Tripwire database is up");
                }
                catch(OperationCanceledException)
                {
                    Logger.LogInformation("Operation cancelled");
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.LogError("Could not connect to database: " + ex.ToString());
                }
            }
            return connected;
        }

        public Task RefreshData(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task<IList<Occupant>> GetOccupants(string systemId)
        {
            return Task.FromResult<IList<Occupant>>(new List<Occupant>());
        }
        
        public Task<IList<OccupiedSystem>> GetOccupiedSystems()
        {
            return Task.FromResult<IList<OccupiedSystem>>(new List<OccupiedSystem>());
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
            using var cmd = new MySqlCommand(@"select s.*,m.solarSystemName from signatures s
            INNER JOIN eve_dumper.mapSolarSystems m
            ON m.solarSystemID=s.systemid
            where systemid in (select distinct systemid from signatures s where type='wormhole') 
                and type='wormhole'
                and m.security < 0.5;", conn);
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
                        Type = reader.GetString("type"),
                        SystemName = reader.GetString("solarSystemName")
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
            var connStr = Config.GetConnectionString(ConnectionString);
            if(string.IsNullOrEmpty(connStr))
                throw new ApplicationException("No connection string found");
            return new MySqlConnection(Config.GetConnectionString(ConnectionString));
        }
    }
}