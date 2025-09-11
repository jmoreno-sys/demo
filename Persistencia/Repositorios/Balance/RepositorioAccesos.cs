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
    public interface IAccesos : IRepositorio<AccesoUsuario>
    {
        Task<int> GuardarAccesoAsync(AccesoUsuario acceso);
        Task EliminarAccesoAsync(List<AccesoUsuario> acceso);
    }
    public class RepositorioAccesos : Repositorio<AccesoUsuario>, IAccesos
    {
        public RepositorioAccesos(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardarAccesoAsync(AccesoUsuario acceso)
        {
            if (acceso == null)
                throw new Exception("No se ha ingresado los datos del acceso");

            /*if (calificacion.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            if (string.IsNullOrEmpty(calificacion.Identificacion))
                throw new Exception("No se ha ingresado la identificación del historial");*/

            await base.CreateAsync(acceso);
            return acceso.Id;
        }
        public async Task EliminarAccesoAsync(List<AccesoUsuario> acceso)
        {
            if (acceso == null)
                throw new Exception("No se ha ingresado los datos del acceso");

            var ids = acceso.Select(x => x.Id).ToArray();
            await base.DeleteAsync(ids);
        }
    }
}
