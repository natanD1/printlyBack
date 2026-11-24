using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    /// <summary>ER_DUP_ENTRY do MySQL: violacao de indice unico.</summary>
    private const int MySqlDuplicateEntry = 1062;

    private readonly PrintlyDbContext _context;

    public UnitOfWork(PrintlyDbContext context)
    {
        _context = context;
    }

    /// <summary>Indice unico violado vira <see cref="UniqueConstraintException"/> (a Application nao conhece o MySQL).</summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new UniqueConstraintException("Registro duplicado.", ex);
        }
    }

    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is MySqlException { Number: MySqlDuplicateEntry };
}
