// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Web.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Data;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;
using Web.Areas.Consultas.Models;
using Microsoft.AspNetCore.Identity;
using Dominio.Entidades.Identidad;
using Persistencia.Repositorios.Balance;
using Dominio.Entidades.Balances;
using Persistencia.Repositorios.Identidad;
using Infraestructura.Servicios;
using Dominio.Tipos;
using Externos.Logica.Garancheck.Modelos;
using Externos.Logica.IESS.Modelos;
using Externos.Logica.SRi.Modelos;
using Web.Areas.Historiales.Models;
using Microsoft.EntityFrameworkCore;
using Externos.Logica.Balances.Modelos;
using Externos.Logica.FJudicial.Modelos;
using Externos.Logica.AntecedentesPenales.Modelos;
using Externos.Logica.PensionesAlimenticias.Modelos;
using static Externos.Logica.ANT.Modelos.Licencia;

namespace Web.Controllers.API.Clientes.Andalucia
{
    [Route("api/Clientes/Andalucia/Consulta")]
    [ApiController]
    public class ConsultaController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly Externos.Logica.SRi.Controlador _sri;
        private readonly Externos.Logica.Balances.Controlador _balances;
        private readonly Externos.Logica.IESS.Controlador _iess;
        private readonly Externos.Logica.FJudicial.Controlador _fjudicial;
        private readonly Externos.Logica.ANT.Controlador _ant;
        private readonly Externos.Logica.PensionesAlimenticias.Controlador _pension;
        private readonly Externos.Logica.Garancheck.Controlador _garancheck;
        private readonly Externos.Logica.SERCOP.Controlador _sercop;
        private readonly Externos.Logica.BuroCredito.Controlador _burocredito;
        private readonly Externos.Logica.Equifax.Controlador _buroCreditoEquifax;
        private readonly Externos.Logica.SuperBancos.Controlador _superBancos;
        private readonly Externos.Logica.AntecedentesPenales.Controlador _antecedentes;
        private readonly Externos.Logica.PredioMunicipio.Controlador _predios;
        private readonly Externos.Logica.FiscaliaDelitos.Controlador _fiscaliaDelitos;
        private readonly Externos.Logica.UAFE.Controlador _uafe;
        private readonly IHttpContextAccessor _httpContext;
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detallesHistorial;
        private readonly IConsultaService _consulta;
        private readonly IUsuarios _usuarios;
        private readonly IPoliticas _politicas;
        private readonly ICalificaciones _calificaciones;
        private readonly IDetalleCalificaciones _detalleCalificaciones;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly IAccesos _accesos;
        private readonly ICredencialesBuro _credencialesBuro;
        private readonly IParametrosClientesHistoriales _parametrosClientesHistoriales;
        private readonly IReportesConsolidados _reporteConsolidado;
        private bool _cache = false;

        public ConsultaController(IConfiguration configuration, ILoggerFactory loggerFactory,
            Externos.Logica.SRi.Controlador sri,
            Externos.Logica.Balances.Controlador balances,
            Externos.Logica.IESS.Controlador iess,
            Externos.Logica.FJudicial.Controlador fjudicial,
            Externos.Logica.ANT.Controlador ant,
            Externos.Logica.PensionesAlimenticias.Controlador pension,
            Externos.Logica.Garancheck.Controlador garancheck,
            Externos.Logica.SERCOP.Controlador sercop,
            Externos.Logica.BuroCredito.Controlador burocredito,
            Externos.Logica.Equifax.Controlador buroCreditoEquifax,
            Externos.Logica.SuperBancos.Controlador superBancos,
            Externos.Logica.AntecedentesPenales.Controlador antecedentes,
            Externos.Logica.PredioMunicipio.Controlador predios,
            Externos.Logica.FiscaliaDelitos.Controlador fiscaliaDelitos,
            Externos.Logica.UAFE.Controlador uafe,
            SignInManager<Usuario> signInManager,
            UserManager<Usuario> userManager,
            IHistoriales historiales,
            IDetallesHistorial detallehistoriales,
            IHttpContextAccessor httpContext,
            IConsultaService consulta,
            IPoliticas politicas,
            ICalificaciones calificaciones,
            IDetalleCalificaciones detalleCalificaciones,
            IPlanesEvaluaciones planesEvaluaciones,
            IAccesos accesos,
            IUsuarios usuarios,
            ICredencialesBuro credencialesBuro,
            IReportesConsolidados reportesConsolidados,
            IParametrosClientesHistoriales parametrosClientesHistoriales)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _sri = sri;
            _balances = balances;
            _iess = iess;
            _fjudicial = fjudicial;
            _ant = ant;
            _pension = pension;
            _garancheck = garancheck;
            _sercop = sercop;
            _burocredito = burocredito;
            _buroCreditoEquifax = buroCreditoEquifax;
            _superBancos = superBancos;
            _antecedentes = antecedentes;
            _predios = predios;
            _uafe = uafe;
            _fiscaliaDelitos = fiscaliaDelitos;
            _httpContext = httpContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _historiales = historiales;
            _detallesHistorial = detallehistoriales;
            _consulta = consulta;
            _usuarios = usuarios;
            _calificaciones = calificaciones;
            _politicas = politicas;
            _detalleCalificaciones = detalleCalificaciones;
            _planesEvaluaciones = planesEvaluaciones;
            _accesos = accesos;
            _credencialesBuro = credencialesBuro;
            _parametrosClientesHistoriales = parametrosClientesHistoriales;
            _reporteConsolidado = reportesConsolidados;
            _cache = _configuration.GetSection("AppSettings:Consultas:Cache").Get<bool>();
        }

        [HttpPost("ObtenerInformacion")]
        public async Task<IActionResult> ObtenerInformacion(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                modelo.Periodos = 1;
                var usuario = string.Empty;
                var clave = string.Empty;
                Usuario user = null;
                var fuenteSocietario = false;
                var fuenteIess = false;
                var fuenteSercop = false;
                var fuenteJudicial = false;
                var fuenteSenescyt = false;
                var fuenteCivil = false;
                var fuenteSri = false;
                var fuenteAnt = false;
                var fuentePension = false;
                var fuenteSuperBancos = false;
                var fuenteAntecedentes = false;
                var fuenteFuerzasArmadas = false;
                var fuenteDeNoBaja = false;
                var fuentePredios = false;
                var fuentePrediosCuenca = false;
                var fuentePrediosStoDomingo = false;
                var fuentePrediosRuminahui = false;
                var fuentePrediosQuininde = false;
                var fuentePrediosLatacunga = false;
                var fuentePrediosManta = false;
                var fuentePrediosAmbato = false;
                var fuentePrediosIbarra = false;
                var fuentePrediosSanCristobal = false;
                var fuentePrediosDuran = false;
                var fuentePrediosLagoAgrio = false;
                var fuentePrediosSantaRosa = false;
                var fuentePrediosSucua = false;
                var fuentePrediosSigSig = false;
                var fuentePrediosMejia = false;
                var fuentePrediosMorona = false;
                var fuentePrediosTena = false;
                var fuentePrediosCatamayo = false;
                var fuentePrediosLoja = false;
                var fuentePrediosSamborondon = false;
                var fuentePrediosDaule = false;
                var fuentePrediosCayambe = false;
                var fuentePrediosAzogues = false;
                var fuentePrediosEsmeraldas = false;
                var fuentePrediosCotacachi = false;
                var fuenteFiscaliaDelitos = false;
                var fuenteUafe = false;
                var fuenteImpedimento = false;
                var fuenteTodas = false;
                var fuenteSriBasico = false;
                var fuenteSriHistorico = false;
                var fuenteCivilBasico = false;
                var fuenteCivilHistorico = false;
                var apiSri = new SriApiViewModel();
                var apiCivil = new CivilApiMetodoViewModel();
                var apiSocietario = new BalancesApiMetodoViewModel();
                var apiIess = new IessApiMetodoViewModel();
                var apiSenescyt = new SenescytApiViewModel();
                var apiLegal = new JudicialApiViewModel();
                var apiImpedimento = new ImpedimentoMetodoApiViewModel();
                var apiSercop = new SercopApiViewModel();
                var apiAnt = new AntApiViewModel();
                var apiPension = new PensionAlimenticiaApiViewModel();
                var apiSuperBancos = new SuperBancosApiViewModel();
                var apiAntecedentes = new AntecedentesPenalesApiViewModel();
                var apiFuerzasArmadas = new FuerzasArmadasApiViewModel();
                var apiDeNoBaja = new DeNoBajaApiViewModel();
                var apiPredios = new PrediosApiViewModel();
                var apiPrediosCuenca = new PrediosCuencaApiViewModel();
                var apiPrediosStoDomingo = new PrediosSantoDomingoApiViewModel();
                var apiPrediosRuminahui = new PrediosRuminahuiApiViewModel();
                var apiPrediosQuininde = new PrediosQuinindeApiViewModel();
                var apiPrediosLatacunga = new PrediosLatacungaApiViewModel();
                var apiPrediosManta = new PrediosMantaApiViewModel();
                var apiPrediosAmbato = new PrediosAmbatoApiViewModel();
                var apiPrediosIbarra = new PrediosIbarraApiViewModel();
                var apiPrediosSanCristobal = new PrediosSanCristobalApiViewModel();
                var apiPrediosDuran = new PrediosDuranApiViewModel();
                var apiPrediosLagoAgrio = new PrediosLagoAgrioApiViewModel();
                var apiPrediosSantaRosa = new PrediosSantaRosaApiViewModel();
                var apiPrediosSucua = new PrediosSucuaApiViewModel();
                var apiPrediosSigSig = new PrediosSigSigApiViewModel();
                var apiPrediosMejia = new PrediosMejiaApiViewModel();
                var apiPrediosMorona = new PrediosMoronaApiViewModel();
                var apiPrediosTena = new PrediosTenaApiViewModel();
                var apiPrediosCatamayo = new PrediosCatamayoApiViewModel();
                var apiPrediosLoja = new PrediosLojaApiViewModel();
                var apiPrediosSamborondon = new PrediosSamborondonApiViewModel();
                var apiPrediosDaule = new PrediosDauleApiViewModel();
                var apiPrediosCayambe = new PrediosCayambeApiViewModel();
                var apiPrediosAzogues = new PrediosAzoguesApiViewModel();
                var apiPrediosEsmeraldas = new PrediosEsmeraldasApiViewModel();
                var apiPrediosCotacachi = new PrediosCotacachiApiViewModel();
                var apiFiscaliaDelitos = new FiscaliaDelitosApiViewModel();
                var apiUafe = new UafeApiViewModel();
                var apiBuroCredito = new BuroCreditoMetodoViewModel();
                var apiEvaluacion = new List<CalificacionApiMetodoViewModel>();
                var fuentes = new[] { FuentesApi.TodasFuentes, FuentesApi.Societario, FuentesApi.Iess, FuentesApi.Sercop,
                                      FuentesApi.Legal,FuentesApi.Senescyt, FuentesApi.Civil,FuentesApi.Sri,
                                      FuentesApi.Ant, FuentesApi.PensionAlimenticia, FuentesApi.SuperBancos, FuentesApi.AntecedentesPenales, FuentesApi.FuerzasArmadas,FuentesApi.DeNoBaja,
                                      FuentesApi.Predios, FuentesApi.FiscaliaDelitos, FuentesApi.PrediosCuenca, FuentesApi.SriBasico,
                                      FuentesApi.SriHistorico, FuentesApi.CivilBasico, FuentesApi.CivilHistorico, FuentesApi.PrediosStoDomingo,
                                      FuentesApi.PrediosRuminahui, FuentesApi.PrediosQuininde, FuentesApi.PrediosLatacunga, FuentesApi.PrediosManta, FuentesApi.PrediosAmbato,
                                      FuentesApi.PrediosIbarra, FuentesApi.PrediosSanCristobal, FuentesApi.PrediosDuran, FuentesApi.PrediosLagoAgrio,
                                      FuentesApi.PrediosSantaRosa, FuentesApi.PrediosSucua, FuentesApi.PrediosSigSig, FuentesApi.PrediosMejia, FuentesApi.PrediosMorona,
                                      FuentesApi.PrediosTena,FuentesApi.PrediosCatamayo,FuentesApi.PrediosLoja, FuentesApi.PrediosSamborondon, FuentesApi.PrediosDaule,
                                      FuentesApi.PrediosCayambe, FuentesApi.PrediosAzogues, FuentesApi.PrediosEsmeraldas, FuentesApi.PrediosCotacachi, FuentesApi.Uafe, FuentesApi.Impedimento };
                CalificacionApiViewModel evaluacion = null;
                DetalleCalificacionApi detalleCalificacion = null;
                //PrestamoApi prestamo = null;
                //var porcentajeEmpresa = 0.00;
                var clasificacionEmpresa = string.Empty;
                var calificacionCliente = string.Empty;
                var fuentesSri = new[] { FuentesApi.Sri, FuentesApi.SriBasico, FuentesApi.SriHistorico };
                var fuentesCivil = new[] { FuentesApi.Civil, FuentesApi.CivilBasico, FuentesApi.CivilHistorico };
                var fuentesPredios = new[] { FuentesApi.Predios, FuentesApi.PrediosCuenca, FuentesApi.PrediosStoDomingo, FuentesApi.PrediosRuminahui, FuentesApi.PrediosQuininde,
                                             FuentesApi.PrediosLatacunga, FuentesApi.PrediosManta, FuentesApi.PrediosAmbato, FuentesApi.PrediosIbarra, FuentesApi.PrediosSanCristobal,
                                             FuentesApi.PrediosDuran, FuentesApi.PrediosLagoAgrio, FuentesApi.PrediosSantaRosa, FuentesApi.PrediosSucua, FuentesApi.PrediosSigSig,
                                             FuentesApi.PrediosMejia, FuentesApi.PrediosMorona, FuentesApi.PrediosTena, FuentesApi.PrediosCatamayo, FuentesApi.PrediosLoja,
                                             FuentesApi.PrediosSamborondon, FuentesApi.PrediosDaule, FuentesApi.PrediosCayambe, FuentesApi.PrediosAzogues, FuentesApi.PrediosEsmeraldas,
                                             FuentesApi.PrediosCotacachi};

                try
                {
                    var request = HttpContext.Request;
                    var authHeader = request.Headers["Authorization"].ToString();
                    if (authHeader != null)
                    {
                        var authHeaderVal = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);
                        if (authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) && authHeaderVal.Parameter != null)
                        {
                            var encoding = System.Text.Encoding.GetEncoding("iso-8859-1");
                            var credentials = encoding.GetString(Convert.FromBase64String(authHeaderVal.Parameter));
                            var separator = credentials.IndexOf(':');
                            usuario = credentials.Substring(0, separator);
                            clave = credentials.Substring(separator + 1);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
                        throw new UnauthorizedAccessException();

                    user = await _usuarios.FirstOrDefaultAsync(m => m, m => m.UserName == usuario, null, m => m.Include(m => m.Empresa.PlanesEmpresas).Include(m => m.Empresa.PlanesBuroCredito).Include(m => m.Empresa.PlanesEvaluaciones));

                    if (user == null)
                        throw new Exception("El usuario no se encuentra registrado.");

                    if (user.Estado != Dominio.Tipos.EstadosUsuarios.Activo)
                        throw new Exception("Cuenta de usuario inactiva.");

                    var passwordVerification = new PasswordHasher<Usuario>().VerifyHashedPassword(user, user.PasswordHash, clave);
                    if (passwordVerification == PasswordVerificationResult.Failed)
                        throw new Exception("La clave ingresada es incorrecta");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    return Unauthorized(new { mensaje = ex.Message });
                }

                var usuarioActual = user;
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión");

                if (usuarioActual.Empresa.Estado != EstadosEmpresas.Activo)
                    throw new Exception("La empresa asociada al usuario no está activa");

#if !DEBUG
                if (usuarioActual.Empresa.Identificacion != Dominio.Constantes.Clientes.Cliente1790325083001)
                    return Unauthorized();
#endif

                if (modelo == null)
                    throw new Exception("No se han ingresado los datos de la consulta.");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("La identificación ingresada no es válida.");

                if (modelo.Fuente != null && fuentes.Intersect(modelo.Fuente).Count() != modelo.Fuente.Length)
                    throw new Exception("Uno o varios números no coinciden con ninguna Fuente Existente.");

                if (modelo.Fuente != null && fuentesSri.Intersect(modelo.Fuente).Count() >= 2)
                    throw new Exception("Solo puede consultar una fuente para el SRI: 2, 201 o 202.");

                if (modelo.Fuente != null && fuentesCivil.Intersect(modelo.Fuente).Count() >= 2)
                    throw new Exception("Solo puede consultar una fuente para Civil: 3, 301 o 302.");

                if (modelo.Fuente == null)
                    modelo.Fuente = new FuentesApi[] { FuentesApi.TodasFuentes };

                if (modelo.Fuente != null && modelo.Fuente.Intersect(fuentesPredios).Count() > 1)
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.FuentesIncorrectas,
                        mensaje = "No se puede consultar más de un Predio"
                    });

                var identificacionOriginal = modelo.Identificacion?.Trim();
                if (modelo.SegmentoCartera == Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Desconocido && modelo.Evaluar)
                    throw new Exception("Se debe definir un segmento de cartera para poder realizar la evaluación");

                if (modelo.SegmentoCartera == Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Consumo && (ValidacionViewModel.ValidarRuc(identificacionOriginal) && (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))))
                    throw new Exception("No se puede consultar RUCS con la opción Consumo");

                var planesVigentes = usuarioActual.Empresa.PlanesEmpresas.Where(m => m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo).ToList();
                if (!planesVigentes.Any())
                    throw new Exception("No es posible realizar esta consulta ya que no tiene planes activos vigentes.");

                var idPlan = 0;
                var tipoIdentificacion = string.Empty;
                var parametros = string.Empty;
                if (ValidacionViewModel.ValidarCedula(identificacionOriginal))
                {
                    tipoIdentificacion = Dominio.Constantes.General.Cedula;
                    var planEmpresaCedula = usuarioActual.Empresa.PlanesEmpresas.FirstOrDefault(m => (m.NumeroConsultasCedula > 0 || (m.NumeroConsultas.HasValue && m.NumeroConsultas.Value > 0)) && m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo);
                    if (planEmpresaCedula == null)
                        throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo para cédulas.");

                    if (planEmpresaCedula.BloquearConsultas)
                    {
                        if (planEmpresaCedula.PlanDemostracion)
                        {
                            if (planEmpresaCedula.NumeroConsultas.HasValue && planEmpresaCedula.NumeroConsultas.Value > 0)
                            {
                                var historialUnificado = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaCedula.Id);
                                if (historialUnificado >= planEmpresaCedula.NumeroConsultas)
                                    throw new Exception($"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas ({planEmpresaCedula.NumeroConsultas}) en su plan de demostración.");
                            }
                            else
                                throw new Exception("El plan contratado no tiene definido un número de consultas");
                        }
                        else
                        {
                            if (planEmpresaCedula.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Separado)
                            {
                                var fechaActual = DateTime.Today;
                                var historialCedulas = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaCedula.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year && m.TipoIdentificacion == Dominio.Constantes.General.Cedula);
                                if (historialCedulas >= planEmpresaCedula.NumeroConsultasCedula)
                                    throw new Exception($"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas para cédulas ({planEmpresaCedula.NumeroConsultasCedula}) en su plan.");
                            }
                            else if (planEmpresaCedula.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado)
                            {
                                if (planEmpresaCedula.NumeroConsultas.HasValue && planEmpresaCedula.NumeroConsultas.Value > 0)
                                {
                                    var fechaActual = DateTime.Today;
                                    var historialUnificado = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaCedula.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year);
                                    if (historialUnificado >= planEmpresaCedula.NumeroConsultas)
                                        throw new Exception($"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas ({planEmpresaCedula.NumeroConsultas}) en su plan.");
                                }
                                else
                                    throw new Exception("El plan contratado no tiene definido un número de consultas");
                            }
                            else
                                throw new Exception("El plan contratado no tiene definido un tipo de consultas");
                        }
                    }
                    idPlan = planEmpresaCedula.Id;
                }
                else if (ValidacionViewModel.ValidarRuc(identificacionOriginal) || ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                {
                    if (ValidacionViewModel.ValidarRuc(identificacionOriginal))
                        tipoIdentificacion = Dominio.Constantes.General.RucNatural;

                    if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal))
                        tipoIdentificacion = Dominio.Constantes.General.RucJuridico;

                    if (ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                        tipoIdentificacion = Dominio.Constantes.General.SectorPublico;

                    var planEmpresaRucs = usuarioActual.Empresa.PlanesEmpresas.FirstOrDefault(m => (m.NumeroConsultasRuc > 0 || (m.NumeroConsultas.HasValue && m.NumeroConsultas.Value > 0)) && m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo);
                    if (planEmpresaRucs == null)
                        throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo para RUCs naturales o jurídicos.");

                    if (planEmpresaRucs.BloquearConsultas)
                    {
                        if (planEmpresaRucs.PlanDemostracion)
                        {
                            if (planEmpresaRucs.NumeroConsultas.HasValue && planEmpresaRucs.NumeroConsultas.Value > 0)
                            {
                                var historialUnificado = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaRucs.Id);
                                if (historialUnificado >= planEmpresaRucs.NumeroConsultas)
                                    throw new Exception($"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas ({planEmpresaRucs.NumeroConsultas}) en su plan de demostración.");
                            }
                            else
                                throw new Exception("El plan contratado no tiene definido un número de consultas");
                        }
                        else
                        {
                            if (planEmpresaRucs.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Separado)
                            {
                                var fechaActual = DateTime.Today;
                                var historialCedulas = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaRucs.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year && (m.TipoIdentificacion == Dominio.Constantes.General.RucNatural || m.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || m.TipoIdentificacion == Dominio.Constantes.General.SectorPublico));
                                if (historialCedulas >= planEmpresaRucs.NumeroConsultasRuc)
                                    throw new Exception($"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas para RUCs ({planEmpresaRucs.NumeroConsultasRuc}) en su plan.");
                            }
                            else if (planEmpresaRucs.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado)
                            {
                                if (planEmpresaRucs.NumeroConsultas.HasValue && planEmpresaRucs.NumeroConsultas.Value > 0)
                                {
                                    var fechaActual = DateTime.Today;
                                    var historialUnificado = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaRucs.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year);
                                    if (historialUnificado >= planEmpresaRucs.NumeroConsultas)
                                        throw new Exception($"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas ({planEmpresaRucs.NumeroConsultas}) en su plan.");
                                }
                                else
                                    throw new Exception("El plan contratado no tiene definido un número de consultas");
                            }
                            else
                                throw new Exception("El plan contratado no tiene definido un tipo de consultas");
                        }
                    }
                    idPlan = planEmpresaRucs.Id;
                }
                else
                    throw new Exception("La identificación ingresada no corresponde ni a cédulas ni a RUCs");

                if (idPlan == 0)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene planes vigentes.");

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation($"Procesando historial de usuario: {user.Id}. Identificación: {identificacionOriginal}. Periodo: {modelo.Periodos}. IP: {ip}.");

                if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                {
                    parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { modelo.Periodos } });
                    if (modelo.Periodos == 1)
                    {
                        var infoPeriodos = _configuration.GetSection("AppSettings:PeriodosDinamicos").Get<PeriodosDinamicosViewModel>();
                        if (infoPeriodos != null)
                        {
                            var ultimosPeriodos = infoPeriodos.Periodos.Select(m => m.Valor).ToList();
                            parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = ultimosPeriodos });
                        }
                    }
                }
                else
                {
                    parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { 0 } });
                    modelo.Periodos = 0;
                }

                _logger.LogInformation("Registrando historial de usuarios en base de datos");
                var idHistorial = await _historiales.GuardarHistorialAsync(new Historial()
                {
                    IdUsuario = user.Id,
                    DireccionIp = ip?.Trim().ToUpper(),
                    Identificacion = modelo.Identificacion?.Trim().ToUpper(),
                    Periodo = modelo.Periodos,
                    Fecha = DateTime.Now,
                    TipoConsulta = Dominio.Tipos.Consultas.Api,
                    ParametrosBusqueda = parametros,
                    IdPlanEmpresa = idPlan,
                    TipoIdentificacion = tipoIdentificacion
                });
                _logger.LogInformation($"Registro de historial exitoso. Id Historial: {idHistorial}");
                //Nueva tabla historial
                var idReporteConsolidado = await _reporteConsolidado.GuardarReporteConsolidadoAsync(new Dominio.Entidades.Balances.ReporteConsolidado()
                {
                    IdUsuario = user.Id,
                    DireccionIp = ip?.Trim().ToUpper(),
                    Identificacion = modelo.Identificacion?.Trim().ToUpper(),
                    TipoIdentificacion = tipoIdentificacion,
                    TipoConsulta = Dominio.Tipos.Consultas.Api,
                    ParametrosBusqueda = parametros,
                    Fecha = DateTime.Now,
                    NombreUsuario = usuarioActual.NombreCompleto,
                    IdEmpresa = usuarioActual.IdEmpresa,
                    NombreEmpresa = usuarioActual.Empresa.RazonSocial,
                    IdentificacionEmpresa = usuarioActual.Empresa.Identificacion,
                    HistorialId = idHistorial
                });
                _logger.LogInformation($"Registro de Reporte consolidado exitoso. Id Historial: {idReporteConsolidado}");

                try
                {
                    if (modelo.SegmentoCartera != Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Desconocido)
                        await _parametrosClientesHistoriales.GuardarParametroClienteHistorialAsync(new ParametroClienteHistorial()
                        {
                            IdHistorial = idHistorial,
                            Valor = ((short)modelo.SegmentoCartera).ToString(),
                            Parametro = Dominio.Tipos.ParametrosClientes.SegmentoCartera,
                            FechaCreacion = DateTime.Now,
                            UsuarioCreacion = usuarioActual.Id
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                fuenteTodas = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.TodasFuentes);
                fuenteSocietario = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Societario);
                fuenteIess = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Iess);
                fuenteSercop = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Sercop);
                fuenteJudicial = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Legal);
                fuenteSenescyt = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Senescyt);
                fuenteCivil = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Civil);
                fuenteSri = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Sri);
                fuenteAnt = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Ant);
                fuentePension = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PensionAlimenticia);
                fuenteSuperBancos = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.SuperBancos);
                fuenteAntecedentes = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.AntecedentesPenales);
                fuenteFuerzasArmadas = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.FuerzasArmadas);
                fuenteDeNoBaja = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.DeNoBaja);
                fuentePredios = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Predios);
                //fuenteFiscaliaDelitos = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.FiscaliaDelitos);
                fuentePrediosCuenca = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCuenca);
                fuentePrediosStoDomingo = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosStoDomingo);
                fuentePrediosRuminahui = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosRuminahui);
                fuentePrediosQuininde = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosQuininde);
                fuentePrediosLatacunga = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosLatacunga);
                fuentePrediosManta = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosManta);
                fuentePrediosAmbato = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosAmbato);
                fuentePrediosIbarra = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosIbarra);
                fuentePrediosSanCristobal = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSanCristobal);
                fuentePrediosDuran = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosDuran);
                fuentePrediosLagoAgrio = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosLagoAgrio);
                fuentePrediosSantaRosa = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSantaRosa);
                fuentePrediosSucua = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSucua);
                fuentePrediosSigSig = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSigSig);
                fuentePrediosMejia = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosMejia);
                fuentePrediosMorona = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosMorona);
                fuentePrediosTena = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosTena);
                fuentePrediosCatamayo = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCatamayo);
                fuentePrediosLoja = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosLoja);
                fuentePrediosSamborondon = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSamborondon);
                //fuentePrediosDaule = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosDaule);
                fuentePrediosCayambe = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCayambe);
                fuentePrediosAzogues = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosAzogues);
                fuentePrediosEsmeraldas = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosEsmeraldas);
                fuentePrediosCotacachi = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCotacachi);
                fuenteSriBasico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.SriBasico);
                fuenteSriHistorico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.SriHistorico);
                fuenteCivilBasico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.CivilBasico);
                fuenteCivilHistorico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.CivilHistorico);
                fuenteUafe = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Uafe);
                fuenteImpedimento = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Impedimento);

                var datos = new RespuestaApiViewModel();

                if (fuenteTodas || fuenteSri)
                {
                    apiSri = await ObtenerReporteSRI(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Sri = apiSri != null && apiSri.Empresa != null ? new SriApiViewModel()
                    {
                        Empresa = apiSri.Empresa,
                        Contactos = apiSri.Contactos,
                        EmpresasSimilares = apiSri.EmpresasSimilares,
                        CatastroFantasma = apiSri.CatastroFantasma
                    } : null;
                }

                if (fuenteSriBasico)
                {
                    apiSri = await ObtenerReporteSRIBasico(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Sri = apiSri != null && apiSri.Empresa != null ? new SriApiViewModel()
                    {
                        Empresa = apiSri.Empresa,
                        Contactos = apiSri.Contactos
                    } : null;
                }

                if (fuenteSriHistorico)
                {
                    apiSri = await ObtenerReporteSRIHistorico(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Sri = apiSri != null && apiSri.Empresa != null ? new SriApiViewModel()
                    {
                        Empresa = apiSri.Empresa,
                        Contactos = apiSri.Contactos
                    } : null;
                }

                if (fuenteTodas || fuenteCivil)
                {
                    apiCivil = await ObtenerReporteCivil(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Civil = apiCivil != null && (apiCivil.General != null || apiCivil.Personal != null) ? new CivilApiViewModel()
                    {
                        General = apiCivil.General,
                        Personal = apiCivil.Personal,
                        Contactos = apiCivil.Contactos,
                        Familiares = apiCivil.Familiares
                    } : null;
                }

                if (fuenteCivilBasico)
                {
                    apiCivil = await ObtenerReporteCivilBasico(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Civil = apiCivil != null && apiCivil.Contactos != null ? new CivilApiViewModel()
                    {
                        Contactos = apiCivil.Contactos
                    } : null;
                }

                if (fuenteCivilHistorico)
                {
                    apiCivil = await ObtenerReporteCivilHistorico(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Civil = apiCivil != null && apiCivil.Contactos != null ? new CivilApiViewModel()
                    {
                        General = apiCivil.General,
                        Contactos = apiCivil.Contactos
                    } : null;
                }

                try
                {
                    if (apiCivil != null && apiCivil.RegistroCivil != null)
                    {
                        if (datos != null && datos.Civil != null)
                        {
                            #region General
                            if (datos.Civil.General != null)
                            {
                                datos.Civil.General.EstadoCivil = apiCivil.RegistroCivil.EstadoCivil;
                                datos.Civil.General.Profesion = apiCivil.RegistroCivil.Profesion;
                                datos.Civil.General.Instruccion = apiCivil.RegistroCivil.Instruccion;
                                datos.Civil.General.Nacionalidad = apiCivil.RegistroCivil.Nacionalidad;
                                var direcciones = !string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio) ? apiCivil.RegistroCivil.LugarDomicilio.Split('/').ToList() : new List<string>();
                                if (direcciones.Any())
                                {
                                    if (direcciones.Count >= 1)
                                        datos.Civil.General.Provincia = direcciones[0];

                                    if (direcciones.Count >= 2)
                                        datos.Civil.General.Canton = direcciones[1];

                                    if (direcciones.Count >= 3)
                                        datos.Civil.General.Parroquia = direcciones[2];
                                }

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                    datos.Civil.General.NumeroCasa = apiCivil.RegistroCivil.NumeracionDomicilio;

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio?.Trim()))
                                    datos.Civil.General.Domicilio = apiCivil.RegistroCivil.LugarDomicilio;

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                    datos.Civil.General.Calle = apiCivil.RegistroCivil.CalleDomicilio;

                                if (apiCivil.RegistroCivil.FechaCedulacion != default)
                                    datos.Civil.General.FechaCedulacion = apiCivil.RegistroCivil.FechaCedulacion;
                            }
                            else
                            {
                                var general = new Externos.Logica.Garancheck.Modelos.Persona();
                                general.TipoIdentificacion = 'C';
                                general.Identificacion = apiCivil.RegistroCivil.Cedula;
                                general.FechaNacimiento = apiCivil.RegistroCivil.FechaNacimiento;
                                general.EstadoCivil = apiCivil.RegistroCivil.EstadoCivil;
                                general.Genero = ReporteViewModel.FormatoGenero(apiCivil.RegistroCivil.Genero);
                                general.Profesion = apiCivil.RegistroCivil.Profesion;
                                general.Instruccion = apiCivil.RegistroCivil.Instruccion;
                                general.Nacionalidad = apiCivil.RegistroCivil.Nacionalidad;
                                var direcciones = !string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio) ? apiCivil.RegistroCivil.LugarDomicilio.Split('/').ToList() : new List<string>();
                                if (direcciones.Any())
                                {
                                    if (direcciones.Count >= 1)
                                        general.Provincia = direcciones[0];

                                    if (direcciones.Count >= 2)
                                        general.Canton = direcciones[1];

                                    if (direcciones.Count >= 3)
                                        general.Parroquia = direcciones[2];
                                }

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                    general.NumeroCasa = apiCivil.RegistroCivil.NumeracionDomicilio;

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio?.Trim()))
                                    general.Domicilio = apiCivil.RegistroCivil.LugarDomicilio;

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                    general.Calle = apiCivil.RegistroCivil.CalleDomicilio;

                                if (apiCivil.RegistroCivil.FechaCedulacion != default)
                                    general.FechaCedulacion = apiCivil.RegistroCivil.FechaCedulacion;

                                datos.Civil.General = general;
                            }
                            #endregion General

                            #region Personal
                            if (datos.Civil.Personal != null)
                            {
                                datos.Civil.Personal.Sexo = ReporteViewModel.FormatoGenero(apiCivil.RegistroCivil.Genero);
                                datos.Civil.Personal.LugarNacimiento = apiCivil.RegistroCivil.LugarNacimiento;
                                datos.Civil.Personal.Nacionalidad = apiCivil.RegistroCivil.Nacionalidad;
                                datos.Civil.Personal.EstadoCivil = apiCivil.RegistroCivil.EstadoCivil;
                                datos.Civil.Personal.NivelEstudio = apiCivil.RegistroCivil.Instruccion;
                                datos.Civil.Personal.Profesion = apiCivil.RegistroCivil.Profesion;
                                datos.Civil.Personal.NombreConyuge = apiCivil.RegistroCivil.Conyuge?.Trim();
                                datos.Civil.Personal.CedulaConyuge = apiCivil.RegistroCivil.CedulaConyuge?.Trim();
                                datos.Civil.Personal.NombrePadre = apiCivil.RegistroCivil.NombrePadre?.Trim();
                                datos.Civil.Personal.CedPadre = apiCivil.RegistroCivil.CedulaPadre?.Trim();
                                datos.Civil.Personal.NombreMadre = apiCivil.RegistroCivil.NombreMadre?.Trim();
                                datos.Civil.Personal.CedMadre = apiCivil.RegistroCivil.CedulaMadre?.Trim();
                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                    datos.Civil.Personal.NombreCalle = apiCivil.RegistroCivil.CalleDomicilio?.Trim();
                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                    datos.Civil.Personal.NumeroCasa = apiCivil.RegistroCivil.NumeracionDomicilio?.Trim();
                                datos.Civil.Personal.DesEstudio = apiCivil.RegistroCivil.Instruccion;
                                datos.Civil.Personal.DesProfesion = apiCivil.RegistroCivil.Profesion;
                            }
                            else
                            {
                                var personal = new Externos.Logica.Garancheck.Modelos.Personal();
                                personal.Sexo = ReporteViewModel.FormatoGenero(apiCivil.RegistroCivil.Genero);
                                personal.LugarNacimiento = apiCivil.RegistroCivil.LugarNacimiento;
                                personal.Nacionalidad = apiCivil.RegistroCivil.Nacionalidad;
                                personal.EstadoCivil = apiCivil.RegistroCivil.EstadoCivil;
                                personal.NivelEstudio = apiCivil.RegistroCivil.Instruccion;
                                personal.Profesion = apiCivil.RegistroCivil.Profesion;
                                personal.NombreConyuge = apiCivil.RegistroCivil.Conyuge?.Trim();
                                personal.CedulaConyuge = apiCivil.RegistroCivil.CedulaConyuge?.Trim();
                                personal.NombrePadre = apiCivil.RegistroCivil.NombrePadre?.Trim();
                                personal.CedPadre = apiCivil.RegistroCivil.CedulaPadre?.Trim();
                                personal.NombreMadre = apiCivil.RegistroCivil.NombreMadre?.Trim();
                                personal.CedMadre = apiCivil.RegistroCivil.CedulaMadre?.Trim();
                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                    personal.NombreCalle = apiCivil.RegistroCivil.CalleDomicilio?.Trim();
                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                    personal.NumeroCasa = apiCivil.RegistroCivil.NumeracionDomicilio?.Trim();
                                personal.DesEstudio = apiCivil.RegistroCivil.Instruccion;
                                personal.DesProfesion = apiCivil.RegistroCivil.Profesion;
                                datos.Civil.Personal = personal;

                            }
                            #endregion Personal

                            #region Familiares
                            if (datos.Civil.Familiares != null)
                            {
                                if (datos.Civil.Familiares.Padre != null)
                                {
                                    datos.Civil.Familiares.Padre.Cedula = apiCivil.RegistroCivil.CedulaPadre?.Trim();
                                    datos.Civil.Familiares.Padre.Nombre = apiCivil.RegistroCivil.NombrePadre?.Trim();
                                }
                                else
                                {
                                    datos.Civil.Familiares.Padre = new Datos()
                                    {
                                        Cedula = apiCivil.RegistroCivil.CedulaPadre?.Trim(),
                                        Nombre = apiCivil.RegistroCivil.NombrePadre?.Trim()
                                    };
                                }

                                if (datos.Civil.Familiares.Madre != null)
                                {
                                    datos.Civil.Familiares.Madre.Cedula = apiCivil.RegistroCivil.CedulaMadre?.Trim();
                                    datos.Civil.Familiares.Madre.Nombre = apiCivil.RegistroCivil.NombreMadre?.Trim();
                                }
                                else
                                {
                                    datos.Civil.Familiares.Madre = new Datos()
                                    {
                                        Cedula = apiCivil.RegistroCivil.CedulaMadre?.Trim(),
                                        Nombre = apiCivil.RegistroCivil.NombreMadre?.Trim()
                                    };
                                }

                                if (datos.Civil.Familiares.Conyuge != null)
                                {
                                    datos.Civil.Familiares.Conyuge.Cedula = apiCivil.RegistroCivil.CedulaConyuge?.Trim();
                                    datos.Civil.Familiares.Conyuge.Nombre = apiCivil.RegistroCivil.Conyuge?.Trim();
                                }
                                else
                                {
                                    datos.Civil.Familiares.Conyuge = new Datos()
                                    {
                                        Cedula = apiCivil.RegistroCivil.CedulaConyuge?.Trim(),
                                        Nombre = apiCivil.RegistroCivil.Conyuge?.Trim()
                                    };
                                }
                            }
                            else
                            {
                                var familiares = new Externos.Logica.Garancheck.Modelos.Familia();
                                familiares.Padre = new Datos()
                                {
                                    Cedula = apiCivil.RegistroCivil.CedulaPadre,
                                    Nombre = apiCivil.RegistroCivil.NombrePadre
                                };

                                familiares.Madre = new Datos()
                                {
                                    Cedula = apiCivil.RegistroCivil.CedulaMadre,
                                    Nombre = apiCivil.RegistroCivil.NombreMadre
                                };

                                familiares.Conyuge = new Datos()
                                {
                                    Cedula = apiCivil.RegistroCivil.CedulaConyuge,
                                    Nombre = apiCivil.RegistroCivil.Conyuge
                                };
                                datos.Civil.Familiares = familiares;
                            }
                            #endregion Familiares

                            #region Contactos
                            if (datos.Civil.Contactos != null && datos.Civil.Contactos.Direcciones != null && datos.Civil.Contactos.Direcciones.Any())
                            {
                                var direccionTemp = apiCivil.RegistroCivil.LugarDomicilio;
                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                    direccionTemp += $"/{apiCivil.RegistroCivil.CalleDomicilio}";

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                    direccionTemp += $"/{apiCivil.RegistroCivil.NumeracionDomicilio}";

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio?.Trim()))
                                    datos.Civil.Contactos.Direcciones.Add(direccionTemp);

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarNacimiento?.Trim()))
                                    datos.Civil.Contactos.Direcciones.Add(apiCivil.RegistroCivil.LugarNacimiento);
                            }
                            else if (datos.Civil.Contactos != null)
                            {
                                var direccionesContactos = new List<string>();
                                var direccionTemp = apiCivil.RegistroCivil.LugarDomicilio;
                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                    direccionTemp += $"/{apiCivil.RegistroCivil.CalleDomicilio}";

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                    direccionTemp += $"/{apiCivil.RegistroCivil.NumeracionDomicilio}";

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio?.Trim()))
                                    direccionesContactos.Add(direccionTemp);

                                if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarNacimiento?.Trim()))
                                    direccionesContactos.Add(apiCivil.RegistroCivil.LugarNacimiento);

                                datos.Civil.Contactos.Direcciones = direccionesContactos;
                            }

                            if (datos.Civil.Contactos != null && datos.Civil.Contactos.Direcciones != null && datos.Civil.Contactos.Direcciones.Any())
                                datos.Civil.General.Direcciones = datos.Civil.Contactos.Direcciones.Select((item, index) => new { Key = index, Value = item }).ToDictionary(x => x.Key, x => x.Value);

                            if (datos.Civil.Contactos != null && datos.Civil.Contactos.Telefonos != null && datos.Civil.Contactos.Telefonos.Any())
                                datos.Civil.General.Telefonos = datos.Civil.Contactos.Telefonos.Select((item, index) => new { Key = index, Value = item }).ToDictionary(x => x.Key, x => x.Value);

                            if (datos.Civil.Contactos != null && datos.Civil.Contactos.Correos != null && datos.Civil.Contactos.Correos.Any())
                                datos.Civil.General.Correos = datos.Civil.Contactos.Correos.Select((item, index) => new { Key = index, Value = item }).ToDictionary(x => x.Key, x => x.Value);
                            #endregion Contactos
                        }
                        else if (datos != null && datos.Civil == null)
                        {
                            datos.Civil = new CivilApiViewModel();

                            #region General
                            var general = new Externos.Logica.Garancheck.Modelos.Persona();
                            general.TipoIdentificacion = 'C';
                            general.Cedula = apiCivil.RegistroCivil.Cedula;
                            general.Nombres = apiCivil.RegistroCivil.Nombre;
                            general.Identificacion = apiCivil.RegistroCivil.Cedula;
                            general.FechaNacimiento = apiCivil.RegistroCivil.FechaNacimiento;
                            general.EstadoCivil = apiCivil.RegistroCivil.EstadoCivil;
                            general.Genero = ReporteViewModel.FormatoGenero(apiCivil.RegistroCivil.Genero);
                            general.Profesion = apiCivil.RegistroCivil.Profesion;
                            general.Instruccion = apiCivil.RegistroCivil.Instruccion;
                            general.Nacionalidad = apiCivil.RegistroCivil.Nacionalidad;
                            var direcciones = !string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio) ? apiCivil.RegistroCivil.LugarDomicilio.Split('/').ToList() : new List<string>();
                            if (direcciones.Any())
                            {
                                if (direcciones.Count >= 1)
                                    general.Provincia = direcciones[0];

                                if (direcciones.Count >= 2)
                                    general.Canton = direcciones[1];

                                if (direcciones.Count >= 3)
                                    general.Parroquia = direcciones[2];
                            }

                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                general.NumeroCasa = apiCivil.RegistroCivil.NumeracionDomicilio;

                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio?.Trim()))
                                general.Domicilio = apiCivil.RegistroCivil.LugarDomicilio;

                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                general.Calle = apiCivil.RegistroCivil.CalleDomicilio;

                            if (apiCivil.RegistroCivil.FechaCedulacion != default)
                                general.FechaCedulacion = apiCivil.RegistroCivil.FechaCedulacion;

                            datos.Civil.General = general;
                            #endregion General

                            #region Personal
                            var personal = new Externos.Logica.Garancheck.Modelos.Personal();
                            personal.Sexo = ReporteViewModel.FormatoGenero(apiCivil.RegistroCivil.Genero);
                            personal.LugarNacimiento = apiCivil.RegistroCivil.LugarNacimiento;
                            personal.Nacionalidad = apiCivil.RegistroCivil.Nacionalidad;
                            personal.EstadoCivil = apiCivil.RegistroCivil.EstadoCivil;
                            personal.NivelEstudio = apiCivil.RegistroCivil.Instruccion;
                            personal.Profesion = apiCivil.RegistroCivil.Profesion;
                            personal.NombreConyuge = apiCivil.RegistroCivil.Conyuge?.Trim();
                            personal.CedulaConyuge = apiCivil.RegistroCivil.CedulaConyuge?.Trim();
                            personal.NombrePadre = apiCivil.RegistroCivil.NombrePadre?.Trim();
                            personal.CedPadre = apiCivil.RegistroCivil.CedulaPadre?.Trim();
                            personal.NombreMadre = apiCivil.RegistroCivil.NombreMadre?.Trim();
                            personal.CedMadre = apiCivil.RegistroCivil.CedulaMadre?.Trim();
                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                personal.NombreCalle = apiCivil.RegistroCivil.CalleDomicilio?.Trim();
                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                personal.NumeroCasa = apiCivil.RegistroCivil.NumeracionDomicilio?.Trim();
                            personal.DesEstudio = apiCivil.RegistroCivil.Instruccion;
                            personal.DesProfesion = apiCivil.RegistroCivil.Profesion;
                            datos.Civil.Personal = personal;
                            #endregion Personal

                            #region Familiares
                            var familiares = new Externos.Logica.Garancheck.Modelos.Familia();
                            familiares.Padre = new Datos()
                            {
                                Cedula = apiCivil.RegistroCivil.CedulaPadre?.Trim(),
                                Nombre = apiCivil.RegistroCivil.NombrePadre?.Trim()
                            };

                            familiares.Madre = new Datos()
                            {
                                Cedula = apiCivil.RegistroCivil.CedulaMadre?.Trim(),
                                Nombre = apiCivil.RegistroCivil.NombreMadre?.Trim()
                            };

                            familiares.Conyuge = new Datos()
                            {
                                Cedula = apiCivil.RegistroCivil.CedulaConyuge?.Trim(),
                                Nombre = apiCivil.RegistroCivil.Conyuge?.Trim()
                            };
                            datos.Civil.Familiares = familiares;
                            #endregion Familiares

                            #region Contactos
                            var contactos = new Externos.Logica.Garancheck.Modelos.Contacto();
                            var direccionesContactos = new List<string>();
                            var direccionTempContactos = apiCivil.RegistroCivil.LugarDomicilio;
                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.CalleDomicilio?.Trim()))
                                direccionTempContactos += $"/{apiCivil.RegistroCivil.CalleDomicilio}";

                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.NumeracionDomicilio?.Trim()))
                                direccionTempContactos += $"/{apiCivil.RegistroCivil.NumeracionDomicilio}";

                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarDomicilio?.Trim()))
                                direccionesContactos.Add(direccionTempContactos);

                            if (!string.IsNullOrEmpty(apiCivil.RegistroCivil.LugarNacimiento?.Trim()))
                                direccionesContactos.Add(apiCivil.RegistroCivil.LugarNacimiento);

                            contactos.Direcciones = direccionesContactos;
                            datos.Civil.Contactos = contactos;

                            if (datos.Civil.Contactos.Direcciones != null && datos.Civil.Contactos.Direcciones.Any())
                                datos.Civil.General.Direcciones = datos.Civil.Contactos.Direcciones.Select((item, index) => new { Key = index, Value = item }).ToDictionary(x => x.Key, x => x.Value);

                            if (datos.Civil.Contactos != null && datos.Civil.Contactos.Telefonos != null && datos.Civil.Contactos.Telefonos.Any())
                                datos.Civil.General.Telefonos = datos.Civil.Contactos.Telefonos.Select((item, index) => new { Key = index, Value = item }).ToDictionary(x => x.Key, x => x.Value);

                            if (datos.Civil.Contactos != null && datos.Civil.Contactos.Correos != null && datos.Civil.Contactos.Correos.Any())
                                datos.Civil.General.Correos = datos.Civil.Contactos.Correos.Select((item, index) => new { Key = index, Value = item }).ToDictionary(x => x.Key, x => x.Value);
                            #endregion Contactos
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                if (fuenteTodas || fuenteSocietario)
                {
                    apiSocietario = await ObtenerReporteBalance(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        Periodos = modelo.Periodos,
                        IdUsuario = user.Id
                    });
                    datos.Societario = apiSocietario != null && (apiSocietario.DirectorioCompania != null || (apiSocietario.Balances != null && apiSocietario.Balances.Any()) || (apiSocietario.AnalisisHorizontal != null && apiSocietario.AnalisisHorizontal.Any()) || (apiSocietario.RepresentantesEmpresas != null && apiSocietario.RepresentantesEmpresas.Any()) || (apiSocietario.Accionistas != null && apiSocietario.Accionistas.Any()) || (apiSocietario.EmpresasAccionista != null && apiSocietario.EmpresasAccionista.Any())) ? new BalanceApiViewModel()
                    {
                        DirectorioCompania = apiSocietario.DirectorioCompania != null ? apiSocietario.DirectorioCompania : null,
                        Balances = apiSocietario.Balances != null && apiSocietario.Balances.Any() ? apiSocietario.Balances : null,
                        AnalisisHorizontal = apiSocietario.AnalisisHorizontal != null && apiSocietario.AnalisisHorizontal.Any() ? apiSocietario.AnalisisHorizontal : null,
                        RepresentantesEmpresas = apiSocietario.RepresentantesEmpresas != null && apiSocietario.RepresentantesEmpresas.Any() ? apiSocietario.RepresentantesEmpresas : null,
                        Accionistas = apiSocietario.Accionistas != null && apiSocietario.Accionistas.Any() ? apiSocietario.Accionistas : null,
                        EmpresasAccionista = apiSocietario.EmpresasAccionista != null && apiSocietario.EmpresasAccionista.Any() ? apiSocietario.EmpresasAccionista : null
                    } : null;
                }

                if (fuenteTodas || fuenteIess)
                {
                    apiIess = await ObtenerReporteIESS(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id,
                        IdEmpresa = usuarioActual.IdEmpresa
                    });
                    datos.Iess = apiIess != null && (apiIess.Iess != null || apiIess.Afiliado != null) ? new IessApiViewModel()
                    {
                        Obligacion = apiIess.Iess != null ? apiIess.Iess : null,
                        Afiliado = apiIess.Afiliado != null ? new AfiliadoApiViewModel()
                        {
                            Certificado = apiIess.Afiliado != null && (!string.IsNullOrEmpty(apiIess.Afiliado.Instituto) || !string.IsNullOrEmpty(apiIess.Afiliado.Persona)
                           || !string.IsNullOrEmpty(apiIess.Afiliado.Empresa) || !string.IsNullOrEmpty(apiIess.Afiliado.MsjEstado) || !string.IsNullOrEmpty(apiIess.Afiliado.Estado)) ?
                           $"{apiIess.Afiliado.Instituto} {apiIess.Afiliado.Persona} {apiIess.Afiliado.Empresa} {apiIess.Afiliado.MsjEstado}{apiIess.Afiliado.Estado}" : null,
                            Estado = apiIess.Afiliado != null && !string.IsNullOrEmpty(apiIess.Afiliado.Estado) ? $"{apiIess.Afiliado.Estado}" : null,
                            Fecha = apiIess.Afiliado != null && !string.IsNullOrEmpty(apiIess.Afiliado.Fecha) ? apiIess.Afiliado.Fecha : null
                        } : null,
                        EmpresasAfiliado = apiIess.AfiliadoAdicional != null && apiIess.AfiliadoAdicional.Any() ? apiIess.AfiliadoAdicional : null,
                        EmpleadosEmpresa = apiIess.EmpleadosEmpresa != null && apiIess.EmpleadosEmpresa.Any() ? apiIess.EmpleadosEmpresa : null
                    } : null;
                }

                if (fuenteTodas || fuenteSenescyt)
                {
                    apiSenescyt = await ObtenerReporteSenescyt(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Senescyt = apiSenescyt != null ? apiSenescyt.Senescyt : null;
                }

                if (fuenteTodas || fuenteJudicial)
                {
                    apiLegal = await ObtenerReporteLegal(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id,
                        IdEmpresa = usuarioActual.IdEmpresa
                    });
                    datos.Judicial = apiLegal;
                }

                if (fuenteImpedimento)
                {
                    datos.Judicial = apiLegal;
                    apiImpedimento = await ObtenerReporteImpedimento(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Judicial.Impedimento = apiImpedimento != null && apiImpedimento.Impedimento != null ? new ImpedimentoApiViewModel()
                    {
                        Nombre = !string.IsNullOrEmpty(apiImpedimento.Impedimento.Nombre?.Trim()) ? apiImpedimento.Impedimento.Nombre.Trim() : null,
                        Identificacion = !string.IsNullOrEmpty(apiImpedimento.Impedimento.Identificacion?.Trim()) ? apiImpedimento.Impedimento.Identificacion.Trim() : null,
                        NumeroCertificado = !string.IsNullOrEmpty(apiImpedimento.Impedimento.NumeroCertificado?.Trim()) ? apiImpedimento.Impedimento.NumeroCertificado.Trim() : null,
                        RegistraImpedimento = !string.IsNullOrEmpty(apiImpedimento.Impedimento.ImpedimentoValor?.Trim()) ? apiImpedimento.Impedimento.ImpedimentoValor.Trim() : null,
                        Contenido = !string.IsNullOrEmpty(apiImpedimento.Impedimento.Contenido?.Trim()) ? apiImpedimento.Impedimento.Contenido.Trim().Replace(", conforme a las siguientes causales:", ".").Trim() : null,
                    } : null;
                }

                if (fuenteTodas || fuenteSercop)
                {
                    apiSercop = await ObtenerReporteSERCOP(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Sercop = apiSercop != null && (apiSercop.Proveedor != null || (apiSercop.ProveedorContraloria != null && apiSercop.ProveedorContraloria.Any())) ? new SercopApiViewModel()
                    {
                        Proveedor = apiSercop.Proveedor != null ? apiSercop.Proveedor : null,
                        ProveedorContraloria = apiSercop.ProveedorContraloria != null && apiSercop.ProveedorContraloria.Any() ? apiSercop.ProveedorContraloria : null
                    } : null;
                }

                if (fuenteTodas || fuenteAnt)
                {
                    apiAnt = await ObtenerReporteANT(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Ant = apiAnt != null && (apiAnt.Licencia != null || (apiAnt.AutosHistorico != null && apiAnt.AutosHistorico.Any())) ? new AntApiViewModel()
                    {
                        Licencia = apiAnt.Licencia != null ? apiAnt.Licencia : null,
                        AutosHistorico = apiAnt.AutosHistorico != null && apiAnt.AutosHistorico.Any() ? apiAnt.AutosHistorico : null
                    } : null;
                }

                if (fuenteTodas || fuentePension)
                {
                    apiPension = await ObtenerReportePensionAlimenticia(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PensionAlimenticia = apiPension != null ? apiPension.PensionAlimenticia : null;
                }

                if (fuenteTodas || fuenteSuperBancos)
                {
                    //apiSuperBancos = await ObtenerReporteSuperBancos(new ApiViewModel_1790325083001()
                    //{
                    //    IdHistorial = idHistorial,
                    //    Identificacion = identificacionOriginal,
                    //    IdUsuario = user.Id,
                    //});
                    datos.SuperBancos = null;
                }

                if (fuenteAntecedentes)
                {
                    //apiAntecedentes = await ObtenerReporteAntecedentesPenales(new ApiViewModel_1790325083001()
                    //{
                    //    IdHistorial = idHistorial,
                    //    Identificacion = identificacionOriginal,
                    //    IdUsuario = user.Id,
                    //});
                    //datos.AntecedentesPenales = apiAntecedentes != null ? apiAntecedentes.Antecedentes : null;
                    datos.AntecedentesPenales = null;
                }

                if (fuenteFuerzasArmadas)
                {
                    apiFuerzasArmadas = await ObtenerReporteFuerzasArmadas(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id,
                    });
                    datos.FuerzasArmadas = apiFuerzasArmadas != null ? apiFuerzasArmadas.FuerzasArmadas : null;
                }

                if (fuenteDeNoBaja)
                {
                    apiDeNoBaja = await ObtenerReporteDeNoBaja(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id,
                    });
                    datos.DeNoBaja = apiDeNoBaja != null ? apiDeNoBaja.DeNoBaja : null;
                }

                if (fuentePredios)
                {
                    apiPredios = await ObtenerReportePredios(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.Predios = apiPredios;
                }

                if (fuentePrediosCuenca)
                {
                    apiPrediosCuenca = await ObtenerReportePrediosCuenca(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosCuenca = apiPrediosCuenca;
                }

                if (fuentePrediosStoDomingo)
                {
                    apiPrediosStoDomingo = await ObtenerReportePrediosSantoDomingo(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosSantoDomingo = apiPrediosStoDomingo;
                }

                if (fuentePrediosRuminahui)
                {
                    apiPrediosRuminahui = await ObtenerReportePrediosRuminahui(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosRuminahui = apiPrediosRuminahui;
                }

                if (fuentePrediosQuininde)
                {
                    apiPrediosQuininde = await ObtenerReportePrediosQuininde(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosQuininde = apiPrediosQuininde;
                }

                if (fuentePrediosLatacunga)
                {
                    apiPrediosLatacunga = await ObtenerReportePrediosLatacunga(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosLatacunga = apiPrediosLatacunga;
                }

                if (fuentePrediosManta)
                {
                    apiPrediosManta = await ObtenerReportePrediosManta(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosManta = apiPrediosManta;
                }

                if (fuentePrediosAmbato)
                {
                    apiPrediosAmbato = await ObtenerReportePrediosAmbato(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosAmbato = apiPrediosAmbato;
                }

                if (fuentePrediosIbarra)
                {
                    apiPrediosIbarra = await ObtenerReportePrediosIbarra(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosIbarra = apiPrediosIbarra;
                }

                if (fuentePrediosSanCristobal)
                {
                    apiPrediosSanCristobal = await ObtenerReportePrediosSanCristobal(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosSanCristobal = apiPrediosSanCristobal;
                }

                if (fuentePrediosDuran)
                {
                    apiPrediosDuran = await ObtenerReportePrediosDuran(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosDuran = apiPrediosDuran;
                }

                if (fuentePrediosLagoAgrio)
                {
                    apiPrediosLagoAgrio = await ObtenerReportePrediosLagoAgrio(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosLagoAgrio = apiPrediosLagoAgrio;
                }

                if (fuentePrediosSantaRosa)
                {
                    apiPrediosSantaRosa = await ObtenerReportePrediosSantaRosa(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosSantaRosa = apiPrediosSantaRosa;
                }

                if (fuentePrediosSucua)
                {
                    apiPrediosSucua = await ObtenerReportePrediosSucua(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosSucua = apiPrediosSucua;
                }

                if (fuentePrediosSigSig)
                {
                    apiPrediosSigSig = await ObtenerReportePrediosSigSig(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosSigsig = apiPrediosSigSig;
                }

                if (fuentePrediosMejia)
                {
                    apiPrediosMejia = await ObtenerReportePrediosMejia(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosMejia = apiPrediosMejia;
                }

                if (fuentePrediosMorona)
                {
                    apiPrediosMorona = await ObtenerReportePrediosMorona(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosMorona = apiPrediosMorona;
                }

                if (fuentePrediosTena)
                {
                    apiPrediosTena = await ObtenerReportePrediosTena(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosTena = apiPrediosTena;
                }
                if (fuentePrediosCatamayo)
                {
                    apiPrediosCatamayo = await ObtenerReportePrediosCatamayo(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosCatamayo = apiPrediosCatamayo;
                }

                if (fuentePrediosLoja)
                {
                    apiPrediosLoja = await ObtenerReportePrediosLoja(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosLoja = apiPrediosLoja;
                }

                if (fuentePrediosSamborondon)
                {
                    apiPrediosSamborondon = await ObtenerReportePrediosSamborondon(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosSamborondon = apiPrediosSamborondon;
                }

                if (fuentePrediosDaule)
                {
                    apiPrediosDaule = await ObtenerReportePrediosDaule(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosDaule = apiPrediosDaule;
                }

                if (fuentePrediosCayambe)
                {
                    apiPrediosCayambe = await ObtenerReportePrediosCayambe(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosCayambe = apiPrediosCayambe;
                }

                if (fuentePrediosAzogues)
                {
                    apiPrediosAzogues = await ObtenerReportePrediosAzogues(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosAzogues = apiPrediosAzogues;
                }

                if (fuentePrediosEsmeraldas)
                {
                    apiPrediosEsmeraldas = await ObtenerReportePrediosEsmeraldas(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosEsmeraldas = apiPrediosEsmeraldas;
                }

                if (fuentePrediosCotacachi)
                {
                    apiPrediosCotacachi = await ObtenerReportePrediosCotacachi(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.PrediosCotacachi = apiPrediosCotacachi;
                }

                if (fuenteFiscaliaDelitos)
                {
                    apiFiscaliaDelitos = await ObtenerReporteFiscaliaDelitos(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.FiscaliaDelitos = apiFiscaliaDelitos;
                }

                if (fuenteUafe)
                {
                    apiUafe = await ObtenerReporteUafe(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });
                    datos.UAFE = apiUafe;
                }

                if (modelo.ConsultarBuro)
                {
                    apiBuroCredito = await ObtenerReporteBuroCredito(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        Identificacion = identificacionOriginal,
                        IdUsuario = user.Id
                    });

                    datos.BuroCredito = apiBuroCredito != null && (apiBuroCredito.BuroCredito != null || apiBuroCredito.BuroCreditoEquifax != null) ? new BuroCreditoApi()
                    {
                        BuroCreditoAval = apiBuroCredito != null && apiBuroCredito.BuroCredito != null ? apiBuroCredito.BuroCredito : null,
                        BuroCreditoEquifax = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null ? apiBuroCredito.BuroCreditoEquifax : null
                    } : null;
                }

                #region Evaluacion

                if (modelo.Evaluar && modelo.SegmentoCartera == Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.MicroCredito)
                {
                    datos.Evaluacion = new EvaluacionApi();
                    apiEvaluacion = await ObtenerCalificacionMicrocredito(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        TipoIdentificacion = tipoIdentificacion,
                        IdUsuario = user.Id,
                        IdEmpresa = usuarioActual.IdEmpresa
                    });

                    if (apiEvaluacion != null && apiEvaluacion.Any())
                    {
                        foreach (var item in apiEvaluacion)
                        {
                            evaluacion = new CalificacionApiViewModel()
                            {
                                Cabecera = new CabeceraCalificacionApi()
                                {
                                    Resultado = item.Aprobado ? item.Aprobado : false,
                                    TotalValidados = item.TotalValidados >= 0 ? item.TotalValidados : 0,
                                    TotalAprobados = item.TotalAprobados >= 0 ? item.TotalAprobados : 0,
                                    TotalRechazados = item.TotalRechazados >= 0 ? item.TotalRechazados : 0,
                                    Calificacion = item.Calificacion >= 0 ? item.Calificacion : 0,
                                    Score = item.Score > 0 ? item.Score : null,
                                    CalificacionCliente = !string.IsNullOrEmpty(calificacionCliente) ? calificacionCliente : null,
                                    CapacidadPagoMensual = item.CupoEstimado > 0 ? item.CupoEstimado : null,
                                    VentasEmpresa = item.VentasEmpresa > 0 ? item.VentasEmpresa : null,
                                    PatrimonioEmpresa = item.PatrimonioEmpresa > 0 ? item.PatrimonioEmpresa : null,
                                    IngresoEstimado = !string.IsNullOrEmpty(item.RangoIngreso) ? $"${item.RangoIngreso}" : null,
                                    GastoFinanciero = item.GastoFinanciero.HasValue ? item.GastoFinanciero : null
                                }
                            };

                            evaluacion.DetalleCalificacion = new List<DetalleCalificacionApi>();
                            foreach (var item2 in item.DetalleCalificacion)
                            {
                                detalleCalificacion = new DetalleCalificacionApi()
                                {
                                    Politica = !string.IsNullOrEmpty(item2.Politica) ? item2.Politica : null,
                                    ValorResultado = !string.IsNullOrEmpty(item2.ValorResultado) ? item2.ValorResultado : null,
                                    ReferenciaMinima = !string.IsNullOrEmpty(item2.ReferenciaMinima) ? item2.ReferenciaMinima : null,
                                    ResultadoPolitica = item2.ResultadoPolitica ? item2.ResultadoPolitica : false
                                };
                                evaluacion.DetalleCalificacion.Add(detalleCalificacion);
                            }

                            datos.Evaluacion.EvaluacionGeneral = evaluacion;
                        }
                    }
                    else
                    {
                        datos.Evaluacion = null;
                    }
                }
                else if (modelo.Evaluar && modelo.SegmentoCartera == Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Consumo)
                {
                    datos.Evaluacion = new EvaluacionApi();
                    apiEvaluacion = await ObtenerCalificacionConsumo(new ApiViewModel_1790325083001()
                    {
                        IdHistorial = idHistorial,
                        TipoIdentificacion = tipoIdentificacion,
                        IdUsuario = user.Id,
                        IdEmpresa = usuarioActual.IdEmpresa
                    });

                    if (apiEvaluacion != null && apiEvaluacion.Any())
                    {
                        foreach (var item in apiEvaluacion)
                        {
                            evaluacion = new CalificacionApiViewModel()
                            {
                                Cabecera = new CabeceraCalificacionApi()
                                {
                                    Resultado = item.Aprobado ? item.Aprobado : false,
                                    TotalValidados = item.TotalValidados >= 0 ? item.TotalValidados : 0,
                                    TotalAprobados = item.TotalAprobados >= 0 ? item.TotalAprobados : 0,
                                    TotalRechazados = item.TotalRechazados >= 0 ? item.TotalRechazados : 0,
                                    Calificacion = item.Calificacion >= 0 ? item.Calificacion : 0,
                                    Score = item.Score > 0 ? item.Score : null,
                                    CalificacionCliente = !string.IsNullOrEmpty(calificacionCliente) ? calificacionCliente : null,
                                    CapacidadPagoMensual = item.CupoEstimado > 0 ? item.CupoEstimado : null,
                                    VentasEmpresa = item.VentasEmpresa > 0 ? item.VentasEmpresa : null,
                                    PatrimonioEmpresa = item.PatrimonioEmpresa > 0 ? item.PatrimonioEmpresa : null,
                                    IngresoEstimado = !string.IsNullOrEmpty(item.RangoIngreso) ? $"${item.RangoIngreso}" : null,
                                    GastoFinanciero = item.GastoFinanciero.HasValue ? item.GastoFinanciero : null
                                }
                            };

                            evaluacion.DetalleCalificacion = new List<DetalleCalificacionApi>();
                            foreach (var item2 in item.DetalleCalificacion)
                            {
                                detalleCalificacion = new DetalleCalificacionApi()
                                {
                                    Politica = !string.IsNullOrEmpty(item2.Politica) ? item2.Politica : null,
                                    ValorResultado = !string.IsNullOrEmpty(item2.ValorResultado) ? item2.ValorResultado : null,
                                    ReferenciaMinima = !string.IsNullOrEmpty(item2.ReferenciaMinima) ? item2.ReferenciaMinima : null,
                                    ResultadoPolitica = item2.ResultadoPolitica ? item2.ResultadoPolitica : false
                                };
                                evaluacion.DetalleCalificacion.Add(detalleCalificacion);
                            }
                            datos.Evaluacion.EvaluacionGeneral = evaluacion;
                        }
                    }
                    else
                        datos.Evaluacion = null;
                }
                #endregion Evaluacion

                #region Tipo Identificación
                var tipoIdentificacionApi = string.Empty;
                if (ValidacionViewModel.ValidarCedula(identificacionOriginal))
                    tipoIdentificacionApi = "N";
                else if (ValidacionViewModel.ValidarRuc(identificacionOriginal))
                {
                    if (datos != null && datos.Sri != null && datos.Sri.Empresa != null && !string.IsNullOrEmpty(datos.Sri.Empresa.PersonaSociedad))
                    {
                        if (datos.Sri.Empresa.PersonaSociedad == "SCD")
                            tipoIdentificacionApi = "J";
                        else
                            tipoIdentificacionApi = "N";
                    }
                    else
                    {
                        if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                            tipoIdentificacionApi = "J";
                        else
                            tipoIdentificacionApi = "N";
                    }
                }
                else
                    tipoIdentificacionApi = "N/A";

                if (datos != null && datos.Civil != null)
                    datos.Civil.TipoIdentificacion = tipoIdentificacionApi;

                if (datos != null && datos.Sri != null)
                    datos.Sri.TipoIdentificacion = tipoIdentificacionApi;
                #endregion Tipo Identificación

                return Json(datos, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
        }
        private async Task<SriApiViewModel> ObtenerReporteSRI(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                List<Externos.Logica.Balances.Modelos.Similares> r_similares = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                Externos.Logica.Balances.Modelos.CatastroFantasma catastroFantasma = null;
                var cacheSri = false;
                var cacheContactosEmpresa = false;
                var cacheEmpSimilares = false;
                var cacheCatastrosFantasmas = false;
                var cedulaEntidades = false;
                var consultaFantasma = false;
                var impuestosRenta = new List<Externos.Logica.SRi.Modelos.Anexo>();
                ResultadoContribuyente resultadoSri = null;
                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.Sri;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                switch (tipoFuente)
                                {
                                    case 1:
                                        resultadoSri = await _sri.GetRespuestaAsyncV2(modelo.Identificacion);
                                        r_sri = resultadoSri?.Contribuyente;
                                        break;
                                    case 2:
                                        r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 3:
                                        r_sri = await _sri.GetEmpresaAccionistaAsync(modelo.Identificacion);
                                        if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                            r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);

                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 4:
                                        r_sri = await _sri.GetContribuyenteHistoricoAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;
                                        break;
                                    default:
                                        resultadoSri = await _sri.GetRespuestaAsyncV2(modelo.Identificacion);
                                        r_sri = resultadoSri?.Contribuyente;
                                        break;
                                }
                                contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                if (r_sri != null)
                                {
                                    r_similares = await _balances.GetEmpresasSimilaresAsync(modelo.Identificacion);
                                    if (r_sri.Fantasma)
                                    {
                                        catastroFantasma = await _balances.GetCatastrosFantasmasAsync(modelo.Identificacion);
                                        consultaFantasma = true;
                                    }
                                }
                            }
                            else
                            {
                                switch (tipoFuente)
                                {
                                    case 1:
                                        resultadoSri = await _sri.GetRespuestaAsyncV2(modelo.Identificacion);
                                        r_sri = resultadoSri?.Contribuyente;
                                        break;
                                    case 2:
                                        r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 3:
                                        r_sri = await _sri.GetEmpresaAccionistaAsync(modelo.Identificacion);
                                        if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                            r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);

                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 4:
                                        r_sri = await _sri.GetContribuyenteHistoricoAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;
                                        break;
                                    default:
                                        resultadoSri = await _sri.GetRespuestaAsyncV2(modelo.Identificacion);
                                        r_sri = resultadoSri?.Contribuyente;
                                        break;
                                }
                                contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                if (r_sri != null)
                                {
                                    r_similares = await _balances.GetEmpresasSimilaresAsync(modelo.Identificacion);
                                    if (r_sri.Fantasma)
                                    {
                                        catastroFantasma = await _balances.GetCatastrosFantasmasAsync(modelo.Identificacion);
                                        consultaFantasma = true;
                                    }
                                }
                            }

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                            {
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);
                                if (string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                    r_sri.AgenteRepresentante = await _balances.GetRepresentanteLegalEmpresaAccionistaAsync(modelo.Identificacion);

                                if (!string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                    r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);
                            }
                        }

                        if (r_sri != null)
                        {
                            if (r_sri.Anexos != null && r_sri.Anexos.Any())
                                impuestosRenta.AddRange(r_sri.Anexos.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
                                {
                                    Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
                                    Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
                                    Formulario = m.Formulario,
                                    Periodo = m.Periodo
                                }).ToList());

                            if (r_sri.Rentas != null && r_sri.Rentas.Any())
                            {
                                if (impuestosRenta.Any())
                                {
                                    foreach (var item in impuestosRenta)
                                    {
                                        var impuesto = r_sri.Rentas.FirstOrDefault(m => m.Periodo == item.Periodo);
                                        if (impuesto != null && impuesto.Causado.HasValue && impuesto.Causado >= 0)
                                        {
                                            item.Formulario = impuesto.Formulario;
                                            item.Causado = impuesto.Causado;
                                        }

                                        if (impuesto != null && impuesto.Formulario != item.Formulario)
                                            item.Formulario = impuesto.Formulario;
                                    }
                                }
                                else
                                {
                                    impuestosRenta.AddRange(r_sri.Rentas.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
                                    {
                                        Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
                                        Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
                                        Formulario = m.Formulario,
                                        Periodo = m.Periodo
                                    }).ToList());
                                }
                            }

                            if (r_sri.Divisas != null && r_sri.Divisas.Any())
                            {
                                if (impuestosRenta.Any())
                                {
                                    foreach (var item in impuestosRenta)
                                    {
                                        var impuesto = r_sri.Divisas.FirstOrDefault(m => m.Periodo == item.Periodo);
                                        if (impuesto != null && impuesto.Divisas.HasValue && impuesto.Divisas >= 0)
                                            item.Divisas = impuesto.Divisas;
                                    }
                                }
                                else
                                {
                                    impuestosRenta.AddRange(r_sri.Divisas.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
                                    {
                                        Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
                                        Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
                                        Formulario = m.Formulario,
                                        Periodo = m.Periodo
                                    }).ToList());
                                }
                            }

                            r_sri.Anexos = impuestosRenta.OrderByDescending(m => m.Periodo).ToList();
                        }

                        if (r_sri == null)
                        {
                            var datosDetalleSri = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleSri != null)
                            {
                                cacheSri = true;
                                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
                            }
                        }
                        //if (contactosEmpresa == null)
                        //{
                        //    var datosDetalleContEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleContEmpresa != null)
                        //    {
                        //        cacheContactosEmpresa = true;
                        //        contactosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContEmpresa);
                        //    }
                        //}
                        //if (r_similares == null)
                        //{
                        //    var datosDetalleEmpSimilares = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasSimilares && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleEmpSimilares != null)
                        //    {
                        //        cacheEmpSimilares = true;
                        //        r_similares = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Similares>>(datosDetalleEmpSimilares);
                        //    }
                        //}
                        //if (catastroFantasma == null && consultaFantasma)
                        //{
                        //    var datosDetalleCatastroFantasma = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.CatastroFantasma && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleCatastroFantasma != null)
                        //    {
                        //        cacheCatastrosFantasmas = true;
                        //        catastroFantasma = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.CatastroFantasma>(datosDetalleCatastroFantasma);
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
                    var pathContactoEmpresa = Path.Combine(pathFuentes, "sriContactoEmpresaDemo.json");
                    var pathSimilares = Path.Combine(pathFuentes, "sriSimilaresDemo.json");
                    var pathCatastroFantasma = Path.Combine(pathFuentes, "sriCatastrosDemo.json");
                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));
                    r_similares = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Similares>>(System.IO.File.ReadAllText(pathSimilares));
                    contactosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(System.IO.File.ReadAllText(pathContactoEmpresa));
                    catastroFantasma = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.CatastroFantasma>(System.IO.File.ReadAllText(pathCatastroFantasma));
                }

                var datos = new SriApiViewModel()
                {
                    Empresa = r_sri,
                    Contactos = contactosEmpresa,
                    EmpresasSimilares = r_similares,
                    CatastroFantasma = catastroFantasma
                };

                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
                                    historial.IdentificacionSecundaria = r_sri.RUC;
                            }
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante?.Trim()) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                                historial.NombresPersona = r_sri.AgenteRepresentante.Trim().ToUpper();

                            if (r_sri != null && (historial.TipoIdentificacion == Dominio.Constantes.General.Cedula || historial.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historial.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                            {
                                if (ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal) && ValidacionViewModel.ValidarRuc(historial.Identificacion))
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
                                else
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Trim();
                            }
                            else
                            {
                                if (r_sri != null && r_sri.PersonaSociedad == "SCD" && historial.TipoIdentificacion == Dominio.Constantes.General.Cedula && ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal))
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
                                else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
                                    historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
                            }
                            await _historiales.UpdateAsync(historial);
                            if (historialConsolidado != null)
                            {
                                historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                                historialConsolidado.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidado);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Sri,
                            Generado = datos.Empresa != null,
                            Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null,
                            Cache = cacheSri,
                            FechaRegistro = DateTime.Now,
                            Reintento = null,
                            DataError = resultadoSri != null ? resultadoSri.Error : null,
                            FuenteActiva = resultadoSri != null ? resultadoSri.FuenteActiva : null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheContactosEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.EmpresasSimilares,
                            Generado = datos.EmpresasSimilares != null && datos.EmpresasSimilares.Any(),
                            Data = datos.EmpresasSimilares != null ? JsonConvert.SerializeObject(datos.EmpresasSimilares) : null,
                            Cache = cacheEmpSimilares,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        if (consultaFantasma)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.CatastroFantasma,
                                Generado = datos.CatastroFantasma != null,
                                Data = datos.CatastroFantasma != null ? JsonConvert.SerializeObject(datos.CatastroFantasma) : null,
                                Cache = cacheCatastrosFantasmas,
                                FechaRegistro = DateTime.Now,
                                Reintento = null
                            });
                        }
                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<CivilApiMetodoViewModel> ObtenerReporteCivil(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.Garancheck.Modelos.Persona r_garancheck = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactos = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosIess = null;
                Externos.Logica.Garancheck.Modelos.Personal datosPersonal = null;
                Externos.Logica.Garancheck.Modelos.Familia familiares = null;
                Externos.Logica.Garancheck.Modelos.RegistroCivil registroCivil = null;
                var datos = new CivilApiMetodoViewModel();
                Historial historialTemp = null;
                var cedulaEntidades = false;
                var cacheCivil = false;
                var cachePersonal = false;
                var cacheContactos = false;
                var cacheContactosIess = false;
                var cacheRegistroCivil = false;
                var cacheFamiliares = false;
                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.RegistroCivil;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Civil identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    contactos = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                    contactosIess = await _garancheck.GetContactoAfiliadoAsync(modelo.Identificacion);
                                    datosPersonal = await _garancheck.GetInformacionPersonalAsync(modelo.Identificacion);

                                    switch (tipoFuente)
                                    {
                                        case 1:
                                            registroCivil = await _garancheck.GetRegistroCivilLineaAsync(modelo.Identificacion);
                                            if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                            {
                                                registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                            }
                                            else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                            {
                                                registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                            }

                                            if (registroCivil == null)
                                                r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);

                                            if (registroCivil != null)
                                                familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                            if (familiares == null)
                                                familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);

                                            break;
                                        case 2:
                                            r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);
                                            if (datosPersonal != null)
                                                familiares = await _garancheck.GetFamiliaLineaAsync(modelo.Identificacion, datosPersonal.CedulaConyuge, datosPersonal.NombreConyuge, datosPersonal.CedMadre, datosPersonal.NombreMadre, datosPersonal.CedPadre, datosPersonal.NombrePadre);

                                            if (familiares == null)
                                                familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);

                                            break;
                                        case 3:
                                            registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(modelo.Identificacion);
                                            if (registroCivil != null)
                                                cacheCivil = true;

                                            familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);
                                            break;
                                        case 4:
                                            registroCivil = await _garancheck.GetRegistroCivilLineaAsync(modelo.Identificacion);
                                            if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                            {
                                                registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                            }
                                            else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                            {
                                                registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                            }

                                            if (registroCivil == null)
                                                r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);

                                            if (registroCivil != null)
                                                familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                            if (familiares == null)
                                                familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);

                                            break;
                                        default:
                                            r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);
                                            familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);
                                            break;
                                    }
                                }
                            }
                            else
                            {

                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                    {
                                        if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        {
                                            contactos = await _garancheck.GetContactoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                            contactosIess = await _garancheck.GetContactoAfiliadoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                            datosPersonal = await _garancheck.GetInformacionPersonalAsync(historialTemp.IdentificacionSecundaria.Trim());

                                            switch (tipoFuente)
                                            {
                                                case 1:
                                                    registroCivil = await _garancheck.GetRegistroCivilLineaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    }
                                                    else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                        registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                        registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                        registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                        registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                    }

                                                    if (registroCivil == null)
                                                        r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                    if (registroCivil != null)
                                                        familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                    if (familiares == null)
                                                        familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                    break;
                                                case 2:
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    if (datosPersonal != null)
                                                        familiares = await _garancheck.GetFamiliaLineaAsync(historialTemp.IdentificacionSecundaria.Trim(), datosPersonal.CedulaConyuge, datosPersonal.NombreConyuge, datosPersonal.CedMadre, datosPersonal.NombreMadre, datosPersonal.CedPadre, datosPersonal.NombrePadre);

                                                    if (familiares == null)
                                                        familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                    break;
                                                case 3:
                                                    registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    if (registroCivil != null)
                                                        cacheCivil = true;

                                                    familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    break;
                                                case 4:
                                                    registroCivil = await _garancheck.GetRegistroCivilLineaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    }
                                                    else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                        registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                        registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                        registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                        registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                    }

                                                    if (registroCivil == null)
                                                        r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                    if (registroCivil != null)
                                                        familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                    if (familiares == null)
                                                        familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                    break;
                                                default:
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                    {
                                        contactos = await _garancheck.GetContactoAsync(cedulaTemp);
                                        contactosIess = await _garancheck.GetContactoAfiliadoAsync(cedulaTemp);
                                        datosPersonal = await _garancheck.GetInformacionPersonalAsync(cedulaTemp);

                                        switch (tipoFuente)
                                        {
                                            case 1:
                                                registroCivil = await _garancheck.GetRegistroCivilLineaAsync(cedulaTemp);
                                                if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                }
                                                else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                    registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                    registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                    registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                }

                                                if (registroCivil == null)
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);

                                                if (registroCivil != null)
                                                    familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                if (familiares == null)
                                                    familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);

                                                break;
                                            case 2:
                                                r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);
                                                if (datosPersonal != null)
                                                    familiares = await _garancheck.GetFamiliaLineaAsync(cedulaTemp, datosPersonal.CedulaConyuge, datosPersonal.NombreConyuge, datosPersonal.CedMadre, datosPersonal.NombreMadre, datosPersonal.CedPadre, datosPersonal.NombrePadre);

                                                if (familiares == null)
                                                    familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);

                                                break;
                                            case 3:
                                                registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(cedulaTemp);
                                                if (registroCivil != null)
                                                    cacheCivil = true;

                                                familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);
                                                break;
                                            case 4:
                                                registroCivil = await _garancheck.GetRegistroCivilLineaAsync(cedulaTemp);
                                                if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                }
                                                else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                    registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                    registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                    registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                }

                                                if (registroCivil == null)
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);

                                                if (registroCivil != null)
                                                    familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                if (familiares == null)
                                                    familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);

                                                break;
                                            default:
                                                r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);
                                                familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        if (registroCivil == null && r_garancheck == null)
                        {
                            var datosDetalleGarancheck = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleGarancheck != null)
                            {
                                cacheCivil = true;
                                r_garancheck = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(datosDetalleGarancheck);
                            }
                        }
                        //if (datosPersonal == null)
                        //{
                        //    var datosDetallePersonales = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Personales && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetallePersonales != null)
                        //    {
                        //        cachePersonal = true;
                        //        datosPersonal = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Personal>(datosDetallePersonales);
                        //    }
                        //}
                        //if (contactos == null)
                        //{
                        //    var datosDetalleContactos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Contactos && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleContactos != null)
                        //    {
                        //        cacheContactos = true;
                        //        contactos = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContactos);
                        //    }
                        //}
                        //if (contactosIess == null)
                        //{
                        //    var datosDetalleContactosIess = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleContactosIess != null)
                        //    {
                        //        cacheContactosIess = true;
                        //        contactosIess = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContactosIess);
                        //    }
                        //}
                        //if (familiares == null)
                        //{
                        //    var datosDetalleFamiliares = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Familiares && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleFamiliares != null)
                        //    {
                        //        cacheFamiliares = true;
                        //        familiares = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Familia>(datosDetalleFamiliares);
                        //    }
                        //}
                        //if (registroCivil == null)
                        //{
                        //    var datosDetalleRegistroCivil = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleRegistroCivil != null)
                        //    {
                        //        cacheRegistroCivil = true;
                        //        registroCivil = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(datosDetalleRegistroCivil);
                        //    }
                        //}

                        var nacionalidades = new List<NacionalidadViewModel>();
                        var pathNacionalidades = Path.Combine("wwwroot", "data", "dataNacionalidades.json");
                        var nacMadre = new NacionalidadViewModel();
                        var nacPadre = new NacionalidadViewModel();
                        if (System.IO.File.Exists(pathNacionalidades))
                        {
                            nacionalidades = JsonConvert.DeserializeObject<List<NacionalidadViewModel>>(System.IO.File.ReadAllText(pathNacionalidades));
                            if (datosPersonal != null && nacionalidades != null && nacionalidades.Any())
                            {
                                nacMadre = nacionalidades.FirstOrDefault(x => x.Codigo?.Trim().ToUpper() == datosPersonal.NacMadre?.Trim().ToUpper());
                                nacPadre = nacionalidades.FirstOrDefault(x => x.Codigo?.Trim().ToUpper() == datosPersonal.NacPadre?.Trim().ToUpper());

                                datosPersonal.NacPadre = nacPadre != null ? nacPadre.Nacionalidad : "DESCONOCIDO";
                                datosPersonal.NacMadre = nacMadre != null ? nacMadre.Nacionalidad : "DESCONOCIDO";
                            }
                        }

                        if (datosPersonal != null)
                        {
                            if (!string.IsNullOrWhiteSpace(datosPersonal.FechaExpedicion?.Trim()))
                                datosPersonal.FechaExpedicion = DateTime.TryParse(datosPersonal.FechaExpedicion, out _) ? DateTime.Parse(datosPersonal.FechaExpedicion).ToString("dd/MM/yyyy") : string.Empty;

                            if (!string.IsNullOrWhiteSpace(datosPersonal.FechaDefuncion?.Trim()))
                                datosPersonal.FechaDefuncion = DateTime.TryParse(datosPersonal.FechaDefuncion, out _) ? DateTime.Parse(datosPersonal.FechaDefuncion).ToString("dd/MM/yyyy") : string.Empty;
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Civil con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                    datos = new CivilApiMetodoViewModel()
                    {
                        General = r_garancheck,
                        Familiares = familiares,
                        Personal = datosPersonal,
                        Contactos = contactos,
                        RegistroCivil = registroCivil
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                    datos = JsonConvert.DeserializeObject<CivilApiMetodoViewModel>(System.IO.File.ReadAllText(pathCivil));
                }

                _logger.LogInformation("Fuente de Civil procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Civil. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidadoTemp = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.Cedula?.Trim()) && !string.IsNullOrEmpty(registroCivil.Nombre?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = registroCivil.Nombre?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = registroCivil.Cedula?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }

                            if (registroCivil != null && registroCivil.FechaCedulacion != default && string.IsNullOrEmpty(historial.FechaExpedicionCedula?.Trim()))
                            {
                                historial.FechaExpedicionCedula = registroCivil.FechaCedulacion.ToString("dd/MM/yyyy");
                                await _historiales.UpdateAsync(historial);
                            }

                            if (r_garancheck != null && !string.IsNullOrEmpty(r_garancheck.Identificacion?.Trim()) && !string.IsNullOrEmpty(r_garancheck.Nombres?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = r_garancheck.Nombres?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = r_garancheck.Identificacion?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }
                            if (historialConsolidadoTemp != null)
                            {
                                historialConsolidadoTemp.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidadoTemp);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Ciudadano,
                            Generado = datos.General != null,
                            Data = datos.General != null ? JsonConvert.SerializeObject(datos.General) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Familiares,
                            Generado = datos.Familiares != null,
                            Data = datos.Familiares != null ? JsonConvert.SerializeObject(datos.Familiares) : null,
                            Cache = cacheFamiliares,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Personales,
                            Generado = datos.Personal != null,
                            Data = datos.Personal != null ? JsonConvert.SerializeObject(datos.Personal) : null,
                            Cache = cachePersonal,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Contactos,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheContactos,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.RegistroCivil,
                            Generado = datos.RegistroCivil != null,
                            Data = datos.RegistroCivil != null ? JsonConvert.SerializeObject(datos.RegistroCivil) : null,
                            Cache = cacheRegistroCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.ContactosIess,
                            Generado = contactosIess != null,
                            Data = contactosIess != null ? JsonConvert.SerializeObject(contactosIess) : null,
                            Cache = cacheContactosIess,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Civil procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<BalancesApiMetodoViewModel> ObtenerReporteBalance(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var datos = new BalancesApiMetodoViewModel();
                var periodosBusqueda = new List<int>();
                var cacheBalance = false;
                var cacheAccionistas = false;
                var balancesMultiples = false;
                var soloEmpresasSimilares = false;
                var busquedaNoJuridico = false;
                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;

                Externos.Logica.Balances.Modelos.BalanceEmpresa r_balance = null;
                List<Externos.Logica.Balances.Modelos.BalanceEmpresa> r_balances = null;
                Externos.Logica.Balances.Modelos.DirectorioCompania directorioCompania = null;
                Externos.Logica.Balances.Modelos.IndicadoresCompania indicadoresCompania = null;
                List<AnalisisHorizontalViewModel> analisisHorizontal = null;
                List<Externos.Logica.Balances.Modelos.RepresentanteEmpresa> representantesEmpresas = null;
                List<Externos.Logica.Balances.Modelos.Accionista> accionistas = null;
                List<Externos.Logica.Balances.Modelos.AccionistaEmpresa> empresasAccionista = null;

                if (!_cache)
                {
                    var historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Balances identificación: {modelo.Identificacion}");
                        if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                        {
                            _logger.LogInformation($"Procesando Empresas Representante identificación: {modelo.Identificacion}");
                            representantesEmpresas = await _balances.GetRepresentantesEmpresasAsync(historialTemp.NombresPersona);
                            _logger.LogInformation($"Procesando Empresas Accionistas identificación: {modelo.Identificacion}");
                            empresasAccionista = await _balances.GetAccionistaEmpresasAsync(historialTemp.NombresPersona);
                            busquedaNoJuridico = true;
                        }
                        else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            directorioCompania = await _balances.GetDirectorioCompaniasAsync(modelo.Identificacion);
                            balancesMultiples = true;
                            var infoPeriodos = _configuration.GetSection("AppSettings:PeriodosDinamicos").Get<PeriodosDinamicosViewModel>();
                            if (infoPeriodos != null)
                            {
                                #region Consulta Balances
                                var ultimosPeriodos = infoPeriodos.Periodos.Select(m => m.Valor).ToList();
                                if (infoPeriodos.Activo)
                                {
                                    var periodoActual = DateTime.Now.Year;
                                    ultimosPeriodos = new List<int>();
                                    for (int i = 0; i < infoPeriodos.Frecuencia; i++)
                                        ultimosPeriodos.Add(periodoActual - i);
                                }
                                periodosBusqueda = ultimosPeriodos;

                                r_balances = new List<Externos.Logica.Balances.Modelos.BalanceEmpresa>();
                                foreach (var item in ultimosPeriodos)
                                {
                                    var periodoBalance = _balances.GetType().GetProperty("Periodo");
                                    if (periodoBalance != null)
                                        periodoBalance.SetValue(_balances, item);

                                    r_balance = await _balances.GetRespuestaAsync(modelo.Identificacion);
                                    if (r_balance != null)
                                    {
                                        indicadoresCompania = await _balances.GetIndicadoresCompaniasAsync(modelo.Identificacion, item);
                                        if (indicadoresCompania != null)
                                        {
                                            r_balance.Indices.LiquidezCorriente = indicadoresCompania.LiquidezCorriente != 0 || r_balance.Indices.LiquidezCorriente == indicadoresCompania.LiquidezCorriente ? (decimal)indicadoresCompania.LiquidezCorriente : r_balance.Indices.LiquidezCorriente;
                                            r_balance.Indices.PruebaAcida = indicadoresCompania.PruebaAcida != 0 || r_balance.Indices.PruebaAcida == indicadoresCompania.PruebaAcida ? (decimal)indicadoresCompania.PruebaAcida : r_balance.Indices.PruebaAcida;
                                            r_balance.Indices.EndeudamientoActivo = indicadoresCompania.EndeudamientoActivo != 0 || r_balance.Indices.EndeudamientoActivo == indicadoresCompania.EndeudamientoActivo ? (decimal)indicadoresCompania.EndeudamientoActivo : r_balance.Indices.EndeudamientoActivo;
                                            r_balance.Indices.CoberturaIntereses = indicadoresCompania.CoberturaInteres != 0 || r_balance.Indices.CoberturaIntereses == indicadoresCompania.CoberturaInteres ? (decimal)indicadoresCompania.CoberturaInteres : r_balance.Indices.CoberturaIntereses;
                                            r_balance.Indices.MargenBruto = indicadoresCompania.MargenBruto != 0 || r_balance.Indices.MargenBruto == indicadoresCompania.MargenBruto ? (decimal)indicadoresCompania.MargenBruto : r_balance.Indices.MargenBruto;
                                            r_balance.Indices.MargenOperacional = indicadoresCompania.MargenOperacional != 0 || r_balance.Indices.MargenOperacional == indicadoresCompania.MargenOperacional ? (decimal)indicadoresCompania.MargenOperacional : r_balance.Indices.MargenOperacional;
                                            r_balance.Indices.ROA = indicadoresCompania.Roa != 0 || r_balance.Indices.ROA == indicadoresCompania.Roa ? (decimal)indicadoresCompania.Roa : r_balance.Indices.ROA;
                                            r_balance.Indices.ROE = indicadoresCompania.Roe != 0 || r_balance.Indices.ROE == indicadoresCompania.Roe ? (decimal)indicadoresCompania.Roe : r_balance.Indices.ROE;
                                        }
                                        r_balances.Add(r_balance);
                                    }
                                }
                                #endregion Consulta Balances

                                #region Análisis Horizontal
                                analisisHorizontal = new List<AnalisisHorizontalViewModel>();
                                var analisisPeriodo = new List<AnalisisHorizontalViewModel>();
                                var agrupacion = r_balances.OrderByDescending(m => m.Periodo).GroupBy(m => m.Periodo).Take(3).ToList();
                                analisisPeriodo = agrupacion.Select(m => new { Analisis = m.FirstOrDefault() }).Select(item => new AnalisisHorizontalViewModel()
                                {
                                    Periodo = item.Analisis.Periodo,
                                    RUC = item.Analisis.RUC,
                                    RazonSocial = item.Analisis.RazonSocial,
                                    CodigoActividadEconomica = item.Analisis.CodigoActividadEconomica,
                                    ActividadEconomica = item.Analisis.ActividadEconomica,
                                    CIIU = item.Analisis.CIIU,
                                    Indices = new IndicesViewModel()
                                    {
                                        UtilidadBruta = item.Analisis.Indices.UtilidadBruta,
                                        UtilidadOperacional = item.Analisis.Indices.UtilidadOperacional,
                                        GananciaAntesDe15x100YImpuestos = item.Analisis.Indices.GananciaAntesDe15x100YImpuestos,
                                        GananciaAntesDeImpuestos = item.Analisis.Indices.GananciaAntesDeImpuestos,
                                        GananciaNeta = item.Analisis.Indices.GananciaNeta,
                                        EBITDA = item.Analisis.Indices.EBITDA,
                                        CapitalTrabajo = item.Analisis.Indices.CapitalTrabajo,
                                        MargenBruto = item.Analisis.Indices.MargenBruto,
                                        MargenOperacional = item.Analisis.Indices.MargenOperacional,
                                        MargenNeto = item.Analisis.Indices.MargenNeto,
                                        EndeudamientoActivo = item.Analisis.Indices.EndeudamientoActivo,
                                        ROA = item.Analisis.Indices.ROA,
                                        ROE = item.Analisis.Indices.ROE,
                                        CoberturaIntereses = item.Analisis.Indices.CoberturaIntereses,
                                        DiasInventario = item.Analisis.Indices.DiasInventario,
                                        PeriodoPromCobro = item.Analisis.Indices.PeriodoPromCobro,
                                        PeriodoPromPago = item.Analisis.Indices.PeriodoPromPago,
                                        PruebaAcida = item.Analisis.Indices.PruebaAcida,
                                        LiquidezCorriente = item.Analisis.Indices.LiquidezCorriente
                                    },
                                    Activos = new ActivosViewModel()
                                    {
                                        OtrosActivosCorrientes = item.Analisis.Activos.OtrosActivosCorrientes,
                                        OtrosActivosNoCorrientes = item.Analisis.Activos.OtrosActivosNoCorrientes,
                                        TotalActivoCorriente = item.Analisis.Activos.TotalActivoCorriente,
                                        TotalActivoNoCorriente = item.Analisis.Activos.TotalActivoNoCorriente,
                                        TotalActivo = item.Analisis.Activos.TotalActivo
                                    },
                                    Pasivos = new PasivosViewModel()
                                    {
                                        OtrosPasivosCorrientes = item.Analisis.Pasivos.OtrosPasivosCorrientes,
                                        OtrosPasivosNoCorrientes = item.Analisis.Pasivos.OtrosPasivosNoCorrientes,
                                        TotalPasivoCorriente = item.Analisis.Pasivos.TotalPasivoCorriente,
                                        TotalPasivoNoCorriente = item.Analisis.Pasivos.TotalPasivoNoCorriente,
                                        TotalPasivo = item.Analisis.Pasivos.TotalPasivo,
                                    },
                                    Patrimonio = new PatrimonioViewModel()
                                    {
                                        CapitalSuscrito = item.Analisis.Patrimonio.CapitalSuscrito,
                                        PatrimonioNeto = item.Analisis.Patrimonio.PatrimonioNeto,
                                        TotalPasivoPatrimonio = item.Analisis.Patrimonio.TotalPasivoPatrimonio,
                                    },
                                    Otros = new OtrosViewModel()
                                    {
                                        Inventarios = item.Analisis.Otros.Inventarios,
                                        EfectivoYCaja = item.Analisis.Otros.EfectivoYCaja,
                                        PropiedadPlantaEquipo = item.Analisis.Otros.PropiedadPlantaEquipo,
                                        AportesSociosFuturasCap = item.Analisis.Otros.AportesSociosFuturasCap,
                                        ReservaLegal = item.Analisis.Otros.ReservaLegal,
                                        Ventas = item.Analisis.Otros.Ventas,
                                        Servicios = item.Analisis.Otros.Servicios,
                                        TotalIngresos = item.Analisis.Otros.TotalIngresos,
                                        ImpuestoRenta = item.Analisis.Otros.ImpuestoRenta,
                                        CostoDeVentas = item.Analisis.Otros.CostoDeVentas,
                                        CostosOperacionales = item.Analisis.Otros.CostosOperacionales,
                                        IngresosOperacionales = item.Analisis.Otros.IngresosOperacionales,
                                        IngresosNoOperacionales = item.Analisis.Otros.IngresosNoOperacionales,
                                        OtrosIngresosNoOperacionales = item.Analisis.Otros.OtrosIngresosNoOperacionales,
                                        CxCComercialesTerceros = item.Analisis.Otros.CxCComercialesTerceros,
                                        CxCAccionistasYRelacionados = item.Analisis.Otros.CxCAccionistasYRelacionados,
                                        ProvisionesCorrientes = item.Analisis.Otros.ProvisionesCorrientes,
                                        ProvisionesNoCorrientes = item.Analisis.Otros.ProvisionesNoCorrientes,
                                        ProvisionesBeneficiosEmpleados = item.Analisis.Otros.ProvisionesBeneficiosEmpleados,
                                        GastosOperacionales = item.Analisis.Otros.GastosOperacionales,
                                        GastosFinancieros = item.Analisis.Otros.GastosFinancieros,
                                        P15x100Trabajadores = item.Analisis.Otros.P15x100Trabajadores,
                                        CxPProveedoresTerceros = item.Analisis.Otros.CxPProveedoresTerceros,
                                        CxPAccionistasYRelacionados = item.Analisis.Otros.CxPAccionistasYRelacionados,
                                        ObFinancierasCortoPlazo = item.Analisis.Otros.ObFinancierasCortoPlazo,
                                        OBFinancierasLargoPlazo = item.Analisis.Otros.OBFinancierasLargoPlazo,
                                        Depreciaciones = item.Analisis.Otros.Depreciaciones,
                                        UtilidadEjercicio = item.Analisis.Otros.UtilidadEjercicio,
                                        PerdidaEjercicio = item.Analisis.Otros.PerdidaEjercicio,
                                        ResultadosAcumulados = item.Analisis.Otros.ResultadosAcumulados,
                                    }
                                }).ToList();
                                var agrupacionAnalisis = analisisPeriodo.OrderByDescending(m => m.Periodo).GroupBy(m => m.Periodo).Take(3).ToList();
                                if (agrupacionAnalisis.Count() > 1)
                                {
                                    for (int i = agrupacionAnalisis.Count() - 1; i > 0; i--)
                                    {
                                        var anioUno = agrupacionAnalisis[i].Select(m => m).FirstOrDefault();
                                        var anioDos = agrupacionAnalisis[i - 1].Select(m => m).FirstOrDefault();
                                        var analisis = new AnalisisHorizontalViewModel()
                                        {
                                            Periodo = i,
                                            Indices = new IndicesViewModel()
                                            {
                                                UtilidadBruta = anioUno.Indices.UtilidadBruta == 0 && anioUno.Indices.UtilidadBruta != anioDos.Indices.UtilidadBruta ? 100 : anioUno.Indices.UtilidadBruta != 0 && anioUno.Indices.UtilidadBruta != anioDos.Indices.UtilidadBruta ? ((anioDos.Indices.UtilidadBruta - anioUno.Indices.UtilidadBruta) / Math.Abs(anioUno.Indices.UtilidadBruta.Value)) * 100 : null,
                                                UtilidadOperacional = anioUno.Indices.UtilidadOperacional == 0 && anioUno.Indices.UtilidadOperacional != anioDos.Indices.UtilidadOperacional ? 100 : anioUno.Indices.UtilidadOperacional != 0 && anioUno.Indices.UtilidadOperacional != anioDos.Indices.UtilidadOperacional ? ((anioDos.Indices.UtilidadOperacional - anioUno.Indices.UtilidadOperacional) / Math.Abs(anioUno.Indices.UtilidadOperacional.Value)) * 100 : null,
                                                GananciaAntesDe15x100YImpuestos = anioUno.Indices.GananciaAntesDe15x100YImpuestos == 0 && anioUno.Indices.GananciaAntesDe15x100YImpuestos != anioDos.Indices.GananciaAntesDe15x100YImpuestos ? 100 : anioUno.Indices.GananciaAntesDe15x100YImpuestos != 0 && anioUno.Indices.GananciaAntesDe15x100YImpuestos != anioDos.Indices.GananciaAntesDe15x100YImpuestos ? ((anioDos.Indices.GananciaAntesDe15x100YImpuestos - anioUno.Indices.GananciaAntesDe15x100YImpuestos) / Math.Abs(anioUno.Indices.GananciaAntesDe15x100YImpuestos.Value)) * 100 : null,
                                                GananciaAntesDeImpuestos = anioUno.Indices.GananciaAntesDeImpuestos == 0 && anioUno.Indices.GananciaAntesDeImpuestos != anioDos.Indices.GananciaAntesDeImpuestos ? 100 : anioUno.Indices.GananciaAntesDeImpuestos != 0 && anioUno.Indices.GananciaAntesDeImpuestos != anioDos.Indices.GananciaAntesDeImpuestos ? ((anioDos.Indices.GananciaAntesDeImpuestos - anioUno.Indices.GananciaAntesDeImpuestos) / Math.Abs(anioUno.Indices.GananciaAntesDeImpuestos.Value)) * 100 : null,
                                                GananciaNeta = anioUno.Indices.GananciaNeta == 0 && anioUno.Indices.GananciaNeta != anioDos.Indices.GananciaNeta ? 100 : anioUno.Indices.GananciaNeta != 0 && anioUno.Indices.GananciaNeta != anioDos.Indices.GananciaNeta ? ((anioDos.Indices.GananciaNeta - anioUno.Indices.GananciaNeta) / Math.Abs(anioUno.Indices.GananciaNeta.Value)) * 100 : null,
                                                EBITDA = anioUno.Indices.EBITDA == 0 && anioUno.Indices.EBITDA != anioDos.Indices.EBITDA ? 100 : anioUno.Indices.EBITDA != 0 && anioUno.Indices.EBITDA != anioDos.Indices.EBITDA ? ((anioDos.Indices.EBITDA - anioUno.Indices.EBITDA) / Math.Abs(anioUno.Indices.EBITDA.Value)) * 100 : null,
                                                CapitalTrabajo = anioUno.Indices.CapitalTrabajo == 0 && anioUno.Indices.CapitalTrabajo != anioDos.Indices.CapitalTrabajo ? 100 : anioUno.Indices.CapitalTrabajo != 0 && anioUno.Indices.CapitalTrabajo != anioDos.Indices.CapitalTrabajo ? ((anioDos.Indices.CapitalTrabajo - anioUno.Indices.CapitalTrabajo) / Math.Abs(anioUno.Indices.CapitalTrabajo.Value)) * 100 : null,
                                                MargenBruto = anioUno.Indices.MargenBruto == 0 && anioUno.Indices.MargenBruto != anioDos.Indices.MargenBruto ? 100 : anioUno.Indices.MargenBruto != 0 && anioUno.Indices.MargenBruto != anioDos.Indices.MargenBruto ? ((anioDos.Indices.MargenBruto - anioUno.Indices.MargenBruto) / Math.Abs(anioUno.Indices.MargenBruto.Value)) * 100 : null,
                                                MargenOperacional = anioUno.Indices.MargenOperacional == 0 && anioUno.Indices.MargenOperacional != anioDos.Indices.MargenOperacional ? 100 : anioUno.Indices.MargenOperacional != 0 && anioUno.Indices.MargenOperacional != anioDos.Indices.MargenOperacional ? ((anioDos.Indices.MargenOperacional - anioUno.Indices.MargenOperacional) / Math.Abs(anioUno.Indices.MargenOperacional.Value)) * 100 : null,
                                                MargenNeto = anioUno.Indices.MargenNeto == 0 && anioUno.Indices.MargenNeto != anioDos.Indices.MargenNeto ? 100 : anioUno.Indices.MargenNeto != 0 && anioUno.Indices.MargenNeto != anioDos.Indices.MargenNeto ? ((anioDos.Indices.MargenNeto - anioUno.Indices.MargenNeto) / Math.Abs(anioUno.Indices.MargenNeto.Value)) * 100 : null,
                                                EndeudamientoActivo = anioUno.Indices.EndeudamientoActivo == 0 && anioUno.Indices.EndeudamientoActivo != anioDos.Indices.EndeudamientoActivo ? 100 : anioUno.Indices.EndeudamientoActivo != 0 && anioUno.Indices.EndeudamientoActivo != anioDos.Indices.EndeudamientoActivo ? ((anioDos.Indices.EndeudamientoActivo - anioUno.Indices.EndeudamientoActivo) / Math.Abs(anioUno.Indices.EndeudamientoActivo.Value)) * 100 : null,
                                                ROA = anioUno.Indices.ROA == 0 && anioUno.Indices.ROA != anioDos.Indices.ROA ? 100 : anioUno.Indices.ROA != 0 && anioUno.Indices.ROA != anioDos.Indices.ROA ? ((anioDos.Indices.ROA - anioUno.Indices.ROA) / Math.Abs(anioUno.Indices.ROA.Value)) * 100 : null,
                                                ROE = anioUno.Indices.ROE == 0 && anioUno.Indices.ROE != anioDos.Indices.ROE ? 100 : anioUno.Indices.ROE != 0 && anioUno.Indices.ROE != anioDos.Indices.ROE ? ((anioDos.Indices.ROE - anioUno.Indices.ROE) / Math.Abs(anioUno.Indices.ROE.Value)) * 100 : null,
                                                CoberturaIntereses = anioUno.Indices.CoberturaIntereses == 0 && anioUno.Indices.CoberturaIntereses != anioDos.Indices.CoberturaIntereses ? 100 : anioUno.Indices.CoberturaIntereses != 0 && anioUno.Indices.CoberturaIntereses != anioDos.Indices.CoberturaIntereses ? ((anioDos.Indices.CoberturaIntereses - anioUno.Indices.CoberturaIntereses) / Math.Abs(anioUno.Indices.CoberturaIntereses.Value)) * 100 : null,
                                                DiasInventario = anioUno.Indices.DiasInventario == 0 && anioUno.Indices.DiasInventario != anioDos.Indices.DiasInventario ? 100 : anioUno.Indices.DiasInventario != 0 && anioUno.Indices.DiasInventario != anioDos.Indices.DiasInventario ? ((anioDos.Indices.DiasInventario - anioUno.Indices.DiasInventario) / Math.Abs(anioUno.Indices.DiasInventario.Value)) * 100 : null,
                                                PeriodoPromCobro = anioUno.Indices.PeriodoPromCobro == 0 && anioUno.Indices.PeriodoPromCobro != anioDos.Indices.PeriodoPromCobro ? 100 : anioUno.Indices.PeriodoPromCobro != 0 && anioUno.Indices.PeriodoPromCobro != anioDos.Indices.PeriodoPromCobro ? ((anioDos.Indices.PeriodoPromCobro - anioUno.Indices.PeriodoPromCobro) / Math.Abs(anioUno.Indices.PeriodoPromCobro.Value)) * 100 : null,
                                                PeriodoPromPago = anioUno.Indices.PeriodoPromPago == 0 && anioUno.Indices.PeriodoPromPago != anioDos.Indices.PeriodoPromPago ? 100 : anioUno.Indices.PeriodoPromPago != 0 && anioUno.Indices.PeriodoPromPago != anioDos.Indices.PeriodoPromPago ? ((anioDos.Indices.PeriodoPromPago - anioUno.Indices.PeriodoPromPago) / Math.Abs(anioUno.Indices.PeriodoPromPago.Value)) * 100 : null,
                                                PruebaAcida = anioUno.Indices.PruebaAcida == 0 && anioUno.Indices.PruebaAcida != anioDos.Indices.PruebaAcida ? 100 : anioUno.Indices.PruebaAcida != 0 && anioUno.Indices.PruebaAcida != anioDos.Indices.PruebaAcida ? ((anioDos.Indices.PruebaAcida - anioUno.Indices.PruebaAcida) / Math.Abs(anioUno.Indices.PruebaAcida.Value)) * 100 : null,
                                                LiquidezCorriente = anioUno.Indices.LiquidezCorriente == 0 && anioUno.Indices.LiquidezCorriente != anioDos.Indices.LiquidezCorriente ? 100 : anioUno.Indices.LiquidezCorriente != 0 && anioUno.Indices.LiquidezCorriente != anioDos.Indices.LiquidezCorriente ? ((anioDos.Indices.LiquidezCorriente - anioUno.Indices.LiquidezCorriente) / Math.Abs(anioUno.Indices.LiquidezCorriente.Value)) * 100 : null
                                            },
                                            Activos = new ActivosViewModel()
                                            {
                                                OtrosActivosCorrientes = anioUno.Activos.OtrosActivosCorrientes == 0 && anioUno.Activos.OtrosActivosCorrientes != anioDos.Activos.OtrosActivosCorrientes ? 100 : anioUno.Activos.OtrosActivosCorrientes != 0 && anioUno.Activos.OtrosActivosCorrientes != anioDos.Activos.OtrosActivosCorrientes ? ((anioDos.Activos.OtrosActivosCorrientes - anioUno.Activos.OtrosActivosCorrientes) / Math.Abs(anioUno.Activos.OtrosActivosCorrientes.Value)) * 100 : null,
                                                OtrosActivosNoCorrientes = anioUno.Activos.OtrosActivosNoCorrientes == 0 && anioUno.Activos.OtrosActivosNoCorrientes != anioDos.Activos.OtrosActivosNoCorrientes ? 100 : anioUno.Activos.OtrosActivosNoCorrientes != 0 && anioUno.Activos.OtrosActivosNoCorrientes != anioDos.Activos.OtrosActivosNoCorrientes ? ((anioDos.Activos.OtrosActivosNoCorrientes - anioUno.Activos.OtrosActivosNoCorrientes) / Math.Abs(anioUno.Activos.OtrosActivosNoCorrientes.Value)) * 100 : null,
                                                TotalActivoCorriente = anioUno.Activos.TotalActivoCorriente == 0 && anioUno.Activos.TotalActivoCorriente != anioDos.Activos.TotalActivoCorriente ? 100 : anioUno.Activos.TotalActivoCorriente != 0 && anioUno.Activos.TotalActivoCorriente != anioDos.Activos.TotalActivoCorriente ? ((anioDos.Activos.TotalActivoCorriente - anioUno.Activos.TotalActivoCorriente) / Math.Abs(anioUno.Activos.TotalActivoCorriente.Value)) * 100 : null,
                                                TotalActivoNoCorriente = anioUno.Activos.TotalActivoNoCorriente == 0 && anioUno.Activos.TotalActivoNoCorriente != anioDos.Activos.TotalActivoNoCorriente ? 100 : anioUno.Activos.TotalActivoNoCorriente != 0 && anioUno.Activos.TotalActivoNoCorriente != anioDos.Activos.TotalActivoNoCorriente ? ((anioDos.Activos.TotalActivoNoCorriente - anioUno.Activos.TotalActivoNoCorriente) / Math.Abs(anioUno.Activos.TotalActivoNoCorriente.Value)) * 100 : null,
                                                TotalActivo = anioUno.Activos.TotalActivo == 0 && anioUno.Activos.TotalActivo != anioDos.Activos.TotalActivo ? 100 : anioUno.Activos.TotalActivo != 0 && anioUno.Activos.TotalActivo != anioDos.Activos.TotalActivo ? ((anioDos.Activos.TotalActivo - anioUno.Activos.TotalActivo) / Math.Abs(anioUno.Activos.TotalActivo.Value)) * 100 : 0
                                            },
                                            Pasivos = new PasivosViewModel()
                                            {
                                                OtrosPasivosCorrientes = anioUno.Pasivos.OtrosPasivosCorrientes == 0 && anioUno.Pasivos.OtrosPasivosCorrientes != anioDos.Pasivos.OtrosPasivosCorrientes ? 100 : anioUno.Pasivos.OtrosPasivosCorrientes != 0 && anioUno.Pasivos.OtrosPasivosCorrientes != anioDos.Pasivos.OtrosPasivosCorrientes ? ((anioDos.Pasivos.OtrosPasivosCorrientes - anioUno.Pasivos.OtrosPasivosCorrientes) / Math.Abs(anioUno.Pasivos.OtrosPasivosCorrientes.Value)) * 100 : null,
                                                OtrosPasivosNoCorrientes = anioUno.Pasivos.OtrosPasivosNoCorrientes == 0 && anioUno.Pasivos.OtrosPasivosNoCorrientes != anioDos.Pasivos.OtrosPasivosNoCorrientes ? 100 : anioUno.Pasivos.OtrosPasivosNoCorrientes != 0 && anioUno.Pasivos.OtrosPasivosNoCorrientes != anioDos.Pasivos.OtrosPasivosNoCorrientes ? ((anioDos.Pasivos.OtrosPasivosNoCorrientes - anioUno.Pasivos.OtrosPasivosNoCorrientes) / Math.Abs(anioUno.Pasivos.OtrosPasivosNoCorrientes.Value)) * 100 : null,
                                                TotalPasivoCorriente = anioUno.Pasivos.TotalPasivoCorriente == 0 && anioUno.Pasivos.TotalPasivoCorriente != anioDos.Pasivos.TotalPasivoCorriente ? 100 : anioUno.Pasivos.TotalPasivoCorriente != 0 && anioUno.Pasivos.TotalPasivoCorriente != anioDos.Pasivos.TotalPasivoCorriente ? ((anioDos.Pasivos.TotalPasivoCorriente - anioUno.Pasivos.TotalPasivoCorriente) / Math.Abs(anioUno.Pasivos.TotalPasivoCorriente.Value)) * 100 : null,
                                                TotalPasivoNoCorriente = anioUno.Pasivos.TotalPasivoNoCorriente == 0 && anioUno.Pasivos.TotalPasivoNoCorriente != anioDos.Pasivos.TotalPasivoNoCorriente ? 100 : anioUno.Pasivos.TotalPasivoNoCorriente != 0 && anioUno.Pasivos.TotalPasivoNoCorriente != anioDos.Pasivos.TotalPasivoNoCorriente ? ((anioDos.Pasivos.TotalPasivoNoCorriente - anioUno.Pasivos.TotalPasivoNoCorriente) / Math.Abs(anioUno.Pasivos.TotalPasivoNoCorriente.Value)) * 100 : null,
                                                TotalPasivo = anioUno.Pasivos.TotalPasivo == 0 && anioUno.Pasivos.TotalPasivo != anioDos.Pasivos.TotalPasivo ? 100 : anioUno.Pasivos.TotalPasivo != 0 && anioUno.Pasivos.TotalPasivo != anioDos.Pasivos.TotalPasivo ? ((anioDos.Pasivos.TotalPasivo - anioUno.Pasivos.TotalPasivo) / Math.Abs(anioUno.Pasivos.TotalPasivo.Value)) * 100 : 0
                                            },
                                            Patrimonio = new PatrimonioViewModel()
                                            {
                                                CapitalSuscrito = anioUno.Patrimonio.CapitalSuscrito == 0 && anioUno.Patrimonio.CapitalSuscrito != anioDos.Patrimonio.CapitalSuscrito ? 100 : anioUno.Patrimonio.CapitalSuscrito != 0 && anioUno.Patrimonio.CapitalSuscrito != anioDos.Patrimonio.CapitalSuscrito ? ((anioDos.Patrimonio.CapitalSuscrito - anioUno.Patrimonio.CapitalSuscrito) / Math.Abs(anioUno.Patrimonio.CapitalSuscrito.Value)) * 100 : null,
                                                PatrimonioNeto = anioUno.Patrimonio.PatrimonioNeto == 0 && anioUno.Patrimonio.PatrimonioNeto != anioDos.Patrimonio.PatrimonioNeto ? 100 : anioUno.Patrimonio.PatrimonioNeto != 0 && anioUno.Patrimonio.PatrimonioNeto != anioDos.Patrimonio.PatrimonioNeto ? ((anioDos.Patrimonio.PatrimonioNeto - anioUno.Patrimonio.PatrimonioNeto) / Math.Abs(anioUno.Patrimonio.PatrimonioNeto.Value)) * 100 : null,
                                                TotalPasivoPatrimonio = anioUno.Patrimonio.TotalPasivoPatrimonio == 0 && anioUno.Patrimonio.TotalPasivoPatrimonio != anioDos.Patrimonio.TotalPasivoPatrimonio ? 100 : anioUno.Patrimonio.TotalPasivoPatrimonio != 0 && anioUno.Patrimonio.TotalPasivoPatrimonio != anioDos.Patrimonio.TotalPasivoPatrimonio ? ((anioDos.Patrimonio.TotalPasivoPatrimonio - anioUno.Patrimonio.TotalPasivoPatrimonio) / Math.Abs(anioUno.Patrimonio.TotalPasivoPatrimonio.Value)) * 100 : null,
                                            },
                                            Otros = new OtrosViewModel()
                                            {
                                                Inventarios = anioUno.Otros.Inventarios == 0 && anioUno.Otros.Inventarios != anioDos.Otros.Inventarios ? 100 : anioUno.Otros.Inventarios != 0 && anioUno.Otros.Inventarios != anioDos.Otros.Inventarios ? ((anioDos.Otros.Inventarios - anioUno.Otros.Inventarios) / Math.Abs(anioUno.Otros.Inventarios.Value)) * 100 : null,
                                                EfectivoYCaja = anioUno.Otros.EfectivoYCaja == 0 && anioUno.Otros.EfectivoYCaja != anioDos.Otros.EfectivoYCaja ? 100 : anioUno.Otros.EfectivoYCaja != 0 && anioUno.Otros.EfectivoYCaja != anioDos.Otros.EfectivoYCaja ? ((anioDos.Otros.EfectivoYCaja - anioUno.Otros.EfectivoYCaja) / Math.Abs(anioUno.Otros.EfectivoYCaja.Value)) * 100 : null,
                                                PropiedadPlantaEquipo = anioUno.Otros.PropiedadPlantaEquipo == 0 && anioUno.Otros.PropiedadPlantaEquipo != anioDos.Otros.PropiedadPlantaEquipo ? 100 : anioUno.Otros.PropiedadPlantaEquipo != 0 && anioUno.Otros.PropiedadPlantaEquipo != anioDos.Otros.PropiedadPlantaEquipo ? ((anioDos.Otros.PropiedadPlantaEquipo - anioUno.Otros.PropiedadPlantaEquipo) / Math.Abs(anioUno.Otros.PropiedadPlantaEquipo.Value)) * 100 : null,
                                                AportesSociosFuturasCap = anioUno.Otros.AportesSociosFuturasCap == 0 && anioUno.Otros.AportesSociosFuturasCap != anioDos.Otros.AportesSociosFuturasCap ? 100 : anioUno.Otros.AportesSociosFuturasCap != 0 && anioUno.Otros.AportesSociosFuturasCap != anioDos.Otros.AportesSociosFuturasCap ? ((anioDos.Otros.AportesSociosFuturasCap - anioUno.Otros.AportesSociosFuturasCap) / Math.Abs(anioUno.Otros.AportesSociosFuturasCap.Value)) * 100 : null,
                                                ReservaLegal = anioUno.Otros.ReservaLegal == 0 && anioUno.Otros.ReservaLegal != anioDos.Otros.ReservaLegal ? 100 : anioUno.Otros.ReservaLegal != 0 && anioUno.Otros.ReservaLegal != anioDos.Otros.ReservaLegal ? ((anioDos.Otros.ReservaLegal - anioUno.Otros.ReservaLegal) / Math.Abs(anioUno.Otros.ReservaLegal.Value)) * 100 : null,
                                                Ventas = anioUno.Otros.Ventas == 0 && anioUno.Otros.Ventas != anioDos.Otros.Ventas ? 100 : anioUno.Otros.Ventas != 0 && anioUno.Otros.Ventas != anioDos.Otros.Ventas ? ((anioDos.Otros.Ventas - anioUno.Otros.Ventas) / Math.Abs(anioUno.Otros.Ventas.Value)) * 100 : null,
                                                Servicios = anioUno.Otros.Servicios == 0 && anioUno.Otros.Servicios != anioDos.Otros.Servicios ? 100 : anioUno.Otros.Servicios != 0 && anioUno.Otros.Servicios != anioDos.Otros.Servicios ? ((anioDos.Otros.Servicios - anioUno.Otros.Servicios) / Math.Abs(anioUno.Otros.Servicios.Value)) * 100 : null,
                                                TotalIngresos = anioUno.Otros.TotalIngresos == 0 && anioUno.Otros.TotalIngresos != anioDos.Otros.TotalIngresos ? 100 : anioUno.Otros.TotalIngresos != 0 && anioUno.Otros.TotalIngresos != anioDos.Otros.TotalIngresos ? ((anioDos.Otros.TotalIngresos - anioUno.Otros.TotalIngresos) / Math.Abs(anioUno.Otros.TotalIngresos.Value)) * 100 : null,
                                                ImpuestoRenta = anioUno.Otros.ImpuestoRenta == 0 && anioUno.Otros.ImpuestoRenta != anioDos.Otros.ImpuestoRenta ? 100 : anioUno.Otros.ImpuestoRenta != 0 && anioUno.Otros.ImpuestoRenta != anioDos.Otros.ImpuestoRenta ? ((anioDos.Otros.ImpuestoRenta - anioUno.Otros.ImpuestoRenta) / Math.Abs(anioUno.Otros.ImpuestoRenta.Value)) * 100 : null,
                                                CostoDeVentas = anioUno.Otros.CostoDeVentas == 0 && anioUno.Otros.CostoDeVentas != anioDos.Otros.CostoDeVentas ? 100 : anioUno.Otros.CostoDeVentas != 0 && anioUno.Otros.CostoDeVentas != anioDos.Otros.CostoDeVentas ? ((anioDos.Otros.CostoDeVentas - anioUno.Otros.CostoDeVentas) / Math.Abs(anioUno.Otros.CostoDeVentas.Value)) * 100 : null,
                                                CostosOperacionales = anioUno.Otros.CostosOperacionales == 0 && anioUno.Otros.CostosOperacionales != anioDos.Otros.CostosOperacionales ? 100 : anioUno.Otros.CostosOperacionales != 0 && anioUno.Otros.CostosOperacionales != anioDos.Otros.CostosOperacionales ? ((anioDos.Otros.CostosOperacionales - anioUno.Otros.CostosOperacionales) / Math.Abs(anioUno.Otros.CostosOperacionales.Value)) * 100 : null,
                                                IngresosOperacionales = anioUno.Otros.IngresosOperacionales == 0 && anioUno.Otros.IngresosOperacionales != anioDos.Otros.IngresosOperacionales ? 100 : anioUno.Otros.IngresosOperacionales != 0 && anioUno.Otros.IngresosOperacionales != anioDos.Otros.IngresosOperacionales ? ((anioDos.Otros.IngresosOperacionales - anioUno.Otros.IngresosOperacionales) / Math.Abs(anioUno.Otros.IngresosOperacionales.Value)) * 100 : null,
                                                IngresosNoOperacionales = anioUno.Otros.IngresosNoOperacionales == 0 && anioUno.Otros.IngresosNoOperacionales != anioDos.Otros.IngresosNoOperacionales ? 100 : anioUno.Otros.IngresosNoOperacionales != 0 && anioUno.Otros.IngresosNoOperacionales != anioDos.Otros.IngresosNoOperacionales ? ((anioDos.Otros.IngresosNoOperacionales - anioUno.Otros.IngresosNoOperacionales) / Math.Abs(anioUno.Otros.IngresosNoOperacionales.Value)) * 100 : null,
                                                OtrosIngresosNoOperacionales = anioUno.Otros.OtrosIngresosNoOperacionales == 0 && anioUno.Otros.OtrosIngresosNoOperacionales != anioDos.Otros.OtrosIngresosNoOperacionales ? 100 : anioUno.Otros.OtrosIngresosNoOperacionales != 0 && anioUno.Otros.OtrosIngresosNoOperacionales != anioDos.Otros.OtrosIngresosNoOperacionales ? ((anioDos.Otros.OtrosIngresosNoOperacionales - anioUno.Otros.OtrosIngresosNoOperacionales) / Math.Abs(anioUno.Otros.OtrosIngresosNoOperacionales.Value)) * 100 : null,
                                                CxCComercialesTerceros = anioUno.Otros.CxCComercialesTerceros == 0 && anioUno.Otros.CxCComercialesTerceros != anioDos.Otros.CxCComercialesTerceros ? 100 : anioUno.Otros.CxCComercialesTerceros != 0 && anioUno.Otros.CxCComercialesTerceros != anioDos.Otros.CxCComercialesTerceros ? ((anioDos.Otros.CxCComercialesTerceros - anioUno.Otros.CxCComercialesTerceros) / Math.Abs(anioUno.Otros.CxCComercialesTerceros.Value)) * 100 : null,
                                                CxCAccionistasYRelacionados = anioUno.Otros.CxCAccionistasYRelacionados == 0 && anioUno.Otros.CxCAccionistasYRelacionados != anioDos.Otros.CxCAccionistasYRelacionados ? 100 : anioUno.Otros.CxCAccionistasYRelacionados != 0 && anioUno.Otros.CxCAccionistasYRelacionados != anioDos.Otros.CxCAccionistasYRelacionados ? ((anioDos.Otros.CxCAccionistasYRelacionados - anioUno.Otros.CxCAccionistasYRelacionados) / Math.Abs(anioUno.Otros.CxCAccionistasYRelacionados.Value)) * 100 : null,
                                                ProvisionesCorrientes = anioUno.Otros.ProvisionesCorrientes == 0 && anioUno.Otros.ProvisionesCorrientes != anioDos.Otros.ProvisionesCorrientes ? 100 : anioUno.Otros.ProvisionesCorrientes != 0 && anioUno.Otros.ProvisionesCorrientes != anioDos.Otros.ProvisionesCorrientes ? ((anioDos.Otros.ProvisionesCorrientes - anioUno.Otros.ProvisionesCorrientes) / Math.Abs(anioUno.Otros.ProvisionesCorrientes.Value)) * 100 : null,
                                                ProvisionesNoCorrientes = anioUno.Otros.ProvisionesNoCorrientes == 0 && anioUno.Otros.ProvisionesNoCorrientes != anioDos.Otros.ProvisionesNoCorrientes ? 100 : anioUno.Otros.ProvisionesNoCorrientes != 0 && anioUno.Otros.ProvisionesNoCorrientes != anioDos.Otros.ProvisionesNoCorrientes ? ((anioDos.Otros.ProvisionesNoCorrientes - anioUno.Otros.ProvisionesNoCorrientes) / Math.Abs(anioUno.Otros.ProvisionesNoCorrientes.Value)) * 100 : null,
                                                ProvisionesBeneficiosEmpleados = anioUno.Otros.ProvisionesBeneficiosEmpleados == 0 && anioUno.Otros.ProvisionesBeneficiosEmpleados != anioDos.Otros.ProvisionesBeneficiosEmpleados ? 100 : anioUno.Otros.ProvisionesBeneficiosEmpleados != 0 && anioUno.Otros.ProvisionesBeneficiosEmpleados != anioDos.Otros.ProvisionesBeneficiosEmpleados ? ((anioDos.Otros.ProvisionesBeneficiosEmpleados - anioUno.Otros.ProvisionesBeneficiosEmpleados) / Math.Abs(anioUno.Otros.ProvisionesBeneficiosEmpleados.Value)) * 100 : null,
                                                GastosOperacionales = anioUno.Otros.GastosOperacionales == 0 && anioUno.Otros.GastosOperacionales != anioDos.Otros.GastosOperacionales ? 100 : anioUno.Otros.GastosOperacionales != 0 && anioUno.Otros.GastosOperacionales != anioDos.Otros.GastosOperacionales ? ((anioDos.Otros.GastosOperacionales - anioUno.Otros.GastosOperacionales) / Math.Abs(anioUno.Otros.GastosOperacionales.Value)) * 100 : null,
                                                GastosFinancieros = anioUno.Otros.GastosFinancieros == 0 && anioUno.Otros.GastosFinancieros != anioDos.Otros.GastosFinancieros ? 100 : anioUno.Otros.GastosFinancieros != 0 && anioUno.Otros.GastosFinancieros != anioDos.Otros.GastosFinancieros ? ((anioDos.Otros.GastosFinancieros - anioUno.Otros.GastosFinancieros) / Math.Abs(anioUno.Otros.GastosFinancieros.Value)) * 100 : null,
                                                P15x100Trabajadores = anioUno.Otros.P15x100Trabajadores == 0 && anioUno.Otros.P15x100Trabajadores != anioDos.Otros.P15x100Trabajadores ? 100 : anioUno.Otros.P15x100Trabajadores != 0 && anioUno.Otros.P15x100Trabajadores != anioDos.Otros.P15x100Trabajadores ? ((anioDos.Otros.P15x100Trabajadores - anioUno.Otros.P15x100Trabajadores) / Math.Abs(anioUno.Otros.P15x100Trabajadores.Value)) * 100 : null,
                                                CxPProveedoresTerceros = anioUno.Otros.CxPProveedoresTerceros == 0 && anioUno.Otros.CxPProveedoresTerceros != anioDos.Otros.CxPProveedoresTerceros ? 100 : anioUno.Otros.CxPProveedoresTerceros != 0 && anioUno.Otros.CxPProveedoresTerceros != anioDos.Otros.CxPProveedoresTerceros ? ((anioDos.Otros.CxPProveedoresTerceros - anioUno.Otros.CxPProveedoresTerceros) / Math.Abs(anioUno.Otros.CxPProveedoresTerceros.Value)) * 100 : null,
                                                CxPAccionistasYRelacionados = anioUno.Otros.CxPAccionistasYRelacionados == 0 && anioUno.Otros.CxPAccionistasYRelacionados != anioDos.Otros.CxPAccionistasYRelacionados ? 100 : anioUno.Otros.CxPAccionistasYRelacionados != 0 && anioUno.Otros.CxPAccionistasYRelacionados != anioDos.Otros.CxPAccionistasYRelacionados ? ((anioDos.Otros.CxPAccionistasYRelacionados - anioUno.Otros.CxPAccionistasYRelacionados) / Math.Abs(anioUno.Otros.CxPAccionistasYRelacionados.Value)) * 100 : null,
                                                ObFinancierasCortoPlazo = anioUno.Otros.ObFinancierasCortoPlazo == 0 && anioUno.Otros.ObFinancierasCortoPlazo != anioDos.Otros.ObFinancierasCortoPlazo ? 100 : anioUno.Otros.ObFinancierasCortoPlazo != 0 && anioUno.Otros.ObFinancierasCortoPlazo != anioDos.Otros.ObFinancierasCortoPlazo ? ((anioDos.Otros.ObFinancierasCortoPlazo - anioUno.Otros.ObFinancierasCortoPlazo) / Math.Abs(anioUno.Otros.ObFinancierasCortoPlazo.Value)) * 100 : null,
                                                OBFinancierasLargoPlazo = anioUno.Otros.OBFinancierasLargoPlazo == 0 && anioUno.Otros.OBFinancierasLargoPlazo != anioDos.Otros.OBFinancierasLargoPlazo ? 100 : anioUno.Otros.OBFinancierasLargoPlazo != 0 && anioUno.Otros.OBFinancierasLargoPlazo != anioDos.Otros.OBFinancierasLargoPlazo ? ((anioDos.Otros.OBFinancierasLargoPlazo - anioUno.Otros.OBFinancierasLargoPlazo) / Math.Abs(anioUno.Otros.OBFinancierasLargoPlazo.Value)) * 100 : null,
                                                Depreciaciones = anioUno.Otros.Depreciaciones == 0 && anioUno.Otros.Depreciaciones != anioDos.Otros.Depreciaciones ? 100 : anioUno.Otros.Depreciaciones != 0 && anioUno.Otros.Depreciaciones != anioDos.Otros.Depreciaciones ? ((anioDos.Otros.Depreciaciones - anioUno.Otros.Depreciaciones) / Math.Abs(anioUno.Otros.Depreciaciones.Value)) * 100 : null,
                                                UtilidadEjercicio = anioUno.Otros.UtilidadEjercicio == 0 && anioUno.Otros.UtilidadEjercicio != anioDos.Otros.UtilidadEjercicio ? 100 : anioUno.Otros.UtilidadEjercicio != 0 && anioUno.Otros.UtilidadEjercicio != anioDos.Otros.UtilidadEjercicio ? ((anioDos.Otros.UtilidadEjercicio - anioUno.Otros.UtilidadEjercicio) / Math.Abs(anioUno.Otros.UtilidadEjercicio.Value)) * 100 : null,
                                                PerdidaEjercicio = anioUno.Otros.PerdidaEjercicio == 0 && anioUno.Otros.PerdidaEjercicio != anioDos.Otros.PerdidaEjercicio ? 100 : anioUno.Otros.PerdidaEjercicio != 0 && anioUno.Otros.PerdidaEjercicio != anioDos.Otros.PerdidaEjercicio ? ((anioDos.Otros.PerdidaEjercicio - anioUno.Otros.PerdidaEjercicio) / Math.Abs(anioUno.Otros.PerdidaEjercicio.Value)) * 100 : null,
                                                ResultadosAcumulados = anioUno.Otros.ResultadosAcumulados == 0 && anioUno.Otros.ResultadosAcumulados != anioDos.Otros.ResultadosAcumulados ? 100 : anioUno.Otros.ResultadosAcumulados != 0 && anioUno.Otros.ResultadosAcumulados != anioDos.Otros.ResultadosAcumulados ? ((anioDos.Otros.ResultadosAcumulados - anioUno.Otros.ResultadosAcumulados) / Math.Abs(anioUno.Otros.ResultadosAcumulados.Value)) * 100 : null
                                            }
                                        };
                                        analisisHorizontal.Add(agrupacionAnalisis[i].Select(m => m).FirstOrDefault());
                                        analisisHorizontal.Add(analisis);

                                        if (i == 1) analisisHorizontal.Add(agrupacionAnalisis[i - 1].Select(m => m).FirstOrDefault());
                                    }
                                }
                                r_balances = r_balances.Where(m => !string.IsNullOrEmpty(m.RUC) && !string.IsNullOrEmpty(m.RazonSocial) && m.Periodo != 0).ToList();
                                if (r_balances.Any())
                                    r_balance = r_balances.FirstOrDefault(m => !string.IsNullOrEmpty(m.RUC) && !string.IsNullOrEmpty(m.RazonSocial) && m.Periodo != 0);

                                #endregion Análisis Horizontal
                            }

                            _logger.LogInformation($"Procesando Empresas Representante identificación: {modelo.Identificacion}");
                            representantesEmpresas = await _balances.GetRepresentantesEmpresasAsync(historialTemp.NombresPersona);
                            accionistas = await _balances.GetInformacionAccionistasAsync(modelo.Identificacion);
                            if (accionistas != null && !accionistas.Any())
                                accionistas = null;

                            empresasAccionista = await _balances.GetAccionistaEmpresasAsync(historialTemp.NombresPersona);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Balances con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (directorioCompania == null && !busquedaNoJuridico)
                    {
                        var datosDetalleDirectorio = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.DirectorioCompanias && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleDirectorio != null)
                        {
                            cacheBalance = true;
                            directorioCompania = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.DirectorioCompania>(datosDetalleDirectorio);
                        }
                    }

                    if (analisisHorizontal == null && !busquedaNoJuridico)
                    {
                        var datosDetalleAnalisis = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.AnalisisHorizontal && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleAnalisis != null)
                        {
                            cacheBalance = true;
                            analisisHorizontal = JsonConvert.DeserializeObject<List<AnalisisHorizontalViewModel>>(datosDetalleAnalisis);
                        }
                    }

                    if (r_balances == null && !busquedaNoJuridico)
                    {
                        var datosDetalleBalances = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Balances && m.Historial.Periodo == 1 && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleBalances != null)
                        {
                            cacheBalance = true;
                            r_balances = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.BalanceEmpresa>>(datosDetalleBalances);
                        }
                    }

                    if (accionistas == null && !busquedaNoJuridico)
                    {
                        var datosDetalleAccionistas = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Accionistas && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleAccionistas != null)
                        {
                            cacheAccionistas = true;
                            accionistas = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Accionista>>(datosDetalleAccionistas);
                        }
                    }

                    if (representantesEmpresas == null || (representantesEmpresas != null && !representantesEmpresas.Any()))
                    {
                        var datosDetalleRepresentante = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.RepresentantesEmpresas && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleRepresentante != null)
                        {
                            cacheBalance = true;
                            representantesEmpresas = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.RepresentanteEmpresa>>(datosDetalleRepresentante);
                        }
                    }

                    datos = new BalancesApiMetodoViewModel()
                    {
                        Balance = r_balance,
                        Balances = r_balances,
                        MultiplesPeriodos = balancesMultiples,
                        PeriodoBusqueda = historialTemp.Periodo.Value,
                        PeriodosBusqueda = periodosBusqueda,
                        SoloEmpresasSimilares = soloEmpresasSimilares,
                        DirectorioCompania = directorioCompania,
                        AnalisisHorizontal = analisisHorizontal,
                        RepresentantesEmpresas = representantesEmpresas,
                        Accionistas = accionistas,
                        EmpresasAccionista = empresasAccionista
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathBalances = Path.Combine(pathFuentes, ValidacionViewModel.ValidarCedula(identificacionOriginal) ? "balancesPersonaDemo.json" : "balancesDemo.json");
                    datos = JsonConvert.DeserializeObject<BalancesApiMetodoViewModel>(System.IO.File.ReadAllText(pathBalances));
                }

                _logger.LogInformation("Fuente de Balances procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Balances. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (directorioCompania != null && !string.IsNullOrEmpty(directorioCompania.Representante?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                                historial.NombresPersona = directorioCompania.Representante.Trim().ToUpper();

                            await _historiales.UpdateAsync(historial);
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Balances,
                            Generado = datos.Balances != null && datos.Balances.Any(),
                            Data = datos.Balances != null && datos.Balances.Any() ? JsonConvert.SerializeObject(datos.Balances) : null,
                            Cache = cacheBalance,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Balances procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.DirectorioCompanias,
                            Generado = datos.DirectorioCompania != null,
                            Data = datos.DirectorioCompania != null ? JsonConvert.SerializeObject(datos.DirectorioCompania) : null,
                            Cache = cacheBalance,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Directorio Compañias procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.AnalisisHorizontal,
                            Generado = datos.AnalisisHorizontal != null,
                            Data = datos.AnalisisHorizontal != null ? JsonConvert.SerializeObject(datos.AnalisisHorizontal) : null,
                            Cache = cacheBalance,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Análisis Horizontal procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.RepresentantesEmpresas,
                            Generado = datos.RepresentantesEmpresas != null,
                            Data = datos.RepresentantesEmpresas != null && datos.RepresentantesEmpresas.Any() ? JsonConvert.SerializeObject(datos.RepresentantesEmpresas) : null,
                            Cache = cacheBalance,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Representantes empresas procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Accionistas,
                            Generado = datos.Accionistas != null,
                            Data = datos.Accionistas != null ? JsonConvert.SerializeObject(datos.Accionistas) : null,
                            Cache = cacheAccionistas,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Accionistas Compañias procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.EmpresasAccionista,
                            Generado = datos.EmpresasAccionista != null,
                            Data = datos.EmpresasAccionista != null && datos.EmpresasAccionista.Any() ? JsonConvert.SerializeObject(datos.EmpresasAccionista) : null,
                            Cache = cacheBalance,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Empresas Accionista procesado correctamente");

                        if (historial != null)
                        {
                            var parametros = string.Empty;
                            var periodoTemp = 0;
                            if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                            {
                                periodoTemp = 1;
                                var infoPeriodos = _configuration.GetSection("AppSettings:PeriodosDinamicos").Get<PeriodosDinamicosViewModel>();
                                if (infoPeriodos != null)
                                {
                                    var ultimosPeriodos = r_balances.Select(m => m.Periodo).ToList();
                                    parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = ultimosPeriodos });
                                }
                            }
                            else
                                parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { periodoTemp } });

                            historial.Periodo = periodoTemp;
                            historial.ParametrosBusqueda = parametros;
                            await _historiales.UpdateAsync(historial);
                        }
                        _logger.LogInformation("Historial de la Fuente Balances procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<IessApiMetodoViewModel> ObtenerReporteIESS(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.IESS.Modelos.Persona r_iess = null;
                Externos.Logica.IESS.Modelos.Afiliacion r_afiliacion = null;
                List<Externos.Logica.IESS.Modelos.Afiliado> r_afiliado = null;
                List<Externos.Logica.IESS.Modelos.Empleado> r_empleadosempresa = null;
                //V2
                ResultadoListAfiliado resultadoAfiliado = null;
                ResultadoListEmpleado resultadoListEmpleado = null;
                Externos.Logica.IESS.Modelos.ResultadoAfiliacion resultadoAfiliacion = null;
                Externos.Logica.IESS.Modelos.ResultadoPersona resultadoIess = null;
                //V2
                var datos = new IessApiMetodoViewModel();
                Historial historialTemp = null;
                var cacheIess = false;
                var cacheAfiliado = false;
                var cedulaEntidades = false;
                var cacheAfiliadoAdicional = false;
                var cacheEmpresaEmpleados = false;
                var busquedaEmpresaEmpleados = false;
                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.Iess;

                var idEmpresasSalarios = new List<int>();
                try
                {
                    var pathEmpresasSalarioIess = Path.Combine("wwwroot", "data", "empresasSalarioIess.json");
                    var archivoSalarios = System.IO.File.ReadAllText(pathEmpresasSalarioIess);
                    var empresasSalariosIess = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoSalarios);
                    if (empresasSalariosIess != null && empresasSalariosIess.Any())
                        idEmpresasSalarios = empresasSalariosIess.Select(m => m.Id).Distinct().ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                var idEmpresasEmpleados = new List<int>();
                try
                {
                    var pathEmpresaEmpleadosIess = Path.Combine("wwwroot", "data", "empresasEmpleadosIess.json");
                    var archivoEmpleados = System.IO.File.ReadAllText(pathEmpresaEmpleadosIess);
                    var empresaEmpleadosIess = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoEmpleados);
                    if (empresaEmpleadosIess != null && empresaEmpleadosIess.Any())
                        idEmpresasEmpleados = empresaEmpleadosIess.Select(m => m.Id).Distinct().ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente IESS identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                resultadoIess = await _iess.GetRespuestaAsyncV2(modelo.Identificacion);
                                r_iess = resultadoIess?.Persona;
                                if (r_iess != null)
                                {
                                    if (r_iess.Obligacion != null && !string.IsNullOrEmpty(r_iess.Obligacion.MoraOriginal) && r_iess.Obligacion.Mora.HasValue)
                                        r_iess.Obligacion.Mora = double.Parse(r_iess.Obligacion.MoraOriginal.Replace(",", ""));
                                }
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    resultadoAfiliacion = await ObtenerRAfiliacion(modelo, modelo.Identificacion, tipoFuente);
                                    r_afiliacion = resultadoAfiliacion?.Afiliacion;
                                    resultadoAfiliado = await _iess.GetInformacionAfiliadoV2(modelo.Identificacion);
                                    r_afiliado = resultadoAfiliado?.Afiliado;
                                }
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                    {
                                        if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        {
                                            resultadoAfiliacion = await ObtenerRAfiliacion(modelo, historialTemp.IdentificacionSecundaria.Trim(), tipoFuente);
                                            r_afiliacion = resultadoAfiliacion?.Afiliacion;
                                            resultadoAfiliado = await _iess.GetInformacionAfiliadoV2(historialTemp.IdentificacionSecundaria.Trim());
                                            r_afiliado = resultadoAfiliado?.Afiliado;
                                        }
                                    }
                                    if (idEmpresasEmpleados.Contains(modelo.IdEmpresa))
                                    {
                                        resultadoListEmpleado = await _iess.GetInformacionEmpresaEmpleadosIessV2(modelo.Identificacion);
                                        r_empleadosempresa = resultadoListEmpleado?.Empleado;
                                        busquedaEmpresaEmpleados = true;
                                    }
                                }
                                else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                    {
                                        resultadoAfiliacion = await ObtenerRAfiliacion(modelo, cedulaTemp, tipoFuente);
                                        r_afiliacion = resultadoAfiliacion?.Afiliacion;
                                        resultadoAfiliado = await _iess.GetInformacionAfiliadoV2(cedulaTemp);
                                        r_afiliado = resultadoAfiliado?.Afiliado;
                                    }
                                }
                                r_iess = await _iess.GetRespuestaAsync(modelo.Identificacion);
                                if (r_iess != null)
                                {
                                    if (r_iess.Obligacion != null && !string.IsNullOrEmpty(r_iess.Obligacion.MoraOriginal) && r_iess.Obligacion.Mora.HasValue)
                                        r_iess.Obligacion.Mora = double.Parse(r_iess.Obligacion.MoraOriginal.Replace(",", ""));
                                }
                            }
                        }

                        if (r_afiliacion != null)
                        {
                            var empresasAfiliacion = Regex.Matches(r_afiliacion.Empresa, "001").Count();
                            if (empresasAfiliacion > 1)
                            {
                                r_afiliacion.Empresa = r_afiliacion.Empresa.Replace("001", "001. ");
                            }

                            if (!string.IsNullOrEmpty(r_afiliacion.Reporte))
                                r_afiliacion.Reporte = null;
                        }

                        if (r_afiliado != null && !r_afiliado.Any())
                            r_afiliado = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente IESS con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_iess == null)
                    {
                        var datosDetalleIess = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Iess && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleIess != null)
                        {
                            cacheIess = true;
                            r_iess = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Persona>(datosDetalleIess);
                        }
                    }
                    if (r_afiliacion == null)
                    {
                        var datosDetalleAfiliado = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleAfiliado != null)
                        {
                            cacheAfiliado = true;
                            r_afiliacion = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Afiliacion>(datosDetalleAfiliado);
                        }
                    }
                    //if (r_afiliado == null)
                    //{
                    //    var datosDetalleAfiliado = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.AfiliadoAdicional && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                    //    if (datosDetalleAfiliado != null)
                    //    {
                    //        cacheAfiliadoAdicional = true;
                    //        r_afiliado = JsonConvert.DeserializeObject<List<Externos.Logica.IESS.Modelos.Afiliado>>(datosDetalleAfiliado);
                    //    }
                    //}
                    //if (r_empleadosempresa == null && busquedaEmpresaEmpleados)
                    //{

                    //    var datosDetalleEmpresaEmpleado = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.IessEmpresaEmpleados && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                    //    if (datosDetalleEmpresaEmpleado != null)
                    //    {
                    //        cacheEmpresaEmpleados = true;
                    //        r_empleadosempresa = JsonConvert.DeserializeObject<List<Externos.Logica.IESS.Modelos.Empleado>>(datosDetalleEmpresaEmpleado);
                    //    }
                    //}

                    datos = new IessApiMetodoViewModel()
                    {
                        Iess = r_iess,
                        Afiliado = r_afiliacion,
                        AfiliadoAdicional = r_afiliado,
                        EmpleadosEmpresa = r_empleadosempresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathIess = Path.Combine(pathFuentes, "iessDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathIess);
                    datos = JsonConvert.DeserializeObject<IessApiMetodoViewModel>(archivo);
                    datos.Afiliado.Reporte = null;
                }
                _logger.LogInformation("Fuente de IESS procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente IESS. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Iess,
                            Generado = datos.Iess != null,
                            Data = datos.Iess != null ? JsonConvert.SerializeObject(datos.Iess) : null,
                            Cache = cacheIess,
                            FechaRegistro = DateTime.Now,
                            Reintento = null,
                            DataError = resultadoIess?.Error,
                            FuenteActiva = resultadoIess?.FuenteActiva
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Afiliado,
                            Generado = datos.Afiliado != null,
                            Data = datos.Afiliado != null ? JsonConvert.SerializeObject(datos.Afiliado) : null,
                            Cache = cacheAfiliado,
                            FechaRegistro = DateTime.Now,
                            Reintento = null,
                            DataError = resultadoAfiliacion?.Error,
                            FuenteActiva = resultadoAfiliacion?.FuenteActiva
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.AfiliadoAdicional,
                            Generado = datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any(),
                            Data = datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any() ? JsonConvert.SerializeObject(datos.AfiliadoAdicional) : null,
                            Cache = cacheAfiliadoAdicional,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = resultadoAfiliado?.Error,
                            FuenteActiva = resultadoAfiliado?.FuenteActiva
                        });
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.IessEmpresaEmpleados,
                            Generado = datos.EmpleadosEmpresa != null && datos.EmpleadosEmpresa.Any(),
                            Data = datos.EmpleadosEmpresa != null && datos.EmpleadosEmpresa.Any() ? JsonConvert.SerializeObject(datos.EmpleadosEmpresa) : null,
                            Cache = cacheEmpresaEmpleados,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = resultadoListEmpleado?.Error,
                            FuenteActiva = resultadoListEmpleado?.FuenteActiva
                        });
                        _logger.LogInformation("Historial de la Fuente IESS procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                if (!idEmpresasSalarios.Contains(modelo.IdEmpresa))
                {
                    if (datos != null && datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any())
                    {
                        foreach (var item in datos.AfiliadoAdicional)
                        {
                            item.SalarioAfiliado = null;
                            item.SalarioAfiliadoOriginal = null;
                            item.SalarioAfiliadoSuperior = null;
                            item.SalarioAfiliadoOriginalSuperior = null;
                        }
                    }
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }

        private async Task<Externos.Logica.IESS.Modelos.ResultadoAfiliacion> ObtenerRAfiliacion(ApiViewModel_1790325083001 modelo, string identificacion, int? tipoFuente)
        {
            try
            {
                Externos.Logica.IESS.Modelos.ResultadoAfiliacion r_afiliacion = null;
                DateTime? fechaNacimiento = null;
                var fuentes = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.RegistroCivil };
                var detallesHistorial = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentes.Contains(m.TipoFuente), null, null, 0, null, true);
                if (detallesHistorial.Any())
                {
                    var registroCivil = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);
                    if (registroCivil != null)
                    {
                        var dataRc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(registroCivil.Data);
                        if (dataRc != null)
                            fechaNacimiento = dataRc.FechaNacimiento;
                    }
                    else
                    {
                        var personaGc = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);
                        if (personaGc != null)
                        {
                            var dataGc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(personaGc.Data);
                            if (dataGc != null)
                                fechaNacimiento = dataGc.FechaNacimiento;
                        }
                    }
                }
                switch (tipoFuente)
                {
                    case 1:
                        if (fechaNacimiento.HasValue)
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficialV2(identificacion, fechaNacimiento.Value);
                        break;
                    case 2:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        break;
                    case 3:
                        if (fechaNacimiento.HasValue)
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficialV2(identificacion, fechaNacimiento.Value);
                        if (r_afiliacion == null)
                            r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        break;
                    case 4:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        if (r_afiliacion == null && fechaNacimiento.HasValue)
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficialV2(identificacion, fechaNacimiento.Value);
                        break;
                    default:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        break;
                }
                if (r_afiliacion != null && r_afiliacion.Afiliacion != null) r_afiliacion.Afiliacion.ReportePdf = null;
                return r_afiliacion;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }

        private async Task<SenescytApiViewModel> ObtenerReporteSenescyt(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.Senescyt.Modelos.Persona r_senescyt = null;
                var datos = new SenescytApiViewModel();
                Historial historialTemp = null;
                var cacheSenescyt = false;
                var cedulaEntidades = false;
                var hostSenescyt = string.Empty;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Senescyt identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, i => i.Include(m => m.Usuario).ThenInclude(m => m.Empresa), true);
                        hostSenescyt = !string.IsNullOrWhiteSpace(historialTemp.Usuario.Empresa.DireccionIp) ? historialTemp.Usuario.Empresa.DireccionIp : null;
                        //var datosCacheFecha = historialTemp != null ? (await _detalleHistorial.ReadAsync(m => m.Data, m => m.Historial.Identificacion == historialTemp.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt && m.Generado && !m.Cache && !string.IsNullOrEmpty(m.Data) && m.Historial.Fecha.Date == DateTime.Now.Date, o => o.OrderByDescending(m => m.Id), null, null, null, true)).FirstOrDefault() : null;
                        //if (datosCacheFecha != null)
                        //{
                        //    busquedaNuevaSenescyt = true;
                        //    cacheSenescyt = true;
                        //    r_senescyt = JsonConvert.DeserializeObject<Externos.Logica.Senescyt.Modelos.Persona>(datosCacheFecha);
                        //    _logger.LogInformation($"Datos obtenidos de una consulta realizada previamente el mismo día en la Fuente Senescyt identificación: {modelo.Identificacion}");
                        //}
                        //else
                        //{
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    //r_senescyt = await _senescyt.GetRespuestaAsync(modelo.Identificacion);
                                    var resultado = await _consulta.ObtenerSenescytConsultaExterna(modelo.Identificacion, hostSenescyt);
                                    if (resultado != null)
                                        r_senescyt = resultado.Data;
                                }
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                    {
                                        if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        {
                                            //r_senescyt = await _senescyt.GetRespuestaAsync(r_sri.RepresentanteLegal.Trim());
                                            var resultado = await _consulta.ObtenerSenescytConsultaExterna(historialTemp.IdentificacionSecundaria.Trim(), hostSenescyt);
                                            if (resultado != null)
                                                r_senescyt = resultado.Data;
                                        }
                                    }
                                }
                                else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                    {
                                        //r_senescyt = await _senescyt.GetRespuestaAsync(cedulaTemp);
                                        var resultado = await _consulta.ObtenerSenescytConsultaExterna(cedulaTemp, hostSenescyt);
                                        if (resultado != null)
                                            r_senescyt = resultado.Data;
                                    }
                                }
                            }
                        }
                        //}
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Senescyt con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_senescyt == null)
                    {
                        var datosDetalleASenescyt = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleASenescyt != null)
                        {
                            cacheSenescyt = true;
                            r_senescyt = JsonConvert.DeserializeObject<Externos.Logica.Senescyt.Modelos.Persona>(datosDetalleASenescyt);
                        }
                    }

                    datos = new SenescytApiViewModel()
                    {
                        Senescyt = r_senescyt
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSenecyt = Path.Combine(pathFuentes, "senescytDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathSenecyt);
                    datos = JsonConvert.DeserializeObject<SenescytApiViewModel>(archivo);
                }

                _logger.LogInformation("Fuente de Senescyt procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Senescyt. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Senescyt,
                            Generado = datos.Senescyt != null,
                            Data = datos.Senescyt != null ? JsonConvert.SerializeObject(datos.Senescyt) : null,
                            Cache = cacheSenescyt,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Senescyt procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<JudicialApiViewModel> ObtenerReporteLegal(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicial = null;
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicialNombres = null;
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicialempresaRuc = null;
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicialempresa = null;
                var datos = new JudicialApiViewModel();
                Historial historialTemp = null;
                var cacheLegal = false;
                var cacheLegalEmpresa = false;
                var cedulaEntidades = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Legal identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var empresasConsultaPersonalizada = new List<int>();
                        try
                        {
                            var pathEmpresasConsultaJudicial = Path.Combine("wwwroot", "data", "empresasConsultaJudicial.json");
                            var archivoJudicial = System.IO.File.ReadAllText(pathEmpresasConsultaJudicial);
                            var empresasConsultaJudicial = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoJudicial);
                            if (empresasConsultaJudicial != null && empresasConsultaJudicial.Any())
                                empresasConsultaPersonalizada = empresasConsultaJudicial.Select(m => m.Id).Distinct().ToList();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                        if (!empresasConsultaPersonalizada.Contains(modelo.IdEmpresa))
                        {
                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                if (cedulaEntidades)
                                {
                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
                                        r_fjudicial = _fjudicial.GetRespuesta(modelo.Identificacion);
                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                            r_fjudicialNombres = _fjudicial.GetRespuesta(historialTemp.NombresPersona);

                                        if (r_fjudicial != null && r_fjudicialNombres != null)
                                        {
                                            r_fjudicial.Actor = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Actor, r_fjudicialNombres.Actor);
                                            r_fjudicial.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Demandado, r_fjudicialNombres.Demandado);
                                        }
                                        else if (r_fjudicialNombres != null && r_fjudicial == null)
                                            r_fjudicial = r_fjudicialNombres;

                                        if (r_fjudicial == null)
                                        {
                                            var datosDetalleFJudicial = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                            if (datosDetalleFJudicial != null)
                                            {
                                                cacheLegal = true;
                                                r_fjudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleFJudicial);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
                                        r_fjudicialempresaRuc = _fjudicial.GetRespuesta(modelo.Identificacion);
                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.RazonSocialEmpresa))
                                            r_fjudicialempresa = _fjudicial.GetRespuesta(historialTemp.RazonSocialEmpresa);

                                        if (r_fjudicialempresa != null && r_fjudicialempresaRuc != null)
                                        {
                                            r_fjudicialempresa.Actor = ReporteViewModel.NormalizarProcesosLegal(r_fjudicialempresa.Actor, r_fjudicialempresaRuc.Actor);
                                            r_fjudicialempresa.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicialempresa.Demandado, r_fjudicialempresaRuc.Demandado);
                                        }
                                        else if (r_fjudicialempresaRuc != null && r_fjudicialempresa == null)
                                            r_fjudicialempresa = r_fjudicialempresaRuc;

                                        if (r_fjudicialempresa == null)
                                        {
                                            var datosDetalleJFJEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                            if (datosDetalleJFJEmpresa != null)
                                            {
                                                cacheLegalEmpresa = true;
                                                r_fjudicialempresa = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleJFJEmpresa);
                                            }
                                        }
                                    }
                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                    {
                                        if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        {
                                            r_fjudicial = _fjudicial.GetRespuesta(historialTemp.IdentificacionSecundaria);
                                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                                r_fjudicialNombres = _fjudicial.GetRespuesta(historialTemp.NombresPersona);

                                            if (r_fjudicial != null && r_fjudicialNombres != null)
                                            {
                                                r_fjudicial.Actor = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Actor, r_fjudicialNombres.Actor);
                                                r_fjudicial.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Demandado, r_fjudicialNombres.Demandado);
                                            }
                                            else if (r_fjudicialNombres != null && r_fjudicial == null)
                                                r_fjudicial = r_fjudicialNombres;

                                            if (r_fjudicial == null)
                                            {
                                                var datosDetalleFJudicial = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                                if (datosDetalleFJudicial != null)
                                                {
                                                    cacheLegal = true;
                                                    r_fjudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleFJudicial);
                                                }
                                            }
                                        }
                                    }
                                    else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                    {
                                        var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                        if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        {
                                            r_fjudicial = _fjudicial.GetRespuesta(cedulaTemp);
                                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                                r_fjudicialNombres = _fjudicial.GetRespuesta(historialTemp.NombresPersona);

                                            if (r_fjudicial != null && r_fjudicialNombres != null)
                                            {
                                                r_fjudicial.Actor = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Actor, r_fjudicialNombres.Actor);
                                                r_fjudicial.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Demandado, r_fjudicialNombres.Demandado);
                                            }
                                            else if (r_fjudicialNombres != null && r_fjudicial == null)
                                                r_fjudicial = r_fjudicialNombres;

                                            if (r_fjudicial == null)
                                            {
                                                var datosDetalleFJudicial = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                                if (datosDetalleFJudicial != null)
                                                {
                                                    cacheLegalEmpresa = true;
                                                    r_fjudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleFJudicial);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                if (cedulaEntidades)
                                {
                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
                                        r_fjudicial = _fjudicial.GetRespuesta(modelo.Identificacion);
                                        if (r_fjudicial == null)
                                        {
                                            var datosDetalleFJudicial = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                            if (datosDetalleFJudicial != null)
                                            {
                                                cacheLegal = true;
                                                r_fjudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleFJudicial);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
                                        r_fjudicialempresa = _fjudicial.GetRespuesta(modelo.Identificacion);
                                        if (r_fjudicialempresa == null)
                                        {
                                            var datosDetalleJFJEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                            if (datosDetalleJFJEmpresa != null)
                                            {
                                                cacheLegalEmpresa = true;
                                                r_fjudicialempresa = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleJFJEmpresa);
                                            }
                                        }
                                    }
                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                    {
                                        if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        {
                                            r_fjudicial = _fjudicial.GetRespuesta(historialTemp.IdentificacionSecundaria);
                                            if (r_fjudicial == null)
                                            {
                                                var datosDetalleFJudicial = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                                if (datosDetalleFJudicial != null)
                                                {
                                                    cacheLegal = true;
                                                    r_fjudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleFJudicial);
                                                }
                                            }
                                        }
                                    }
                                    else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                    {
                                        var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                        if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        {
                                            r_fjudicial = _fjudicial.GetRespuesta(cedulaTemp);
                                            if (r_fjudicial == null)
                                            {
                                                var datosDetalleFJudicial = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                                if (datosDetalleFJudicial != null)
                                                {
                                                    cacheLegal = true;
                                                    r_fjudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(datosDetalleFJudicial);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente legal con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    datos = new JudicialApiViewModel()
                    {
                        Persona = r_fjudicial,
                        Empresa = r_fjudicialempresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathLegal = Path.Combine(pathFuentes, "legalDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathLegal);
                    var datosTemp = JsonConvert.DeserializeObject<JudicialApiViewModel>(archivo);
                    datos = new JudicialApiViewModel()
                    {
                        Persona = datosTemp.Persona,
                        Empresa = datosTemp.Empresa
                    };
                }
                _logger.LogInformation("Fuente de Legal procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Legal. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.FJudicial,
                            Generado = datos.Persona != null,
                            Data = datos.Persona != null ? JsonConvert.SerializeObject(datos.Persona) : null,
                            Cache = cacheLegal,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.FJEmpresa,
                            Generado = datos.Empresa != null,
                            Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null,
                            Cache = cacheLegalEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Legal procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<ImpedimentoMetodoApiViewModel> ObtenerReporteImpedimento(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.FJudicial.Modelos.Impedimento impedimento = null;
                Historial historialTemp = null;
                var cacheImpedimento = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Legal Impedimento identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        string fechaNacimiento = null;
                        var fuentes = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.RegistroCivil };
                        var detallesHistorial = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentes.Contains(m.TipoFuente), null, null, 0, null, true);
                        if (detallesHistorial.Any())
                        {
                            var registroCivil = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);
                            if (registroCivil != null)
                            {
                                var dataRc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(registroCivil.Data);
                                if (dataRc != null)
                                    fechaNacimiento = dataRc.FechaNacimiento.ToString("dd/MM/yyyy");
                            }
                            else
                            {
                                var personaGc = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);
                                if (personaGc != null)
                                {
                                    var dataGc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(personaGc.Data);
                                    if (dataGc != null)
                                        fechaNacimiento = dataGc.FechaNacimiento.Value.ToString("dd/MM/yyyy");
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(historialTemp.Identificacion) && ValidacionViewModel.ValidarCedula(historialTemp.Identificacion) && !string.IsNullOrEmpty(fechaNacimiento))
                            impedimento = await _fjudicial.GetImpedimentos(historialTemp.Identificacion, fechaNacimiento);
                        else if (!string.IsNullOrEmpty(historialTemp.Identificacion) && ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion) && !string.IsNullOrEmpty(fechaNacimiento))
                            impedimento = await _fjudicial.GetImpedimentos(historialTemp.Identificacion.Substring(0, 10), fechaNacimiento);
                        else if (!string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)) && !string.IsNullOrEmpty(fechaNacimiento))
                            impedimento = await _fjudicial.GetImpedimentos(historialTemp.IdentificacionSecundaria, fechaNacimiento);

                        if (impedimento != null && impedimento.Reporte.Length > 0)
                            impedimento.Reporte = null;

                        if (impedimento == null)
                        {
                            var datosDetalleImpedimento = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Impedimento && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleImpedimento != null)
                            {
                                cacheImpedimento = true;
                                impedimento = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Impedimento>(datosDetalleImpedimento);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Legal Impedimento con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathImpedimento = Path.Combine(pathFuentes, "impedimentoDemo.json");
                    impedimento = JsonConvert.DeserializeObject<Impedimento>(System.IO.File.ReadAllText(pathImpedimento));

                    if (impedimento != null && impedimento.Reporte != null && impedimento.Reporte.Length > 0)
                        impedimento.Reporte = null;

                }
                _logger.LogInformation("Fuente de Legal Impedimento procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Legal Impedimento. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Impedimento,
                            Generado = impedimento != null,
                            Data = impedimento != null ? JsonConvert.SerializeObject(impedimento) : null,
                            Cache = cacheImpedimento,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return new ImpedimentoMetodoApiViewModel() { Impedimento = impedimento };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<SercopApiViewModel> ObtenerReporteSERCOP(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.SERCOP.Modelos.ProveedorIncumplido r_sercop = null;
                List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria> r_sercopcontraloria = null;
                var datos = new SercopApiViewModel();
                Historial historialTemp = null;
                var cacheSercop = false;
                var cedulaEntidades = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SERCOP identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    r_sercop = await _sercop.GetRespuestaAsync(modelo.Identificacion);
                                }
                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.RazonSocialEmpresa))
                                {
                                    r_sercopcontraloria = _sercop.GetProveedorContraloria(historialTemp.RazonSocialEmpresa);
                                    if (r_sercopcontraloria == null && !r_sercopcontraloria.Any())
                                    {
                                        var datosDetalleContraloria = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.ProveedorContraloria && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                        if (datosDetalleContraloria != null && !datosDetalleContraloria.Any())
                                        {
                                            cacheSercop = true;
                                            r_sercopcontraloria = JsonConvert.DeserializeObject<List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria>>(datosDetalleContraloria);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) || ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_sercop = await _sercop.GetRespuestaAsync(modelo.Identificacion);

                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.RazonSocialEmpresa))
                                    {
                                        r_sercopcontraloria = _sercop.GetProveedorContraloria(historialTemp.RazonSocialEmpresa);
                                        if (r_sercopcontraloria == null && !r_sercopcontraloria.Any())
                                        {
                                            var datosDetalleContraloria = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.ProveedorContraloria && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                            if (datosDetalleContraloria != null && !datosDetalleContraloria.Any())
                                            {
                                                cacheSercop = true;
                                                r_sercopcontraloria = JsonConvert.DeserializeObject<List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria>>(datosDetalleContraloria);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SERCOP con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_sercop == null)
                    {
                        var datosDetalleProveedor = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Proveedor && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleProveedor != null)
                        {
                            cacheSercop = true;
                            r_sercop = JsonConvert.DeserializeObject<Externos.Logica.SERCOP.Modelos.ProveedorIncumplido>(datosDetalleProveedor);
                        }
                    }

                    datos = new SercopApiViewModel()
                    {
                        Proveedor = r_sercop,
                        ProveedorContraloria = r_sercopcontraloria,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSercop = Path.Combine(pathFuentes, "sercopDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathSercop);
                    datos = JsonConvert.DeserializeObject<SercopApiViewModel>(archivo);
                }

                _logger.LogInformation("Fuente de SERCOP procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SERCOP. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Proveedor,
                            Generado = datos.Proveedor != null && (datos.Proveedor.ProveedoresIncop.Count() != 0 || datos.Proveedor.ProveedoresContraloria.Count() != 0),
                            Data = datos.Proveedor != null && (datos.Proveedor.ProveedoresIncop.Count() != 0 || datos.Proveedor.ProveedoresContraloria.Count() != 0) ? JsonConvert.SerializeObject(datos.Proveedor) : null,
                            Cache = cacheSercop,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.ProveedorContraloria,
                            Generado = datos.ProveedorContraloria != null && datos.ProveedorContraloria.Count() != 0,
                            Data = datos.ProveedorContraloria != null && datos.ProveedorContraloria.Count() != 0 ? JsonConvert.SerializeObject(datos.ProveedorContraloria) : null,
                            Cache = cacheSercop,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente SERCOP procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<AntApiViewModel> ObtenerReporteANT(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.ANT.Modelos.Licencia r_ant = null;
                //List<Externos.Logica.ANT.Modelos.AutoHistorico> autosHistorico = null;
                var datos = new AntApiViewModel();
                Historial historialTemp = null;
                var cacheAnt = false;
                //var cacheHistoricoAutos = false;
                ResultadoLicencia resultadoLicencia = null;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente ANT identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                            {
                                resultadoLicencia = await _ant.GetRespuestaAsyncV2(modelo.Identificacion);
                                r_ant = resultadoLicencia?.Licencia;
                                //autosHistorico = await _ant.ObtenerVehiculoHistorico(modelo.Identificacion);
                            }

                            if (r_ant == null && historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                            {
                                resultadoLicencia = await _ant.GetRespuestaAsyncV2(historialTemp.IdentificacionSecundaria);
                                r_ant = resultadoLicencia?.Licencia;
                                //autosHistorico = await _ant.ObtenerVehiculoHistorico(historialTemp.IdentificacionSecundaria);
                            }

                            if (r_ant != null && (string.IsNullOrEmpty(r_ant.Cedula) || string.IsNullOrEmpty(r_ant.Titular)))
                                r_ant = null;
                        }
                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            resultadoLicencia = await _ant.GetRespuestaAsyncV2(modelo.Identificacion);
                            r_ant = resultadoLicencia?.Licencia;
                            //autosHistorico = await _ant.ObtenerVehiculoHistorico(modelo.Identificacion);
                        }

                        //if (autosHistorico == null || (autosHistorico != null && !autosHistorico.Any()))
                        //    autosHistorico = null;

                        if (r_ant == null)
                        {
                            var datosDetalleAnt = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Ant && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleAnt != null)
                            {
                                cacheAnt = true;
                                r_ant = JsonConvert.DeserializeObject<Externos.Logica.ANT.Modelos.Licencia>(datosDetalleAnt);
                            }
                        }

                        if (r_ant != null && historialTemp != null)
                        {
                            if ((r_ant.Cedula?.Trim() == historialTemp.Identificacion?.Trim() || r_ant.Cedula?.Trim() == historialTemp.IdentificacionSecundaria?.Trim()) && !string.IsNullOrEmpty(historialTemp.NombresPersona) && !string.IsNullOrEmpty(r_ant.Titular) && historialTemp.NombresPersona != r_ant.Titular)
                                r_ant.Titular = historialTemp.NombresPersona;
                        }

                        //if (autosHistorico == null)
                        //{
                        //    var datosDetalleAutoHistorico = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.AutoHistorico && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleAutoHistorico != null)
                        //    {
                        //        cacheHistoricoAutos = true;
                        //        autosHistorico = JsonConvert.DeserializeObject<List<Externos.Logica.ANT.Modelos.AutoHistorico>>(datosDetalleAutoHistorico);
                        //    }
                        //}

                        datos = new AntApiViewModel()
                        {
                            Licencia = r_ant,
                            //AutosHistorico = autosHistorico
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente ANT con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathAnt = Path.Combine(pathFuentes, "antDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathAnt);
                    var licencia = JsonConvert.DeserializeObject<AntApiViewModel>(archivo);
                    datos = new AntApiViewModel()
                    {
                        Licencia = licencia.Licencia,
                        //AutosHistorico = licencia.AutosHistorico
                    };
                }

                _logger.LogInformation("Fuente de ANT procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente ANT. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Ant,
                            Generado = datos.Licencia != null,
                            Data = datos.Licencia != null ? JsonConvert.SerializeObject(datos.Licencia) : null,
                            Cache = cacheAnt,
                            FechaRegistro = DateTime.Now,
                            Reintento = null,
                            DataError = resultadoLicencia != null ? resultadoLicencia.Error : null,
                            FuenteActiva = resultadoLicencia != null ? resultadoLicencia.FuenteActiva : null
                        });

                        //await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        //{
                        //    IdHistorial = modelo.IdHistorial,
                        //    TipoFuente = Dominio.Tipos.Fuentes.AutoHistorico,
                        //    Generado = datos.AutosHistorico != null,
                        //    Data = datos.AutosHistorico != null ? JsonConvert.SerializeObject(datos.AutosHistorico) : null,
                        //    Cache = cacheHistoricoAutos,
                        //    FechaRegistro = DateTime.Now,
                        //    Reintento = false
                        //});
                        _logger.LogInformation("Historial de la Fuente ANT procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<BuroCreditoMetodoViewModel> ObtenerReporteBuroCredito(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                    identificacionBuro = $"{modelo.Identificacion}001";

                if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                    identificacionBuro = modelo.Identificacion.Substring(0, 10);

                Externos.Logica.BuroCredito.Modelos.CreditoRespuesta r_burocredito = null;
                Externos.Logica.Equifax.Modelos.Resultado r_burocreditoEquifax = null;
                Historial historialTemp = null;
                var cacheBuroCredito = false;
                var idUsuario = modelo.IdUsuario;
                var idPlanBuro = 0;
                var datos = new BuroCreditoMetodoViewModel();
                var dataError = string.Empty;
                var culture = System.Globalization.CultureInfo.CurrentCulture;

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                idPlanBuro = planBuroCredito.Id;

                if (idPlanBuro == 0)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene planes vigentes de Buró de Crédito.");

                var fechaActual = DateTime.Now;
                var primerDiadelMes = new DateTime(fechaActual.Year, fechaActual.Month, 1);
                var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);
                var numeroHistorialBuro = await _historiales.CountAsync(s => s.Id != modelo.IdHistorial && s.IdPlanBuroCredito == idPlanBuro && s.Fecha >= primerDiadelMes && s.Fecha <= ultimoDiadelMes);
                var resultadoPermiso = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;

                if (planBuroCredito.BloquearConsultas)
                    resultadoPermiso = planBuroCredito.NumeroMaximoConsultas > numeroHistorialBuro ? Dominio.Tipos.EstadosPlanesBuroCredito.Activo : Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;

                if (resultadoPermiso != Dominio.Tipos.EstadosPlanesBuroCredito.Activo)
                    throw new Exception("Contrate un nuevo plan para el Buró de Crédito.");

                var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                if (historial != null)
                {
                    historial.IdPlanBuroCredito = idPlanBuro;
                    historial.TipoFuenteBuro = planBuroCredito.Fuente;
                    await _historiales.UpdateAsync(historial);
                    if (historialConsolidado != null)
                    {
                        historialConsolidado.ConsultaBuro = historial.IdPlanBuroCredito.HasValue && historial.IdPlanBuroCredito.Value > 0;
                        historialConsolidado.FuenteBuro = historial.TipoFuenteBuro != null && historial.TipoFuenteBuro.HasValue ? historial.TipoFuenteBuro.Value : 0;
                        await _reporteConsolidado.UpdateAsync(historialConsolidado);
                    }
                }

                try
                {
                    var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                    historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                    var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                    var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                    if (!cacheBuro && ambiente == "Production")
                    {
                        var buroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => new { m.Data, m.Historial.Fecha }, m => m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && (m.Historial.Identificacion == modelo.Identificacion || m.Historial.Identificacion == identificacionBuro) && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Historial.PlanBuroCredito.Fuente == planBuroCredito.Fuente && m.Historial.TipoFuenteBuro == planBuroCredito.Fuente && m.Generado && !m.Cache && planBuroCredito.PersistenciaCache > 0, o => o.OrderByDescending(m => m.Id));
                        if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Aval)
                                credenciales = new[] { credencial.Usuario, credencial.Clave, credencial.Enlace, credencial.ProductData, credencial.TokenAcceso, credencial.FechaCreacionToken.HasValue && credencial.FechaCreacionToken.Value != default ? credencial.FechaCreacionToken.Value.ToString() : string.Empty };

                            if (buroCredito != null && DateTime.Today.Date.AddDays(-planBuroCredito.PersistenciaCache) <= buroCredito.Fecha.Date)
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval con la persistencia del plan de la empresa para la identificación: {modelo.Identificacion}");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(buroCredito.Data);
                                cacheBuroCredito = true;
                            }
                            else
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval identificación: {modelo.Identificacion}");
                                if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
#if !DEBUG
                                    r_burocredito = await _burocredito.GetRespuestaAsync(modelo.Identificacion.Substring(0, 10), credenciales);
#endif
                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
#if !DEBUG
                                    r_burocredito = await _burocredito.GetRespuestaAsync(modelo.Identificacion, credenciales);
#endif
                                }
                                else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
#if !DEBUG
                                    r_burocredito = await _burocredito.GetRespuestaAsync(modelo.Identificacion, credenciales);
#endif
                                }

                                if (r_burocredito != null && r_burocredito.Result == null)
                                {
                                    dataError = JsonConvert.SerializeObject(r_burocredito);
                                    r_burocredito = null;
                                }
                            }
                        }
                        else if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave, credencial.Enlace, credencial.ProductData, credencial.TokenAcceso, credencial.FechaCreacionToken.HasValue && credencial.FechaCreacionToken.Value != default ? credencial.FechaCreacionToken.Value.ToString() : string.Empty };

                            if (buroCredito != null && DateTime.Today.Date.AddDays(-planBuroCredito.PersistenciaCache) <= buroCredito.Fecha.Date)
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax con la persistencia del plan de la empresa para la identificación: {modelo.Identificacion}");
                                r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(buroCredito.Data);
                                cacheBuroCredito = true;
                            }
                            else
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax identificación: {modelo.Identificacion}");
                                if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
#if !DEBUG
                                    if (credencial == null || string.IsNullOrWhiteSpace(credencial.ProductData?.Trim()))
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaAsync(modelo.Identificacion.Substring(0, 10), credenciales);
                                    else
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaV2Async(modelo.Identificacion.Substring(0, 10), credenciales);
#endif
                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
#if !DEBUG
                                    if (credencial == null || string.IsNullOrWhiteSpace(credencial.ProductData?.Trim()))
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaAsync(modelo.Identificacion, credenciales);
                                    else
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaV2Async(modelo.Identificacion, credenciales);
#endif
                                }
                                else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
#if !DEBUG
                                    if (credencial == null || string.IsNullOrWhiteSpace(credencial.ProductData?.Trim()))
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaAsync(modelo.Identificacion, credenciales);
                                    else
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaV2Async(modelo.Identificacion, credenciales);
#endif
                                }

                                if (r_burocreditoEquifax != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(credencial.TokenAcceso?.Trim()) && !string.IsNullOrWhiteSpace(r_burocreditoEquifax.TokenAcceso?.Trim()) && credencial.TokenAcceso.Trim() != r_burocreditoEquifax.TokenAcceso.Trim())
                                    {
                                        credencial.TokenAcceso = r_burocreditoEquifax.TokenAcceso;
                                        credencial.FechaCreacionToken = r_burocreditoEquifax.FechaCreacionToken;
                                        await _credencialesBuro.UpdateAsync(credencial);
                                    }
                                    else if (!string.IsNullOrWhiteSpace(r_burocreditoEquifax.TokenAcceso?.Trim()) && string.IsNullOrWhiteSpace(credencial.TokenAcceso?.Trim()))
                                    {
                                        credencial.TokenAcceso = r_burocreditoEquifax.TokenAcceso;
                                        credencial.FechaCreacionToken = r_burocreditoEquifax.FechaCreacionToken;
                                        await _credencialesBuro.UpdateAsync(credencial);
                                    }
                                }

                                if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados == null)
                                {
                                    dataError = JsonConvert.SerializeObject(r_burocreditoEquifax);
                                    r_burocreditoEquifax = null;
                                }
                            }
                        }
                        else
                            throw new Exception("No se pudo realizar la consulta de Buró de Crédito en ninguna de las fuentes");
                    }
                    else
                    {
                        var pathBase = System.IO.Path.Combine("wwwroot", "data");
                        var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                        if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                        {
                            if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                            {
                                var pathBuroEmpresa = Path.Combine(pathFuentes, "buroAvalEmpresaDemo.json");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroEmpresa));
                            }
                            else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                var pathBuroCedula = Path.Combine(pathFuentes, "buroAvalCedulaDemo.json");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroCedula));
                            }
                        }
                        else if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                var pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaDemo.json");
                                r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(System.IO.File.ReadAllText(pathBuroEquifax));
                            }
                            else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                            {
                                var pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaDemo.json");
                                r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(System.IO.File.ReadAllText(pathBuroEquifax));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar fuente Buró de Crédito con identificación {modelo.Identificacion}: {ex.Message}");
                }

                if (r_burocredito == null && planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                {
                    var datosDetalleBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosDetalleBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                    {
                        _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheBuroCredito = true;
                        r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(datosDetalleBuroCredito);
                    }
                }
                else if (r_burocreditoEquifax == null && planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                {
                    var datosDetalleBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosDetalleBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                    {
                        _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheBuroCredito = true;
                        r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(datosDetalleBuroCredito);
                    }
                }

                if ((historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && r_burocredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                {
                    var deudaSuma = 0.00;
                    var ingresoPrevio = 0.00;
                    var ingresoInferior = string.Empty;
                    var ingresoSuperior = string.Empty;

                    if (r_burocredito != null && r_burocredito.Result != null && r_burocredito.Result.DeudaVigenteTotal != null && r_burocredito.Result.DeudaVigenteTotal.Any())
                    {
                        var deudaVencido = r_burocredito.Result.DeudaVigenteTotal.Sum(x => x.ValorVencido);
                        var deudaDemandaJudicial = r_burocredito.Result.DeudaVigenteTotal.Sum(x => x.ValorDemandaJudicial);
                        var deudaCarteraCastigada = r_burocredito.Result.DeudaVigenteTotal.Sum(x => x.CarteraCastigada);
                        deudaSuma = (double)(deudaVencido + deudaDemandaJudicial + deudaCarteraCastigada);
                    }
                    if (r_burocredito != null && r_burocredito.Result != null && r_burocredito.Result.Ingreso != null && r_burocredito.Result.Ingreso.Any() && !string.IsNullOrEmpty(r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso))
                    {
                        var posibleIngreso = Regex.Matches(r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso, @"\d\.?\d+\,?\d*");
                        if (posibleIngreso.Count() == 2)
                        {
                            ingresoInferior = posibleIngreso[0].ToString().Replace(".", "").Replace(",", ".");
                            ingresoSuperior = posibleIngreso[1].ToString().Replace(".", "").Replace(",", ".");
                        }
                        else if (posibleIngreso.Count() == 1)
                            ingresoSuperior = posibleIngreso[0].ToString().Replace(".", "").Replace(",", ".");
                    }

                    if (r_burocredito != null && r_burocredito.Result.GastoFinanciero != null && r_burocredito.Result.GastoFinanciero.Any() && r_burocredito.Result.GastoFinanciero.FirstOrDefault().CuotaEstimadaTitular > 0 && deudaSuma == 0)
                    {
                        ingresoPrevio = (double)(r_burocredito.Result.GastoFinanciero.FirstOrDefault()?.CuotaEstimadaTitular * 1.40);
                        if (double.TryParse(ingresoSuperior, out _) && double.Parse(ingresoSuperior) > 0 && double.Parse(ingresoSuperior) >= ingresoPrevio)
                            r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoSuperior;
                        else if (!r_burocredito.Result.Ingreso.Any())
                            r_burocredito.Result.Ingreso.Add(new Externos.Logica.BuroCredito.Modelos.CreditoRespuesta.Ingreso() { RangoIngreso = ingresoPrevio.ToString("N", culture) });
                        else
                            r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoPrevio.ToString("N", culture);
                    }
                    else if (r_burocredito != null && r_burocredito.Result.GastoFinanciero != null && r_burocredito.Result.GastoFinanciero.Any() && r_burocredito.Result.GastoFinanciero.FirstOrDefault().CuotaEstimadaTitular > 0 && deudaSuma > 0)
                    {
                        ingresoPrevio = (double)(r_burocredito.Result.GastoFinanciero.FirstOrDefault().CuotaEstimadaTitular * 1.40);
                        if (double.TryParse(ingresoInferior, out _) && double.Parse(ingresoInferior) > 0)
                            r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoInferior;
                        else if (double.TryParse(ingresoSuperior, out _) && double.Parse(ingresoSuperior) > 0)
                            r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoSuperior;
                        else if (!r_burocredito.Result.Ingreso.Any())
                            r_burocredito.Result.Ingreso.Add(new Externos.Logica.BuroCredito.Modelos.CreditoRespuesta.Ingreso() { RangoIngreso = ingresoPrevio.ToString("N", culture) });
                        else
                            r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoPrevio.ToString("N", culture);
                    }
                    else if (double.TryParse(ingresoSuperior, out _) && double.Parse(ingresoSuperior) > 0 && deudaSuma == 0)
                        r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoSuperior;
                    else if (double.TryParse(ingresoInferior, out _) && double.Parse(ingresoInferior) > 0 && deudaSuma > 0)
                        r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoInferior;
                    else if (r_burocredito != null && r_burocredito.Result != null && r_burocredito.Result.Ingreso != null && r_burocredito.Result.Ingreso.Any())
                        r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoSuperior;
                }
                else if ((historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && r_burocreditoEquifax != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                {
                    var deudaSumaEquifax = 0.00;
                    var ingresoPrevioEquifax = 0.00;
                    var ingresoEstimadoEquifax = 0.00;
                    if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360 != null && r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Any())
                    {
                        var deudaVencido = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != string.Empty).Sum(x => x.Vencido);
                        var deudaDemandaJudicial = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != string.Empty).Sum(x => x.DemandaJudicial);
                        var deudaCarteraCastigada = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != string.Empty).Sum(x => x.CarteraCastigada);
                        deudaSumaEquifax = (double)(deudaVencido + deudaDemandaJudicial + deudaCarteraCastigada);
                    }
                    if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago > 0 && deudaSumaEquifax == 0)
                    {
                        ingresoPrevioEquifax = r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago * 1.40;
                        if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 != null && r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor > 0)
                        {
                            ingresoEstimadoEquifax = (double)r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor;
                            if (ingresoEstimadoEquifax > 0 && (double)ingresoEstimadoEquifax > ingresoPrevioEquifax)
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoEstimadoEquifax;
                            else
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                        }
                        else if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 == null)
                            r_burocreditoEquifax.Resultados.IndicadorCOVID0 = new Externos.Logica.Equifax.Resultados._IndicadorCOVID0() { IncomePredictor = (decimal)ingresoPrevioEquifax };
                        else
                            r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                    }
                    else if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago > 0 && deudaSumaEquifax > 0)
                    {
                        ingresoPrevioEquifax = r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago * 1.40;
                        if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 != null && r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor > 0)
                        {
                            ingresoEstimadoEquifax = (double)r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor;
                            if (ingresoEstimadoEquifax > 0)
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoEstimadoEquifax;
                            else
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                        }
                        else if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 == null)
                            r_burocreditoEquifax.Resultados.IndicadorCOVID0 = new Externos.Logica.Equifax.Resultados._IndicadorCOVID0() { IncomePredictor = (decimal)ingresoPrevioEquifax };
                        else
                            r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                    }
                }

                datos = new BuroCreditoMetodoViewModel()
                {
                    BuroCredito = r_burocredito,
                    DatosCache = cacheBuroCredito,
                    Fuente = planBuroCredito.Fuente,
                    BuroCreditoEquifax = r_burocreditoEquifax
                };

                _logger.LogInformation("Fuente de Buró de Crédito procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        if (planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                        {
                            var codigoBuro = new List<string> { "A401", "A402", "A410", "A404", "A405", "A406", "A407", "A408", "A409", "A411", "A412", "A415", "A416", "A418", "A419", "A420", "A909", "A999" };
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.BuroCredito,
                                Generado = datos.BuroCredito != null && !codigoBuro.Contains(datos.BuroCredito.ResponseCode),
                                Data = datos.BuroCredito != null ? JsonConvert.SerializeObject(datos.BuroCredito) : null,
                                Cache = cacheBuroCredito,
                                DataError = !string.IsNullOrEmpty(dataError) ? dataError : null,
                                FechaRegistro = DateTime.Now,
                                Reintento = null,
                                Observacion = datos.BuroCredito != null && !string.IsNullOrEmpty(datos.BuroCredito.Usuario) ? $"Usuario WS AVAL: {datos.BuroCredito.Usuario}" : null
                            });
                            _logger.LogInformation("Historial de la Fuente Aval Buró de Crédito procesado correctamente");

                        }
                        else if (planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.BuroCredito,
                                Generado = datos.BuroCreditoEquifax != null,
                                Data = datos.BuroCreditoEquifax != null ? JsonConvert.SerializeObject(datos.BuroCreditoEquifax) : null,
                                Cache = cacheBuroCredito,
                                DataError = !string.IsNullOrEmpty(dataError) ? dataError : null,
                                FechaRegistro = DateTime.Now,
                                Reintento = null,
                                Observacion = datos.BuroCreditoEquifax != null && !string.IsNullOrEmpty(datos.BuroCreditoEquifax.Usuario) ? $"Usuario WS Equifax: {datos.BuroCreditoEquifax.Usuario}" : null
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax Buró de Crédito procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                if (datos != null)
                {
                    if (datos.BuroCreditoEquifax != null)
                    {
                        datos.BuroCreditoEquifax.Usuario = null;
                        datos.BuroCreditoEquifax.Clave = null;
                    }

                    if (datos.BuroCredito != null)
                    {
                        datos.BuroCredito.Usuario = null;
                        datos.BuroCredito.Clave = null;
                    }
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<SuperBancosApiViewModel> ObtenerReporteSuperBancos(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosCedula = null;
                Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosNatural = null;
                Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosEmpresa = null;
                var datos = new SuperBancosApiViewModel();
                var cacheSuperBancosCedula = false;
                var cacheSuperBancosNatural = false;
                var cacheSuperBancosEmpresa = false;
                Historial historialTemp = null;
                var swEmpresa = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SuperBancos identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var fechaExpedicion = _superBancos.GetType().GetProperty("FechaExpedicionCedula");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                        {
                            if (historialTemp != null && string.IsNullOrEmpty(historialTemp.FechaExpedicionCedula?.Trim()))
                                throw new Exception($"No se puede consultar super de bancos porque no se tiene fecha de expedición de cédula. Identificación: {modelo.Identificacion}");

                            //Cedula
                            if (fechaExpedicion != null)
                                fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

                            r_superBancosCedula = await _superBancos.GetRespuestaAsync(modelo.Identificacion);
                            r_superBancosCedula.Reporte = null;

                            //Natural
                            if (fechaExpedicion != null)
                                fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

                            r_superBancosNatural = await _superBancos.GetRespuestaAsync($"{modelo.Identificacion}001");
                            r_superBancosNatural.Reporte = null;
                        }
                        else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                        {
                            if (historialTemp != null && string.IsNullOrEmpty(historialTemp.FechaExpedicionCedula?.Trim()))
                                throw new Exception($"No se puede consultar super de bancos porque no se tiene fecha de expedición de cédula. Identificación: {modelo.Identificacion}");

                            //Natural
                            if (fechaExpedicion != null)
                                fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

                            r_superBancosNatural = await _superBancos.GetRespuestaAsync(modelo.Identificacion);
                            r_superBancosNatural.Reporte = null;

                            //Cedula
                            var cedula = modelo.Identificacion.Substring(0, 10);
                            if (fechaExpedicion != null)
                                fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

                            r_superBancosCedula = await _superBancos.GetRespuestaAsync(cedula);
                            r_superBancosCedula.Reporte = null;
                        }
                        else
                        {
                            //Juridico
                            swEmpresa = true;
                            r_superBancosEmpresa = await _superBancos.GetRespuestaAsync(modelo.Identificacion);

                            if (r_superBancosEmpresa != null && r_superBancosEmpresa.Reporte.Length > 0)
                                r_superBancosEmpresa.Reporte = null;

                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.FechaExpedicionCedula?.Trim()))
                            {
                                //Natural
                                var rucNatural = $"{historialTemp.IdentificacionSecundaria}001";
                                if (fechaExpedicion != null)
                                    fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

                                r_superBancosNatural = await _superBancos.GetRespuestaAsync(rucNatural);

                                if (r_superBancosNatural != null && r_superBancosNatural.Reporte.Length > 0)
                                    r_superBancosNatural.Reporte = null;

                                //Cedula
                                var cedula = historialTemp.IdentificacionSecundaria;
                                if (fechaExpedicion != null)
                                    fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

                                r_superBancosCedula = await _superBancos.GetRespuestaAsync(cedula);

                                if (r_superBancosCedula != null && r_superBancosCedula.Reporte.Length > 0)
                                    r_superBancosCedula.Reporte = null;
                            }
                            else
                                _logger.LogInformation($"No se puede consultar super de bancos porque no se tiene fecha de expedición de cédula. Identificación: {modelo.Identificacion}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SuperBancos con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_superBancosCedula == null)
                    {
                        var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosSuperBancos != null)
                        {
                            cacheSuperBancosCedula = true;
                            r_superBancosCedula = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
                        }
                    }

                    if (r_superBancosNatural == null)
                    {
                        var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosSuperBancos != null)
                        {
                            cacheSuperBancosNatural = true;
                            r_superBancosNatural = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
                        }
                    }

                    if (r_superBancosEmpresa == null && swEmpresa)
                    {
                        var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosSuperBancos != null)
                        {
                            cacheSuperBancosEmpresa = true;
                            r_superBancosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
                        }
                    }

                    datos = new SuperBancosApiViewModel()
                    {
                        SuperBancosCedula = r_superBancosCedula,
                        SuperBancosNatural = r_superBancosNatural,
                        SuperBancosEmpresa = r_superBancosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSuperBancos = Path.Combine(pathFuentes, "superBancosDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathSuperBancos);
                    datos = JsonConvert.DeserializeObject<SuperBancosApiViewModel>(archivo);

                    if (datos.SuperBancosCedula != null)
                        datos.SuperBancosCedula.Reporte = null;

                    if (datos.SuperBancosNatural != null)
                        datos.SuperBancosNatural.Reporte = null;

                    if (datos.SuperBancosEmpresa != null)
                        datos.SuperBancosEmpresa.Reporte = null;
                }

                _logger.LogInformation("Fuente de SuperBancos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SuperBancos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.SuperBancos,
                            Generado = datos.SuperBancosCedula != null,
                            Data = datos.SuperBancosCedula != null ? JsonConvert.SerializeObject(datos.SuperBancosCedula) : null,
                            Cache = cacheSuperBancosCedula,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente SuperBancos procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.SuperBancosNatural,
                            Generado = datos.SuperBancosNatural != null,
                            Data = datos.SuperBancosNatural != null ? JsonConvert.SerializeObject(datos.SuperBancosNatural) : null,
                            Cache = cacheSuperBancosNatural,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente SuperBancos Natural procesado correctamente");

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.SuperBancosEmpresa,
                            Generado = datos.SuperBancosEmpresa != null,
                            Data = datos.SuperBancosEmpresa != null ? JsonConvert.SerializeObject(datos.SuperBancosEmpresa) : null,
                            Cache = cacheSuperBancosEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente SuperBancos Juridico procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<AntecedentesPenalesApiViewModel> ObtenerReporteAntecedentesPenales(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.AntecedentesPenales.Modelos.Resultado r_antecedentes = null;
                var datos = new AntecedentesPenalesApiViewModel();
                Historial historialTemp = null;
                var cacheAntecedentes = false;
                ResultadoAntecedentesPenales resultadoAntecedentesPenales = null;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Antecedentes Penales identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (historialTemp != null && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)) && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                            {
                                resultadoAntecedentesPenales = await _antecedentes.GetRespuestaAsyncV2(historialTemp.IdentificacionSecundaria);
                                r_antecedentes = resultadoAntecedentesPenales?.Resultado;
                            }
                            else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    resultadoAntecedentesPenales = await _antecedentes.GetRespuestaAsyncV2(modelo.Identificacion);
                                    r_antecedentes = resultadoAntecedentesPenales?.Resultado;
                                }
                            }
                        }
                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            resultadoAntecedentesPenales = await _antecedentes.GetRespuestaAsyncV2(modelo.Identificacion);
                            r_antecedentes = resultadoAntecedentesPenales?.Resultado;
                        }

                        if (r_antecedentes != null && r_antecedentes.Reporte.Length > 0)
                            r_antecedentes.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Antecedentes Penales con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_antecedentes == null)
                    {
                        var datosAntecedentes = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.AntecedentesPenales && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosAntecedentes != null)
                        {
                            cacheAntecedentes = true;
                            r_antecedentes = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.Resultado>(datosAntecedentes);
                        }
                    }

                    datos = new AntecedentesPenalesApiViewModel()
                    {
                        Antecedentes = r_antecedentes,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathAntecedentes = Path.Combine(pathFuentes, "antecedentesDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathAntecedentes);
                    datos = JsonConvert.DeserializeObject<AntecedentesPenalesApiViewModel>(archivo);
                    datos.Antecedentes.Reporte = null;
                }

                _logger.LogInformation("Fuente de Antecedentes Penales procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Antecedentes Penales. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.AntecedentesPenales,
                            Generado = datos.Antecedentes != null,
                            Data = datos.Antecedentes != null ? JsonConvert.SerializeObject(datos.Antecedentes) : null,
                            Cache = cacheAntecedentes,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = resultadoAntecedentesPenales != null ? resultadoAntecedentesPenales.Error : null,
                            FuenteActiva = resultadoAntecedentesPenales != null ? resultadoAntecedentesPenales.FuenteActiva : null
                        });
                        _logger.LogInformation("Historial de la Fuente Antecedentes Penales procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<PrediosApiViewModel> ObtenerReportePredios(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.Resultado r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.Resultado r_prediosEmpresa = null;
                var datos = new PrediosApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Predios identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (historialTemp != null)
                        {
                            if ((ValidacionViewModel.ValidarCedula(historialTemp.Identificacion) || ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria)) && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                r_prediosRepresentante = await _predios.GetRespuestaAsync(historialTemp.NombresPersona);

                            if ((ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)) && !string.IsNullOrEmpty(historialTemp.RazonSocialEmpresa) && historialTemp.NombresPersona != historialTemp.RazonSocialEmpresa)
                            {
                                r_prediosEmpresa = await _predios.GetRespuestaAsync(historialTemp.RazonSocialEmpresa);
                                busquedaEmpresa = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null && busquedaEmpresa)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosApiViewModel>(archivo);
                }

                _logger.LogInformation("Fuente de Predio procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipio,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresa,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<PrediosCuencaApiViewModel> ObtenerReportePrediosCuenca(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca r_prediosEmpresa = null;
                var datos = new PrediosCuencaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Predios Cuenca identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosCuenca(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosCuenca(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosCuenca(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosCuenca(cedulaTemp);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Cuenca con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null && busquedaEmpresa)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosCuencaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosCuencaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosCuencaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Cuenca procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Cuenca. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCuenca,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Cuenca procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Cuenca procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosSantoDomingoApiViewModel> ObtenerReportePrediosSantoDomingo(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo r_prediosEmpresa = null;
                var datos = new PrediosSantoDomingoApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Santo Domingo Cuenca identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosSantoDomingo(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosSantoDomingo(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosSantoDomingo(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosSantoDomingo(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Santo Domingo con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null || (r_prediosRepresentante != null && r_prediosRepresentante.Detalle != null && !r_prediosRepresentante.Detalle.Any()))
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(datosPredios);
                        }
                    }

                    if ((r_prediosEmpresa == null || (r_prediosEmpresa != null && r_prediosEmpresa.Detalle != null && !r_prediosEmpresa.Detalle.Any())) && busquedaEmpresa)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosSantoDomingoApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosSantoDomingoDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosSantoDomingoApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Santo Domingo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santo Domingo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Santo Domingo procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosRuminahuiApiViewModel> ObtenerReportePrediosRuminahui(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui r_prediosEmpresa = null;
                var datos = new PrediosRuminahuiApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Rumiñahui Cuenca identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosRuminahui(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosRuminahui(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosRuminahui(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosRuminahui(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Rumiñahui con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null || (r_prediosRepresentante != null && r_prediosRepresentante.Detalle != null && !r_prediosRepresentante.Detalle.Any()))
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(datosPredios);
                        }
                    }

                    if ((r_prediosEmpresa == null || (r_prediosEmpresa != null && r_prediosEmpresa.Detalle != null && !r_prediosEmpresa.Detalle.Any())) && busquedaEmpresa)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosRuminahuiApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosRuminahuiDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosRuminahuiApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Rumiñahui procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Rumiñahui. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioRuminahui,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Rumiñahui procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosQuinindeApiViewModel> ObtenerReportePrediosQuininde(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde r_prediosEmpresa = null;
                var datos = new PrediosQuinindeApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Quinindé Cuenca identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosQuininde(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosQuininde(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosQuininde(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosQuininde(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Quinindé con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosQuinindeApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosQuinindeDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosQuinindeApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Quinindé procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Quinindé. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioQuininde,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Quinindé procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosLatacungaApiViewModel> ObtenerReportePrediosLatacunga(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga r_prediosEmpresa = null;
                var datos = new PrediosLatacungaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Latacunga identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosLatacunga(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosLatacunga(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosLatacunga(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosLatacunga(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Latacunga con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLatacunga && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosLatacungaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosLatacungaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosLatacungaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Latacunga procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Latacunga. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLatacunga,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Latacunga procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosMantaApiViewModel> ObtenerReportePrediosManta(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosManta r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosManta r_prediosEmpresa = null;
                var datos = new PrediosMantaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Manta identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosManta(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosManta(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosManta(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosManta(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Latacunga con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioManta && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosManta>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosManta>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosMantaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosMantaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosMantaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Manta procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Manta. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioManta,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Manta procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosAmbatoApiViewModel> ObtenerReportePrediosAmbato(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato r_prediosEmpresa = null;
                var datos = new PrediosAmbatoApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Ambato identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosAmbato(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosAmbato(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosAmbato(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosAmbato(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Latacunga con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAmbato && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosAmbatoApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosAmbatoDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosAmbatoApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Ambato procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ambato. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioAmbato,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Ambato procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosIbarraApiViewModel> ObtenerReportePrediosIbarra(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra r_prediosEmpresa = null;
                var datos = new PrediosIbarraApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Ibarra identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosIbarra(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosIbarra(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosIbarra(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosIbarra(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Latacunga con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioIbarra && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosIbarraApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosIbarraDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosIbarraApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Ibarra procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ibarra. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioIbarra,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Ibarra procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosSanCristobalApiViewModel> ObtenerReportePrediosSanCristobal(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal r_prediosEmpresa = null;
                var datos = new PrediosSanCristobalApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente San Cristóbal identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosSanCristobal(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosSanCristobal(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosSanCristobal(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosSanCristobal(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios San Cristóbal con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosSanCristobalApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosSanCristobalDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosSanCristobalApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio San Cristóbal procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio San Cristóbal. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio San Cristóbal procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosDuranApiViewModel> ObtenerReportePrediosDuran(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosDuran r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosDuran r_prediosEmpresa = null;
                var datos = new PrediosDuranApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Durán identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosDuran(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosDuran(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosDuran(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosDuran(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Durán con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDuran && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDuran>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDuran>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosDuranApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosDuranDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosDuranApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Durán procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Durán. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioDuran,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Durán procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosLagoAgrioApiViewModel> ObtenerReportePrediosLagoAgrio(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio r_prediosEmpresa = null;
                var datos = new PrediosLagoAgrioApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Lago Agrio identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosLagoAgrio(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosLagoAgrio(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosLagoAgrio(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosLagoAgrio(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Lago Agrio con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosLagoAgrioApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosLagoAgrioDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosLagoAgrioApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Lago Agrio procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Lago Agrio. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Lago Agrio procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosSantaRosaApiViewModel> ObtenerReportePrediosSantaRosa(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa r_prediosEmpresa = null;
                var datos = new PrediosSantaRosaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Lago Agrio identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosSantaRosa(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosSantaRosa(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosSantaRosa(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosSantaRosa(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Santa Rosa con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosSantaRosaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosSantaRosaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosSantaRosaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Santa Rosa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santa Rosa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Santa Rosa procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosSucuaApiViewModel> ObtenerReportePrediosSucua(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSucua r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosSucua r_prediosEmpresa = null;
                var datos = new PrediosSucuaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Sucúa identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosSucua(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosSucua(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosSucua(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosSucua(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Sucúa con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSucua && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSucua>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSucua>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosSucuaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosSucuaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosSucuaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Sucúa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sucúa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSucua,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Sucúa procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosSigSigApiViewModel> ObtenerReportePrediosSigSig(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig r_prediosEmpresa = null;
                var datos = new PrediosSigSigApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Sígsig identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosSigSig(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosSigSig(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosSigSig(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosSigSig(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Sígsig con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSigSig && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosSigSigApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosSigSigDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosSigSigApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Sígsig procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sígsig. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSigSig,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Sígsig procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosMejiaApiViewModel> ObtenerReportePrediosMejia(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosMejia r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosMejia r_prediosEmpresa = null;
                var datos = new PrediosMejiaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Mejia identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosMejia(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosMejia(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosMejia(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosMejia(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Mejia con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMejia && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMejia>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMejia>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosMejiaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosMejiaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosMejiaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Mejia procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Mejia. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMejia,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Mejia procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosMoronaApiViewModel> ObtenerReportePrediosMorona(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosMorona r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosMorona r_prediosEmpresa = null;
                var datos = new PrediosMoronaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Morona identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosMorona(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosMorona(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosMorona(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosMorona(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Morona con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMorona && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMorona>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMorona>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosMoronaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosMoronaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosMoronaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Morona procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Morona. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMorona,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Morona procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosTenaApiViewModel> ObtenerReportePrediosTena(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosTena r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosTena r_prediosEmpresa = null;
                var datos = new PrediosTenaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Tena identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosTena(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosTena(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosTena(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosTena(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Tena con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioTena && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosTena>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosTena>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosTenaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosTenaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosTenaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Tena procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Tena. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioTena,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Tena procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosCatamayoApiViewModel> ObtenerReportePrediosCatamayo(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo r_prediosEmpresa = null;
                var datos = new PrediosCatamayoApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Catamayo identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = await _predios.GetPrediosCatamayo(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = await _predios.GetPrediosCatamayo(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = await _predios.GetPrediosCatamayo(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = await _predios.GetPrediosCatamayo(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Catamayo con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCatamayo && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosCatamayoApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosCatamayoDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosCatamayoApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Catamayo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Catamayo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCatamayo,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Catamayo procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosLojaApiViewModel> ObtenerReportePrediosLoja(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosLoja r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosLoja r_prediosEmpresa = null;
                var datos = new PrediosLojaApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Loja identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = await _predios.GetRespuestaAsyncLoja(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = await _predios.GetRespuestaAsyncLoja(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = await _predios.GetRespuestaAsyncLoja(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = await _predios.GetRespuestaAsyncLoja(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Loja con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLoja && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLoja>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLoja>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosLojaApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosLojaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosLojaApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Loja procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Loja. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLoja,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Loja procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosSamborondonApiViewModel> ObtenerReportePrediosSamborondon(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon r_prediosEmpresa = null;
                var datos = new PrediosSamborondonApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Samborondon identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosSamborondon(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosSamborondon(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosSamborondon(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosSamborondon(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Samborondon con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSamborondon && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosSamborondonApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosSamborondonDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosSamborondonApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Samborondon procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Samborondon. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSamborondon,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Samborondon procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosDauleApiViewModel> ObtenerReportePrediosDaule(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosDaule r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosDaule r_prediosEmpresa = null;
                var datos = new PrediosDauleApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Daule identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosDaule(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosDaule(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosDaule(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosDaule(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Daule con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDaule && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDaule>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDaule && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDaule>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosDauleApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosDauleDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosDauleApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Daule procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Daule. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioDaule,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Daule procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDaule,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosCayambeApiViewModel> ObtenerReportePrediosCayambe(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe r_prediosEmpresa = null;
                var datos = new PrediosCayambeApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Cayambe identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosCayambe(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosCayambe(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosCayambe(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosCayambe(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Cayambe con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCayambe && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCayambe && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosCayambeApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosCayambeDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosCayambeApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Cayambe procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Cayambe. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCayambe,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Cayambe procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCayambe,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosAzoguesApiViewModel> ObtenerReportePrediosAzogues(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues r_prediosEmpresa = null;
                var datos = new PrediosAzoguesApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Azogues identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosAzogues(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosAzogues(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosAzogues(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosAzogues(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Azogues con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAzogues && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAzogues && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosAzoguesApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosAzoguesDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosAzoguesApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Azogues procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Azogues. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioAzogues,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Azogues procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAzogues,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosEsmeraldasApiViewModel> ObtenerReportePrediosEsmeraldas(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas r_prediosEmpresa = null;
                var datos = new PrediosEsmeraldasApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Esmeraldas identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosEsmeraldas(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosEsmeraldas(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosEsmeraldas(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosEsmeraldas(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Predios Esmeraldas con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEsmeraldas && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaEsmeraldas && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosEsmeraldasApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosEsmeraldasDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosEsmeraldasApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de Predio Esmeraldas procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Esmeraldas. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEsmeraldas,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Predio Esmeraldas procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaEsmeraldas,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<PrediosCotacachiApiViewModel> ObtenerReportePrediosCotacachi(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi r_prediosRepresentante = null;
                Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi r_prediosEmpresa = null;
                var datos = new PrediosCotacachiApiViewModel();
                Historial historialTemp = null;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var cedulaEntidades = false;
                var busquedaEmpresa = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando FuenteCotacachi identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_prediosRepresentante = _predios.GetPrediosCotacachi(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    r_prediosEmpresa = _predios.GetPrediosCotacachi(modelo.Identificacion);
                                    busquedaEmpresa = true;
                                }

                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        r_prediosRepresentante = _predios.GetPrediosCotacachi(historialTemp.IdentificacionSecundaria.Trim());

                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        r_prediosRepresentante = _predios.GetPrediosCotacachi(cedulaTemp);
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente PrediosCotacachi con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_prediosRepresentante == null)
                    {
                        var datosPredios = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCotacachi && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPredios != null)
                        {
                            cachePredios = true;
                            r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi>(datosPredios);
                        }
                    }

                    if (r_prediosEmpresa == null)
                    {
                        var datosPrediosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCotacachi && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosPrediosEmpresa != null)
                        {
                            cachePrediosEmpresa = true;
                            r_prediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi>(datosPrediosEmpresa);
                        }
                    }

                    datos = new PrediosCotacachiApiViewModel()
                    {
                        PrediosRepresentante = r_prediosRepresentante,
                        PrediosEmpresa = r_prediosEmpresa
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathPredios = Path.Combine(pathFuentes, "prediosCotacachiDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathPredios);
                    datos = JsonConvert.DeserializeObject<PrediosCotacachiApiViewModel>(archivo);
                    busquedaEmpresa = true;
                }

                _logger.LogInformation("Fuente de PredioCotacachi procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente PredioCotacachi. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCotacachi,
                            Generado = datos.PrediosRepresentante != null,
                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
                            Cache = cachePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente PredioCotacachi procesado correctamente");

                        if (busquedaEmpresa)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCotacachi,
                                Generado = datos.PrediosEmpresa != null,
                                Data = datos.PrediosEmpresa != null ? JsonConvert.SerializeObject(datos.PrediosEmpresa) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<FiscaliaDelitosApiViewModel> ObtenerReporteFiscaliaDelitos(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito fiscaliaPersona = null;
                Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito fiscaliaEmpresa = null;
                var datos = new FiscaliaDelitosApiViewModel();
                Historial historialTemp = null;
                var cedulaEntidades = false;
                var cacheFiscalia = false;
                var cacheFiscaliaEmpresa = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Fiscalía Delitos identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    fiscaliaPersona = await _fiscaliaDelitos.GetRespuestaAsync(modelo.Identificacion);
                            }
                            else
                            {
                                if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                {
                                    fiscaliaEmpresa = await _fiscaliaDelitos.GetRespuestaAsync(modelo.Identificacion);
                                    if (fiscaliaEmpresa == null)
                                    {
                                        var datosDetalleFiscaliaEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                        if (datosDetalleFiscaliaEmpresa != null)
                                        {
                                            cacheFiscaliaEmpresa = true;
                                            fiscaliaEmpresa = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(datosDetalleFiscaliaEmpresa);
                                        }
                                    }
                                }
                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                        fiscaliaPersona = await _fiscaliaDelitos.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                }
                                else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        fiscaliaPersona = await _fiscaliaDelitos.GetRespuestaAsync(cedulaTemp);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Fiscalía Delitos con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (fiscaliaPersona == null)
                    {
                        var datosDetalleFiscaliaPersona = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosPersona && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleFiscaliaPersona != null)
                        {
                            cacheFiscalia = true;
                            fiscaliaPersona = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(datosDetalleFiscaliaPersona);
                        }
                    }

                    datos = new FiscaliaDelitosApiViewModel()
                    {
                        FiscaliaPersona = fiscaliaPersona,
                        FiscaliaEmpresa = fiscaliaEmpresa,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathLegal = Path.Combine(pathFuentes, "fiscaliaDelitosDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathLegal);
                    datos = JsonConvert.DeserializeObject<FiscaliaDelitosApiViewModel>(archivo);
                }

                _logger.LogInformation("Fuente de Fiscalía Delitos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Fiscalía Delitos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.FiscaliaDelitosPersona,
                            Generado = datos.FiscaliaPersona != null,
                            Data = datos.FiscaliaPersona != null ? JsonConvert.SerializeObject(datos.FiscaliaPersona) : null,
                            Cache = cacheFiscalia,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa,
                            Generado = datos.FiscaliaEmpresa != null,
                            Data = datos.FiscaliaEmpresa != null ? JsonConvert.SerializeObject(datos.FiscaliaEmpresa) : null,
                            Cache = cacheFiscaliaEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Fiscalía Delitos procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<PensionAlimenticiaApiViewModel> ObtenerReportePensionAlimenticia(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia r_pension = null;
                var datos = new PensionAlimenticiaApiViewModel();
                var cachePension = false;
                var historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                ResultadoResponsePensionAlmenticia resultadoPAlimenticia = null;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Pension alimenticia identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                            {
                                {
                                    resultadoPAlimenticia = await _pension.GetRespuestaAsyncV2(modelo.Identificacion);
                                    r_pension = resultadoPAlimenticia?.PensionAlimenticia;
                                }
                            }
                            if (r_pension == null && historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                            {
                                resultadoPAlimenticia = await _pension.GetRespuestaAsyncV2(historialTemp.IdentificacionSecundaria);
                                r_pension = resultadoPAlimenticia?.PensionAlimenticia;
                            }
                        }
                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            resultadoPAlimenticia = await _pension.GetRespuestaAsyncV2(modelo.Identificacion);
                            r_pension = resultadoPAlimenticia?.PensionAlimenticia;
                        }

                        if (r_pension != null && r_pension.Resultados == null)
                            r_pension = null;

                        if (r_pension == null)
                        {
                            var datosDetallePension = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetallePension != null)
                            {
                                cachePension = true;
                                r_pension = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(datosDetallePension);
                            }
                        }

                        datos = new PensionAlimenticiaApiViewModel()
                        {
                            PensionAlimenticia = r_pension
                        };

                        if (datos.PensionAlimenticia != null && datos.PensionAlimenticia.Resultados != null)
                            foreach (var item in datos.PensionAlimenticia.Resultados)
                            {
                                item.Nombre = historialTemp.NombresPersona;
                                item.Cedula = (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico) ? historialTemp.IdentificacionSecundaria : historialTemp.Identificacion;
                            }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Pension Alimenticia con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathAnt = Path.Combine(pathFuentes, "pensionAlimenticiaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathAnt);
                    var pension = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(archivo);
                    datos = new PensionAlimenticiaApiViewModel()
                    {
                        PensionAlimenticia = pension,
                    };
                }

                _logger.LogInformation("Fuente de Pension alimenticia procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Pension alimenticia. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.PensionAlimenticia,
                            Generado = datos.PensionAlimenticia != null,
                            Data = datos.PensionAlimenticia != null ? JsonConvert.SerializeObject(datos.PensionAlimenticia) : null,
                            Cache = cachePension,
                            FechaRegistro = DateTime.Now,
                            Reintento = null,
                            DataError = resultadoPAlimenticia != null ? resultadoPAlimenticia.Error : null,
                            FuenteActiva = resultadoPAlimenticia != null ? resultadoPAlimenticia.FuenteActiva : null
                        });
                        _logger.LogInformation("Historial de la Fuente Pension alimenticia procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<UafeApiViewModel> ObtenerReporteUafe(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.UAFE.Modelos.Resultado r_onu = null;
                Externos.Logica.UAFE.Modelos.Resultado r_onu2206 = null;
                Externos.Logica.UAFE.Modelos.ResultadoInterpol r_interpol = null;
                Externos.Logica.UAFE.Modelos.ResultadoOfac r_ofac = null;
                var cacheOnu = false;
                var cacheOnu2206 = false;
                var cacheInterpol = false;
                var cacheOfac = false;
                string mensajeErrorOnu = null;
                string mensajeErrorOnu2206 = null;
                string mensajeErrorInterpol = null;
                string mensajeErrorOfac = null;
                var accesoOnu = false;

                var empresasOnu = new List<int>();
                try
                {
                    var pathEmpresasConsultaJudicial = Path.Combine("wwwroot", "data", "empresasOnu.json");
                    var archivoOnu = System.IO.File.ReadAllText(pathEmpresasConsultaJudicial);
                    var empresasConsultaOnu = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoOnu);
                    if (empresasConsultaOnu != null && empresasConsultaOnu.Any())
                        empresasOnu = empresasConsultaOnu.Select(m => m.Id).Distinct().ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                var datos = new UafeApiViewModel();
                Historial historialTemp = null;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente UAFE identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (historialTemp != null)
                        {
                            if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                            {
                                if (empresasOnu.Contains(modelo.IdEmpresa))
                                {
                                    r_onu = await _uafe.GetRespuestaAsync(historialTemp.NombresPersona, new[] { historialTemp.RazonSocialEmpresa });
                                    r_onu2206 = _uafe.GetInformacionOnu2206(historialTemp.NombresPersona, historialTemp.RazonSocialEmpresa);
                                    accesoOnu = true;
                                }

                                DateTime? fechaNacimiento = null;
                                var fuentes = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.RegistroCivil };
                                var detallesHistorial = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentes.Contains(m.TipoFuente), null, null, 0, null, true);
                                if (detallesHistorial.Any())
                                {
                                    var registroCivil = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);
                                    if (registroCivil != null)
                                    {
                                        var dataRc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(registroCivil.Data);
                                        if (dataRc != null)
                                            fechaNacimiento = dataRc.FechaNacimiento;
                                    }
                                    else
                                    {
                                        var personaGc = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);
                                        if (personaGc != null)
                                        {
                                            var dataGc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(personaGc.Data);
                                            if (dataGc != null)
                                                fechaNacimiento = dataGc.FechaNacimiento;
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(historialTemp.NombresPersona) && fechaNacimiento.HasValue)
                                    r_interpol = await _uafe.GetInformacionInterpol(historialTemp.NombresPersona, fechaNacimiento);

                                r_ofac = _uafe.GetInformacionOfac(historialTemp.NombresPersona, historialTemp.IdentificacionSecundaria, historialTemp.RazonSocialEmpresa, historialTemp.Identificacion);
                            }
                            else
                            {
                                if (empresasOnu.Contains(modelo.IdEmpresa))
                                {
                                    r_onu = await _uafe.GetRespuestaAsync(historialTemp.NombresPersona, new[] { historialTemp.RazonSocialEmpresa });
                                    r_onu2206 = _uafe.GetInformacionOnu2206(historialTemp.NombresPersona, historialTemp.RazonSocialEmpresa);
                                    accesoOnu = true;
                                }

                                DateTime? fechaNacimiento = null;
                                var fuentes = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.RegistroCivil };
                                var detallesHistorial = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentes.Contains(m.TipoFuente), null, null, 0, null, true);
                                if (detallesHistorial.Any())
                                {
                                    var registroCivil = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);
                                    if (registroCivil != null)
                                    {
                                        var dataRc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(registroCivil.Data);
                                        if (dataRc != null)
                                            fechaNacimiento = dataRc.FechaNacimiento;
                                    }
                                    else
                                    {
                                        var personaGc = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);
                                        if (personaGc != null)
                                        {
                                            var dataGc = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(personaGc.Data);
                                            if (dataGc != null)
                                                fechaNacimiento = dataGc.FechaNacimiento;
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(historialTemp.NombresPersona) && fechaNacimiento.HasValue)
                                    r_interpol = await _uafe.GetInformacionInterpol(historialTemp.NombresPersona, fechaNacimiento);

                                var cedula = string.Empty;
                                if (!ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    cedula = modelo.Identificacion.Substring(0, 10);
                                else
                                    cedula = modelo.Identificacion;

                                r_ofac = _uafe.GetInformacionOfac(historialTemp.NombresPersona, cedula, !ValidacionViewModel.ValidarCedula(modelo.Identificacion) ? historialTemp.NombresPersona : "", !ValidacionViewModel.ValidarCedula(modelo.Identificacion) ? modelo.Identificacion : "");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente UAFE con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (accesoOnu)
                    {
                        var aplicaBusquedaCacheOnu = true;
                        if (datos.ONU != null && datos.ONU.Individuo == null && datos.ONU.Entidad == null && !string.IsNullOrEmpty(datos.ONU.MensajeError))
                        {
                            aplicaBusquedaCacheOnu = false;
                            mensajeErrorOnu = datos.ONU.MensajeError;
                        }

                        var aplicaBusquedaCacheOnu2206 = true;
                        if (datos.ONU2206 != null && datos.ONU2206.Individuo == null && datos.ONU2206.Entidad == null && !string.IsNullOrEmpty(datos.ONU2206.MensajeError))
                        {
                            aplicaBusquedaCacheOnu2206 = false;
                            mensajeErrorOnu2206 = datos.ONU2206.MensajeError;
                        }

                        if (r_onu == null && aplicaBusquedaCacheOnu)
                        {
                            var datosDetalleUafe = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.UafeOnu && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleUafe != null)
                            {
                                cacheOnu = true;
                                r_onu = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.Resultado>(datosDetalleUafe);
                            }
                        }

                        if (r_onu2206 == null && aplicaBusquedaCacheOnu2206)
                        {
                            var datosDetalleUafe = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.UafeOnu2206 && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleUafe != null)
                            {
                                cacheOnu2206 = true;
                                r_onu2206 = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.Resultado>(datosDetalleUafe);
                            }
                        }
                    }

                    var aplicaBusquedaCacheInterpol = true;
                    if (datos.Interpol != null && datos.Interpol.NoticiaIndividuo == null && !string.IsNullOrEmpty(datos.Interpol.MensajeError))
                    {
                        aplicaBusquedaCacheInterpol = false;
                        mensajeErrorInterpol = datos.Interpol.MensajeError;
                    }

                    var aplicaBusquedaCacheOfac = true;
                    if (datos.OFAC != null && string.IsNullOrEmpty(datos.OFAC.ContenidoIndividuo) && string.IsNullOrEmpty(datos.OFAC.ContenidoEmpresa) && !string.IsNullOrEmpty(datos.OFAC.MensajeError))
                    {
                        aplicaBusquedaCacheOfac = false;
                        mensajeErrorOfac = datos.OFAC.MensajeError;
                    }

                    if (r_interpol == null && aplicaBusquedaCacheInterpol)
                    {
                        var datosDetalleUafe = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.UafeInterpol && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleUafe != null)
                        {
                            cacheInterpol = true;
                            r_interpol = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.ResultadoInterpol>(datosDetalleUafe);
                        }
                    }

                    if (r_ofac == null && aplicaBusquedaCacheOfac)
                    {
                        var datosDetalleUafe = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.UafeOfac && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleUafe != null)
                        {
                            cacheOfac = true;
                            r_ofac = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.ResultadoOfac>(datosDetalleUafe);
                        }
                    }

                    datos = new UafeApiViewModel()
                    {
                        ONU = r_onu,
                        ONU2206 = r_onu2206,
                        Interpol = r_interpol,
                        OFAC = r_ofac,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathLegal = Path.Combine(pathFuentes, "uafeDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathLegal);
                    var datosTemp = JsonConvert.DeserializeObject<UafeViewModel>(archivo);
                    datos = new UafeApiViewModel()
                    {
                        ONU = datosTemp.ONU,
                        ONU2206 = datosTemp.ONU2206,
                        Interpol = datosTemp.Interpol,
                        OFAC = datosTemp.OFAC
                    };
                }

                if (accesoOnu)
                {
                    if (datos.ONU != null && datos.ONU.Individuo == null && datos.ONU.Entidad == null)
                        datos.ONU = null;

                    if (datos.ONU2206 != null && datos.ONU2206.Individuo == null && datos.ONU2206.Entidad == null)
                        datos.ONU2206 = null;
                }

                if (datos.Interpol != null && datos.Interpol.NoticiaIndividuo == null)
                    datos.Interpol = null;

                if (datos.OFAC != null && string.IsNullOrEmpty(datos.OFAC.ContenidoIndividuo) && string.IsNullOrEmpty(datos.OFAC.ContenidoEmpresa))
                    datos.OFAC = null;

                _logger.LogInformation("Fuente de UAFE procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente UAFE. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesUafe = new[] { Dominio.Tipos.Fuentes.UafeOnu, Dominio.Tipos.Fuentes.UafeOnu2206, Dominio.Tipos.Fuentes.UafeInterpol, Dominio.Tipos.Fuentes.UafeOfac };
                        var historialesUafe = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesUafe.Contains(m.TipoFuente));
                        var historialOnu = historialesUafe.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.UafeOnu);
                        var historialOnu2206 = historialesUafe.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.UafeOnu2206);
                        var historialInterpol = historialesUafe.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.UafeInterpol);
                        var historialOfac = historialesUafe.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.UafeOfac);

                        if (accesoOnu)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.UafeOnu,
                                Generado = datos.ONU != null,
                                Data = datos.ONU != null ? JsonConvert.SerializeObject(datos.ONU) : null,
                                Cache = cacheOnu,
                                FechaRegistro = DateTime.Now,
                                Reintento = false,
                                DataError = mensajeErrorOnu
                            });

                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.UafeOnu2206,
                                Generado = datos.ONU2206 != null,
                                Data = datos.ONU2206 != null ? JsonConvert.SerializeObject(datos.ONU2206) : null,
                                Cache = cacheOnu2206,
                                FechaRegistro = DateTime.Now,
                                Reintento = false,
                                DataError = mensajeErrorOnu2206
                            });
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.UafeInterpol,
                            Generado = datos.Interpol != null,
                            Data = datos.Interpol != null ? JsonConvert.SerializeObject(datos.Interpol) : null,
                            Cache = cacheInterpol,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = mensajeErrorInterpol
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.UafeOfac,
                            Generado = datos.OFAC != null,
                            Data = datos.OFAC != null ? JsonConvert.SerializeObject(datos.OFAC) : null,
                            Cache = cacheOfac,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = mensajeErrorOfac
                        });
                        _logger.LogInformation("Historial de la Fuente UAFE procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<FuerzasArmadasApiViewModel> ObtenerReporteFuerzasArmadas(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada r_fuerzaArmada = null;
                var cacheFuerzaArmada = false;
                Historial historialTemp = null;
                var datos = new FuerzasArmadasApiViewModel();
                ResultadoPersonaFuerzaArmada resultadoFuerzaArmada = null;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Fuerzas Armadas identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (historialTemp != null && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)) && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                            {
                                resultadoFuerzaArmada = _antecedentes.GetFuerzasArmadasV2(historialTemp.IdentificacionSecundaria);
                                r_fuerzaArmada = resultadoFuerzaArmada?.PersonaFuerzaArmada;
                            }
                            else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    resultadoFuerzaArmada = _antecedentes.GetFuerzasArmadasV2(modelo.Identificacion);
                                    r_fuerzaArmada = resultadoFuerzaArmada?.PersonaFuerzaArmada;
                                }
                            }
                        }
                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            resultadoFuerzaArmada = _antecedentes.GetFuerzasArmadasV2(modelo.Identificacion);
                            r_fuerzaArmada = resultadoFuerzaArmada?.PersonaFuerzaArmada;
                        }

                        if (r_fuerzaArmada != null && r_fuerzaArmada.Reporte.Length > 0)
                            r_fuerzaArmada.Reporte = null;

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Fuerzas Armadas con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_fuerzaArmada == null)
                    {
                        var datosFuerzaArmada = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.FuerzaArmada && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosFuerzaArmada != null)
                        {
                            cacheFuerzaArmada = true;
                            r_fuerzaArmada = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada>(datosFuerzaArmada);
                        }
                    }

                    datos = new FuerzasArmadasApiViewModel()
                    {
                        FuerzasArmadas = r_fuerzaArmada,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathFuerzaArmada = Path.Combine(pathFuentes, "fuerzaArmadaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathFuerzaArmada);
                    datos = JsonConvert.DeserializeObject<FuerzasArmadasApiViewModel>(archivo);

                    try
                    {
                        var filePath = Path.GetTempFileName();
                        ViewBag.RutaArchivo = filePath;
                        System.IO.File.WriteAllBytes(filePath, datos.FuerzasArmadas.Reporte);
                        datos.FuerzasArmadas.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        ViewBag.RutaArchivo = string.Empty;
                        _logger.LogError($"Error al registrar certificado de Fuerzas Armadas {modelo.Identificacion}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Fuente de Fuerzas Armadas procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Fuerzas Armadas. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.FuerzaArmada,
                            Generado = datos.FuerzasArmadas != null,
                            Data = datos.FuerzasArmadas != null ? JsonConvert.SerializeObject(datos.FuerzasArmadas) : null,
                            Cache = cacheFuerzaArmada,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Fuerzas Armadas procesado correctamente");

                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        public async Task<DeNoBajaApiViewModel> ObtenerReporteDeNoBaja(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia r_denobaja = null;
                var cacheDeNoBaja = false;
                Historial historialTemp = null;
                var datos = new DeNoBajaApiViewModel();
                ResultadoResultadoNoPolicia resultadoDeNoBaja = null;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente De No Baja identificación: {modelo.Identificacion}");
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (historialTemp != null && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)) && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                            {
                                resultadoDeNoBaja = await _antecedentes.GetRespuestaAsyncDeNoBajaV2(historialTemp.IdentificacionSecundaria);
                                r_denobaja = resultadoDeNoBaja?.ResultadoNoPolicia;
                            }
                            else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    resultadoDeNoBaja = await _antecedentes.GetRespuestaAsyncDeNoBajaV2(modelo.Identificacion);
                                    r_denobaja = resultadoDeNoBaja?.ResultadoNoPolicia;
                                }
                            }
                        }
                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            resultadoDeNoBaja = await _antecedentes.GetRespuestaAsyncDeNoBajaV2(modelo.Identificacion);
                            r_denobaja = resultadoDeNoBaja?.ResultadoNoPolicia;
                        }

                        if (r_denobaja != null && r_denobaja.Resultado != null && r_denobaja.Resultado.Reporte.Length > 0)
                            r_denobaja.Resultado.Reporte = null;

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente De No Baja con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (r_denobaja == null)
                    {
                        var datosDeNoBaja = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.DeNoBaja && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDeNoBaja != null)
                        {
                            cacheDeNoBaja = true;
                            r_denobaja = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia>(datosDeNoBaja);
                        }
                    }

                    datos = new DeNoBajaApiViewModel()
                    {
                        DeNoBaja = r_denobaja,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathDeNoBaja = Path.Combine(pathFuentes, "deNoBajaDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathDeNoBaja);
                    datos = JsonConvert.DeserializeObject<DeNoBajaApiViewModel>(archivo);

                    try
                    {
                        var filePath = Path.GetTempFileName();
                        ViewBag.RutaArchivo = filePath;
                        System.IO.File.WriteAllBytes(filePath, datos.DeNoBaja.Resultado.Reporte);
                        datos.DeNoBaja.Resultado.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        ViewBag.RutaArchivo = string.Empty;
                        _logger.LogError($"Error al registrar certificado de No Baja {modelo.Identificacion}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Fuente de De No Baja procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente De No Baja. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.DeNoBaja,
                            Generado = datos.DeNoBaja != null,
                            Data = datos.DeNoBaja != null ? JsonConvert.SerializeObject(datos.DeNoBaja) : null,
                            Cache = cacheDeNoBaja,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = resultadoDeNoBaja != null ? resultadoDeNoBaja.Error : null,
                            FuenteActiva = resultadoDeNoBaja != null ? resultadoDeNoBaja.FuenteActiva : null
                        });
                        _logger.LogInformation("Historial de la Fuente De No Baja procesado correctamente");

                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<SriApiViewModel> ObtenerReporteSRIBasico(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                var cacheSri = false;
                var cacheContactosEmpresa = false;
                var cedulaEntidades = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                            cedulaEntidades = true;
                        }

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            r_sri = await _sri.GetContribuyenteBasico360Sri(modelo.Identificacion);
                            if (r_sri != null)
                                cacheSri = true;

                            contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                            {
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);
                                r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);
                            }
                        }

                        if (r_sri == null)
                        {
                            var datosDetalleSri = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleSri != null)
                            {
                                cacheSri = true;
                                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
                            }
                        }

                        #region Normalizacion Contactos
                        var cedulaAux = string.Empty;
                        if (cedulaEntidades)
                            cedulaAux = modelo.Identificacion.Substring(0, 10);
                        else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion) && r_sri != null && !string.IsNullOrEmpty(r_sri.RepresentanteLegal))
                            cedulaAux = r_sri.RepresentanteLegal;
                        else
                            cedulaAux = modelo.Identificacion;

                        if (r_sri != null && !string.IsNullOrEmpty(cedulaAux))
                        {
                            var contactosCedula = await _garancheck.GetContactoAsync(cedulaAux);
                            if (contactosEmpresa != null)
                            {
                                if (contactosCedula != null)
                                {
                                    if (contactosCedula.Direcciones != null && contactosCedula.Direcciones.Any())
                                    {
                                        if (contactosEmpresa.Direcciones != null && contactosEmpresa.Direcciones.Any())
                                        {
                                            contactosEmpresa.Direcciones.AddRange(contactosCedula.Direcciones);
                                            contactosEmpresa.Direcciones = contactosEmpresa.Direcciones.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Direcciones = contactosCedula.Direcciones.ToList();
                                    }

                                    if (contactosCedula.Telefonos != null && contactosCedula.Telefonos.Any())
                                    {
                                        if (contactosEmpresa.Telefonos != null && contactosEmpresa.Telefonos.Any())
                                        {
                                            contactosEmpresa.Telefonos.AddRange(contactosCedula.Telefonos);
                                            contactosEmpresa.Telefonos = contactosEmpresa.Telefonos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Telefonos = contactosCedula.Telefonos.ToList();
                                    }

                                    if (contactosCedula.Correos != null && contactosCedula.Correos.Any())
                                    {
                                        if (contactosEmpresa.Correos != null && contactosEmpresa.Correos.Any())
                                        {
                                            contactosEmpresa.Correos.AddRange(contactosCedula.Correos);
                                            contactosEmpresa.Correos = contactosEmpresa.Correos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Correos = contactosCedula.Correos.ToList();
                                    }
                                }
                            }
                            else if (contactosCedula != null)
                                contactosEmpresa = contactosCedula;
                        }
                        #endregion Normalizacion Contactos
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));

                }
                var datos = new SriApiViewModel()
                {
                    Empresa = r_sri,
                    Contactos = contactosEmpresa
                };
                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
                                    historial.IdentificacionSecundaria = r_sri.RUC;
                            }
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante?.Trim()) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                                historial.NombresPersona = r_sri.AgenteRepresentante.Trim().ToUpper();

                            if (r_sri != null && (historial.TipoIdentificacion == Dominio.Constantes.General.Cedula || historial.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historial.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                            {
                                if (ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal) && ValidacionViewModel.ValidarRuc(historial.Identificacion))
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
                                else
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Trim();
                            }
                            else
                            {
                                if (r_sri != null && r_sri.PersonaSociedad == "SCD" && historial.TipoIdentificacion == Dominio.Constantes.General.Cedula && ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal))
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
                                else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
                                    historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
                            }
                            await _historiales.UpdateAsync(historial);
                            if (historialConsolidado != null)
                            {
                                historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                                historialConsolidado.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidado);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Sri,
                            Generado = datos.Empresa != null,
                            Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null,
                            Cache = cacheSri,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheContactosEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<SriApiViewModel> ObtenerReporteSRIHistorico(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                var cacheSri = false;
                var cedulaEntidades = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                            cedulaEntidades = true;
                        }

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                            {
                                var directorioEmpresa = _balances.GetDirectorioCompanias(modelo.Identificacion);
                                if (directorioEmpresa != null)
                                {
                                    var direccion = new[] { directorioEmpresa.Provincia, directorioEmpresa.Canton, directorioEmpresa.Ciudad, directorioEmpresa.Calle, directorioEmpresa.Numero, directorioEmpresa.Interseccion };
                                    r_sri = new Contribuyente()
                                    {
                                        RUC = directorioEmpresa.Ruc,
                                        RazonSocial = directorioEmpresa.Nombre,
                                        FechaInicio = directorioEmpresa.FechaConstitucion.HasValue ? directorioEmpresa.FechaConstitucion.Value : DateTime.Now,
                                        AgenteRepresentante = directorioEmpresa.Representante,
                                        Direccion = string.Join(" / ", direccion.Where(m => !string.IsNullOrEmpty(m))),
                                        Estado = directorioEmpresa.SituacionLegal == "ACTIVA" ? "ACT" : "SDE",
                                        EstadoContribuyente = directorioEmpresa.SituacionLegal == "ACTIVA" ? "ACTIVO" : "SUSPENDIDO",
                                        Clase = "OTROS",
                                        Tipo = "SOCIEDAD",
                                        PersonaSociedad = "SCD",
                                        Subtipo = "BAJO CONTROL DE LA SUPERINTENDENCIA DE COMPAÑIAS"
                                    };
                                }
                                else
                                    r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);
                            }
                            else
                                r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);

                            if (r_sri != null)
                                cacheSri = true;

                            contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);
                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                                r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);
                        }

                        if (r_sri == null)
                        {
                            var datosDetalleSri = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleSri != null)
                            {
                                cacheSri = true;
                                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
                            }
                        }

                        #region Normalizacion Contactos
                        var cedulaAux = string.Empty;
                        if (cedulaEntidades)
                            cedulaAux = modelo.Identificacion.Substring(0, 10);
                        else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion) && r_sri != null && !string.IsNullOrEmpty(r_sri.RepresentanteLegal))
                            cedulaAux = r_sri.RepresentanteLegal;
                        else
                            cedulaAux = modelo.Identificacion.Substring(0, 10);

                        if (r_sri != null && !string.IsNullOrEmpty(cedulaAux))
                        {
                            var contactosCedula = await _garancheck.GetContactoAsync(cedulaAux);
                            if (contactosEmpresa != null)
                            {
                                if (contactosCedula != null)
                                {
                                    if (contactosCedula.Direcciones != null && contactosCedula.Direcciones.Any())
                                    {
                                        if (contactosEmpresa.Direcciones != null && contactosEmpresa.Direcciones.Any())
                                        {
                                            contactosEmpresa.Direcciones.AddRange(contactosCedula.Direcciones);
                                            contactosEmpresa.Direcciones = contactosEmpresa.Direcciones.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Direcciones = contactosCedula.Direcciones.ToList();
                                    }

                                    if (contactosCedula.Telefonos != null && contactosCedula.Telefonos.Any())
                                    {
                                        if (contactosEmpresa.Telefonos != null && contactosEmpresa.Telefonos.Any())
                                        {
                                            contactosEmpresa.Telefonos.AddRange(contactosCedula.Telefonos);
                                            contactosEmpresa.Telefonos = contactosEmpresa.Telefonos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Telefonos = contactosCedula.Telefonos.ToList();
                                    }

                                    if (contactosCedula.Correos != null && contactosCedula.Correos.Any())
                                    {
                                        if (contactosEmpresa.Correos != null && contactosEmpresa.Correos.Any())
                                        {
                                            contactosEmpresa.Correos.AddRange(contactosCedula.Correos);
                                            contactosEmpresa.Correos = contactosEmpresa.Correos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Correos = contactosCedula.Correos.ToList();
                                    }
                                }
                            }
                            else if (contactosCedula != null)
                                contactosEmpresa = contactosCedula;
                        }
                        #endregion Normalizacion Contactos
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));

                }
                var datos = new SriApiViewModel()
                {
                    Empresa = r_sri,
                    Contactos = contactosEmpresa
                };
                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
                                    historial.IdentificacionSecundaria = r_sri.RUC;
                            }
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante?.Trim()) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                                historial.NombresPersona = r_sri.AgenteRepresentante.Trim().ToUpper();

                            if (r_sri != null && (historial.TipoIdentificacion == Dominio.Constantes.General.Cedula || historial.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historial.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                            {
                                if (ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal) && ValidacionViewModel.ValidarRuc(historial.Identificacion))
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
                                else
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Trim();
                            }
                            else
                            {
                                if (r_sri != null && r_sri.PersonaSociedad == "SCD" && historial.TipoIdentificacion == Dominio.Constantes.General.Cedula && ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal))
                                    historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
                                else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
                                    historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
                            }
                            await _historiales.UpdateAsync(historial);
                            if (historialConsolidado != null)
                            {
                                historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                                historialConsolidado.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidado);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Sri,
                            Generado = datos.Empresa != null,
                            Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null,
                            Cache = cacheSri,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheSri,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<CivilApiMetodoViewModel> ObtenerReporteCivilBasico(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.Garancheck.Modelos.RegistroCivil registroCivil = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactos = null;
                var datos = new CivilApiMetodoViewModel();
                Historial historialTemp = null;
                var cacheCivil = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Civil identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            registroCivil = await _garancheck.GetRegistroCivilLineaBasicoAsync(modelo.Identificacion);
                            contactos = await _garancheck.GetContactoAsync(modelo.Identificacion);
                        }
                        else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                            if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                            {
                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                    {
                                        registroCivil = await _garancheck.GetRegistroCivilLineaBasicoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                        contactos = await _garancheck.GetContactoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                    }
                                }
                            }
                            else
                            {
                                var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                {
                                    registroCivil = await _garancheck.GetRegistroCivilLineaBasicoAsync(cedulaTemp);
                                    contactos = await _garancheck.GetContactoAsync(cedulaTemp);
                                }
                            }
                        }
                        if (registroCivil != null)
                            cacheCivil = true;

                        if (registroCivil == null)
                        {
                            var datosDetalleRegistroCivil = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleRegistroCivil != null)
                            {
                                cacheCivil = true;
                                registroCivil = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(datosDetalleRegistroCivil);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Civil con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                    datos = new CivilApiMetodoViewModel()
                    {
                        RegistroCivil = registroCivil,
                        Contactos = contactos
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                    datos = JsonConvert.DeserializeObject<CivilApiMetodoViewModel>(System.IO.File.ReadAllText(pathCivil));
                }
                _logger.LogInformation("Fuente de Civil procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Civil. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.Cedula?.Trim()) && !string.IsNullOrEmpty(registroCivil.Nombre?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = registroCivil.Nombre?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = registroCivil.Cedula?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }

                            if (registroCivil != null && registroCivil.FechaCedulacion != default && string.IsNullOrEmpty(historial.FechaExpedicionCedula?.Trim()))
                            {
                                historial.FechaExpedicionCedula = registroCivil.FechaCedulacion.ToString("dd/MM/yyyy");
                                await _historiales.UpdateAsync(historial);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.RegistroCivil,
                            Generado = datos.RegistroCivil != null,
                            Data = datos.RegistroCivil != null ? JsonConvert.SerializeObject(datos.RegistroCivil) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Contactos,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Civil procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<CivilApiMetodoViewModel> ObtenerReporteCivilHistorico(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.Garancheck.Modelos.Persona r_garancheck = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactos = null;
                var datos = new CivilApiMetodoViewModel();
                Historial historialTemp = null;
                var cacheCivil = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Civil identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            r_garancheck = await _garancheck.GetCivilHistoricoBasicoAsync(modelo.Identificacion);
                            contactos = await _garancheck.GetContactoAsync(modelo.Identificacion);
                        }
                        else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                            if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                            {
                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                    {
                                        r_garancheck = await _garancheck.GetCivilHistoricoBasicoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                        contactos = await _garancheck.GetContactoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                    }
                                }
                            }
                            else
                            {
                                var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                {
                                    r_garancheck = await _garancheck.GetCivilHistoricoBasicoAsync(cedulaTemp);
                                    contactos = await _garancheck.GetContactoAsync(cedulaTemp);
                                }
                            }
                        }
                        if (r_garancheck != null)
                            cacheCivil = true;

                        if (r_garancheck == null)
                        {
                            var datosDetalleGarancheck = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleGarancheck != null)
                            {
                                cacheCivil = true;
                                r_garancheck = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(datosDetalleGarancheck);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Civil con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                    datos = new CivilApiMetodoViewModel()
                    {
                        General = r_garancheck,
                        Contactos = contactos
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                    datos = JsonConvert.DeserializeObject<CivilApiMetodoViewModel>(System.IO.File.ReadAllText(pathCivil));
                }
                _logger.LogInformation("Fuente de Civil procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Civil. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_garancheck != null && !string.IsNullOrEmpty(r_garancheck.Identificacion?.Trim()) && !string.IsNullOrEmpty(r_garancheck.Nombres?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = r_garancheck.Nombres?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = r_garancheck.Identificacion?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Ciudadano,
                            Generado = datos.General != null,
                            Data = datos.General != null ? JsonConvert.SerializeObject(datos.General) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Contactos,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Civil procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }

        #region Evaluacion
        private async Task<List<CalificacionApiMetodoViewModel>> ObtenerCalificacionMicrocredito(ApiViewModel_1790325083001 modelo)
        {
            try
            {
                _logger.LogInformation($"Procesando informacion Cedula del Historial {modelo.IdHistorial}");
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                #region Inicialización
                var datosPersona = new CalificacionApiMetodoViewModel();
                var detalleCalificacion = new List<DetalleCalificacionApiMetodoViewModel>();
                var politica = new Politica();
                var datosHistorial = new SRIViewModel();
                var datosSocietario = new BalancesViewModel();
                var detalleHistorial = new DatosJsonViewModel();
                var calificacionAnterior = new Calificacion();
                var detalleCalificacionAnterior = new List<DetalleCalificacion>();
                var observaciones = new List<string>();
                int minimo;
                var datosPersonaLista = new List<CalificacionApiMetodoViewModel>();
                var culture = System.Globalization.CultureInfo.CurrentCulture;

                //Planes Activos
                var resultadoPermiso = Dominio.Tipos.EstadosPlanesEvaluaciones.Activo; ;
                var dataPlanEvaluacion = await _planesEvaluaciones.FirstOrDefaultAsync(s => s, s => s.IdEmpresa == modelo.IdEmpresa && s.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo);
                if (dataPlanEvaluacion == null)
                    throw new Exception("No se encontró un plan de evaluación Activo.");

                var dataUsuario = await _accesos.AnyAsync(s => s.IdUsuario == modelo.IdUsuario && s.Estado == Dominio.Tipos.EstadosAccesos.Activo && s.Acceso == Dominio.Tipos.TiposAccesos.Evaluacion);
                if (!dataUsuario)
                    throw new Exception("El usuario no tiene permisos para la evaluación.");

                var fechaActual = DateTime.Now;
                var primerDiadelMes = new DateTime(fechaActual.Year, fechaActual.Month, 1);
                var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);
                var numeroHistorialEvaluacion = await _historiales.CountAsync(s => s.Id != modelo.IdHistorial && s.IdPlanEvaluacion == dataPlanEvaluacion.Id && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date);

                if (dataPlanEvaluacion.BloquearConsultas)
                    resultadoPermiso = dataPlanEvaluacion.NumeroConsultas > numeroHistorialEvaluacion ? Dominio.Tipos.EstadosPlanesEvaluaciones.Activo : Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo;
                if (resultadoPermiso != Dominio.Tipos.EstadosPlanesEvaluaciones.Activo)
                    throw new Exception("No es posible realizar esta consulta ya que excedió el límite de consultas del plan Evaluación.");

                //Politicas
                var politicasActuales = await _politicas.ReadAsync(m => m, m => m.IdEmpresa == modelo.IdEmpresa && m.Estado);
                if (!politicasActuales.Any())
                    throw new Exception("La empresa actual no tiene registrada políticas.");

                var tipoIdentificacion = await _historiales.FirstOrDefaultAsync(m => m.TipoIdentificacion, m => m.Id == modelo.IdHistorial);
                #endregion Inicialización

                #region Políticas Evaluaciones
                bool aprobacionAdicional = false;

                _logger.LogInformation("Obteniendo políticas para procesamiento...");
                //General

                var quiceTrabajadoresMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.quiceTrabajadoresMicrocredito);
                var antecedentesPenalesMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.antecedentesPenalesMicrocredito);
                var antiguedadRucMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.antiguedadRucMicrocredito);
                var anioConstitucionMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.anioConstitucionMicrocredito);
                var capitalSuscritoMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.capitalSuscritoMicrocredito);
                var contactableDireccionesMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.contactableDireccionesMicrocredito);
                var contactableEmailsMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.contactableEmailsMicrocredito);
                var contactableTelefonosMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.contactableTelefonosMicrocredito);
                var cuentasPagarProveedoresTercerosMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.cuentasPagarProveedoresTercerosMicrocredito);
                var deudaFirmeMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.deudaFirmeMicrocredito);
                var edadMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.edadMicrocredito);
                var efectivoCajaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.efectivoCajaMicrocredito);
                var estadoJuridicoMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.estadoJuridicoMicrocredito);
                var estadoTributarioMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.estadoTributarioMicrocredito);
                var gastosOperacionalesMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.gastosOperacionalesMicrocredito);
                var iessAfiliacionMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.iessAfiliacionMicrocredito);
                var iessMoraPatronalMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.iessMoraPatronalMicrocredito);
                var impuestoRentaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.impuestoRentaMicrocredito);
                var liquidezCorrienteMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.liquidezCorrienteMicrocredito);
                var noticiasDelitoMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.noticiasDelitoMicrocredito);
                var noticiasDelitoEmpresaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.noticiasDelitoEmpresaMicrocredito);
                var pagoMultasPendientesMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.pagoMultasPendientesMicrocredito);
                var pagoPendientePensionAlimenticiaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.pagoPendientePensionAlimenticiaMicrocredito);
                var patrimonioNetoMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.patrimonioNetoMicrocredito);
                var periodoMedioCobranzaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.periodoMedioCobranzaMicrocredito);
                var permisoFacturacionMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.permisoFacturacionMicrocredito);
                var sercopMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.sercopMicrocredito);
                var superintendenciaBancosCedulaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.superintendenciaBancosCedulaMicrocredito);
                var superintendenciaBancosEmpresaMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.superintendenciaBancosEmpresaMicrocredito);
                var superintendenciaBancosRucNaturalMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.superintendenciaBancosRucNaturalMicrocredito);
                var titulosSenescytMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.titulosSenescytMicrocredito);
                var totalActivosMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.totalActivosMicrocredito);
                var totalIngresosMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.totalIngresosMicrocredito);
                var totalPasivosMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.totalPasivosMicrocredito);
                var ultimoAnioBalanceMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.ultimoAnioBalanceMicrocredito);
                var utilidadEjercicioMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.utilidadEjercicioMicrocredito);
                var vehiculosMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.vehiculosMicrocredito);
                var propiedadesMicrocredito = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.propiedadesMicrocredito);
                #endregion Políticas Evaluaciones

                #region Sri
                try
                {
                    _logger.LogInformation("Procesando políticas SRI...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);

                    if (detalleHistorial != null)
                    {
                        datosHistorial.Sri = JsonConvert.DeserializeObject<Contribuyente>(detalleHistorial.Datos);

                        datosPersona.FechaInicio = datosHistorial.Sri.FechaInicio;
                        var diferenciaAnios = DateTime.Today.Year - datosPersona.FechaInicio.Date.Year;
                        if (datosPersona.FechaInicio.Date > DateTime.Today.AddYears(-diferenciaAnios))
                            diferenciaAnios--;

                        if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                            minimo = 2;
                        else
                            minimo = 1;

                        var estadoRuc = Dominio.Constantes.Politicas.RucActivo;
                        var resultadoComparacion = diferenciaAnios >= minimo && estadoRuc == datosHistorial.Sri.Estado;
                        if (!string.IsNullOrEmpty(datosHistorial.Sri.EstadoContribuyente) && datosHistorial.Sri.EstadoContribuyente == "SUSPENDIDO" && datosHistorial.Sri.EstadoTributario != null && !string.IsNullOrEmpty(datosHistorial.Sri.EstadoTributario.Estado) && datosHistorial.Sri.EstadoTributario.Estado != "OBLIGACIONES TRIBUTARIAS PENDIENTES")
                        {
                            resultadoComparacion = diferenciaAnios >= minimo;
                            if (antiguedadRucMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = antiguedadRucMicrocredito.Id,
                                    Politica = antiguedadRucMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAniosRucRimpeJuridico : Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAniosRucRimpeNatural, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.FechaAnios, datosPersona.FechaInicio.ToString("yyyy/MM/dd"), diferenciaAnios),
                                    Valor = diferenciaAnios.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (antiguedadRucMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(antiguedadRucMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                        else
                        {
                            if (antiguedadRucMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = antiguedadRucMicrocredito.Id,
                                    Politica = antiguedadRucMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAniosRucRimpeJuridico : Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAniosRucRimpeNatural, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.FechaAnios, datosPersona.FechaInicio.ToString("yyyy/MM/dd"), diferenciaAnios),
                                    Valor = diferenciaAnios.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (antiguedadRucMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(antiguedadRucMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }

                        minimo = 40;
                        if (datosHistorial.Sri.Deudas != null && datosHistorial.Sri.Deudas.Any() && datosHistorial.Sri.Deudas.ContainsKey("Firmes"))
                        {
                            resultadoComparacion = datosHistorial.Sri.Deudas["Firmes"].Valor.Value < minimo;
                            if (deudaFirmeMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = deudaFirmeMicrocredito.Id,
                                    Politica = deudaFirmeMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", datosHistorial.Sri.Deudas["Firmes"].Valor.Value),
                                    Valor = datosHistorial.Sri.Deudas["Firmes"].Valor.Value.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (deudaFirmeMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(deudaFirmeMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                        else
                        {
                            if (deudaFirmeMicrocredito != null)
                            {
                                resultadoComparacion = true;
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = deudaFirmeMicrocredito.Id,
                                    Politica = deudaFirmeMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", "0.00"),
                                    Valor = "0.00",
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (deudaFirmeMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(deudaFirmeMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }

                        if (datosHistorial.Sri.EstadoTributario != null && !string.IsNullOrEmpty(datosHistorial.Sri.EstadoTributario.Estado))
                        {
                            resultadoComparacion = datosHistorial.Sri.EstadoTributario.Estado != "OBLIGACIONES TRIBUTARIAS PENDIENTES";
                            if (estadoTributarioMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = estadoTributarioMicrocredito.Id,
                                    Politica = estadoTributarioMicrocredito.Nombre,
                                    ReferenciaMinima = Dominio.Constantes.TextoReferencia.EstadoTributario,
                                    ValorResultado = datosHistorial.Sri.EstadoTributario.Estado,
                                    Valor = datosHistorial.Sri.EstadoTributario.Estado,
                                    Parametro = Dominio.Constantes.TextoReferencia.EstadoTributario,
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (estadoTributarioMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(estadoTributarioMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }

                        minimo = 0;
                        if (datosHistorial.Sri.Anexos != null && datosHistorial.Sri.Anexos.Any())
                        {
                            var datosRenta = datosHistorial.Sri.Anexos.OrderByDescending(x => x.Periodo).Select(x => new { x.Periodo, x.Causado }).ToList().Take(2);
                            int periodo = 0;
                            double causado = 0;
                            var impuestoMensaje = string.Empty;
                            if (datosRenta != null && datosRenta.Any())
                            {
                                if (datosRenta.Count() == 2 && datosRenta.First().Periodo == DateTime.Now.AddYears(-1).Year)
                                {
                                    periodo = datosRenta.First().Periodo;
                                    causado = (double)datosRenta.First().Causado;
                                }
                                else if (datosRenta.Count() == 2 && datosRenta.Last().Periodo == DateTime.Now.AddYears(-1).Year)
                                {
                                    periodo = datosRenta.Last().Periodo;
                                    causado = (double)datosRenta.Last().Causado;
                                }
                                else if (datosRenta.Count() == 1 && datosRenta.First().Periodo == DateTime.Now.AddYears(-1).Year)
                                {
                                    periodo = datosRenta.First().Periodo;
                                    causado = (double)datosRenta.First().Causado;
                                }
                                else
                                {
                                    periodo = DateTime.Now.AddYears(-1).Year;
                                    impuestoMensaje = $"No presenta valor en el año {periodo}";
                                }
                            }
                            else
                            {
                                periodo = DateTime.Now.AddYears(-1).Year;
                                impuestoMensaje = $"No presenta valor en el año {periodo}";
                            }

                            if (string.IsNullOrEmpty(impuestoMensaje))
                            {
                                resultadoComparacion = causado > minimo;
                                if (impuestoRentaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = impuestoRentaMicrocredito.Id,
                                        Politica = $"{impuestoRentaMicrocredito.Nombre} {periodo}",
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", causado >= 0 ? causado.ToString("N", culture) : "0.00"),
                                        Valor = causado >= 0 ? causado.ToString("N", culture) : "0.00",
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        Observacion = $"Periodo {periodo}",
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (impuestoRentaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(impuestoRentaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = false;
                                if (impuestoRentaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = impuestoRentaMicrocredito.Id,
                                        Politica = $"{impuestoRentaMicrocredito.Nombre} {periodo}",
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                        ValorResultado = impuestoMensaje,
                                        Valor = impuestoMensaje,
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        Observacion = $"Periodo {periodo}",
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (impuestoRentaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(impuestoRentaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }

                        if ((!string.IsNullOrEmpty(datosHistorial.Sri.EstadoContribuyente) && datosHistorial.Sri.EstadoContribuyente != "SUSPENDIDO") || (datosHistorial.Sri.EstadoTributario != null && !string.IsNullOrEmpty(datosHistorial.Sri.EstadoTributario.Estado) && datosHistorial.Sri.EstadoTributario.Estado == "OBLIGACIONES TRIBUTARIAS PENDIENTES"))
                        {
                            if (datosHistorial.Sri.PermisoFacturacion != null && !string.IsNullOrEmpty(datosHistorial.Sri.PermisoFacturacion.Vigencia))
                            {
                                var valorVigencia = Regex.Matches(datosHistorial.Sri.PermisoFacturacion.Vigencia, @"[0-9]+");
                                if (valorVigencia != null && int.TryParse(valorVigencia[0].ToString(), out _))
                                {
                                    var valorFacturacion = int.Parse(valorVigencia[0].ToString());
                                    minimo = 12;
                                    if (valorFacturacion <= 3)
                                    {
                                        resultadoComparacion = valorFacturacion > minimo;
                                        if (permisoFacturacionMicrocredito != null)
                                        {
                                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                            {
                                                IdPolitica = permisoFacturacionMicrocredito.Id,
                                                Politica = permisoFacturacionMicrocredito.Nombre,
                                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMeses, minimo),
                                                ValorResultado = $"{valorVigencia[0]} meses",
                                                Valor = valorVigencia[0].ToString(),
                                                Parametro = minimo.ToString(),
                                                ResultadoPolitica = resultadoComparacion,
                                                FechaCreacion = DateTime.Now
                                            });
                                            if (!resultadoComparacion)
                                            {
                                                observaciones.Add(permisoFacturacionMicrocredito.Nombre);
                                                aprobacionAdicional = true;
                                            }
                                        }
                                    }
                                    else if (valorFacturacion > 3 && valorFacturacion < 12)
                                    {
                                        resultadoComparacion = valorFacturacion > minimo;
                                        if (permisoFacturacionMicrocredito != null)
                                        {
                                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                            {
                                                IdPolitica = permisoFacturacionMicrocredito.Id,
                                                Politica = permisoFacturacionMicrocredito.Nombre,
                                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMeses, minimo),
                                                ValorResultado = $"{valorVigencia[0]} meses",
                                                Valor = valorVigencia[0].ToString(),
                                                Parametro = minimo.ToString(),
                                                ResultadoPolitica = resultadoComparacion,
                                                FechaCreacion = DateTime.Now
                                            });
                                            if (permisoFacturacionMicrocredito.Excepcional && !resultadoComparacion)
                                            {
                                                observaciones.Add(permisoFacturacionMicrocredito.Nombre);
                                                aprobacionAdicional = true;
                                            }
                                        }
                                    }
                                    else if (valorFacturacion >= 12)
                                    {
                                        resultadoComparacion = valorFacturacion >= minimo;
                                        if (permisoFacturacionMicrocredito != null)
                                        {
                                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                            {
                                                IdPolitica = permisoFacturacionMicrocredito.Id,
                                                Politica = permisoFacturacionMicrocredito.Nombre,
                                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMeses, minimo),
                                                ValorResultado = $"{valorVigencia[0]} meses",
                                                Valor = valorVigencia[0].ToString(),
                                                Parametro = minimo.ToString(),
                                                ResultadoPolitica = resultadoComparacion,
                                                FechaCreacion = DateTime.Now
                                            });
                                            if (permisoFacturacionMicrocredito.Excepcional && !resultadoComparacion)
                                            {
                                                observaciones.Add(permisoFacturacionMicrocredito.Nombre);
                                                aprobacionAdicional = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SRI. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Sri

                #region Civil
                try
                {
                    var direccionesAdicionales = new List<string>();
                    Externos.Logica.Garancheck.Modelos.Persona personaTemp = null;
                    _logger.LogInformation("Procesando políticas Civil...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);
                    var resultadoComparacion = false;
                    var edad = -1;
                    if (detalleHistorial != null)
                    {
                        //Civil En Línea
                        var persona = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(detalleHistorial.Datos);
                        if (persona != null && persona.FechaNacimiento != default)
                        {
                            edad = DateTime.Today.Year - persona.FechaNacimiento.Year;
                            if (persona.FechaNacimiento.Date > DateTime.Today.AddYears(-edad))
                                edad--;
                        }

                        if (persona != null)
                        {
                            var direccionTempRegCivil = string.Join("/", new[] { persona.LugarDomicilio?.Trim(), persona.CalleDomicilio?.Trim(), persona.NumeracionDomicilio?.Trim() }.Where(m => !string.IsNullOrEmpty(m)).ToArray());
                            if (!string.IsNullOrEmpty(direccionTempRegCivil))
                                direccionesAdicionales.Add(direccionTempRegCivil);
                        }
                    }
                    else
                    {
                        //Civil Histórico
                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);

                        if (detalleHistorial != null)
                        {
                            var persona = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(detalleHistorial.Datos);
                            if (!string.IsNullOrEmpty(persona.FechaNacimiento.Value.ToString()))
                            {
                                edad = DateTime.Today.Year - persona.FechaNacimiento.Value.Year;
                                if (persona.FechaNacimiento.Value.Date > DateTime.Today.AddYears(-edad))
                                    edad--;
                            }
                            personaTemp = persona;
                        }
                    }

                    //Edad
                    if (edad >= 0)
                    {
                        if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                            resultadoComparacion = (edad > 25 && edad <= 79);
                        else
                            resultadoComparacion = (edad > 18 && edad <= 79);

                        if (edadMicrocredito != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = edadMicrocredito.Id,
                                Politica = edadMicrocredito.Nombre,
                                ReferenciaMinima = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? "Mayor a 25 años y Menor o igual a 79 años" : "Mayor a 18 años y Menor o igual a 79 años",
                                ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Edad, edad.ToString()),
                                Valor = edad.ToString(),
                                Parametro = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? "[25,79]" : "[18,79]",
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (edadMicrocredito.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(edadMicrocredito.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }

                    //Contactos
                    var detalleHistorialPersonal = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Personales && m.Generado);

                    Externos.Logica.Garancheck.Modelos.Personal personalTemp = null;
                    if (detalleHistorialPersonal != null)
                        personalTemp = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Personal>(detalleHistorialPersonal.Datos);

                    if (personaTemp == null)
                    {
                        var detalleHistorialCiudadano = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);

                        if (detalleHistorialCiudadano != null)
                            personaTemp = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(detalleHistorialCiudadano.Datos);
                    }

                    if (personalTemp != null && !string.IsNullOrEmpty(personalTemp.NombreCalle?.Trim()) && !string.IsNullOrEmpty(personalTemp.NumeroCasa?.Trim()))
                    {
                        if (personaTemp != null && !string.IsNullOrEmpty(personaTemp.Provincia?.Trim()) && !string.IsNullOrEmpty(personaTemp.Canton?.Trim()) && !string.IsNullOrEmpty(personaTemp.Parroquia?.Trim()))
                            direccionesAdicionales.Add($"{personaTemp.Provincia} / {personaTemp.Canton} / {personaTemp.Parroquia} / {personalTemp.NombreCalle} {personalTemp.NumeroCasa}");
                        else
                            direccionesAdicionales.Add($"{personalTemp.NombreCalle} {personalTemp.NumeroCasa}");
                    }

                    var contactosPersona = new Contacto();
                    var direccionesContacto = 0;
                    var telefonosContacto = 0;
                    var emailsContacto = 0;
                    var detalleContactos = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Contactos && m.Generado);

                    if (detalleContactos != null)
                    {
                        contactosPersona = JsonConvert.DeserializeObject<Contacto>(detalleContactos.Datos);
                        direccionesContacto += contactosPersona.Direcciones.Count;
                        telefonosContacto += contactosPersona.Telefonos.Count;
                        emailsContacto += contactosPersona.Correos.Count;
                    }

                    var detalleContactosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa && m.Generado);

                    if (detalleContactosEmpresa != null)
                    {
                        contactosPersona = JsonConvert.DeserializeObject<Contacto>(detalleContactosEmpresa.Datos);
                        direccionesContacto += contactosPersona.Direcciones.Count;
                        telefonosContacto += contactosPersona.Telefonos.Count;
                        emailsContacto += contactosPersona.Correos.Count;
                    }

                    var detalleContactosIess = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess && m.Generado);

                    if (detalleContactosIess != null)
                    {
                        contactosPersona = JsonConvert.DeserializeObject<Contacto>(detalleContactosIess.Datos);
                        direccionesContacto += contactosPersona.Direcciones.Count;
                        telefonosContacto += contactosPersona.Telefonos.Count;
                        emailsContacto += contactosPersona.Correos.Count;
                    }

                    if (direccionesAdicionales.Any())
                        direccionesContacto += direccionesAdicionales.Count;

                    minimo = 2;
                    resultadoComparacion = direccionesContacto >= minimo;

                    if (contactableDireccionesMicrocredito != null)
                    {
                        detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                        {
                            IdPolitica = contactableDireccionesMicrocredito.Id,
                            Politica = contactableDireccionesMicrocredito.Nombre,
                            ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgual, minimo),
                            ValorResultado = direccionesContacto.ToString(),
                            Valor = direccionesContacto.ToString(),
                            Parametro = minimo.ToString(),
                            ResultadoPolitica = resultadoComparacion,
                            FechaCreacion = DateTime.Now
                        });
                        if (contactableDireccionesMicrocredito.Excepcional && !resultadoComparacion)
                        {
                            observaciones.Add(contactableDireccionesMicrocredito.Nombre);
                            aprobacionAdicional = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                        minimo = 2;
                    else
                        minimo = 1;

                    resultadoComparacion = telefonosContacto >= minimo;

                    if (contactableTelefonosMicrocredito != null)
                    {
                        detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                        {
                            IdPolitica = contactableTelefonosMicrocredito.Id,
                            Politica = contactableTelefonosMicrocredito.Nombre,
                            ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgual, minimo),
                            ValorResultado = telefonosContacto.ToString(),
                            Valor = telefonosContacto.ToString(),
                            Parametro = minimo.ToString(),
                            ResultadoPolitica = resultadoComparacion,
                            FechaCreacion = DateTime.Now
                        });
                        if (contactableTelefonosMicrocredito.Excepcional && !resultadoComparacion)
                        {
                            observaciones.Add(contactableTelefonosMicrocredito.Nombre);
                            aprobacionAdicional = true;
                        }
                    }

                    minimo = 1;
                    resultadoComparacion = emailsContacto >= minimo;

                    if (contactableEmailsMicrocredito != null)
                    {
                        detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                        {
                            IdPolitica = contactableEmailsMicrocredito.Id,
                            Politica = contactableEmailsMicrocredito.Nombre,
                            ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgual, minimo),
                            ValorResultado = emailsContacto.ToString(),
                            Valor = emailsContacto.ToString(),
                            Parametro = minimo.ToString(),
                            ResultadoPolitica = resultadoComparacion,
                            FechaCreacion = DateTime.Now
                        });
                        if (contactableEmailsMicrocredito.Excepcional && !resultadoComparacion)
                        {
                            observaciones.Add(contactableEmailsMicrocredito.Nombre);
                            aprobacionAdicional = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Civil. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Civil

                #region Societario
                try
                {
                    _logger.LogInformation("Procesando políticas SOCIETARIO...");
                    //Societario
                    datosSocietario.HistorialCabecera = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Balance && m.Generado);

                    if (detalleHistorial != null)
                    {
                        datosSocietario.Balance = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.BalanceEmpresa>(detalleHistorial.Datos);
                        datosSocietario.PeriodoBusqueda = datosSocietario.Balance.Periodo;
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Balances && m.Generado);

                    if (detalleHistorial != null)
                    {
                        datosSocietario.Balances = JsonConvert.DeserializeObject<List<BalanceEmpresa>>(detalleHistorial.Datos);
                        datosSocietario.Balance = datosSocietario.Balances.OrderByDescending(m => m.Periodo).FirstOrDefault();
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DirectorioCompanias && m.Generado);

                    if (detalleHistorial != null)
                    {
                        datosSocietario.DirectorioCompania = JsonConvert.DeserializeObject<DirectorioCompania>(detalleHistorial.Datos);
                    }
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);

                    if (detalleHistorial != null)
                    {
                        datosSocietario.Sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(detalleHistorial.Datos);
                    }

                    if (datosSocietario != null)
                    {
                        if (datosSocietario.DirectorioCompania != null)
                        {
                            if (datosSocietario.HistorialCabecera != null && !string.IsNullOrEmpty(datosSocietario.HistorialCabecera.TipoIdentificacion) && datosSocietario.HistorialCabecera.TipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                                minimo = 1;
                            else
                                minimo = 2;

                            var fechaConstitucion = datosSocietario.DirectorioCompania.FechaConstitucion.Value;
                            var diferenciaAnios = DateTime.Today.Year - fechaConstitucion.Date.Year;
                            if (fechaConstitucion.Date > DateTime.Today.AddYears(-diferenciaAnios))
                                diferenciaAnios--;

                            var resultadoComparacion = diferenciaAnios >= minimo;
                            if (anioConstitucionMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = anioConstitucionMicrocredito.Id,
                                    Politica = anioConstitucionMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(minimo == 1 ? Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAnioMicrocredito : Dominio.Constantes.TextoReferencia.MayorIgualAnios, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.FechaAnios, fechaConstitucion.ToString("yyyy/MM/dd"), diferenciaAnios),
                                    Valor = fechaConstitucion.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (anioConstitucionMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(anioConstitucionMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            if (datosSocietario.DirectorioCompania.CapitalSuscrito != null)
                            {
                                minimo = 400;
                                resultadoComparacion = datosSocietario.DirectorioCompania.CapitalSuscrito >= minimo;

                                if (capitalSuscritoMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = capitalSuscritoMicrocredito.Id,
                                        Politica = capitalSuscritoMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMoneda, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", datosSocietario.DirectorioCompania.CapitalSuscrito),
                                        Valor = datosSocietario.DirectorioCompania.CapitalSuscrito.ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (capitalSuscritoMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(capitalSuscritoMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }

                            if (datosSocietario.DirectorioCompania.SituacionLegal != null)
                            {
                                resultadoComparacion = datosSocietario.DirectorioCompania.SituacionLegal == "ACTIVA";

                                if (estadoJuridicoMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = estadoJuridicoMicrocredito.Id,
                                        Politica = estadoJuridicoMicrocredito.Nombre,
                                        ReferenciaMinima = "ACTIVA",
                                        ValorResultado = datosSocietario.DirectorioCompania.SituacionLegal,
                                        Valor = datosSocietario.DirectorioCompania.SituacionLegal,
                                        Parametro = "ACTIVA",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (estadoJuridicoMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(estadoJuridicoMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }

                        if (datosSocietario.Balances != null || datosSocietario.Balance != null)
                        {
                            decimal sumaLiquidezCorriente = 0;
                            sumaLiquidezCorriente = Math.Round(datosSocietario.Balance.Indices.LiquidezCorriente, 2, MidpointRounding.AwayFromZero);

                            minimo = 1;
                            var resultadoComparacion = sumaLiquidezCorriente > minimo;

                            if (liquidezCorrienteMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = liquidezCorrienteMicrocredito.Id,
                                    Politica = liquidezCorrienteMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                    ValorResultado = sumaLiquidezCorriente.ToString(),
                                    Valor = sumaLiquidezCorriente.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (liquidezCorrienteMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(liquidezCorrienteMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaTrabajadores15 = 0;
                            sumaTrabajadores15 = Math.Round(datosSocietario.Balance.Otros.P15x100Trabajadores, 2, MidpointRounding.AwayFromZero);

                            minimo = 1000;
                            resultadoComparacion = sumaTrabajadores15 >= minimo;

                            if (quiceTrabajadoresMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = quiceTrabajadoresMicrocredito.Id,
                                    Politica = quiceTrabajadoresMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MinimoMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaTrabajadores15),
                                    Valor = sumaTrabajadores15.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (quiceTrabajadoresMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(quiceTrabajadoresMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaTotalEfectivoCaja = 0;
                            sumaTotalEfectivoCaja = Math.Round(datosSocietario.Balance.Otros.EfectivoYCaja, 2, MidpointRounding.AwayFromZero);

                            minimo = 10000;
                            resultadoComparacion = sumaTotalEfectivoCaja > minimo;

                            if (efectivoCajaMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = efectivoCajaMicrocredito.Id,
                                    Politica = efectivoCajaMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaTotalEfectivoCaja),
                                    Valor = sumaTotalEfectivoCaja.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (efectivoCajaMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(efectivoCajaMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaCxPProveedoresTerceros = 0;
                            sumaCxPProveedoresTerceros = Math.Round(datosSocietario.Balance.Otros.CxPProveedoresTerceros, 2, MidpointRounding.AwayFromZero);

                            decimal sumaCxCProveedoresTerceros = 0;
                            sumaCxCProveedoresTerceros = Math.Round(datosSocietario.Balance.Otros.CxCComercialesTerceros, 2, MidpointRounding.AwayFromZero);

                            resultadoComparacion = sumaCxPProveedoresTerceros <= sumaCxCProveedoresTerceros;

                            if (cuentasPagarProveedoresTercerosMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = cuentasPagarProveedoresTercerosMicrocredito.Id,
                                    Politica = cuentasPagarProveedoresTercerosMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.CuentasXPagar, sumaCxCProveedoresTerceros),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaCxPProveedoresTerceros),
                                    Valor = sumaCxPProveedoresTerceros.ToString(),
                                    Parametro = sumaCxCProveedoresTerceros.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (cuentasPagarProveedoresTercerosMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(cuentasPagarProveedoresTercerosMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaTotalIngresos = 0;
                            sumaTotalIngresos = Math.Round(datosSocietario.Balance.Otros.TotalIngresos, 2, MidpointRounding.AwayFromZero);

                            minimo = 100000;
                            resultadoComparacion = sumaTotalIngresos > minimo;

                            if (totalIngresosMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = totalIngresosMicrocredito.Id,
                                    Politica = totalIngresosMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaTotalIngresos),
                                    Valor = sumaTotalIngresos.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (totalIngresosMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(totalIngresosMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaGastosOperacionales = 0;
                            sumaGastosOperacionales = Math.Round(datosSocietario.Balance.Otros.GastosOperacionales, 2, MidpointRounding.AwayFromZero);

                            if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                                resultadoComparacion = sumaGastosOperacionales <= (sumaTotalIngresos * 7 / 10);
                            else
                                resultadoComparacion = sumaGastosOperacionales <= (sumaTotalIngresos * 8 / 10);

                            if (gastosOperacionalesMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = gastosOperacionalesMicrocredito.Id,
                                    Politica = gastosOperacionalesMicrocredito.Nombre,
                                    ReferenciaMinima = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? string.Format(Dominio.Constantes.ConstanteCliente1790325083001.GastosOperacionalesSetentaPorciento, sumaTotalIngresos * 7 / 10) : string.Format(Dominio.Constantes.TextoReferencia.GastosOperacionales, sumaTotalIngresos * 8 / 10),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaGastosOperacionales),
                                    Valor = sumaGastosOperacionales.ToString(),
                                    Parametro = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? (sumaTotalIngresos * 7 / 10).ToString() : (sumaTotalIngresos * 8 / 10).ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (gastosOperacionalesMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(gastosOperacionalesMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaTotalPatrimonioNeto = 0;
                            sumaTotalPatrimonioNeto = Math.Round(datosSocietario.Balance.Patrimonio.PatrimonioNeto, 2, MidpointRounding.AwayFromZero);
                            if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                                minimo = 50000;
                            else
                                minimo = 15000;

                            resultadoComparacion = sumaTotalPatrimonioNeto > minimo;

                            if (patrimonioNetoMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = patrimonioNetoMicrocredito.Id,
                                    Politica = patrimonioNetoMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaTotalPatrimonioNeto),
                                    Valor = sumaTotalPatrimonioNeto.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (patrimonioNetoMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(patrimonioNetoMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaPromedioMedioCobro = 0;
                            sumaPromedioMedioCobro = Math.Round(datosSocietario.Balance.Indices.PeriodoPromCobro, 2, MidpointRounding.AwayFromZero);
                            if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                                minimo = 90;
                            else
                                minimo = 60;
                            resultadoComparacion = sumaPromedioMedioCobro <= minimo;

                            if (periodoMedioCobranzaMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = periodoMedioCobranzaMicrocredito.Id,
                                    Politica = periodoMedioCobranzaMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorIgualDias, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Dias, sumaPromedioMedioCobro.ToString()),
                                    Valor = sumaPromedioMedioCobro.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (periodoMedioCobranzaMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(periodoMedioCobranzaMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaTotalActivos = 0;
                            sumaTotalActivos = Math.Round(datosSocietario.Balance.Activos.TotalActivo, 2, MidpointRounding.AwayFromZero);
                            if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                                minimo = 30000;
                            else
                                minimo = 10000;
                            resultadoComparacion = sumaTotalActivos > minimo;

                            if (totalActivosMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = totalActivosMicrocredito.Id,
                                    Politica = totalActivosMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaTotalActivos),
                                    Valor = sumaTotalActivos.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (totalActivosMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(totalActivosMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaTotalPasivos = 0;
                            sumaTotalPasivos = Math.Round(datosSocietario.Balance.Pasivos.TotalPasivo, 2, MidpointRounding.AwayFromZero);
                            if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                                resultadoComparacion = sumaTotalPasivos <= (sumaTotalActivos * 7 / 10);
                            else
                                resultadoComparacion = sumaTotalPasivos <= (sumaTotalActivos * 8 / 10);

                            if (totalPasivosMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = totalPasivosMicrocredito.Id,
                                    Politica = totalPasivosMicrocredito.Nombre,
                                    ReferenciaMinima = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? string.Format(Dominio.Constantes.ConstanteCliente1790325083001.PasivosSetentaPorciento, (sumaTotalActivos * 7 / 10)) : string.Format(Dominio.Constantes.TextoReferencia.Pasivos, (sumaTotalActivos * 8 / 10)),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaTotalPasivos),
                                    Valor = sumaTotalPasivos.ToString(),
                                    Parametro = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? (sumaTotalActivos * 7 / 10).ToString() : (sumaTotalActivos * 8 / 10).ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (totalPasivosMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(totalPasivosMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            decimal sumaUtilidadEjercicio = 0;
                            sumaUtilidadEjercicio = Math.Round(datosSocietario.Balance.Otros.UtilidadEjercicio, 2, MidpointRounding.AwayFromZero);
                            minimo = 10000;
                            resultadoComparacion = sumaUtilidadEjercicio > minimo;

                            if (utilidadEjercicioMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = utilidadEjercicioMicrocredito.Id,
                                    Politica = utilidadEjercicioMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", sumaUtilidadEjercicio),
                                    Valor = sumaUtilidadEjercicio.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (utilidadEjercicioMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(utilidadEjercicioMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Societario. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Societario

                #region Evaluación IESS
                try
                {
                    _logger.LogInformation("Procesando políticas IESS...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Iess && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var iess = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Persona>(detalleHistorial.Datos);

                        if (iess != null)
                        {
                            var mora = Convert.ToDouble(iess.Mora);
                            minimo = 50;

                            var resultadoComparacion = mora < minimo;

                            if (iessMoraPatronalMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = iessMoraPatronalMicrocredito.Id,
                                    Politica = iessMoraPatronalMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", mora.ToString()),
                                    Valor = mora.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (iessMoraPatronalMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(iessMoraPatronalMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                    _logger.LogInformation("Fin procesamiento políticas IESS.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de IESS. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación IESS

                #region Evaluación Afiliado
                try
                {
                    _logger.LogInformation("Procesando políticas AFILIADO...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var afiliado = JsonConvert.DeserializeObject<Afiliacion>(detalleHistorial.Datos);
                        var estadoAfiliadoIess = new[] { "ACTIVO/A", "JUBILADO/A", "AFILIADO ACTIVO" };
                        var resultadoComparacion = false;
                        var estadoAfiliado = "N/A";
                        if (!string.IsNullOrEmpty(afiliado.Estado))
                        {
                            estadoAfiliado = afiliado.Estado;
                            resultadoComparacion = estadoAfiliadoIess.Contains(afiliado.Estado);
                        }

                        if (iessAfiliacionMicrocredito != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = iessAfiliacionMicrocredito.Id,
                                Politica = iessAfiliacionMicrocredito.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.EstadoAfiliacion, "ACTIVO/A, JUBILADO/A"),
                                ValorResultado = estadoAfiliado,
                                Valor = estadoAfiliado,
                                Parametro = "ACTIVO/A, JUBILADO/A, AFILIADO ACTIVO",
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (iessAfiliacionMicrocredito.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(iessAfiliacionMicrocredito.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }

                    _logger.LogInformation("Fin procesamiento políticas AFILIADO.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de AFILIADO. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación Afiliado

                #region Senescyt
                try
                {
                    _logger.LogInformation("Procesando políticas SENECYT...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var senescyt = JsonConvert.DeserializeObject<Externos.Logica.Senescyt.Modelos.Persona>(detalleHistorial.Datos);
                        minimo = 0;

                        var resultadoComparacion = senescyt.TotalTitulos > 0;

                        if (titulosSenescytMicrocredito != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = titulosSenescytMicrocredito.Id,
                                Politica = titulosSenescytMicrocredito.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Titulos, senescyt.TotalTitulos.ToString()),
                                Valor = senescyt.TotalTitulos.ToString(),
                                Parametro = 0.ToString(),
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (titulosSenescytMicrocredito.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(titulosSenescytMicrocredito.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }
                    _logger.LogInformation("Fin procesamiento políticas SENECYT.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SENECYT. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación Senescyt                

                #region Evaluación Ant
                try
                {
                    _logger.LogInformation("Procesando políticas ANT...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ant && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var fuenteAnt = JsonConvert.DeserializeObject<Externos.Logica.ANT.Modelos.Licencia>(detalleHistorial.Datos);
                        if (fuenteAnt != null)
                        {
                            minimo = 100;
                            var autos = fuenteAnt.Autos != null && fuenteAnt.Autos.Any() ? fuenteAnt.Autos.Where(m => (!string.IsNullOrEmpty(m.Placa) && m.Placa.Length > 1 && m.Placa != "SIN/PLACA") && (string.IsNullOrEmpty(m.NombrePropietario) || m.NombrePropietario == fuenteAnt.Titular)).ToList() : new List<Externos.Logica.ANT.Modelos.Auto>();
                            if (fuenteAnt.Multas != null && fuenteAnt.Multas.Any())
                                fuenteAnt.Multas = fuenteAnt.Multas.Where(m => autos.Select(m => m.Placa).Contains(m.Placa) || m.Placa == "-" || m.Placa == "SIN/PLACA" || m.Placa.Length == 1 || m.Placa?.Trim() == string.Empty).ToList();

                            var totalMultas = fuenteAnt.Multas.Sum(x => (!x.Pagada.HasValue || (x.Pagada.HasValue && !x.Pagada.Value)) && (!x.Reclamo.HasValue || (x.Reclamo.HasValue && !x.Reclamo.Value))
                                              && (!x.Anulada.HasValue || (x.Anulada.HasValue && !x.Anulada.Value)) ? x.Saldo : 0);
                            var resultadoComparacion = totalMultas < minimo;
                            if (pagoMultasPendientesMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = pagoMultasPendientesMicrocredito.Id,
                                    Politica = pagoMultasPendientesMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorIgualMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", totalMultas.ToString("N", culture)),
                                    Valor = totalMultas.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (pagoMultasPendientesMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(pagoMultasPendientesMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            if (!string.IsNullOrEmpty(tipoIdentificacion) && (tipoIdentificacion == Dominio.Constantes.General.RucNatural || tipoIdentificacion == Dominio.Constantes.General.Cedula))
                            {
                                minimo = 0;
                                resultadoComparacion = autos.Count > minimo;

                                if (vehiculosMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = vehiculosMicrocredito.Id,
                                        Politica = vehiculosMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, autos.Count.ToString()),
                                        Valor = autos.Count.ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (vehiculosMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(vehiculosMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de ANT. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación Ant

                #region Evaluación SERCOP
                try
                {
                    _logger.LogInformation("Procesando políticas SERCOP...");
                    int? contadorSercop = null;
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Proveedor && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var proveedor = JsonConvert.DeserializeObject<Externos.Logica.SERCOP.Modelos.ProveedorIncumplido>(detalleHistorial.Datos);
                        contadorSercop = 0;
                        if (proveedor.ProveedoresIncop != null && proveedor.ProveedoresIncop.Any()) contadorSercop += proveedor.ProveedoresIncop.Count;
                        if (proveedor.ProveedoresContraloria != null && proveedor.ProveedoresContraloria.Any()) contadorSercop += proveedor.ProveedoresContraloria.Count;
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.ProveedorContraloria && m.Generado);

                    if (detalleHistorial != null)
                    {
                        if (contadorSercop == null) contadorSercop = 0;
                        var proveedorContraloria = JsonConvert.DeserializeObject<List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria>>(detalleHistorial.Datos);
                        if (proveedorContraloria != null && proveedorContraloria.Any()) contadorSercop += proveedorContraloria.Count;
                    }
                    if (contadorSercop == null) contadorSercop = 0;

                    if (contadorSercop != null)
                    {
                        minimo = 0;
                        var resultadoComparacion = contadorSercop == minimo;

                        if (sercopMicrocredito != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = sercopMicrocredito.Id,
                                Politica = sercopMicrocredito.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Maximo, minimo),
                                ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Procesos, contadorSercop),
                                Valor = contadorSercop.ToString(),
                                Parametro = minimo.ToString(),
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (sercopMicrocredito.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(sercopMicrocredito.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }
                    _logger.LogInformation("Fin procesamiento políticas SERCOP.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SERCOP. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación SERCOP

                #region PensionAlimenticia
                try
                {
                    _logger.LogInformation("Procesando políticas Pensión Alimenticia...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia && m.Generado);
                    var resultadoComparacion = false;

                    if (detalleHistorial != null)
                    {
                        var pensionAlimenticia = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(detalleHistorial.Datos);
                        double totalDeudaAlimenticia = 0;
                        minimo = 0;

                        if (pensionAlimenticia != null && pensionAlimenticia.Resultados != null && pensionAlimenticia.Resultados.Any())
                        {
                            var valorDeudaPension = new List<List<Externos.Logica.PensionesAlimenticias.Modelos.Movimiento>>();
                            var resultadoPension = pensionAlimenticia.Resultados;
                            var nombreHistorial = resultadoPension.FirstOrDefault().Nombre;
                            var movimientosPension = resultadoPension.Select(x => new { x.Movimientos, x.Intervinientes }).ToList();
                            if (movimientosPension != null && movimientosPension.Any() && !string.IsNullOrEmpty(nombreHistorial))
                            {
                                var nombreDivido = nombreHistorial.Split(' ');
                                foreach (var item in movimientosPension)
                                {
                                    var obligadoNombre = item.Intervinientes.Where(x => x.Tipo.ToUpper() == "OBLIGADO PRINCIPAL").Select(x => x.Nombre).ToList();
                                    if (obligadoNombre != null && obligadoNombre.Any())
                                    {
                                        var listaNombre = new List<bool>();
                                        foreach (var item1 in obligadoNombre)
                                        {
                                            var nombreSeparado = item1.Split(' ');
                                            listaNombre.Clear();
                                            foreach (var item2 in nombreSeparado)
                                            {
                                                if (nombreHistorial.Contains(item2))
                                                    listaNombre.Add(true);
                                                else
                                                    listaNombre.Add(false);
                                            }
                                            if (listaNombre.Count(x => x) == nombreDivido.Length)
                                                valorDeudaPension.Add(item.Movimientos);
                                        }
                                    }
                                }
                            }

                            if (valorDeudaPension != null && valorDeudaPension.Any())
                            {
                                totalDeudaAlimenticia = valorDeudaPension.Sum(x => x.Sum(m => double.Parse(!string.IsNullOrEmpty(m.ValorDeudaOriginal) ? m.ValorDeudaOriginal.Replace(",", ".") : "0"))) - valorDeudaPension.Sum(x => x.Sum(m => double.Parse(!string.IsNullOrEmpty(m.ValorPagadoOriginal) ? m.ValorPagadoOriginal.Replace(",", ".") : "0")));
                                resultadoComparacion = totalDeudaAlimenticia <= minimo;
                                if (pagoPendientePensionAlimenticiaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = pagoPendientePensionAlimenticiaMicrocredito.Id,
                                        Politica = pagoPendientePensionAlimenticiaMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MaximoMoneda, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", totalDeudaAlimenticia.ToString("N", culture)),
                                        Valor = totalDeudaAlimenticia.ToString("N", culture),
                                        Parametro = minimo.ToString("N", culture),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });

                                    if (pagoPendientePensionAlimenticiaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(pagoPendientePensionAlimenticiaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = true;
                                if (pagoPendientePensionAlimenticiaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = pagoPendientePensionAlimenticiaMicrocredito.Id,
                                        Politica = pagoPendientePensionAlimenticiaMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MaximoMoneda, "0.00"),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", "0.00"),
                                        Valor = "0.00",
                                        Parametro = "0.00",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });

                                    if (pagoPendientePensionAlimenticiaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(pagoPendientePensionAlimenticiaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        resultadoComparacion = true;
                        if (pagoPendientePensionAlimenticiaMicrocredito != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = pagoPendientePensionAlimenticiaMicrocredito.Id,
                                Politica = pagoPendientePensionAlimenticiaMicrocredito.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MaximoMoneda, "0.00"),
                                ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", "0.00"),
                                Valor = "0.00",
                                Parametro = "0.00",
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });

                            if (pagoPendientePensionAlimenticiaMicrocredito.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(pagoPendientePensionAlimenticiaMicrocredito.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Pensión Alimenticia. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region SuperintendenciaBancos
                try
                {
                    _logger.LogInformation("Procesando políticas SuperintendenciaBancos...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var superBancos = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Datos);
                        if (superBancos != null)
                        {
                            var estadoBancos = superBancos.Estado;
                            if (!string.IsNullOrEmpty(estadoBancos))
                            {
                                var resultadoComparacion = estadoBancos.ToUpper().Equals("HABILITADO");
                                if (superintendenciaBancosCedulaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = superintendenciaBancosCedulaMicrocredito.Id,
                                        Politica = superintendenciaBancosCedulaMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.EstadoSuperBancos, "HABILITADO"),
                                        ValorResultado = estadoBancos.ToUpper(),
                                        Valor = estadoBancos.ToUpper(),
                                        Parametro = "HABILITADO",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (superintendenciaBancosCedulaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(superintendenciaBancosCedulaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var superBancosRucNatural = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Datos);
                        if (superBancosRucNatural != null)
                        {
                            var estadoBancos = superBancosRucNatural.Estado;
                            if (!string.IsNullOrEmpty(estadoBancos))
                            {
                                var resultadoComparacion = estadoBancos.ToUpper().Equals("HABILITADO");
                                if (superintendenciaBancosRucNaturalMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = superintendenciaBancosRucNaturalMicrocredito.Id,
                                        Politica = superintendenciaBancosRucNaturalMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.EstadoSuperBancos, "HABILITADO"),
                                        ValorResultado = estadoBancos.ToUpper(),
                                        Valor = estadoBancos.ToUpper(),
                                        Parametro = "HABILITADO",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (superintendenciaBancosRucNaturalMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(superintendenciaBancosRucNaturalMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var superBancosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Datos);
                        if (superBancosEmpresa != null)
                        {
                            var estadoBancos = superBancosEmpresa.Estado;
                            if (!string.IsNullOrEmpty(estadoBancos))
                            {
                                var resultadoComparacion = estadoBancos.ToUpper().Equals("HABILITADO");
                                if (superintendenciaBancosEmpresaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = superintendenciaBancosEmpresaMicrocredito.Id,
                                        Politica = superintendenciaBancosEmpresaMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.EstadoSuperBancos, "HABILITADO"),
                                        ValorResultado = estadoBancos.ToUpper(),
                                        Valor = estadoBancos.ToUpper(),
                                        Parametro = "HABILITADO",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (superintendenciaBancosEmpresaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(superintendenciaBancosEmpresaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SuperintendenciaBancos. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region AntecedentesPenales
                try
                {
                    _logger.LogInformation("Procesando políticas Antecedentes Penales...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.AntecedentesPenales && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var antecedentes = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.Resultado>(detalleHistorial.Datos);
                        if (antecedentes != null && !string.IsNullOrEmpty(antecedentes.Antecedente))
                        {
                            var resultadoComparacion = !antecedentes.Antecedente.ToUpper().Equals("SI");
                            if (antecedentesPenalesMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = antecedentesPenalesMicrocredito.Id,
                                    Politica = antecedentesPenalesMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.AntecedentesPenales, "NO"),
                                    ValorResultado = antecedentes.Antecedente.ToUpper(),
                                    Valor = antecedentes.Antecedente.ToUpper(),
                                    Parametro = "NO",
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (antecedentesPenalesMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(antecedentesPenalesMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Antecedentes Penales. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region Predios
                try
                {
                    if (!string.IsNullOrEmpty(tipoIdentificacion) && (tipoIdentificacion == Dominio.Constantes.General.RucNatural || tipoIdentificacion == Dominio.Constantes.General.Cedula))
                    {
                        _logger.LogInformation("Procesando políticas Predios...");
                        var datosPredioQuito = new Externos.Logica.PredioMunicipio.Modelos.Resultado();
                        var datosPredioCuenca = new Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca();
                        var datosPredioStoDomingo = new Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo();
                        var datosPredioRuminahui = new Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui();
                        var datosPredioQuininde = new Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde();
                        var cantidadPropiedades = 0;
                        var prediosCuenca = new List<string>();
                        var prediosQuininde = new List<string>();
                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioQuito = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioCuenca = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioStoDomingo = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioRuminahui = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioQuininde = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(detalleHistorial.Datos);

                        if (datosPredioQuito != null && datosPredioQuito.Detalle != null && datosPredioQuito.Detalle.Any())
                            cantidadPropiedades += datosPredioQuito.Detalle.Where(m => m.Concepto.ToUpper().Contains("PREDIO") || m.Concepto.ToUpper().Contains("PREDIAL") || m.Concepto.ToUpper().Contains("CEM")).GroupBy(m => m.Numero).Count();

                        if (datosPredioCuenca != null && datosPredioCuenca.ValoresPendientes != null && datosPredioCuenca.ValoresPendientes.Any())
                            prediosCuenca.AddRange(datosPredioCuenca.ValoresPendientes.GroupBy(m => m.ClaveCatastral).Select(m => m.ToString()).ToList());

                        if (datosPredioCuenca != null && datosPredioCuenca.ValoresCancelados != null && datosPredioCuenca.ValoresCancelados.Any())
                            prediosCuenca.AddRange(datosPredioCuenca.ValoresCancelados.GroupBy(m => m.Clave).Select(m => m.ToString()).ToList());

                        cantidadPropiedades += prediosCuenca.GroupBy(m => m).Count();

                        if (datosPredioStoDomingo != null && datosPredioStoDomingo.Detalle != null && datosPredioStoDomingo.Detalle.Any())
                            cantidadPropiedades += datosPredioStoDomingo.Detalle.GroupBy(m => m.Clave).Count();

                        if (datosPredioRuminahui != null && datosPredioRuminahui.Detalle != null && datosPredioRuminahui.Detalle.Any())
                            cantidadPropiedades += datosPredioRuminahui.Detalle.GroupBy(m => m.ClaveCatastral).Count();

                        if (datosPredioQuininde != null && datosPredioQuininde.ValoresPendientes != null && datosPredioQuininde.ValoresPendientes.Any())
                            prediosQuininde.AddRange(datosPredioQuininde.ValoresPendientes.SelectMany(x => x.InformacionPredio.Select(m => m.Clave)).ToList());

                        if (datosPredioQuininde != null && datosPredioQuininde.ValoresCancelados != null && datosPredioQuininde.ValoresCancelados.Any())
                            prediosQuininde.AddRange(datosPredioQuininde.ValoresCancelados.SelectMany(x => x.InformacionPredio.Select(m => m.Clave)).ToList());

                        cantidadPropiedades += prediosQuininde.GroupBy(m => m).Count();

                        if (cantidadPropiedades > 0)
                        {
                            minimo = 0;
                            var resultadoComparacion = cantidadPropiedades > minimo;
                            if (propiedadesMicrocredito != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = propiedadesMicrocredito.Id,
                                    Politica = propiedadesMicrocredito.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Predios, cantidadPropiedades.ToString()),
                                    Valor = cantidadPropiedades.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (propiedadesMicrocredito.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(propiedadesMicrocredito.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Predios. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region NoticiaDelito
                try
                {
                    _logger.LogInformation("Procesando políticas Noticias del Delito...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosPersona && m.Generado);
                    var entidad = await _historiales.FirstOrDefaultAsync(m => new { m.IdentificacionSecundaria, m.NombresPersona }, m => m.Id == modelo.IdHistorial, null);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos) && entidad != null)
                    {
                        var delitoNoticia = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(detalleHistorial.Datos);
                        if (delitoNoticia != null)
                        {
                            minimo = 0;
                            var resultadoComparacion = false;
                            var cantidadSujeto = new List<string>();
                            var sujetos = delitoNoticia.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Contains("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                            if (sujetos != null && sujetos.Any())
                            {
                                var nombreDivido = entidad.NombresPersona?.Split(' ');
                                var listaNombre = new List<bool>();

                                foreach (var item1 in sujetos.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                                {
                                    if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Contains("SOSPECHOSO"))
                                    {
                                        if (!string.IsNullOrEmpty(item1.Cedula) && !string.IsNullOrEmpty(entidad.IdentificacionSecundaria) && entidad.IdentificacionSecundaria == item1.Cedula)
                                            cantidadSujeto.Add(item1.Numero);
                                        else
                                        {
                                            var nombreSeparado = item1.NombresCompletos.Split(' ');
                                            listaNombre.Clear();
                                            foreach (var item2 in nombreSeparado)
                                            {
                                                if (!string.IsNullOrEmpty(entidad.NombresPersona) && entidad.NombresPersona.Contains(item2))
                                                    listaNombre.Add(true);
                                                else
                                                    listaNombre.Add(false);
                                            }
                                            if (nombreDivido != null && nombreDivido.Any() && listaNombre.Count(x => x) == nombreDivido.Length)
                                                cantidadSujeto.Add(item1.Numero);
                                        }
                                    }
                                }
                                cantidadSujeto = cantidadSujeto.Distinct().ToList();
                            }

                            if (cantidadSujeto.Count() > 0)
                            {
                                resultadoComparacion = minimo > cantidadSujeto.Count();
                                if (noticiasDelitoMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = noticiasDelitoMicrocredito.Id,
                                        Politica = noticiasDelitoMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.NoticiaDelito, minimo.ToString()),
                                        ValorResultado = cantidadSujeto.Count().ToString(),
                                        Valor = cantidadSujeto.Count().ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (noticiasDelitoMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(noticiasDelitoMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = true;
                                if (noticiasDelitoMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = noticiasDelitoMicrocredito.Id,
                                        Politica = noticiasDelitoMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.NoticiaDelito, minimo.ToString()),
                                        ValorResultado = cantidadSujeto.Count().ToString(),
                                        Valor = cantidadSujeto.Count().ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (noticiasDelitoMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(noticiasDelitoMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa && m.Generado);
                    var empresaFiscalia = await _historiales.FirstOrDefaultAsync(m => new { m.Identificacion, m.RazonSocialEmpresa }, m => m.Id == modelo.IdHistorial, null);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos) && empresaFiscalia != null)
                    {
                        var delitosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(detalleHistorial.Datos);
                        if (delitosEmpresa != null)
                        {
                            minimo = 0;
                            var resultadoComparacion = false;
                            var cantidadSujeto = new List<string>();
                            var sujetos = delitosEmpresa.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Contains("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                            if (sujetos != null && sujetos.Any())
                            {
                                foreach (var item1 in sujetos.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                                {
                                    if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Contains("SOSPECHOSO"))
                                    {
                                        if (!string.IsNullOrEmpty(item1.Cedula) && !string.IsNullOrEmpty(empresaFiscalia.Identificacion) && empresaFiscalia.Identificacion == item1.Cedula)
                                            cantidadSujeto.Add(item1.Numero);
                                        else if (!string.IsNullOrEmpty(item1.NombresCompletos) && !string.IsNullOrEmpty(empresaFiscalia.RazonSocialEmpresa) && empresaFiscalia.RazonSocialEmpresa == item1.NombresCompletos)
                                            cantidadSujeto.Add(item1.Numero);
                                    }
                                }
                                cantidadSujeto = cantidadSujeto.Distinct().ToList();
                            }

                            if (cantidadSujeto.Count() > 0)
                            {
                                resultadoComparacion = minimo > cantidadSujeto.Count();
                                if (noticiasDelitoEmpresaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = noticiasDelitoEmpresaMicrocredito.Id,
                                        Politica = noticiasDelitoEmpresaMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.NoticiaDelito, minimo.ToString()),
                                        ValorResultado = cantidadSujeto.Count().ToString(),
                                        Valor = cantidadSujeto.Count().ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (noticiasDelitoEmpresaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(noticiasDelitoEmpresaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = true;
                                if (noticiasDelitoEmpresaMicrocredito != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = noticiasDelitoEmpresaMicrocredito.Id,
                                        Politica = noticiasDelitoEmpresaMicrocredito.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.NoticiaDelito, minimo.ToString()),
                                        ValorResultado = cantidadSujeto.Count().ToString(),
                                        Valor = cantidadSujeto.Count().ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (noticiasDelitoEmpresaMicrocredito.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(noticiasDelitoEmpresaMicrocredito.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Noticias del Delito. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region UltimoAnioBalance
                try
                {
                    if (datosSocietario != null && datosSocietario.DirectorioCompania != null && datosSocietario.DirectorioCompania.UltimoBalance != 0)
                    {
                        var fechaActualAnio = DateTime.Now;
                        if (fechaActualAnio.Date >= new DateTime(fechaActualAnio.Year, 7, 1))
                            minimo = fechaActualAnio.Year - 1;
                        else
                            minimo = fechaActualAnio.Year - 2;

                        var resultadoComparacion = datosSocietario.DirectorioCompania.UltimoBalance >= minimo;
                        if (ultimoAnioBalanceMicrocredito != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = ultimoAnioBalanceMicrocredito.Id,
                                Politica = ultimoAnioBalanceMicrocredito.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.IgualAnio, minimo),
                                ValorResultado = datosSocietario.DirectorioCompania.UltimoBalance.ToString(),
                                Valor = datosSocietario.DirectorioCompania.UltimoBalance.ToString(),
                                Parametro = minimo.ToString(),
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (ultimoAnioBalanceMicrocredito.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(ultimoAnioBalanceMicrocredito.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar política Último Año Balance. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion UltimoAnioBalance

                #region Procesamiento Calificación de Evaluación
                if (detalleCalificacion != null && detalleCalificacion.Any())
                {
                    datosPersona.TipoCalificacion = Dominio.Tipos.TiposCalificaciones.Evaluacion;
                    datosPersona.TotalValidados = detalleCalificacion.Count;
                    datosPersona.TotalAprobados = detalleCalificacion.Count(m => m.ResultadoPolitica);
                    datosPersona.TotalRechazados = detalleCalificacion.Count(m => !m.ResultadoPolitica);

                    if (datosPersona.TotalValidados > 0) datosPersona.Calificacion = Math.Round((decimal)datosPersona.TotalAprobados * 100 / datosPersona.TotalValidados, 2, MidpointRounding.AwayFromZero);
                    datosPersona.Aprobado = datosPersona.Calificacion >= Dominio.Constantes.ConstantesCalificacion.MinimoCalificacion;
                    datosPersona.DetalleCalificacion = detalleCalificacion;

                    if (aprobacionAdicional)
                    {
                        datosPersona.Observaciones = string.Join(". ", observaciones);
                        datosPersona.Aprobado = false;
                    }

                    var calificacionesDetalle = datosPersona.DetalleCalificacion.Select(m => new DetalleCalificacion
                    {
                        IdPolitica = m.IdPolitica,
                        Valor = m.Valor,
                        Parametro = m.Parametro,
                        Aprobado = m.ResultadoPolitica,
                        Datos = m.ValorResultado,
                        ReferenciaMinima = m.ReferenciaMinima,
                        UsuarioCreacion = modelo.IdUsuario,
                        Observacion = m.Observacion,
                        FechaCorte = m.FechaCorte,
                        FechaCreacion = m.FechaCreacion
                    }).ToList();

                    datosPersona.IdCalificacion = await _calificaciones.GuardarCalificacionAsync(new Calificacion()
                    {
                        IdHistorial = modelo.IdHistorial,
                        Puntaje = datosPersona.Calificacion,
                        Aprobado = datosPersona.Aprobado,
                        NumeroAprobados = datosPersona.TotalAprobados,
                        NumeroRechazados = datosPersona.TotalRechazados,
                        TotalVerificados = datosPersona.TotalValidados,
                        Observaciones = datosPersona.Observaciones,
                        UsuarioCreacion = modelo.IdUsuario,
                        TipoCalificacion = datosPersona.TipoCalificacion,
                        DetalleCalificacion = calificacionesDetalle,
                        FechaCreacion = DateTime.Now,
                        CalificacionPersonalizada = true
                    });
                    datosPersonaLista.Add(datosPersona);
                }
                if ((detalleCalificacion != null && detalleCalificacion.Any()))
                {
                    var historialAnterior = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null);
                    var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial, null);
                    historialAnterior.IdPlanEvaluacion = dataPlanEvaluacion.Id;
                    await _historiales.UpdateAsync(historialAnterior);
                    if (historialConsolidado != null)
                    {
                        var historialEvaluacionBuro = await _calificaciones.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoCalificacion == TiposCalificaciones.Buro, null);
                        if (historialEvaluacionBuro != null)
                            historialConsolidado.AprobadoEvaluacion = datosPersona.Aprobado && historialEvaluacionBuro.Aprobado;
                        else
                            historialConsolidado.AprobadoEvaluacion = datosPersona.Aprobado;

                        historialConsolidado.ConsultaEvaluacion = historialAnterior.IdPlanEvaluacion.HasValue && historialAnterior.IdPlanEvaluacion.Value > 0;
                        await _reporteConsolidado.UpdateAsync(historialConsolidado);
                    }
                }
                #endregion Procesamiento Calificación de Evaluación

                _logger.LogInformation($"Fin de procesamiento RUC Jurídico {modelo.IdHistorial}");
                return datosPersonaLista;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<List<CalificacionApiMetodoViewModel>> ObtenerCalificacionConsumo(ApiViewModel_1790325083001 modelo)
        {
            try
            {

                _logger.LogInformation($"Procesando informacion Cedula del Historial {modelo.IdHistorial}");
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                #region Inicialización
                var datosPersona = new CalificacionApiMetodoViewModel();
                var detalleCalificacion = new List<DetalleCalificacionApiMetodoViewModel>();
                var politica = new Politica();
                var datosHistorial = new SRIViewModel();
                var datosSocietario = new BalancesViewModel();
                var detalleHistorial = new DatosJsonViewModel();
                var calificacionAnterior = new Calificacion();
                var detalleCalificacionAnterior = new List<DetalleCalificacion>();
                var observaciones = new List<string>();
                int minimo;
                var datosPersonaLista = new List<CalificacionApiMetodoViewModel>();
                var culture = System.Globalization.CultureInfo.CurrentCulture;

                //Planes Activos
                var resultadoPermiso = Dominio.Tipos.EstadosPlanesEvaluaciones.Activo; ;
                var dataPlanEvaluacion = await _planesEvaluaciones.FirstOrDefaultAsync(s => s, s => s.IdEmpresa == modelo.IdEmpresa && s.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo);
                if (dataPlanEvaluacion == null)
                    throw new Exception("No se encontró un plan de evaluación Activo.");

                var dataUsuario = await _accesos.AnyAsync(s => s.IdUsuario == modelo.IdUsuario && s.Estado == Dominio.Tipos.EstadosAccesos.Activo && s.Acceso == Dominio.Tipos.TiposAccesos.Evaluacion);
                if (!dataUsuario)
                    throw new Exception("El usuario no tiene permisos para la evaluación.");

                var fechaActual = DateTime.Now;
                var primerDiadelMes = new DateTime(fechaActual.Year, fechaActual.Month, 1);
                var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);
                var numeroHistorialEvaluacion = await _historiales.CountAsync(s => s.Id != modelo.IdHistorial && s.IdPlanEvaluacion == dataPlanEvaluacion.Id && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date);

                if (dataPlanEvaluacion.BloquearConsultas)
                    resultadoPermiso = dataPlanEvaluacion.NumeroConsultas > numeroHistorialEvaluacion ? Dominio.Tipos.EstadosPlanesEvaluaciones.Activo : Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo;
                if (resultadoPermiso != Dominio.Tipos.EstadosPlanesEvaluaciones.Activo)
                    throw new Exception("No es posible realizar esta consulta ya que excedió el límite de consultas del plan Evaluación.");

                //Politicas
                var politicasActuales = await _politicas.ReadAsync(m => m, m => m.IdEmpresa == modelo.IdEmpresa && m.Estado);
                if (!politicasActuales.Any())
                    throw new Exception("La empresa actual no tiene registrada políticas.");

                var tipoIdentificacion = await _historiales.FirstOrDefaultAsync(m => m.TipoIdentificacion, m => m.Id == modelo.IdHistorial);
                #endregion Inicialización

                #region Políticas Evaluaciones
                bool aprobacionAdicional = false;

                _logger.LogInformation("Obteniendo políticas para procesamiento...");
                //General
                var antecedentesPenalesConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.antecedentesPenalesConsumo);
                var antiguedadRucConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.antiguedadRucConsumo);
                var contactableDireccionesConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.contactableDireccionesConsumo);
                var contactableEmailsConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.contactableEmailsConsumo);
                var contactableTelefonosConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.contactableTelefonosConsumo);
                var deudaFirmeConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.deudaFirmeConsumo);
                var edadConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.edadConsumo);
                var iessMoraPatronalConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.iessMoraPatronalConsumo);
                var impuestoRentaConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.impuestoRentaConsumo);
                var noticiasDelitoConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.noticiasDelitoConsumo);
                var pagoMultasPendientesConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.pagoMultasPendientesConsumo);
                var pagoPendientePensionAlimenticiaConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.pagoPendientePensionAlimenticiaConsumo);
                var permisoFacturacionConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.permisoFacturacionConsumo);
                var sercopConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.sercopConsumo);
                var superintendenciaBancosCedulaConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.superintendenciaBancosCedulaConsumo);
                var superintendenciaBancosRucNaturalConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.superintendenciaBancosRucNaturalConsumo);
                var titulosSenescytConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.titulosSenescytConsumo);
                var vehiculosConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.vehiculosConsumo);
                var propiedadesConsumo = politicasActuales.FirstOrDefault(m => m.Tipo == Dominio.Tipos.Politicas.propiedadesConsumo);

                #endregion Políticas Evaluaciones

                #region Sri
                try
                {
                    _logger.LogInformation("Procesando políticas SRI...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);

                    if (detalleHistorial != null)
                    {
                        datosHistorial.Sri = JsonConvert.DeserializeObject<Contribuyente>(detalleHistorial.Datos);

                        datosPersona.FechaInicio = datosHistorial.Sri.FechaInicio;
                        var diferenciaAnios = DateTime.Today.Year - datosPersona.FechaInicio.Date.Year;
                        if (datosPersona.FechaInicio.Date > DateTime.Today.AddYears(-diferenciaAnios))
                            diferenciaAnios--;

                        minimo = 1;
                        var estadoRuc = Dominio.Constantes.Politicas.RucActivo;
                        var resultadoComparacion = diferenciaAnios >= minimo && estadoRuc == datosHistorial.Sri.Estado;
                        if (!string.IsNullOrEmpty(datosHistorial.Sri.EstadoContribuyente) && datosHistorial.Sri.EstadoContribuyente == "SUSPENDIDO" && datosHistorial.Sri.EstadoTributario != null && !string.IsNullOrEmpty(datosHistorial.Sri.EstadoTributario.Estado) && datosHistorial.Sri.EstadoTributario.Estado != "OBLIGACIONES TRIBUTARIAS PENDIENTES")
                        {
                            resultadoComparacion = diferenciaAnios >= minimo;
                            if (antiguedadRucConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = antiguedadRucConsumo.Id,
                                    Politica = antiguedadRucConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAniosRucRimpeNatural, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.FechaAnios, datosPersona.FechaInicio.ToString("yyyy/MM/dd"), diferenciaAnios),
                                    Valor = diferenciaAnios.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (antiguedadRucConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(antiguedadRucConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                        else
                        {
                            if (antiguedadRucConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = antiguedadRucConsumo.Id,
                                    Politica = antiguedadRucConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.ConstanteCliente1790325083001.MayorIgualAniosRucRimpeNatural, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.FechaAnios, datosPersona.FechaInicio.ToString("yyyy/MM/dd"), diferenciaAnios),
                                    Valor = diferenciaAnios.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (antiguedadRucConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(antiguedadRucConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }

                        minimo = 40;
                        if (datosHistorial.Sri.Deudas != null && datosHistorial.Sri.Deudas.Any() && datosHistorial.Sri.Deudas.ContainsKey("Firmes"))
                        {
                            resultadoComparacion = datosHistorial.Sri.Deudas["Firmes"].Valor.Value < minimo;
                            if (deudaFirmeConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = deudaFirmeConsumo.Id,
                                    Politica = deudaFirmeConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", datosHistorial.Sri.Deudas["Firmes"].Valor.Value),
                                    Valor = datosHistorial.Sri.Deudas["Firmes"].Valor.Value.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (deudaFirmeConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(deudaFirmeConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                        else
                        {
                            if (deudaFirmeConsumo != null)
                            {
                                resultadoComparacion = true;
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = deudaFirmeConsumo.Id,
                                    Politica = deudaFirmeConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", "0.00"),
                                    Valor = "0.00",
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (deudaFirmeConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(deudaFirmeConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }

                        minimo = 0;
                        if (datosHistorial.Sri.Anexos != null && datosHistorial.Sri.Anexos.Any())
                        {
                            var datosRenta = datosHistorial.Sri.Anexos.OrderByDescending(x => x.Periodo).Select(x => new { x.Periodo, x.Causado }).ToList().Take(2);
                            int periodo = 0;
                            double causado = 0;
                            var impuestoMensaje = string.Empty;
                            if (datosRenta != null && datosRenta.Any())
                            {
                                if (datosRenta.Count() == 2 && datosRenta.First().Periodo == DateTime.Now.AddYears(-1).Year)
                                {
                                    periodo = datosRenta.First().Periodo;
                                    causado = (double)datosRenta.First().Causado;
                                }
                                else if (datosRenta.Count() == 2 && datosRenta.Last().Periodo == DateTime.Now.AddYears(-1).Year)
                                {
                                    periodo = datosRenta.Last().Periodo;
                                    causado = (double)datosRenta.Last().Causado;
                                }
                                else if (datosRenta.Count() == 1 && datosRenta.First().Periodo == DateTime.Now.AddYears(-1).Year)
                                {
                                    periodo = datosRenta.First().Periodo;
                                    causado = (double)datosRenta.First().Causado;
                                }
                                else
                                {
                                    periodo = DateTime.Now.AddYears(-1).Year;
                                    impuestoMensaje = $"No presenta valor en el año {periodo}";
                                }
                            }
                            else
                            {
                                periodo = DateTime.Now.AddYears(-1).Year;
                                impuestoMensaje = $"No presenta valor en el año {periodo}";
                            }

                            if (string.IsNullOrEmpty(impuestoMensaje))
                            {
                                resultadoComparacion = causado > minimo;
                                if (impuestoRentaConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = impuestoRentaConsumo.Id,
                                        Politica = $"{impuestoRentaConsumo.Nombre} {periodo}",
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", causado >= 0 ? causado.ToString("N", culture) : "0.00"),
                                        Valor = causado >= 0 ? causado.ToString("N", culture) : "0.00",
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        Observacion = $"Periodo {periodo}",
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (impuestoRentaConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(impuestoRentaConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = false;
                                if (impuestoRentaConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = impuestoRentaConsumo.Id,
                                        Politica = $"{impuestoRentaConsumo.Nombre} {periodo}",
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorMoneda, minimo),
                                        ValorResultado = impuestoMensaje,
                                        Valor = impuestoMensaje,
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        Observacion = $"Periodo {periodo}",
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (impuestoRentaConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(impuestoRentaConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }

                        if ((!string.IsNullOrEmpty(datosHistorial.Sri.EstadoContribuyente) && datosHistorial.Sri.EstadoContribuyente != "SUSPENDIDO") || (datosHistorial.Sri.EstadoTributario != null && !string.IsNullOrEmpty(datosHistorial.Sri.EstadoTributario.Estado) && datosHistorial.Sri.EstadoTributario.Estado == "OBLIGACIONES TRIBUTARIAS PENDIENTES"))
                        {
                            if (datosHistorial.Sri.PermisoFacturacion != null && !string.IsNullOrEmpty(datosHistorial.Sri.PermisoFacturacion.Vigencia))
                            {
                                var valorVigencia = Regex.Matches(datosHistorial.Sri.PermisoFacturacion.Vigencia, @"[0-9]+");
                                if (valorVigencia != null && int.TryParse(valorVigencia[0].ToString(), out _))
                                {
                                    var valorFacturacion = int.Parse(valorVigencia[0].ToString());
                                    minimo = 12;
                                    if (valorFacturacion <= 3)
                                    {
                                        resultadoComparacion = valorFacturacion > minimo;
                                        if (permisoFacturacionConsumo != null)
                                        {
                                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                            {
                                                IdPolitica = permisoFacturacionConsumo.Id,
                                                Politica = permisoFacturacionConsumo.Nombre,
                                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMeses, minimo),
                                                ValorResultado = $"{valorVigencia[0]} meses",
                                                Valor = valorVigencia[0].ToString(),
                                                Parametro = minimo.ToString(),
                                                ResultadoPolitica = resultadoComparacion,
                                                FechaCreacion = DateTime.Now
                                            });
                                            if (!resultadoComparacion)
                                            {
                                                observaciones.Add(permisoFacturacionConsumo.Nombre);
                                                aprobacionAdicional = true;
                                            }
                                        }
                                    }
                                    else if (valorFacturacion > 3 && valorFacturacion < 12)
                                    {
                                        resultadoComparacion = valorFacturacion > minimo;
                                        if (permisoFacturacionConsumo != null)
                                        {
                                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                            {
                                                IdPolitica = permisoFacturacionConsumo.Id,
                                                Politica = permisoFacturacionConsumo.Nombre,
                                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMeses, minimo),
                                                ValorResultado = $"{valorVigencia[0]} meses",
                                                Valor = valorVigencia[0].ToString(),
                                                Parametro = minimo.ToString(),
                                                ResultadoPolitica = resultadoComparacion,
                                                FechaCreacion = DateTime.Now
                                            });
                                            if (permisoFacturacionConsumo.Excepcional && !resultadoComparacion)
                                            {
                                                observaciones.Add(permisoFacturacionConsumo.Nombre);
                                                aprobacionAdicional = true;
                                            }
                                        }
                                    }
                                    else if (valorFacturacion >= 12)
                                    {
                                        resultadoComparacion = valorFacturacion >= minimo;
                                        if (permisoFacturacionConsumo != null)
                                        {
                                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                            {
                                                IdPolitica = permisoFacturacionConsumo.Id,
                                                Politica = permisoFacturacionConsumo.Nombre,
                                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgualMeses, minimo),
                                                ValorResultado = $"{valorVigencia[0]} meses",
                                                Valor = valorVigencia[0].ToString(),
                                                Parametro = minimo.ToString(),
                                                ResultadoPolitica = resultadoComparacion,
                                                FechaCreacion = DateTime.Now
                                            });
                                            if (permisoFacturacionConsumo.Excepcional && !resultadoComparacion)
                                            {
                                                observaciones.Add(permisoFacturacionConsumo.Nombre);
                                                aprobacionAdicional = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SRI. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Sri

                #region Civil
                try
                {
                    var direccionesAdicionales = new List<string>();
                    Externos.Logica.Garancheck.Modelos.Persona personaTemp = null;
                    _logger.LogInformation("Procesando políticas Civil...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);

                    var resultadoComparacion = false;
                    var edad = -1;
                    if (detalleHistorial != null)
                    {
                        //Civil En Línea
                        var persona = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(detalleHistorial.Datos);
                        if (persona != null && persona.FechaNacimiento != default)
                        {
                            edad = DateTime.Today.Year - persona.FechaNacimiento.Year;
                            if (persona.FechaNacimiento.Date > DateTime.Today.AddYears(-edad))
                                edad--;
                        }

                        if (persona != null)
                        {
                            var direccionTempRegCivil = string.Join("/", new[] { persona.LugarDomicilio?.Trim(), persona.CalleDomicilio?.Trim(), persona.NumeracionDomicilio?.Trim() }.Where(m => !string.IsNullOrEmpty(m)).ToArray());
                            if (!string.IsNullOrEmpty(direccionTempRegCivil))
                                direccionesAdicionales.Add(direccionTempRegCivil);
                        }
                    }
                    else
                    {
                        //Civil Histórico
                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);

                        if (detalleHistorial != null)
                        {
                            var persona = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(detalleHistorial.Datos);
                            if (!string.IsNullOrEmpty(persona.FechaNacimiento.Value.ToString()))
                            {
                                edad = DateTime.Today.Year - persona.FechaNacimiento.Value.Year;
                                if (persona.FechaNacimiento.Value.Date > DateTime.Today.AddYears(-edad))
                                    edad--;
                            }
                            personaTemp = persona;
                        }
                    }

                    //Edad
                    if (edad >= 0)
                    {
                        if (!string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico)
                            resultadoComparacion = (edad > 25 && edad <= 79);
                        else
                            resultadoComparacion = (edad > 18 && edad <= 79);

                        if (edadConsumo != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = edadConsumo.Id,
                                Politica = edadConsumo.Nombre,
                                ReferenciaMinima = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? "Mayor a 25 años y Menor o igual a 79 años" : "Mayor a 18 años y Menor o igual a 79 años",
                                ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Edad, edad.ToString()),
                                Valor = edad.ToString(),
                                Parametro = !string.IsNullOrEmpty(tipoIdentificacion) && tipoIdentificacion == Dominio.Constantes.General.RucJuridico ? "[25,79]" : "[18,79]",
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (edadConsumo.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(edadConsumo.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }

                    //Contactos
                    var detalleHistorialPersonal = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Personales && m.Generado);

                    Externos.Logica.Garancheck.Modelos.Personal personalTemp = null;
                    if (detalleHistorialPersonal != null)
                        personalTemp = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Personal>(detalleHistorialPersonal.Datos);

                    if (personaTemp == null)
                    {
                        var detalleHistorialCiudadano = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);

                        if (detalleHistorialCiudadano != null)
                            personaTemp = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(detalleHistorialCiudadano.Datos);
                    }

                    if (personalTemp != null && !string.IsNullOrEmpty(personalTemp.NombreCalle?.Trim()) && !string.IsNullOrEmpty(personalTemp.NumeroCasa?.Trim()))
                    {
                        if (personaTemp != null && !string.IsNullOrEmpty(personaTemp.Provincia?.Trim()) && !string.IsNullOrEmpty(personaTemp.Canton?.Trim()) && !string.IsNullOrEmpty(personaTemp.Parroquia?.Trim()))
                            direccionesAdicionales.Add($"{personaTemp.Provincia} / {personaTemp.Canton} / {personaTemp.Parroquia} / {personalTemp.NombreCalle} {personalTemp.NumeroCasa}");
                        else
                            direccionesAdicionales.Add($"{personalTemp.NombreCalle} {personalTemp.NumeroCasa}");
                    }

                    var contactosPersona = new Contacto();
                    var direccionesContacto = 0;
                    var telefonosContacto = 0;
                    var emailsContacto = 0;
                    var detalleContactos = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Contactos && m.Generado);

                    if (detalleContactos != null)
                    {
                        contactosPersona = JsonConvert.DeserializeObject<Contacto>(detalleContactos.Datos);
                        direccionesContacto += contactosPersona.Direcciones.Count;
                        telefonosContacto += contactosPersona.Telefonos.Count;
                        emailsContacto += contactosPersona.Correos.Count;
                    }

                    var detalleContactosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa && m.Generado);

                    if (detalleContactosEmpresa != null)
                    {
                        contactosPersona = JsonConvert.DeserializeObject<Contacto>(detalleContactosEmpresa.Datos);
                        direccionesContacto += contactosPersona.Direcciones.Count;
                        telefonosContacto += contactosPersona.Telefonos.Count;
                        emailsContacto += contactosPersona.Correos.Count;
                    }

                    var detalleContactosIess = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess && m.Generado);

                    if (detalleContactosIess != null)
                    {
                        contactosPersona = JsonConvert.DeserializeObject<Contacto>(detalleContactosIess.Datos);
                        direccionesContacto += contactosPersona.Direcciones.Count;
                        telefonosContacto += contactosPersona.Telefonos.Count;
                        emailsContacto += contactosPersona.Correos.Count;
                    }

                    if (direccionesAdicionales.Any())
                        direccionesContacto += direccionesAdicionales.Count;

                    minimo = 2;
                    resultadoComparacion = direccionesContacto >= minimo;

                    if (contactableDireccionesConsumo != null)
                    {
                        detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                        {
                            IdPolitica = contactableDireccionesConsumo.Id,
                            Politica = contactableDireccionesConsumo.Nombre,
                            ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgual, minimo),
                            ValorResultado = direccionesContacto.ToString(),
                            Valor = direccionesContacto.ToString(),
                            Parametro = minimo.ToString(),
                            ResultadoPolitica = resultadoComparacion,
                            FechaCreacion = DateTime.Now
                        });
                        if (contactableDireccionesConsumo.Excepcional && !resultadoComparacion)
                        {
                            observaciones.Add(contactableDireccionesConsumo.Nombre);
                            aprobacionAdicional = true;
                        }
                    }

                    minimo = 1;
                    resultadoComparacion = telefonosContacto >= minimo;

                    if (contactableTelefonosConsumo != null)
                    {
                        detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                        {
                            IdPolitica = contactableTelefonosConsumo.Id,
                            Politica = contactableTelefonosConsumo.Nombre,
                            ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgual, minimo),
                            ValorResultado = telefonosContacto.ToString(),
                            Valor = telefonosContacto.ToString(),
                            Parametro = minimo.ToString(),
                            ResultadoPolitica = resultadoComparacion,
                            FechaCreacion = DateTime.Now
                        });
                        if (contactableTelefonosConsumo.Excepcional && !resultadoComparacion)
                        {
                            observaciones.Add(contactableTelefonosConsumo.Nombre);
                            aprobacionAdicional = true;
                        }
                    }

                    minimo = 1;
                    resultadoComparacion = emailsContacto >= minimo;

                    if (contactableEmailsConsumo != null)
                    {
                        detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                        {
                            IdPolitica = contactableEmailsConsumo.Id,
                            Politica = contactableEmailsConsumo.Nombre,
                            ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MayorIgual, minimo),
                            ValorResultado = emailsContacto.ToString(),
                            Valor = emailsContacto.ToString(),
                            Parametro = minimo.ToString(),
                            ResultadoPolitica = resultadoComparacion,
                            FechaCreacion = DateTime.Now
                        });
                        if (contactableEmailsConsumo.Excepcional && !resultadoComparacion)
                        {
                            observaciones.Add(contactableEmailsConsumo.Nombre);
                            aprobacionAdicional = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Civil. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Civil

                #region Evaluación IESS
                try
                {
                    _logger.LogInformation("Procesando políticas IESS...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Iess && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var iess = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Persona>(detalleHistorial.Datos);

                        if (iess != null)
                        {
                            var mora = Convert.ToDouble(iess.Mora);
                            minimo = 50;

                            var resultadoComparacion = mora < minimo;

                            if (iessMoraPatronalConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = iessMoraPatronalConsumo.Id,
                                    Politica = iessMoraPatronalConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", mora.ToString()),
                                    Valor = mora.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (iessMoraPatronalConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(iessMoraPatronalConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                    _logger.LogInformation("Fin procesamiento políticas IESS.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de IESS. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación IESS

                #region Senescyt
                try
                {
                    _logger.LogInformation("Procesando políticas SENECYT...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var senescyt = JsonConvert.DeserializeObject<Externos.Logica.Senescyt.Modelos.Persona>(detalleHistorial.Datos);
                        minimo = 0;

                        var resultadoComparacion = senescyt.TotalTitulos > 0;

                        if (titulosSenescytConsumo != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = titulosSenescytConsumo.Id,
                                Politica = titulosSenescytConsumo.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Titulos, senescyt.TotalTitulos.ToString()),
                                Valor = senescyt.TotalTitulos.ToString(),
                                Parametro = 0.ToString(),
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (titulosSenescytConsumo.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(titulosSenescytConsumo.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }
                    _logger.LogInformation("Fin procesamiento políticas SENECYT.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SENECYT. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación Senescyt                

                #region Evaluación Ant
                try
                {
                    _logger.LogInformation("Procesando políticas ANT...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ant && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var fuenteAnt = JsonConvert.DeserializeObject<Externos.Logica.ANT.Modelos.Licencia>(detalleHistorial.Datos);
                        if (fuenteAnt != null)
                        {
                            minimo = 100;
                            var autos = fuenteAnt.Autos != null && fuenteAnt.Autos.Any() ? fuenteAnt.Autos.Where(m => (!string.IsNullOrEmpty(m.Placa) && m.Placa.Length > 1 && m.Placa != "SIN/PLACA") && (string.IsNullOrEmpty(m.NombrePropietario) || m.NombrePropietario == fuenteAnt.Titular)).ToList() : new List<Externos.Logica.ANT.Modelos.Auto>();
                            if (fuenteAnt.Multas != null && fuenteAnt.Multas.Any())
                                fuenteAnt.Multas = fuenteAnt.Multas.Where(m => autos.Select(m => m.Placa).Contains(m.Placa) || m.Placa == "-" || m.Placa == "SIN/PLACA" || m.Placa.Length == 1 || m.Placa?.Trim() == string.Empty).ToList();

                            var totalMultas = fuenteAnt.Multas.Sum(x => (!x.Pagada.HasValue || (x.Pagada.HasValue && !x.Pagada.Value)) && (!x.Reclamo.HasValue || (x.Reclamo.HasValue && !x.Reclamo.Value))
                                              && (!x.Anulada.HasValue || (x.Anulada.HasValue && !x.Anulada.Value)) ? x.Saldo : 0);
                            var resultadoComparacion = totalMultas < minimo;
                            if (pagoMultasPendientesConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = pagoMultasPendientesConsumo.Id,
                                    Politica = pagoMultasPendientesConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MenorIgualMoneda, minimo),
                                    ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", totalMultas.ToString("N", culture)),
                                    Valor = totalMultas.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (pagoMultasPendientesConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(pagoMultasPendientesConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }

                            if (!string.IsNullOrEmpty(tipoIdentificacion) && (tipoIdentificacion == Dominio.Constantes.General.RucNatural || tipoIdentificacion == Dominio.Constantes.General.Cedula))
                            {
                                minimo = 0;
                                resultadoComparacion = autos.Count > minimo;

                                if (vehiculosConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = vehiculosConsumo.Id,
                                        Politica = vehiculosConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, autos.Count.ToString()),
                                        Valor = autos.Count.ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (vehiculosConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(vehiculosConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de ANT. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación Ant

                #region Evaluación SERCOP
                try
                {
                    _logger.LogInformation("Procesando políticas SERCOP...");
                    int? contadorSercop = null;
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Proveedor && m.Generado);

                    if (detalleHistorial != null)
                    {
                        var proveedor = JsonConvert.DeserializeObject<Externos.Logica.SERCOP.Modelos.ProveedorIncumplido>(detalleHistorial.Datos);
                        contadorSercop = 0;
                        if (proveedor.ProveedoresIncop != null && proveedor.ProveedoresIncop.Any()) contadorSercop += proveedor.ProveedoresIncop.Count;
                        if (proveedor.ProveedoresContraloria != null && proveedor.ProveedoresContraloria.Any()) contadorSercop += proveedor.ProveedoresContraloria.Count;
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.ProveedorContraloria && m.Generado);

                    if (detalleHistorial != null)
                    {
                        if (contadorSercop == null) contadorSercop = 0;
                        var proveedorContraloria = JsonConvert.DeserializeObject<List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria>>(detalleHistorial.Datos);
                        if (proveedorContraloria != null && proveedorContraloria.Any()) contadorSercop += proveedorContraloria.Count;
                    }
                    if (contadorSercop == null) contadorSercop = 0;

                    if (contadorSercop != null)
                    {
                        minimo = 0;
                        var resultadoComparacion = contadorSercop == minimo;

                        if (sercopConsumo != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = sercopConsumo.Id,
                                Politica = sercopConsumo.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Maximo, minimo),
                                ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Procesos, contadorSercop),
                                Valor = contadorSercop.ToString(),
                                Parametro = minimo.ToString(),
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });
                            if (sercopConsumo.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(sercopConsumo.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }
                    _logger.LogInformation("Fin procesamiento políticas SERCOP.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SERCOP. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Evaluación SERCOP

                #region PensionAlimenticia
                try
                {
                    _logger.LogInformation("Procesando políticas Pensión Alimenticia...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia && m.Generado);
                    var resultadoComparacion = false;

                    if (detalleHistorial != null)
                    {
                        var pensionAlimenticia = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(detalleHistorial.Datos);
                        double totalDeudaAlimenticia = 0;
                        minimo = 0;

                        if (pensionAlimenticia != null && pensionAlimenticia.Resultados != null && pensionAlimenticia.Resultados.Any())
                        {
                            var valorDeudaPension = new List<List<Externos.Logica.PensionesAlimenticias.Modelos.Movimiento>>();
                            var resultadoPension = pensionAlimenticia.Resultados;
                            var nombreHistorial = resultadoPension.FirstOrDefault().Nombre;
                            var movimientosPension = resultadoPension.Select(x => new { x.Movimientos, x.Intervinientes }).ToList();
                            if (movimientosPension != null && movimientosPension.Any() && !string.IsNullOrEmpty(nombreHistorial))
                            {
                                var nombreDivido = nombreHistorial.Split(' ');
                                foreach (var item in movimientosPension)
                                {
                                    var obligadoNombre = item.Intervinientes.Where(x => x.Tipo.ToUpper() == "OBLIGADO PRINCIPAL").Select(x => x.Nombre).ToList();
                                    if (obligadoNombre != null && obligadoNombre.Any())
                                    {
                                        var listaNombre = new List<bool>();
                                        foreach (var item1 in obligadoNombre)
                                        {
                                            var nombreSeparado = item1.Split(' ');
                                            listaNombre.Clear();
                                            foreach (var item2 in nombreSeparado)
                                            {
                                                if (nombreHistorial.Contains(item2))
                                                    listaNombre.Add(true);
                                                else
                                                    listaNombre.Add(false);
                                            }
                                            if (listaNombre.Count(x => x) == nombreDivido.Length)
                                                valorDeudaPension.Add(item.Movimientos);
                                        }
                                    }
                                }
                            }

                            if (valorDeudaPension != null && valorDeudaPension.Any())
                            {
                                totalDeudaAlimenticia = valorDeudaPension.Sum(x => x.Sum(m => double.Parse(!string.IsNullOrEmpty(m.ValorDeudaOriginal) ? m.ValorDeudaOriginal.Replace(",", ".") : "0"))) - valorDeudaPension.Sum(x => x.Sum(m => double.Parse(!string.IsNullOrEmpty(m.ValorPagadoOriginal) ? m.ValorPagadoOriginal.Replace(",", ".") : "0")));
                                resultadoComparacion = totalDeudaAlimenticia <= minimo;
                                if (pagoPendientePensionAlimenticiaConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = pagoPendientePensionAlimenticiaConsumo.Id,
                                        Politica = pagoPendientePensionAlimenticiaConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MaximoMoneda, minimo),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", totalDeudaAlimenticia.ToString("N", culture)),
                                        Valor = totalDeudaAlimenticia.ToString("N", culture),
                                        Parametro = minimo.ToString("N", culture),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });

                                    if (pagoPendientePensionAlimenticiaConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(pagoPendientePensionAlimenticiaConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = true;
                                if (pagoPendientePensionAlimenticiaConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = pagoPendientePensionAlimenticiaConsumo.Id,
                                        Politica = pagoPendientePensionAlimenticiaConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MaximoMoneda, "0.00"),
                                        ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", "0.00"),
                                        Valor = "0.00",
                                        Parametro = "0.00",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });

                                    if (pagoPendientePensionAlimenticiaConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(pagoPendientePensionAlimenticiaConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        resultadoComparacion = true;
                        if (pagoPendientePensionAlimenticiaConsumo != null)
                        {
                            detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                            {
                                IdPolitica = pagoPendientePensionAlimenticiaConsumo.Id,
                                Politica = pagoPendientePensionAlimenticiaConsumo.Nombre,
                                ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.MaximoMoneda, "0.00"),
                                ValorResultado = string.Format(System.Globalization.CultureInfo.InvariantCulture, "${0:0,0.00}", "0.00"),
                                Valor = "0.00",
                                Parametro = "0.00",
                                ResultadoPolitica = resultadoComparacion,
                                FechaCreacion = DateTime.Now
                            });

                            if (pagoPendientePensionAlimenticiaConsumo.Excepcional && !resultadoComparacion)
                            {
                                observaciones.Add(pagoPendientePensionAlimenticiaConsumo.Nombre);
                                aprobacionAdicional = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Pensión Alimenticia. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region SuperintendenciaBancos
                try
                {
                    _logger.LogInformation("Procesando políticas SuperintendenciaBancos...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var superBancos = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Datos);
                        if (superBancos != null)
                        {
                            var estadoBancos = superBancos.Estado;
                            if (!string.IsNullOrEmpty(estadoBancos))
                            {
                                var resultadoComparacion = estadoBancos.ToUpper().Equals("HABILITADO");
                                if (superintendenciaBancosCedulaConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = superintendenciaBancosCedulaConsumo.Id,
                                        Politica = superintendenciaBancosCedulaConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.EstadoSuperBancos, "HABILITADO"),
                                        ValorResultado = estadoBancos.ToUpper(),
                                        Valor = estadoBancos.ToUpper(),
                                        Parametro = "HABILITADO",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (superintendenciaBancosCedulaConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(superintendenciaBancosCedulaConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }

                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var superBancosRucNatural = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Datos);
                        if (superBancosRucNatural != null)
                        {
                            var estadoBancos = superBancosRucNatural.Estado;
                            if (!string.IsNullOrEmpty(estadoBancos))
                            {
                                var resultadoComparacion = estadoBancos.ToUpper().Equals("HABILITADO");
                                if (superintendenciaBancosRucNaturalConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = superintendenciaBancosRucNaturalConsumo.Id,
                                        Politica = superintendenciaBancosRucNaturalConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.EstadoSuperBancos, "HABILITADO"),
                                        ValorResultado = estadoBancos.ToUpper(),
                                        Valor = estadoBancos.ToUpper(),
                                        Parametro = "HABILITADO",
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (superintendenciaBancosRucNaturalConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(superintendenciaBancosRucNaturalConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de SuperintendenciaBancos. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region AntecedentesPenales
                try
                {
                    _logger.LogInformation("Procesando políticas Antecedentes Penales...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.AntecedentesPenales && m.Generado);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos))
                    {
                        var antecedentes = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.Resultado>(detalleHistorial.Datos);
                        if (antecedentes != null && !string.IsNullOrEmpty(antecedentes.Antecedente))
                        {
                            var resultadoComparacion = !antecedentes.Antecedente.ToUpper().Equals("SI");
                            if (antecedentesPenalesConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = antecedentesPenalesConsumo.Id,
                                    Politica = antecedentesPenalesConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.AntecedentesPenales, "NO"),
                                    ValorResultado = antecedentes.Antecedente.ToUpper(),
                                    Valor = antecedentes.Antecedente.ToUpper(),
                                    Parametro = "NO",
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (antecedentesPenalesConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(antecedentesPenalesConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Antecedentes Penales. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region Predios
                try
                {
                    if (!string.IsNullOrEmpty(tipoIdentificacion) && (tipoIdentificacion == Dominio.Constantes.General.RucNatural || tipoIdentificacion == Dominio.Constantes.General.Cedula))
                    {
                        _logger.LogInformation("Procesando políticas Predios...");
                        var datosPredioQuito = new Externos.Logica.PredioMunicipio.Modelos.Resultado();
                        var datosPredioCuenca = new Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca();
                        var datosPredioStoDomingo = new Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo();
                        var datosPredioRuminahui = new Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui();
                        var datosPredioQuininde = new Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde();
                        var cantidadPropiedades = 0;
                        var prediosCuenca = new List<string>();
                        var prediosQuininde = new List<string>();
                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioQuito = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioCuenca = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioStoDomingo = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioRuminahui = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(detalleHistorial.Datos);

                        detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                        {
                            Datos = m.Data
                        }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde && m.Generado);

                        if (detalleHistorial != null)
                            datosPredioQuininde = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(detalleHistorial.Datos);

                        if (datosPredioQuito != null && datosPredioQuito.Detalle != null && datosPredioQuito.Detalle.Any())
                            cantidadPropiedades += datosPredioQuito.Detalle.Where(m => m.Concepto.ToUpper().Contains("PREDIO") || m.Concepto.ToUpper().Contains("PREDIAL") || m.Concepto.ToUpper().Contains("CEM")).GroupBy(m => m.Numero).Count();

                        if (datosPredioCuenca != null && datosPredioCuenca.ValoresPendientes != null && datosPredioCuenca.ValoresPendientes.Any())
                            prediosCuenca.AddRange(datosPredioCuenca.ValoresPendientes.GroupBy(m => m.ClaveCatastral).Select(m => m.ToString()).ToList());

                        if (datosPredioCuenca != null && datosPredioCuenca.ValoresCancelados != null && datosPredioCuenca.ValoresCancelados.Any())
                            prediosCuenca.AddRange(datosPredioCuenca.ValoresCancelados.GroupBy(m => m.Clave).Select(m => m.ToString()).ToList());

                        cantidadPropiedades += prediosCuenca.GroupBy(m => m).Count();

                        if (datosPredioStoDomingo != null && datosPredioStoDomingo.Detalle != null && datosPredioStoDomingo.Detalle.Any())
                            cantidadPropiedades += datosPredioStoDomingo.Detalle.GroupBy(m => m.Clave).Count();

                        if (datosPredioRuminahui != null && datosPredioRuminahui.Detalle != null && datosPredioRuminahui.Detalle.Any())
                            cantidadPropiedades += datosPredioRuminahui.Detalle.GroupBy(m => m.ClaveCatastral).Count();

                        if (datosPredioQuininde != null && datosPredioQuininde.ValoresPendientes != null && datosPredioQuininde.ValoresPendientes.Any())
                            prediosQuininde.AddRange(datosPredioQuininde.ValoresPendientes.SelectMany(x => x.InformacionPredio.Select(m => m.Clave)).ToList());

                        if (datosPredioQuininde != null && datosPredioQuininde.ValoresCancelados != null && datosPredioQuininde.ValoresCancelados.Any())
                            prediosQuininde.AddRange(datosPredioQuininde.ValoresCancelados.SelectMany(x => x.InformacionPredio.Select(m => m.Clave)).ToList());

                        cantidadPropiedades += prediosQuininde.GroupBy(m => m).Count();

                        if (cantidadPropiedades > 0)
                        {
                            minimo = 0;
                            var resultadoComparacion = cantidadPropiedades > minimo;
                            if (propiedadesConsumo != null)
                            {
                                detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                {
                                    IdPolitica = propiedadesConsumo.Id,
                                    Politica = propiedadesConsumo.Nombre,
                                    ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.Mayor, minimo),
                                    ValorResultado = string.Format(Dominio.Constantes.TextoReferencia.Predios, cantidadPropiedades.ToString()),
                                    Valor = cantidadPropiedades.ToString(),
                                    Parametro = minimo.ToString(),
                                    ResultadoPolitica = resultadoComparacion,
                                    FechaCreacion = DateTime.Now
                                });
                                if (propiedadesConsumo.Excepcional && !resultadoComparacion)
                                {
                                    observaciones.Add(propiedadesConsumo.Nombre);
                                    aprobacionAdicional = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Predios. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region NoticiaDelito
                try
                {
                    _logger.LogInformation("Procesando políticas Noticias del Delito...");
                    detalleHistorial = await _detallesHistorial.FirstOrDefaultAsync(m => new DatosJsonViewModel
                    {
                        Datos = m.Data
                    }, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosPersona && m.Generado);
                    var entidad = await _historiales.FirstOrDefaultAsync(m => new { m.IdentificacionSecundaria, m.NombresPersona }, m => m.Id == modelo.IdHistorial, null);
                    if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Datos) && entidad != null)
                    {
                        var delitoNoticia = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(detalleHistorial.Datos);
                        if (delitoNoticia != null)
                        {
                            minimo = 0;
                            var resultadoComparacion = false;
                            var cantidadSujeto = new List<string>();
                            var sujetos = delitoNoticia.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Contains("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                            if (sujetos != null && sujetos.Any())
                            {
                                var nombreDivido = entidad.NombresPersona?.Split(' ');
                                var listaNombre = new List<bool>();

                                foreach (var item1 in sujetos.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                                {
                                    if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Contains("SOSPECHOSO"))
                                    {
                                        if (!string.IsNullOrEmpty(item1.Cedula) && !string.IsNullOrEmpty(entidad.IdentificacionSecundaria) && entidad.IdentificacionSecundaria == item1.Cedula)
                                            cantidadSujeto.Add(item1.Numero);
                                        else
                                        {
                                            var nombreSeparado = item1.NombresCompletos.Split(' ');
                                            listaNombre.Clear();
                                            foreach (var item2 in nombreSeparado)
                                            {
                                                if (!string.IsNullOrEmpty(entidad.NombresPersona) && entidad.NombresPersona.Contains(item2))
                                                    listaNombre.Add(true);
                                                else
                                                    listaNombre.Add(false);
                                            }
                                            if (nombreDivido != null && nombreDivido.Any() && listaNombre.Count(x => x) == nombreDivido.Length)
                                                cantidadSujeto.Add(item1.Numero);
                                        }
                                    }
                                }
                                cantidadSujeto = cantidadSujeto.Distinct().ToList();
                            }

                            if (cantidadSujeto.Count() > 0)
                            {
                                resultadoComparacion = minimo > cantidadSujeto.Count();
                                if (noticiasDelitoConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = noticiasDelitoConsumo.Id,
                                        Politica = noticiasDelitoConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.NoticiaDelito, minimo.ToString()),
                                        ValorResultado = cantidadSujeto.Count().ToString(),
                                        Valor = cantidadSujeto.Count().ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (noticiasDelitoConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(noticiasDelitoConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                            else
                            {
                                resultadoComparacion = true;
                                if (noticiasDelitoConsumo != null)
                                {
                                    detalleCalificacion.Add(new DetalleCalificacionApiMetodoViewModel()
                                    {
                                        IdPolitica = noticiasDelitoConsumo.Id,
                                        Politica = noticiasDelitoConsumo.Nombre,
                                        ReferenciaMinima = string.Format(Dominio.Constantes.TextoReferencia.NoticiaDelito, minimo.ToString()),
                                        ValorResultado = cantidadSujeto.Count().ToString(),
                                        Valor = cantidadSujeto.Count().ToString(),
                                        Parametro = minimo.ToString(),
                                        ResultadoPolitica = resultadoComparacion,
                                        FechaCreacion = DateTime.Now
                                    });
                                    if (noticiasDelitoConsumo.Excepcional && !resultadoComparacion)
                                    {
                                        observaciones.Add(noticiasDelitoConsumo.Nombre);
                                        aprobacionAdicional = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar políticas de Noticias del Delito. {ex.Message}");
                    _logger.LogError(ex, ex.Message);
                }
                #endregion

                #region Procesamiento Calificación de Evaluación
                if (detalleCalificacion != null && detalleCalificacion.Any())
                {
                    datosPersona.TipoCalificacion = Dominio.Tipos.TiposCalificaciones.Evaluacion;
                    datosPersona.TotalValidados = detalleCalificacion.Count;
                    datosPersona.TotalAprobados = detalleCalificacion.Count(m => m.ResultadoPolitica);
                    datosPersona.TotalRechazados = detalleCalificacion.Count(m => !m.ResultadoPolitica);

                    if (datosPersona.TotalValidados > 0) datosPersona.Calificacion = Math.Round((decimal)datosPersona.TotalAprobados * 100 / datosPersona.TotalValidados, 2, MidpointRounding.AwayFromZero);
                    datosPersona.Aprobado = datosPersona.Calificacion >= Dominio.Constantes.ConstantesCalificacion.MinimoCalificacion;
                    datosPersona.DetalleCalificacion = detalleCalificacion;

                    if (aprobacionAdicional)
                    {
                        datosPersona.Observaciones = string.Join(". ", observaciones);
                        datosPersona.Aprobado = false;
                    }

                    var calificacionesDetalle = datosPersona.DetalleCalificacion.Select(m => new DetalleCalificacion
                    {
                        IdPolitica = m.IdPolitica,
                        Valor = m.Valor,
                        Parametro = m.Parametro,
                        Aprobado = m.ResultadoPolitica,
                        Datos = m.ValorResultado,
                        ReferenciaMinima = m.ReferenciaMinima,
                        UsuarioCreacion = modelo.IdUsuario,
                        Observacion = m.Observacion,
                        FechaCorte = m.FechaCorte,
                        FechaCreacion = m.FechaCreacion
                    }).ToList();

                    datosPersona.IdCalificacion = await _calificaciones.GuardarCalificacionAsync(new Calificacion()
                    {
                        IdHistorial = modelo.IdHistorial,
                        Puntaje = datosPersona.Calificacion,
                        Aprobado = datosPersona.Aprobado,
                        NumeroAprobados = datosPersona.TotalAprobados,
                        NumeroRechazados = datosPersona.TotalRechazados,
                        TotalVerificados = datosPersona.TotalValidados,
                        Observaciones = datosPersona.Observaciones,
                        UsuarioCreacion = modelo.IdUsuario,
                        TipoCalificacion = datosPersona.TipoCalificacion,
                        DetalleCalificacion = calificacionesDetalle,
                        FechaCreacion = DateTime.Now,
                        CalificacionPersonalizada = true
                    });
                    datosPersonaLista.Add(datosPersona);
                }
                if ((detalleCalificacion != null && detalleCalificacion.Any()))
                {
                    var historialAnterior = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null);
                    var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial, null);
                    historialAnterior.IdPlanEvaluacion = dataPlanEvaluacion.Id;
                    await _historiales.UpdateAsync(historialAnterior);
                    if (historialConsolidado != null)
                    {
                        var historialEvaluacionBuro = await _calificaciones.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoCalificacion == TiposCalificaciones.Buro, null);
                        if (historialEvaluacionBuro != null)
                            historialConsolidado.AprobadoEvaluacion = datosPersona.Aprobado && historialEvaluacionBuro.Aprobado;
                        else
                            historialConsolidado.AprobadoEvaluacion = datosPersona.Aprobado;

                        historialConsolidado.ConsultaEvaluacion = historialAnterior.IdPlanEvaluacion.HasValue && historialAnterior.IdPlanEvaluacion.Value > 0;
                        await _reporteConsolidado.UpdateAsync(historialConsolidado);
                    }
                }
                #endregion Procesamiento Calificación de Evaluación

                _logger.LogInformation($"Fin de procesamiento RUC Jurídico {modelo.IdHistorial}");
                return datosPersonaLista;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        #endregion Evaluacion
    }
}
