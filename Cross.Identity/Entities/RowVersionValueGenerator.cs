namespace Cross.Identity.Entities;

/// <summary>
/// Генерирует значение для RowVersion только при использовании InMemory-провайдера.
/// Для SQL Server возвращает null — значение генерирует БД (rowversion/timestamp).
/// </summary>
public class RowVersionValueGenerator : ValueGenerator<byte[]?>
{
    public override bool GeneratesTemporaryValues => false;

    public override byte[]? Next(EntityEntry entry)
    {
        if (entry.Context.Database.IsInMemory())
            return new byte[8];

        return null;
    }
}
