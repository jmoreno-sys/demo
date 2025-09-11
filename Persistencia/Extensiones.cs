// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistencia.Repositorios.Balance;
using Persistencia.Repositorios.Identidad;

namespace Persistencia
{
    public static class Extensiones
    {
        public static IServiceCollection AddPersistencia(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ContextoPrincipal>(m =>
            {
                if (configuration == null) return;

                var connectionString = configuration.GetConnectionString(nameof(ContextoPrincipal));
                const string migrationsHistoryTableName = "__EFMigrationsHistory";
                m.UseSqlServer(connectionString, x => x.MigrationsHistoryTable(migrationsHistoryTableName, Esquemas.Migraciones));
            });

            services.AddDbContextFactory<ContextoPrincipal>(options =>
            {
                var connectionString = configuration.GetConnectionString(nameof(ContextoPrincipal));
                options.UseSqlServer(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", Esquemas.Migraciones));
            }, ServiceLifetime.Scoped);

            #region Repositorios         
            //Identidad
            services.AddTransient<IRoles, RepositorioRoles>();
            services.AddTransient<IUsuarios, RepositorioUsuario>();

            //Balances
            services.AddTransient<IEmpresas, RepositorioEmpresas>();
            services.AddTransient<IHistoriales, RepositorioHistoriales>();
            services.AddTransient<IDetallesHistorial, RepositorioDetallesHistorial>();
            services.AddTransient<IPlanesIdentificaciones, RepositorioPlanesIdentificaciones>();
            services.AddTransient<IPlanesConsultas, RepositorioPlanesConsultas>();
            services.AddTransient<IPlanesEmpresas, RepositorioPlanesEmpresas>();
            services.AddTransient<IPlanesBuroCredito, RepositorioPlanesBuroCredito>();
            services.AddTransient<IPoliticas, RepositorioPoliticas>();
            services.AddTransient<ICalificaciones, RepositorioCalificaciones>();
            services.AddTransient<IDetalleCalificaciones, RepositorioDetalleCalificaciones>();
            services.AddTransient<IPlanesEvaluaciones, RepositorioPlanesEvaluaciones>();
            services.AddTransient<IAccesos, RepositorioAccesos>();
            services.AddTransient<ICredencialesBuro, RepositorioCredencialesBuro>();
            services.AddTransient<IParametrosClientesHistoriales, RepositorioParametrosClientesHistoriales>();
            services.AddTransient<IReportesConsolidados, RepositorioReportesConsolidados>();

            return services;
            #endregion Repositorios
        }
    }
}
