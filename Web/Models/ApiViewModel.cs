// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Dominio.Entidades.Balances;
using Dominio.Tipos;
using Dominio.Tipos.Clientes.Cliente0990981930001;
using Dominio.Tipos.Clientes.Cliente1790325083001;
using Newtonsoft.Json;
using Web.Areas.Consultas.Models;

namespace Web.Models
{
    public class ApiViewModel
    {
        public string Identificacion { get; set; }
        public int Periodos { get; set; }
        public int IdHistorial { get; set; }
        public FuentesApi[] Fuente { get; set; }
        public bool Evaluar { get; set; }
        public bool ConsultarBuro { get; set; }
        public string TipoIdentificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public Historial Historial { get; set; }
        public List<DetalleHistorial> DetallesHistoriales { get; set; }
    }

    public class ApiViewModel_1790325083001
    {
        public string Identificacion { get; set; }
        public int Periodos { get; set; }
        public int IdHistorial { get; set; }
        public FuentesApi[] Fuente { get; set; }
        public bool Evaluar { get; set; }
        public bool ConsultarBuro { get; set; }
        public string TipoIdentificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public SegmentoCartera SegmentoCartera { get; set; }
    }

    public class ApiViewModel_0990304211001
    {
        public string Identificacion { get; set; }
        public int Periodos { get; set; }
        public int IdHistorial { get; set; }
        public FuentesApi[] Fuente { get; set; }
        public bool Evaluar { get; set; }
        public bool ConsultarBuro { get; set; }
        public string TipoIdentificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public ModeloBuro_0990304211001 ModeloBuro { get; set; }
    }

    public class ApiViewModel_1790010937001
    {
        public string Identificacion { get; set; }
        public int IdHistorial { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
    }

    public class ApiViewModel_0990981930001
    {
        public string Identificacion { get; set; }
        public int Periodos { get; set; }
        public int IdHistorial { get; set; }
        public FuentesApi[] Fuente { get; set; }
        public bool Evaluar { get; set; }
        public TipoBuroModelo? ModeloBuro { get; set; }
        public string TipoIdentificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public ModeloBuro_0990981930001 ModeloBuroAutomotriz { get; set; }
        public ModeloBuroMicrofinanza_0990981930001 ModeloBuroMicrofinanza { get; set; }
    }

    public class ModeloBuro_0990981930001
    {
        public TipoLinea TipoLineaBLitoral { get; set; }
        public TipoCredito TipoCreditoBLitoral { get; set; }
        public int? PlazoBLitoral { get; set; }
        public decimal? MontoBLitoral { get; set; }
        public decimal? IngresoBLitoral { get; set; }
        public decimal? GastoHogarBLitoral { get; set; }
        public decimal? RestaGastoFinancieroBLitoral { get; set; }
    }

    public class ModeloBuroMicrofinanza_0990981930001
    {
        public TipoCreditoMicrofinanzas TipoCreditoBLitoralMicrofinanza { get; set; }
        public int? PlazoBLitoralMicrofinanza { get; set; }
        public decimal? MontoBLitoralMicrofinanza { get; set; }
        public decimal? IngresoBLitoralMicrofinanza { get; set; }
        public decimal? GastoHogarBLitoralMicrofinanza { get; set; }
        public decimal? RestaGastoFinancieroBLitoralMicrofinanza { get; set; }
    }

    public class ModeloBuro_0990304211001
    {
        public string IdentificacionConyuge { get; set; }
        public string IdentificacionGarante { get; set; }
        public string TipoProducto { get; set; }
        public decimal Ingresos { get; set; }
        public decimal RestaGastoFinanciero { get; set; }
        public int Plazo { get; set; }
        public decimal Monto { get; set; }
        public decimal ValorEntrada { get; set; }
    }

