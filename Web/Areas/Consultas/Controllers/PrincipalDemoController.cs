// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System.Data;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Text.RegularExpressions;
using Web.Areas.Consultas.Models;
using Dominio.Entidades.Balances;
using Persistencia.Repositorios.Balance;
using Microsoft.AspNetCore.Identity;
using Persistencia.Repositorios.Identidad;
using Microsoft.EntityFrameworkCore;
using Infraestructura.Servicios;
using QuickChart;
using Microsoft.AspNetCore.Http.Extensions;
using Dominio.Entidades.Identidad;
using Externos.Logica.Garancheck.Modelos;
using Dominio.Tipos;
using Externos.Logica.Modelos;
using Persistencia.Migraciones.Principal;

namespace Web.Areas.Consultas.Controllers
{
    [Area("Consultas")]
    [Route("Consultas/PrincipalDemo")]
    [Authorize(Policy = "Consultas")]
    public class PrincipalDemoController : Controller
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
        private readonly Externos.Logica.UAFE.Controlador _uafe;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detallesHistorial;
        private readonly IUsuarios _usuarios;
        private readonly IConsultaService _consulta;
        private readonly ICalificaciones _calificaciones;
        private readonly IPlanesBuroCredito _planesBuroCredito;
        private readonly IAccesos _accesos;
        private readonly ICredencialesBuro _credencialesBuro;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly IParametrosClientesHistoriales _parametrosClientesHistoriales;
        private readonly IEmailService _emailSender;
        private readonly IReportesConsolidados _reporteConsolidado;

        public PrincipalDemoController(IConfiguration configuration,
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
            Externos.Logica.UAFE.Controlador uafe,
            IHistoriales historiales,
            IDetallesHistorial detallehistoriales,
            IUsuarios usuarios,
            IConsultaService consulta,
            ICalificaciones calificaciones,
            IPlanesBuroCredito planesBuroCredito,
            IPlanesEvaluaciones planesEvaluaciones,
            IAccesos accesos,
            ICredencialesBuro credencialesBuro,
            IParametrosClientesHistoriales parametrosClientesHistoriales,
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
            _uafe = uafe;
            _historiales = historiales;
            _detallesHistorial = detallehistoriales;
            _usuarios = usuarios;
            _consulta = consulta;
            _calificaciones = calificaciones;
            _planesBuroCredito = planesBuroCredito;
            _accesos = accesos;
            _credencialesBuro = credencialesBuro;
            _planesEvaluaciones = planesEvaluaciones;
            _parametrosClientesHistoriales = parametrosClientesHistoriales;
            _emailSender = emailSender;
            _reporteConsolidado = reportesConsolidados;
        }

        public async Task<IActionResult> Inicio(string identificacion = null)
        {
            try
            {
                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (string.IsNullOrEmpty(usuarioActual.Empresa.Identificacion))
                    throw new Exception("No se encontró el RUC de la empresa.");

                ViewBag.IdEmpresa = usuarioActual.IdEmpresa;
                ViewBag.Identificacion = identificacion;

                if (usuarioActual.Id != Dominio.Constantes.General.IdUsuarioDemo)
                    return Unauthorized();

                return View("InicioDemo");
            }
            catch (Exception ex)
            {
                ViewBag.IdEmpresa = 0;
                ViewBag.Identificacion = identificacion;
                _logger.LogError(ex, ex.Message);
                return View();
            }
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

                if (usuarioActual.Empresa.VistaPersonalizada)
                    ValidarEmpresaPersonalizada(new ParametroClienteViewModel()
                    {
                        IdentificacionEmpresa = usuarioActual.Empresa.Identificacion,
                        Identificacion = modelo.Identificacion.Trim(),
                        Valor_1790325083001 = modelo.Valor_1790325083001
                    });

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

                //var habilitarBuro = usuarioActual.Empresa.PlanesBuroCredito.Any(m => m.NumeroMaximoConsultas > 0 && m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);

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

                if (usuarioActual.Empresa.VistaPersonalizada)
                {
                    await GuardarParametroClienteHistorial(new ParametroClienteViewModel()
                    {
                        IdHistorial = idHistorial,
                        IdentificacionEmpresa = usuarioActual.Empresa.Identificacion,
                        Valor_1790325083001 = modelo.Valor_1790325083001,
                        IdUsuario = usuarioActual.Id
                    });
                }

                return Json(new { idHistorial, tipoIdentificacion = ValidacionViewModel.ObtenerTipoIdentificacion(identificacionOriginal) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(PrincipalController), StatusCodes.Status500InternalServerError);
            }
        }

        private async Task GuardarParametroClienteHistorial(ParametroClienteViewModel parametro)
        {
            try
            {
                if (parametro == null)
                    throw new Exception("No se enviaron parametros para clientes personalizados");

                var parametroCliente = Dominio.Tipos.ParametrosClientes.Desconocido;
                var valor = string.Empty;

                switch (parametro.IdentificacionEmpresa)
                {
                    case Dominio.Constantes.Clientes.Cliente1790325083001:
                        parametroCliente = Dominio.Tipos.ParametrosClientes.SegmentoCartera;
                        valor = parametro.Valor_1790325083001 != Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Desconocido ? ((short)parametro.Valor_1790325083001).ToString() : string.Empty;
                        break;
                    default:
                        break;
                }

                if (parametroCliente != Dominio.Tipos.ParametrosClientes.Desconocido && !string.IsNullOrEmpty(valor))
                    await _parametrosClientesHistoriales.GuardarParametroClienteHistorialAsync(new ParametroClienteHistorial()
                    {
                        IdHistorial = parametro.IdHistorial,
                        Valor = valor,
                        Parametro = parametroCliente,
                        FechaCreacion = DateTime.Now,
                        UsuarioCreacion = parametro.IdUsuario
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void ValidarEmpresaPersonalizada(ParametroClienteViewModel parametro)
        {
            try
            {
                if (parametro == null)
                    throw new Exception("No se han ingresado los datos de la consulta.");

                switch (parametro.IdentificacionEmpresa)
                {
                    case Dominio.Constantes.Clientes.Cliente1790325083001:
                        if ((ValidacionViewModel.ValidarRucJuridico(parametro.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(parametro.Identificacion)) && parametro.Valor_1790325083001 == Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Consumo)
                            throw new Exception("No se puede consultar RUCS con la opción Consumo");

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        #endregion Historiales

        #region Fuentes
        [HttpPost]
        [Route("ObtenerReporteSRI")]
        public async Task<IActionResult> ObtenerReporteSRI(ReporteViewModel modelo)
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
                List<Externos.Logica.Balances.Modelos.Similares> r_similares = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                Externos.Logica.Balances.Modelos.CatastroFantasma catastroFantasma = null;
                var busquedaNuevaSri = false;
                var cacheSri = false;
                var cacheContactosEmpresa = false;
                var cacheEmpSimilares = false;
                var cacheCatastrosFantasmas = false;
                var consultaFantasma = false;
                var impuestosRenta = new List<Externos.Logica.SRi.Modelos.Anexo>();
                historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);

                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.Sri;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSri = Path.Combine(pathFuentes, "sriFullDemo.json");
                var pathContactoEmpresa = Path.Combine(pathFuentes, "sriContactoEmpresaDemo.json");
                var pathSimilares = Path.Combine(pathFuentes, "sriSimilaresDemo.json");
                var pathCatastroFantasma = Path.Combine(pathFuentes, "sriCatastrosDemo.json");
                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));
                r_similares = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Similares>>(System.IO.File.ReadAllText(pathSimilares));
                contactosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(System.IO.File.ReadAllText(pathContactoEmpresa));
                //catastroFantasma = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.CatastroFantasma>(System.IO.File.ReadAllText(pathCatastroFantasma));
                busquedaNuevaSri = false;

                if (r_sri != null && string.IsNullOrEmpty(r_sri.RUC))
                {
                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.Identificacion))
                        r_sri.RUC = historialTemp.Identificacion;
                }

                var datos = new SRIViewModel()
                {
                    Sri = r_sri,
                    ContactosEmpresa = contactosEmpresa,
                    EmpresasSimilares = r_similares,
                    CatastroFantasma = catastroFantasma,
                    BusquedaNueva = busquedaNuevaSri
                };

                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesEmpresas = new[] { Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.ContactosEmpresa, Dominio.Tipos.Fuentes.EmpresasSimilares, Dominio.Tipos.Fuentes.CatastroFantasma };
                        var historialesEmpresa = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesEmpresas.Contains(m.TipoFuente));
                        var historialSri = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri);
                        var historialContEmpresas = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa);
                        var historialEmpSimilares = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasSimilares);
                        var historialCatastroFantasma = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.CatastroFantasma);

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

                        if (historialSri != null && (!historialSri.Generado || !busquedaNuevaSri))
                        {
                            historialSri.IdHistorial = modelo.IdHistorial;
                            historialSri.TipoFuente = Dominio.Tipos.Fuentes.Sri;
                            historialSri.Generado = datos.Sri != null;
                            historialSri.Data = datos.Sri != null ? JsonConvert.SerializeObject(datos.Sri) : null;
                            historialSri.Cache = cacheSri;
                            historialSri.FechaRegistro = DateTime.Now;
                            historialSri.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSri);
                            _logger.LogInformation("Historial de la Fuente SRI actualizado correctamente");
                        }
                        else if (historialSri == null)
                        {
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
                        }

                        if (historialContEmpresas != null && (!historialContEmpresas.Generado || !busquedaNuevaSri))
                        {
                            historialContEmpresas.IdHistorial = modelo.IdHistorial;
                            historialContEmpresas.TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa;
                            historialContEmpresas.Generado = datos.ContactosEmpresa != null;
                            historialContEmpresas.Data = datos.ContactosEmpresa != null ? JsonConvert.SerializeObject(datos.ContactosEmpresa) : null;
                            historialContEmpresas.Cache = cacheContactosEmpresa;
                            historialContEmpresas.FechaRegistro = DateTime.Now;
                            historialContEmpresas.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContEmpresas);
                            _logger.LogInformation("Historial de la Fuente Contactos Empresa actualizado correctamente");
                        }
                        else if (historialContEmpresas == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa,
                                Generado = datos.ContactosEmpresa != null,
                                Data = datos.ContactosEmpresa != null ? JsonConvert.SerializeObject(datos.ContactosEmpresa) : null,
                                Cache = cacheContactosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }

                        if (historialEmpSimilares != null && (!historialEmpSimilares.Generado || !busquedaNuevaSri))
                        {
                            historialEmpSimilares.IdHistorial = modelo.IdHistorial;
                            historialEmpSimilares.TipoFuente = Dominio.Tipos.Fuentes.EmpresasSimilares;
                            historialEmpSimilares.Generado = datos.EmpresasSimilares != null;
                            historialEmpSimilares.Data = datos.EmpresasSimilares != null ? JsonConvert.SerializeObject(datos.EmpresasSimilares) : null;
                            historialEmpSimilares.Cache = cacheEmpSimilares;
                            historialEmpSimilares.FechaRegistro = DateTime.Now;
                            historialEmpSimilares.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialEmpSimilares);
                            _logger.LogInformation("Historial de la Fuente Empresas Similares actualizado correctamente");
                        }
                        else if (historialEmpSimilares == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.EmpresasSimilares,
                                Generado = datos.EmpresasSimilares != null && datos.EmpresasSimilares.Any(),
                                Data = datos.EmpresasSimilares != null ? JsonConvert.SerializeObject(datos.EmpresasSimilares) : null,
                                Cache = cacheEmpSimilares,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }

                        if (consultaFantasma)
                        {
                            if (historialCatastroFantasma != null && (!historialCatastroFantasma.Generado || !busquedaNuevaSri))
                            {
                                historialCatastroFantasma.IdHistorial = modelo.IdHistorial;
                                historialCatastroFantasma.TipoFuente = Dominio.Tipos.Fuentes.CatastroFantasma;
                                historialCatastroFantasma.Generado = datos.CatastroFantasma != null;
                                historialCatastroFantasma.Data = datos.CatastroFantasma != null ? JsonConvert.SerializeObject(datos.CatastroFantasma) : null;
                                historialCatastroFantasma.Cache = cacheCatastrosFantasmas;
                                historialCatastroFantasma.FechaRegistro = DateTime.Now;
                                historialCatastroFantasma.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialCatastroFantasma);
                                _logger.LogInformation("Historial de la Fuente Catastros Fantasmas actualizado correctamente");
                            }
                            else if (historialCatastroFantasma == null)
                            {
                                await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                                {
                                    IdHistorial = modelo.IdHistorial,
                                    TipoFuente = Dominio.Tipos.Fuentes.CatastroFantasma,
                                    Generado = datos.CatastroFantasma != null,
                                    Data = datos.CatastroFantasma != null ? JsonConvert.SerializeObject(datos.CatastroFantasma) : null,
                                    Cache = cacheCatastrosFantasmas,
                                    FechaRegistro = DateTime.Now,
                                    Reintento = false
                                });

                            }
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

                return PartialView("../Shared/Fuentes/_FuenteSri", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteSri", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteCivil")]
        public async Task<IActionResult> ObtenerReporteCivil(ReporteViewModel modelo)
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
                Externos.Logica.Garancheck.Modelos.RegistroCivil registroCivil = null;
                var datos = new CivilViewModel();
                var cacheCivil = false;
                var cachePersonal = false;
                var cacheContactos = false;
                var cacheContactosIess = false;
                var cacheRegistroCivil = false;
                var busquedaNuevaCivil = false;

                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.RegistroCivil;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                datos = JsonConvert.DeserializeObject<CivilViewModel>(System.IO.File.ReadAllText(pathCivil));

                _logger.LogInformation("Fuente de Civil procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Civil. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesCivil = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.Personales, Dominio.Tipos.Fuentes.Contactos, Dominio.Tipos.Fuentes.RegistroCivil, Dominio.Tipos.Fuentes.ContactosIess };
                        var historialesCivil = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesCivil.Contains(m.TipoFuente));
                        var historialCiudadano = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano);
                        var historialPersonal = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Personales);
                        var historialContactos = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Contactos);
                        var historialRegCivil = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil);
                        var historialContactosIess = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess);

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

                        if (historialCiudadano != null && (!historialCiudadano.Generado || !busquedaNuevaCivil))
                        {
                            historialCiudadano.IdHistorial = modelo.IdHistorial;
                            historialCiudadano.TipoFuente = Dominio.Tipos.Fuentes.Ciudadano;
                            historialCiudadano.Generado = r_garancheck != null;
                            historialCiudadano.Data = r_garancheck != null ? JsonConvert.SerializeObject(r_garancheck) : null;
                            historialCiudadano.Cache = cacheCivil;
                            historialCiudadano.FechaRegistro = DateTime.Now;
                            historialCiudadano.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialCiudadano);
                            _logger.LogInformation("Historial de la Fuente Garancheck para Civil se actualizado correctamente");
                        }
                        else if (historialCiudadano == null)
                        {
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
                        }

                        if (historialPersonal != null && (!historialPersonal.Generado || !busquedaNuevaCivil))
                        {
                            historialPersonal.IdHistorial = modelo.IdHistorial;
                            historialPersonal.TipoFuente = Dominio.Tipos.Fuentes.Personales;
                            historialPersonal.Generado = datosPersonal != null;
                            historialPersonal.Data = datosPersonal != null ? JsonConvert.SerializeObject(datosPersonal) : null;
                            historialPersonal.Cache = cachePersonal;
                            historialPersonal.FechaRegistro = DateTime.Now;
                            historialPersonal.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPersonal);
                            _logger.LogInformation("Historial de la Fuente Datos Personales para Civil se actualizado correctamente");
                        }
                        else if (historialPersonal == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.Personales,
                                Generado = datos.Personales != null,
                                Data = datos.Personales != null ? JsonConvert.SerializeObject(datos.Personales) : null,
                                Cache = cachePersonal,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }

                        if (historialContactos != null && (!historialContactos.Generado || !busquedaNuevaCivil))
                        {
                            historialContactos.IdHistorial = modelo.IdHistorial;
                            historialContactos.TipoFuente = Dominio.Tipos.Fuentes.Contactos;
                            historialContactos.Generado = contactos != null;
                            historialContactos.Data = contactos != null ? JsonConvert.SerializeObject(contactos) : null;
                            historialContactos.Cache = cacheContactos;
                            historialContactos.FechaRegistro = DateTime.Now;
                            historialContactos.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContactos);
                            _logger.LogInformation("Historial de la Fuente Contactos para Civil se actualizado correctamente");
                        }
                        else if (historialContactos == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.Contactos,
                                Generado = datos.Contactos != null,
                                Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                                Cache = cacheContactos,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }

                        if (historialRegCivil != null && (!historialRegCivil.Generado || !busquedaNuevaCivil))
                        {
                            historialRegCivil.IdHistorial = modelo.IdHistorial;
                            historialRegCivil.TipoFuente = Dominio.Tipos.Fuentes.RegistroCivil;
                            historialRegCivil.Generado = registroCivil != null;
                            historialRegCivil.Data = registroCivil != null ? JsonConvert.SerializeObject(registroCivil) : null;
                            historialRegCivil.Cache = cacheRegistroCivil;
                            historialRegCivil.FechaRegistro = DateTime.Now;
                            historialRegCivil.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialRegCivil);
                            _logger.LogInformation("Historial de la Fuente Reg. Civil para Civil se actualizado correctamente");
                        }
                        else if (historialRegCivil == null)
                        {
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
                        }

                        if (historialContactosIess != null && (!historialContactosIess.Generado || !busquedaNuevaCivil))
                        {
                            historialContactosIess.IdHistorial = modelo.IdHistorial;
                            historialContactosIess.TipoFuente = Dominio.Tipos.Fuentes.ContactosIess;
                            historialContactosIess.Generado = contactosIess != null;
                            historialContactosIess.Data = contactosIess != null ? JsonConvert.SerializeObject(contactosIess) : null;
                            historialContactosIess.Cache = cacheContactosIess;
                            historialContactosIess.FechaRegistro = DateTime.Now;
                            historialContactosIess.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContactosIess);
                            _logger.LogInformation("Historial de la Fuente Contactos Iess para Civil se actualizado correctamente");
                        }
                        else if (historialContactosIess == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.ContactosIess,
                                Generado = datos.ContactosIess != null,
                                Data = datos.ContactosIess != null ? JsonConvert.SerializeObject(datos.ContactosIess) : null,
                                Cache = cacheContactosIess,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }
                        _logger.LogInformation("Historial de la Fuente Civil procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView("../Shared/Fuentes/_FuentePersona", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePersona", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteArbolFamiliar")]
        public async Task<IActionResult> ObtenerReporteArbolFamiliar(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.Garancheck.Modelos.Familia familiares = null;
                var datos = new CivilViewModel();
                var cacheCivil = false;
                var busquedaNuevaFamilia = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                datos = JsonConvert.DeserializeObject<CivilViewModel>(System.IO.File.ReadAllText(pathCivil));

                _logger.LogInformation("Fuente de Árbol Familiar procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Árbol Familiar. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialArbolFamiliar = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Familiares);
                        if (historialArbolFamiliar != null && (!historialArbolFamiliar.Generado || !busquedaNuevaFamilia))
                        {
                            historialArbolFamiliar.IdHistorial = modelo.IdHistorial;
                            historialArbolFamiliar.TipoFuente = Dominio.Tipos.Fuentes.Familiares;
                            historialArbolFamiliar.Generado = familiares != null;
                            historialArbolFamiliar.Data = familiares != null ? JsonConvert.SerializeObject(familiares) : null;
                            historialArbolFamiliar.Cache = cacheCivil;
                            historialArbolFamiliar.FechaRegistro = DateTime.Now;
                            historialArbolFamiliar.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialArbolFamiliar);
                            _logger.LogInformation("Historial de la Fuente Familiares para Civil se actualizado correctamente");
                        }
                        else if (historialArbolFamiliar == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.Familiares,
                                Generado = datos.Familiares != null,
                                Data = datos.Familiares != null ? JsonConvert.SerializeObject(datos.Familiares) : null,
                                Cache = cacheCivil,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }
                        _logger.LogInformation("Historial de la Fuente Árbol Familiar procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteFamilia", datos);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteFamilia", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteBalance")]
        public async Task<IActionResult> ObtenerReporteBalance(ReporteViewModel modelo)
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
                var cacheAccionistas = false;
                var busquedaNuevaBalance = false;
                var busquedaNoJuridico = false;
                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                List<Externos.Logica.Balances.Modelos.BalanceEmpresa> r_balances = null;
                Externos.Logica.Balances.Modelos.DirectorioCompania directorioCompania = null;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathBalances = Path.Combine(pathFuentes, ValidacionViewModel.ValidarCedula(identificacionOriginal) ? "balancesPersonaDemo.json" : "balancesDemo.json");
                datos = JsonConvert.DeserializeObject<BalancesViewModel>(System.IO.File.ReadAllText(pathBalances));

                _logger.LogInformation("Fuente de Balances procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Balances. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuenteBalance = new[] { Dominio.Tipos.Fuentes.Balance, Dominio.Tipos.Fuentes.Balances, Dominio.Tipos.Fuentes.DirectorioCompanias, Dominio.Tipos.Fuentes.AnalisisHorizontal, Dominio.Tipos.Fuentes.RepresentantesEmpresas, Dominio.Tipos.Fuentes.Accionistas, Dominio.Tipos.Fuentes.EmpresasAccionista };
                        var historialesBalance = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuenteBalance.Contains(m.TipoFuente));
                        var historialBalance = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Balance);
                        var historialBalances = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Balances);
                        var historialDirectorio = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DirectorioCompanias);
                        var historialRepresentantesEmpresas = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.RepresentantesEmpresas);
                        var historialEmpresasAccionista = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasAccionista);
                        var historialAnalisis = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.AnalisisHorizontal);
                        var historialAccionistas = historialesBalance.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Accionistas);

                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);

                        if (historialBalance != null && (!historialBalance.Generado || !busquedaNuevaBalance) && !busquedaNoJuridico)
                        {
                            historialBalance.IdHistorial = modelo.IdHistorial;
                            historialBalance.TipoFuente = Dominio.Tipos.Fuentes.Balances;
                            historialBalance.Generado = datos.Balances != null && datos.Balances.Any();
                            historialBalance.Data = datos.Balances != null && datos.Balances.Any() ? JsonConvert.SerializeObject(datos.Balances) : null;
                            historialBalance.Cache = cacheBalance;
                            historialBalance.FechaRegistro = DateTime.Now;
                            historialBalance.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialBalance);
                            _logger.LogInformation("Historial de la Fuente Balance actualizado correctamente");
                        }
                        else if ((historialBalance == null || historialBalances == null) && !busquedaNoJuridico)
                        {
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
                        }

                        if (historialDirectorio != null && (!historialDirectorio.Generado || !busquedaNuevaBalance) && !busquedaNoJuridico)
                        {
                            historialDirectorio.IdHistorial = modelo.IdHistorial;
                            historialDirectorio.TipoFuente = Dominio.Tipos.Fuentes.DirectorioCompanias;
                            historialDirectorio.Generado = datos.DirectorioCompania != null;
                            historialDirectorio.Data = datos.DirectorioCompania != null ? JsonConvert.SerializeObject(datos.DirectorioCompania) : null;
                            historialDirectorio.Cache = cacheBalance;
                            historialDirectorio.FechaRegistro = DateTime.Now;
                            historialDirectorio.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialDirectorio);
                            _logger.LogInformation("Historial de la Fuente Directorio Compañias actualizado correctamente");
                        }
                        else if (historialDirectorio == null && !busquedaNoJuridico)
                        {
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
                        }

                        if (historialAnalisis != null && (!historialAnalisis.Generado || !busquedaNuevaBalance) && !busquedaNoJuridico)
                        {
                            historialAnalisis.IdHistorial = modelo.IdHistorial;
                            historialAnalisis.TipoFuente = Dominio.Tipos.Fuentes.AnalisisHorizontal;
                            historialAnalisis.Generado = datos.AnalisisHorizontal != null;
                            historialAnalisis.Data = datos.AnalisisHorizontal != null ? JsonConvert.SerializeObject(datos.AnalisisHorizontal) : null;
                            historialAnalisis.Cache = cacheBalance;
                            historialAnalisis.FechaRegistro = DateTime.Now;
                            historialAnalisis.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAnalisis);
                            _logger.LogInformation("Historial de la Fuente Análisis Horizontal actualizado correctamente");
                        }
                        else if (historialAnalisis == null && !busquedaNoJuridico)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.AnalisisHorizontal,
                                Generado = datos.AnalisisHorizontal != null,
                                Data = datos.AnalisisHorizontal != null ? JsonConvert.SerializeObject(datos.AnalisisHorizontal) : null,
                                Cache = cacheBalance,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Análisis Horizontal procesado correctamente");
                        }

                        if (historialRepresentantesEmpresas != null && (!historialRepresentantesEmpresas.Generado || !busquedaNuevaBalance))
                        {
                            historialRepresentantesEmpresas.IdHistorial = modelo.IdHistorial;
                            historialRepresentantesEmpresas.TipoFuente = Dominio.Tipos.Fuentes.RepresentantesEmpresas;
                            historialRepresentantesEmpresas.Generado = datos.RepresentantesEmpresas != null;
                            historialRepresentantesEmpresas.Data = datos.RepresentantesEmpresas != null && datos.RepresentantesEmpresas.Any() ? JsonConvert.SerializeObject(datos.RepresentantesEmpresas) : null;
                            historialRepresentantesEmpresas.Cache = cacheBalance;
                            historialRepresentantesEmpresas.FechaRegistro = DateTime.Now;
                            historialRepresentantesEmpresas.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialRepresentantesEmpresas);
                            _logger.LogInformation("Historial de la Fuente Representantes empresas actualizado correctamente");
                        }
                        else if (historialRepresentantesEmpresas == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.RepresentantesEmpresas,
                                Generado = datos.RepresentantesEmpresas != null,
                                Data = datos.RepresentantesEmpresas != null && datos.RepresentantesEmpresas.Any() ? JsonConvert.SerializeObject(datos.RepresentantesEmpresas) : null,
                                Cache = cacheBalance,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Representantes empresas procesado correctamente");
                        }

                        if (historialAccionistas != null && (!historialAccionistas.Generado || !busquedaNuevaBalance) && !busquedaNoJuridico)
                        {
                            historialAccionistas.IdHistorial = modelo.IdHistorial;
                            historialAccionistas.TipoFuente = Dominio.Tipos.Fuentes.Accionistas;
                            historialAccionistas.Generado = datos.Accionistas != null;
                            historialAccionistas.Data = datos.Accionistas != null ? JsonConvert.SerializeObject(datos.Accionistas) : null;
                            historialAccionistas.Cache = cacheAccionistas;
                            historialAccionistas.FechaRegistro = DateTime.Now;
                            historialAccionistas.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAccionistas);
                            _logger.LogInformation("Historial de la Fuente Accionistas Compañias actualizado correctamente");
                        }
                        else if (historialAccionistas == null && !busquedaNoJuridico)
                        {
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
                        }

                        if (historialEmpresasAccionista != null && (!historialEmpresasAccionista.Generado || !busquedaNuevaBalance))
                        {
                            historialEmpresasAccionista.IdHistorial = modelo.IdHistorial;
                            historialEmpresasAccionista.TipoFuente = Dominio.Tipos.Fuentes.EmpresasAccionista;
                            historialEmpresasAccionista.Generado = datos.EmpresasAccionista != null;
                            historialEmpresasAccionista.Data = datos.EmpresasAccionista != null && datos.EmpresasAccionista.Any() ? JsonConvert.SerializeObject(datos.EmpresasAccionista) : null;
                            historialEmpresasAccionista.Cache = cacheBalance;
                            historialEmpresasAccionista.FechaRegistro = DateTime.Now;
                            historialEmpresasAccionista.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialEmpresasAccionista);
                            _logger.LogInformation("Historial de la Fuente Empresas Accionista actualizado correctamente");
                        }
                        else if (historialEmpresasAccionista == null)
                        {
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
                        }

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

                return PartialView("../Shared/Fuentes/_FuenteBalances", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteBalances", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteIESS")]
        public async Task<IActionResult> ObtenerReporteIESS(ReporteViewModel modelo)
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

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new IessViewModel();
                var busquedaNuevaIess = false;
                var busquedaNuevaAfiliado = false;
                var busquedaNuevaAfiliadoAdicional = false;
                var busquedaNuevaEmpresaEmpleados = false;
                var cacheIess = false;
                var cacheAfiliado = false;
                var cacheAfiliadoAdicional = false;
                var cacheEmpresaEmpleados = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathIess = Path.Combine(pathFuentes, "iessDemo.json");
                var archivo = System.IO.File.ReadAllText(pathIess);
                datos = JsonConvert.DeserializeObject<IessViewModel>(archivo);
                datos.EmpresaConfiable = true;
                ViewBag.TipoFuente = 1;

                try
                {
                    byte[] pdf = null;
                    //var pdf = await _consulta.ObtenerReportePdf(datos.Afiliado.Reporte);
                    if (pdf != null && pdf.Length > 0)
                    {
                        var filePath = Path.GetTempFileName();
                        ViewBag.RutaArchivo = filePath;
                        System.IO.File.WriteAllBytes(filePath, pdf);
                    }
                    datos.Afiliado.Reporte = null;
                }
                catch (Exception ex)
                {
                    ViewBag.RutaArchivo = string.Empty;
                    _logger.LogError($"Error al registrar certificado de afiliación {modelo.Identificacion}: {ex.Message}");
                }

                try
                {
                    if (datos != null && datos.Afiliado != null && !string.IsNullOrEmpty(datos.Afiliado.Empresa))
                    {
                        var datosEmpresa = datos.Afiliado.Empresa;
                        var empresas = Regex.Matches(datosEmpresa, @": (\w+).")?.Select(m => m.ToString().Replace(":", "").Replace(".", "").Trim()).ToArray();
                        if (empresas != null && empresas.Any())
                        {
                            foreach (var item in empresas)
                            {
                                if (!string.IsNullOrEmpty(item) && item.Length >= 10)
                                {
                                    datosEmpresa = datosEmpresa.Replace(item, $"<a class=\"btnConsultarIdentificacion\" data-tippy-content=\"Consultar empresa\" target=\"_blank\"  href='{Url.Action("Inicio", "Principal", new { Area = "Consultas", Identificacion = item })}'>{item}</a>");
                                }
                            }
                            ViewBag.EmpresasFormato = datosEmpresa;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                _logger.LogInformation("Fuente de IESS procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente IESS. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesIess = new[] { Dominio.Tipos.Fuentes.Iess, Dominio.Tipos.Fuentes.Afiliado, Dominio.Tipos.Fuentes.AfiliadoAdicional, Dominio.Tipos.Fuentes.IessEmpresaEmpleados };
                        var historialesIess = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesIess.Contains(m.TipoFuente));
                        var historialObligacion = historialesIess.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Iess);
                        var historialAfiliado = historialesIess.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado);
                        var historialAfiliadoAdicional = historialesIess.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.AfiliadoAdicional);
                        var historialEmpresaEmpleados = historialesIess.FirstOrDefault(m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.IessEmpresaEmpleados);
                        if (historialObligacion != null && (!historialObligacion.Generado || !busquedaNuevaIess))
                        {
                            historialObligacion.IdHistorial = modelo.IdHistorial;
                            historialObligacion.TipoFuente = Dominio.Tipos.Fuentes.Iess;
                            historialObligacion.Generado = datos.Iess != null;
                            historialObligacion.Data = datos.Iess != null ? JsonConvert.SerializeObject(datos.Iess) : null;
                            historialObligacion.Cache = cacheIess;
                            historialObligacion.FechaRegistro = DateTime.Now;
                            historialObligacion.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialObligacion);
                            _logger.LogInformation("Historial de la Fuente Certificado Obligación actualizado correctamente");
                        }
                        else if (historialObligacion == null)
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
                        }

                        if (historialAfiliado != null && (!historialAfiliado.Generado || !busquedaNuevaAfiliado))
                        {
                            historialAfiliado.IdHistorial = modelo.IdHistorial;
                            historialAfiliado.TipoFuente = Dominio.Tipos.Fuentes.Afiliado;
                            historialAfiliado.Generado = datos.Afiliado != null;
                            historialAfiliado.Data = datos.Afiliado != null ? JsonConvert.SerializeObject(datos.Afiliado) : null;
                            historialAfiliado.Cache = cacheAfiliado;
                            historialAfiliado.FechaRegistro = DateTime.Now;
                            historialAfiliado.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAfiliado);
                            _logger.LogInformation("Historial de la Fuente Certificado Afiliación actualizado correctamente");
                        }
                        else if (historialAfiliado == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.Afiliado,
                                Generado = datos.Afiliado != null,
                                Data = datos.Afiliado != null ? JsonConvert.SerializeObject(datos.Afiliado) : null,
                                Cache = cacheAfiliado,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }

                        if (historialAfiliadoAdicional != null && (!historialAfiliadoAdicional.Generado || !busquedaNuevaAfiliadoAdicional))
                        {
                            historialAfiliadoAdicional.IdHistorial = modelo.IdHistorial;
                            historialAfiliadoAdicional.TipoFuente = Dominio.Tipos.Fuentes.AfiliadoAdicional;
                            historialAfiliadoAdicional.Generado = datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any();
                            historialAfiliadoAdicional.Data = datos.AfiliadoAdicional != null && datos.AfiliadoAdicional.Any() ? JsonConvert.SerializeObject(datos.AfiliadoAdicional) : null;
                            historialAfiliadoAdicional.Cache = cacheAfiliadoAdicional;
                            historialAfiliadoAdicional.FechaRegistro = DateTime.Now;
                            historialAfiliadoAdicional.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAfiliadoAdicional);
                            _logger.LogInformation("Historial de la Fuente Afiliado Adicional actualizado correctamente");
                        }
                        else if (historialAfiliadoAdicional == null)
                        {
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
                        }

                        if (historialEmpresaEmpleados != null && (!historialEmpresaEmpleados.Generado || !busquedaNuevaEmpresaEmpleados))
                        {
                            historialEmpresaEmpleados.IdHistorial = modelo.IdHistorial;
                            historialEmpresaEmpleados.TipoFuente = Dominio.Tipos.Fuentes.IessEmpresaEmpleados;
                            historialEmpresaEmpleados.Generado = datos.EmpleadosEmpresa != null && datos.EmpleadosEmpresa.Any();
                            historialEmpresaEmpleados.Data = datos.EmpleadosEmpresa != null && datos.EmpleadosEmpresa.Any() ? JsonConvert.SerializeObject(datos.EmpleadosEmpresa) : null;
                            historialEmpresaEmpleados.Cache = cacheEmpresaEmpleados;
                            historialEmpresaEmpleados.FechaRegistro = DateTime.Now;
                            historialEmpresaEmpleados.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialEmpresaEmpleados);
                            _logger.LogInformation("Historial de la Fuente Afiliado Adicional actualizado correctamente");
                        }
                        else if (historialEmpresaEmpleados == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.IessEmpresaEmpleados,
                                Generado = datos.EmpleadosEmpresa != null && datos.EmpleadosEmpresa.Any(),
                                Data = datos.EmpleadosEmpresa != null && datos.EmpleadosEmpresa.Any() ? JsonConvert.SerializeObject(datos.EmpleadosEmpresa) : null,
                                Cache = cacheEmpresaEmpleados,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }
                        _logger.LogInformation("Historial de la Fuente IESS procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");

