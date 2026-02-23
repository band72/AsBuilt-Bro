using Microsoft.EntityFrameworkCore;

namespace RCS.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Ensure Database Exists (Creates if not present)
        context.Database.EnsureCreated();
        
        // Manual Schema Updates (Running scripts if needed for new tables on existing DB)
        try
        {
            // Cogo Codes
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""CogoCodes"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CogoCodes"" PRIMARY KEY AUTOINCREMENT,
                    ""LocalCode"" TEXT NOT NULL,
                    ""SystemCode"" TEXT NULL,
                    ""Description"" TEXT NULL
                );
            ");

            // Materials
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Materials"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Materials"" PRIMARY KEY AUTOINCREMENT,
                    ""PartKey"" TEXT NULL,
                    ""Discipline"" TEXT NULL,
                    ""FeatureType"" TEXT NULL,
                    ""Size"" TEXT NULL,
                    ""Material"" TEXT NULL,
                    ""Manufacturer"" TEXT NULL,
                    ""Model"" TEXT NULL,
                    ""Year"" TEXT NULL,
                    ""Notes"" TEXT NULL
                );
            ");

            // Symbols
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""SymbolManager"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SymbolManager"" PRIMARY KEY AUTOINCREMENT,
                    ""ClientCode"" TEXT NULL,
                    ""SystemCode"" TEXT NULL,
                    ""Symbol"" TEXT NULL,
                    ""Type"" TEXT NULL,
                    ""Discipline"" TEXT NULL
                );
            ");
        }
        catch (Exception ex)
        {
             // Log or rethrow? For now, we assume this is safe.
             System.Diagnostics.Debug.WriteLine($"Error verifying schema: {ex.Message}");
        }
    }
}
