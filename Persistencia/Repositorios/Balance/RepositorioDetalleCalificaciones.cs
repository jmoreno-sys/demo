// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Balance
{
    public interface IDetalleCalificaciones : IRepositorio<DetalleCalificacion>
    {
        Task<int> GuardarPoliticaAsync(DetalleCalificacion detalleCalificacion);
        Task EliminarDetalleCalificacionAsync(List<DetalleCalificacion> detalleCalificacion);
    }
    public class RepositorioDetalleCalificaciones : Repositorio<DetalleCalificacion>, IDetalleCalificaciones
    {
        public RepositorioDetalleCalificaciones(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardarPoliticaAsync(DetalleCalificacion detalleCalificacion)
        {
            if (detalleCalificacion == null)
                throw new Exception("No se ha ingresado los datos del detalle de calificaciones");

            /*if (calificacion.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            if (string.IsNullOrEmpty(calificacion.Identificacion))
                throw new Exception("No se ha ingresado la identificación del historial");*/

            await base.CreateAsync(detalleCalificacion);
            return detalleCalificacion.Id;
        }
        public async Task EliminarDetalleCalificacionAsync(List<DetalleCalificacion> detalleCalificacion)
        {
            if (detalleCalificacion == null)
                throw new Exception("No se ha ingresado los datos del detalle calificacion");

            var ids = detalleCalificacion.Select(x => x.Id).ToArray();
            await base.DeleteAsync(ids);
        }
    }
}
