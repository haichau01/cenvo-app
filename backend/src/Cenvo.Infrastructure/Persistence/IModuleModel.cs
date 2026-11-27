using Microsoft.EntityFrameworkCore;

namespace Cenvo.Infrastructure.Persistence;

/// <summary>Each module registers its own entities/configuration into the shared model.</summary>
public interface IModuleModel
{
    void Build(ModelBuilder mb);
}
