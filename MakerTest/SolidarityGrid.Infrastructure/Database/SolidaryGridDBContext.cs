using Microsoft.EntityFrameworkCore;
using SolidarityGrid.Infrastructure.Entity;

namespace SolidarityGrid.Infrastructure.Database
{
    public class SolidaryGridDBContext : DbContext
    {
        public SolidaryGridDBContext(DbContextOptions<SolidaryGridDBContext> options) : base(options) { }

        public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentEntity>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.TransactionId).HasMaxLength(64);
                entity.Property(e => e.PaymentAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasColumnType("tinyint");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
