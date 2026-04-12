# Ticket 5.1: SQLite Database Setup
**Goal**: Integrate a high-performance SQLite database for library indexing to replace JSON state files.

### Context
Slopsmith uses a high-performance SQLite DB for rapidly browsing thousands of CDLCs. Openthesia currently serializes the entire library to a generic `GameState.json`. We are moving to SQLite to ensure UI responsiveness.

### Implementation Steps
1. **Dependency Installation**: Ensure the `Microsoft.Data.Sqlite` NuGet package is included in `Openthesia.csproj`.
2. **Database Management**: 
   - Create `[NEW] Core/Database/LibraryDatabase.cs`.
   - On initialization, it should create or connect to `web_library.db` in `KnownFolders.RoamingAppData.Path`.
3. **Table Schema**: Create the tables equivalent to Slopsmith's backend:
   ```sqlite
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
   )
   ```
4. **Data Operations**: Write C# helper methods inside `LibraryDatabase.cs` for:
   - `Put(filename, mtime, size, meta)` to upsert a song.
   - `QueryPage(q, page, size, sort, direction)` to return a paginated list of songs matching the schema.

### Definition of Done
The application starts up and instantiates the SQLite database correctly on disk. The DB class is accessible globally via the Core layer.
