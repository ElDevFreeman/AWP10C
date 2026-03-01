using Microsoft.EntityFrameworkCore;
using TiendaWeb.Models;

namespace TiendaWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Especificar explícitamente el nombre de la tabla
            modelBuilder.Entity<Category>().ToTable("Category");
        }
    }
}