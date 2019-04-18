using Kirides.Libs.Data;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kirides.Libs.Tests.Data
{
    public class SimpleDbMapperTest
    {
        [Fact]
        public async Task TestSingleMapping()
        {
            SQLiteConnection sqlite = new SQLiteConnection("Data Source=:memory:");
            sqlite.Open();
            FillDatabase(sqlite);
            SimpleDbMapper mapper = new SimpleDbMapper();
            long memUsageBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            var result = await mapper.GetAsync<User>(sqlite, "SELECT Name FROM Users WHERE Id = 1", null, CancellationToken.None);

            sw.Stop();
            var memoryUsed = ((double)GC.GetTotalMemory(false) - memUsageBefore);
            var meanMemoryUsed = ((double)GC.GetTotalMemory(true) - memUsageBefore);
        }

        [Fact]
        public async Task TestLazyMapping()
        {
            SQLiteConnection sqlite = new SQLiteConnection("Data Source=:memory:");
            sqlite.Open();
            FillDatabase(sqlite);
            SimpleDbMapper mapper = new SimpleDbMapper();
            long memUsageBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1; i++)
            {
                var result = await mapper.GetAsync<long>(sqlite, "SELECT Count(*) FROM Users", null, CancellationToken.None);
            }

            sw.Stop();
            var memoryUsed = ((double)GC.GetTotalMemory(false) - memUsageBefore);
            var meanMemoryUsed = ((double)GC.GetTotalMemory(true) - memUsageBefore);
        }

        private void FillDatabase(SQLiteConnection sqlite)
        {
            var cmd = sqlite.CreateCommand();
            cmd.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Firstname TEXT NULL )";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO Users (Name) VALUES ('Hans')";
            cmd.ExecuteNonQuery();
        }
    }

    internal class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Firstname { get; set; }
    }
}
