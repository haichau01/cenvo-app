namespace Cenvo.SharedKernel;

/// <summary>Generates per-branch document numbers: <c>{prefix}{yyMMdd}-{####}</c> (date in Vietnam time).</summary>
public interface IDocumentNumberGenerator
{
    Task<string> Next(Guid branchId, string prefix, DateTime atUtc, CancellationToken ct);
}
