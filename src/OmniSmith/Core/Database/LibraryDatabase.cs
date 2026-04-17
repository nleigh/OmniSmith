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

        public LibraryDatabase()
        {
            string dbPath = Path.Combine(KnownFolders.RoamingAppData.Path, "OmniSmith", "Library.db");
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                DefaultTimeout = 5
            }.ToString();

            InitializeDatabase();
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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO songs 
                (filename, mtime, size, title, artist, album, year, duration, tuning, arrangements, has_lyrics)
                VALUES ($filename, $mtime, $size, $title, $artist, $album, $year, $duration, $tuning, $arrangements, $hasLyrics);";

            command.Parameters.AddWithValue("$filename", filename);
            command.Parameters.AddWithValue("$mtime", mtime);
            command.Parameters.AddWithValue("$size", size);
            command.Parameters.AddWithValue("$title", meta.Title);
            command.Parameters.AddWithValue("$artist", meta.Artist);
            command.Parameters.AddWithValue("$album", meta.Album);
            command.Parameters.AddWithValue("$year", meta.Year);
            command.Parameters.AddWithValue("$duration", meta.Duration);
            command.Parameters.AddWithValue("$tuning", meta.Tuning);
            command.Parameters.AddWithValue("$arrangements", meta.Arrangements);
            command.Parameters.AddWithValue("$hasLyrics", meta.HasLyrics ? 1 : 0);

            command.ExecuteNonQuery();
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

        public List<SongEntry> QueryPage(string query, int page, int size, string sortColumn, string direction)
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
            command.CommandText = $@"
                SELECT * FROM songs 
                WHERE filename LIKE $query OR title LIKE $query OR artist LIKE $query OR album LIKE $query
                ORDER BY {sortColumn} {dir}
                LIMIT $limit OFFSET $offset;";

            command.Parameters.AddWithValue("$query", $"%{query}%");
            command.Parameters.AddWithValue("$limit", size);
            command.Parameters.AddWithValue("$offset", offset);

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
