// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Balance
{
    public interface ICredencialesBuro : IRepositorio<CredencialBuro>
    {
    }

    public class RepositorioCredencialesBuro : Repositorio<CredencialBuro>, ICredencialesBuro
    {
        public RepositorioCredencialesBuro(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }
    }
}