    public class RespuestaApiViewModel
    {
        public int IdPadre { get; set; }
        public SriApiViewModel Sri { get; set; }
        public CivilApiViewModel Civil { get; set; }
        public BalanceApiViewModel Societario { get; set; }
        public IessApiViewModel Iess { get; set; }
        public Externos.Logica.Senescyt.Modelos.Persona Senescyt { get; set; }
        public SuperBancosApiViewModel SuperBancos { get; set; }
        public PrediosApiViewModel Predios { get; set; }
        public PrediosCuencaApiViewModel PrediosCuenca { get; set; }
        public PrediosSantoDomingoApiViewModel PrediosSantoDomingo { get; set; }
        public PrediosRuminahuiApiViewModel PrediosRuminahui { get; set; }
        public PrediosQuinindeApiViewModel PrediosQuininde { get; set; }
        public PrediosLatacungaApiViewModel PrediosLatacunga { get; set; }
        public PrediosMantaApiViewModel PrediosManta { get; set; }
        public PrediosAmbatoApiViewModel PrediosAmbato { get; set; }
        public PrediosIbarraApiViewModel PrediosIbarra { get; set; }
        public PrediosSanCristobalApiViewModel PrediosSanCristobal { get; set; }
        public PrediosDuranApiViewModel PrediosDuran { get; set; }
        public PrediosLagoAgrioApiViewModel PrediosLagoAgrio { get; set; }
        public PrediosSantaRosaApiViewModel PrediosSantaRosa { get; set; }
        public PrediosSucuaApiViewModel PrediosSucua { get; set; }
        public PrediosSigSigApiViewModel PrediosSigsig { get; set; }
        public PrediosMejiaApiViewModel PrediosMejia { get; set; }
        public PrediosMoronaApiViewModel PrediosMorona { get; set; }
        public PrediosTenaApiViewModel PrediosTena { get; set; }
        public PrediosCatamayoApiViewModel PrediosCatamayo { get; set; }
        public PrediosLojaApiViewModel PrediosLoja { get; set; }
        public PrediosSamborondonApiViewModel PrediosSamborondon { get; set; }
        [JsonIgnore]
        public PrediosDauleApiViewModel PrediosDaule { get; set; }
        public PrediosCayambeApiViewModel PrediosCayambe { get; set; }
        public PrediosAzoguesApiViewModel PrediosAzogues { get; set; }
        public PrediosEsmeraldasApiViewModel PrediosEsmeraldas { get; set; }
        public PrediosCotacachiApiViewModel PrediosCotacachi { get; set; }
        public FiscaliaDelitosApiViewModel FiscaliaDelitos { get; set; }
        public UafeApiViewModel UAFE { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.Resultado AntecedentesPenales { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada FuerzasArmadas { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia DeNoBaja { get; set; }
        public JudicialApiViewModel Judicial { get; set; }
        public SercopApiViewModel Sercop { get; set; }
        public AntApiViewModel Ant { get; set; }
        public Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia PensionAlimenticia { get; set; }
        public BuroCreditoApi BuroCredito { get; set; }
        public EvaluacionApi Evaluacion { get; set; }
    }

    public class RespuestaApiViewModel_0990981930001
    {
        public int IdPadre { get; set; }
        public SriApiViewModel Sri { get; set; }
        public CivilApiViewModel Civil { get; set; }
        public BalanceApiViewModel Societario { get; set; }
        public IessApiViewModel Iess { get; set; }
        public Externos.Logica.Senescyt.Modelos.Persona Senescyt { get; set; }
        public SuperBancosApiViewModel SuperBancos { get; set; }
        public PrediosApiViewModel Predios { get; set; }
        public PrediosCuencaApiViewModel PrediosCuenca { get; set; }
        public PrediosSantoDomingoApiViewModel PrediosSantoDomingo { get; set; }
        public PrediosRuminahuiApiViewModel PrediosRuminahui { get; set; }
        public PrediosQuinindeApiViewModel PrediosQuininde { get; set; }
        public PrediosLatacungaApiViewModel PrediosLatacunga { get; set; }
        public PrediosMantaApiViewModel PrediosManta { get; set; }
        public PrediosAmbatoApiViewModel PrediosAmbato { get; set; }
        public PrediosIbarraApiViewModel PrediosIbarra { get; set; }
        public PrediosSanCristobalApiViewModel PrediosSanCristobal { get; set; }
        public PrediosDuranApiViewModel PrediosDuran { get; set; }
        public PrediosLagoAgrioApiViewModel PrediosLagoAgrio { get; set; }
        public PrediosSantaRosaApiViewModel PrediosSantaRosa { get; set; }
        public PrediosSucuaApiViewModel PrediosSucua { get; set; }
        public PrediosSigSigApiViewModel PrediosSigsig { get; set; }
        public PrediosMejiaApiViewModel PrediosMejia { get; set; }
        public PrediosMoronaApiViewModel PrediosMorona { get; set; }
        public PrediosTenaApiViewModel PrediosTena { get; set; }
        public PrediosCatamayoApiViewModel PrediosCatamayo { get; set; }
        public PrediosLojaApiViewModel PrediosLoja { get; set; }
        public PrediosSamborondonApiViewModel PrediosSamborondon { get; set; }
        public PrediosDauleApiViewModel PrediosDaule { get; set; }
        public PrediosCayambeApiViewModel PrediosCayambe { get; set; }
        public PrediosAzoguesApiViewModel PrediosAzogues { get; set; }
        public PrediosEsmeraldasApiViewModel PrediosEsmeraldas { get; set; }
        public PrediosCotacachiApiViewModel PrediosCotacachi { get; set; }
        public FiscaliaDelitosApiViewModel FiscaliaDelitos { get; set; }
        public UafeApiViewModel UAFE { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.Resultado AntecedentesPenales { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada FuerzasArmadas { get; set; }
        public Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia DeNoBaja { get; set; }
        public JudicialApiViewModel Judicial { get; set; }
        public SercopApiViewModel Sercop { get; set; }
        public AntApiViewModel Ant { get; set; }
        public Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia PensionAlimenticia { get; set; }
        public BuroCreditoApi BuroCredito { get; set; }
        public BuroCreditoApi_0990981930001 BuroCreditoAutomotriz { get; set; }
        public BuroCreditoApiMicrofinanza_0990981930001 BuroCreditoMicrofinanza { get; set; }
        public string MensajeErrorBuro { get; set; }
        public EvaluacionApi Evaluacion { get; set; }
    }

    public class SriApiViewModel
    {
        public string TipoIdentificacion { get; set; }
        public Externos.Logica.SRi.Modelos.Contribuyente Empresa { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public List<Externos.Logica.Balances.Modelos.Similares> EmpresasSimilares { get; set; }
        public Externos.Logica.Balances.Modelos.CatastroFantasma CatastroFantasma { get; set; }
    }

    public class BalanceApiViewModel
    {
        public Externos.Logica.Balances.Modelos.DirectorioCompania DirectorioCompania { get; set; }
        public List<Externos.Logica.Balances.Modelos.BalanceEmpresa> Balances { get; set; }
        public List<AnalisisHorizontalViewModel> AnalisisHorizontal { get; set; }
        public List<Externos.Logica.Balances.Modelos.RepresentanteEmpresa> RepresentantesEmpresas { get; set; }
        public List<Externos.Logica.Balances.Modelos.Accionista> Accionistas { get; set; }
        public List<Externos.Logica.Balances.Modelos.AccionistaEmpresa> EmpresasAccionista { get; set; }
    }

    public class BalancesApiMetodoViewModel
    {
        public Externos.Logica.Balances.Modelos.BalanceEmpresa Balance { get; set; }
        public List<Externos.Logica.Balances.Modelos.BalanceEmpresa> Balances { get; set; }
        public List<AnalisisHorizontalViewModel> AnalisisHorizontal { get; set; }
        public Externos.Logica.Balances.Modelos.DirectorioCompania DirectorioCompania { get; set; }
        public bool MultiplesPeriodos { get; set; }
        public int PeriodoBusqueda { get; set; }
        public bool SoloEmpresasSimilares { get; set; }
        public List<int> PeriodosBusqueda { get; set; }
        public List<Externos.Logica.Balances.Modelos.RepresentanteEmpresa> RepresentantesEmpresas { get; set; }
        public List<Externos.Logica.Balances.Modelos.Accionista> Accionistas { get; set; }
        public List<Externos.Logica.Balances.Modelos.AccionistaEmpresa> EmpresasAccionista { get; set; }
    }

    public class CivilApiViewModel
    {
        public string TipoIdentificacion { get; set; }
        public Externos.Logica.Garancheck.Modelos.Persona General { get; set; }
        public Externos.Logica.Garancheck.Modelos.Personal Personal { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.Garancheck.Modelos.Familia Familiares { get; set; }
    }

    public class CivilApiMetodoViewModel
    {
        public Externos.Logica.Garancheck.Modelos.Persona General { get; set; }
        public Externos.Logica.Garancheck.Modelos.Personal Personal { get; set; }
        public Externos.Logica.Garancheck.Modelos.Contacto Contactos { get; set; }
        public Externos.Logica.Garancheck.Modelos.Familia Familiares { get; set; }
        public Externos.Logica.Garancheck.Modelos.RegistroCivil RegistroCivil { get; set; }
    }

    public class JudicialApiViewModel
    {
        public Externos.Logica.FJudicial.Modelos.Persona Persona { get; set; }
        public Externos.Logica.FJudicial.Modelos.Persona Empresa { get; set; }
        public ImpedimentoApiViewModel Impedimento { get; set; }
    }

    public class ImpedimentoApiViewModel
    {
        public string Nombre { get; set; }
        public string Identificacion { get; set; }
        public string NumeroCertificado { get; set; }
        public string RegistraImpedimento { get; set; }
        public string Contenido { get; set; }
    }

    public class ImpedimentoMetodoApiViewModel
    {
        public Externos.Logica.FJudicial.Modelos.Impedimento Impedimento { get; set; }
        [JsonIgnore]
        public bool CacheImpedimento { get; set; }
    }

    public class IessApiViewModel
    {
        public Externos.Logica.IESS.Modelos.Persona Obligacion { get; set; }
        public AfiliadoApiViewModel Afiliado { get; set; }
        public List<Externos.Logica.IESS.Modelos.Afiliado> EmpresasAfiliado { get; set; }
        public List<Externos.Logica.IESS.Modelos.Empleado> EmpleadosEmpresa { get; set; }
        public bool AfiliadoActiva { get; set; }
        public bool ObligacionActiva { get; set; }

    }

    public class AfiliadoApiViewModel
    {
        public string Certificado { get; set; }
        public string Estado { get; set; }
        public string Fecha { get; set; }
    }
    public class IessApiMetodoViewModel
    {
        public Externos.Logica.IESS.Modelos.Persona Iess { get; set; }
        public Externos.Logica.IESS.Modelos.Afiliacion Afiliado { get; set; }
        public List<Externos.Logica.IESS.Modelos.Afiliado> AfiliadoAdicional { get; set; }
        public List<Externos.Logica.IESS.Modelos.Empleado> EmpleadosEmpresa { get; set; }
        public string? ErrorIess { get; set; }
        public string? ErrorAfiliado { get; set; }
        public string? ErrorAfiliadoAdicional { get; set; }
        public string? ErrorEmpleadosEmpresa { get; set; }
        public bool? FuenteActivaIess { get; set; }
        public bool? FuenteActivaAfiliado { get; set; }
        public bool? FuenteActivaAfiliadoAdicional { get; set; }
        public bool? FuenteActivaEmpleadosEmpresa { get; set; }

    }

    public class SenescytApiViewModel
    {
        public Externos.Logica.Senescyt.Modelos.Persona Senescyt { get; set; }
    }

    public class SuperBancosApiViewModel
    {
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosCedula { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosNatural { get; set; }
        public Externos.Logica.SuperBancos.Modelos.Resultado SuperBancosEmpresa { get; set; }
        [JsonIgnore]
        public bool CacheSuperBancosEmpresa { get; set; }
        [JsonIgnore]
        public bool CacheSuperBancosNatural { get; set; }
        [JsonIgnore]
        public bool CacheSuperBancosCedula { get; set; }
    }

    public class AntecedentesPenalesApiViewModel
    {
        public Externos.Logica.AntecedentesPenales.Modelos.Resultado Antecedentes { get; set; }
        [JsonIgnore]
        public bool CacheAntecedentes { get; set; }
        public bool? FuenteActivaAntecedentes { get; set; }
        public string? ErrorAntecedentes { get; set; }
    }

    public class FuerzasArmadasApiViewModel
    {
        public Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada FuerzasArmadas { get; set; }
        [JsonIgnore]
        public bool CacheFuerzaArmada { get; set; }
    }

    public class DeNoBajaApiViewModel
    {
        public Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia DeNoBaja { get; set; }
        [JsonIgnore]
        public bool CacheDeNoBaja { get; set; }
    }

    public class PrediosApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.Resultado PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.Resultado PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosCuencaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosSantoDomingoApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosRuminahuiApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosQuinindeApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosLatacungaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosMantaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosManta PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosAmbatoApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosIbarraApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosSanCristobalApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosDuranApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDuran PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosLagoAgrioApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosSantaRosaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosSucuaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSucua PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosSigSigApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosMejiaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMejia PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosMoronaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosMorona PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosTenaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosTena PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }
    public class PrediosCatamayoApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }
    public class PrediosLojaApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosLoja PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }
    public class PrediosSamborondonApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }
    public class PrediosDauleApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosDaule PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }
    public class PrediosCayambeApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }
    public class PrediosAzoguesApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosEsmeraldasApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class PrediosCotacachiApiViewModel
    {
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi PrediosRepresentante { get; set; }
        public Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi PrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool CachePredios { get; set; }
        [JsonIgnore]
        public bool CachePrediosEmpresa { get; set; }
        [JsonIgnore]
        public bool BusquedaEmpresa { get; set; }
    }

    public class FiscaliaDelitosApiViewModel
    {
        public Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito FiscaliaPersona { get; set; }
        public Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito FiscaliaEmpresa { get; set; }
        [JsonIgnore]
        public bool cacheFiscalia { get; set; }
        [JsonIgnore]
        public bool cacheFiscaliaEmpresa { get; set; }
    }

    public class SercopApiViewModel
    {
        public Externos.Logica.SERCOP.Modelos.ProveedorIncumplido Proveedor { get; set; }
        public List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria> ProveedorContraloria { get; set; }
        [JsonIgnore]
        public bool CacheSercop { get; set; }
    }

    public class AntApiViewModel
    {
        public Externos.Logica.ANT.Modelos.Licencia Licencia { get; set; }
        public List<Externos.Logica.ANT.Modelos.AutoHistorico> AutosHistorico { get; set; }
        [JsonIgnore]
        public bool CacheAnt { get; set; }
        public bool? FuenteActivaAnt { get; set; }
        public string? ErrorAnt { get; set; }
    }

    public class CalificacionApiViewModel
    {
        public CabeceraCalificacionApi Cabecera { get; set; }
        public List<DetalleCalificacionApi> DetalleCalificacion { get; set; }
    }

    public class CalificacionAyasaApiViewModel
    {
        public string Score { get; set; }
        public string TipoDecision { get; set; }
        public string DecisionModelo { get; set; }
    }

    public class CabeceraCalificacionApi
    {
        public bool Resultado { get; set; }
        public int TotalValidados { get; set; }
        public int TotalAprobados { get; set; }
        public int TotalRechazados { get; set; }
        public decimal Calificacion { get; set; }
        public double? Score { get; set; }
        public string CalificacionCliente { get; set; }
        public double? CapacidadPagoMensual { get; set; }
        public double? VentasEmpresa { get; set; }
        public double? PatrimonioEmpresa { get; set; }
        public string IngresoEstimado { get; set; }
        public double? GastoFinanciero { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Mensaje { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RangoIngreso { get; set; }
        public List<PrestamoApi> InformacionCapacidadPagoMensual { get; set; }
    }

    public class DetalleCalificacionApi
    {
        public string Politica { get; set; }
        public string ValorResultado { get; set; }
        public string ReferenciaMinima { get; set; }
        public bool ResultadoPolitica { get; set; }
    }

    public class PrestamoApi
    {
        public double? CapacidadPagoMensual { get; set; }
        public string Plazo { get; set; }
    }

    public class EvaluacionApi
    {
        public CalificacionApiViewModel EvaluacionBuro { get; set; }
        public CalificacionApiViewModel EvaluacionGeneral { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CalificacionAyasaApiViewModel EvaluacionBuroAyasa { get; set; }
    }

    public class BuroCreditoApi
    {
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta BuroCreditoAval { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado BuroCreditoEquifax { get; set; }
        public string MensajeError { get; set; }
    }

    public class BuroCreditoApiCovid360_0990981930001
    {
        public Externos.Logica.Equifax.Modelos.Resultado BuroCreditoEquifax { get; set; }
        public string MensajeError { get; set; }
    }

    public class BuroCreditoApi_0990981930001
    {
        public Externos.Logica.Equifax.Resultados._RespuestaBancoLitoral BuroCreditoEquifax { get; set; }
        public string MensajeError { get; set; }
    }

    public class BuroCreditoApiMicrofinanza_0990981930001
    {
        public Externos.Logica.Equifax.Resultados._RespuestaBancoLitoralMicrofinanza BuroCreditoEquifax { get; set; }
        public string MensajeError { get; set; }
    }

    public class BuroCreditoMetodoViewModel
    {
        public Externos.Logica.BuroCredito.Modelos.CreditoRespuesta BuroCredito { get; set; }
        public Externos.Logica.Equifax.Modelos.Resultado BuroCreditoEquifax { get; set; }
        public bool DatosCache { get; set; }
        public FuentesBuro Fuente { get; set; }
        public string MensajeError { get; set; }
    }

    public class PensionAlimenticiaApiViewModel
    {
        public Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia PensionAlimenticia { get; set; }
        public bool CachePension { get; set; }
        public bool? FuenteActivaPAlimenticia { get; set; }
        public string? ErrorPAlimenticia { get; set; }
    }

    public class UafeApiViewModel
    {
        public Externos.Logica.UAFE.Modelos.Resultado ONU { get; set; }
        public Externos.Logica.UAFE.Modelos.Resultado ONU2206 { get; set; }
        public Externos.Logica.UAFE.Modelos.ResultadoInterpol Interpol { get; set; }
        public Externos.Logica.UAFE.Modelos.ResultadoOfac OFAC { get; set; }
        [JsonIgnore]
        public bool accesoOnu { get; set; }
        [JsonIgnore]
        public bool accesoInterpol { get; set; }
        [JsonIgnore]
        public bool accesoOfac { get; set; }
        [JsonIgnore]
        public bool cacheOnu { get; set; }
        [JsonIgnore]
        public bool cacheOnu2206 { get; set; }
        [JsonIgnore]
        public bool cacheInterpol { get; set; }
        [JsonIgnore]
        public bool cacheOfac { get; set; }
        [JsonIgnore]
        public string mensajeErrorOnu { get; set; }
        [JsonIgnore]
        public string mensajeErrorOnu2206 { get; set; }
        [JsonIgnore]
        public string mensajeErrorInterpol { get; set; }
        [JsonIgnore]
        public string mensajeErrorOfac { get; set; }
    }

    public class CalificacionApiMetodoViewModel
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
        public List<DetalleCalificacionApiMetodoViewModel> DetalleCalificacion { get; set; }
    }

    public class DetalleCalificacionApiMetodoViewModel
    {
        public int IdPolitica { get; set; }
        public string Politica { get; set; }
        public Politicas Tipo { get; set; }
        public string ValorResultado { get; set; }
        public string ReferenciaMinima { get; set; }
        public string Valor { get; set; }
        public string Parametro { get; set; }
        public bool ResultadoPolitica { get; set; }
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

    public class OperacionHistoricaApiViewModel
    {
        public DateTime? FechaCorte { get; set; }
        public double? DemandaJudicial { get; set; }
        public double? CarteraCastigada { get; set; }
        public double? Vencido { get; set; }
    }

    public class InstitucionApiViewModel
    {
        public string Nombre { get; set; }
        public decimal Valor { get; set; }
        public string NmbVencimiento { get; set; }
    }

    public class HistoricoVencidoApiViewModel
    {
        public DateTime FechaCorte { get; set; }
        public string NmbVencimiento { get; set; }
        public decimal Valor { get; set; }
        public string SistemaFinanciero { get; set; }
    }

    public class ApiActualizarViewModel
    {
        public int IdPadre { get; set; }
        public string Identificacion { get; set; }
        //public FuentesApi[] Fuente { get; set; }
        //public bool Evaluar { get; set; }
        public bool ConsultarBuro { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public TipoBuroModelo? ModeloBuro { get; set; }
    }

    public class RespuestaActualizacionApiViewModel
    {
        public int IdPadre { get; set; }
        public BuroCreditoApiCovid360_0990981930001 BuroCredito { get; set; }
        public BuroCreditoApi_0990981930001 BuroCreditoAutomotriz { get; set; }
        public BuroCreditoApiMicrofinanza_0990981930001 BuroCreditoMicrofinanza { get; set; }
    }
    #region Clientes
    public class ApiClienteAMCViewModel
    {
        public string Identificacion { get; set; }
        public string Referencia { get; set; }
        public int Periodos { get; set; }
        public Dictionary<FuentesApiAmc, string> Data { get; set; }
    }

    #region Bco. Pichincha
    public class RespuestaApiViewModel_1790010937001
    {
        public SriApiViewModel_1790010937001 Sri { get; set; }
        public IessApiViewModel_1790010937001 Iess { get; set; }
        public AntApiViewModel_1790010937001 Ant { get; set; }
        public PensionAlimenticiaApiViewModel_1790010937001 PensionAlimenticia { get; set; }
        public PredioApiViewModel_1790010937001 Predios { get; set; }
    }

    public class SriApiViewModel_1790010937001
    {
        public string RUC { get; set; }
        public string RazonSocial { get; set; }
        public string NombreComercial { get; set; }
        public string Estado { get; set; }
        public string EstadoContribuyente { get; set; }
        public string Actividad { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaCese { get; set; }
        public DateTime? FechaReinicio { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public string RepresentanteLegal { get; set; }
        public string Tipo { get; set; }
        public string Clasificacion { get; set; }
        public IDictionary<string, Externos.Logica.SRi.Modelos.Deuda> Deudas { get; set; }
        public IList<Externos.Logica.SRi.Modelos.Renta> Rentas { get; set; }
        public IList<Externos.Logica.SRi.Modelos.Anexo> Anexos { get; set; }
        public List<Externos.Logica.SRi.Modelos.Representante> Representantes { get; set; }
        public int UltimoPeriodoImpuestos { get; set; }
        public double? UltimoValorRenta { get; set; }
        public double? PenultimoValorRenta { get; set; }
        public double? AntepenultimoValorRenta { get; set; }
    }

    public class IessApiViewModel_1790010937001
    {
        public IessApiViewModel_1790010937001()
        {
            Empresas = new List<EmpresaAfiliadoIessApiViewModel_1790010937001>();
        }
        public string TipoAfiliacion { get; set; }
        public List<EmpresaAfiliadoIessApiViewModel_1790010937001> Empresas { get; set; }
    }

    public class EmpresaAfiliadoIessApiViewModel_1790010937001
    {
        public string RucEmpresa { get; set; }
        public string NombreEmpresa { get; set; }
    }

    public class AntApiViewModel_1790010937001
    {
        public AntApiViewModel_1790010937001()
        {
            Autos = new List<AutoAntApiViewModel_1790010937001>();
            Multas = new List<MultaAntApiViewModel_1790010937001>();
        }

        public double? Puntos { get; set; }
        public string PuntosOriginal { get; set; }
        public List<AutoAntApiViewModel_1790010937001> Autos { get; set; }
        public List<MultaAntApiViewModel_1790010937001> Multas { get; set; }
    }

    public class MultaAntApiViewModel_1790010937001
    {
        public string Placa { get; set; }
        public string Citacion { get; set; }
        public double Puntos { get; set; }
        public DateTime FechaRegistro { get; set; }
        public double ValorEmision { get; set; }
        public double ValorInteres { get; set; }
        public double ValorTotal { get; set; }
        public double Saldo { get; set; }
    }

    public class AutoAntApiViewModel_1790010937001
    {
        public string Placa { get; set; }
        public string TotalMatricula { get; set; }
        public DateTime? FechaUltimaMatricula { get; set; }
    }

    public class PensionAlimenticiaApiViewModel_1790010937001
    {
        public PensionAlimenticiaApiViewModel_1790010937001()
        {
            Procesos = new List<ProcesoPensionAlimenticiaApiViewModel_1790010937001>();
        }

        public List<ProcesoPensionAlimenticiaApiViewModel_1790010937001> Procesos { get; set; }
    }

    public class ProcesoPensionAlimenticiaApiViewModel_1790010937001
    {
        public string TipoPension { get; set; }
        public string PensionActualOriginal { get; set; }
        public double? PensionActual { get; set; }
        public double? TotalDeuda { get; set; }
        public double? TotalPagado { get; set; }
        public bool AlDia { get; set; }
    }

    public class PredioApiViewModel_1790010937001
    {
        public PredioApiViewModel_1790010937001()
        {
            Detalle = new List<DetallePredioApiViewModel_1790010937001>();
        }
        public List<DetallePredioApiViewModel_1790010937001> Detalle { get; set; }
    }

    public class DetallePredioApiViewModel_1790010937001
    {
        public string Municipio { get; set; }
        public string Direccion { get; set; }
        public double Valor { get; set; }
        public string Estado { get; set; }
        public double? TicValor { get; set; }
        public DateTime? Fecha { get; set; }
        public double MontoAbonos { get; set; }
        public string RNum { get; set; }
        public double Total { get; set; }
    }
    #endregion Bco. Pichincha
    #endregion Clientes
}
