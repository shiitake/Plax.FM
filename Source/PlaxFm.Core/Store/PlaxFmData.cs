using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaxFm.Core.Models;

namespace PlaxFm.Core.Store
{
    class PlaxFmData
    {
        public bool DoesDbExist { get; set; }
        public SQLiteConnection DbConnection { get; set; }
        private readonly SQLiteConnection _dbConnection;
        private readonly string _dbName = "PlaxFmDb.sqlite";
        private readonly string _dbFile;
        //private readonly string _connectionString = "Data Source = PlaxFmDb.sqlite; Version=3;";

        public PlaxFmData(string configLocation)
        {
            //get db file
            _dbFile = Path.Combine(configLocation, _dbName);

            //get db connection
            _dbConnection = CreateConnection(_dbFile);
            DbConnection = _dbConnection;

            //does db exist
            var dbInfo = new FileInfo(_dbFile);
            if (dbInfo.Exists) return;
            CreateNewDb();
        }

        public void CreateNewDb()
        {
            CreateDbFile();
            _dbConnection.Open();
            CreateUserTable(_dbConnection);
            CreateSetupTable(_dbConnection);
            CreateSongCacheTable(_dbConnection);
            _dbConnection.Close();
        }

        public void CreateDbFile()
        {
            SQLiteConnection.CreateFile(_dbFile);
        }

        private SQLiteConnection CreateConnection(string dataSource)
        {
            return new SQLiteConnection($"Data Source={dataSource}");
        }

        public void CreateUserTable(SQLiteConnection conn)
        {
            try
            {
                var sql =
                "create table User (PlexId integer primary key autoincrement not null, PlexUsername varchar(50), LastFmUsername varchar(50), SessionId varchar(50), Token varchar(50), IsAuthorized bit, PlexToken varchar(50))";
                using (var command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error creating the table. {ex.Message}");
            }
        }

        public void CreateSetupTable(SQLiteConnection conn)
        {
            try
            {
                var sql =
                "create table Setup (Initialized bool not null, Profile varchar(50) primary key)";
                using (var command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error creating the table. {ex.Message}");
            }
        }

        public void CreateSongCacheTable(SQLiteConnection conn)
        {
            try
            {
                Console.WriteLine("Creating SongCache table");
                var sql =
                    "create table Songs (CacheId integer primary key autoincrement not null, UserId int not null, MediaId int not null, Title varchar(50), Artist varchar(50), Album varchar(50), TimePlayed DateTime, HasBeenScrobbled bool)";
                using (var command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error creating the table. {ex.Message}");
            }
        }

        public int GetUserCount()
        {
            //todo: add this functionality. 
            return 0;
        }

        public int InsertNewUser(SQLiteConnection conn, User user)
        {
            try
            {
                var auth = user.IsAuthorized ? 1 : 0;
                var sql =
                    $"insert into User (PlexId, PlexUsername, LastFmUsername, SessionId, Token, IsAuthorized, PlexToken) values ({user.PlexId}, '{user.PlexUsername}', '{user.LastFmUsername}', '{user.SessionId}', '{user.Token}', {auth}, '{user.PlexToken}')";
                using (var command = new SQLiteCommand(sql, conn))
                {
                    return command.ExecuteNonQuery();
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error inserting data into the table. {ex.Message}");
            }
            return 0;
        }

        public User GetUser(SQLiteConnection conn, int PlexId)
        {
            try
            {
                var sql = $"select * from User where PlexId = {PlexId}";
                using (var command = new SQLiteCommand(sql, conn))
                {
                    var user = command.ExecuteReader();
                    if (user.HasRows)
                    {
                        var dt = new DataTable();
                        dt.Load(user);
                        var row = dt.Rows[0];
                        return new User
                        {
                            PlexId = Int32.Parse(row["PlexId"].ToString()),
                            PlexUsername = row["PlexUsername"].ToString()
                        };
                    }
                    else
                    {
                        return null;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error retrieving data for user {PlexId} from the table. {ex.Message}");
                return null;
            }

        }

        public void SaveDataSet(DataSet storage)
        {
            
        }
    }
}
