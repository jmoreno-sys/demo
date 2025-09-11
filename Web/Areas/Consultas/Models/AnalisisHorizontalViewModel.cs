// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Serialization;
using Externos.Logica.Balances.Modelos;
using Externos.Logica.Interfaces;

namespace Web.Areas.Consultas.Models
{
    public class AnalisisHorizontalViewModel
    {
        public int Periodo { get; set; }
        public string RUC { get; set; }
        public string RazonSocial { get; set; }
        public string CodigoActividadEconomica { get; set; }
        public string ActividadEconomica { get; set; }
        public string CIIU { get; set; }
        public ActivosViewModel Activos { get; set; }
        public PasivosViewModel Pasivos { get; set; }
        public PatrimonioViewModel Patrimonio { get; set; }
        public OtrosViewModel Otros { get; set; }
        public IndicesViewModel Indices { get; set; }
    }
    public class ActivosViewModel
    {
        public decimal? ActivosImpuestosCorrientes { get; set; }
        public decimal? OtrosActivosCorrientes { get; set; }
        public decimal? OtrosActivosNoCorrientes { get; set; }
        public decimal? TotalActivoCorriente { get; set; }
        public decimal? TotalActivoNoCorriente { get; set; }
        public decimal? TotalActivo { get; set; }
    }
    public class PasivosViewModel
    {
        public decimal? OtrosPasivosCorrientes { get; set; }
        public decimal? OtrosPasivosNoCorrientes { get; set; }
        public decimal? TotalPasivoCorriente { get; set; }
        public decimal? TotalPasivoNoCorriente { get; set; }
        public decimal? TotalPasivo { get; set; }
    }
    public class PatrimonioViewModel
    {
        public decimal? CapitalSuscrito { get; set; }
        public decimal? PatrimonioNeto { get; set; }
        public decimal? TotalPasivoPatrimonio { get; set; }
    }
    public class OtrosViewModel
    {
        public decimal? Inventarios { get; set; }
        public decimal? EfectivoYCaja { get; set; }
        public decimal? PropiedadPlantaEquipo { get; set; }
        public decimal? AportesSociosFuturasCap { get; set; }
        public decimal? ReservaLegal { get; set; }
        public decimal? Ventas { get; set; }
        public decimal? Servicios { get; set; }
        public decimal? TotalIngresos { get; set; }
        public decimal? ImpuestoRenta { get; set; }
        public decimal? CostoDeVentas { get; set; }
        public decimal? GastosOperacionales { get; set; }
        public decimal? IngresosNoOperacionales { get; set; }
        public decimal? UtilidadEjercicio { get; set; }
        public decimal? PerdidaEjercicio { get; set; }
        public decimal? ResultadosAcumulados { get; set; }
        public decimal? OtrosIngresosNoOperacionales { get; set; }
        public decimal? P15x100Trabajadores { get; set; }
        public decimal? GastosFinancieros { get; set; }
        public decimal? CxCComercialesTerceros { get; set; }
        public decimal? CxCAccionistasYRelacionados { get; set; }
        public decimal? CxPProveedoresTerceros { get; set; }
        public decimal? CxPAccionistasYRelacionados { get; set; }
        public decimal? ProvisionesBeneficiosEmpleados { get; set; }
        public decimal? ObFinancierasCortoPlazo { get; set; }
        public decimal? OBFinancierasLargoPlazo { get; set; }
        public decimal? ProvisionesCorrientes { get; set; }
        public decimal? ProvisionesNoCorrientes { get; set; }
        public decimal? Depreciaciones { get; set; }
        public decimal? IngresosOperacionales { get; set; }
        public decimal? CostosOperacionales { get; set; }
    }

    public class IndicesViewModel
    {
        public decimal? UtilidadBruta { get; set; }
        public decimal? UtilidadOperacional { get; set; }
        public decimal? GananciaAntesDe15x100YImpuestos { get; set; }
        public decimal? GananciaAntesDeImpuestos { get; set; }
        public decimal? GananciaNeta { get; set; }
        public decimal? PruebaAcida { get; set; }
        public decimal? LiquidezCorriente { get; set; }
        public decimal? EBITDA { get; set; }
        public decimal? MargenBruto { get; set; }
        public decimal? MargenOperacional { get; set; }
        public decimal? MargenNeto { get; set; }
        public decimal? CoberturaIntereses { get; set; }
        public decimal? EndeudamientoActivo { get; set; }
        public decimal? PeriodoPromCobro { get; set; }
        public decimal? PeriodoPromPago { get; set; }
        public decimal? CapitalTrabajo { get; set; }
        public decimal? DiasInventario { get; set; }
        public decimal? ROA { get; set; }
        public decimal? ROE { get; set; }
    }
}
