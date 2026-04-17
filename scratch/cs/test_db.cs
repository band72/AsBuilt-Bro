using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RCS.Data;

class Program
{
    static void Main()
    {
        Console.WriteLine("Starting Db test...");
        try
        {
            using (var db = new AppDbContext())
            {
                DbInitializer.Initialize(db);
                var f = db.Figures.FirstOrDefault();
                Console.WriteLine(f != null ? "Found figure: " + f.Name : "No figures found.");
                
                // test query
                var sql = "SELECT IsVisible FROM Figures LIMIT 1";
                using (var conn = db.Database.GetDbConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var res = cmd.ExecuteScalar();
                        Console.WriteLine("IsVisible read directly: " + res);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRASH TRACE:");
            Console.WriteLine(ex.ToString());
        }
    }
}
