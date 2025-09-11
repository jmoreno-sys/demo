// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Dominio.Entidades.Balances;
using Dominio.Tipos;
using Dominio.Tipos.Clientes.Cliente1790325083001;
using Dominio.Tipos.Clientes.Cliente1090105244001;
using Newtonsoft.Json;
using Dominio.Tipos.Clientes.Cliente0990981930001;
using Dominio.Tipos.Clientes.Cliente1590001585001;
using Dominio.Tipos.Clientes.Cliente0992701374001;

namespace Web.Areas.Consultas.Models
{
    public class ReporteViewModel
    {
        public string Identificacion { get; set; }
        //public int Periodo { get; set; }
        public int IdHistorial { get; set; }
        public DateTime? FechaCedulacion { get; set; }
        public bool? AntecedentesPenales { get; set; }

        #region Buro BCapital
        public double IngresosBC { get; set; }
        public double GastosBC { get; set; }
        public double MontoSolicitadoBC { get; set; }
        public int PlazoBC { get; set; }
        public TipoPrestamoBCapitalAval TipoPrestamoBC { get; set; }
        public int MesesExperienciaActividadBC { get; set; }
        public string IdentificacionConyugeBC { get; set; }
        public bool PropiedadesBC { get; set; }

        #endregion Buro BCapital

        #region Clientes
        public SegmentoCartera Valor_1790325083001 { get; set; }
        public string ProveedorInternacional { get; set; }
        #endregion Clientes

        #region Indumot
        public string IdentificacionConyugeIndumot { get; set; }
        public string IdentificacionGaranteIndumot { get; set; }
        public string TipoProductoIndumot { get; set; }
        public decimal IngresosIndumot { get; set; }
        public decimal RestaGastoFinancieroIndumot { get; set; }
        public int PlazoIndumot { get; set; }
        public decimal MontoIndumot { get; set; }
        public decimal ValorEntradaIndumot { get; set; }
        #endregion Indumot

        #region Buro Cooperativas
        public double Ingresos { get; set; }
        public double Gastos { get; set; }
        public double MontoSolicitado { get; set; }
        public int Plazo { get; set; }
        public TipoPrestamoCooperativasAval TipoPrestamo { get; set; }

        #endregion Buro Cooperativas

        #region Banco Litoral
        public TipoLinea TipoLineaBLitoral { get; set; }
        public TipoCredito TipoCreditoBLitoral { get; set; }
        public int PlazoBLitoral { get; set; }
        public decimal MontoBLitoral { get; set; }
        public decimal IngresoBLitoral { get; set; }
        public decimal GastoHogarBLitoral { get; set; }
        public decimal RestaGastoFinancieroBLitoral { get; set; }
        public bool? ConsultaBuroBLitoral { get; set; }

        #endregion Banco Litoral

        #region Banco Litoral Microfinanza
        public TipoCreditoMicrofinanzas TipoCreditoBLitoralMicrofinanza { get; set; }
        public int PlazoBLitoralMicrofinanza { get; set; }
        public decimal MontoBLitoralMicrofinanza { get; set; }
        public decimal IngresoBLitoralMicrofinanza { get; set; }
        public decimal GastoHogarBLitoralMicrofinanza { get; set; }
        public decimal RestaGastoFinancieroBLitoralMicrofinanza { get; set; }
        public bool? ConsultaBuroBLitoralMicrofinanza { get; set; }

        #endregion Banco Litoral Microfinanza

        #region Cooperativa Tena
        public TipoCreditoCoopTena TipoCreditoCoopTena { get; set; }
        public int PlazoCoopTena { get; set; }
        public decimal GastoHogarCoopTena { get; set; }
        public decimal MontoCoopTena { get; set; }
        public decimal IngresoCoopTena { get; set; }
        public decimal RestaGastoFinancieroCoopTena { get; set; }

        #endregion Cooperativa Tena

        #region Banco DMiro
        public string IdentificacionConyugeBancoDMiro { get; set; }
        public EstadoCivil EstadoCivilBancoDMiro { get; set; }
        public double AntiguedadLaboralBancoDMiro { get; set; }
        public TipoInstruccion TipoInstruccionBancoDMiro { get; set; }
        public TipoPrestamo TipoPrestamoBancoDMiro { get; set; }
        public double MontoSolicitadoBancoDMiro { get; set; }
        public double PlazoBancoDMiro { get; set; }
        public double IngresoBancoDMiro { get; set; }
        public double? GastosPersonalesBancoDMiro { get; set; }
        public bool ModeloBancoDMiro { get; set; }

        #endregion Banco DMiro

