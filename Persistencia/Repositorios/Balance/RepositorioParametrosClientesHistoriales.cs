// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Balance
{
    public interface IParametrosClientesHistoriales : IRepositorio<ParametroClienteHistorial>
    {
        Task GuardarParametroClienteHistorialAsync(ParametroClienteHistorial parametro);
    }
    public class RepositorioParametrosClientesHistoriales : Repositorio<ParametroClienteHistorial>, IParametrosClientesHistoriales
    {
        public RepositorioParametrosClientesHistoriales(IDbContextFactory<ContextoPrincipal> contexto, ILoggerFactory logger) : base(contexto, logger) { }

        public async Task GuardarParametroClienteHistorialAsync(ParametroClienteHistorial parametro)
        {
            if (parametro == null)
                throw new Exception("No se ha ingresado los datos del Parámetro Cliente Historial");

            parametro.IdHistorial = parametro.IdHistorial;
            parametro.Valor = parametro.Valor;
            parametro.Parametro = parametro.Parametro;

            await base.CreateAsync(parametro);
        }
    }
}
