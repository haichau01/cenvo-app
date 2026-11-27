using Cenvo.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Infrastructure.Persistence;

/// <summary>
/// Atomic upsert in PostgreSQL: two POS terminals submitting at the same moment still get different numbers.
/// Runs immediately (outside the SaveChanges transaction), so a failed save may skip a number — acceptable.
/// </summary>
public class DocumentNumberGenerator(AppDbContext db) : IDocumentNumberGenerator
{
    public async Task<string> Next(Guid branchId, string prefix, DateTime atUtc, CancellationToken ct)
    {
        var key = $"{prefix}{VnTime.ToLocal(atUtc):yyMMdd}";
        var numbers = await db.Database.SqlQuery<int>($"""
            insert into document_sequence (branch_id, key, last_number) values ({branchId}, {key}, 1)
            on conflict (branch_id, key) do update set last_number = document_sequence.last_number + 1
            returning last_number as "Value"
            """).ToListAsync(ct);
        return $"{key}-{numbers[0]:0000}";
    }
}