        public static string EstadoSRi(string estado)
        {
            switch (estado)
            {
                case "ACT":
                    return "ACTIVO";

                case "SDE":
                    return "SUSPENDIDO";

                case "PAS":
                    return "PASIVO";

                default:
                    return "N/A";
            }
        }

        public static string EstadoSRiColor(string estado)
        {
            switch (estado)
            {
                case "ACT":
                    return "success";

                case "SDE":
                    return "warning";

                default:
                    return "danger";
            }
        }

        public static string EstadoSRiEstablecimientoColor(string estado)
        {
            switch (estado)
            {
                case "ABIERTO":
                    return "success";

                case "CERRADO":
                    return "danger";

                default:
                    return "warning";
            }
        }

        public static string TipoSRiEstablecimiento(string est)
        {
            switch (est)
            {
                case "MAT":
                    return "MATRIZ";

                case "LOC":
                    return "LOCAL";

                case "OFI":
                    return "OFICINA";

                default:
                    return est;
            }
        }

        public static string TipoContribuyente(string tipo)
        {
            switch (tipo)
            {
                case "SCD":
                    return "SOCIEDAD";
                case "PNL":
                    return "PERSONA NATURAL";
                default:
                    return "OTRO";
            }
        }

        public static string EstadoANTPuntosColor(double? puntos)
        {
            if (puntos == null)
                return "info";

            if (puntos >= 25 && puntos <= 30)
            {
                return "success";
            }
            else if (puntos >= 15 && puntos < 25)
            {
                return "warning";
            }
            else
            {
                return "danger";
            }
        }

        public static string FormatoGenero(string estado)
        {
            switch (estado)
            {
                case "HOMBRE":
                    return "MASCULINO";

                case "MUJER":
                    return "FEMENINO";

                default:
                    return "DESCONOCIDO";
            }
        }

        public static IDictionary<int, Externos.Logica.FJudicial.Modelos.Proceso> NormalizarProcesosLegal(IDictionary<int, Externos.Logica.FJudicial.Modelos.Proceso> dict1, IDictionary<int, Externos.Logica.FJudicial.Modelos.Proceso> dict2)
        {
            try
            {
                if (dict1.Any() && dict2.Any())
                {
                    var procesos = new List<Externos.Logica.FJudicial.Modelos.Proceso>();
                    procesos.AddRange(dict1.Values);
                    procesos.AddRange(dict2.Values.Where(m => !procesos.Any(s => s.Codigo == m.Codigo)));
                    return procesos.Distinct().Select((item, index) => new { Key = index, Value = item }).ToList().ToDictionary(x => x.Key, x => x.Value);
                }
                else if (dict1.Any() && !dict2.Any())
                    return dict1;
                else if (!dict1.Any() && dict2.Any())
                    return dict2;
            }
            catch (Exception) { }
            return new Dictionary<int, Externos.Logica.FJudicial.Modelos.Proceso>();
        }

        public static string NacionalidadesInterpol(List<string> lst)
        {
            if (lst == null)
                return "N/A";

            if (!lst.Any())
                return "N/A";

            try
            {
                var path = System.IO.Path.Combine("wwwroot", "data", "dataPaisesInterpol.json");
                var archivo = System.IO.File.ReadAllText(path);
                var datos = JsonConvert.DeserializeObject<List<PaisInterpolViewModel>>(archivo);
                if (datos != null && datos.Any())
                    return string.Join(" | ", datos.Where(m => m != null && lst.Contains(m.Codigo)).Select(m => m.Nombre).ToArray());
            }
            catch (Exception)
            {
            }
            return string.Join(" | ", lst);
        }

        public static string NacionalidadInterpol(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "N/A";

            try
            {
                var path = System.IO.Path.Combine("wwwroot", "data", "dataPaisesInterpol.json");
                var archivo = System.IO.File.ReadAllText(path);
                var datos = JsonConvert.DeserializeObject<List<PaisInterpolViewModel>>(archivo);
                if (datos != null && datos.Any())
                    return datos.FirstOrDefault(m => m.Codigo == str)?.Nombre?.ToUpper();
            }
            catch (Exception)
            {
            }
            return "N/A";
        }
        public static string EstadoIessColor(string estado)
        {
            switch (estado)
            {
                case "AFILIADO ACTIVO":
                    return "success";

                default:
                    return "warning";
            }
        }
    }

