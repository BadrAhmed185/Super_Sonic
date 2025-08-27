using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Super_Sonic.Models;


namespace Super_Sonic
{
  
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<PartnerProduct> PartnerProducts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SubTransaction> SubTransactions { get; set; }
        public DbSet<InterestRate> InterestRates { get; set; }

        public DbSet<PartnerLogForInvest_Drawal> PartnerLogForInvest_Drawals { get; set; }  

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(15,5)");
            }

            // Client -> Products
            modelBuilder.Entity<Client>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Client)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Partner -> PartnerProducts
            modelBuilder.Entity<Partner>()
                .HasMany(p => p.PartnerProducts)
                .WithOne(pp => pp.Partner)
                .HasForeignKey(pp => pp.PartnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product -> PartnerProducts
            modelBuilder.Entity<Product>()
                .HasMany(p => p.PartnerProducts)
                .WithOne(pp => pp.Product)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product -> Transactions
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Transactions)
                .WithOne(t => t.Product)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Transaction -> SubTransactions
            modelBuilder.Entity<Transaction>()
                .HasMany(t => t.SubTransactions)
                .WithOne(st => st.Transaction)
                .HasForeignKey(st => st.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Partner -> SubTransactions
            modelBuilder.Entity<Partner>()
                .HasMany(p => p.SubTransactions)
                .WithOne(st => st.Partner)
                .HasForeignKey(st => st.PartnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
