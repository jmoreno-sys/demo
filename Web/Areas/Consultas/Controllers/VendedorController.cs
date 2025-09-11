using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
using System.Text.RegularExpressions;
using Web.Areas.Consultas.Models;
using Dominio.Entidades.Balances;
using Persistencia.Repositorios.Balance;
using Microsoft.AspNetCore.Identity;
using Persistencia.Repositorios.Identidad;
using Microsoft.EntityFrameworkCore;
using Infraestructura.Servicios;
using Microsoft.AspNetCore.Http.Extensions;
using Persistencia.Migraciones.Principal;

namespace Web.Areas.Consultas.Controllers
{
    [Area("Consultas")]
    [Route("Consultas/Vendedor")]
    [Authorize(Policy = "Consultas")]
    public class VendedorController : Controller
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
        private readonly Externos.Logica.FiscaliaDelitos.Controlador _fiscaliaDelitos;
        private readonly Externos.Logica.Equifax.Controlador _buroCreditoEquifax;
        private readonly Externos.Logica.SuperBancos.Controlador _superBancos;
        private readonly Externos.Logica.AntecedentesPenales.Controlador _antecedentes;
        private readonly Externos.Logica.PredioMunicipio.Controlador _predios;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detallesHistorial;
        private readonly IUsuarios _usuarios;
        private readonly IConsultaService _consulta;
        private readonly ICalificaciones _calificaciones;
        private readonly IPlanesBuroCredito _planesBuroCredito;
        private readonly IAccesos _accesos;
        private readonly ICredencialesBuro _credencialesBuro;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly IEmailService _emailSender;
        private readonly IReportesConsolidados _reporteConsolidado;
        private bool _cache = false;

