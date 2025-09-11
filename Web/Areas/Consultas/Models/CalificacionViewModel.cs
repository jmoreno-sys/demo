// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Dominio.Entidades.Balances;
using Dominio.Tipos;

namespace Web.Areas.Consultas.Models
{
    public class ReporteCalificacionViewModel
    {
        public int IdHistorial { get; set; }
        public int IdEmpresa { get; set; }
        public int IdCalificacion { get; set; }
        public int IdCalificacionBuro { get; set; }
    }

    public class CalificacionViewModel
    {
        public int IdCalificacion { get; set; }
        public string Identificacion { get; set; }
        public int IdHistorial { get; set; }
        public bool BusquedaNueva { get; set; }
        public DateTime FechaInicio { get; set; }
        public int TotalValidados { get; set; }
        public int TotalAprobados { get; set; }
        public int TotalRechazados { get; set; }
        public decimal Calificacion { get; set; }
        public string Observaciones { get; set; }
        public bool Aprobado { get; set; }
        public double? Score { get; set; }
        public double? CupoEstimado { get; set; }
        public double? VentasEmpresa { get; set; }
        public double? PatrimonioEmpresa { get; set; }
        public string RangoIngreso { get; set; }
        public double? GastoFinanciero { get; set; }
        public TiposCalificaciones TipoCalificacion { get; set; }
        public FuentesBuro? TipoFuente { get; set; }
        public List<DetalleCalificacionViewModel> DetalleCalificacion { get; set; }

        #region AYASA
        public bool CalificacionClienteAyasa { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta.ModeloAutomotrizAyasa ModeloAutomotrizaAyasa { get; set; }
        #endregion

        #region INDUMOT
        public bool CalificacionClienteIndumot { get; set; }
        public Externos.Logica.Equifax.Resultados._NewDataSetExpertoIndumot ModeloIndumot { get; set; }
        #endregion

        #region BCAPITAL
        public bool CalificacionBCapital { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta ModeloBCapital { get; set; }
        #endregion BCAPITAL

        #region Cooperativas
        public bool CalificacionCooperativas { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta ModeloCooperativas { get; set; }
        #endregion Cooperativas

        #region RAGUI
        public bool CalificacionRagui { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta ModeloRagui { get; set; }
        #endregion RAGUI

        #region ALMESPANA
        public bool CalificacionVendedorAlmespana { get; set; }

        #endregion ALMESPANA

        #region MMOTASA
        public bool CalificacionMMotasa { get; set; }

        #endregion MMOTASA
        #region MERCAMOVIL
        public bool CalificacionMercaMovil{ get; set; }

        #endregion MERCAMOVIL
    }

    public class DetalleCalificacionViewModel
    {
        public int IdPolitica { get; set; }
        public string Politica { get; set; }
        public Politicas Tipo { get; set; }
        public string ValorResultado { get; set; }
        public string ReferenciaMinima { get; set; }
        public string Valor { get; set; }
        public string Parametro { get; set; }
        public bool ResultadoPolitica { get; set; }
        public bool Excepcional { get; set; }
        public string Observacion { get; set; }
        public DateTime? FechaCorte { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Instituciones { get; set; }
        public static bool ResultadoComparacion(decimal valor1, decimal valor2, string operador)
        {
            switch (operador)
            {
                case ">":
                    return valor1 > valor2;

                case "<":
                    return valor1 < valor2;

                case ">=":
                    return valor1 >= valor2;

                case "<=":
                    return valor1 <= valor2;

                default:
                    return false;
            }
        }
    }
    public class InstitucionViewModel
    {
        public string Nombre { get; set; }
        public decimal Valor { get; set; }
        public string NmbVencimiento { get; set; }
    }

    public class HistoricoVencidoViewModel
    {
        public DateTime FechaCorte { get; set; }
        public string NmbVencimiento { get; set; }
        public decimal Valor { get; set; }
        public string SistemaFinanciero { get; set; }
    }
    public class OperacionHistoricaViewModel
    {
        public DateTime? FechaCorte { get; set; }
        public double? DemandaJudicial { get; set; }
        public double? CarteraCastigada { get; set; }
        public double? Vencido { get; set; }
        public string RazonSocial { get; set; }
    }

    public class ProcesoLegal
    {
        public string Proceso { get; set; }
    }

    public class ProfesionEspecial
    {
        public string Profesion { get; set; }
    }

    public class ReporteCalificacionClienteViewModel
    {
        public int IdHistorial { get; set; }
        public int IdEmpresa { get; set; }
        public int IdCalificacion { get; set; }
        public int IdCalificacionBuro { get; set; }
    }
}
