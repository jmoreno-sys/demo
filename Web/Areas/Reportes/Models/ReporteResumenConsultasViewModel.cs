// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Web.Areas.Historiales.Models
{
    public class ReporteResumenConsultasFiltrosViewModel
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public bool? Meses { get; set; }
    }

    public class ReporteResumenConsultasViewModel
    {
        public string RazonSocial { get; set; }
        public string NombreEmpresa { get; set; }
        public string Identificacion { get; set; }
        public int? NumeroMaximoConsultasRuc { get; set; }
        public int? ConsultasActualesMensualesRuc { get; set; }
        public int? SaldoRuc { get; set; }
        public decimal? ValorAdicionalRuc { get; set; }
        public int? NumeroMaximoConsultasCedula { get; set; }
        public int? ConsultasActualesMensualesCedula { get; set; }
        public int? SaldoCedula { get; set; }
        public decimal? ValorAdicionalCedula { get; set; }
        public int? NumeroMaximoConsultasBuro { get; set; }
        public int? ConsultasActualesBuro { get; set; }
        public int? NumeroMaximoConsultas { get; set; }
        public int? ConsultasActualesMensuales { get; set; }
        public int? Saldo { get; set; }
        public decimal? ValorAdicional { get; set; }
        public int? IdEmpresa { get; set; }
        public bool EsUnificado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int? IdAsesorComercialConfiable { get; set; }
        public bool PlanDemo {  get; set; }
    }
}
