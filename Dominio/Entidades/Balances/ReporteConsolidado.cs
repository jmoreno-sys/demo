// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class ReporteConsolidado : Entidad
    {
        public ReporteConsolidado()
        {
        }
        public int IdUsuario { get; set; }
        public string DireccionIp { get; set; }
        public string Identificacion { get; set; }
        public string TipoIdentificacion { get; set; }
        public string ParametrosBusqueda { get; set; }
        public Consultas TipoConsulta { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreUsuario { get; set; }
        public string RazonSocial { get; set; }
        public string NombrePersona { get; set; }
        public string NombreEmpresa { get; set; }
        public string IdentificacionEmpresa { get; set; }
        public int IdEmpresa { get; set; }
        public bool ConsultaBuro { get; set; }
        public bool ConsultaEvaluacion { get; set; }
        public bool AprobadoEvaluacion { get; set; }
        public FuentesBuro FuenteBuro { get; set; }

        #region Relaciones
        public int HistorialId { get; set; }
        public virtual Historial Historial { get; set; }
        #endregion
    }
}
