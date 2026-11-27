using System.Linq.Expressions;
using Cenvo.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IEnumerable<IModuleModel> modules,
    ICurrentUser currentUser,
    IDomainEventDispatcher dispatcher) : DbContext(options)
{
    private const int MaxEventRounds = 10;

    // EF parameterizes context properties used in global filters, so they are re-evaluated per request.
    public Guid CurrentBranchId => currentUser.BranchId ?? Guid.Empty;
    public bool AllBranches => currentUser.IsAllBranches;

    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        cb.Properties<decimal>().HavePrecision(18, 2);
        cb.Properties<string>().HaveMaxLength(500);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DocumentSequence>().HasKey(x => new { x.BranchId, x.Key });
        foreach (var module in modules) module.Build(mb);

        foreach (var entityType in mb.Model.GetEntityTypes().Where(t => t.BaseType == null).ToList())
        {
            var clr = entityType.ClrType;
            var entity = mb.Entity(clr);
            if (typeof(BaseEntity).IsAssignableFrom(clr)) entity.Ignore(nameof(BaseEntity.DomainEvents));

            // Client-generated Guid keys: new entities added to a collection are tracked as Added (not Modified).
            if (entityType.FindPrimaryKey()?.Properties is [{ Name: "Id", ClrType: var keyType }] && keyType == typeof(Guid))
                entity.Property("Id").ValueGeneratedNever();

            var filter = BuildFilter(clr);
            if (filter != null) entity.HasQueryFilter(filter);
        }
    }

    /// <summary>Filters by branch (IBranchScoped) and hides soft-deleted rows (ISoftDelete).</summary>
    private LambdaExpression? BuildFilter(Type clr)
    {
        var e = Expression.Parameter(clr, "e");
        Expression? body = null;
        if (typeof(IBranchScoped).IsAssignableFrom(clr))
        {
            var context = Expression.Constant(this);
            body = Expression.OrElse(
                Expression.Property(context, nameof(AllBranches)),
                Expression.Equal(Expression.Property(e, nameof(IBranchScoped.BranchId)),
                                 Expression.Property(context, nameof(CurrentBranchId))));
        }
        if (typeof(ISoftDelete).IsAssignableFrom(clr))
        {
            var notDeleted = Expression.Not(Expression.Property(e, nameof(ISoftDelete.IsDeleted)));
            body = body == null ? notDeleted : Expression.AndAlso(body, notDeleted);
        }
        return body == null ? null : Expression.Lambda(body, e);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        await DispatchDomainEvents(ct);
        ApplyAudit();
        return await base.SaveChangesAsync(ct);
    }

    public override int SaveChanges() => throw new NotSupportedException("Dùng SaveChangesAsync.");

    /// <summary>Handlers run before saving, so whatever they write lands in the same transaction (atomic).</summary>
    private async Task DispatchDomainEvents(CancellationToken ct)
    {
        for (var round = 0; round < MaxEventRounds; round++)
        {
            var sources = ChangeTracker.Entries<BaseEntity>().Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0).ToList();
            if (sources.Count == 0) return;

            var events = sources.SelectMany(e => e.DomainEvents).ToList();
            sources.ForEach(e => e.ClearEvents());
            await dispatcher.Dispatch(events, ct);
        }
        throw new InvalidOperationException("Domain event phát vòng lặp quá nhiều lần.");
    }

    private void ApplyAudit()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= currentUser.UserId;
                    if (entry.Entity is IBranchScoped { BranchId: var b } scoped && b == Guid.Empty)
                        scoped.BranchId = currentUser.BranchId ?? throw new BusinessException("Chưa chọn chi nhánh làm việc.");
                    break;
                case EntityState.Modified:
                    MarkUpdated(entry.Entity, now);
                    break;
                case EntityState.Deleted when entry.Entity is ISoftDelete softDelete:
                    entry.State = EntityState.Modified;
                    softDelete.IsDeleted = true;
                    MarkUpdated(entry.Entity, now);
                    break;
            }
        }
    }

    private void MarkUpdated(BaseEntity entity, DateTime now)
    {
        entity.UpdatedAt = now;
        entity.UpdatedBy = currentUser.UserId;
    }
}
