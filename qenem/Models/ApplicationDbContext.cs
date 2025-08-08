using Microsoft.EntityFrameworkCore;

namespace qenem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<UsuarioModel> Usuario { get; set; }
    }
}
