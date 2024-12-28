using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoogleLogin.Models
{
    public class AppIdentityDbContext : IdentityDbContext<AppUser>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

        public DbSet<TbEmail> TbEmails { get; set; }
        public DbSet<TbOrder> TbOrders{ get; set; }
        public DbSet<TbShopifyLog> TbLogs{ get; set; }
        public DbSet<TbShopifyToken> TbTokens{ get; set; }
        public DbSet<TbShopifyUser> TbShopifyUsers{ get; set; }        
        public DbSet<TbSms> TbSmss { get; set; }
    }
}