#if !DEBUG
                    if (!idEmpresasSalarios.Contains(usuarioActual.IdEmpresa))
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
#endif
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView("../Shared/Fuentes/_FuenteIess", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteIess", null);
            }
        }

        private async Task<Externos.Logica.IESS.Modelos.Afiliacion> ObtenerRAfiliacion(ReporteViewModel modelo, string identificacion, int? tipoFuente)
        {
            try
            {
                Externos.Logica.IESS.Modelos.Afiliacion r_afiliacion = null;
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
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficial(identificacion, fechaNacimiento.Value);
                            ViewBag.TipoFuente = 1;
                        }
                        break;
                    case 2:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsync(identificacion);
                        ViewBag.TipoFuente = 2;
                        break;
                    case 3:
                        if (fechaNacimiento.HasValue)
                        {
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficial(identificacion, fechaNacimiento.Value);
                            ViewBag.TipoFuente = 1;
                        }
                        if (r_afiliacion == null)
                        {
                            r_afiliacion = await _iess.GetAfiliacionCertificadoAsync(identificacion);
                            ViewBag.TipoFuente = 2;
                        }
                        break;
                    case 4:
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsync(identificacion);
                        ViewBag.TipoFuente = 2;
                        if (r_afiliacion == null && fechaNacimiento.HasValue)
                        {
                            r_afiliacion = _iess.GetCertificadoAfiliacionOficial(identificacion, fechaNacimiento.Value);
                            ViewBag.TipoFuente = 1;
                        }
                        break;
                    default:
                        ViewBag.TipoFuente = 2;
                        r_afiliacion = await _iess.GetAfiliacionCertificadoAsync(identificacion);
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

        [HttpPost]
        [Route("ObtenerReporteSenescyt")]
        public async Task<IActionResult> ObtenerReporteSenescyt(ReporteViewModel modelo)
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
                var busquedaNuevaSenescyt = false;
                var cacheSenescyt = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSenecyt = Path.Combine(pathFuentes, "senescytDemo.json");
                var archivo = System.IO.File.ReadAllText(pathSenecyt);
                datos = JsonConvert.DeserializeObject<SenescytViewModel>(archivo);

                _logger.LogInformation("Fuente de Senescyt procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Senescyt. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialSenescyt = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt);
                        if (historialSenescyt != null)
                        {
                            if (!historialSenescyt.Generado || !busquedaNuevaSenescyt)
                            {
                                historialSenescyt.IdHistorial = modelo.IdHistorial;
                                historialSenescyt.TipoFuente = Dominio.Tipos.Fuentes.Senescyt;
                                historialSenescyt.Generado = r_senescyt != null;
                                historialSenescyt.Data = r_senescyt != null ? JsonConvert.SerializeObject(r_senescyt) : null;
                                historialSenescyt.Cache = cacheSenescyt;
                                historialSenescyt.FechaRegistro = DateTime.Now;
                                historialSenescyt.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSenescyt);
                                _logger.LogInformation("Historial de la Fuente Senescyt actualizado correctamente");
                            }
                        }
                        else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteSenescyt", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteSenescyt", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteLegal")]
        public async Task<IActionResult> ObtenerReporteLegal(ReporteViewModel modelo)
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
                var datos = new JudicialViewModel();
                var busquedaNuevaLegal = false;
                var cacheLegal = false;
                var cacheLegalEmpresa = false;
                var validarFuente = true;
                var empresasConsultaPersonalizada = new List<int>();
                ViewBag.FuenteImpedimento = false;
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

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathLegal = Path.Combine(pathFuentes, "legalDemo.json");
                var archivo = System.IO.File.ReadAllText(pathLegal);
                datos = JsonConvert.DeserializeObject<JudicialViewModel>(archivo);

                _logger.LogInformation("Fuente de Legal procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Legal. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesJudicial = new[] { Dominio.Tipos.Fuentes.FJudicial, Dominio.Tipos.Fuentes.FJEmpresa };
                        var historialesJudicial = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesJudicial.Contains(m.TipoFuente));
                        var historialJudicial = historialesJudicial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial);
                        var historialEmpresa = historialesJudicial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FJEmpresa);

                        if (historialJudicial != null && (!historialJudicial.Generado || !busquedaNuevaLegal))
                        {
                            historialJudicial.IdHistorial = modelo.IdHistorial;
                            historialJudicial.TipoFuente = Dominio.Tipos.Fuentes.FJudicial;
                            historialJudicial.Generado = datos.FJudicial != null;
                            historialJudicial.Data = datos.FJudicial != null ? JsonConvert.SerializeObject(datos.FJudicial) : null;
                            historialJudicial.Cache = cacheLegal;
                            historialJudicial.FuenteActiva = validarFuente;
                            historialJudicial.FechaRegistro = DateTime.Now;
                            historialJudicial.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialJudicial);
                            _logger.LogInformation("Historial de la Fuente Judicial Persona actualizado correctamente");
                        }
                        else if (historialJudicial == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.FJudicial,
                                Generado = datos.FJudicial != null,
                                Data = datos.FJudicial != null ? JsonConvert.SerializeObject(datos.FJudicial) : null,
                                Cache = cacheLegal,
                                FuenteActiva = validarFuente,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }

                        if (historialEmpresa != null && (!historialEmpresa.Generado || !busquedaNuevaLegal))
                        {
                            historialEmpresa.IdHistorial = modelo.IdHistorial;
                            historialEmpresa.TipoFuente = Dominio.Tipos.Fuentes.FJEmpresa;
                            historialEmpresa.Generado = datos.FJEmpresa != null;
                            historialEmpresa.Data = datos.FJEmpresa != null ? JsonConvert.SerializeObject(datos.FJEmpresa) : null;
                            historialEmpresa.Cache = cacheLegalEmpresa;
                            historialEmpresa.FuenteActiva = validarFuente;
                            historialEmpresa.FechaRegistro = DateTime.Now;
                            historialEmpresa.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialEmpresa);
                            _logger.LogInformation("Historial de la Fuente Judicial Empresa actualizado correctamente");
                        }
                        else if (historialEmpresa == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.FJEmpresa,
                                Generado = datos.FJEmpresa != null,
                                Data = datos.FJEmpresa != null ? JsonConvert.SerializeObject(datos.FJEmpresa) : null,
                                Cache = cacheLegalEmpresa,
                                FuenteActiva = validarFuente,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }
                        _logger.LogInformation("Historial de la Fuente Legal procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteFJudicial", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteFJudicial", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteImpedimento")]
        public async Task<IActionResult> ObtenerReporteImpedimento(ReporteViewModel modelo)
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
                Externos.Logica.FJudicial.Modelos.Impedimento impedimento = null;
                var cacheImpedimento = false;
                var busquedaImpedimento = false;
                var rutaArchivo = string.Empty;
                ViewBag.FuenteImpedimento = true;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathImpedimento = Path.Combine(pathFuentes, "impedimentoDemo.json");
                impedimento = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Impedimento>(System.IO.File.ReadAllText(pathImpedimento));

                if (impedimento != null && impedimento.Reporte != null && impedimento.Reporte.Length > 0)
                {
                    try
                    {
                        var filePath = Path.GetTempFileName();
                        ViewBag.RutaArchivoImpedimento = filePath;
                        System.IO.File.WriteAllBytes(filePath, impedimento.Reporte);
                    }
                    catch (Exception ex)
                    {
                        ViewBag.RutaArchivoImpedimento = string.Empty;
                        _logger.LogError($"Error al registrar certificado de Impedimento {modelo.Identificacion}: {ex.Message}");
                    }
                    impedimento.Reporte = null;
                }

                _logger.LogInformation("Fuente de Legal Impedimento procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Legal Impedimento. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialImpedimento = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Impedimento);
                        if (historialImpedimento != null && (!historialImpedimento.Generado || !busquedaImpedimento))
                        {
                            historialImpedimento.IdHistorial = modelo.IdHistorial;
                            historialImpedimento.TipoFuente = Dominio.Tipos.Fuentes.Impedimento;
                            historialImpedimento.Generado = impedimento != null;
                            historialImpedimento.Data = impedimento != null ? JsonConvert.SerializeObject(impedimento) : null;
                            historialImpedimento.Cache = cacheImpedimento;
                            historialImpedimento.FechaRegistro = DateTime.Now;
                            historialImpedimento.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialImpedimento);
                            _logger.LogInformation("Historial de la Fuente Legal Impedimento se actualizado correctamente");
                        }
                        else if (historialImpedimento == null)
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView("../Shared/Fuentes/_FuenteImpedimento", new JudicialViewModel() { Impedimento = impedimento, BusquedaNuevaImpedimento = busquedaImpedimento, RutaArchivo = rutaArchivo });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteImpedimento", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteANT")]
        public async Task<IActionResult> ObtenerReporteANT(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                //List<Externos.Logica.ANT.Modelos.AutoHistorico> autosHistorico = null;
                var datos = new ANTViewModel();
                Historial historialTemp = null;
                var busquedaNuevaAnt = false;
                var cacheAnt = false;
                //var cacheHistoricoAutos = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathAnt = Path.Combine(pathFuentes, "antDemo.json");
                var archivo = System.IO.File.ReadAllText(pathAnt);
                var licencia = JsonConvert.DeserializeObject<ANTViewModel>(archivo);
                datos = new ANTViewModel()
                {
                    HistorialCabecera = historialTemp,
                    Licencia = licencia.Licencia,
                    BusquedaNueva = busquedaNuevaAnt,
                    //AutosHistorico = licencia.AutosHistorico
                };

                _logger.LogInformation("Fuente de ANT procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente ANT. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesAnt = new[] { Dominio.Tipos.Fuentes.Ant, Dominio.Tipos.Fuentes.AutoHistorico };
                        var historialesAnt = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesAnt.Contains(m.TipoFuente));
                        var historialAnt = historialesAnt.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ant);
                        //var historialAutosHistorico = historialesAnt.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.AutoHistorico);
                        if (historialAnt != null && (!historialAnt.Generado || !busquedaNuevaAnt))
                        {
                            historialAnt.IdHistorial = modelo.IdHistorial;
                            historialAnt.TipoFuente = Dominio.Tipos.Fuentes.Ant;
                            historialAnt.Generado = datos.Licencia != null;
                            historialAnt.Data = datos.Licencia != null ? JsonConvert.SerializeObject(datos.Licencia) : null;
                            historialAnt.Cache = cacheAnt;
                            historialAnt.FechaRegistro = DateTime.Now;
                            historialAnt.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAnt);
                            _logger.LogInformation("Historial de la Fuente ANT actualizado correctamente");
                        }
                        else if (historialAnt == null)
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
                        }
                        _logger.LogInformation("Historial de la Fuente ANT procesado correctamente");

                        //if (historialAutosHistorico != null && (!historialAutosHistorico.Generado || !busquedaNuevaAnt))
                        //{
                        //    historialAutosHistorico.IdHistorial = modelo.IdHistorial;
                        //    historialAutosHistorico.TipoFuente = Dominio.Tipos.Fuentes.AutoHistorico;
                        //    historialAutosHistorico.Generado = datos.AutosHistorico != null;
                        //    historialAutosHistorico.Data = datos.AutosHistorico != null ? JsonConvert.SerializeObject(datos.AutosHistorico) : null;
                        //    historialAutosHistorico.Cache = cacheHistoricoAutos;
                        //    historialAutosHistorico.FechaRegistro = DateTime.Now;
                        //    historialAutosHistorico.Reintento = true;
                        //    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAutosHistorico);
                        //    _logger.LogInformation("Historial de la Fuente Autos Histórico actualizado correctamente");
                        //}
                        //else if (historialAutosHistorico == null)
                        //{
                        //    await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        //    {
                        //        IdHistorial = modelo.IdHistorial,
                        //        TipoFuente = Dominio.Tipos.Fuentes.AutoHistorico,
                        //        Generado = datos.AutosHistorico != null,
                        //        Data = datos.AutosHistorico != null ? JsonConvert.SerializeObject(datos.AutosHistorico) : null,
                        //        Cache = cacheHistoricoAutos,
                        //        FechaRegistro = DateTime.Now,
                        //        Reintento = false
                        //    });
                        //}
                        //_logger.LogInformation("Historial de la Fuente  Autos Histórico procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteANT", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteANT", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteSERCOP")]
        public async Task<IActionResult> ObtenerReporteSERCOP(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new SERCOPViewModel();
                var busquedaNuevaSercop = false;
                var cacheSercop = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSercop = Path.Combine(pathFuentes, "sercopDemo.json");
                var archivo = System.IO.File.ReadAllText(pathSercop);
                datos = JsonConvert.DeserializeObject<SERCOPViewModel>(archivo);

                _logger.LogInformation("Fuente de SERCOP procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SERCOP. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesSercop = new[] { Dominio.Tipos.Fuentes.Proveedor, Dominio.Tipos.Fuentes.ProveedorContraloria };
                        var historialesSercop = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesSercop.Contains(m.TipoFuente));
                        var historialProveedor = historialesSercop.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Proveedor);
                        var historialContraloria = historialesSercop.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ProveedorContraloria);

                        if (historialProveedor != null && (!historialProveedor.Generado || !busquedaNuevaSercop))
                        {
                            historialProveedor.IdHistorial = modelo.IdHistorial;
                            historialProveedor.TipoFuente = Dominio.Tipos.Fuentes.Proveedor;
                            historialProveedor.Generado = datos.Proveedor != null;
                            historialProveedor.Data = datos.Proveedor != null ? JsonConvert.SerializeObject(datos.Proveedor) : null;
                            historialProveedor.Cache = cacheSercop;
                            historialProveedor.FechaRegistro = DateTime.Now;
                            historialProveedor.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialProveedor);
                            _logger.LogInformation("Historial de la Fuente Sercop actualizado correctamente");
                        }
                        else if (historialProveedor == null)
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
                        }

                        if (historialContraloria != null && (!historialContraloria.Generado || !busquedaNuevaSercop))
                        {
                            historialContraloria.IdHistorial = modelo.IdHistorial;
                            historialContraloria.TipoFuente = Dominio.Tipos.Fuentes.ProveedorContraloria;
                            historialContraloria.Generado = datos.ProveedorContraloria != null;
                            historialContraloria.Data = datos.ProveedorContraloria != null ? JsonConvert.SerializeObject(datos.ProveedorContraloria) : null;
                            historialContraloria.Cache = cacheSercop;
                            historialContraloria.FechaRegistro = DateTime.Now;
                            historialContraloria.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContraloria);
                            _logger.LogInformation("Historial de la Fuente Sercop Contraloria actualizado correctamente");
                        }
                        else if (historialContraloria == null)
                        {
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
                        }
                        _logger.LogInformation("Historial de la Fuente SERCOP procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView("../Shared/Fuentes/_FuenteSercop", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteSercop", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePensionAlimenticia")]
        public async Task<IActionResult> ObtenerReportePensionAlimenticia(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PensionAlimenticiaViewModel();
                var busquedaNuevaPension = false;
                var cachePension = false;
                var validarFuente = true;
                var historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathAnt = Path.Combine(pathFuentes, "pensionAlimenticiaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathAnt);
                var pension = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(archivo);
                datos = new PensionAlimenticiaViewModel()
                {
                    HistorialCabecera = historialTemp,
                    PensionAlimenticia = pension,
                    BusquedaNueva = busquedaNuevaPension
                };

                _logger.LogInformation("Fuente de Pension alimenticia procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Pension alimenticia. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPension = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia);
                        if (historialPension != null && (!historialPension.Generado || !busquedaNuevaPension))
                        {
                            historialPension.IdHistorial = modelo.IdHistorial;
                            historialPension.TipoFuente = Dominio.Tipos.Fuentes.PensionAlimenticia;
                            historialPension.Generado = datos.PensionAlimenticia != null;
                            historialPension.Data = datos.PensionAlimenticia != null ? JsonConvert.SerializeObject(datos.PensionAlimenticia) : null;
                            historialPension.Cache = cachePension;
                            historialPension.FuenteActiva = validarFuente;
                            historialPension.FechaRegistro = DateTime.Now;
                            historialPension.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPension);
                            _logger.LogInformation("Historial de la Fuente Pension alimenticia actualizado correctamente");
                        }
                        else if (historialPension == null)
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PensionAlimenticia,
                                Generado = datos.PensionAlimenticia != null,
                                Data = datos.PensionAlimenticia != null ? JsonConvert.SerializeObject(datos.PensionAlimenticia) : null,
                                Cache = cachePension,
                                FuenteActiva = validarFuente,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                        }
                        _logger.LogInformation("Historial de la Fuente Pension alimenticia procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePensionAlimenticia", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePensionAlimenticia", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteSuperBancos")]
        public async Task<IActionResult> ObtenerReporteSuperBancos(ReporteViewModel modelo)
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
                ViewBag.RutaArchivoCedula = string.Empty;
                ViewBag.RutaArchivoNatural = string.Empty;
                ViewBag.RutaArchivoEmpresa = string.Empty;
                ViewBag.MsjErrorSuperBancos = string.Empty;
                var datos = new SuperBancosViewModel();
                var busquedaNuevaSuperBancosCedula = false;
                var busquedaNuevaSuperBancosNatural = false;
                var busquedaNuevaSuperBancosEmpresa = false;
                var cacheSuperBancosCedula = false;
                var cacheSuperBancosNatural = false;
                var cacheSuperBancosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSuperBancos = Path.Combine(pathFuentes, "superBancosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathSuperBancos);
                datos = JsonConvert.DeserializeObject<SuperBancosViewModel>(archivo);

                if (datos.SuperBancos != null)
                {
                    try
                    {
                        var filePath = Path.GetTempFileName();
                        ViewBag.RutaArchivoCedula = filePath;
                        ViewBag.RutaArchivoNatural = filePath;
                        ViewBag.RutaArchivoEmpresa = filePath;
                        System.IO.File.WriteAllBytes(filePath, datos.SuperBancos.Reporte);
                        datos.SuperBancos.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        ViewBag.RutaArchivo = string.Empty;
                        _logger.LogError($"Error al registrar certificado de super de bancos {modelo.Identificacion}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Fuente de SuperBancos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SuperBancos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesCivil = new[] { Dominio.Tipos.Fuentes.SuperBancos, Dominio.Tipos.Fuentes.SuperBancosNatural, Dominio.Tipos.Fuentes.SuperBancosEmpresa };
                        var historialesSuperBancos = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesCivil.Contains(m.TipoFuente));
                        var historialSuperBancosCedula = historialesSuperBancos.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos);
                        var historialSuperBancosNatural = historialesSuperBancos.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural);
                        var historialSuperBancosEmpresa = historialesSuperBancos.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa);

                        if (historialSuperBancosCedula != null)
                        {
                            if (!historialSuperBancosCedula.Generado || !busquedaNuevaSuperBancosCedula)
                            {
                                historialSuperBancosCedula.IdHistorial = modelo.IdHistorial;
                                historialSuperBancosCedula.TipoFuente = Dominio.Tipos.Fuentes.SuperBancos;
                                historialSuperBancosCedula.Generado = r_superBancosCedula != null;
                                historialSuperBancosCedula.Data = r_superBancosCedula != null ? JsonConvert.SerializeObject(r_superBancosCedula) : null;
                                historialSuperBancosCedula.Cache = cacheSuperBancosCedula;
                                historialSuperBancosCedula.FechaRegistro = DateTime.Now;
                                historialSuperBancosCedula.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSuperBancosCedula);
                                _logger.LogInformation("Historial de la Fuente SuperBancos actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.SuperBancos,
                                Generado = datos.SuperBancos != null,
                                Data = datos.SuperBancos != null ? JsonConvert.SerializeObject(datos.SuperBancos) : null,
                                Cache = cacheSuperBancosCedula,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente SuperBancos procesado correctamente");
                        }

                        if (historialSuperBancosNatural != null)
                        {
                            if (!historialSuperBancosNatural.Generado || !busquedaNuevaSuperBancosNatural)
                            {
                                historialSuperBancosNatural.IdHistorial = modelo.IdHistorial;
                                historialSuperBancosNatural.TipoFuente = Dominio.Tipos.Fuentes.SuperBancosNatural;
                                historialSuperBancosNatural.Generado = r_superBancosNatural != null;
                                historialSuperBancosNatural.Data = r_superBancosNatural != null ? JsonConvert.SerializeObject(r_superBancosNatural) : null;
                                historialSuperBancosNatural.Cache = cacheSuperBancosNatural;
                                historialSuperBancosNatural.FechaRegistro = DateTime.Now;
                                historialSuperBancosNatural.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSuperBancosNatural);
                                _logger.LogInformation("Historial de la Fuente SuperBancos Natural actualizado correctamente");
                            }
                        }
                        else
                        {
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
                        }

                        if (historialSuperBancosEmpresa != null)
                        {
                            if (!historialSuperBancosEmpresa.Generado || !busquedaNuevaSuperBancosEmpresa)
                            {
                                historialSuperBancosEmpresa.IdHistorial = modelo.IdHistorial;
                                historialSuperBancosEmpresa.TipoFuente = Dominio.Tipos.Fuentes.SuperBancosEmpresa;
                                historialSuperBancosEmpresa.Generado = r_superBancosEmpresa != null;
                                historialSuperBancosEmpresa.Data = r_superBancosEmpresa != null ? JsonConvert.SerializeObject(r_superBancosEmpresa) : null;
                                historialSuperBancosEmpresa.Cache = cacheSuperBancosEmpresa;
                                historialSuperBancosEmpresa.FechaRegistro = DateTime.Now;
                                historialSuperBancosEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSuperBancosEmpresa);
                                _logger.LogInformation("Historial de la Fuente SuperBancos Juridico actualizado correctamente");
                            }
                        }
                        else
                        {
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteSuperBancos", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteSuperBancos", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteSuperBancosCedula")]
        public async Task<IActionResult> ObtenerReporteSuperBancosCedula(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosCedula = null;
                var busquedaNuevaSuperBancosCedula = false;
                var cacheSuperBancosCedula = false;
                var rutaArchivo = string.Empty;
                ViewBag.MsjErrorSuperBancos = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSuperBancos = Path.Combine(pathFuentes, "superBancosIndividualDemo.json");
                var archivo = System.IO.File.ReadAllText(pathSuperBancos);
                r_superBancosCedula = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(archivo);

                if (r_superBancosCedula != null)
                {
                    try
                    {
                        var filePath = Path.GetTempFileName();
                        rutaArchivo = filePath;
                        System.IO.File.WriteAllBytes(filePath, r_superBancosCedula.Reporte);
                        r_superBancosCedula.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al registrar certificado de super de bancos {modelo.Identificacion}: {ex.Message}");
                    }
                }

                if (r_superBancosCedula == null)
                {
                    busquedaNuevaSuperBancosCedula = true;
                    var datosSuperBancos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                    if (datosSuperBancos != null)
                    {
                        cacheSuperBancosCedula = true;
                        r_superBancosCedula = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(datosSuperBancos);
                    }
                }

                _logger.LogInformation("Fuente de SuperBancos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SuperBancos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialSuperBancosCedula = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos);
                        if (historialSuperBancosCedula != null)
                        {
                            if (!historialSuperBancosCedula.Generado || !busquedaNuevaSuperBancosCedula)
                            {
                                historialSuperBancosCedula.IdHistorial = modelo.IdHistorial;
                                historialSuperBancosCedula.TipoFuente = Dominio.Tipos.Fuentes.SuperBancos;
                                historialSuperBancosCedula.Generado = r_superBancosCedula != null;
                                historialSuperBancosCedula.Data = r_superBancosCedula != null ? JsonConvert.SerializeObject(r_superBancosCedula) : null;
                                historialSuperBancosCedula.Cache = cacheSuperBancosCedula;
                                historialSuperBancosCedula.FechaRegistro = DateTime.Now;
                                historialSuperBancosCedula.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSuperBancosCedula);
                                _logger.LogInformation("Historial de la Fuente SuperBancos actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.SuperBancos,
                                Generado = r_superBancosCedula != null,
                                Data = r_superBancosCedula != null ? JsonConvert.SerializeObject(r_superBancosCedula) : null,
                                Cache = cacheSuperBancosCedula,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente SuperBancos procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionSuperBancos", new InformacionSuperBancosViewModel() { RutaArchivo = rutaArchivo, SuperBancos = r_superBancosCedula, TipoConsulta = 1, BusquedaNueva = busquedaNuevaSuperBancosCedula });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionSuperBancos", new InformacionSuperBancosViewModel() { });
            }
        }

        [HttpPost]
        [Route("ObtenerReporteSuperBancosNatural")]
        public async Task<IActionResult> ObtenerReporteSuperBancosNatural(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosNatural = null;
                var busquedaNuevaSuperBancosNatural = false;
                var cacheSuperBancosNatural = false;
                var rutaArchivo = string.Empty;
                ViewBag.MsjErrorSuperBancos = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSuperBancos = Path.Combine(pathFuentes, "superBancosIndividualDemo.json");
                var archivo = System.IO.File.ReadAllText(pathSuperBancos);
                r_superBancosNatural = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(archivo);

                if (r_superBancosNatural != null)
                {
                    try
                    {
                        var filePath = Path.GetTempFileName();
                        rutaArchivo = filePath;
                        System.IO.File.WriteAllBytes(filePath, r_superBancosNatural.Reporte);
                        r_superBancosNatural.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al registrar certificado de super de bancos {modelo.Identificacion}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Fuente de SuperBancos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SuperBancos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    var historialSuperBancosNatural = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural);
                    if (historialSuperBancosNatural != null)
                    {
                        if (!historialSuperBancosNatural.Generado || !busquedaNuevaSuperBancosNatural)
                        {
                            historialSuperBancosNatural.IdHistorial = modelo.IdHistorial;
                            historialSuperBancosNatural.TipoFuente = Dominio.Tipos.Fuentes.SuperBancosNatural;
                            historialSuperBancosNatural.Generado = r_superBancosNatural != null;
                            historialSuperBancosNatural.Data = r_superBancosNatural != null ? JsonConvert.SerializeObject(r_superBancosNatural) : null;
                            historialSuperBancosNatural.Cache = cacheSuperBancosNatural;
                            historialSuperBancosNatural.FechaRegistro = DateTime.Now;
                            historialSuperBancosNatural.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSuperBancosNatural);
                            _logger.LogInformation("Historial de la Fuente SuperBancos Natural actualizado correctamente");
                        }
                    }
                    else
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.SuperBancosNatural,
                            Generado = r_superBancosNatural != null,
                            Data = r_superBancosNatural != null ? JsonConvert.SerializeObject(r_superBancosNatural) : null,
                            Cache = cacheSuperBancosNatural,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente SuperBancos Natural procesado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionSuperBancos", new InformacionSuperBancosViewModel() { RutaArchivo = rutaArchivo, SuperBancos = r_superBancosNatural, TipoConsulta = 2, BusquedaNueva = busquedaNuevaSuperBancosNatural });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionSuperBancos", new InformacionSuperBancosViewModel() { });
            }
        }

        [HttpPost]
        [Route("ObtenerReporteSuperBancosEmpresa")]
        public async Task<IActionResult> ObtenerReporteSuperBancosEmpresa(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.SuperBancos.Modelos.Resultado r_superBancosEmpresa = null;
                var busquedaNuevaSuperBancosEmpresa = false;
                var cacheSuperBancosEmpresa = false;
                var rutaArchivo = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathSuperBancos = Path.Combine(pathFuentes, "superBancosIndividualDemo.json");
                var archivo = System.IO.File.ReadAllText(pathSuperBancos);
                r_superBancosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(archivo);

                if (r_superBancosEmpresa != null)
                {
                    try
                    {
                        var filePath = Path.GetTempFileName();
                        rutaArchivo = filePath;
                        System.IO.File.WriteAllBytes(filePath, r_superBancosEmpresa.Reporte);
                        r_superBancosEmpresa.Reporte = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al registrar certificado de super de bancos {modelo.Identificacion}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Fuente de SuperBancos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SuperBancos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    var historialSuperBancosEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa);
                    if (historialSuperBancosEmpresa != null)
                    {
                        if (!historialSuperBancosEmpresa.Generado || !busquedaNuevaSuperBancosEmpresa)
                        {
                            historialSuperBancosEmpresa.IdHistorial = modelo.IdHistorial;
                            historialSuperBancosEmpresa.TipoFuente = Dominio.Tipos.Fuentes.SuperBancosEmpresa;
                            historialSuperBancosEmpresa.Generado = r_superBancosEmpresa != null;
                            historialSuperBancosEmpresa.Data = r_superBancosEmpresa != null ? JsonConvert.SerializeObject(r_superBancosEmpresa) : null;
                            historialSuperBancosEmpresa.Cache = cacheSuperBancosEmpresa;
                            historialSuperBancosEmpresa.FechaRegistro = DateTime.Now;
                            historialSuperBancosEmpresa.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSuperBancosEmpresa);
                            _logger.LogInformation("Historial de la Fuente SuperBancos Juridico actualizado correctamente");
                        }
                    }
                    else
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.SuperBancosEmpresa,
                            Generado = r_superBancosEmpresa != null,
                            Data = r_superBancosEmpresa != null ? JsonConvert.SerializeObject(r_superBancosEmpresa) : null,
                            Cache = cacheSuperBancosEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente SuperBancos Juridico procesado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionSuperBancos", new InformacionSuperBancosViewModel() { RutaArchivo = rutaArchivo, SuperBancos = r_superBancosEmpresa, TipoConsulta = 3, BusquedaNueva = busquedaNuevaSuperBancosEmpresa });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionSuperBancos", new InformacionSuperBancosViewModel() { });
            }
        }

        [HttpPost]
        [Route("ObtenerReporteAntecedentesPenales")]
        public async Task<IActionResult> ObtenerReporteAntecedentesPenales(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.AntecedentesPenales.Modelos.Resultado r_antecedentes = null;
                ViewBag.RutaArchivo = string.Empty;
                var datos = new AntecedentesPenalesViewModel();
                var busquedaNuevaAntecedentes = false;
                var cacheAntecedentes = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathAntecedentes = Path.Combine(pathFuentes, "antecedentesDemo.json");
                var archivo = System.IO.File.ReadAllText(pathAntecedentes);
                datos = JsonConvert.DeserializeObject<AntecedentesPenalesViewModel>(archivo);

                try
                {
                    var filePath = Path.GetTempFileName();
                    ViewBag.RutaArchivo = filePath;
                    System.IO.File.WriteAllBytes(filePath, datos.Antecedentes.Reporte);
                    datos.Antecedentes.Reporte = null;
                }
                catch (Exception ex)
                {
                    ViewBag.RutaArchivo = string.Empty;
                    _logger.LogError($"Error al registrar certificado de antecedentes penales {modelo.Identificacion}: {ex.Message}");
                }

                _logger.LogInformation("Fuente de Antecedentes Penales procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Antecedentes Penales. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialAntecedentes = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.AntecedentesPenales);
                        if (historialAntecedentes != null)
                        {
                            if (!historialAntecedentes.Generado || !busquedaNuevaAntecedentes)
                            {
                                historialAntecedentes.IdHistorial = modelo.IdHistorial;
                                historialAntecedentes.TipoFuente = Dominio.Tipos.Fuentes.AntecedentesPenales;
                                historialAntecedentes.Generado = r_antecedentes != null;
                                historialAntecedentes.Data = r_antecedentes != null ? JsonConvert.SerializeObject(r_antecedentes) : null;
                                historialAntecedentes.Cache = cacheAntecedentes;
                                historialAntecedentes.FechaRegistro = DateTime.Now;
                                historialAntecedentes.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialAntecedentes);
                                _logger.LogInformation("Historial de la Fuente Antecedentes Penales actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.AntecedentesPenales,
                                Generado = datos.Antecedentes != null,
                                Data = datos.Antecedentes != null ? JsonConvert.SerializeObject(datos.Antecedentes) : null,
                                Cache = cacheAntecedentes,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Antecedentes Penales procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteAntecedentesPenales", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteAntecedentesPenales", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePredios")]
        public async Task<IActionResult> ObtenerReportePredios(ReporteViewModel modelo)
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
                var datos = new PrediosViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresa);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipio;
                                historialPredio.Generado = r_prediosRepresentante != null;
                                historialPredio.Data = r_prediosRepresentante != null ? JsonConvert.SerializeObject(r_prediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                    historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresa;
                                    historialPredioEmpresa.Generado = r_prediosEmpresa != null;
                                    historialPredioEmpresa.Data = r_prediosEmpresa != null ? JsonConvert.SerializeObject(r_prediosEmpresa) : null;
                                    historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                    historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                    historialPredioEmpresa.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePredios", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePredios", null);
            }
        }

        [Route("ObtenerReportePrediosRepresentante")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentante(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosViewModel>(archivo);
                datos = new InformacionPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante
                };

                _logger.LogInformation("Fuente de Predio Representante procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Representante. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipio;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Representante actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipio,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Representante procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPredios", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPredios", null);
            }
        }

        [Route("ObtenerReportePrediosEmpresa")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresa(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosViewModel>(archivo);
                datos = new InformacionPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa
                };

                _logger.LogInformation("Fuente de Predio Empresa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Empresa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresa);
                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresa;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresa,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
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
                return PartialView("../Shared/Fuentes/_FuenteInformacionPredios", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPredios", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteDetallePredios")]
        public async Task<IActionResult> ObtenerReporteDetallePredios(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                DetallePrediosViewModel resultado = null;
                var busquedaNuevaDetallePredios = false;
                var cacheDetallePredios = false;
                var rutaArchivo = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathDetallPredios = Path.Combine(pathFuentes, "detallePrediosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathDetallPredios);

                resultado = new DetallePrediosViewModel()
                {
                    Detalle = JsonConvert.DeserializeObject<List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm>>(archivo)
                };

                _logger.LogInformation("Fuente de Detalle de Predios procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Detalle de Predios. Id Historial: {modelo.IdHistorial}");

                try
                {
                    var historialDetallePredios = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DetallePredios);
                    if (historialDetallePredios != null)
                    {
                        if (!historialDetallePredios.Generado || !busquedaNuevaDetallePredios)
                        {
                            historialDetallePredios.IdHistorial = modelo.IdHistorial;
                            historialDetallePredios.TipoFuente = Dominio.Tipos.Fuentes.DetallePredios;
                            historialDetallePredios.Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any();
                            historialDetallePredios.Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null;
                            historialDetallePredios.Cache = cacheDetallePredios;
                            historialDetallePredios.FechaRegistro = DateTime.Now;
                            historialDetallePredios.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialDetallePredios);
                            _logger.LogInformation("Historial de la Fuente Detalle de Predios actualizado correctamente");
                        }
                    }
                    else
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.DetallePredios,
                            Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any(),
                            Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null,
                            Cache = cacheDetallePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Detalle de Predios procesado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteDetallePredios", resultado);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteDetallePredios", new DetallePrediosViewModel() { });
            }
        }

        [HttpPost]
        [Route("ObtenerReporteDetallePrediosEmpresa")]
        public async Task<IActionResult> ObtenerReporteDetallePrediosEmpresa(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                DetallePrediosViewModel resultado = null;
                var busquedaNuevaDetallePrediosEmpresa = false;
                var cacheDetallePrediosEmpresa = false;
                var rutaArchivo = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathDetallPredios = Path.Combine(pathFuentes, "detallePrediosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathDetallPredios);

                resultado = new DetallePrediosViewModel()
                {
                    Detalle = JsonConvert.DeserializeObject<List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm>>(archivo)
                };

                _logger.LogInformation("Fuente de Detalle de Predios procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Detalle de Predios Empresa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    var historialDetallePredios = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DetallePrediosEmpresa);
                    if (historialDetallePredios != null)
                    {
                        if (!historialDetallePredios.Generado || !busquedaNuevaDetallePrediosEmpresa)
                        {
                            historialDetallePredios.IdHistorial = modelo.IdHistorial;
                            historialDetallePredios.TipoFuente = Dominio.Tipos.Fuentes.DetallePrediosEmpresa;
                            historialDetallePredios.Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any();
                            historialDetallePredios.Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null;
                            historialDetallePredios.Cache = cacheDetallePrediosEmpresa;
                            historialDetallePredios.FechaRegistro = DateTime.Now;
                            historialDetallePredios.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialDetallePredios);
                            _logger.LogInformation("Historial de la Fuente Detalle de Predios Empresa actualizado correctamente");
                        }
                    }
                    else
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.DetallePrediosEmpresa,
                            Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any(),
                            Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null,
                            Cache = cacheDetallePrediosEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Detalle de Predios Empresa procesado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteDetallePredios", resultado);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteDetallePredios", new DetallePrediosViewModel() { });
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosCuenca")]
        public async Task<IActionResult> ObtenerReportePrediosCuenca(ReporteViewModel modelo)
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
                var datos = new PrediosCuencaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosCuencaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosCuencaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Cuenca procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Cuenca. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCuenca;
                                historialPredio.Generado = r_prediosRepresentante != null;
                                historialPredio.Data = r_prediosRepresentante != null ? JsonConvert.SerializeObject(r_prediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Cuenca actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                    historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca;
                                    historialPredioEmpresa.Generado = r_prediosEmpresa != null;
                                    historialPredioEmpresa.Data = r_prediosEmpresa != null ? JsonConvert.SerializeObject(r_prediosEmpresa) : null;
                                    historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                    historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                    historialPredioEmpresa.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa Cuenaca actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosCuenca", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosCuenca", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteCuenca")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteCuenca(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionCuencaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;
                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosCuencaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosCuencaViewModel>(archivo);
                datos = new InformacionCuencaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Cuenca procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Cuenca. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCuenca;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Cuenca actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCuenca,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Cuenca procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCuenca", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCuenca", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaCuenca")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaCuenca(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionCuencaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;
                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosCuencaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosCuencaViewModel>(archivo);
                datos = new InformacionCuencaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Cuenca procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Cuenca. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Cuenca actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
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
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCuenca", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCuenca", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosSantoDomingo")]
        public async Task<IActionResult> ObtenerReportePrediosSantoDomingo(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo r_prediosEmpresa = null;
                var datos = new PrediosSantoDomingoViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;
                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSantoDomingoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosSantoDomingoViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Santo Domingo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santo Domingo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Santo Domingo actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                    historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo;
                                    historialPredioEmpresa.Generado = r_prediosEmpresa != null;
                                    historialPredioEmpresa.Data = r_prediosEmpresa != null ? JsonConvert.SerializeObject(r_prediosEmpresa) : null;
                                    historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                    historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                    historialPredioEmpresa.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosSantoDomingo", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosSantoDomingo", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteSantoDomingo")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteSantoDomingo(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSantoDomingoPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;
                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSantoDomingoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSantoDomingoViewModel>(archivo);
                datos = new InformacionSantoDomingoPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Santo Domingo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santo Domingo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Santo Domingo actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Santo Domingo procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantoDomingo", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantoDomingo", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaSantoDomingo")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaSantoDomingo(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSantoDomingoPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSantoDomingoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSantoDomingoViewModel>(archivo);
                datos = new InformacionSantoDomingoPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Santo Domingo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santo Domingo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Santo Domingo actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Santo Domingo procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantoDomingo", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantoDomingo", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRuminahui")]
        public async Task<IActionResult> ObtenerReportePrediosRuminahui(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui r_prediosEmpresa = null;
                var datos = new PrediosRuminahuiViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosRuminahuiDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosRuminahuiViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Rumiñahui procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Rumiñahui. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioRuminahui;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Rumiñahui actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                    historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui;
                                    historialPredioEmpresa.Generado = r_prediosEmpresa != null;
                                    historialPredioEmpresa.Data = r_prediosEmpresa != null ? JsonConvert.SerializeObject(r_prediosEmpresa) : null;
                                    historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                    historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                    historialPredioEmpresa.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosRuminahui", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosRuminahui", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteRuminahui")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteRuminahui(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionRuminahuiPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosRuminahuiDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosRuminahuiViewModel>(archivo);
                datos = new InformacionRuminahuiPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Rumiñahui procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Rumiñahui. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioRuminahui;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Rumiñahui actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioRuminahui,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Rumiñahui procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosRuminahui", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosRuminahui", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaRuminahui")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaRuminahui(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionRuminahuiPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosRuminahuiDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosRuminahuiViewModel>(archivo);
                datos = new InformacionRuminahuiPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Rumiñahui procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Rumiñahui. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Rumiñahui actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Rumiñahui procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosRuminahui", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosRuminahui", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosQuininde")]
        public async Task<IActionResult> ObtenerReportePrediosQuininde(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosQuinindeViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosQuinindeDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosQuinindeViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Quinindé procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Quinindé. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioQuininde;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Quinindé actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosQuininde", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosQuininde", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteQuininde")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteQuininde(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionQuinindePrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosQuinindeDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosQuinindeViewModel>(archivo);
                datos = new InformacionQuinindePrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Quinindé procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Quinindé. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioQuininde;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Quinindé actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioQuininde,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Quinindé procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosQuininde", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosQuininde", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaQuininde")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaQuininde(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionQuinindePrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosQuinindeDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosQuinindeViewModel>(archivo);
                datos = new InformacionQuinindePrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Quinindé procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Quinindé. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Quinindé actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Quinindé procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosQuininde", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosQuininde", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosLatacunga")]
        public async Task<IActionResult> ObtenerReportePrediosLatacunga(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosLatacungaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLatacungaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosLatacungaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Latacunga procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Latacunga. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLatacunga);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLatacunga;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Latacunga actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosLatacunga", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosLatacunga", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteLatacunga")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteLatacunga(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionLatacungaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLatacungaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosLatacungaViewModel>(archivo);
                datos = new InformacionLatacungaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Latacunga procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Latacunga. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLatacunga);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLatacunga;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Latacunga actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLatacunga,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Latacunga procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLatacunga", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLatacunga", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaLatacunga")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaLatacunga(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionLatacungaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLatacungaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosLatacungaViewModel>(archivo);
                datos = new InformacionLatacungaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Latacunga procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Latacunga. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Latacunga actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Latacunga procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLatacunga", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLatacunga", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosManta")]
        public async Task<IActionResult> ObtenerReportePrediosManta(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosMantaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMantaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosMantaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Manta procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Manta. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioManta);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioManta;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Manta actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosManta", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosManta", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteManta")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteManta(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionMantaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMantaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosMantaViewModel>(archivo);
                datos = new InformacionMantaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Manta procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Manta. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioManta);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioManta;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Manta actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioManta,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Manta procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosManta", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosManta", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaManta")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaManta(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionMantaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMantaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosMantaViewModel>(archivo);
                datos = new InformacionMantaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Manta procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Manta. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Manta actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Manta procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosManta", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosManta", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosAmbato")]
        public async Task<IActionResult> ObtenerReportePrediosAmbato(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosAmbatoViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosAmbatoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosAmbatoViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Ambato procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ambato. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAmbato);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioAmbato;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Ambato actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosAmbato", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosAmbato", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteAmbato")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteAmbato(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionAmbatoPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosAmbatoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosAmbatoViewModel>(archivo);
                datos = new InformacionAmbatoPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Ambato procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ambato. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAmbato);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioAmbato;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Ambato actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioAmbato,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Ambato procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosAmbato", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosAmbato", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaAmbato")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaAmbato(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionAmbatoPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosAmbatoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosAmbatoViewModel>(archivo);
                datos = new InformacionAmbatoPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Ambato procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ambato. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Ambato actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Ambato procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosAmbato", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosAmbato", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosIbarra")]
        public async Task<IActionResult> ObtenerReportePrediosIbarra(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosIbarraViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosIbarraDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosIbarraViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Ibarra procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ibarra. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioIbarra);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioIbarra;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Ibarra actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosIbarra", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosIbarra", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteIbarra")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteIbarra(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionIbarraPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosIbarraDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosIbarraViewModel>(archivo);
                datos = new InformacionIbarraPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Ibarra procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ibarra. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioIbarra);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioIbarra;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Ibarra actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioIbarra,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Ibarra procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosIbarra", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosIbarra", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaIbarra")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaIbarra(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionIbarraPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosIbarraDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosIbarraViewModel>(archivo);
                datos = new InformacionIbarraPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Ibarra procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Ibarra. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Ibarra actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Ibarra procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosIbarra", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosIbarra", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosSanCristobal")]
        public async Task<IActionResult> ObtenerReportePrediosSanCristobal(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosSanCristobalViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSanCristobalDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosSanCristobalViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio San Cristobal procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio San Cristobal. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio San Cristobal actualizado correctamente");
                            }
                        }
                        else
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
                            _logger.LogInformation("Historial de la Fuente Predio San Cristobal procesado correctamente");
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosSanCristobal", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosSanCristobal", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteSanCristobal")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteSanCristobal(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSanCristobalPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSanCristobalDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSanCristobalViewModel>(archivo);
                datos = new InformacionSanCristobalPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio San Cristobal procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio San Cristobal. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio San Cristobal actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio San Cristobal procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSanCristobal", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSanCristobal", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaSanCristobal")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaSanCristobal(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSanCristobalPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSanCristobalDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSanCristobalViewModel>(archivo);
                datos = new InformacionSanCristobalPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio San Cristobal procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio San Cristobal. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa San Cristobal actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa San Cristobal procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSanCristobal", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSanCristobal", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosDuran")]
        public async Task<IActionResult> ObtenerReportePrediosDuran(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosDuranViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosDuranDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosDuranViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Durán procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Durán. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDuran);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioDuran;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Durán actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosDuran", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosDuran", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteDuran")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteDuran(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionDuranPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosDuranDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosDuranViewModel>(archivo);
                datos = new InformacionDuranPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Durán procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Durán. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDuran);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioDuran;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Durán actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioDuran,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Durán procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosDuran", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosDuran", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaDuran")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaDuran(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionDuranPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosDuranDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosDuranViewModel>(archivo);
                datos = new InformacionDuranPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Durán procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Durán. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Durán actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Durán procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosDuran", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosDuran", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosLagoAgrio")]
        public async Task<IActionResult> ObtenerReportePrediosLagoAgrio(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosLagoAgrioViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLagoAgrioDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosLagoAgrioViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Lago Agrio procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Lago Agrio. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Lago Agrio actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosLagoAgrio", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosLagoAgrio", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteLagoAgrio")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteLagoAgrio(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionLagoAgrioPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLagoAgrioDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosLagoAgrioViewModel>(archivo);
                datos = new InformacionLagoAgrioPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Lago Agrio procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Lago Agrio. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Lago Agrio actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Lago Agrio procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLagoAgrio", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLagoAgrio", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaLagoAgrio")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaLagoAgrio(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionLagoAgrioPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLagoAgrioDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosLagoAgrioViewModel>(archivo);
                datos = new InformacionLagoAgrioPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Lago Agrio procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Lago Agrio. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Lago Agrio actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Lago Agrio procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLagoAgrio", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLagoAgrio", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosSantaRosa")]
        public async Task<IActionResult> ObtenerReportePrediosSantaRosa(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosSantaRosaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSantaRosaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosSantaRosaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Santa Rosa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santa Rosa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Santa Rosa actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosSantaRosa", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosSantaRosa", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteSantaRosa")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteSantaRosa(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSantaRosaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSantaRosaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSantaRosaViewModel>(archivo);
                datos = new InformacionSantaRosaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Santa Rosa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santa Rosa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Santa Rosa actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Santa Rosa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantaRosa", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantaRosa", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaSantaRosa")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaSantaRosa(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSantaRosaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSantaRosaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSantaRosaViewModel>(archivo);
                datos = new InformacionSantaRosaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Santa Rosa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santa Rosa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Santa Rosa actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Santa Rosa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantaRosa", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSantaRosa", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosSucua")]
        public async Task<IActionResult> ObtenerReportePrediosSucua(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosSucuaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSucuaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosSucuaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Sucúa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sucúa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSucua);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSucua;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Sucúa actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosSucua", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosSucua", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteSucua")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteSucua(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSucuaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSucuaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSucuaViewModel>(archivo);
                datos = new InformacionSucuaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Sucúa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sucúa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSucua);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSucua;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Sucúa actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSucua,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Sucúa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSucua", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSucua", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaSucua")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaSucua(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSucuaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSucuaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSucuaViewModel>(archivo);
                datos = new InformacionSucuaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Sucúa procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sucúa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Sucúa actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Sucúa procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSucua", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSucua", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosSigSig")]
        public async Task<IActionResult> ObtenerReportePrediosSigSig(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosSigSigViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSigSigDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosSigSigViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Sígsig procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sígsig. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSigSig);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSigSig;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Sígsig actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosSigSig", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosSigSig", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteSigSig")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteSigSig(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSigSigPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSigSigDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSigSigViewModel>(archivo);
                datos = new InformacionSigSigPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Sígsig procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sígsig. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSigSig);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSigSig;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Sigsig actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSigSig,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Sígsig procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSigSig", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSigSig", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaSigSig")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaSigSig(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSigSigPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSigSigDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSigSigViewModel>(archivo);
                datos = new InformacionSigSigPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Sígsig procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Sígsig. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Sígsig actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Sígsig procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSigSig", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSigSig", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosMejia")]
        public async Task<IActionResult> ObtenerReportePrediosMejia(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosMejiaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMejiaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosMejiaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Mejia procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Mejia. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMejia);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMejia;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Mejia actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosMejia", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosMejia", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteMejia")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteMejia(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionMejiaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMejiaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosMejiaViewModel>(archivo);
                datos = new InformacionMejiaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Mejia procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Mejia. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMejia);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMejia;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Mejia actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMejia,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Mejia procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMejia", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMejia", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaMejia")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaMejia(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionMejiaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMejiaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosMejiaViewModel>(archivo);
                datos = new InformacionMejiaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Mejia procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Mejia. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Mejia actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Mejia procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMejia", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMejia", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosMorona")]
        public async Task<IActionResult> ObtenerReportePrediosMorona(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosMoronaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMoronaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosMoronaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Morona procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Morona. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMorona);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMorona;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Morona actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosMorona", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosMorona", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteMorona")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteMorona(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionMoronaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMoronaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosMoronaViewModel>(archivo);
                datos = new InformacionMoronaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Morona procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Morona. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMorona);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMorona;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Morona actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioMorona,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Morona procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMorona", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMorona", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaMorona")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaMorona(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionMoronaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosMoronaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosMoronaViewModel>(archivo);
                datos = new InformacionMoronaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Morona procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Morona. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Morona actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Morona procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMorona", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosMorona", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosTena")]
        public async Task<IActionResult> ObtenerReportePrediosTena(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosTenaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosTenaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosTenaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Tena procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Tena. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioTena);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioTena;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Tena actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosTena", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosTena", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteTena")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteTena(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionTenaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosTenaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosTenaViewModel>(archivo);
                datos = new InformacionTenaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Tena procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Tena. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioTena);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioTena;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Tena actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioTena,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Tena procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosTena", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosTena", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaTena")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaTena(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionTenaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosTenaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosTenaViewModel>(archivo);
                datos = new InformacionTenaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Tena procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Tena. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Tena actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Tena procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosTena", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosTena", null);
            }
        }

        [Route("ObtenerReportePrediosCatamayo")]
        public async Task<IActionResult> ObtenerReportePrediosCatamayo(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosCatamayoViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosCatamayoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosCatamayoViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Catamayo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Catamayo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCatamayo);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCatamayo;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Catamayo actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosCatamayo", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosCatamayo", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteCatamayo")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteCatamayo(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionCatamayoPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosCatamayoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosCatamayoViewModel>(archivo);
                datos = new InformacionCatamayoPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Catamayo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Catamayo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCatamayo);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCatamayo;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Catamayo actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCatamayo,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Catamayo procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCatamayo", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCatamayo", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaCatamayo")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaCatamayo(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionCatamayoPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosCatamayoDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosCatamayoViewModel>(archivo);
                datos = new InformacionCatamayoPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Catamayo procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Catamayo. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Catamayo actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Catamayo procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCatamayo", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosCatamayo", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosLoja")]
        public async Task<IActionResult> ObtenerReportePrediosLoja(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosLojaViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLojaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosLojaViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Loja procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Loja. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLoja);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLoja;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Loja actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosLoja", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosLoja", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteLoja")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteLoja(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionLojaPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLojaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosLojaViewModel>(archivo);
                datos = new InformacionLojaPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Loja procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Loja. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLoja);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLoja;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Loja actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioLoja,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Loja procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLoja", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLoja", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaLoja")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaLoja(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionLojaPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosLojaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosLojaViewModel>(archivo);
                datos = new InformacionLojaPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Loja procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Loja. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Loja actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Loja procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLoja", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosLoja", null);
            }
        }
        [HttpPost]
        [Route("ObtenerReporteDetallePrediosLoja")]
        public async Task<IActionResult> ObtenerReporteDetallePrediosLoja(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                DetallePrediosLojaViewModel resultado = null;
                var busquedaNuevaDetallePredios = false;
                var cacheDetallePredios = false;
                var rutaArchivo = string.Empty;
                var identificacionBuscar = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathDetallPredios = Path.Combine(pathFuentes, "detallePrediosLojaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathDetallPredios);

                resultado = new DetallePrediosLojaViewModel()
                {
                    Detalle = JsonConvert.DeserializeObject<List<Externos.Logica.PredioMunicipio.Modelos.DatosPrediosPropiedadesLoja>>(archivo)
                };

                _logger.LogInformation("Fuente de Detalle de Predios procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Detalle de Predios. Id Historial: {modelo.IdHistorial}");

                try
                {
                    var historialDetallePredios = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DetallePrediosLoja);
                    if (historialDetallePredios != null)
                    {
                        if (!historialDetallePredios.Generado || !busquedaNuevaDetallePredios)
                        {
                            historialDetallePredios.IdHistorial = modelo.IdHistorial;
                            historialDetallePredios.TipoFuente = Dominio.Tipos.Fuentes.DetallePrediosLoja;
                            historialDetallePredios.Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any();
                            historialDetallePredios.Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null;
                            historialDetallePredios.Cache = cacheDetallePredios;
                            historialDetallePredios.FechaRegistro = DateTime.Now;
                            historialDetallePredios.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialDetallePredios);
                            _logger.LogInformation("Historial de la Fuente Detalle de Predios actualizado correctamente");
                        }
                    }
                    else
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.DetallePrediosLoja,
                            Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any(),
                            Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null,
                            Cache = cacheDetallePredios,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Detalle de Predios procesado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteDetallePrediosLoja", resultado);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteDetallePrediosLoja", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteDetallePrediosEmpresaLoja")]
        public async Task<IActionResult> ObtenerReporteDetallePrediosEmpresaLoja(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                DetallePrediosLojaViewModel resultado = null;
                var busquedaNuevaDetallePrediosEmpresa = false;
                var cacheDetallePrediosEmpresa = false;
                var rutaArchivo = string.Empty;
                var identificacionBuscar = string.Empty;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathDetallPredios = Path.Combine(pathFuentes, "detallePrediosLojaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathDetallPredios);

                resultado = new DetallePrediosLojaViewModel()
                {
                    Detalle = JsonConvert.DeserializeObject<List<Externos.Logica.PredioMunicipio.Modelos.DatosPrediosPropiedadesLoja>>(archivo)
                };

                _logger.LogInformation("Fuente de Detalle de Predios procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Detalle de Predios Empresa. Id Historial: {modelo.IdHistorial}");

                try
                {
                    var historialDetallePredios = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DetallePrediosEmpresaLoja);
                    if (historialDetallePredios != null)
                    {
                        if (!historialDetallePredios.Generado || !busquedaNuevaDetallePrediosEmpresa)
                        {
                            historialDetallePredios.IdHistorial = modelo.IdHistorial;
                            historialDetallePredios.TipoFuente = Dominio.Tipos.Fuentes.DetallePrediosEmpresaLoja;
                            historialDetallePredios.Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any();
                            historialDetallePredios.Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null;
                            historialDetallePredios.Cache = cacheDetallePrediosEmpresa;
                            historialDetallePredios.FechaRegistro = DateTime.Now;
                            historialDetallePredios.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialDetallePredios);
                            _logger.LogInformation("Historial de la Fuente Detalle de Predios Empresa actualizado correctamente");
                        }
                    }
                    else
                    {
                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.DetallePrediosEmpresaLoja,
                            Generado = resultado != null && resultado.Detalle != null && resultado.Detalle.Any(),
                            Data = resultado != null && resultado.Detalle != null && resultado.Detalle.Any() ? JsonConvert.SerializeObject(resultado.Detalle) : null,
                            Cache = cacheDetallePrediosEmpresa,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });
                        _logger.LogInformation("Historial de la Fuente Detalle de Predios Empresa procesado correctamente");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteDetallePrediosLoja", resultado);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteDetallePrediosLoja", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosSamborondon")]
        public async Task<IActionResult> ObtenerReportePrediosSamborondon(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new PrediosSamborondonViewModel();
                var busquedaNuevaPredios = false;
                var busquedaNuevaPrediosEmpresa = false;
                var cachePredios = false;
                var cachePrediosEmpresa = false;
                var busquedaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSamborondonDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                datos = JsonConvert.DeserializeObject<PrediosSamborondonViewModel>(archivo);
                busquedaEmpresa = true;

                _logger.LogInformation("Fuente de Predio Samborondon procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Samborondon. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSamborondon);
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon);

                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSamborondon;
                                historialPredio.Generado = datos.PrediosRepresentante != null;
                                historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Samborondon actualizado correctamente");
                            }
                        }
                        else
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
                        }

                        if (busquedaEmpresa)
                        {
                            if (historialPredioEmpresa != null)
                            {
                                if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                                {
                                    historialPredio.IdHistorial = modelo.IdHistorial;
                                    historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon;
                                    historialPredio.Generado = datos.PrediosRepresentante != null;
                                    historialPredio.Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null;
                                    historialPredio.Cache = cachePredios;
                                    historialPredio.FechaRegistro = DateTime.Now;
                                    historialPredio.Reintento = true;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                    _logger.LogInformation("Historial de la Fuente Predio Empresa actualizado correctamente");
                                }
                            }
                            else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuentePrediosSamborondon", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuentePrediosSamborondon", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosRepresentanteSamborondon")]
        public async Task<IActionResult> ObtenerReportePrediosRepresentanteSamborondon(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSamborondonPrediosViewModel();
                var busquedaNuevaPredios = false;
                var cachePredios = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSamborondonDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSamborondonViewModel>(archivo);
                datos = new InformacionSamborondonPrediosViewModel()
                {
                    Predios = datosCache.PrediosRepresentante,
                    BusquedaNueva = datosCache.BusquedaNuevaRepresentante,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 1
                };

                _logger.LogInformation("Fuente de Predio Samborondon procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Samborondon. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredio = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSamborondon);
                        if (historialPredio != null)
                        {
                            if (!historialPredio.Generado || !busquedaNuevaPredios)
                            {
                                historialPredio.IdHistorial = modelo.IdHistorial;
                                historialPredio.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSamborondon;
                                historialPredio.Generado = datos.Predios != null;
                                historialPredio.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredio.Cache = cachePredios;
                                historialPredio.FechaRegistro = DateTime.Now;
                                historialPredio.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredio);
                                _logger.LogInformation("Historial de la Fuente Predio Samborondon actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSamborondon,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePredios,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Samborondon procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSamborondon", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSamborondon", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReportePrediosEmpresaSamborondon")]
        public async Task<IActionResult> ObtenerReportePrediosEmpresaSamborondon(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                var datos = new InformacionSamborondonPrediosViewModel();
                var busquedaNuevaPrediosEmpresa = false;
                var cachePrediosEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathPredios = Path.Combine(pathFuentes, "prediosSamborondonDemo.json");
                var archivo = System.IO.File.ReadAllText(pathPredios);
                var datosCache = JsonConvert.DeserializeObject<PrediosSamborondonViewModel>(archivo);
                datos = new InformacionSamborondonPrediosViewModel()
                {
                    Predios = datosCache.PrediosEmpresa,
                    BusquedaNueva = datosCache.BusquedaNuevaEmpresa,
                    HistorialCabecera = datosCache.HistorialCabecera,
                    TipoConsulta = 2
                };

                _logger.LogInformation("Fuente de Predio Samborondon procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Samborondon. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialPredioEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon);

                        if (historialPredioEmpresa != null)
                        {
                            if (!historialPredioEmpresa.Generado || !busquedaNuevaPrediosEmpresa)
                            {
                                historialPredioEmpresa.IdHistorial = modelo.IdHistorial;
                                historialPredioEmpresa.TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon;
                                historialPredioEmpresa.Generado = datos.Predios != null;
                                historialPredioEmpresa.Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null;
                                historialPredioEmpresa.Cache = cachePrediosEmpresa;
                                historialPredioEmpresa.FechaRegistro = DateTime.Now;
                                historialPredioEmpresa.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPredioEmpresa);
                                _logger.LogInformation("Historial de la Fuente Predio Empresa Samborondon actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon,
                                Generado = datos.Predios != null,
                                Data = datos.Predios != null ? JsonConvert.SerializeObject(datos.Predios) : null,
                                Cache = cachePrediosEmpresa,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Predio Empresa Samborondon procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSamborondon", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteInformacionPrediosSamborondon", null);
            }
        }

        [HttpPost]
        [Route("ObtenerFiscaliaDelitos")]
        public async Task<IActionResult> ObtenerFiscaliaDelitos(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                var datos = new DelitosViewModel();
                var cacheFiscalia = false;
                var cacheFiscaliaEmpresa = false;
                var busquedaNuevaFiscalia = false;
                var busquedaNuevaFiscaliaEmpresa = false;

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathLegal = Path.Combine(pathFuentes, "fiscaliaDelitosDemo.json");
                var archivo = System.IO.File.ReadAllText(pathLegal);
                datos = JsonConvert.DeserializeObject<DelitosViewModel>(archivo);
                datos.FuenteActiva = true;

                _logger.LogInformation("Fuente de Fiscalía Delitos procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Fiscalía Delitos. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesFiscalia = new[] { Dominio.Tipos.Fuentes.FiscaliaDelitosPersona, Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa };
                        var historialesFiscalia = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesFiscalia.Contains(m.TipoFuente));
                        var historialFiscaliaPersona = historialesFiscalia.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosPersona);
                        var historialFiscaliaEmpresa = historialesFiscalia.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa);

                        if (historialFiscaliaPersona != null && (historialFiscaliaPersona.Generado || !busquedaNuevaFiscalia))
                        {
                            historialFiscaliaPersona.IdHistorial = modelo.IdHistorial;
                            historialFiscaliaPersona.TipoFuente = Dominio.Tipos.Fuentes.FiscaliaDelitosPersona;
                            historialFiscaliaPersona.Generado = datos.FiscaliaPersona != null;
                            historialFiscaliaPersona.Data = datos.FiscaliaPersona != null ? JsonConvert.SerializeObject(datos.FiscaliaPersona) : null;
                            historialFiscaliaPersona.Cache = cacheFiscalia;
                            historialFiscaliaPersona.FechaRegistro = DateTime.Now;
                            historialFiscaliaPersona.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialFiscaliaPersona);
                            _logger.LogInformation("Historial de la Fuente Fiscalía Delitos Persona actualizado correctamente");
                        }
                        else if (historialFiscaliaPersona == null)
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
                        }

                        if (historialFiscaliaEmpresa != null && (historialFiscaliaEmpresa.Generado || !busquedaNuevaFiscaliaEmpresa))
                        {
                            historialFiscaliaEmpresa.IdHistorial = modelo.IdHistorial;
                            historialFiscaliaEmpresa.TipoFuente = Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa;
                            historialFiscaliaEmpresa.Generado = datos.FiscaliaEmpresa != null;
                            historialFiscaliaEmpresa.Data = datos.FiscaliaEmpresa != null ? JsonConvert.SerializeObject(datos.FiscaliaEmpresa) : null;
                            historialFiscaliaEmpresa.Cache = cacheFiscaliaEmpresa;
                            historialFiscaliaEmpresa.FechaRegistro = DateTime.Now;
                            historialFiscaliaEmpresa.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialFiscaliaEmpresa);
                            _logger.LogInformation("Historial de la Fuente Fiscalía Delitos Empresa actualizado correctamente");
                        }
                        else if (historialFiscaliaEmpresa == null)
                        {
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
                        }
                        _logger.LogInformation("Historial de la Fuente Fiscalía Delitos procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                #region DatosDelitos ProcesadosSospechosos
                try
                {
                    var numeroDelito = new List<string>();
                    var numeroAdministro = new List<string>();
                    var numeroDelitoEmpresa = new List<string>();
                    var numeroAdministroEmpresa = new List<string>();
                    if (datos != null && datos.FiscaliaPersona != null && datos.FiscaliaPersona.ProcesosNoticiaDelito != null && datos.FiscaliaPersona.ProcesosNoticiaDelito.Any())
                    {
                        var sujetosNoticiaDelito = datos.FiscaliaPersona.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Equals("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                        if (sujetosNoticiaDelito != null && sujetosNoticiaDelito.Any())
                        {
                            var nombreDivido = datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) ? datos.HistorialCabecera.NombresPersona.Split(' ') : new string[0];
                            var listaNombre = new List<bool>();
                            foreach (var item1 in sujetosNoticiaDelito.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                            {
                                if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Equals("SOSPECHOSO"))
                                {
                                    if (!string.IsNullOrEmpty(item1.Cedula) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.Identificacion) && datos.HistorialCabecera.Identificacion == item1.Cedula)
                                        numeroDelito.Add(item1.Numero);
                                    else if (!string.IsNullOrEmpty(item1.Cedula) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.IdentificacionSecundaria) && datos.HistorialCabecera.IdentificacionSecundaria == item1.Cedula)
                                        numeroDelito.Add(item1.Numero);
                                    else
                                    {
                                        var nombreSeparado = item1.NombresCompletos.Split(' ');
                                        listaNombre.Clear();
                                        foreach (var item2 in nombreSeparado)
                                        {
                                            if (datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) && datos.HistorialCabecera.NombresPersona.Contains(item2))
                                                listaNombre.Add(true);
                                            else
                                                listaNombre.Add(false);
                                        }
                                        if (nombreDivido != null && nombreDivido.Any() && listaNombre.Count(x => x) == nombreDivido.Length)
                                            numeroDelito.Add(item1.Numero);
                                    }
                                }
                            }
                            numeroDelito = numeroDelito.Distinct().ToList();
                            datos.FiscaliaPersona.ProcesosNoticiaDelito = datos.FiscaliaPersona.ProcesosNoticiaDelito.Where(x => numeroDelito.Contains(x.Numero)).Select(x => x).ToList();
                        }
                        else
                            datos.FiscaliaPersona.ProcesosNoticiaDelito.Clear();
                    }

                    if (datos != null && datos.FiscaliaPersona != null && datos.FiscaliaPersona.ProcesosActoAdministrativo != null && datos.FiscaliaPersona.ProcesosActoAdministrativo.Any())
                    {
                        var sujetosActoAdministrativo = datos.FiscaliaPersona.ProcesosActoAdministrativo.Where(x => x.Descripcion.ToUpper().Equals("PROCESADO") || x.Descripcion.ToUpper().Equals("SOSPECHOSO")).Select(x => new { x.Numero, x.CedulaDenunciante, x.NombreDenunciante }).ToList();
                        if (sujetosActoAdministrativo != null && sujetosActoAdministrativo.Any())
                        {
                            var nombreDivido = datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) ? datos.HistorialCabecera.NombresPersona.Split(' ') : new string[0];
                            var listaNombre = new List<bool>();
                            foreach (var item1 in sujetosActoAdministrativo)
                            {
                                if (!string.IsNullOrEmpty(item1.CedulaDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.Identificacion) && datos.HistorialCabecera.Identificacion == item1.CedulaDenunciante)
                                    numeroAdministro.Add(item1.Numero);
                                else if (!string.IsNullOrEmpty(item1.CedulaDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.IdentificacionSecundaria) && datos.HistorialCabecera.IdentificacionSecundaria == item1.CedulaDenunciante)
                                    numeroAdministro.Add(item1.Numero);
                                else
                                {
                                    var nombreSeparado = item1.NombreDenunciante.Split(' ');
                                    listaNombre.Clear();
                                    foreach (var item2 in nombreSeparado)
                                    {
                                        if (datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) && datos.HistorialCabecera.NombresPersona.Contains(item2))
                                            listaNombre.Add(true);
                                        else
                                            listaNombre.Add(false);
                                    }
                                    if (nombreDivido != null && nombreDivido.Any() && listaNombre.Count(x => x) == nombreDivido.Length)
                                        numeroAdministro.Add(item1.Numero);
                                }
                            }
                            numeroAdministro = numeroAdministro.Distinct().ToList();
                            datos.FiscaliaPersona.ProcesosActoAdministrativo = datos.FiscaliaPersona.ProcesosActoAdministrativo.Where(x => numeroAdministro.Contains(x.Numero)).Select(x => x).ToList();
                        }
                        else
                            datos.FiscaliaPersona.ProcesosActoAdministrativo.Clear();
                    }

                    if (datos != null && datos.FiscaliaEmpresa != null && datos.FiscaliaEmpresa.ProcesosNoticiaDelito != null && datos.FiscaliaEmpresa.ProcesosNoticiaDelito.Any())
                    {
                        var sujetos = datos.FiscaliaEmpresa.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Equals("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                        if (sujetos != null && sujetos.Any())
                        {
                            foreach (var item1 in sujetos.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                            {
                                if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Equals("SOSPECHOSO"))
                                {
                                    if (!string.IsNullOrEmpty(item1.Cedula) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.Identificacion) && datos.HistorialCabecera.Identificacion == item1.Cedula)
                                        numeroDelitoEmpresa.Add(item1.Numero);
                                    else if (!string.IsNullOrEmpty(item1.NombresCompletos) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.RazonSocialEmpresa) && datos.HistorialCabecera.RazonSocialEmpresa == item1.NombresCompletos)
                                        numeroDelitoEmpresa.Add(item1.Numero);
                                }
                            }
                            numeroDelitoEmpresa = numeroDelitoEmpresa.Distinct().ToList();
                            datos.FiscaliaEmpresa.ProcesosNoticiaDelito = datos.FiscaliaEmpresa.ProcesosNoticiaDelito.Where(x => numeroDelitoEmpresa.Contains(x.Numero)).Select(x => x).ToList();
                        }
                        else
                            datos.FiscaliaEmpresa.ProcesosNoticiaDelito.Clear();
                    }

                    if (datos != null && datos.FiscaliaEmpresa != null && datos.FiscaliaEmpresa.ProcesosActoAdministrativo != null && datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Any())
                    {
                        var sujetos = datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Where(x => x.Descripcion.ToUpper().Equals("PROCESADO") || x.Descripcion.ToUpper().Equals("SOSPECHOSO")).Select(x => new { x.Numero, x.CedulaDenunciante, x.NombreDenunciante }).ToList();
                        if (sujetos != null && sujetos.Any())
                        {
                            foreach (var item1 in sujetos)
                            {
                                if (!string.IsNullOrEmpty(item1.CedulaDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.Identificacion) && datos.HistorialCabecera.Identificacion == item1.CedulaDenunciante)
                                    numeroAdministroEmpresa.Add(item1.Numero);
                                else if (!string.IsNullOrEmpty(item1.NombreDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.RazonSocialEmpresa) && datos.HistorialCabecera.RazonSocialEmpresa == item1.NombreDenunciante)
                                    numeroAdministroEmpresa.Add(item1.Numero);
                            }
                            numeroAdministroEmpresa = numeroAdministroEmpresa.Distinct().ToList();
                            datos.FiscaliaEmpresa.ProcesosActoAdministrativo = datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Where(x => numeroAdministroEmpresa.Contains(x.Numero)).Select(x => x).ToList();
                        }
                        else
                            datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                #endregion DatosDelitos ProcesadosSospechosos

                return PartialView("../Shared/Fuentes/_FuenteFiscaliaDelitos", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteFiscaliaDelitos", null);
            }
        }

        [HttpPost]
        [Route("ObtenerUafe")]
        public async Task<IActionResult> ObtenerUafe(ReporteViewModel modelo)
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

                var busquedaNuevaOnu = false;
                var busquedaNuevaOnu2206 = false;
                var busquedaNuevaInterpol = false;
                var busquedaNuevaOfac = false;
                var cacheOnu = false;
                var cacheOnu2206 = false;
                var cacheInterpol = false;
                var cacheOfac = false;
                string mensajeErrorOnu = null;
                string mensajeErrorOnu2206 = null;
                string mensajeErrorInterpol = null;
                string mensajeErrorOfac = null;
                var accesoOnu = false;
                var accesoOfac = false;
                var accesoInterpol = false;

                var datos = new UafeViewModel();

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathLegal = Path.Combine(pathFuentes, "uafeDemo.json");
                var archivo = System.IO.File.ReadAllText(pathLegal);
                datos = JsonConvert.DeserializeObject<UafeViewModel>(archivo);
                datos.BusquedaJuridica = true;
                datos.AccesoOnu = true;
                datos.AccesoInterpol = true;
                datos.AccesoOfac = true;

                if (accesoOnu)
                {
                    if (datos.ONU != null && datos.ONU.Individuo == null && datos.ONU.Entidad == null)
                        datos.ONU = null;

                    if (datos.ONU2206 != null && datos.ONU2206.Individuo == null && datos.ONU2206.Entidad == null)
                        datos.ONU2206 = null;
                }

                if (accesoInterpol)
                {
                    if (datos.Interpol != null && datos.Interpol.NoticiaIndividuo == null)
                        datos.Interpol = null;
                }

                if (accesoOfac)
                {
                    if (datos.OFAC != null && string.IsNullOrEmpty(datos.OFAC.ContenidoIndividuo) && string.IsNullOrEmpty(datos.OFAC.ContenidoEmpresa))
                        datos.OFAC = null;
                }

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
                            if (historialOnu != null && (!historialOnu.Generado || !busquedaNuevaOnu))
                            {
                                historialOnu.IdHistorial = modelo.IdHistorial;
                                historialOnu.TipoFuente = Dominio.Tipos.Fuentes.UafeOnu;
                                historialOnu.Generado = datos.ONU != null;
                                historialOnu.Data = datos.ONU != null ? JsonConvert.SerializeObject(datos.ONU) : null;
                                historialOnu.Cache = cacheOnu;
                                historialOnu.FechaRegistro = DateTime.Now;
                                historialOnu.Reintento = true;
                                historialOnu.DataError = mensajeErrorOnu;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialOnu);
                                _logger.LogInformation("Historial de la Fuente UAFE ONU actualizado correctamente");
                            }
                            else if (historialOnu == null)
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
                            }

                            if (historialOnu2206 != null && (!historialOnu2206.Generado || !busquedaNuevaOnu2206))
                            {
                                historialOnu2206.IdHistorial = modelo.IdHistorial;
                                historialOnu2206.TipoFuente = Dominio.Tipos.Fuentes.UafeOnu2206;
                                historialOnu2206.Generado = datos.ONU2206 != null;
                                historialOnu2206.Data = datos.ONU2206 != null ? JsonConvert.SerializeObject(datos.ONU2206) : null;
                                historialOnu2206.Cache = cacheOnu2206;
                                historialOnu2206.FechaRegistro = DateTime.Now;
                                historialOnu2206.Reintento = true;
                                historialOnu2206.DataError = mensajeErrorOnu2206;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialOnu2206);
                                _logger.LogInformation("Historial de la Fuente UAFE ONU 2206 actualizado correctamente");
                            }
                            else if (historialOnu2206 == null)
                            {
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
                        }

                        if (accesoInterpol)
                        {
                            if (historialInterpol != null && (!historialInterpol.Generado || !busquedaNuevaInterpol))
                            {
                                historialInterpol.IdHistorial = modelo.IdHistorial;
                                historialInterpol.TipoFuente = Dominio.Tipos.Fuentes.UafeInterpol;
                                historialInterpol.Generado = datos.Interpol != null;
                                historialInterpol.Data = datos.Interpol != null ? JsonConvert.SerializeObject(datos.Interpol) : null;
                                historialInterpol.Cache = cacheInterpol;
                                historialInterpol.FechaRegistro = DateTime.Now;
                                historialInterpol.Reintento = true;
                                historialInterpol.DataError = mensajeErrorInterpol;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialInterpol);
                                _logger.LogInformation("Historial de la Fuente UAFE Interpol actualizado correctamente");
                            }
                            else if (historialInterpol == null)
                            {
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
                            }
                        }

                        if (accesoOfac)
                        {
                            if (historialOfac != null && (!historialOfac.Generado || !busquedaNuevaOfac))
                            {
                                historialOfac.IdHistorial = modelo.IdHistorial;
                                historialOfac.TipoFuente = Dominio.Tipos.Fuentes.UafeOfac;
                                historialOfac.Generado = datos.OFAC != null;
                                historialOfac.Data = datos.OFAC != null ? JsonConvert.SerializeObject(datos.OFAC) : null;
                                historialOfac.Cache = cacheOfac;
                                historialOfac.FechaRegistro = DateTime.Now;
                                historialOfac.Reintento = true;
                                historialOfac.DataError = mensajeErrorOfac;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialOfac);
                                _logger.LogInformation("Historial de la Fuente UAFE OFAC actualizado correctamente");
                            }
                            else if (historialOfac == null)
                            {
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
                            }
                        }
                        _logger.LogInformation("Historial de la Fuente UAFE procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteUAFE", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteUAFE", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteFuerzasArmadas")]
        public async Task<IActionResult> ObtenerReporteFuerzasArmadas(ReporteViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada r_fuerzaArmada = null;
                ViewBag.RutaArchivo = string.Empty;
                var busquedaNuevaFuerzaArmada = false;
                var cacheFuerzaArmada = false;
                var rutaArchivo = string.Empty;
                var datos = new FuerzasArmadasViewModel();

                var pathBase = System.IO.Path.Combine("wwwroot", "data");
                var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                var pathFuerzaArmada = Path.Combine(pathFuentes, "fuerzaArmadaDemo.json");
                var archivo = System.IO.File.ReadAllText(pathFuerzaArmada);
                datos = JsonConvert.DeserializeObject<FuerzasArmadasViewModel>(archivo);

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

                _logger.LogInformation("Fuente de Fuerzas Armadas procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Fuerzas Armadas. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialFuerzasArmadas = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.FuerzaArmada);
                        if (historialFuerzasArmadas != null)
                        {
                            if (!historialFuerzasArmadas.Generado || !busquedaNuevaFuerzaArmada)
                            {
                                historialFuerzasArmadas.IdHistorial = modelo.IdHistorial;
                                historialFuerzasArmadas.TipoFuente = Dominio.Tipos.Fuentes.FuerzaArmada;
                                historialFuerzasArmadas.Generado = r_fuerzaArmada != null;
                                historialFuerzasArmadas.Data = r_fuerzaArmada != null ? JsonConvert.SerializeObject(r_fuerzaArmada) : null;
                                historialFuerzasArmadas.Cache = cacheFuerzaArmada;
                                historialFuerzasArmadas.FechaRegistro = DateTime.Now;
                                historialFuerzasArmadas.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialFuerzasArmadas);
                                _logger.LogInformation("Historial de la Fuente Fuerzas Armadas actualizado correctamente");
                            }
                        }
                        else
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
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return PartialView("../Shared/Fuentes/_FuenteFuerzasArmadas", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteFuerzasArmadas", null);
            }
        }

        [HttpPost]
        [Route("ObtenerReporteBuroCredito")]
        public async Task<IActionResult> ObtenerReporteBuroCredito(ReporteViewModel modelo)
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
                var busquedaNuevaBuroCredito = false;
                var cacheBuroCredito = false;
                var idUsuario = User.GetUserId<int>();
                var idPlanBuro = 0;
                var datos = new BuroCreditoViewModel();
                var dataErrorEquifax = string.Empty;
                var dataErrorAval = string.Empty;
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var aplicaConsultaBuroCompartida = false;
                var mensajeErrorBuro = string.Empty;

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

                var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                if (historial != null)
                {
                    historial.IdPlanBuroCredito = idPlanBuro;
                    historial.TipoFuenteBuro = planBuroCredito.Fuente;
                    if (aplicaConsultaBuroCompartida)
                        historial.ConsultaBuroCompartido = true;
                    await _historiales.UpdateAsync(historial);
                }

                try
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                    if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                    {
                        if (usuarioActual.Empresa.Identificacion == Dominio.Constantes.Clientes.Cliente1090105244001)
                        {
                            if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                            {
                                var pathBuroEmpresa = Path.Combine(pathFuentes, "buroAvalEmpresaBCapitalDemo.json");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroEmpresa));
                            }
                            else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                var pathBuroCedula = Path.Combine(pathFuentes, "buroAvalCedulaBCapitalDemo.json");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroCedula));
                            }

                        }
                        else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                        {
                            var pathBuroEmpresa = Path.Combine(pathFuentes, !planBuroCredito.ModeloCooperativas ? "buroAvalEmpresaDemo.json" : "buroAvalEmpresaCoacDemo.json");
                            //var pathBuroEmpresa = Path.Combine(pathFuentes, "buroAvalEmpresaAyasaDemo.json");
                            r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroEmpresa));
                        }
                        else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                        {
                            var pathBuroCedula = Path.Combine(pathFuentes, !planBuroCredito.ModeloCooperativas ? "buroAvalCedulaDemo.json" : "buroAvalCedulaCoacDemo.json");
                            //var pathBuroCedula = Path.Combine(pathFuentes, "buroAvalCedulaAyasaDemo.json");
                            r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroCedula));
                        }
                    }
                    else if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                    {
                        if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                        {
                            var pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaDemo.json");
                            if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990304211001)
                                pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaIndumotDemo.json");
                            else if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0190325180001)
                                pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaSanJoseDemo.json");
                            r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(System.IO.File.ReadAllText(pathBuroEquifax));
                        }
                        else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                        {
                            var pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaDemo.json");
                            if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990304211001)
                                pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaIndumotDemo.json");
                            else if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0190325180001)
                                pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaSanJoseDemo.json");
                            r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(System.IO.File.ReadAllText(pathBuroEquifax));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar fuente Buró de Crédito con identificación {modelo.Identificacion}: {ex.Message}");
                    mensajeErrorBuro = ex.Message;
                }

                if (r_burocredito == null && planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                {
                    var datosDetalleBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosDetalleBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                    {
                        _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheBuroCredito = true;
                        r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(datosDetalleBuroCredito);
                        busquedaNuevaBuroCredito = true;
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
                        busquedaNuevaBuroCredito = true;
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
                    BusquedaNueva = busquedaNuevaBuroCredito,
                    DatosCache = cacheBuroCredito,
                    Fuente = planBuroCredito.Fuente,
                    BuroCreditoEquifax = r_burocreditoEquifax,
                    ErrorEquifax = !string.IsNullOrEmpty(dataErrorEquifax) ? JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(dataErrorEquifax) : null,
                    ErrorAval = !string.IsNullOrEmpty(dataErrorAval) ? JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(dataErrorAval) : null,
                    MensajeError = mensajeErrorBuro
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
                            var historialBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito);
                            if (historialBuroCredito != null)
                            {
                                if (!historialBuroCredito.Generado || !busquedaNuevaBuroCredito)
                                {
                                    historialBuroCredito.IdHistorial = modelo.IdHistorial;
                                    historialBuroCredito.TipoFuente = Dominio.Tipos.Fuentes.BuroCredito;
                                    historialBuroCredito.Generado = datos.BuroCredito != null && !codigoBuro.Contains(datos.BuroCredito.ResponseCode);
                                    historialBuroCredito.Data = datos.BuroCredito != null ? JsonConvert.SerializeObject(datos.BuroCredito) : null;
                                    historialBuroCredito.Cache = cacheBuroCredito;
                                    historialBuroCredito.DataError = !string.IsNullOrEmpty(dataErrorAval) ? dataErrorAval : null;
                                    historialBuroCredito.FechaRegistro = DateTime.Now;
                                    historialBuroCredito.Reintento = true;
                                    historialBuroCredito.Observacion = datos.BuroCredito != null && !string.IsNullOrEmpty(datos.BuroCredito.Usuario) ? $"Usuario WS AVAL: {datos.BuroCredito.Usuario}" : null;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialBuroCredito);
                                    _logger.LogInformation("Historial de la Fuente Aval Buró de Crédito actualizado correctamente");
                                }
                            }
                            else
                            {
                                await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                                {
                                    IdHistorial = modelo.IdHistorial,
                                    TipoFuente = Dominio.Tipos.Fuentes.BuroCredito,
                                    Generado = datos.BuroCredito != null && !codigoBuro.Contains(datos.BuroCredito.ResponseCode),
                                    Data = datos.BuroCredito != null ? JsonConvert.SerializeObject(datos.BuroCredito) : null,
                                    Cache = cacheBuroCredito,
                                    DataError = !string.IsNullOrEmpty(dataErrorAval) ? dataErrorAval : null,
                                    FechaRegistro = DateTime.Now,
                                    Reintento = false,
                                    Observacion = datos.BuroCredito != null && !string.IsNullOrEmpty(datos.BuroCredito.Usuario) ? $"Usuario WS AVAL: {datos.BuroCredito.Usuario}" : null
                                });
                                _logger.LogInformation("Historial de la Fuente Aval Buró de Crédito procesado correctamente");
                            }
                        }
                        else if (planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            var historialBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito);
                            if (historialBuroCredito != null)
                            {
                                if (!historialBuroCredito.Generado || !busquedaNuevaBuroCredito)
                                {
                                    historialBuroCredito.IdHistorial = modelo.IdHistorial;
                                    historialBuroCredito.TipoFuente = Dominio.Tipos.Fuentes.BuroCredito;
                                    historialBuroCredito.Generado = datos.BuroCreditoEquifax != null;
                                    historialBuroCredito.Data = datos.BuroCreditoEquifax != null ? JsonConvert.SerializeObject(datos.BuroCreditoEquifax) : null;
                                    historialBuroCredito.Cache = cacheBuroCredito;
                                    historialBuroCredito.DataError = !string.IsNullOrEmpty(dataErrorEquifax) ? dataErrorEquifax : null;
                                    historialBuroCredito.FechaRegistro = DateTime.Now;
                                    historialBuroCredito.Reintento = true;
                                    historialBuroCredito.Observacion = datos.BuroCreditoEquifax != null && !string.IsNullOrEmpty(datos.BuroCreditoEquifax.Usuario) ? $"Usuario WS Equifax: {datos.BuroCreditoEquifax.Usuario}" : null;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialBuroCredito);
                                    _logger.LogInformation("Historial de la Fuente Equifax Buró de Crédito actualizado correctamente");
                                }
                            }
                            else
                            {
                                await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                                {
                                    IdHistorial = modelo.IdHistorial,
                                    TipoFuente = Dominio.Tipos.Fuentes.BuroCredito,
                                    Generado = datos.BuroCreditoEquifax != null,
                                    Data = datos.BuroCreditoEquifax != null ? JsonConvert.SerializeObject(datos.BuroCreditoEquifax) : null,
                                    Cache = cacheBuroCredito,
                                    DataError = !string.IsNullOrEmpty(dataErrorEquifax) ? dataErrorEquifax : null,
                                    FechaRegistro = DateTime.Now,
                                    Reintento = false,
                                    Observacion = datos.BuroCreditoEquifax != null && !string.IsNullOrEmpty(datos.BuroCreditoEquifax.Usuario) ? $"Usuario WS Equifax: {datos.BuroCreditoEquifax.Usuario}" : null
                                });
                                _logger.LogInformation("Historial de la Fuente Equifax Buró de Crédito procesado correctamente");
                            }
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                var vistaBuro = string.Empty;
                if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                    vistaBuro = "_FuenteBuroCredito";
                else if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                    vistaBuro = "_FuenteBuroEquifax";
                else
                    vistaBuro = "_FuenteBuro";

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
                return PartialView($"../Shared/Fuentes/{vistaBuro}", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView("../Shared/Fuentes/_FuenteBuro", null);
            }
        }
        #endregion Fuentes

        #region ConsultasEquifax

        [HttpPost]
        [Route("ObtenerNivelTotalDeudaHistoricaEquifax")]
        public async Task<IActionResult> ObtenerNivelTotalDeudaHistoricaEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoTotalDeudaHistorica nivelTotalDeudaHistorica = null;
                var busquedaNivelTotalDeudaHistorica = false;
                var cacheNivelTotalDeudaHistorica = false;
                var datos = new NivelTotalDeudaHistoricaViewModel();

                try
                {
                    var consultaDeudaHistorico = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelTotalDeudaHistorica && m.Generado, null, null, true);
                    if (consultaDeudaHistorico != null && !string.IsNullOrEmpty(consultaDeudaHistorico.Data))
                    {
                        nivelTotalDeudaHistorica = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoTotalDeudaHistorica>(consultaDeudaHistorico.Data);
                        datos = new NivelTotalDeudaHistoricaViewModel()
                        {
                            TotalDeudaHistorica = nivelTotalDeudaHistorica,
                            BusquedaNueva = busquedaNivelTotalDeudaHistorica,
                            DatosCache = consultaDeudaHistorico.Cache,
                        };

                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesTotalDeudaHistorica", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoDeudaHistorica3601 == null || !datosEquifax.Resultados.RecursivoDeudaHistorica3601.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Deuda Historica 360.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelTotalDeudaHistorica = await _buroCreditoEquifax.GetTotalDeudaHistoricaAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, credencial);

                            if (nivelTotalDeudaHistorica != null && !nivelTotalDeudaHistorica.ResultadoConsultaTotalDeudaHistorica)
                                nivelTotalDeudaHistorica = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelTotalDeudaHistorica = Path.Combine(pathFuentes, "totalDeudaHistoricaDemo.json");
                            nivelTotalDeudaHistorica = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoTotalDeudaHistorica>(System.IO.File.ReadAllText(pathNivelTotalDeudaHistorica));
                            busquedaNivelTotalDeudaHistorica = false;
                            cacheNivelTotalDeudaHistorica = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Total Deuda Historica {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelTotalDeudaHistorica == null)
                {
                    busquedaNivelTotalDeudaHistorica = true;
                    var datosNivelTotalDeudaHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelTotalDeudaHistorica && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelTotalDeudaHistorica != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Total Deuda Historica con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelTotalDeudaHistorica = true;
                        nivelTotalDeudaHistorica = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoTotalDeudaHistorica>(datosNivelTotalDeudaHistorica);
                    }
                }

                datos = new NivelTotalDeudaHistoricaViewModel()
                {
                    TotalDeudaHistorica = nivelTotalDeudaHistorica,
                    BusquedaNueva = busquedaNivelTotalDeudaHistorica,
                    DatosCache = cacheNivelTotalDeudaHistorica,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Total Deuda Historica procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Total Deuda Historica. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelTotalDeudaHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelTotalDeudaHistorica);
                        if (historialNivelTotalDeudaHistorica != null)
                        {
                            if (!historialNivelTotalDeudaHistorica.Generado || !busquedaNivelTotalDeudaHistorica)
                            {
                                historialNivelTotalDeudaHistorica.IdHistorial = modelo.IdHistorial;
                                historialNivelTotalDeudaHistorica.TipoFuente = Dominio.Tipos.Fuentes.NivelTotalDeudaHistorica;
                                historialNivelTotalDeudaHistorica.Generado = nivelTotalDeudaHistorica != null;
                                historialNivelTotalDeudaHistorica.Data = nivelTotalDeudaHistorica != null ? JsonConvert.SerializeObject(nivelTotalDeudaHistorica) : null;
                                historialNivelTotalDeudaHistorica.Cache = cacheNivelTotalDeudaHistorica;
                                historialNivelTotalDeudaHistorica.FechaRegistro = DateTime.Now;
                                historialNivelTotalDeudaHistorica.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelTotalDeudaHistorica);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Total Deuda Historica actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelTotalDeudaHistorica,
                                Generado = nivelTotalDeudaHistorica != null,
                                Data = nivelTotalDeudaHistorica != null ? JsonConvert.SerializeObject(nivelTotalDeudaHistorica) : null,
                                Cache = cacheNivelTotalDeudaHistorica,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Total Deuda Historica procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesTotalDeudaHistorica", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesTotalDeudaHistorica", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelEvolucionHistoricaDistribucionEndeudamientoEquifax")]
        public async Task<IActionResult> ObtenerNivelEvolucionHistoricaDistribucionEndeudamientoEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico nivelEvolucionHistorica = null;
                var busquedaNivelEvolucionHistorica = false;
                var cacheNivelEvolucionHistorica = false;
                var datos = new NivelEvolucionHistoricaViewModel();

                try
                {
                    var consultaEvolucionHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelEvolucionHistoricaDistribucionEndeudamiento && m.Generado, null, null, true);
                    if (consultaEvolucionHistorica != null && !string.IsNullOrEmpty(consultaEvolucionHistorica.Data))
                    {
                        nivelEvolucionHistorica = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico>(consultaEvolucionHistorica.Data);
                        datos = new NivelEvolucionHistoricaViewModel()
                        {
                            EvolucionHistorica = nivelEvolucionHistorica,
                            BusquedaNueva = busquedaNivelEvolucionHistorica,
                            DatosCache = consultaEvolucionHistorica.Cache,
                        };

                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesEvolucionHistorica", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoDetalleDistribucionEndeudamientoEducativo3600 == null || !datosEquifax.Resultados.RecursivoDetalleDistribucionEndeudamientoEducativo3600.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Detalle Distribución Endeudamiento Educativo 360.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelEvolucionHistorica = await _buroCreditoEquifax.GetEvolucionHistoricaDistribucionEndeudamientoAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, credencial);

                            if (nivelEvolucionHistorica != null && !nivelEvolucionHistorica.ResultadoConsultaEvolucionHistorico)
                                nivelEvolucionHistorica = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelEvolucionHistorica = Path.Combine(pathFuentes, "evolucionHistoricoDemo.json");
                            nivelEvolucionHistorica = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico>(System.IO.File.ReadAllText(pathNivelEvolucionHistorica));
                            busquedaNivelEvolucionHistorica = false;
                            cacheNivelEvolucionHistorica = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Evolución Histórica Distribución Endeudamiento {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelEvolucionHistorica == null)
                {
                    busquedaNivelEvolucionHistorica = true;
                    var datosNivelEvolucionHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelEvolucionHistoricaDistribucionEndeudamiento && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelEvolucionHistorica != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Evolución Histórica Distribución Endeudamiento con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelEvolucionHistorica = true;
                        nivelEvolucionHistorica = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico>(datosNivelEvolucionHistorica);
                    }
                }

                datos = new NivelEvolucionHistoricaViewModel()
                {
                    EvolucionHistorica = nivelEvolucionHistorica,
                    BusquedaNueva = busquedaNivelEvolucionHistorica,
                    DatosCache = cacheNivelEvolucionHistorica,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Evolución Histórica Distribución Endeudamiento procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Evolución Histórica Distribución Endeudamiento. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelEvolucionHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelEvolucionHistoricaDistribucionEndeudamiento);
                        if (historialNivelEvolucionHistorica != null)
                        {
                            if (!historialNivelEvolucionHistorica.Generado || !busquedaNivelEvolucionHistorica)
                            {
                                historialNivelEvolucionHistorica.IdHistorial = modelo.IdHistorial;
                                historialNivelEvolucionHistorica.TipoFuente = Dominio.Tipos.Fuentes.NivelEvolucionHistoricaDistribucionEndeudamiento;
                                historialNivelEvolucionHistorica.Generado = nivelEvolucionHistorica != null;
                                historialNivelEvolucionHistorica.Data = nivelEvolucionHistorica != null ? JsonConvert.SerializeObject(nivelEvolucionHistorica) : null;
                                historialNivelEvolucionHistorica.Cache = cacheNivelEvolucionHistorica;
                                historialNivelEvolucionHistorica.FechaRegistro = DateTime.Now;
                                historialNivelEvolucionHistorica.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelEvolucionHistorica);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Evolución Histórica Distribución Endeudamiento actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelEvolucionHistoricaDistribucionEndeudamiento,
                                Generado = nivelEvolucionHistorica != null,
                                Data = nivelEvolucionHistorica != null ? JsonConvert.SerializeObject(nivelEvolucionHistorica) : null,
                                Cache = cacheNivelEvolucionHistorica,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Evolución Histórica Distribución Endeudamiento procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesEvolucionHistorica", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesEvolucionHistorica", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelHistoricoEstructuraVencimientosEquifax")]
        public async Task<IActionResult> ObtenerNivelHistoricoEstructuraVencimientosEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento nivelHistoricoEstructuraVencimiento = null;
                var busquedaNivelHistoricoEstructuraVencimiento = false;
                var cacheNivelHistoricoEstructuraVencimiento = false;
                var datos = new HistoricoEstructuraVencimientoViewModel();

                try
                {
                    var consultaHistoricoEstructuraVencimiento = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelHistoricoEstructuraVencimientos && m.Generado, null, null, true);
                    if (consultaHistoricoEstructuraVencimiento != null && !string.IsNullOrEmpty(consultaHistoricoEstructuraVencimiento.Data))
                    {
                        nivelHistoricoEstructuraVencimiento = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento>(consultaHistoricoEstructuraVencimiento.Data);
                        datos = new HistoricoEstructuraVencimientoViewModel()
                        {
                            HistoricoEstructuraVencimiento = nivelHistoricoEstructuraVencimiento,
                            BusquedaNueva = busquedaNivelHistoricoEstructuraVencimiento,
                            DatosCache = consultaHistoricoEstructuraVencimiento.Cache,
                        };

                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesHistoricoEstructuraVencimiento", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoDeudaHistorica3601 == null || !datosEquifax.Resultados.RecursivoDeudaHistorica3601.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Deuda Historica 360.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelHistoricoEstructuraVencimiento = await _buroCreditoEquifax.GetHistoricoEstructuraVencimientosAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, credencial);

                            if (nivelHistoricoEstructuraVencimiento != null && !nivelHistoricoEstructuraVencimiento.ResultadoConsultaHistoricoEstructuraVencimiento)
                                nivelHistoricoEstructuraVencimiento = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelHistoricoEstructuraVencimiento = Path.Combine(pathFuentes, "historicoEstructuraVencimientoDemo.json");
                            nivelHistoricoEstructuraVencimiento = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento>(System.IO.File.ReadAllText(pathNivelHistoricoEstructuraVencimiento));
                            busquedaNivelHistoricoEstructuraVencimiento = false;
                            cacheNivelHistoricoEstructuraVencimiento = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Total Deuda Historica {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelHistoricoEstructuraVencimiento == null)
                {
                    busquedaNivelHistoricoEstructuraVencimiento = true;
                    var datosNivelHistoricoEstructuraVencimiento = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelHistoricoEstructuraVencimientos && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelHistoricoEstructuraVencimiento != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Total Deuda Historica con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelHistoricoEstructuraVencimiento = true;
                        nivelHistoricoEstructuraVencimiento = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento>(datosNivelHistoricoEstructuraVencimiento);
                    }
                }

                datos = new HistoricoEstructuraVencimientoViewModel()
                {
                    HistoricoEstructuraVencimiento = nivelHistoricoEstructuraVencimiento,
                    BusquedaNueva = busquedaNivelHistoricoEstructuraVencimiento,
                    DatosCache = cacheNivelHistoricoEstructuraVencimiento,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Total Deuda Historica procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Total Deuda Historica. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelHistoricoEstructuraVencimiento = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelHistoricoEstructuraVencimientos);
                        if (historialNivelHistoricoEstructuraVencimiento != null)
                        {
                            if (!historialNivelHistoricoEstructuraVencimiento.Generado || !busquedaNivelHistoricoEstructuraVencimiento)
                            {
                                historialNivelHistoricoEstructuraVencimiento.IdHistorial = modelo.IdHistorial;
                                historialNivelHistoricoEstructuraVencimiento.TipoFuente = Dominio.Tipos.Fuentes.NivelHistoricoEstructuraVencimientos;
                                historialNivelHistoricoEstructuraVencimiento.Generado = nivelHistoricoEstructuraVencimiento != null;
                                historialNivelHistoricoEstructuraVencimiento.Data = nivelHistoricoEstructuraVencimiento != null ? JsonConvert.SerializeObject(nivelHistoricoEstructuraVencimiento) : null;
                                historialNivelHistoricoEstructuraVencimiento.Cache = cacheNivelHistoricoEstructuraVencimiento;
                                historialNivelHistoricoEstructuraVencimiento.FechaRegistro = DateTime.Now;
                                historialNivelHistoricoEstructuraVencimiento.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelHistoricoEstructuraVencimiento);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Total Deuda Historica actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelHistoricoEstructuraVencimientos,
                                Generado = nivelHistoricoEstructuraVencimiento != null,
                                Data = nivelHistoricoEstructuraVencimiento != null ? JsonConvert.SerializeObject(nivelHistoricoEstructuraVencimiento) : null,
                                Cache = cacheNivelHistoricoEstructuraVencimiento,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Total Deuda Historica procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesHistoricoEstructuraVencimiento", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesHistoricoEstructuraVencimiento", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelSaldoPorVencerPorInstitucionEquifax")]
        public async Task<IActionResult> ObtenerNivelSaldoPorVencerPorInstitucionEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (string.IsNullOrEmpty(modelo.CodigoInstitucion?.Trim()))
                    throw new Exception("El campo Código Institución es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoSaldoVencerInstitucion nivelSaldoVencerInstitucion = null;
                var busquedaNivelSaldoVencerInstitucion = false;
                var cacheNivelSaldoVencerInstitucion = false;
                var datos = new NivelSaldoPorVencerPorInstitucionViewModel();
                var lstDatos = new List<NivelSaldoPorVencerPorInstitucionViewModel>();

                try
                {
                    var consultaEvolucionHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelSaldoPorVencerPorInstitucion && m.Generado, null, null, true);
                    if (consultaEvolucionHistorica != null && !string.IsNullOrEmpty(consultaEvolucionHistorica.Data))
                        lstDatos = JsonConvert.DeserializeObject<List<NivelSaldoPorVencerPorInstitucionViewModel>>(consultaEvolucionHistorica.Data);

                    if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()))
                    {
                        nivelSaldoVencerInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()).SaldoVencerInstitucion;
                        datos = new NivelSaldoPorVencerPorInstitucionViewModel()
                        {
                            SaldoVencerInstitucion = nivelSaldoVencerInstitucion,
                            BusquedaNueva = busquedaNivelSaldoVencerInstitucion,
                            DatosCache = consultaEvolucionHistorica.Cache,
                        };
                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesSaldoVencerInstitucion", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoComposicionEstructuraDeVencimiento == null || !datosEquifax.Resultados.RecursivoComposicionEstructuraDeVencimiento.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Composicion Estructura De Vencimiento.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelSaldoVencerInstitucion = await _buroCreditoEquifax.GetSaldoPorVencerPorInstitucionAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, modelo.CodigoInstitucion.Trim(), credencial);

                            if (nivelSaldoVencerInstitucion != null && !nivelSaldoVencerInstitucion.ResultadoConsultaNivelSaldoVencerInstitucion)
                                nivelSaldoVencerInstitucion = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelEvolucionHistorica = Path.Combine(pathFuentes, "nivelSaldoVencerInstitucionDemo.json");
                            nivelSaldoVencerInstitucion = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoSaldoVencerInstitucion>(System.IO.File.ReadAllText(pathNivelEvolucionHistorica));
                            busquedaNivelSaldoVencerInstitucion = false;
                            cacheNivelSaldoVencerInstitucion = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Saldo Por Vencer Por Institución {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelSaldoVencerInstitucion == null)
                {
                    busquedaNivelSaldoVencerInstitucion = true;
                    var datosNivelEvolucionHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelSaldoPorVencerPorInstitucion && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelEvolucionHistorica != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Saldo Por Vencer Por Institución con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelSaldoVencerInstitucion = true;
                        lstDatos = JsonConvert.DeserializeObject<List<NivelSaldoPorVencerPorInstitucionViewModel>>(datosNivelEvolucionHistorica);
                        if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()))
                            nivelSaldoVencerInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()).SaldoVencerInstitucion;
                    }
                }

                datos = new NivelSaldoPorVencerPorInstitucionViewModel()
                {
                    SaldoVencerInstitucion = nivelSaldoVencerInstitucion,
                    BusquedaNueva = busquedaNivelSaldoVencerInstitucion,
                    DatosCache = cacheNivelSaldoVencerInstitucion,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Saldo Por Vencer Por Institución procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Saldo Por Vencer Por Institución. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelEvolucionHistorica = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelSaldoPorVencerPorInstitucion);
                        if (historialNivelEvolucionHistorica != null)
                        {
                            var lstEvolucionHistorica = new List<NivelSaldoPorVencerPorInstitucionViewModel>();
                            if (historialNivelEvolucionHistorica.Data != null)
                                lstEvolucionHistorica = JsonConvert.DeserializeObject<List<NivelSaldoPorVencerPorInstitucionViewModel>>(historialNivelEvolucionHistorica.Data);

                            if (nivelSaldoVencerInstitucion != null)
                                lstEvolucionHistorica.Add(new NivelSaldoPorVencerPorInstitucionViewModel() { SaldoVencerInstitucion = nivelSaldoVencerInstitucion, CodigoInstitucion = modelo.CodigoInstitucion.Trim() });

                            if (!lstEvolucionHistorica.Any())
                                lstEvolucionHistorica = null;

                            if (!historialNivelEvolucionHistorica.Generado || !busquedaNivelSaldoVencerInstitucion)
                            {
                                historialNivelEvolucionHistorica.IdHistorial = modelo.IdHistorial;
                                historialNivelEvolucionHistorica.TipoFuente = Dominio.Tipos.Fuentes.NivelSaldoPorVencerPorInstitucion;
                                historialNivelEvolucionHistorica.Generado = nivelSaldoVencerInstitucion != null || (nivelSaldoVencerInstitucion == null && lstEvolucionHistorica != null && lstEvolucionHistorica.Any());
                                historialNivelEvolucionHistorica.Data = (nivelSaldoVencerInstitucion != null || (nivelSaldoVencerInstitucion == null && lstEvolucionHistorica != null && lstEvolucionHistorica.Any())) ? JsonConvert.SerializeObject(lstEvolucionHistorica) : null;
                                historialNivelEvolucionHistorica.Cache = cacheNivelSaldoVencerInstitucion;
                                historialNivelEvolucionHistorica.FechaRegistro = DateTime.Now;
                                historialNivelEvolucionHistorica.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelEvolucionHistorica);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Saldo Por Vencer Por Institución actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelSaldoPorVencerPorInstitucion,
                                Generado = nivelSaldoVencerInstitucion != null,
                                Data = nivelSaldoVencerInstitucion != null ? JsonConvert.SerializeObject(new List<NivelSaldoPorVencerPorInstitucionViewModel>() { new NivelSaldoPorVencerPorInstitucionViewModel() { SaldoVencerInstitucion = nivelSaldoVencerInstitucion, CodigoInstitucion = modelo.CodigoInstitucion.Trim() } }) : null,
                                Cache = cacheNivelSaldoVencerInstitucion,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Saldo Por Vencer Por Institución procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesSaldoVencerInstitucion", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesSaldoVencerInstitucion", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelOperacionesInstitucionEquifax")]
        public async Task<IActionResult> ObtenerNivelOperacionesInstitucionEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (string.IsNullOrEmpty(modelo.CodigoInstitucion?.Trim()))
                    throw new Exception("El campo Código Institución es obligatorio");

                if (string.IsNullOrEmpty(modelo.TipoCredito?.Trim()))
                    throw new Exception("El campo Tipo Crédito es obligatorio");

                if (modelo.FechaCorte.HasValue && modelo.FechaCorte.Value == default)
                    throw new Exception("El campo Fecha Corte es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoOperacionInstitucion nivelOperacionInstitucion = null;
                var busquedaNivelOperacionInstitucion = false;
                var cacheNivelOperacionInstitucion = false;
                var datos = new NivelOperacionInstitucionViewModel();
                var lstDatos = new List<NivelOperacionInstitucionViewModel>();

                try
                {
                    var consultaOperacionInstitucion = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucion && m.Generado, null, null, true);
                    if (consultaOperacionInstitucion != null && !string.IsNullOrEmpty(consultaOperacionInstitucion.Data))
                        lstDatos = JsonConvert.DeserializeObject<List<NivelOperacionInstitucionViewModel>>(consultaOperacionInstitucion.Data);

                    if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")))
                    {
                        nivelOperacionInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")).OperacionInstitucion;
                        datos = new NivelOperacionInstitucionViewModel()
                        {
                            OperacionInstitucion = nivelOperacionInstitucion,
                            BusquedaNueva = busquedaNivelOperacionInstitucion,
                            DatosCache = consultaOperacionInstitucion.Cache,
                        };
                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesOperacionInstitucion", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoDetalleDistribucionEndeudamientoEducativo3600 == null || !datosEquifax.Resultados.RecursivoDetalleDistribucionEndeudamientoEducativo3600.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Composicion Estructura De Vencimiento.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelOperacionInstitucion = await _buroCreditoEquifax.GetOperacionesPorInstitucionAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, modelo.CodigoInstitucion.Trim(), modelo.TipoCredito.Trim(), "V", modelo.FechaCorte.Value.ToString("yyyy-MM-dd"), credencial);

                            if (nivelOperacionInstitucion != null && !nivelOperacionInstitucion.ResultadoConsultaNivelOperacionInstitucion)
                                nivelOperacionInstitucion = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelOperacionInstitucion = Path.Combine(pathFuentes, "nivelOperacionInstitucionDemo.json");
                            nivelOperacionInstitucion = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoOperacionInstitucion>(System.IO.File.ReadAllText(pathNivelOperacionInstitucion));
                            busquedaNivelOperacionInstitucion = false;
                            cacheNivelOperacionInstitucion = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Operaciones Por Institución {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelOperacionInstitucion == null)
                {
                    busquedaNivelOperacionInstitucion = true;
                    var datosNivelOperacionInstitucion = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucion && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelOperacionInstitucion != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Operaciones Por Institución con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelOperacionInstitucion = true;
                        lstDatos = JsonConvert.DeserializeObject<List<NivelOperacionInstitucionViewModel>>(datosNivelOperacionInstitucion);
                        if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")))
                            nivelOperacionInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")).OperacionInstitucion;
                    }
                }

                datos = new NivelOperacionInstitucionViewModel()
                {
                    OperacionInstitucion = nivelOperacionInstitucion,
                    BusquedaNueva = busquedaNivelOperacionInstitucion,
                    DatosCache = cacheNivelOperacionInstitucion,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Operaciones Por Institución procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Operaciones Por Institución. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelOperacionInstitucion = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucion);
                        if (historialNivelOperacionInstitucion != null)
                        {
                            var lstOperacionInstitucion = new List<NivelOperacionInstitucionViewModel>();

                            if (historialNivelOperacionInstitucion.Data != null)
                                lstOperacionInstitucion = JsonConvert.DeserializeObject<List<NivelOperacionInstitucionViewModel>>(historialNivelOperacionInstitucion.Data);

                            if (nivelOperacionInstitucion != null)
                                lstOperacionInstitucion.Add(new NivelOperacionInstitucionViewModel() { OperacionInstitucion = nivelOperacionInstitucion, CodigoInstitucion = modelo.CodigoInstitucion.Trim(), TipoCredito = modelo.TipoCredito.Trim(), FechaCorte = modelo.FechaCorte.Value.ToString("yyyy-MM-dd") });

                            if (!lstOperacionInstitucion.Any())
                                lstOperacionInstitucion = null;

                            if (!historialNivelOperacionInstitucion.Generado || !busquedaNivelOperacionInstitucion)
                            {
                                historialNivelOperacionInstitucion.IdHistorial = modelo.IdHistorial;
                                historialNivelOperacionInstitucion.TipoFuente = Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucion;
                                historialNivelOperacionInstitucion.Generado = nivelOperacionInstitucion != null || (nivelOperacionInstitucion == null && lstOperacionInstitucion != null && lstOperacionInstitucion.Any());
                                historialNivelOperacionInstitucion.Data = (nivelOperacionInstitucion != null || (nivelOperacionInstitucion == null && lstOperacionInstitucion != null && lstOperacionInstitucion.Any())) ? JsonConvert.SerializeObject(lstOperacionInstitucion) : null;
                                historialNivelOperacionInstitucion.Cache = cacheNivelOperacionInstitucion;
                                historialNivelOperacionInstitucion.FechaRegistro = DateTime.Now;
                                historialNivelOperacionInstitucion.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelOperacionInstitucion);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Operaciones Por Institución actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucion,
                                Generado = nivelOperacionInstitucion != null,
                                Data = nivelOperacionInstitucion != null ? JsonConvert.SerializeObject(new List<NivelOperacionInstitucionViewModel>() { new NivelOperacionInstitucionViewModel() { OperacionInstitucion = nivelOperacionInstitucion, CodigoInstitucion = modelo.CodigoInstitucion.Trim(), TipoCredito = modelo.TipoCredito.Trim(), FechaCorte = modelo.FechaCorte.Value.ToString("yyyy-MM-dd") } }) : null,
                                Cache = cacheNivelOperacionInstitucion,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Operaciones Por Institución procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesOperacionInstitucion", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesOperacionInstitucion", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelDetalleVencidoPorInstitucionEquifax")]
        public async Task<IActionResult> ObtenerNivelDetalleVencidoPorInstitucionEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (string.IsNullOrEmpty(modelo.CodigoInstitucion?.Trim()))
                    throw new Exception("El campo Código Institución es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoDetalleVencidoPorInstitucion nivelDetalleVencidoInstitucion = null;
                var busquedaNivelDetalleVencidoInstitucion = false;
                var cacheNivelDetalleVencidoInstitucion = false;
                var datos = new NivelDetalleVencidoInstitucionViewModel();
                var lstDatos = new List<NivelDetalleVencidoInstitucionViewModel>();

                try
                {
                    var consultaDetalleVencidoInstitucion = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleVencidoPorInstitucion && m.Generado, null, null, true);
                    if (consultaDetalleVencidoInstitucion != null && !string.IsNullOrEmpty(consultaDetalleVencidoInstitucion.Data))
                        lstDatos = JsonConvert.DeserializeObject<List<NivelDetalleVencidoInstitucionViewModel>>(consultaDetalleVencidoInstitucion.Data);

                    if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()))
                    {
                        nivelDetalleVencidoInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()).DetalleVencidoInstitucion;
                        datos = new NivelDetalleVencidoInstitucionViewModel()
                        {
                            DetalleVencidoInstitucion = nivelDetalleVencidoInstitucion,
                            BusquedaNueva = busquedaNivelDetalleVencidoInstitucion,
                            DatosCache = consultaDetalleVencidoInstitucion.Cache,
                        };
                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesDetalleVencidoPorInstitucion", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoComposicionEstructuraDeVencimiento == null || !datosEquifax.Resultados.RecursivoComposicionEstructuraDeVencimiento.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Composicion Estructura De Vencimiento.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelDetalleVencidoInstitucion = await _buroCreditoEquifax.GetDetalleVencidoPorInstitucionAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, modelo.CodigoInstitucion.Trim(), credencial);

                            if (nivelDetalleVencidoInstitucion != null && !nivelDetalleVencidoInstitucion.ResultadoConsultaDetalleVencidoPorInstitucion)
                                nivelDetalleVencidoInstitucion = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelDetalleVencidoPorInstitucion = Path.Combine(pathFuentes, "nivelDetalleVencidoPorInstitucionDemo.json");
                            nivelDetalleVencidoInstitucion = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoDetalleVencidoPorInstitucion>(System.IO.File.ReadAllText(pathNivelDetalleVencidoPorInstitucion));
                            busquedaNivelDetalleVencidoInstitucion = false;
                            cacheNivelDetalleVencidoInstitucion = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Operaciones Por Institución {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelDetalleVencidoInstitucion == null)
                {
                    busquedaNivelDetalleVencidoInstitucion = true;
                    var datosNivelDetalleVencidoInstitucion = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleVencidoPorInstitucion && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelDetalleVencidoInstitucion != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Operaciones Por Institución con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelDetalleVencidoInstitucion = true;
                        lstDatos = JsonConvert.DeserializeObject<List<NivelDetalleVencidoInstitucionViewModel>>(datosNivelDetalleVencidoInstitucion);
                        if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()))
                            nivelDetalleVencidoInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()).DetalleVencidoInstitucion;
                    }
                }

                datos = new NivelDetalleVencidoInstitucionViewModel()
                {
                    DetalleVencidoInstitucion = nivelDetalleVencidoInstitucion,
                    BusquedaNueva = busquedaNivelDetalleVencidoInstitucion,
                    DatosCache = cacheNivelDetalleVencidoInstitucion,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Operaciones Por Institución procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Operaciones Por Institución. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelDetalleVencidoInstitucion = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleVencidoPorInstitucion);
                        if (historialNivelDetalleVencidoInstitucion != null)
                        {
                            var lstDetalleVencidoInstitucion = new List<NivelDetalleVencidoInstitucionViewModel>();
                            if (historialNivelDetalleVencidoInstitucion.Data != null)
                                lstDetalleVencidoInstitucion = JsonConvert.DeserializeObject<List<NivelDetalleVencidoInstitucionViewModel>>(historialNivelDetalleVencidoInstitucion.Data);

                            if (nivelDetalleVencidoInstitucion != null)
                                lstDetalleVencidoInstitucion.Add(new NivelDetalleVencidoInstitucionViewModel() { DetalleVencidoInstitucion = nivelDetalleVencidoInstitucion, CodigoInstitucion = modelo.CodigoInstitucion.Trim() });

                            if (!lstDetalleVencidoInstitucion.Any())
                                lstDetalleVencidoInstitucion = null;

                            if (!historialNivelDetalleVencidoInstitucion.Generado || !busquedaNivelDetalleVencidoInstitucion)
                            {
                                historialNivelDetalleVencidoInstitucion.IdHistorial = modelo.IdHistorial;
                                historialNivelDetalleVencidoInstitucion.TipoFuente = Dominio.Tipos.Fuentes.NivelDetalleVencidoPorInstitucion;
                                historialNivelDetalleVencidoInstitucion.Generado = nivelDetalleVencidoInstitucion != null || (nivelDetalleVencidoInstitucion == null && lstDetalleVencidoInstitucion != null && lstDetalleVencidoInstitucion.Any());
                                historialNivelDetalleVencidoInstitucion.Data = (nivelDetalleVencidoInstitucion != null || (nivelDetalleVencidoInstitucion == null && lstDetalleVencidoInstitucion != null && lstDetalleVencidoInstitucion.Any())) ? JsonConvert.SerializeObject(lstDetalleVencidoInstitucion) : null;
                                historialNivelDetalleVencidoInstitucion.Cache = cacheNivelDetalleVencidoInstitucion;
                                historialNivelDetalleVencidoInstitucion.FechaRegistro = DateTime.Now;
                                historialNivelDetalleVencidoInstitucion.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelDetalleVencidoInstitucion);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Operaciones Por Institución actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelDetalleVencidoPorInstitucion,
                                Generado = nivelDetalleVencidoInstitucion != null,
                                Data = nivelDetalleVencidoInstitucion != null ? JsonConvert.SerializeObject(new List<NivelDetalleVencidoInstitucionViewModel>() { new NivelDetalleVencidoInstitucionViewModel() { DetalleVencidoInstitucion = nivelDetalleVencidoInstitucion, CodigoInstitucion = modelo.CodigoInstitucion.Trim() } }) : null,
                                Cache = cacheNivelDetalleVencidoInstitucion,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Operaciones Por Institución procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesDetalleVencidoPorInstitucion", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesDetalleVencidoPorInstitucion", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelDetalleOperacionesEntidadesEquifax")]
        public async Task<IActionResult> ObtenerNivelDetalleOperacionesEntidadesEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.FechaCorte.HasValue && modelo.FechaCorte.Value == default)
                    throw new Exception("El campo Fecha Corte es obligatorio");

                if (string.IsNullOrEmpty(modelo.SistemaCrediticio?.Trim()))
                    throw new Exception("El campo Sistema Crediticio es obligatorio");

                var idUsuario = User.GetUserId<int>();

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                Historial historialTemp = null;
                Externos.Logica.Equifax.Modelos.ResultadoDetalleOperacionEntidad nivelDetalleOperacionEntidad = null;
                var busquedaNivelDetalleOperacionEntidad = false;
                var cacheNivelDetalleOperacionEntidad = false;
                var datos = new NivelDetalleOperacionEntidadViewModel();
                var lstDatos = new List<NivelDetalleOperacionEntidadViewModel>();

                try
                {
                    var consultaDetalleOperacionEntidad = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleOperacionesYEntidades && m.Generado, null, null, true);
                    if (consultaDetalleOperacionEntidad != null && !string.IsNullOrEmpty(consultaDetalleOperacionEntidad.Data))
                        lstDatos = JsonConvert.DeserializeObject<List<NivelDetalleOperacionEntidadViewModel>>(consultaDetalleOperacionEntidad.Data);

                    if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd") && x.SistemaCrediticio == modelo.SistemaCrediticio.Trim()))
                    {
                        nivelDetalleOperacionEntidad = lstDatos.FirstOrDefault(x => x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd") && x.SistemaCrediticio == modelo.SistemaCrediticio.Trim()).DetalleOperacionEntidad;
                        datos = new NivelDetalleOperacionEntidadViewModel()
                        {
                            DetalleOperacionEntidad = nivelDetalleOperacionEntidad,
                            BusquedaNueva = busquedaNivelDetalleOperacionEntidad,
                            DatosCache = consultaDetalleOperacionEntidad.Cache,
                        };
                        return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesDetalleOperacionesEntidades", datos);
                    }
                    else
                    {
                        var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                        var consultaEquifax = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == modelo.IdHistorial && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito, null, null, true);

                        if (string.IsNullOrEmpty(consultaEquifax))
                            throw new Exception($"No se pudo obtener datos del Buró de crédito Equifax para la identificación: {modelo.Identificacion}");

                        var datosEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(consultaEquifax);

                        if (datosEquifax != null && string.IsNullOrEmpty(datosEquifax.IdCodigoConsulta))
                            throw new Exception("No se puedo obtener el Código de la Consulta.");

                        if (datosEquifax != null && (datosEquifax.Resultados.RecursivoDeudaHistorica3601 == null || !datosEquifax.Resultados.RecursivoDeudaHistorica3601.Any()))
                            throw new Exception("No se puedo obtener datos de la tabla Recursivo Composicion Estructura De Vencimiento.");

                        var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                        var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                        if (!cacheBuro && ambiente == "Production")
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave };

                            var tipoIdentificacion = string.Empty;
                            if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula)
                                tipoIdentificacion = "C";
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                tipoIdentificacion = "C";
                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                            }
                            else if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.TipoIdentificacion) && (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico))
                                tipoIdentificacion = "R";

                            nivelDetalleOperacionEntidad = await _buroCreditoEquifax.GetDetalleOperacionesYEntidadesAsync(datosEquifax.IdCodigoConsulta, tipoIdentificacion, modelo.Identificacion, modelo.FechaCorte.Value.ToString("yyyy-MM-dd"), modelo.SistemaCrediticio.Trim(), credencial);

                            if (nivelDetalleOperacionEntidad != null && !nivelDetalleOperacionEntidad.ResultadoConsultaNivelDetalleOperacionEntidad)
                                nivelDetalleOperacionEntidad = null;
                        }
                        else
                        {
                            var pathBase = System.IO.Path.Combine("wwwroot", "data");
                            var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                            var pathNivelDetalleOperacionEntidad = Path.Combine(pathFuentes, "nivelDetalleOperacionEntidadDemo.json");
                            nivelDetalleOperacionEntidad = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoDetalleOperacionEntidad>(System.IO.File.ReadAllText(pathNivelDetalleOperacionEntidad));
                            busquedaNivelDetalleOperacionEntidad = false;
                            cacheNivelDetalleOperacionEntidad = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar Nivel Operaciones Por Institución {modelo.Identificacion}: {ex.Message}");
                }

                if (nivelDetalleOperacionEntidad == null)
                {
                    busquedaNivelDetalleOperacionEntidad = true;
                    var datosNivelDetalleOperacionEntidad = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleOperacionesYEntidades && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosNivelDetalleOperacionEntidad != null)
                    {
                        _logger.LogInformation($"Procesando Nivel Operaciones Por Institución con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheNivelDetalleOperacionEntidad = true;
                        lstDatos = JsonConvert.DeserializeObject<List<NivelDetalleOperacionEntidadViewModel>>(datosNivelDetalleOperacionEntidad);
                        if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.FechaCorte == modelo.CodigoInstitucion.Trim() && x.SistemaCrediticio == modelo.SistemaCrediticio.Trim()))
                            nivelDetalleOperacionEntidad = lstDatos.FirstOrDefault(x => x.FechaCorte == modelo.CodigoInstitucion.Trim() && x.SistemaCrediticio == modelo.SistemaCrediticio.Trim()).DetalleOperacionEntidad;
                    }
                }

                datos = new NivelDetalleOperacionEntidadViewModel()
                {
                    DetalleOperacionEntidad = nivelDetalleOperacionEntidad,
                    BusquedaNueva = busquedaNivelDetalleOperacionEntidad,
                    DatosCache = cacheNivelDetalleOperacionEntidad,
                };

                _logger.LogInformation("Fuente de Buró de Crédito Equifax Nivel Operaciones Por Institución procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito Equifax Nivel Operaciones Por Institución. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historialNivelDetalleOperacionEntidad = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleOperacionesYEntidades);
                        if (historialNivelDetalleOperacionEntidad != null)
                        {
                            var lstDetalleOperacionEntidad = new List<NivelDetalleOperacionEntidadViewModel>();
                            if (historialNivelDetalleOperacionEntidad.Data != null)
                                lstDetalleOperacionEntidad = JsonConvert.DeserializeObject<List<NivelDetalleOperacionEntidadViewModel>>(historialNivelDetalleOperacionEntidad.Data);

                            if (nivelDetalleOperacionEntidad != null)
                                lstDetalleOperacionEntidad.Add(new NivelDetalleOperacionEntidadViewModel() { DetalleOperacionEntidad = nivelDetalleOperacionEntidad, FechaCorte = modelo.FechaCorte.Value.ToString("yyyy-MM-dd"), SistemaCrediticio = modelo.SistemaCrediticio.Trim() });

                            if (!lstDetalleOperacionEntidad.Any())
                                lstDetalleOperacionEntidad = null;

                            if (!historialNivelDetalleOperacionEntidad.Generado || !busquedaNivelDetalleOperacionEntidad)
                            {
                                historialNivelDetalleOperacionEntidad.IdHistorial = modelo.IdHistorial;
                                historialNivelDetalleOperacionEntidad.TipoFuente = Dominio.Tipos.Fuentes.NivelDetalleOperacionesYEntidades;
                                historialNivelDetalleOperacionEntidad.Generado = nivelDetalleOperacionEntidad != null || (nivelDetalleOperacionEntidad == null && lstDetalleOperacionEntidad != null && lstDetalleOperacionEntidad.Any());
                                historialNivelDetalleOperacionEntidad.Data = (nivelDetalleOperacionEntidad != null || (nivelDetalleOperacionEntidad == null && lstDetalleOperacionEntidad != null && lstDetalleOperacionEntidad.Any())) ? JsonConvert.SerializeObject(lstDetalleOperacionEntidad) : null;
                                historialNivelDetalleOperacionEntidad.Cache = cacheNivelDetalleOperacionEntidad;
                                historialNivelDetalleOperacionEntidad.FechaRegistro = DateTime.Now;
                                historialNivelDetalleOperacionEntidad.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialNivelDetalleOperacionEntidad);
                                _logger.LogInformation("Historial de la Fuente Equifax - Nivel Operaciones Por Institución actualizado correctamente");
                            }
                        }
                        else
                        {
                            await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                            {
                                IdHistorial = modelo.IdHistorial,
                                TipoFuente = Dominio.Tipos.Fuentes.NivelDetalleOperacionesYEntidades,
                                Generado = nivelDetalleOperacionEntidad != null,
                                Data = nivelDetalleOperacionEntidad != null ? JsonConvert.SerializeObject(new List<NivelDetalleOperacionEntidadViewModel>() { new NivelDetalleOperacionEntidadViewModel() { DetalleOperacionEntidad = nivelDetalleOperacionEntidad, FechaCorte = modelo.FechaCorte.Value.ToString("yyyy-MM-dd"), SistemaCrediticio = modelo.SistemaCrediticio.Trim() } }) : null,
                                Cache = cacheNivelDetalleOperacionEntidad,
                                FechaRegistro = DateTime.Now,
                                Reintento = false
                            });
                            _logger.LogInformation("Historial de la Fuente Equifax - Nivel Operaciones Por Institución procesado correctamente");
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesDetalleOperacionesEntidades", datos);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return PartialView($"../Shared/Fuentes/FuentesEquifax/_FuentesDetalleOperacionesEntidades", null);
            }
        }
        #endregion ConsultasEquifax
    }
}
