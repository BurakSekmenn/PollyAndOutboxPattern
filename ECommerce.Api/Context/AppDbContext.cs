using ECommerce.Api.Entitiy;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Context
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<OutboxInvoice> OutboxInvoices => Set<OutboxInvoice>();
        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<OutboxInvoice>(e =>
            {
                e.HasKey(x => x.OrderId);
                e.Property(x => x.PayloadJson).HasColumnType("longtext");
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.CreatedUtc).HasColumnType("datetime(6)");
                e.Property(x => x.UpdatedUtc).HasColumnType("datetime(6)");
                e.Property(x => x.NextDueUtc).HasColumnType("datetime(6)");

                e.HasIndex(x => new { x.Status, x.NextDueUtc });   // “due” sorgusu için
            });
        }
    }
}
