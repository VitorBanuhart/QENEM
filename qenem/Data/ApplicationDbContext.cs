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
        public DbSet<Lista> Listas { get; set; }
        public DbSet<ListaQuestao> ListaQuestoes { get; set; }
        public DbSet<ListaQuestaoSimulado> ListaSimulados { get; set; }
        public DbSet<Simulado> Simulados { get; set; }
        public DbSet<RespostaUsuario> RespostasUsuario { get; set; }
        public DbSet<Pontos> Pontos { get; set; }

        public DbSet<AvaliaQuestao> AvaliarQuestoes { get; set; }

        public DbSet<qenem.Models.Anotacoes> Anotacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Alternative>().HasNoKey();

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

            modelBuilder.Entity<AvaliaQuestao>()
                .HasIndex(a => new { a.Usuario, a.QuestaoId })
                .IsUnique();

        }
    }
}
