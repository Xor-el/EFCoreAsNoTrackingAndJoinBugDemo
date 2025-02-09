// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;

using (var context = new TransactionDbContext())
{
    var reference = "TRX12345";

    // Delete the database if it exists
    context.Database.EnsureDeleted();
    Console.WriteLine("Database deleted.");

    // Apply migrations and create the database
    context.Database.Migrate();
    Console.WriteLine("Database created with migrations.");


    // Create and save a new TransactionReversalEntity
    var transactionReversalEntity = new TransactionReversalEntity
    {
        Reference = reference,
        Name = "Old Name"
    };
    context.TransactionReversals.Add(transactionReversalEntity);

    // Create and save a new PaymentEntity
    var paymentEntity = new PaymentEntity
    {
        BatchId = reference
    };
    context.Payments.Add(paymentEntity);

    // Save changes to the database
    context.SaveChanges();
    Console.WriteLine("Entities have been successfully added to the database.");

    var paymentQuery = context.Payments.AsNoTracking().AsQueryable(); // Disable Tracking for the Payment Query
    var transactionReversalQuery = context.TransactionReversals.AsQueryable(); // Tracking is still enabled for the Reversal Query

    // Fetch The Reversal and Payment Entity via a Join
    var reversalPairs = await (from reversal in transactionReversalQuery
                               join payment in paymentQuery
                               on reversal.Reference equals payment.BatchId
                               select new
                               {
                                   TransactionReversal = reversal,
                                   Payment = payment
                               }).ToListAsync();

    // Get The Reversal Entity
    var reversalEntity = reversalPairs.First(x => x.TransactionReversal.Reference == reference).TransactionReversal;

    var updatedName = "Updated Name";

    // Update The Name Of The Reversal Entity
    reversalEntity.Name = updatedName;

    // Save changes to the database
    context.SaveChanges();

    Console.WriteLine("Entities have been successfully updated in the database.");

    // Fetch The Reversal Entity
    reversalEntity = await context.TransactionReversals.FirstAsync(x => x.Reference == reference);

    var reversalEntityName = reversalEntity.Name;

    if (string.Equals(reversalEntityName, updatedName, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Entities have been successfully updated in the database.");
    }
    else
    {
        Console.WriteLine($"Entities have not been updated in the database., Name in the DB is {reversalEntityName}");
    }
}


public class TransactionReversalEntity
{
    public int Id { get; set; }
    public string? Reference { get; set; }
    public string? Name { get; set; }
}

public class PaymentEntity
{
    public int Id { get; set; }
    public string? BatchId { get; set; }
}

public class TransactionDbContext : DbContext
{
    public DbSet<TransactionReversalEntity> TransactionReversals { get; set; }
    public DbSet<PaymentEntity> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Update the db connection string with yours
        optionsBuilder.UseSqlServer("Server=localhost;Database=TransactionDemoDb;Trusted_Connection=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionReversalEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(x => x.Reference).IsUnique();
            entity.Property(e => e.Reference).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchId).HasMaxLength(50).IsRequired();
        });
    }
}
