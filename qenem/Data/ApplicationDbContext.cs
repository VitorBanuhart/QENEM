using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using qenem.Models;

namespace qenem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<AreaInteresse> AreasInteresse { get; set; }
        public DbSet<UsuarioArea> UsuarioAreas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsuarioArea>()
                .HasKey(ua => ua.Id);

            modelBuilder.Entity<UsuarioArea>()
                .HasOne(ua => ua.Usuario)
                .WithMany(u => u.UsuarioAreas) // Correto agora
                .HasForeignKey(ua => ua.Id_Usuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioArea>()
                .HasOne(ua => ua.AreaInteresse)
                .WithMany(ai => ai.UsuarioAreas) // Correto agora
                .HasForeignKey(ua => ua.Id_AreaInteresse)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AreaInteresse>()
                .Property(ai => ai.NomeAreaInteresse)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
}
