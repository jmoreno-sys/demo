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
    public class PlanEmpresa : Entidad, IAuditable
    {
        public PlanEmpresa()
        {
            Historial = new List<Historial>();
            BloquearConsultas = false;
        }

        public virtual Empresa Empresa { get; set; }
        public int IdEmpresa { get; set; }
        //Separada
        public int NumeroConsultasRuc { get; set; }
        public int NumeroConsultasCedula { get; set; }
        public decimal ValorPlanAnualRuc { get; set; }
        public decimal ValorPlanAnualCedula { get; set; }
        public decimal ValorPlanMensualRuc { get; set; }
        public decimal ValorPlanMensualCedula { get; set; }
        public decimal ValorPorConsultaRucs { get; set; }
        public decimal ValorPorConsultaCedulas { get; set; }
        public decimal ValorConsultaAdicionalRucs { get; set; }
        public decimal ValorConsultaAdicionalCedulas { get; set; }
        //Unificado
        public int? NumeroConsultas { get; set; }
        public decimal? ValorPlanAnual { get; set; }
        public decimal? ValorPlanMensual { get; set; }
        public decimal? ValorPorConsulta { get; set; }
        public decimal? ValorConsultaAdicional { get; set; }
        public DateTime FechaInicioPlan { get; set; }
        public DateTime FechaFinPlan { get; set; }
        public string NombrePlan { get; set; }
        public EstadosPlanesEmpresas Estado { get; set; }
        public PlanesIdentificaciones TipoPlan { get; set; }
        public bool BloquearConsultas { get; set; }
        public bool PlanDemostracion { get; set; }

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
