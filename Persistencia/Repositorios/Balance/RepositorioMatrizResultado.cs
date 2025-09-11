// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Balance
{
    public interface IMatrizDualResultado : IRepositorio<MatrizDualResultado>
    {
        Task<int> GuardaDecisionAsync(MatrizDualResultado acceso);
    }
    public class RepositorioMatrizResultado : Repositorio<MatrizDualResultado>, IMatrizDualResultado
    {
        public RepositorioMatrizResultado(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task<int> GuardaDecisionAsync(MatrizDualResultado decision)
        {
            if (decision == null)
                throw new Exception("No se ha ingresado los datos");

            /*if (calificacion.IdUsuario == 0)
                throw new Exception("No se ha ingresado el id del Usuario");

            if (string.IsNullOrEmpty(calificacion.Identificacion))
                throw new Exception("No se ha ingresado la identificación del historial");*/

            await base.CreateAsync(decision);
            return decision.Id;
        }
    }
}
