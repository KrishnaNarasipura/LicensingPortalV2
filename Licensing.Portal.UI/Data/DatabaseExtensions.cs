using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Licensing.Portal.Data;

namespace Licensing.Portal.UI.Data
{
    public static class DatabaseExtensions
    {
        public static void InitializeDatabase(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
                
                // Apply pending migrations automatically
                dbContext.Database.Migrate();
            }
        }
    }
}
