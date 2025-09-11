using Infraestructura.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructura
{
    public static class Extensions
    {
        public static IServiceCollection AddInfraestructura(this IServiceCollection services)
        {
            //services.AddTransient<IFiscaliaDelitosService, FiscaliaDelitosService>();

            #region Externos
            services.AddSingleton<Externos.Logica.SRi.Controlador>();
            services.AddSingleton<Externos.Logica.Balances.Controlador>();
            services.AddSingleton<Externos.Logica.IESS.Controlador>();
            //services.AddSingleton<Externos.Logica.Senescyt.Controlador>();
            services.AddSingleton<Externos.Logica.FJudicial.Controlador>();
            services.AddSingleton<Externos.Logica.ANT.Controlador>();
            services.AddSingleton<Externos.Logica.PensionesAlimenticias.Controlador>();
            services.AddSingleton<Externos.Logica.Garancheck.Controlador>();
            services.AddSingleton<Externos.Logica.SERCOP.Controlador>();
            services.AddSingleton<Externos.Logica.BuroCredito.Controlador>();
            services.AddSingleton<Externos.Logica.FiscaliaDelitos.Controlador>();
            services.AddSingleton<Externos.Logica.Equifax.Controlador>();
            services.AddSingleton<Externos.Logica.SuperBancos.Controlador>();
            services.AddSingleton<Externos.Logica.AntecedentesPenales.Controlador>();
            services.AddSingleton<Externos.Logica.PredioMunicipio.Controlador>();
            services.AddSingleton<Externos.Logica.UAFE.Controlador>();
            services.AddSingleton<Externos.Logica.RegistroCivilWS.Controlador>();
            #endregion Externos

            #region Servicios
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IConsultaService, ConsultaService>();
            #endregion Servicios

            return services;
        }
    }
}
