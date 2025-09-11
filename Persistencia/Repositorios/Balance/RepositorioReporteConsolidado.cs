// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Balance
{
    public interface IReportesConsolidados : IRepositorio<ReporteConsolidado>
    {
        Task<int> GuardarReporteConsolidadoAsync(ReporteConsolidado historial);
        Task<int> GuardarReporteConsolidadoProveedorInternacionalAsync(ReporteConsolidado historial);

    }

    public class RepositorioReportesConsolidados : Repositorio<ReporteConsolidado>, IReportesConsolidados
    {
        public RepositorioReportesConsolidados(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardarReporteConsolidadoAsync(ReporteConsolidado historial)
        {
            if (historial == null)
                throw new Exception("No se ha ingresado los datos del historial");

            if (historial.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            if (string.IsNullOrEmpty(historial.Identificacion))
                throw new Exception("No se ha ingresado la identificación del historial");

            await base.CreateAsync(historial);
            return historial.Id;
        }
        public async Task<int> GuardarReporteConsolidadoProveedorInternacionalAsync(ReporteConsolidado historial)
        {
            if (historial == null)
                throw new Exception("No se ha ingresado los datos del historial");

            if (historial.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            await base.CreateAsync(historial);
            return historial.Id;
        }
    }
}
