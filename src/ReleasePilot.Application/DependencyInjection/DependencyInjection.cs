using Microsoft.Extensions.DependencyInjection;

namespace ReleasePilot.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            // Use this assembly to find all Handlers automatically
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Your custom behaviors
            cfg.AddOpenBehavior(typeof(ErrorHandlingBehavior<,>));
        });

        // If you add FluentValidation later, it goes here too:
        // services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
