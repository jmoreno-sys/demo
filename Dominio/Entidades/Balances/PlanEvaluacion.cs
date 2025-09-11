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
    public class PlanEvaluacion : Entidad, IAuditable
    {
        public PlanEvaluacion()
        {
            Historial = new List<Historial>();
            BloquearConsultas = false;
        }

        public virtual Empresa Empresa { get; set; }
        public int IdEmpresa { get; set; }
        public int NumeroConsultas { get; set; }
        public decimal ValorConsulta { get; set; }
        public decimal ValorConsultaAdicional { get; set; }
        public DateTime FechaInicioPlan { get; set; }
        public DateTime FechaFinPlan { get; set; }
        public EstadosPlanesEvaluaciones Estado { get; set; }
        public bool BloquearConsultas { get; set; }

        #region Relaciones
        public IEnumerable<Historial> Historial { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
