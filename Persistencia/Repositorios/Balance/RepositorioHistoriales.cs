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
    public interface IHistoriales : IRepositorio<Historial>
    {
        Task<int> GuardarHistorialAsync(Historial historial);
        Task<int> GuardarHistorialProveedorInternacionalAsync(Historial historial);

    }

    public class RepositorioHistoriales : Repositorio<Historial>, IHistoriales
    {
        public RepositorioHistoriales(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardarHistorialAsync(Historial historial)
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
        public async Task<int> GuardarHistorialProveedorInternacionalAsync(Historial historial)
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
