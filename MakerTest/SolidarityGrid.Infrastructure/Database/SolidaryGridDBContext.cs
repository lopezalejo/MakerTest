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
                entity.HasKey(x => x.TransactionId);
                entity.Property(x => x.TransactionId).HasMaxLength(64);
                entity.Property(x => x.PaymentAmount).HasColumnType("decimal(18,2)");
                entity.Property(x => x.Status).HasColumnType("tinyint");
                entity.Property(x => x.OwnerNodeId).HasMaxLength(32);
                entity.Property(x => x.CompletedByNodeId).HasMaxLength(32);
                entity.Property(x => x.ResultMessage).HasMaxLength(500);
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasIndex(x => new { x.OwnerNodeId, x.Status }).HasDatabaseName("IX_Payments_Owner_Status");
                entity.HasIndex(x => new { x.Status, x.LeaseUntil }).HasDatabaseName("IX_Payments_Status_Lease");
            });
        }
    }
}