        public VendedorController(IConfiguration configuration,
            ILoggerFactory loggerFactory,
            Externos.Logica.SRi.Controlador sri,
            Externos.Logica.Balances.Controlador balances,
            Externos.Logica.IESS.Controlador iess,
            Externos.Logica.FJudicial.Controlador fjudicial,
            Externos.Logica.ANT.Controlador ant,
            Externos.Logica.PensionesAlimenticias.Controlador pension,
            Externos.Logica.Garancheck.Controlador garancheck,
            Externos.Logica.SERCOP.Controlador sercop,
            Externos.Logica.BuroCredito.Controlador burocredito,
            Externos.Logica.FiscaliaDelitos.Controlador fiscaliaDelitos,
            Externos.Logica.Equifax.Controlador buroCreditoEquifax,
            Externos.Logica.SuperBancos.Controlador superBancos,
            Externos.Logica.AntecedentesPenales.Controlador antecedentes,
            Externos.Logica.PredioMunicipio.Controlador predios,
            IHistoriales historiales,
            IDetallesHistorial detallehistoriales,
            IUsuarios usuarios,
            IConsultaService consulta,
            ICalificaciones calificaciones,
            IPlanesBuroCredito planesBuroCredito,
            IPlanesEvaluaciones planesEvaluaciones,
            IAccesos accesos,
            ICredencialesBuro credencialesBuro,
            IEmailService emailSender,
            IReportesConsolidados reportesConsolidados)
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
            _fiscaliaDelitos = fiscaliaDelitos;
            _buroCreditoEquifax = buroCreditoEquifax;
            _superBancos = superBancos;
            _antecedentes = antecedentes;
            _predios = predios;
            _historiales = historiales;
            _detallesHistorial = detallehistoriales;
            _usuarios = usuarios;
            _consulta = consulta;
            _calificaciones = calificaciones;
            _planesBuroCredito = planesBuroCredito;
            _accesos = accesos;
            _credencialesBuro = credencialesBuro;
            _planesEvaluaciones = planesEvaluaciones;
            _emailSender = emailSender;
            _reporteConsolidado = reportesConsolidados;
            _cache = _configuration.GetSection("AppSettings:Consultas:Cache").Get<bool>();
        }

        #region Historiales
        [HttpPost]
        [Route("GuardarHistorial")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarHistorial(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han ingresado los datos de la consulta.");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("La identificación ingresada no es válida.");

                var idUsuario = User.GetUserId<int>();
                var identificacionOriginal = modelo.Identificacion?.Trim();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (!usuarioActual.Empresa.PlanesEmpresas.Any(m => m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo))
                    throw new Exception("No es posible realizar esta consulta ya que no tiene planes activos vigentes.");

                var dataPlanEvaluacion = await _planesEvaluaciones.FirstOrDefaultAsync(s => s, s => s.IdEmpresa == usuarioActual.IdEmpresa && s.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo);
                if (dataPlanEvaluacion == null)
                    throw new Exception("No se encontró un plan de evaluación Activo.");

                var dataUsuario = await _accesos.AnyAsync(s => s.IdUsuario == idUsuario && s.Estado == Dominio.Tipos.EstadosAccesos.Activo && s.Acceso == Dominio.Tipos.TiposAccesos.Evaluacion);
                if (!dataUsuario)
                    throw new Exception("El Usuario no tiene permisos para la Evaluación.");

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
                _logger.LogInformation($"Procesando historial de usuario: {idUsuario}. Identificación: {identificacionOriginal}. IP: {ip}.");

                var periodoTemp = 0;
                if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                {
                    periodoTemp = 1;
                    parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { periodoTemp } });
                    var infoPeriodos = _configuration.GetSection("AppSettings:PeriodosDinamicos").Get<PeriodosDinamicosViewModel>();
                    if (infoPeriodos != null)
                    {
                        var ultimosPeriodos = infoPeriodos.Periodos.Select(m => m.Valor).ToList();
                        parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = ultimosPeriodos });
                    }
                }
                else
                    parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { periodoTemp } });

                var habilitarBuro = usuarioActual.Empresa.PlanesBuroCredito.Any(m => m.NumeroMaximoConsultas > 0 && m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);

                _logger.LogInformation("Registrando historial de usuarios en base de datos");
                var idHistorial = await _historiales.GuardarHistorialAsync(new Historial()
                {
                    IdUsuario = idUsuario,
                    DireccionIp = ip?.Trim().ToUpper(),
                    Identificacion = modelo.Identificacion?.Trim().ToUpper(),
                    Periodo = periodoTemp,
                    Fecha = DateTime.Now,
                    TipoConsulta = Dominio.Tipos.Consultas.Web,
                    ParametrosBusqueda = parametros,
                    IdPlanEmpresa = idPlan,
                    TipoIdentificacion = tipoIdentificacion
                });
                _logger.LogInformation($"Registro de historial exitoso. Id Historial: {idHistorial}");

                var idReporteConsolidado = await _reporteConsolidado.GuardarReporteConsolidadoAsync(new Dominio.Entidades.Balances.ReporteConsolidado()
                {
                    IdUsuario = idUsuario,
                    DireccionIp = ip?.Trim().ToUpper(),
                    Identificacion = modelo.Identificacion?.Trim().ToUpper(),
                    TipoIdentificacion = tipoIdentificacion,
                    TipoConsulta = Dominio.Tipos.Consultas.Web,
                    ParametrosBusqueda = parametros,
                    Fecha = DateTime.Now,
                    NombreUsuario = usuarioActual.NombreCompleto,
                    IdEmpresa = usuarioActual.IdEmpresa,
                    NombreEmpresa = usuarioActual.Empresa.RazonSocial,
                    IdentificacionEmpresa = usuarioActual.Empresa.Identificacion,
                    HistorialId = idHistorial
                });
                _logger.LogInformation($"Registro de Reporte consolidado exitoso. Id Historial: {idReporteConsolidado}");

                await ObtenerInformacionVendedor(new ReporteViewModel()
                {
                    IdHistorial = idHistorial,
                    Identificacion = identificacionOriginal
                });

                return Json(new { idHistorial, tipoIdentificacion = ValidacionViewModel.ObtenerTipoIdentificacion(identificacionOriginal), habilitarBuro, usuarioActual.IdEmpresa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(VendedorController), StatusCodes.Status500InternalServerError);
            }
        }

        private async Task ObtenerInformacionVendedor(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                await ObtenerReporteSRI(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteCivil(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteBalance(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteIESS(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteSenescyt(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteLegal(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteANT(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteSERCOP(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReportePensionAlimenticia(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                //await ObtenerReporteSuperBancos(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
                await ObtenerReporteBuroCredito(new ReporteViewModel() { Identificacion = modelo.Identificacion, IdHistorial = modelo.IdHistorial });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Vendedor {ex.Message}");
                throw;
            }
        }
        #endregion Historiales

        #region Fuentes
        private async Task ObtenerReporteSRI(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Historial historialTemp = null;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                var cacheSri = false;
                var cedulaEntidades = false;
                var impuestosRenta = new List<Externos.Logica.SRi.Modelos.Anexo>();
                historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);

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
                                r_sri = await _sri.GetContribuyenteSriVendedor(modelo.Identificacion);
                            else
                                r_sri = await _sri.GetContribuyenteSriVendedor(modelo.Identificacion);

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                            {
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);
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
                }
                if (r_sri != null && string.IsNullOrEmpty(r_sri.RUC))
                {
                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.Identificacion))
                        r_sri.RUC = historialTemp.Identificacion;
                }

                var datos = new SRIViewModel()
                {
                    Sri = r_sri
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
                            Generado = datos.Sri != null,
                            Data = datos.Sri != null ? JsonConvert.SerializeObject(datos.Sri) : null,
                            Cache = cacheSri,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteCivil(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.Garancheck.Modelos.Persona r_garancheck = null;
                Externos.Logica.Garancheck.Modelos.Personal datosPersonal = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactos = null;
                Externos.Logica.Garancheck.Modelos.RegistroCivil registroCivil = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosIess = null;
                var datos = new CivilViewModel();
                Historial historialTemp = null;
                var cedulaEntidades = false;
                var cacheCivil = false;
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
                                            break;
                                        case 2:
                                            r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);
                                            break;
                                        case 3:
                                            registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(modelo.Identificacion);
                                            if (registroCivil != null)
                                                cacheCivil = true;
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
                                            break;
                                        default:
                                            r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);
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
                                            datosPersonal = await _garancheck.GetInformacionPersonalAsync(modelo.Identificacion);

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
                                                    break;
                                                case 2:
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    break;
                                                case 3:
                                                    registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                    if (registroCivil != null)
                                                        cacheCivil = true;
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
                                                    break;
                                                default:
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
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
                                                break;
                                            case 2:
                                                r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);
                                                break;
                                            case 3:
                                                registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(cedulaTemp);
                                                if (registroCivil != null)
                                                    cacheCivil = true;
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
                                                break;
                                            default:
                                                r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        //if (r_garancheck == null)
                        //{
                        //    var datosDetalleGarancheck = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleGarancheck != null)
                        //    {
                        //        cacheCivil = true;
                        //        r_garancheck = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(datosDetalleGarancheck);
                        //    }
                        //}
                        //if (contactos == null)
                        //{
                        //    var datosDetalleContactos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Contactos && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleContactos != null)
                        //    {
                        //        cacheCivil = true;
                        //        contactos = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContactos);
                        //    }
                        //}
                        //if (contactosIess == null)
                        //{
                        //    var datosDetalleContactosIess = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleContactosIess != null)
                        //    {
                        //        cacheCivil = true;
                        //        contactosIess = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContactosIess);
                        //    }
                        //}
                        //if (registroCivil == null)
                        //{
                        //    var datosDetalleRegistroCivil = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetalleRegistroCivil != null)
                        //    {
                        //        cacheCivil = true;
                        //        registroCivil = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(datosDetalleRegistroCivil);
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Civil con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                    datos = new CivilViewModel()
                    {
                        Ciudadano = r_garancheck,
                        Personales = datosPersonal,
                        Contactos = contactos,
                        RegistroCivil = registroCivil
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                    datos = JsonConvert.DeserializeObject<CivilViewModel>(System.IO.File.ReadAllText(pathCivil));
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
                            Generado = datos.Ciudadano != null,
                            Data = datos.Ciudadano != null ? JsonConvert.SerializeObject(datos.Ciudadano) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Personales,
                            Generado = datos.Personales != null,
                            Data = datos.Personales != null ? JsonConvert.SerializeObject(datos.Personales) : null,
                            Cache = false,
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
                            Reintento = false
                        });

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
                            TipoFuente = Dominio.Tipos.Fuentes.ContactosIess,
                            Generado = datos.ContactosIess != null,
                            Data = datos.ContactosIess != null ? JsonConvert.SerializeObject(datos.ContactosIess) : null,
                            Cache = cacheCivil,
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteBalance(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var datos = new BalancesViewModel();
                var periodosBusqueda = new List<int>();
                var cacheBalance = false;
                var balancesMultiples = false;
                var soloEmpresasSimilares = false;
                var busquedaNoJuridico = false;
                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.Balances.Modelos.BalanceEmpresa r_balance = null;
                List<Externos.Logica.Balances.Modelos.BalanceEmpresa> r_balances = null;
                Externos.Logica.Balances.Modelos.DirectorioCompania directorioCompania = null;
                Externos.Logica.Balances.Modelos.IndicadoresCompania indicadoresCompania = null;

                if (!_cache)
                {
                    var historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Balances identificación: {modelo.Identificacion}");

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
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
                            }
                            _logger.LogInformation($"Procesando Empresas Representante identificación: {modelo.Identificacion}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Balances con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    if (directorioCompania == null && !busquedaNoJuridico) //ver si se borra
                    {
                        var datosDetalleDirectorio = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.DirectorioCompanias && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleDirectorio != null)
                        {
                            cacheBalance = true;
                            directorioCompania = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.DirectorioCompania>(datosDetalleDirectorio);
                        }
                    }

                    if (r_balances == null && !busquedaNoJuridico)//ver si se borra
                    {
                        var datosDetalleBalances = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Balances && m.Historial.Periodo == 1 && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        if (datosDetalleBalances != null)
                        {
                            cacheBalance = true;
                            r_balances = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.BalanceEmpresa>>(datosDetalleBalances);
                        }
                    }

                    datos = new BalancesViewModel()
                    {
                        HistorialCabecera = historialTemp,
                        Balance = r_balance,
                        Balances = r_balances,
                        MultiplesPeriodos = balancesMultiples,
                        PeriodoBusqueda = historialTemp.Periodo.Value,
                        PeriodosBusqueda = periodosBusqueda,
                        SoloEmpresasSimilares = soloEmpresasSimilares,
                        DirectorioCompania = directorioCompania,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathBalances = Path.Combine(pathFuentes, ValidacionViewModel.ValidarCedula(identificacionOriginal) ? "balancesPersonaDemo.json" : "balancesDemo.json");
                    datos = JsonConvert.DeserializeObject<BalancesViewModel>(System.IO.File.ReadAllText(pathBalances));
                }

                _logger.LogInformation("Fuente de Balances procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Balances. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Balances,
                            Generado = datos.Balances != null && datos.Balances.Any(),
                            Data = datos.Balances != null && datos.Balances.Any() ? JsonConvert.SerializeObject(datos.Balances) : null,
                            Cache = cacheBalance,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
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
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Directorio Compañias procesado correctamente");

                        if (historial != null)
                        {
                            var parametros = string.Empty;
                            var periodoTemp = 0;
                            if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                            {
                                periodoTemp = 1;
                                if (r_balances != null)
                                {
                                    var ultimosPeriodos = r_balances.Select(m => m.Periodo).ToList();
                                    parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = ultimosPeriodos });
                                }
                            }
                            else
                                parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { periodoTemp } });

                            historial.Periodo = periodoTemp;
                            if (!string.IsNullOrEmpty(parametros))
                                historial.ParametrosBusqueda = parametros;

                            if (directorioCompania != null && !string.IsNullOrEmpty(directorioCompania.Representante?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                                historial.NombresPersona = directorioCompania.Representante.Trim().ToUpper();

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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteIESS(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

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

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.IESS.Modelos.Persona r_iess = null;
                Externos.Logica.IESS.Modelos.Afiliacion r_afiliacion = null;
                Externos.Logica.IESS.Modelos.ResultadoAfiliacion r_afiliacionV2 = null;
                List<Externos.Logica.IESS.Modelos.Afiliado> r_afiliado = null;
                Externos.Logica.IESS.Modelos.ResultadoPersona resultadoIess = null;
                var datos = new IessViewModel();
                Historial historialTemp = null;
                var cacheIess = false;
                var cacheAfiliado = false;
                var cacheAfiliadoAdicional = false;
                var cedulaEntidades = false;

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
                                if (tipoFuente != 5)
                                {
                                    resultadoIess = await _iess.GetRespuestaAsyncV2(modelo.Identificacion);
                                    r_iess = resultadoIess?.Persona;
                                }
                                if (r_iess != null)
                                {
                                    if (r_iess.Obligacion != null && !string.IsNullOrEmpty(r_iess.Obligacion.MoraOriginal) && r_iess.Obligacion.Mora.HasValue)
                                        r_iess.Obligacion.Mora = double.Parse(r_iess.Obligacion.MoraOriginal.Replace(",", ""));
                                }
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
                                    r_afiliacionV2 = await ObtenerRAfiliacion(modelo, modelo.Identificacion, tipoFuente);
                                    r_afiliacion = r_afiliacionV2?.Afiliacion;
                                    r_afiliado = await _iess.GetInformacionAfiliado(modelo.Identificacion);
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
                                            r_afiliacionV2 = await ObtenerRAfiliacion(modelo, historialTemp.IdentificacionSecundaria.Trim(), tipoFuente);
                                            r_afiliacion = r_afiliacionV2?.Afiliacion;
                                            r_afiliado = await _iess.GetInformacionAfiliado(historialTemp.IdentificacionSecundaria.Trim());
                                        }
                                    }
                                }
                                else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                {
                                    var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                    {
                                        r_afiliacionV2 = await ObtenerRAfiliacion(modelo, cedulaTemp, tipoFuente);
                                        r_afiliacion = r_afiliacionV2?.Afiliacion;
                                        r_afiliado = await _iess.GetInformacionAfiliado(cedulaTemp);
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

                    datos = new IessViewModel()
                    {
                        HistorialCabecera = historialTemp,
                        Iess = r_iess,
                        Afiliado = r_afiliacion,
                        AfiliadoAdicional = r_afiliado
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathIess = Path.Combine(pathFuentes, "iessDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathIess);
                    datos = JsonConvert.DeserializeObject<IessViewModel>(archivo);
                    datos.EmpresaConfiable = true;
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
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Afiliado,
                            Generado = datos.Afiliado != null,
                            Data = datos.Afiliado != null ? JsonConvert.SerializeObject(datos.Afiliado) : null,
                            Cache = cacheAfiliado,
                            FechaRegistro = DateTime.Now,
                            Reintento = false,
                            DataError = r_afiliacionV2?.Error,
                            FuenteActiva = r_afiliacionV2?.FuenteActiva
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.AfiliadoAdicional,
                            Generado = datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any(),
                            Data = datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any() ? JsonConvert.SerializeObject(datos.AfiliadoAdicional) : null,
                            Cache = cacheAfiliadoAdicional,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteSenescyt(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.Senescyt.Modelos.Persona r_senescyt = null;
                var datos = new SenescytViewModel();
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

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                {
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
                                        var resultado = await _consulta.ObtenerSenescytConsultaExterna(cedulaTemp, hostSenescyt);
                                        if (resultado != null)
                                            r_senescyt = resultado.Data;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Senescyt con identificación {modelo.Identificacion}: {ex.Message}");
                    }

                    //if (r_senescyt == null)
                    //{
                    //    var datosDetalleASenescyt = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                    //    if (datosDetalleASenescyt != null)
                    //    {
                    //        cacheSenescyt = true;
                    //        r_senescyt = JsonConvert.DeserializeObject<Externos.Logica.Senescyt.Modelos.Persona>(datosDetalleASenescyt);
                    //    }
                    //}

                    datos = new SenescytViewModel()
                    {
                        Senescyt = r_senescyt,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSenecyt = Path.Combine(pathFuentes, "senescytDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathSenecyt);
                    datos = JsonConvert.DeserializeObject<SenescytViewModel>(archivo);
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
                            Reintento = false
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteLegal(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicial = null;
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicialNombres = null;
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicialempresaRuc = null;
                Externos.Logica.FJudicial.Modelos.Persona r_fjudicialempresa = null;
                var datos = new JudicialViewModel();
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

                        if (!empresasConsultaPersonalizada.Contains(usuarioActual.IdEmpresa))
                        {
                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                if (cedulaEntidades)
                                {
                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
                                        r_fjudicial = _fjudicial.GetFuncionJudicialVendedor(modelo.Identificacion, "");
                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                            r_fjudicialNombres = _fjudicial.GetFuncionJudicialVendedor("", historialTemp.NombresPersona);

                                        if (r_fjudicial != null && r_fjudicialNombres != null)
                                            r_fjudicial.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Demandado, r_fjudicialNombres.Demandado);
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
                                        r_fjudicialempresaRuc = _fjudicial.GetFuncionJudicialVendedor(modelo.Identificacion, "");
                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.RazonSocialEmpresa))
                                            r_fjudicialempresa = _fjudicial.GetFuncionJudicialVendedor("", historialTemp.RazonSocialEmpresa);

                                        if (r_fjudicialempresa != null && r_fjudicialempresaRuc != null)
                                            r_fjudicialempresa.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicialempresa.Demandado, r_fjudicialempresaRuc.Demandado);
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
                                            r_fjudicial = _fjudicial.GetFuncionJudicialVendedor(historialTemp.IdentificacionSecundaria, "");
                                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                                r_fjudicialNombres = _fjudicial.GetFuncionJudicialVendedor("", historialTemp.NombresPersona);

                                            if (r_fjudicial != null && r_fjudicialNombres != null)
                                                r_fjudicial.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Demandado, r_fjudicialNombres.Demandado);
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
                                            r_fjudicial = _fjudicial.GetFuncionJudicialVendedor(cedulaTemp, "");
                                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.NombresPersona))
                                                r_fjudicialNombres = _fjudicial.GetFuncionJudicialVendedor("", historialTemp.NombresPersona);

                                            if (r_fjudicial != null && r_fjudicialNombres != null)
                                                r_fjudicial.Demandado = ReporteViewModel.NormalizarProcesosLegal(r_fjudicial.Demandado, r_fjudicialNombres.Demandado);
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

                    try
                    {
                        //Datos demostracion procesos Felipe Caceres
                        if (r_fjudicial != null && historialTemp != null && (historialTemp.NombresPersona.ToUpper() == Dominio.Constantes.General.NombrePersonaDemo || historialTemp.Identificacion.Contains(Dominio.Constantes.General.CedulaPersonaDemo)))
                        {
                            //var proceso1 = r_fjudicial.Demandado.FirstOrDefault(kvp => kvp.Value.Codigo.Trim() == "17230-2021-06732" || kvp.Value.Codigo.Trim() == "17230202106732");
                            //r_fjudicial.Demandado.Remove(proceso1.Key);

                            //var proceso2 = r_fjudicial.Demandado.FirstOrDefault(kvp => kvp.Value.Codigo.Trim() == "17455-2008-0545*" || kvp.Value.Codigo.Trim() == "1745520080545*");
                            //r_fjudicial.Demandado.Remove(proceso2.Key);

                            r_fjudicial.Demandado.Clear();

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }

                    datos = new JudicialViewModel()
                    {
                        HistorialCabecera = historialTemp,
                        FJudicial = r_fjudicial,
                        FJEmpresa = r_fjudicialempresa,
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathLegal = Path.Combine(pathFuentes, "legalDemo.json");
                    var archivo = System.IO.File.ReadAllText(pathLegal);
                    datos = JsonConvert.DeserializeObject<JudicialViewModel>(archivo);
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
                            Generado = datos.FJudicial != null,
                            Data = datos.FJudicial != null ? JsonConvert.SerializeObject(datos.FJudicial) : null,
                            Cache = cacheLegal,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.FJEmpresa,
                            Generado = datos.FJEmpresa != null,
                            Data = datos.FJEmpresa != null ? JsonConvert.SerializeObject(datos.FJEmpresa) : null,
                            Cache = cacheLegalEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteANT(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.ANT.Modelos.Licencia r_ant = null;
                var datos = new ANTViewModel();
                Historial historialTemp = null;
                var cacheAnt = false;

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
                                r_ant = _ant.ObtenerLicenciaAntOficialVendedor(modelo.Identificacion, Externos.Logica.ANT.Identificacion.CED);

                            if (r_ant == null && historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                r_ant = _ant.ObtenerLicenciaAntOficialVendedor(historialTemp.IdentificacionSecundaria, Externos.Logica.ANT.Identificacion.CED);

                            if (r_ant != null && (string.IsNullOrEmpty(r_ant.Cedula) || string.IsNullOrEmpty(r_ant.Titular)))
                                r_ant = null;
                        }
                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                            r_ant = _ant.ObtenerLicenciaAntOficialVendedor(modelo.Identificacion, Externos.Logica.ANT.Identificacion.CED);

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

                        datos = new ANTViewModel()
                        {
                            HistorialCabecera = historialTemp,
                            Licencia = r_ant,
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
                    var licencia = JsonConvert.DeserializeObject<Externos.Logica.ANT.Modelos.Licencia>(archivo);
                    datos = new ANTViewModel()
                    {
                        HistorialCabecera = historialTemp,
                        Licencia = licencia,
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
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente ANT procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReporteSERCOP(ReporteViewModel modelo)
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
                var datos = new SERCOPViewModel();
                Historial historialTemp = null;
                var cacheSercop = false;
                var cedulaEntidades = false;
                historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.Sercop;

                if (!_cache)
                {
                    try
                    {
                        if (tipoFuente != 5)
                        {
                            _logger.LogInformation($"Procesando Fuente SERCOP identificación: {modelo.Identificacion}");
                            if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                            {
                                cedulaEntidades = true;
                                modelo.Identificacion = $"{modelo.Identificacion}001";
                            }

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

                    datos = new SERCOPViewModel()
                    {
                        HistorialCabecera = historialTemp,
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
                    datos = JsonConvert.DeserializeObject<SERCOPViewModel>(archivo);
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
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.ProveedorContraloria,
                            Generado = datos.ProveedorContraloria != null && datos.ProveedorContraloria.Count() != 0,
                            Data = datos.ProveedorContraloria != null && datos.ProveedorContraloria.Count() != 0 ? JsonConvert.SerializeObject(datos.ProveedorContraloria) : null,
                            Cache = cacheSercop,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        private async Task ObtenerReportePensionAlimenticia(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia r_pension = null;
                var datos = new PensionAlimenticiaViewModel();
                var cachePension = false;
                var historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.Alimentos;

                if (!_cache)
                {
                    try
                    {
                        if (tipoFuente != 5)
                        {
                            _logger.LogInformation($"Procesando Fuente Pension alimenticia identificación: {modelo.Identificacion}");
                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    r_pension = await _pension.GetRespuestaAsync(modelo.Identificacion);

                                if (r_pension == null && historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                    r_pension = await _pension.GetRespuestaAsync(historialTemp.IdentificacionSecundaria);
                            }
                            else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                r_pension = await _pension.GetRespuestaAsync(modelo.Identificacion);

                            if (r_pension != null && r_pension.Resultados == null)
                                r_pension = null;
                        }

                        //if (r_pension == null)
                        //{
                        //    var datosDetallePension = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                        //    if (datosDetallePension != null)
                        //    {
                        //        cachePension = true;
                        //        r_pension = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(datosDetallePension);
                        //    }
                        //}

                        datos = new PensionAlimenticiaViewModel()
                        {
                            HistorialCabecera = historialTemp,
                            PensionAlimenticia = r_pension,
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
                    datos = new PensionAlimenticiaViewModel()
                    {
                        HistorialCabecera = historialTemp,
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
                            Reintento = false
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
        //private async Task ObtenerReporteSuperBancos(ReporteViewModel modelo)
        //{
        //    try
        //    {
        //        if (modelo == null)
        //            throw new Exception("No se han enviado parámetros para obtener el reporte");

        //        if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
        //            throw new Exception("El campo RUC es obligatorio");

        //        modelo.Identificacion = modelo.Identificacion.Trim();
        //        Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosCedula = null;
        //        Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosNatural = null;
        //        Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosEmpresa = null;
        //        ViewBag.RutaArchivoCedula = string.Empty;
        //        ViewBag.RutaArchivoNatural = string.Empty;
        //        ViewBag.RutaArchivoEmpresa = string.Empty;
        //        var datos = new SuperBancosViewModel();
        //        var cacheSuperBancosCedula = false;
        //        var cacheSuperBancosNatural = false;
        //        var cacheSuperBancosEmpresa = false;
        //        var tipoConsulta = 0;
        //        Historial historialTemp = null;

        //        if (!_cache)
        //        {
        //            try
        //            {
        //                _logger.LogInformation($"Procesando Fuente SuperBancos identificación: {modelo.Identificacion}");
        //                historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
        //                var fechaExpedicion = _superBancos.GetType().GetProperty("FechaExpedicionCedula");
        //                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion))
        //                {
        //                    if (historialTemp != null && string.IsNullOrEmpty(historialTemp.FechaExpedicionCedula?.Trim()))
        //                        throw new Exception($"No se puede consultar super de bancos porque no se tiene fecha de expedición de cédula. Identificación: {modelo.Identificacion}");

        //                    //Cedula
        //                    if (fechaExpedicion != null)
        //                        fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

        //                    r_superBancosCedula = await _superBancos.GetRespuestaAsyncVendedor(modelo.Identificacion);

        //                    //Natural
        //                    if (fechaExpedicion != null)
        //                        fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

        //                    r_superBancosNatural = await _superBancos.GetRespuestaAsyncVendedor($"{modelo.Identificacion}001");

        //                    tipoConsulta = 1;
        //                }
        //                else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
        //                {
        //                    if (historialTemp != null && string.IsNullOrEmpty(historialTemp.FechaExpedicionCedula?.Trim()))
        //                        throw new Exception($"No se puede consultar super de bancos porque no se tiene fecha de expedición de cédula. Identificación: {modelo.Identificacion}");

        //                    //Natural
        //                    if (fechaExpedicion != null)
        //                        fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

        //                    r_superBancosNatural = await _superBancos.GetRespuestaAsyncVendedor(modelo.Identificacion);

        //                    //Cedula
        //                    var cedula = modelo.Identificacion.Substring(0, 10);
        //                    if (fechaExpedicion != null)
        //                        fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

        //                    r_superBancosCedula = await _superBancos.GetRespuestaAsyncVendedor(cedula);

        //                    tipoConsulta = 2;
        //                }
        //                else
        //                {
        //                    //Juridico
        //                    r_superBancosEmpresa = await _superBancos.GetRespuestaAsyncVendedor(modelo.Identificacion);

        //                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.FechaExpedicionCedula?.Trim()))
        //                    {
        //                        //Natural
        //                        var rucNatural = $"{historialTemp.IdentificacionSecundaria}001";
        //                        if (fechaExpedicion != null)
        //                            fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

        //                        r_superBancosNatural = await _superBancos.GetRespuestaAsyncVendedor(rucNatural);

        //                        //Cedula
        //                        var cedula = historialTemp.IdentificacionSecundaria;
        //                        if (fechaExpedicion != null)
        //                            fechaExpedicion.SetValue(_superBancos, historialTemp.FechaExpedicionCedula.Trim());

        //                        r_superBancosCedula = await _superBancos.GetRespuestaAsyncVendedor(cedula);

        //                        tipoConsulta = 3;
        //                    }
        //                    else
        //                        _logger.LogInformation($"No se puede consultar super de bancos porque no se tiene fecha de expedición de cédula. Identificación: {modelo.Identificacion}");
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError($"Error al consultar fuente SuperBancos con identificación {modelo.Identificacion}: {ex.Message}");
        //            }

        //            if (r_superBancosCedula == null)
        //            {
        //                var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
        //                if (datosSuperBancos != null)
        //                {
        //                    cacheSuperBancosCedula = true;
        //                    r_superBancosCedula = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
        //                }
        //            }

        //            if (r_superBancosNatural == null)
        //            {
        //                var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
        //                if (datosSuperBancos != null)
        //                {
        //                    cacheSuperBancosNatural = true;
        //                    r_superBancosNatural = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
        //                }
        //            }

        //            if (r_superBancosEmpresa == null && tipoConsulta == 3)
        //            {
        //                var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
        //                if (datosSuperBancos != null)
        //                {
        //                    cacheSuperBancosEmpresa = true;
        //                    r_superBancosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
        //                }
        //            }

        //            datos = new SuperBancosViewModel()
        //            {
        //                SuperBancos = r_superBancosCedula,
        //                SuperBancosNatural = r_superBancosNatural,
        //                SuperBancosEmpresa = r_superBancosEmpresa,
        //                TipoConsulta = tipoConsulta
        //            };
        //        }
        //        else
        //        {
        //            var pathBase = System.IO.Path.Combine("wwwroot", "data");
        //            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
        //            var pathSuperBancos = Path.Combine(pathFuentes, "superBancosDemo.json");
        //            var archivo = System.IO.File.ReadAllText(pathSuperBancos);
        //            datos = JsonConvert.DeserializeObject<SuperBancosViewModel>(archivo);
        //        }

        //        _logger.LogInformation("Fuente de SuperBancos procesada correctamente");
        //        _logger.LogInformation($"Procesando registro de historiales de la fuente SuperBancos. Id Historial: {modelo.IdHistorial}");

        //        try
        //        {
        //            if (modelo.IdHistorial > 0)
        //            {
        //                await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
        //                {
        //                    IdHistorial = modelo.IdHistorial,
        //                    TipoFuente = Dominio.Tipos.Fuentes.SuperBancos,
        //                    Generado = datos.SuperBancos != null,
        //                    Data = datos.SuperBancos != null ? JsonConvert.SerializeObject(datos.SuperBancos) : null,
        //                    Cache = cacheSuperBancosCedula,
        //                    FechaRegistro = DateTime.Now,
        //                    Reintento = false
        //                });
        //                _logger.LogInformation("Historial de la Fuente SuperBancos procesado correctamente");


        //                await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
        //                {
        //                    IdHistorial = modelo.IdHistorial,
        //                    TipoFuente = Dominio.Tipos.Fuentes.SuperBancosNatural,
        //                    Generado = datos.SuperBancosNatural != null,
        //                    Data = datos.SuperBancosNatural != null ? JsonConvert.SerializeObject(datos.SuperBancosNatural) : null,
        //                    Cache = cacheSuperBancosNatural,
        //                    FechaRegistro = DateTime.Now,
        //                    Reintento = false
        //                });
        //                _logger.LogInformation("Historial de la Fuente SuperBancos Natural procesado correctamente");

        //                await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
        //                {
        //                    IdHistorial = modelo.IdHistorial,
        //                    TipoFuente = Dominio.Tipos.Fuentes.SuperBancosEmpresa,
        //                    Generado = datos.SuperBancosEmpresa != null,
        //                    Data = datos.SuperBancosEmpresa != null ? JsonConvert.SerializeObject(datos.SuperBancosEmpresa) : null,
        //                    Cache = cacheSuperBancosEmpresa,
        //                    FechaRegistro = DateTime.Now,
        //                    Reintento = false
        //                });
        //                _logger.LogInformation("Historial de la Fuente SuperBancos Juridico procesado correctamente");
        //            }
        //            else
        //                throw new Exception("El Id del Historial no se ha generado correctamente");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, ex.Message);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e, e.Message);
        //    }
        //}
        private async Task ObtenerReporteBuroCredito(ReporteViewModel modelo)
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
                var idUsuario = User.GetUserId<int>();
                var idPlanBuro = 0;
                var datos = new BuroCreditoViewModel();
                var dataError = string.Empty;
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var aplicaConsultaBuroCompartida = false;

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
                if (planBuroCredito.ConsultasCompartidas && planBuroCredito.NumeroMaximoConsultasCompartidas.HasValue && planBuroCredito.NumeroMaximoConsultasCompartidas.Value > 0)
                {
                    var numeroHistorialBuroSinComp = await _historiales.CountAsync(s => s.Id != modelo.IdHistorial && s.IdPlanBuroCredito == idPlanBuro && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date && !s.ConsultaBuroCompartido);
                    if (numeroHistorialBuroSinComp >= planBuroCredito.NumeroMaximoConsultas)
                    {
                        _logger.LogInformation($"Se ha alcanzado el límite de consultas con las credenciales propias del cliente:  {usuarioActual.IdEmpresa}.");
                        aplicaConsultaBuroCompartida = true;
                    }

                    if (aplicaConsultaBuroCompartida)
                    {
                        var resultadoPermisoCompartido = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;
                        if (planBuroCredito.BloquearConsultas)
                        {
                            var numeroHistorialBuroComp = await _historiales.CountAsync(s => s.Id != modelo.IdHistorial && s.IdPlanBuroCredito == idPlanBuro && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date && s.ConsultaBuroCompartido);
                            resultadoPermisoCompartido = planBuroCredito.NumeroMaximoConsultasCompartidas > numeroHistorialBuroComp ? Dominio.Tipos.EstadosPlanesBuroCredito.Activo : Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
                        }

                        if (resultadoPermisoCompartido != Dominio.Tipos.EstadosPlanesBuroCredito.Activo)
                            throw new Exception("No es posible realizar esta consulta ya que excedió el límite de consultas del plan Buró de Crédito.");
                    }

                    #region Mail
                    if ((numeroHistorialBuroSinComp + 1) == planBuroCredito.NumeroMaximoConsultas)
                    {
                        try
                        {
                            var usuarios = new List<string>();
                            var usuarioAdministrador = await _usuarios.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.UsuariosRoles.Any(s => s.RoleId == (short)Dominio.Tipos.Roles.AdministradorEmpresa), null, null, true);
                            if (usuarioAdministrador != null)
                                usuarios.Add(usuarioAdministrador.NormalizedEmail);

                            var usuarioGc = await _usuarios.FirstOrDefaultAsync(m => m, m => m.Identificacion == Dominio.Constantes.General.CedulaPersonaDemo && m.UsuariosRoles.Any(s => s.RoleId == (short)Dominio.Tipos.Roles.Administrador), null, null, true);
                            if (usuarioGc != null)
                                usuarios.Add(usuarioGc.NormalizedEmail);

                            if (usuarios.Any())
                            {
                                var emisor = _configuration.GetValue<string>("SmtpSettings:From");
                                _logger.LogInformation($"Preparando envío de correo consultas de butó ...");
                                var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.LimiteBuroAlcanzado);
                                if (string.IsNullOrEmpty(template))
                                    throw new Exception($"No se ha cargado la plantilla de tipo: {Dominio.Tipos.TemplatesCorreo.LimiteBuroAlcanzado}");

                                var asunto = "Límite de Consultas Alcanzado Buró de Crédito Plataforma Integral de Información 360°";
                                var domainName = new Uri(HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
                                var enlace = $"{domainName}{Url.Action("Inicio", "Cuenta", new { Area = "Identidad" })}";

                                var replacements = new Dictionary<string, object>
                                {
                                    { "{NOMBREEMPRESA}", $"{usuarioActual.Empresa.Identificacion} - {usuarioActual.Empresa.RazonSocial}" },
                                    { "{NUMCONSULTASCLIENTE}", numeroHistorialBuroSinComp.ToString() },
                                    { "{NUMCONSULTASINTERNAS}", planBuroCredito.NumeroMaximoConsultasCompartidas.Value },
                                    { "{ENLACE}", enlace },
                                };
                                var correosBcc = string.Join(';', usuarios.Select(m => m.ToLower().Trim()));
                                await _emailSender.SendEmailAsync(emisor, asunto, template, "USUARIO", replacements, null, correosBcc);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                    #endregion Mail
                }
                else
                {
                    var resultadoPermiso = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;
                    if (planBuroCredito.BloquearConsultas)
                    {
                        var numeroHistorialBuro = await _historiales.CountAsync(s => s.Id != modelo.IdHistorial && s.IdPlanBuroCredito == idPlanBuro && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date);
                        resultadoPermiso = planBuroCredito.NumeroMaximoConsultas > numeroHistorialBuro ? Dominio.Tipos.EstadosPlanesBuroCredito.Activo : Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
                    }

                    if (resultadoPermiso != Dominio.Tipos.EstadosPlanesBuroCredito.Activo)
                        throw new Exception("No es posible realizar esta consulta ya que excedió el límite de consultas del plan Buró de Crédito.");
                }

                var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                if (historial != null)
                {
                    historial.IdPlanBuroCredito = idPlanBuro;
                    historial.TipoFuenteBuro = planBuroCredito.Fuente;
                    if (aplicaConsultaBuroCompartida)
                        historial.ConsultaBuroCompartido = true;
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
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            if (aplicaConsultaBuroCompartida && credenciales != null && credenciales.Any())
                                credenciales = null;

                            if (buroCredito != null && DateTime.Today.Date.AddDays(-planBuroCredito.PersistenciaCache) <= buroCredito.Fecha.Date)
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval con la persistencia del plan de la empresa para la identificación: {modelo.Identificacion}");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(buroCredito.Data);
                                cacheBuroCredito = true;
                            }
                            else
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval identificación: {modelo.Identificacion}");
                                if (usuarioActual.Empresa.Identificacion == Dominio.Constantes.Clientes.Cliente1792899036001)
                                {
                                    if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Aval)
                                        credenciales = new[] { credencial.Usuario, credencial.Clave, credencial.Enlace };

                                    if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocredito = await _burocredito.GetRespuestaAyasaAsync(modelo.Identificacion.Substring(0, 10), credenciales);
#endif
                                    }
                                    else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocredito = await _burocredito.GetRespuestaAyasaAsync(modelo.Identificacion, credenciales);
#endif
                                    }
                                    else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocredito = await _burocredito.GetRespuestaAyasaAsync(modelo.Identificacion, credenciales);
#endif
                                    }
                                }
                                else
                                {
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


                            if (aplicaConsultaBuroCompartida && credenciales != null && credenciales.Any())
                                credenciales = null;

                            if (buroCredito != null && DateTime.Today.Date.AddDays(-planBuroCredito.PersistenciaCache) <= buroCredito.Fecha.Date)
                            {
                                _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax con la persistencia del plan de la empresa para la identificación: {modelo.Identificacion}");
                                r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(buroCredito.Data);
                                cacheBuroCredito = true;
                            }
                            else
                            {
                                if (credencial == null || !credencial.CovidRest)
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
                                }
                                else
                                {
                                    _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax identificación: {modelo.Identificacion}");
                                    if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaRestAsync(modelo.Identificacion.Substring(0, 10), credenciales);
#endif
                                    }
                                    else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaRestAsync(modelo.Identificacion, credenciales);
#endif
                                    }
                                    else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaRestAsync(modelo.Identificacion, credenciales);
#endif
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
                            throw new Exception("No se pudo realizar la consulta de Buró de Crédito en ninguna de las Fuentes");
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

                try
                {
                    if ((historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && r_burocredito != null && r_burocredito.Result != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
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

                        if (r_burocredito != null && r_burocredito.Result != null && r_burocredito.Result.GastoFinanciero != null && r_burocredito.Result.GastoFinanciero.Any() && r_burocredito.Result.GastoFinanciero.FirstOrDefault().CuotaEstimadaTitular > 0 && deudaSuma == 0)
                        {
                            ingresoPrevio = (double)(r_burocredito.Result.GastoFinanciero.FirstOrDefault()?.CuotaEstimadaTitular * 1.40);
                            if (double.TryParse(ingresoSuperior, out _) && double.Parse(ingresoSuperior) > 0 && double.Parse(ingresoSuperior) >= ingresoPrevio)
                                r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoSuperior;
                            else if (!r_burocredito.Result.Ingreso.Any())
                                r_burocredito.Result.Ingreso.Add(new Externos.Logica.BuroCredito.Modelos.CreditoRespuesta.Ingreso() { RangoIngreso = ingresoPrevio.ToString("N", culture) });
                            else
                                r_burocredito.Result.Ingreso.FirstOrDefault().RangoIngreso = ingresoPrevio.ToString("N", culture);
                        }
                        else if (r_burocredito != null && r_burocredito.Result != null && r_burocredito.Result.GastoFinanciero != null && r_burocredito.Result.GastoFinanciero.Any() && r_burocredito.Result.GastoFinanciero.FirstOrDefault().CuotaEstimadaTitular > 0 && deudaSuma > 0)
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
                    else if ((historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                    {
                        var deudaSumaEquifax = 0.00;
                        var ingresoPrevioEquifax = 0.00;
                        var ingresoEstimadoEquifax = 0.00;
                        if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360 != null && r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Any())
                        {
                            var deudaVencido = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != String.Empty).Sum(x => x.Vencido);
                            var deudaDemandaJudicial = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != String.Empty).Sum(x => x.DemandaJudicial);
                            var deudaCarteraCastigada = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != String.Empty).Sum(x => x.CarteraCastigada);
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                datos = new BuroCreditoViewModel()
                {
                    HistorialCabecera = historialTemp,
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
                                Reintento = false,
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
                                Reintento = false,
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
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }


        private async Task<Externos.Logica.IESS.Modelos.ResultadoAfiliacion> ObtenerRAfiliacion(ReporteViewModel modelo, string identificacion, int? tipoFuente)
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
                        {
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficialV2(identificacion, fechaNacimiento.Value);
                            ViewBag.TipoFuente = 1;
                        }
                        break;
                    case 2:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        ViewBag.TipoFuente = 2;
                        break;
                    case 3:
                        if (fechaNacimiento.HasValue)
                        {
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficialV2(identificacion, fechaNacimiento.Value);
                            ViewBag.TipoFuente = 1;
                        }
                        if (r_afiliacion == null)
                        {
                            r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                            ViewBag.TipoFuente = 2;
                        }
                        break;
                    case 4:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        ViewBag.TipoFuente = 2;
                        if (r_afiliacion == null && fechaNacimiento.HasValue)
                        {
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficialV2(identificacion, fechaNacimiento.Value);
                            ViewBag.TipoFuente = 1;
                        }
                        break;
                    case 5:
                        ViewBag.TipoFuente = 5;
                        _logger.LogInformation("Historial de la Fuente IESS inactiva: 5");
                        break;
                    default:
                        ViewBag.TipoFuente = 2;
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(identificacion);
                        break;
                }
                return r_afiliacion;
            }
            catch (Exception e)
            {

                _logger.LogError(e, e.Message);
                return null;
            }
        }

        #endregion Fuentes



    }
}
