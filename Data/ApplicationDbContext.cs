using InvoiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Contractor> Contractors { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<CallOff> CallOffs { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<CallOffItems> CallOffItems { get; set; }
        public DbSet<InvoiceCallOffs> InvoiceCallOffs { get; set; }
        public DbSet<InvoiceCallOffItems> InvoiceCallOffItems { get; set; }
        //public DbSet<InvoiceItems> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            //Contractors Table
            builder.Entity<Contractor>()
                .HasIndex(c => c.Number)
                .IsUnique();

            builder.Entity<Contractor>()
                .HasIndex(c => c.Name)
                .IsUnique();

            //Invoices Table
            builder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            //Units Table
            builder.Entity<Unit>()
                .HasIndex(u => u.Name)
                .IsUnique();
        }
    }
}
