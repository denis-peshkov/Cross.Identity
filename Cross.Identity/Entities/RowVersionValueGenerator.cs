namespace Cross.Identity.Entities;

/// <summary>
/// Generates a RowVersion value only when using the InMemory provider.
/// For SQL Server returns null — the DB generates the value (rowversion/timestamp).
/// </summary>
internal class RowVersionValueGenerator : ValueGenerator<byte[]?>
{
    public override bool GeneratesTemporaryValues => false;

    public override byte[]? Next(EntityEntry entry)
    {
        if (entry.Context.Database.IsInMemory())
            return new byte[8];

        return null;
    }
}