    public class BalanceViewModel
    {
        public Externos.Logica.Balances.Modelos.BalanceEmpresa Balance { get; set; }
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public bool CacheSri { get; set; }
        public Externos.Logica.IESS.Modelos.Persona Iess { get; set; }
        public Externos.Logica.Senescyt.Modelos.Persona Senescyt { get; set; }
        public Externos.Logica.FJudicial.Modelos.Persona FJudicial { get; set; }
        public Externos.Logica.FJudicial.Modelos.Persona FJEmpresa { get; set; }
        public Externos.Logica.ANT.Modelos.Licencia Ant { get; set; }
        public Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia PensionAlimenticia { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona Ciudadano { get; set; }
        public Externos.Logica.SERCOP.Modelos.ProveedorIncumplido Proveedor { get; set; }
        public List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria> ProveedorContraloria { get; set; }
        public List<Externos.Logica.Balances.Modelos.Similares> EmpresasSimilares { get; set; }
        public List<Externos.Logica.Balances.Modelos.BalanceEmpresa> Balances { get; set; }
        public Externos.Logica.Balances.Modelos.DatosAccionista BalancesVerificarAccionistas { get; set; }
        public Externos.Logica.IESS.Modelos.Afiliacion Afiliado { get; set; }
        public List<Externos.Logica.IESS.Modelos.Afiliado> AfiliadoAdicional { get; set; }
        public List<Externos.Logica.IESS.Modelos.Empleado> EmpleadosEmpresa { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosEmpresa { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosIess { get; set; }
        public Externos.Logica.Garancheck.Modelos.Personal Personales { get; set; }
        public Externos.Logica.Garancheck.Modelos.Familia Familiares { get; set; }
        public Externos.Logica.Garancheck.Modelos.RegistroCivil RegistroCivil { get; set; }
        public Externos.Logica.Balances.Modelos.DirectorioCompania DirectorioCompania { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosCedula { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosNatural { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosEmpresa { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.Resultado AntecedentesPenales { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada FuerzasArmadas { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia DeNoBaja { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.Resultado Predios { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.Resultado PrediosEmpresa { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca PrediosCuenca { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca PrediosEmpresaCuenca { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo PrediosStoDomingo { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo PrediosEmpresaStoDomingo { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui PrediosRuminahui { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui PrediosEmpresaRuminahui { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde PrediosQuininde { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde PrediosEmpresaQuininde { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga PrediosLatacunga { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga PrediosEmpresaLatacunga { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta PrediosManta { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta PrediosEmpresaManta { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato PrediosAmbato { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato PrediosEmpresaAmbato { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra PrediosIbarra { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra PrediosEmpresaIbarra { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal PrediosSanCristobal { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal PrediosEmpresaSanCristobal { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran PrediosDuran { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran PrediosEmpresaDuran { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio PrediosLagoAgrio { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio PrediosEmpresaLagoAgrio { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa PrediosSantaRosa { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa PrediosEmpresaSantaRosa { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua PrediosSucua { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua PrediosEmpresaSucua { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig PrediosSigSig { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig PrediosEmpresaSigSig { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia PrediosMejia { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia PrediosEmpresaMejia { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona PrediosMorona { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona PrediosEmpresaMorona { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena PrediosTena { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena PrediosEmpresaTena { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja PrediosLoja { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja PrediosEmpresaLoja { get; set; }
        public List<Externos.Logica.PredioMunicipio.Modelos.DatosPrediosPropiedadesLoja> PrediosDetallePropiedadesLoja { get; set; }
        public List<Externos.Logica.PredioMunicipio.Modelos.DatosPrediosPropiedadesLoja> PrediosDetallePropiedadesEmpresaLoja { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo PrediosCatamayo { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo PrediosEmpresaCatamayo { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon PrediosSamborondon { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon PrediosEmpresaSamborondon { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule PrediosDaule { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule PrediosEmpresaDaule { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe PrediosCayambe { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe PrediosEmpresaCayambe { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues PrediosAzogues { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues PrediosEmpresaAzogues { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas PrediosEsmeraldas { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas PrediosEmpresaEsmeraldas { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi PrediosCotacachi { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi PrediosEmpresaCotacachi { get; set; }
        public List<AnalisisHorizontalViewModel> AnalisisHorizontal { get; set; }
        public CalificacionViewModel ResultadoPoliticas { get; set; }
        public CalificacionViewModel ResultadoPoliticasBuro { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta BuroCredito { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado BuroCreditoEquifax { get; set; }
        public List<Externos.Logica.Balances.Modelos.RepresentanteEmpresa> RepresentantesEmpresas { get; set; }
        public Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito FiscaliaPersona { get; set; }
        public Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito FiscaliaEmpresa { get; set; }
        public Externos.Logica.Balances.Modelos.CatastroFantasma CatastroFantasma { get; set; }
        public List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm> DetallePredios { get; set; }
        public List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm> DetallePrediosEmpresa { get; set; }
        public Externos.Logica.UAFE.Modelos.Resultado ONU { get; set; }
        public Externos.Logica.UAFE.Modelos.Resultado ONU2206 { get; set; }
        public Externos.Logica.UAFE.Modelos.ResultadoInterpol Interpol { get; set; }
        public Externos.Logica.UAFE.Modelos.ResultadoOfac OFAC { get; set; }
        public string IdentificacionOriginal { get; set; }
        public int IdEmpresa { get; set; }
        public string IdentificacionEmpresa { get; set; }
        public string MensajeEvaluacion { get; set; }
        public string MensajeJudicialPersona { get; set; }
        public string MensajeJudicialEmpresa { get; set; }
        public string EmpresaPersonalizada { get; set; }
        public List<Externos.Logica.Balances.Modelos.Accionista> Accionistas { get; set; }
        public List<Externos.Logica.Balances.Modelos.AccionistaEmpresa> EmpresasAccionista { get; set; }
        public List<Externos.Logica.ANT.Modelos.AutoHistorico> AutosHistorico { get; set; }
        public Externos.Logica.FJudicial.Modelos.Impedimento Impedimento { get; set; }
        public int FuenteIess { get; set; }
        public string RazonSocial { get; set; }

        #region HistorialCache
        public bool CacheDirectorioCompanias { get; set; }
        public bool CacheBalances { get; set; }
        public bool CacheAnalisisHorizontal { get; set; }
        public bool CacheSuperBancoCedula { get; set; }
        public bool CacheSuperBancoNatural { get; set; }
        public bool CacheSuperBancoEmpresa { get; set; }
        public bool CacheAntecedentesPenales { get; set; }
        public bool CacheFuerzasArmadas { get; set; }
        public bool CacheDeNoBaja { get; set; }
        public bool CachePredios { get; set; }
        public bool CacheDetallePredios { get; set; }
        public bool CachePrediosEmpresa { get; set; }
        public bool CacheDetallePrediosEmpresa { get; set; }
        public bool CachePrediosCuenca { get; set; }
        public bool CachePrediosEmpresaCuenca { get; set; }
        public bool CachePrediosStoDomingo { get; set; }
        public bool CachePrediosEmpresaStoDomingo { get; set; }
        public bool CachePrediosRuminahui { get; set; }
        public bool CachePrediosEmpresaRuminahui { get; set; }
        public bool CachePrediosQuininde { get; set; }
        public bool CachePrediosEmpresaQuininde { get; set; }
        public bool CachePrediosLatacunga { get; set; }
        public bool CachePrediosEmpresaLatacunga { get; set; }
        public bool CachePrediosManta { get; set; }
        public bool CachePrediosEmpresaManta { get; set; }
        public bool CachePrediosAmbato { get; set; }
        public bool CachePrediosEmpresaAmbato { get; set; }
        public bool CachePrediosIbarra { get; set; }
        public bool CachePrediosEmpresaIbarra { get; set; }
        public bool CachePrediosSanCristobal { get; set; }
        public bool CachePrediosEmpresaSanCristobal { get; set; }
        public bool CachePrediosDuran { get; set; }
        public bool CachePrediosEmpresaDuran { get; set; }
        public bool CachePrediosLagoAgrio { get; set; }
        public bool CachePrediosEmpresaLagoAgrio { get; set; }
        public bool CachePrediosSantaRosa { get; set; }
        public bool CachePrediosEmpresaSantaRosa { get; set; }
        public bool CachePrediosSucua { get; set; }
        public bool CachePrediosEmpresaSucua { get; set; }
        public bool CachePrediosSigSig { get; set; }
        public bool CachePrediosEmpresaSigSig { get; set; }
        public bool CachePrediosMejia { get; set; }
        public bool CachePrediosEmpresaMejia { get; set; }
        public bool CachePrediosMorona { get; set; }
        public bool CachePrediosEmpresaMorona { get; set; }
        public bool CachePrediosTena { get; set; }
        public bool CachePrediosEmpresaTena { get; set; }
        public bool CachePrediosLoja { get; set; }
        public bool CachePrediosEmpresaLoja { get; set; }
        public bool CachePrediosCatamayo { get; set; }
        public bool CachePrediosEmpresaCatamayo { get; set; }
        public bool CachePrediosSamborondon { get; set; }
        public bool CachePrediosEmpresaSamborondon { get; set; }
        public bool CachePrediosDaule { get; set; }
        public bool CachePrediosEmpresaDaule { get; set; }
        public bool CachePrediosCayambe { get; set; }
        public bool CachePrediosEmpresaCayambe { get; set; }
        public bool CachePrediosAzogues { get; set; }
        public bool CachePrediosEmpresaAzogues { get; set; }
        public bool CachePrediosEsmeraldas { get; set; }
        public bool CachePrediosEmpresaEsmeraldas { get; set; }
        public bool CachePrediosCotacachi { get; set; }
        public bool CachePrediosEmpresaCotacachi { get; set; }
        public bool CacheIess { get; set; }
        public bool CacheAfiliado { get; set; }
        public bool CacheAfiliadoAdicional { get; set; }
        public bool CacheSenescyt { get; set; }
        public bool CacheJudicial { get; set; }
        public bool CacheJudicialEmpresa { get; set; }
        public bool CacheFiscaliaPersona { get; set; }
        public bool CacheFiscaliaEmpresa { get; set; }
        public bool CacheImpedimento { get; set; }
        public bool CacheAnt { get; set; }
        public bool CachePensionAlimenticia { get; set; }
        public bool CacheSercop { get; set; }
        public bool CacheSercopContraloria { get; set; }
        public bool CacheBuro { get; set; }
        public bool CacheFamiliares { get; set; }
        public bool CacheOnu { get; set; }
        public bool CacheOnu20226 { get; set; }
        public bool CacheInterpol { get; set; }
        public bool CacheOfac { get; set; }

        #endregion HistorialCache

        #region FuentesEquifax
        public Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico EvolucionHistorica { get; set; }
        public Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento HistoricoEstructuraVencimiento { get; set; }
        public List<NivelSaldoPorVencerPorInstitucionViewModel> SaldoVencerInstitucion { get; set; }
        public List<NivelOperacionInstitucionViewModel> OperacionInstitucion { get; set; }
        public List<NivelOperacionInstitucionViewModel> OperacionInstitucionPV { get; set; }
        public List<NivelDetalleVencidoInstitucionViewModel> DetalleVencidoInstitucion { get; set; }
        #endregion FuentesEquifax
    }

    public class JudicialViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.FJudicial.Modelos.Persona FJudicial { get; set; }
        public Externos.Logica.FJudicial.Modelos.Persona FJEmpresa { get; set; }
        public Externos.Logica.FJudicial.Modelos.Impedimento Impedimento { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaImpedimento { get; set; }
        public bool? FuenteActiva { get; set; }
        public bool ConsultaPersonalizada { get; set; }
        public string RutaArchivo { get; set; }
    }

    public class IessViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona Ciudadano { get; set; }
        public Externos.Logica.IESS.Modelos.Persona Iess { get; set; }
        public Externos.Logica.IESS.Modelos.Afiliacion Afiliado { get; set; }
        public List<Externos.Logica.IESS.Modelos.Afiliado> AfiliadoAdicional { get; set; }
        public List<Externos.Logica.IESS.Modelos.Empleado> EmpleadosEmpresa { get; set; }
        public Externos.Logica.IESS.Modelos.Jubilado IessJubilado { get; set; }
        public bool BusquedaNuevaIess { get; set; }
        public bool BusquedaNuevaAfiliado { get; set; }
        public bool EmpresaConfiable { get; set; }
        public bool HistorialIess { get; set; }
    }
    public class IessJubiladoViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.IESS.Modelos.Jubilado IessJubilado { get; set; }
        public bool BusquedaNuevaIessJubilado { get; set; }
    }

    public class SenescytViewModel
    {
        public Externos.Logica.Senescyt.Modelos.Persona Senescyt { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool FuenteActiva { get; set; }
    }

    public class SuperBancosViewModel
    {
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancos { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosNatural { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosEmpresa { get; set; }
        public int TipoConsulta { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool BusquedaNuevaNatural { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool FechaExpedicion { get; set; }
    }

    public class InformacionSuperBancosViewModel
    {
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancos { get; set; }
        public string RutaArchivo { get; set; }
        public int TipoConsulta { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool FechaExpedicion { get; set; }
    }

    public class AntecedentesPenalesViewModel
    {
        public Externos.Logica.AntecedentesPenales.Modelos.Resultado Antecedentes { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool FuenteActiva { get; set; }
    }

    public class FuerzasArmadasViewModel
    {
        public Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada FuerzasArmadas { get; set; }
        public bool BusquedaNueva { get; set; }
    }

    public class DeNoBajaViewModel
    {
        public Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia DeNoBaja { get; set; }
        public bool BusquedaNueva { get; set; }
    }

    public class PrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.Resultado PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.Resultado PrediosEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public Historial HistorialCabecera { get; set; }
        public DetallePrediosViewModel DetallePrediosRepresentante { get; set; }
        public DetallePrediosViewModel DetallePrediosEmpresa { get; set; }
    }

    public class InformacionPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.Resultado Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
        public DetallePrediosViewModel DetallePredios { get; set; }
    }

    public class PrediosCuencaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca PrediosEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionCuencaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class DetallePrediosCuencaViewModel
    {
        public string Clave { get; set; }
        public string Titulo { get; set; }
        public int Anio { get; set; }
        public string DescripcionRubro { get; set; }
        public string FechaEmision { get; set; }
        public string FechaPago { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
    }

    public class DetallePrediosViewModel
    {
        public DetallePrediosViewModel()
        {
            Detalle = new List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm>();
        }

        public List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm> Detalle { get; set; }
        public bool BusquedaNueva { get; set; }
    }
    public class DetallePrediosLojaViewModel
    {
        public DetallePrediosLojaViewModel()
        {
            Detalle = new List<Externos.Logica.PredioMunicipio.Modelos.DatosPrediosPropiedadesLoja>();
        }

        public List<Externos.Logica.PredioMunicipio.Modelos.DatosPrediosPropiedadesLoja> Detalle { get; set; }
        public bool BusquedaNueva { get; set; }
    }

    public class ANTViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.ANT.Modelos.Licencia Licencia { get; set; }
        public List<Externos.Logica.ANT.Modelos.AutoHistorico> AutosHistorico { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool FuenteActiva { get; set; }
    }

    public class PensionAlimenticiaViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia PensionAlimenticia { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool FuenteActiva { get; set; }
    }

    public class SERCOPViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona Ciudadano { get; set; }
        public Externos.Logica.SERCOP.Modelos.ProveedorIncumplido Proveedor { get; set; }
        public List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria> ProveedorContraloria { get; set; }
        public bool BusquedaNueva { get; set; }
    }

    public class BuroCreditoViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta BuroCredito { get; set; }
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta ErrorAval { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado BuroCreditoEquifax { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado ErrorEquifax { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
        public FuentesBuro Fuente { get; set; }
        public string MensajeError { get; set; }
    }

    public class BuroCreditoEquifaxViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado Equifax { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
        public FuentesBuro Fuente { get; set; }
    }

    public class DelitosViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito FiscaliaPersona { get; set; }
        public Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito FiscaliaEmpresa { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool FuenteActiva { get; set; }
    }

    public class UafeViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.UAFE.Modelos.Resultado ONU { get; set; }
        public Externos.Logica.UAFE.Modelos.Resultado ONU2206 { get; set; }
        public Externos.Logica.UAFE.Modelos.ResultadoInterpol Interpol { get; set; }
        public Externos.Logica.UAFE.Modelos.ResultadoOfac OFAC { get; set; }
        public bool BusquedaNuevaOnu { get; set; }
        public bool BusquedaNuevaOnu2206 { get; set; }
        public bool BusquedaNuevaInterpol { get; set; }
        public bool BusquedaNuevaOfac { get; set; }
        public string MensajeErrorOnu { get; set; }
        public string MensajeErrorOnu2206 { get; set; }
        public string MensajeErrorInterpol { get; set; }
        public string MensajeErrorOfac { get; set; }
        public bool BusquedaJuridica { get; set; }
        public bool AccesoOnu { get; set; }
        public bool AccesoOfac { get; set; }
        public bool AccesoInterpol { get; set; }
    }

    public class SRIViewModel
    {
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosEmpresa { get; set; }
        public List<Externos.Logica.Balances.Modelos.Similares> EmpresasSimilares { get; set; }
        public Externos.Logica.Balances.Modelos.CatastroFantasma CatastroFantasma { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool FuenteActiva { get; set; }
    }

    public class BalancesViewModel
    {
        public Historial HistorialCabecera { get; set; }
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Balances.Modelos.BalanceEmpresa Balance { get; set; }
        public List<Externos.Logica.Balances.Modelos.BalanceEmpresa> Balances { get; set; }
        public List<AnalisisHorizontalViewModel> AnalisisHorizontal { get; set; }
        public Externos.Logica.Balances.Modelos.DirectorioCompania DirectorioCompania { get; set; }
        public bool MultiplesPeriodos { get; set; }
        public int PeriodoBusqueda { get; set; }
        public bool SoloEmpresasSimilares { get; set; }
        public List<int> PeriodosBusqueda { get; set; }
        public bool BusquedaNueva { get; set; }
        public List<Externos.Logica.Balances.Modelos.RepresentanteEmpresa> RepresentantesEmpresas { get; set; }
        public List<Externos.Logica.Balances.Modelos.Accionista> Accionistas { get; set; }
        public List<Externos.Logica.Balances.Modelos.AccionistaEmpresa> EmpresasAccionista { get; set; }
        public Externos.Logica.Balances.Modelos.DatosAccionista VerificarAccionista { get; set; }
    }

    public class CivilViewModel
    {
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona Ciudadano { get; set; }
        public Externos.Logica.Garancheck.Modelos.Personal Personales { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosEmpresa { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosIess { get; set; }
        public Externos.Logica.Garancheck.Modelos.Familia Familiares { get; set; }
        public Externos.Logica.Garancheck.Modelos.RegistroCivil RegistroCivil { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool ConsultaGenealogia { get; set; } = false;
    }

    public class ContactoViewModel
    {
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosIess { get; set; }
        public Externos.Logica.Garancheck.Modelos.RegistroCivil RegistroCivil { get; set; }
        public Externos.Logica.Garancheck.Modelos.Personal Personales { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona Ciudadano { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosEmpresa { get; set; }
        public bool BusquedaNueva { get; set; }
    }

    public class HistorialViewModel
    {
        public int IdHistorial { get; set; }
        public string TipoIdentificacion { get; set; }
    }

    public class PeriodosDinamicosViewModel
    {
        public PeriodosDinamicosViewModel()
        {
            Periodos = new List<PeriodosViewModel>();
        }

        public bool Activo { get; set; }
        public int Frecuencia { get; set; }
        public List<PeriodosViewModel> Periodos { get; set; }
    }

    public class PeriodosViewModel
    {
        public int Valor { get; set; }
    }

    public class ImpuestoRentaViewModel
    {
        public int Periodo { get; set; }
        public short Formulario { get; set; }
        public double Causado { get; set; }
        public double Divisa { get; set; }
    }

    public class BusquedaNombreViewModel
    {
        public string Cedula { get; set; }
        public string Nombre { get; set; }
    }

    public class NombreFiltroViewModel
    {
        public string Nombre { get; set; }
    }

    public class PropietarioViewModel
    {
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public bool Historico { get; set; }
    }

    public class ParametroClienteViewModel
    {
        public int IdHistorial { get; set; }
        public string IdentificacionEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string Identificacion { get; set; }

        #region Clientes
        public SegmentoCartera Valor_1790325083001 { get; set; }
        public string ProveedorInternacional { get; set; }
        #endregion Clientes
    }

    public class PrediosSantoDomingoViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }

    }

    public class InformacionSantoDomingoPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosRuminahuiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }

    }

    public class InformacionRuminahuiPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosQuinindeViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }

    }

    public class InformacionQuinindePrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class DetallePrediosQuinindeViewModel
    {
        public string DescripcionPredio { get; set; }
        public string SubTotalDescripcion { get; set; }
        public string SubTotalValor { get; set; }
        public List<InformacionPredioQuininde> InformacionPredio { get; set; }
        public string Estado { get; set; }
    }

    public class InformacionPredioQuininde
    {
        public string NumeroEmision { get; set; }
        public string Clave { get; set; }
        public string Periodo { get; set; }
        public string Valor { get; set; }
        public string Interes { get; set; }
        public string Descuento { get; set; }
        public string Coactiva { get; set; }
        public string Total { get; set; }
    }

    public class PrediosLatacungaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionLatacungaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosMantaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionMantaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosAmbatoViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionIbarraPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosIbarraViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionSanCristobalPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosSanCristobalViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionAmbatoPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class InformacionDuranPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosDuranViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionLagoAgrioPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosLagoAgrioViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionSantaRosaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosSantaRosaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionSucuaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosSucuaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionSigSigPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosSigSigViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionMejiaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosMejiaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionMoronaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosMoronaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionPredioMorona
    {
        public string Denominacion { get; set; }
        public string Clave { get; set; }
        public string Concepto { get; set; }
        public decimal Valor { get; set; }
        public decimal Descuento { get; set; }
        public decimal Multa { get; set; }
        public decimal Mora { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public decimal ValorAPagar { get; set; }
    }

    public class InformacionTenaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosTenaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }
    public class InformacionCatamayoPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosCatamayoViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionLojaPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja? Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
        public DetallePrediosLojaViewModel DetallePredios { get; set; }
    }

    public class PrediosLojaViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class InformacionSamborondonPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosSamborondonViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionDaulePrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosDauleViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionCayambePrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosCayambeViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class InformacionAzoguesPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }
    public class InformacionEsmeraldasPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }
    public class InformacionCotacachiPrediosViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi Predios { get; set; }
        public bool BusquedaNueva { get; set; }
        public Historial HistorialCabecera { get; set; }
        public int TipoConsulta { get; set; }
    }

    public class PrediosAzoguesViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class PrediosEsmeraldasViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class PrediosCotacachiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi PrediosEmpresa { get; set; }
        public bool BusquedaNuevaEmpresa { get; set; }
        public bool BusquedaNuevaRepresentante { get; set; }
        public Historial HistorialCabecera { get; set; }
    }

    public class DatosAccionistasViewModel
    {
        public Externos.Logica.Balances.Modelos.DatosAccionista DatosAccionista { get; set; }
        public bool BusquedaNueva { get; set; }
    }

    public class PaisInterpolViewModel
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
    }

    public class PensionAlimenticiaTemporalViewModel
    {
        public string Tipo { get; set; }
        public string Nombre { get; set; }
        public string NombreComprobado { get; set; }
    }

    public class SolicitudCreditoViewModel
    {
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosEmpresa { get; set; }
        public Externos.Logica.Garancheck.Modelos.RegistroCivil RegistroCivil { get; set; }
        public bool TipoJuridico { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona PersonaGarancheck { get; set; }
    }

    public class SolicitudCreditoMundoFactorViewModel
    {
        public Externos.Logica.SRi.Modelos.Contribuyente Sri { get; set; }
        public Externos.Logica.Garancheck.Modelos.RegistroCivil RegistroCivil { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona Ciudadano { get; set; }
        public Externos.Logica.ANT.Modelos.Licencia Ant { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosEmpresa { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.IESS.Modelos.Afiliacion Afiliado { get; set; }
        public List<Externos.Logica.IESS.Modelos.Afiliado> AfiliadoAdicional { get; set; }
        public Externos.Logica.Balances.Modelos.BalanceEmpresa Balance { get; set; }
        public Externos.Logica.Garancheck.Modelos.Personal Personales { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto ContactosIess { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado BuroCreditoEquifax { get; set; }
        public Externos.Logica.Balances.Modelos.DirectorioCompania DirectorioCompania { get; set; }
        public List<Externos.Logica.Balances.Modelos.Accionista> Accionistas { get; set; }
        public List<Externos.Logica.Balances.Modelos.BalanceEmpresa> Balances { get; set; }
    }

    public class AccionistaMundoFactorViewModel
    {
        public string Nombre { get; set; }
        public string Identificacion { get; set; }
    }

    public class AutosMundoFactorViewModel
    {
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int? Anio { get; set; }
        public string Placa { get; set; }
    }

    #region Fuentes Equifax

    public class ReporteEquifaxViewModel
    {
        public string Identificacion { get; set; }
        public int IdHistorial { get; set; }
        public string CodigoInstitucion { get; set; }
        public string TipoCredito { get; set; }
        public DateTime? FechaCorte { get; set; }
        public string SistemaCrediticio { get; set; }
    }

    public class NivelTotalDeudaHistoricaViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoTotalDeudaHistorica TotalDeudaHistorica { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
    }

    public class NivelEvolucionHistoricaViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico EvolucionHistorica { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
    }

    public class HistoricoEstructuraVencimientoViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento HistoricoEstructuraVencimiento { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
    }

    public class NivelSaldoPorVencerPorInstitucionViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoSaldoVencerInstitucion SaldoVencerInstitucion { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
        public string CodigoInstitucion { get; set; }
    }

    public class NivelOperacionInstitucionViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoOperacionInstitucion OperacionInstitucion { get; set; }
        public Externos.Logica.Equifax.Modelos.ResultadoOperacionInstitucion OperacionInstitucionPV { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
        public bool BusquedaNuevaPV { get; set; }
        public bool DatosCachePV { get; set; }
        public string CodigoInstitucion { get; set; }
        public string TipoCredito { get; set; }
        public string FechaCorte { get; set; }
    }

    public class NivelDetalleVencidoInstitucionViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoDetalleVencidoPorInstitucion DetalleVencidoInstitucion { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
        public string CodigoInstitucion { get; set; }
    }

    public class NivelDetalleOperacionEntidadViewModel
    {
        public Externos.Logica.Equifax.Modelos.ResultadoDetalleOperacionEntidad DetalleOperacionEntidad { get; set; }
        public bool BusquedaNueva { get; set; }
        public bool DatosCache { get; set; }
        public string FechaCorte { get; set; }
        public string SistemaCrediticio { get; set; }
    }

    #endregion Fuentes Equifax


}
