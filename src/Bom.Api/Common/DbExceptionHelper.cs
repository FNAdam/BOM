using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bom.Api.Common;

/// <summary>
/// Utilitas untuk mengenali jenis pelanggaran constraint PostgreSQL dari DbUpdateException,
/// sehingga controller dapat membalas status HTTP yang tepat (409 Conflict).
/// </summary>
public static class DbExceptionHelper
{
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public static bool IsForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}
