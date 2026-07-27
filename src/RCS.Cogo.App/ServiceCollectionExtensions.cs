using Microsoft.Extensions.DependencyInjection;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;
using RCS.Piping.Core.Engines;
using RCS.Piping.Core.Builders;
using RCS.Piping.Core.Scripting;
using RCS.Piping.Core.Workflow;
using RCS.Piping.Core.Services;

namespace RCS.Cogo.App;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core COGO and Piping services into the dependency injection container.
    /// Enables headless execution, web API controllers, and CLI tools without WPF UI dependencies.
    /// </summary>
    public static IServiceCollection AddCogoServices(this IServiceCollection services)
    {
        // 1. Core Command Registry & COGO Script Engine
        services.AddSingleton(sp => AppInitializer.InitializeRegistry());
        services.AddTransient<ScriptEngine>();
        services.AddTransient<ICogoContext, CogoContext>();

        // 2. Catalog & Material Services
        services.AddSingleton<IMaterialCatalogCache, MaterialCatalogCache>();

        // 3. Piping & Analysis Engines
        services.AddTransient<PipeScriptCompiler>();
        services.AddTransient<IntakeAnalysisEngine>();
        services.AddTransient<ValidationEngine>();

        // 4. Deliverable Export Builders
        services.AddTransient<DxfBuilder>();
        services.AddTransient<PdfReportBuilder>();
        services.AddTransient<PnezdExportBuilder>();
        services.AddTransient<ExportBundleBuilder>();

        return services;
    }
}
