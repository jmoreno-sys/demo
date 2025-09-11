// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Abstracciones;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class PlanIdentificacion : Entidad, IAuditable
    {
        public PlanIdentificacion()
        {
            Planes = new List<PlanConsulta>();
        }

        public string NombrePlan { get; set; }
        public string Descripcion { get; set; }
        public Planes Tipo { get; set; }
        public decimal ValorPlanAnual { get; set; }
        public EstadosPlanes Estado { get; set; }

        #region Relaciones
        public IEnumerable<PlanConsulta> Planes { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
