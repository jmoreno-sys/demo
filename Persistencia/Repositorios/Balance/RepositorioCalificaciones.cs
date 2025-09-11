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
    public interface ICalificaciones : IRepositorio<Calificacion>
    {
        Task<int> GuardarCalificacionAsync(Calificacion calificacion);
        Task ActualizarCalificacionAsync(Calificacion calificacion);
    }
    public class RepositorioCalificaciones : Repositorio<Calificacion>, ICalificaciones
    {
        public RepositorioCalificaciones(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardarCalificacionAsync(Calificacion calificacion)
        {
            if (calificacion == null)
                throw new Exception("No se ha ingresado los datos de la calificacion");

            /*if (calificacion.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            if (string.IsNullOrEmpty(calificacion.Identificacion))
                throw new Exception("No se ha ingresado la identificación del historial");*/

            await base.CreateAsync(calificacion);
            return calificacion.Id;
        }
        public async Task ActualizarCalificacionAsync(Calificacion calificacion)
        {
            if (calificacion == null)
                throw new Exception("No se ha ingresado los datos de la calificacion");

            await base.UpdateAsync(calificacion);
        }

    }
}
