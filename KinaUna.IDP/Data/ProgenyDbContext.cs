using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.IDP.Data
{
    public class ProgenyDbContext:DbContext
    {
        public ProgenyDbContext(DbContextOptions<ProgenyDbContext> options) : base(options)
        {
            
        }

        public DbSet<Progeny> ProgenyDb { get; set; }
        public DbSet<UserAccess> UserAccessDb { get; set; }
        public DbSet<UserInfo> UserInfoDb { get; set; }
    }
}
