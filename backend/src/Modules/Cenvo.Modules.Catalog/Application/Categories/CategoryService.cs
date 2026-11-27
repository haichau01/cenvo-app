namespace Cenvo.Modules.Catalog.Application.Categories;

public record CategoryInput(string Name, ProductKind Kind, int SortOrder);
public record CategoryDto(Guid Id, string Name, ProductKind Kind, int SortOrder);

public class CategoryService(AppDbContext db)
{
    public Task<List<CategoryDto>> List(ProductKind? kind, CancellationToken ct) =>
        db.Set<Category>().AsNoTracking()
            .Where(c => kind == null || c.Kind == kind)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Kind, c.SortOrder))
            .ToListAsync(ct);

    public async Task<Guid> Save(Guid? id, CategoryInput input, CancellationToken ct)
    {
        var category = id == null
            ? db.Add(new Category()).Entity
            : await db.Set<Category>().FindAsync([id], ct) ?? throw new NotFoundException("danh mục", id);
        category.Update(input.Name, input.Kind, input.SortOrder);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        db.Remove(await db.Set<Category>().FindAsync([id], ct) ?? throw new NotFoundException("danh mục", id));
        await db.SaveChangesAsync(ct);
    }
}
