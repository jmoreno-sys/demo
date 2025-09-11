// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class Historial : Entidad
    {
        public Historial()
        {
            DetalleHistorial = new List<DetalleHistorial>();
            Calificaciones = new List<Calificacion>();
            ParametrosClientesHistorial = new List<ParametroClienteHistorial>();
        }

        public virtual Usuario Usuario { get; set; }
        public int IdUsuario { get; set; }
        public virtual PlanEmpresa PlanEmpresa { get; set; }
        public int IdPlanEmpresa { get; set; }
        public virtual PlanBuroCredito PlanBuroCredito { get; set; }
        public int? IdPlanBuroCredito { get; set; }
        public string DireccionIp { get; set; }
        public string Identificacion { get; set; }
        public string TipoIdentificacion { get; set; }
        public string IdentificacionSecundaria { get; set; }
        public string RazonSocialEmpresa { get; set; }
        public string NombresPersona { get; set; }
        public string FechaExpedicionCedula { get; set; }
        public int? Periodo { get; set; }
        public Consultas TipoConsulta { get; set; }
        public DateTime Fecha { get; set; }
        public string Observacion { get; set; }
        public string ParametrosBusqueda { get; set; }
        public virtual PlanEvaluacion PlanEvaluacion { get; set; }
        public int? IdPlanEvaluacion { get; set; }
        public FuentesBuro? TipoFuenteBuro { get; set; }
        public bool ConsultaBuroCompartido { get; set; }

        #region Relaciones
        public IEnumerable<DetalleHistorial> DetalleHistorial { get; set; }
        public IEnumerable<Calificacion> Calificaciones { get; set; }
        public IEnumerable<ParametroClienteHistorial> ParametrosClientesHistorial { get; set; }
        #endregion
    }
}
