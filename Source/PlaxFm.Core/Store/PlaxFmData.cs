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
        private SQLiteConnection _dbConnection;
        private readonly string PlaxDatebase = "PlaxFmDb.sqlite";

        public PlaxFmData(string configLocation)
        {
            //check for db
            var dbFile = configLocation + @"\" + PlaxDatebase;
            var dbInfo = new FileInfo(dbFile);
            if (!dbInfo.Exists)
            {
                CreateNewDb();

            }
            SQLiteConnection.CreateFile(dbFile);
            _dbConnection = new SQLiteConnection("Data Source=PlaxFmDb.sqlite;Version=3;");
            _dbConnection.Open();

        }

        public void CreateNewDb()
        {
            CreateDbFile();
            _dbConnection = CreateConnection(PlaxDatebase);
            _dbConnection.Open();
            CreateUserTable(_dbConnection);
        }

        public void CreateDbFile()
        {
            SQLiteConnection.CreateFile(PlaxDatebase);
        }

        public SQLiteConnection CreateConnection(string dataSource)
        {
            return new SQLiteConnection($"Data Source={dataSource};Version=3;");
        }

        public void CreateUserTable(SQLiteConnection conn)
        {
            try
            {
                var sql =
                "create table User (PlexId int unique not null, PlexUsername varchar(50), LastFmUsername varchar(50), SessionId varchar(50), Token varchar(50), IsAuthorized bit, PlexToken varchar(50))";
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
                Console.WriteLine($"There was an error retrieviing data for user {PlexId} from the table. {ex.Message}");
                return null;
            }

        }
    }
}
