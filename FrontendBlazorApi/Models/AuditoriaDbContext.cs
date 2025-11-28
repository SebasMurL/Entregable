using Microsoft.EntityFrameworkCore;

namespace FrontendBlazorApi.Models
{
    public class AuditoriaDbContext : DbContext
    {
        public AuditoriaDbContext(DbContextOptions<AuditoriaDbContext> options) : base(options) { }

        public DbSet<Auditoria> Auditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auditoria>(entity =>
            {
                entity.ToTable("Auditoria");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.TablaAfectada)
                    .IsRequired()
                    .HasMaxLength(100);
                    
                entity.Property(e => e.Accion)
                    .IsRequired()
                    .HasMaxLength(10);
                    
                entity.Property(e => e.UsuarioId)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.FechaAuditoria)
                    .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}