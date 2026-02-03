using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Alfanar.MarketIntel.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketIntelDbContext>
{
    public MarketIntelDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarketIntelDbContext>();

        // Use the same connection string as the API (LocalDB)
        var connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=MarketIntel;Integrated Security=True;TrustServerCertificate=True";
        
        optionsBuilder.UseSqlServer(connectionString);

        return new MarketIntelDbContext(optionsBuilder.Options);
    }
}
