using System.Reflection;
using Cenvo.Infrastructure.Persistence;
using Cenvo.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cenvo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Class-name suffixes that are registered in DI automatically (scoped).</summary>
    private static readonly string[] ServiceSuffixes = ["Service", "Issuer", "Checker", "Policy", "Factory", "Strategy"];

    public static IServiceCollection AddInfrastructure(this IServiceCollection s, string connectionString)
    {
        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        s.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        s.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        return s;
    }

    /// <summary>
    /// Registers a module: IModuleModel, IDomainEventHandler&lt;&gt;, and classes with a suffix in <see cref="ServiceSuffixes"/>
    /// (plus every Cenvo.* interface they implement — so other modules depend on the interface, not the class).
    /// </summary>
    public static IServiceCollection AddModule(this IServiceCollection s, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true }))
        {
            if (typeof(IModuleModel).IsAssignableFrom(type)) s.AddSingleton(typeof(IModuleModel), type);

            foreach (var handler in type.GetInterfaces().Where(IsEventHandler)) s.AddScoped(handler, type);

            if (!ServiceSuffixes.Any(type.Name.EndsWith)) continue;
            s.AddScoped(type);
            foreach (var contract in type.GetInterfaces().Where(i => i.Namespace?.StartsWith("Cenvo") == true && !IsEventHandler(i)))
                s.AddScoped(contract, sp => sp.GetRequiredService(type));
        }
        return s;
    }

    private static bool IsEventHandler(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>);
}
