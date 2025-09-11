// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.Common;
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
    public interface IDetallesHistorial : IRepositorio<DetalleHistorial>
    {
        Task GuardarDetalleHistorialAsync(DetalleHistorial detalleHistorial);
        Task ActualizarDetalleHistorialAsync(DetalleHistorial detalleHistorial);
        void GuardarDetalleHistorial(DetalleHistorial detalleHistorial);
    }
    public class RepositorioDetallesHistorial : Repositorio<DetalleHistorial>, IDetallesHistorial
    {
        public RepositorioDetallesHistorial(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }

        public async Task GuardarDetalleHistorialAsync(DetalleHistorial detalleHistorial)
        {
            if (detalleHistorial == null)
                throw new Exception("No se ha ingresado los datos del detalle Historial");

            detalleHistorial.IdHistorial = detalleHistorial.IdHistorial;
            detalleHistorial.TipoFuente = detalleHistorial.TipoFuente;
            detalleHistorial.Generado = detalleHistorial.Generado;
            detalleHistorial.Data = detalleHistorial.Data;

            await base.CreateAsync(detalleHistorial);
        }

        public void GuardarDetalleHistorial(DetalleHistorial detalleHistorial)
        {
            if (detalleHistorial == null)
                throw new Exception("No se ha ingresado los datos del detalle Historial");

            detalleHistorial.IdHistorial = detalleHistorial.IdHistorial;
            detalleHistorial.TipoFuente = detalleHistorial.TipoFuente;
            detalleHistorial.Generado = detalleHistorial.Generado;
            detalleHistorial.Data = detalleHistorial.Data;

            base.Create(detalleHistorial);
        }

        public async Task ActualizarDetalleHistorialAsync(DetalleHistorial detalleHistorial)
        {
            if (detalleHistorial == null)
                throw new Exception("No se ha ingresado los datos del detalle Historial");

            detalleHistorial.IdHistorial = detalleHistorial.IdHistorial;
            detalleHistorial.TipoFuente = detalleHistorial.TipoFuente;
            detalleHistorial.Generado = detalleHistorial.Generado;
            detalleHistorial.Data = detalleHistorial.Data;
            await base.UpdateAsync(detalleHistorial);
        }
    }
}
