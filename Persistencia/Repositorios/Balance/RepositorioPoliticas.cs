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
    public interface IPoliticas : IRepositorio<Politica>
    {
        Task<int> GuardarPoliticaAsync(Politica politica);
    }
    public class RepositorioPoliticas : Repositorio<Politica>, IPoliticas
    {
        public RepositorioPoliticas(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardarPoliticaAsync(Politica politica)
        {
            if (politica == null)
                throw new Exception("No se ha ingresado los datos del historial");

            /*if (calificacion.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            if (string.IsNullOrEmpty(calificacion.Identificacion))
                throw new Exception("No se ha ingresado la identificación del historial");*/

            await base.CreateAsync(politica);
            return politica.Id;
        }
    }
}
