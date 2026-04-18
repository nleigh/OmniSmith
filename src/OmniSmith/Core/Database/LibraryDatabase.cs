using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using Syroot.Windows.IO;

namespace OmniSmith.Core.Database
{
    public record SongMeta(
        string Title,
        string Artist,
        string Album,
        string Year,
        double Duration,
        string Tuning,
        string Arrangements,
        bool HasLyrics
    );

    public record SongEntry(
        string Filename,
        double Mtime,
        long Size,
        string Title,
        string Artist,
        string Album,
        string Year,
        double Duration,
        string Tuning,
        string Arrangements,
        bool HasLyrics
    );

    public class LibraryDatabase
    {
        public static LibraryDatabase Instance { get; set; } = null!;
        private readonly string _connectionString;
        private readonly SqliteConnection _sharedConnection;
        private SqliteCommand _putCommand = null!;

        public LibraryDatabase()
        {
            string dbPath = Path.Combine(KnownFolders.RoamingAppData.Path, "OmniSmith", "Library.db");
            if (!Directory.Exists(Path.GetDirectoryName(dbPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                DefaultTimeout = 10
            }.ToString();

            InitializeDatabase();

            // Open a long-lived connection for efficiency
            _sharedConnection = new SqliteConnection(_connectionString);
            _sharedConnection.Open();
            PrepareCommands();
        }

        private void PrepareCommands()
        {
            _putCommand = _sharedConnection.CreateCommand();
            _putCommand.CommandText = @"
                INSERT OR REPLACE INTO songs 
                (filename, mtime, size, title, artist, album, year, duration, tuning, arrangements, has_lyrics)
                VALUES ($filename, $mtime, $size, $title, $artist, $album, $year, $duration, $tuning, $arrangements, $hasLyrics);";

            _putCommand.Parameters.Add("$filename", SqliteType.Text);
            _putCommand.Parameters.Add("$mtime", SqliteType.Real);
            _putCommand.Parameters.Add("$size", SqliteType.Integer);
            _putCommand.Parameters.Add("$title", SqliteType.Text);
            _putCommand.Parameters.Add("$artist", SqliteType.Text);
            _putCommand.Parameters.Add("$album", SqliteType.Text);
            _putCommand.Parameters.Add("$year", SqliteType.Text);
            _putCommand.Parameters.Add("$duration", SqliteType.Real);
            _putCommand.Parameters.Add("$tuning", SqliteType.Text);
            _putCommand.Parameters.Add("$arrangements", SqliteType.Text);
            _putCommand.Parameters.Add("$hasLyrics", SqliteType.Integer);
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
            pragma.ExecuteNonQuery();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS songs (
                    filename TEXT PRIMARY KEY,
                    mtime REAL,
                    size INTEGER,
                    title TEXT,
                    artist TEXT,
                    album TEXT,
                    year TEXT,
                    duration REAL,
                    tuning TEXT,
                    arrangements TEXT,
                    has_lyrics INTEGER DEFAULT 0
                );";
            command.ExecuteNonQuery();
        }

        public void Put(string filename, double mtime, long size, SongMeta meta)
        {
            lock (_putCommand)
            {
                _putCommand.Parameters["$filename"].Value = filename;
                _putCommand.Parameters["$mtime"].Value = mtime;
                _putCommand.Parameters["$size"].Value = size;
                _putCommand.Parameters["$title"].Value = meta.Title ?? (object)DBNull.Value;
                _putCommand.Parameters["$artist"].Value = meta.Artist ?? (object)DBNull.Value;
                _putCommand.Parameters["$album"].Value = meta.Album ?? (object)DBNull.Value;
                _putCommand.Parameters["$year"].Value = meta.Year ?? (object)DBNull.Value;
                _putCommand.Parameters["$duration"].Value = meta.Duration;
                _putCommand.Parameters["$tuning"].Value = meta.Tuning ?? (object)DBNull.Value;
                _putCommand.Parameters["$arrangements"].Value = meta.Arrangements ?? (object)DBNull.Value;
                _putCommand.Parameters["$hasLyrics"].Value = meta.HasLyrics ? 1 : 0;

                _putCommand.ExecuteNonQuery();
            }
        }

        public (double Mtime, long Size)? GetMtimeSize(string filename)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT mtime, size FROM songs WHERE filename = $filename";
            command.Parameters.AddWithValue("$filename", filename);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetDouble(0), reader.GetInt64(1));
            }
            return null;
        }

        public List<SongEntry> QueryPage(string query, int page, int size, string sortColumn, string direction, string extensionFilter = "")
        {
            var results = new List<SongEntry>();
            int offset = page * size;

            // Validate sortColumn to prevent SQL injection
            var allowedColumns = new HashSet<string> { "filename", "title", "artist", "album", "year", "duration" };
            if (!allowedColumns.Contains(sortColumn.ToLower()))
            {
                sortColumn = "title";
            }

            string dir = direction.ToUpper() == "DESC" ? "DESC" : "ASC";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            string limitClause = size >= 0 ? "LIMIT $limit OFFSET $offset" : "";
            
            string filterSql = "(filename LIKE $query OR title LIKE $query OR artist LIKE $query OR album LIKE $query)";
            if (!string.IsNullOrEmpty(extensionFilter))
            {
                filterSql += " AND filename LIKE $ext";
                command.Parameters.AddWithValue("$ext", $"%{extensionFilter}");
            }

            command.CommandText = $@"
                SELECT * FROM songs 
                WHERE {filterSql}
                ORDER BY {sortColumn} {dir}
                {limitClause};";

            command.Parameters.AddWithValue("$query", $"%{query}%");
            if (size >= 0)
            {
                command.Parameters.AddWithValue("$limit", size);
                command.Parameters.AddWithValue("$offset", offset);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new SongEntry(
                    reader.GetString(0),
                    reader.GetDouble(1),
                    reader.GetInt64(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetDouble(7),
                    reader.GetString(8),
                    reader.GetString(9),
                    reader.GetInt32(10) == 1
                ));
            }

            return results;
        }
    }
}
