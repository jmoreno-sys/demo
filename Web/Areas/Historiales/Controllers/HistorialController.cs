// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Persistencia.Repositorios.Balance;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Data;
using Web.References;
using System.IO;
using Newtonsoft.Json.Linq;
using Web.Areas.Historiales.Models;
using Newtonsoft.Json;
using Persistencia.Repositorios.Identidad;
using Web.Areas.Consultas.Models;
using Externos.Logica.SRi.Modelos;
using Externos.Logica.Garancheck.Modelos;
using Externos.Logica.IESS.Modelos;
using Externos.Logica.Balances.Modelos;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Externos.Logica.PredioMunicipio.Modelos;
using Web.Models;
using Dominio.Entidades.Balances;
using Dominio.Tipos;
using Externos.Logica.FJudicial.Modelos;

namespace Web.Areas.Historiales.Controllers
{
    [Area("Historiales")]
    [Route("Historiales/Historial")]
    [Authorize]
    public class HistorialController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detalleHistorial;
        private readonly IUsuarios _usuarios;
        private readonly ICalificaciones _calificaciones;
        private readonly IParametrosClientesHistoriales _parametrosClientesHistoriales;
        private readonly Externos.Logica.Balances.Controlador _balances;
        private readonly IReportesConsolidados _reporteConsolidado;

        public HistorialController(IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IHttpContextAccessor httpContext,
            IHistoriales historiales,
            IDetallesHistorial detallesHistorial,
            IUsuarios usuarios,
            ICalificaciones calificaciones,
            IParametrosClientesHistoriales parametrosClientesHistoriales,
            Externos.Logica.Balances.Controlador balances,
            IReportesConsolidados reportesConsolidados)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _httpContext = httpContext;
            _historiales = historiales;
            _detalleHistorial = detallesHistorial;
            _usuarios = usuarios;
            _calificaciones = calificaciones;
            _balances = balances;
            _parametrosClientesHistoriales = parametrosClientesHistoriales;
            _reporteConsolidado = reportesConsolidados;
        }

        [Route("Inicio")]
        [Authorize(Policy = "HistorialUsuario")]
        public IActionResult Inicio()
        {
            return View();
        }

        [Route("InicioHistorialGeneral")]
        [Authorize(Policy = "HistorialGeneral")]
        public IActionResult InicioHistorialGeneral()
        {
            ViewBag.TipoHistorial = Dominio.Tipos.Pantallas.HistorialGeneral;
            return View("Inicio");
        }

        [Route("InicioHistorialEmpresa")]
        [Authorize(Policy = "HistorialEmpresa")]
        public IActionResult InicioHistorialEmpresa()
        {
            ViewBag.TipoHistorial = Dominio.Tipos.Pantallas.HistorialEmpresa;
            return View("Inicio");
        }

        #region Reportes
        [HttpPost]
        [Route("ListadoHistorial")]
        [Authorize(Policy = "HistorialUsuario")]
        public async Task<IActionResult> ListadoHistorial([FromBody] HistorialFiltrosViewModel filtros)
        {
            try
            {
                if (filtros == null)
                    throw new Exception("No se han ingresado los filtros de búsqueda");

                var idUsuario = User.GetUserId<int>();
                if (filtros.Meses.HasValue && filtros.Meses.Value)
                {
                    var fecha = filtros.FechaHasta;
                    var primerDiadelMes = new DateTime(fecha.Year, fecha.Month, 1);
                    var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);
                    filtros.FechaDesde = primerDiadelMes;
                    filtros.FechaHasta = ultimoDiadelMes;
                }

                if (filtros == null)
                    throw new Exception("No se ha ingresado ningún filtro");

                if (filtros.FechaDesde.Date == default || filtros.FechaHasta.Date == default)
                    throw new Exception("Las fechas ingresadas no son válidas");

                if (filtros.FechaDesde.Date > filtros.FechaHasta.Date)
                    throw new Exception("La Fecha Hasta no puede ser anterior a la Fecha Desde");

                var idEmpresa = await _usuarios.FirstOrDefaultAsync(m => m.IdEmpresa, m => m.Id == idUsuario);
                _logger.LogInformation($"Histórico usuario {idUsuario} desde: {filtros.FechaDesde.ToString("dd/MM/yyyy")} - {filtros.FechaHasta.ToString("dd/MM/yyyy")}");
                var historial = await _historiales.ReadAsync(m => new
                {
                    m.Id,
                    m.IdUsuario,
                    m.Identificacion,
                    m.TipoIdentificacion,
                    Periodo = JObject.Parse(m.ParametrosBusqueda)["Periodos"],
                    TipoConsulta = m.TipoConsulta.GetEnumDescription(),
                    FechaHora = m.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    m.Fecha,
                    NombreUsuario = m.Usuario.NombreCompleto,
                    RazonSocial = m.RazonSocialEmpresa,
                    NombrePersona = m.NombresPersona,
                    NombreEmpresa = m.Usuario.Empresa.RazonSocial,
                    IdentificacionEmpresa = m.Usuario.Empresa.Identificacion,
                    m.Usuario.IdEmpresa,
                    ConsultaBuro = m.IdPlanBuroCredito.HasValue && m.IdPlanBuroCredito.Value > 0 && m.IdUsuario == idUsuario,
                    ConsultaEvaluacion = m.IdPlanEvaluacion.HasValue && m.IdPlanEvaluacion.Value > 0 && m.IdUsuario == idUsuario,
                    AprobadoEvaluacion = m.Calificaciones.Any() ? m.Calificaciones.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Evaluacion && m.Aprobado) && (m.Calificaciones.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro) ? m.Calificaciones.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro && m.Aprobado) : true) : false,
                    FuenteBuro = m.IdUsuario == idUsuario && m.TipoFuenteBuro.HasValue && m.TipoFuenteBuro.Value > 0 ? m.TipoFuenteBuro.GetEnumDescription() : null,
                }, m => m.IdUsuario == idUsuario && m.Fecha.Date >= filtros.FechaDesde.Date && m.Fecha.Date <= filtros.FechaHasta.Date);

                return Json(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ListadoHistorialGeneral")]
        [Authorize(Policy = "HistorialGeneral")]
        public async Task<IActionResult> ListadoHistorialGeneral([FromBody] HistorialGeneralFiltrosViewModel filtros)
        {
            try
            {
                if (filtros == null)
                    throw new Exception("No se han ingresado los filtros de búsqueda");

                if (filtros.FiltroApagado.HasValue && filtros.FiltroApagado.Value)
                {
                    var fecha = filtros.FechaHasta;
                    var primerDiadelMes = new DateTime(fecha.Year, fecha.Month, fecha.Day);
                    var ultimoDiadelMes = new DateTime(fecha.Year, fecha.Month, fecha.Day);
                    filtros.FechaDesde = primerDiadelMes;
                    filtros.FechaHasta = ultimoDiadelMes;
                }
                else if (filtros.Meses.HasValue && filtros.Meses.Value)
                {
                    var fecha = filtros.FechaHasta;
                    var primerDiadelMes = new DateTime(fecha.Year, fecha.Month, 1);
                    var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);
                    filtros.FechaDesde = primerDiadelMes;
                    filtros.FechaHasta = ultimoDiadelMes;
                }

                if (filtros == null)
                    throw new Exception("No se ha ingresado ningún filtro");

                if (filtros.FechaDesde.Date == default || filtros.FechaHasta.Date == default)
                    throw new Exception("Las fechas ingresadas no son válidas");

                if (filtros.FechaDesde.Date > filtros.FechaHasta.Date)
                    throw new Exception("La Fecha Hasta no puede ser anterior a la Fecha Desde");

                var idUsuario = User.GetUserId<int>();
                var idEmpresa = await _usuarios.FirstOrDefaultAsync(m => m.IdEmpresa, m => m.Id == idUsuario);

                var lstEmpresas = new List<int>();
                if (!string.IsNullOrEmpty(filtros.Empresas))
                    lstEmpresas = JsonConvert.DeserializeObject<List<string>>(filtros.Empresas).Select(m => int.Parse(m)).ToList();

                var historial = await _reporteConsolidado.ReadAsync(m => new
                {
                    Id = m.HistorialId,
                    m.IdUsuario,
                    m.DireccionIp,
                    m.Identificacion,
                    m.TipoIdentificacion,
                    TipoConsulta = m.TipoConsulta.GetEnumDescription(),
                    Periodo = JObject.Parse(m.ParametrosBusqueda)["Periodos"],
                    FechaHora = m.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    m.Fecha,
                    m.NombreUsuario,
                    m.RazonSocial,
                    m.NombrePersona,
                    m.NombreEmpresa,
                    m.IdentificacionEmpresa,
                    m.IdEmpresa,
                    m.ConsultaBuro,
                    m.ConsultaEvaluacion,
                    m.AprobadoEvaluacion,
                    FuenteBuro = m.FuenteBuro.GetEnumDescription()
                }, m => (lstEmpresas.Any() ? lstEmpresas.Contains(m.IdEmpresa) : true) && m.Fecha.Date >= filtros.FechaDesde.Date && m.Fecha.Date <= filtros.FechaHasta.Date);

                return Json(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ListadoHistorialEmpresa")]
        [Authorize(Policy = "HistorialEmpresa")]
        public async Task<IActionResult> ListadoHistorialEmpresa([FromBody] HistorialFiltrosViewModel filtros)
        {
            try
            {
                if (filtros == null)
                    throw new Exception("No se han ingresado los filtros de búsqueda");

                if (filtros.Meses.HasValue && filtros.Meses.Value)
                {
                    var fecha = filtros.FechaHasta;
                    var primerDiadelMes = new DateTime(fecha.Year, fecha.Month, 1);
                    var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);
                    filtros.FechaDesde = primerDiadelMes;
                    filtros.FechaHasta = ultimoDiadelMes;
                }

                if (filtros == null)
                    throw new Exception("No se ha ingresado ningún filtro");

                if (filtros.FechaDesde.Date == default || filtros.FechaHasta.Date == default)
                    throw new Exception("La fecha ingresada no es válida");

                if (filtros.FechaDesde.Date > filtros.FechaHasta.Date)
                    throw new Exception("La Fecha Hasta no puede ser anterior a la Fecha Desde");

                var idUsuario = User.GetUserId<int>();
                var idEmpresa = await _usuarios.FirstOrDefaultAsync(m => m.IdEmpresa, m => m.Id == idUsuario);
                _logger.LogInformation($"Histórico usuario {idUsuario} desde: {filtros.FechaDesde.ToString("dd/MM/yyyy")} - {filtros.FechaHasta.ToString("dd/MM/yyyy")}");
                var historial = await _historiales.ReadAsync(m => new
                {
                    m.Id,
                    m.IdUsuario,
                    m.DireccionIp,
                    m.Identificacion,
                    m.TipoIdentificacion,
                    Periodo = JObject.Parse(m.ParametrosBusqueda)["Periodos"],
                    TipoConsulta = m.TipoConsulta.GetEnumDescription(),
                    FechaHora = m.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    m.Fecha,
                    NombreUsuario = m.Usuario.NombreCompleto,
                    RazonSocial = m.RazonSocialEmpresa,
                    NombrePersona = m.NombresPersona,
                    NombreEmpresa = m.Usuario.Empresa.RazonSocial,
                    IdentificacionEmpresa = m.Usuario.Empresa.Identificacion,
                    m.Usuario.IdEmpresa,
                    ConsultaBuro = m.PlanBuroCredito != null && m.PlanBuroCredito.IdEmpresa == idEmpresa ? m.IdPlanBuroCredito.HasValue && m.IdPlanBuroCredito.Value > 0 : false,
                    ConsultaEvaluacion = m.PlanEvaluacion != null && m.PlanEvaluacion.IdEmpresa == idEmpresa ? m.IdPlanEvaluacion.HasValue && m.IdPlanEvaluacion.Value > 0 : false,
                    AprobadoEvaluacion = m.Calificaciones.Any() ? m.Calificaciones.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Evaluacion && m.Aprobado) && (m.Calificaciones.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro) ? m.Calificaciones.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro && m.Aprobado) : true) : false,
                    FuenteBuro = m.PlanBuroCredito != null && m.PlanBuroCredito.IdEmpresa == idEmpresa && m.TipoFuenteBuro.HasValue && m.TipoFuenteBuro.Value > 0 ? m.TipoFuenteBuro.GetEnumDescription() : null,
                }, m => m.Usuario.IdEmpresa == idEmpresa && m.Fecha.Date >= filtros.FechaDesde.Date && m.Fecha.Date <= filtros.FechaHasta.Date);

                return Json(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }
        #endregion Reportes

        #region Fuentes Detalle Historial
        [HttpPost]
        [Route("ListadoDetalleHistorial")]
        public async Task<IActionResult> ListadoDetalleHistorial(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                var detalleHistorial = await _detalleHistorial.ReadAsync(m => new
                {
                    m.Id,
                    m.IdHistorial,
                    m.TipoFuente,
                    m.Generado,
                    m.Data,
                }, m => m.IdHistorial == idHistorial);
                return Json(detalleHistorial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSri")]
        public async Task<IActionResult> ObtenerDatosSri(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new SRIViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.EmpresasSimilares, Dominio.Tipos.Fuentes.CatastroFantasma };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, null, 0, null, true);

                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);
                if (detalleHistorial != null)
                {
                    datos.Sri = JsonConvert.DeserializeObject<Contribuyente>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                    datos.FuenteActiva = detalleHistorial.FuenteActiva.HasValue ? detalleHistorial.FuenteActiva.Value : false;
                }

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasSimilares && m.Generado);

                if (detalleHistorial != null)
                    datos.EmpresasSimilares = JsonConvert.DeserializeObject<List<Similares>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.CatastroFantasma && m.Generado);

                if (detalleHistorial != null)
                    datos.CatastroFantasma = JsonConvert.DeserializeObject<CatastroFantasma>(detalleHistorial.Data);

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSri.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSri.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosPersona")]
        public async Task<IActionResult> ObtenerDatosPersona(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new CivilViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.Personales, Dominio.Tipos.Fuentes.Contactos, Dominio.Tipos.Fuentes.Familiares, Dominio.Tipos.Fuentes.RegistroCivil, Dominio.Tipos.Fuentes.ContactosEmpresa, Dominio.Tipos.Fuentes.ContactosIess };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, m => m.Include(p => p.Historial).ThenInclude(e => e.PlanEmpresa), 0, null, true);

                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado);
                if (detalleHistorial != null)
                    datos.Ciudadano = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Personales && m.Generado);
                if (detalleHistorial != null)
                    datos.Personales = JsonConvert.DeserializeObject<Personal>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Contactos && m.Generado);
                if (detalleHistorial != null)
                    datos.Contactos = JsonConvert.DeserializeObject<Contacto>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa && m.Generado);
                if (detalleHistorial != null)
                    datos.ContactosEmpresa = JsonConvert.DeserializeObject<Contacto>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess && m.Generado);
                if (detalleHistorial != null)
                    datos.ContactosIess = JsonConvert.DeserializeObject<Contacto>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Familiares && m.Generado);
                if (detalleHistorial != null)
                    datos.Familiares = JsonConvert.DeserializeObject<Familia>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado);
                if (detalleHistorial != null)
                    datos.RegistroCivil = JsonConvert.DeserializeObject<RegistroCivil>(detalleHistorial.Data);

                ViewBag.Contactabilidad = false;
                if (detallesHistorial.FirstOrDefault().Historial.PlanEmpresa.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1792060346001)
                    ViewBag.Contactabilidad = true;

                try
                {
                    var idEmpresasGenealogia = new List<int>();
                    var pathEmpresasGenealogia = Path.Combine("wwwroot", "data", "empresasGenealogiaCivil.json");
                    var archivoGenealogia = System.IO.File.ReadAllText(pathEmpresasGenealogia);
                    var empresasGenealogiaCivil = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoGenealogia);
                    if (empresasGenealogiaCivil != null && empresasGenealogiaCivil.Any())
                        idEmpresasGenealogia = empresasGenealogiaCivil.Select(m => m.Id).Distinct().ToList();
                    var historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == idHistorial, null, n => n.Include(t => t.PlanEmpresa), true);
                    var idempresa = historialTemp.PlanEmpresa.IdEmpresa;
                    if (idEmpresasGenealogia.Contains(idempresa)) datos.ConsultaGenealogia = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePersona.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePersona.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosIess")]
        public async Task<IActionResult> ObtenerDatosIess(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                //var idEmpresasSalarios = new List<int>();
                //try
                //{
                //    var pathEmpresasSalarioIess = Path.Combine("wwwroot", "data", "empresasSalarioIess.json");
                //    var archivoSalarios = System.IO.File.ReadAllText(pathEmpresasSalarioIess);
                //    var empresasSalariosIess = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoSalarios);
                //    if (empresasSalariosIess != null && empresasSalariosIess.Any())
                //        idEmpresasSalarios = empresasSalariosIess.Select(m => m.Id).Distinct().ToList();
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError(ex, ex.Message);
                //}

                //var idEmpresasEmpleados = new List<int>();
                //try
                //{
                //    var pathEmpresaEmpleadosIess = Path.Combine("wwwroot", "data", "empresasEmpleadosIess.json");
                //    var archivoEmpleados = System.IO.File.ReadAllText(pathEmpresaEmpleadosIess);
                //    var empresaEmpleadosIess = JsonConvert.DeserializeObject<List<EmpresaPersonalizadaViewModel>>(archivoEmpleados);
                //    if (empresaEmpleadosIess != null && empresaEmpleadosIess.Any())
                //        idEmpresasEmpleados = empresaEmpleadosIess.Select(m => m.Id).Distinct().ToList();
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError(ex, ex.Message);
                //}

                ViewBag.Historial = true;
                ViewBag.TipoFuente = null;
                var datos = new IessViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.Iess, Dominio.Tipos.Fuentes.Afiliado, Dominio.Tipos.Fuentes.IessJubilado };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, i => i.Include(m => m.Historial.Usuario), 0, null, true);

                datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Iess && m.Generado);
                if (detalleHistorial != null)
                    datos.Iess = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Persona>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado && m.Generado);
                if (detalleHistorial != null)
                    datos.Afiliado = JsonConvert.DeserializeObject<Afiliacion>(detalleHistorial.Data);

               

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.IessJubilado && m.Generado);
                if (detalleHistorial != null)
                    datos.IessJubilado = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Jubilado>(detalleHistorial.Data);

                if (datos.Afiliado == null && datos.AfiliadoAdicional != null)
                {
                    ViewBag.TipoFuente = 5;
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

                ViewBag.IessAfiliacion = false;
                if (datos.HistorialCabecera.Usuario.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1792060346001)
                    ViewBag.IessAfiliacion = true;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteIess.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteIess.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosHistorialIess")]
        public async Task<IActionResult> ObtenerDatosHistorialIess(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                #region ExisteClienteIessBuro

                var pathHistorialIess = Path.Combine("wwwroot", "data", "AdicionalInfo.json");
                var json = System.IO.File.ReadAllText(pathHistorialIess);

                //Deserializar como lista
                var listaUsuariosIess = JsonConvert.DeserializeObject<List<EmpresaIessHistorialViewModel>>(json)
                                        ?? new List<EmpresaIessHistorialViewModel>();

                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                //Validar si el usuario existe en el JSON
                bool UsuarioIess = listaUsuariosIess.Any(e => e.Id == usuarioActual.IdEmpresa);

                #endregion

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

                ViewBag.Historial = true;
                ViewBag.TipoFuente = null;
                var datos = new IessViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.AfiliadoAdicional, Dominio.Tipos.Fuentes.IessEmpresaEmpleados };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente) && m.Generado == true, null, i => i.Include(m => m.Historial.Usuario), 0, null, true);
                datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.AfiliadoAdicional && m.Generado);
                if (detalleHistorial != null)
                    datos.AfiliadoAdicional = JsonConvert.DeserializeObject<List<Afiliado>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.IessEmpresaEmpleados && m.Generado);
                if (detalleHistorial != null)
                    datos.EmpleadosEmpresa = JsonConvert.DeserializeObject<List<Externos.Logica.IESS.Modelos.Empleado>>(detalleHistorial.Data);

                if (datos.Afiliado == null && datos.AfiliadoAdicional != null)
                {
                    ViewBag.TipoFuente = 5;
                }

#if DEBUG
                datos.EmpresaConfiable = true;
#else
                if (datos.HistorialCabecera != null && !idEmpresasSalarios.Contains(datos.HistorialCabecera.Usuario.IdEmpresa))
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

                datos.EmpresaConfiable = idEmpresasSalarios.Contains(datos.HistorialCabecera != null ? datos.HistorialCabecera.Usuario.IdEmpresa : 0);
#endif
                if (datos.HistorialCabecera != null && idEmpresasEmpleados.Contains(datos.HistorialCabecera.Usuario.IdEmpresa))
                {
                    detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.IessEmpresaEmpleados && m.Generado);
                    if (detalleHistorial != null)
                        datos.EmpleadosEmpresa = JsonConvert.DeserializeObject<List<Empleado>>(detalleHistorial.Data);
                }
                datos.HistorialIess = UsuarioIess;
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteIessHistorial.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteIessHistorial.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosBalances")]
        public async Task<IActionResult> ObtenerDatosBalances(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new BalancesViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.Balance, Dominio.Tipos.Fuentes.Balances, Dominio.Tipos.Fuentes.DirectorioCompanias, Dominio.Tipos.Fuentes.RepresentantesEmpresas, Dominio.Tipos.Fuentes.AnalisisHorizontal, Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.SuperBancos, Dominio.Tipos.Fuentes.Accionistas, Dominio.Tipos.Fuentes.EmpresasAccionista, Dominio.Tipos.Fuentes.VerificarAccionistas };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, i => i.Include(m => m.Historial).ThenInclude(e => e.PlanEmpresa), 0, null, true);

                datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Balance && m.Generado);
                if (detalleHistorial != null)
                {
                    datos.Balance = JsonConvert.DeserializeObject<BalanceEmpresa>(detalleHistorial.Data);
                    datos.PeriodoBusqueda = datos.Balance.Periodo;
                }

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Balances && m.Generado);
                if (detalleHistorial != null)
                {
                    datos.Balances = JsonConvert.DeserializeObject<List<BalanceEmpresa>>(detalleHistorial.Data);
                    datos.Balance = datos.Balances.OrderByDescending(m => m.Periodo).FirstOrDefault();
                }

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.DirectorioCompanias && m.Generado);
                if (detalleHistorial != null)
                    datos.DirectorioCompania = JsonConvert.DeserializeObject<DirectorioCompania>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RepresentantesEmpresas && m.Generado);
                if (detalleHistorial != null)
                    datos.RepresentantesEmpresas = JsonConvert.DeserializeObject<List<RepresentanteEmpresa>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.AnalisisHorizontal && m.Generado);
                if (detalleHistorial != null)
                    datos.AnalisisHorizontal = JsonConvert.DeserializeObject<List<AnalisisHorizontalViewModel>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);
                if (detalleHistorial != null)
                    datos.Sri = JsonConvert.DeserializeObject<Contribuyente>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Accionistas && m.Generado);
                if (detalleHistorial != null)
                    datos.Accionistas = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Accionista>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasAccionista && m.Generado);
                if (detalleHistorial != null)
                    datos.EmpresasAccionista = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.AccionistaEmpresa>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.VerificarAccionistas && m.Generado);
                if (detalleHistorial != null)
                    datos.VerificarAccionista = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.DatosAccionista>(detalleHistorial.Data);

                ViewBag.Accionistas = false;
                if (detallesHistorial.FirstOrDefault().Historial.PlanEmpresa.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1792060346001)
                    ViewBag.Accionistas = true;

                datos.MultiplesPeriodos = datos.Balances != null;
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteBalances.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteBalances.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSenescyt")]
        public async Task<IActionResult> ObtenerDatosSenescyt(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new SenescytViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Senescyt, null, null, true);

                if (detalleHistorial != null)
                {
                    if (!string.IsNullOrEmpty(detalleHistorial.Data))
                        datos.Senescyt = JsonConvert.DeserializeObject<Externos.Logica.Senescyt.Modelos.Persona>(detalleHistorial.Data);
                    datos.FuenteActiva = detalleHistorial.FuenteActiva.HasValue ? detalleHistorial.FuenteActiva.Value : true;
                }
                else
                    datos.FuenteActiva = true;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSenescyt.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSenescyt.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosAnt")]
        public async Task<IActionResult> ObtenerDatosAnt(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new ANTViewModel();
                var fuentesAnt = new[] { Dominio.Tipos.Fuentes.Ant, Dominio.Tipos.Fuentes.AutoHistorico };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentesAnt.Contains(m.TipoFuente), null, null, 0, null, true);

                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ant && m.Generado);

                if (detalleHistorial != null)
                    datos.Licencia = JsonConvert.DeserializeObject<Externos.Logica.ANT.Modelos.Licencia>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.AutoHistorico && m.Generado);

                if (detalleHistorial != null)
                    datos.AutosHistorico = JsonConvert.DeserializeObject<List<Externos.Logica.ANT.Modelos.AutoHistorico>>(detalleHistorial.Data);

                datos.FuenteActiva = detalleHistorial != null && detalleHistorial.FuenteActiva.HasValue ? detalleHistorial.FuenteActiva.Value : false;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteAnt.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteAnt.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosPensionAlimenticia")]
        public async Task<IActionResult> ObtenerDatosPensionAlimenticia(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new PensionAlimenticiaViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia && m.Generado, null, null, true);

                if (detalleHistorial != null)
                    datos.PensionAlimenticia = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(detalleHistorial.Data);
                datos.FuenteActiva = detalleHistorial != null && detalleHistorial.FuenteActiva.HasValue ? detalleHistorial.FuenteActiva.Value : false;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePensionAlimenticia.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePensionAlimenticia.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosLegal")]
        public async Task<IActionResult> ObtenerDatosLegal(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

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
                ViewBag.Historial = true;
                ViewBag.FuenteImpedimento = true;
                ViewBag.FuenteFuncionJudicial = true;
                var datos = new JudicialViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.FJudicial, Dominio.Tipos.Fuentes.FJEmpresa, Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.Impedimento };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, i => i.Include(m => m.Historial.Usuario), 0, null, true);

                datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial && m.Generado);
                if (detalleHistorial != null)
                    datos.FJudicial = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FJEmpresa && m.Generado);
                if (detalleHistorial != null)
                    datos.FJEmpresa = JsonConvert.DeserializeObject<Externos.Logica.FJudicial.Modelos.Persona>(detalleHistorial.Data);

                var detalleHistorialFJTemp = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FJudicial);
                if (detalleHistorialFJTemp != null)
                {
                    datos.FuenteActiva = detalleHistorialFJTemp.FuenteActiva;
                    datos.BusquedaNueva = detalleHistorialFJTemp.Cache;
                }

                var detalleHistorialFETemp = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FJEmpresa);
                if (detalleHistorialFETemp != null)
                    datos.BusquedaNuevaEmpresa = detalleHistorialFETemp.Cache;

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);
                if (detalleHistorial != null)
                    datos.Sri = JsonConvert.DeserializeObject<Contribuyente>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Impedimento && m.Generado);
                if (detalleHistorial != null)
                    datos.Impedimento = JsonConvert.DeserializeObject<Impedimento>(detalleHistorial.Data);

                datos.ConsultaPersonalizada = empresasConsultaPersonalizada.Contains(datos.HistorialCabecera != null ? datos.HistorialCabecera.Usuario.IdEmpresa : 0);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteFJudicial.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteFJudicial.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSercop")]
        public async Task<IActionResult> ObtenerDatosSercop(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new SERCOPViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.Proveedor, Dominio.Tipos.Fuentes.ProveedorContraloria, Dominio.Tipos.Fuentes.Sri };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, i => i.Include(m => m.Historial), 0, null, true);

                datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Proveedor && m.Generado);
                if (detalleHistorial != null)
                    datos.Proveedor = JsonConvert.DeserializeObject<Externos.Logica.SERCOP.Modelos.ProveedorIncumplido>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ProveedorContraloria && m.Generado);
                if (detalleHistorial != null)
                    datos.ProveedorContraloria = JsonConvert.DeserializeObject<List<Externos.Logica.SERCOP.Modelos.ProveedorContraloria>>(detalleHistorial.Data);

                detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado);
                if (detalleHistorial != null)
                    datos.Sri = JsonConvert.DeserializeObject<Contribuyente>(detalleHistorial.Data);

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSercop.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSercop.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosBuroCredito")]
        public async Task<IActionResult> ObtenerDatosBuroCredito(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new BuroCreditoViewModel();
                var detalleHistorialBuro = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Fuentes.BuroCredito, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);
                var detalleHistorialIess = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Fuentes.AfiliadoAdicional, null, s => s.Include(x => x.Historial.Usuario.Empresa), true);
                var detalleHistorialEmpleados = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Fuentes.IessEmpresaEmpleados, null, s => s.Include(x => x.Historial.Usuario.Empresa), true);
                if (detalleHistorialBuro != null)
                {
                    var modeloBCapital = detalleHistorialBuro.Historial.Usuario.Empresa.Identificacion == Dominio.Constantes.Clientes.Cliente1090105244001;
                    var modeloCooperativas = false;
                    var modeloBuroBLitoral = false;
                    var modeloBuroBLitoralMicrofinanza = false;
                    var modeloBuroBancoDMiro = detalleHistorialBuro.Historial.Usuario.Empresa.Identificacion == Dominio.Constantes.Clientes.Cliente0992701374001;

                    if (detalleHistorialBuro != null)
                    {
                        datos.HistorialCabecera = detalleHistorialBuro.Historial;
                        datos.BuroCredito = detalleHistorialBuro.Data != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval ? JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(detalleHistorialBuro.Data) : null;
                        datos.BuroCreditoEquifax = detalleHistorialBuro.Data != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax ? JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(detalleHistorialBuro.Data) : null;
                        datos.DatosCache = detalleHistorialBuro.Cache;
                        datos.Fuente = (Dominio.Tipos.FuentesBuro)detalleHistorialBuro.Historial.TipoFuenteBuro;
                        datos.ErrorEquifax = detalleHistorialBuro.DataError != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax ? JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(detalleHistorialBuro.DataError) : null;
                        datos.ErrorAval = detalleHistorialBuro.DataError != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval ? JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(detalleHistorialBuro.DataError) : null;

                        modeloCooperativas = detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.PlanBuroCredito != null ? detalleHistorialBuro.Historial.PlanBuroCredito.ModeloCooperativas : false;

                        if (datos.BuroCreditoEquifax != null)
                        {
                            modeloBuroBLitoral = datos.BuroCreditoEquifax.ResultadosBancoLitoral != null;
                            modeloBuroBLitoralMicrofinanza = datos.BuroCreditoEquifax.ResultadosBancoLitoralMicrofinanza != null;
                        }
                    }


                    var vistaBuro = string.Empty;
                    if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.TipoFuenteBuro != null && detalleHistorialBuro.Historial.Usuario.Empresa.Identificacion == Dominio.Constantes.Clientes.Cliente1792899036001 && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval)
                        vistaBuro = "_FuenteBuroCreditoAyasa";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.TipoFuenteBuro != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval && modeloBCapital)
                        vistaBuro = "_FuenteBuroCreditoBCapital";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.TipoFuenteBuro != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval && modeloBuroBancoDMiro)
                        vistaBuro = "_FuenteBuroCreditoBDMiro";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.TipoFuenteBuro != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval && modeloCooperativas)
                        vistaBuro = "_FuenteBuroCreditoCooperativas";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.TipoFuenteBuro != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval)
                        vistaBuro = "_FuenteBuroCredito";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990304211001)
                        vistaBuro = "_FuenteBuroEquifaxIndumot";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0190325180001)
                        vistaBuro = "_FuenteBuroEquifaxSanJose";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0390027923001)
                        vistaBuro = "_FuenteBuroEquifaxCBCooperativa";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && modeloBuroBLitoral)
                        vistaBuro = "_FuenteBuroEquifaxBancoLitoral";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && modeloBuroBLitoralMicrofinanza)
                        vistaBuro = "_FuenteBuroEquifaxBancoLitoralMicrofinanza";
                    //else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0190111881001)
                    //    vistaBuro = "_FuenteBuroEquifaxFragancias";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1590001585001)
                        vistaBuro = "_FuenteBuroEquifaxCoopTena";
                    else if (detalleHistorialBuro != null && detalleHistorialBuro.Historial != null && detalleHistorialBuro.Historial.TipoFuenteBuro != null && detalleHistorialBuro.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax)
                        vistaBuro = "_FuenteBuroEquifax";
                    else
                        vistaBuro = "_FuenteBuro";

                    return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/{vistaBuro}.cshtml", datos);
                }
                return Json("");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteBuro.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosFiscaliaDelitos")]
        public async Task<IActionResult> ObtenerDatosFiscaliaDelitos(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new DelitosViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.FiscaliaDelitosPersona, Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, i => i.Include(m => m.Historial), 0, null, true);

                datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                var detalleHistorial = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosPersona);
                if (detalleHistorial != null)
                {
                    if (!string.IsNullOrEmpty(detalleHistorial.Data))
                        datos.FiscaliaPersona = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                    datos.FuenteActiva = detalleHistorial.FuenteActiva.HasValue ? detalleHistorial.FuenteActiva.Value : false;
                }

                var detalleHistorialEmpresa = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.FiscaliaDelitosEmpresa);
                if (detalleHistorialEmpresa != null)
                {
                    if (!string.IsNullOrEmpty(detalleHistorialEmpresa.Data))
                        datos.FiscaliaEmpresa = JsonConvert.DeserializeObject<Externos.Logica.FiscaliaDelitos.Modelos.NoticiaDelito>(detalleHistorialEmpresa.Data);
                    datos.BusquedaNuevaEmpresa = detalleHistorialEmpresa.Cache;
                    datos.FuenteActiva = detalleHistorialEmpresa.FuenteActiva.HasValue ? detalleHistorialEmpresa.FuenteActiva.Value : false;
                }

                if (detalleHistorial == null && detalleHistorialEmpresa == null)
                    datos.FuenteActiva = true;

                #region DatosDelitos ProcesadosSospechosos
                try
                {
                    if (datos.HistorialCabecera != null && datos.HistorialCabecera.TipoConsulta == Dominio.Tipos.Consultas.Web)
                    {
                        var numeroDelito = new List<string>();
                        var numeroAdministro = new List<string>();
                        var numeroDelitoEmpresa = new List<string>();
                        var numeroAdministroEmpresa = new List<string>();
                        if (datos != null && datos.FiscaliaPersona != null && datos.FiscaliaPersona.ProcesosNoticiaDelito != null && datos.FiscaliaPersona.ProcesosNoticiaDelito.Any())
                        {
                            var sujetosNoticiaDelito = datos.FiscaliaPersona.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Contains("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                            if (sujetosNoticiaDelito != null && sujetosNoticiaDelito.Any())
                            {
                                var nombreDivido = datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) ? datos.HistorialCabecera.NombresPersona.Split(' ') : new string[0];
                                var listaNombre = new List<bool>();
                                foreach (var item1 in sujetosNoticiaDelito.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                                {
                                    if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Contains("SOSPECHOSO"))
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

                        //if (datos != null && datos.FiscaliaPersona != null && datos.FiscaliaPersona.ProcesosActoAdministrativo != null && datos.FiscaliaPersona.ProcesosActoAdministrativo.Any())
                        //{
                        //    var sujetosActoAdministrativo = datos.FiscaliaPersona.ProcesosActoAdministrativo.Where(x => x.Descripcion.ToUpper().Equals("PROCESADO") || x.Descripcion.ToUpper().Contains("SOSPECHOSO")).Select(x => new { x.Numero, x.CedulaDenunciante, x.NombreDenunciante }).ToList();
                        //    if (sujetosActoAdministrativo != null && sujetosActoAdministrativo.Any())
                        //    {
                        //        var nombreDivido = datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) ? datos.HistorialCabecera.NombresPersona.Split(' ') : new string[0];
                        //        var listaNombre = new List<bool>();
                        //        foreach (var item1 in sujetosActoAdministrativo)
                        //        {
                        //            if (!string.IsNullOrEmpty(item1.CedulaDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.Identificacion) && datos.HistorialCabecera.Identificacion == item1.CedulaDenunciante)
                        //                numeroAdministro.Add(item1.Numero);
                        //            else if (!string.IsNullOrEmpty(item1.CedulaDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.IdentificacionSecundaria) && datos.HistorialCabecera.IdentificacionSecundaria == item1.CedulaDenunciante)
                        //                numeroAdministro.Add(item1.Numero);
                        //            else
                        //            {
                        //                var nombreSeparado = item1.NombreDenunciante.Split(' ');
                        //                listaNombre.Clear();
                        //                foreach (var item2 in nombreSeparado)
                        //                {
                        //                    if (datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.NombresPersona) && datos.HistorialCabecera.NombresPersona.Contains(item2))
                        //                        listaNombre.Add(true);
                        //                    else
                        //                        listaNombre.Add(false);
                        //                }
                        //                if (nombreDivido != null && nombreDivido.Any() && listaNombre.Count(x => x) == nombreDivido.Length)
                        //                    numeroAdministro.Add(item1.Numero);
                        //            }
                        //        }
                        //        numeroAdministro = numeroAdministro.Distinct().ToList();
                        //        datos.FiscaliaPersona.ProcesosActoAdministrativo = datos.FiscaliaPersona.ProcesosActoAdministrativo.Where(x => numeroAdministro.Contains(x.Numero)).Select(x => x).ToList();
                        //    }
                        //    else
                        //        datos.FiscaliaPersona.ProcesosActoAdministrativo.Clear();
                        //}

                        if (datos != null && datos.FiscaliaEmpresa != null && datos.FiscaliaEmpresa.ProcesosNoticiaDelito != null && datos.FiscaliaEmpresa.ProcesosNoticiaDelito.Any())
                        {
                            var sujetos = datos.FiscaliaEmpresa.ProcesosNoticiaDelito.Where(x => x.Sujetos.Any(m => m.Estado.ToUpper().Equals("PROCESADO") || m.Estado.ToUpper().Contains("SOSPECHOSO"))).Select(x => new { x.Numero, x.Sujetos }).ToList();
                            if (sujetos != null && sujetos.Any())
                            {
                                foreach (var item1 in sujetos.SelectMany(x => x.Sujetos.Select(m => new { x.Numero, m.Cedula, m.NombresCompletos, m.Estado })))
                                {
                                    if (item1.Estado.ToUpper().Equals("PROCESADO") || item1.Estado.ToUpper().Contains("SOSPECHOSO"))
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

                        //if (datos != null && datos.FiscaliaEmpresa != null && datos.FiscaliaEmpresa.ProcesosActoAdministrativo != null && datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Any())
                        //{
                        //    var sujetos = datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Where(x => x.Descripcion.ToUpper().Equals("PROCESADO") || x.Descripcion.ToUpper().Contains("SOSPECHOSO")).Select(x => new { x.Numero, x.CedulaDenunciante, x.NombreDenunciante }).ToList();
                        //    if (sujetos != null && sujetos.Any())
                        //    {
                        //        foreach (var item1 in sujetos)
                        //        {
                        //            if (!string.IsNullOrEmpty(item1.CedulaDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.Identificacion) && datos.HistorialCabecera.Identificacion == item1.CedulaDenunciante)
                        //                numeroAdministroEmpresa.Add(item1.Numero);
                        //            else if (!string.IsNullOrEmpty(item1.NombreDenunciante) && datos.HistorialCabecera != null && !string.IsNullOrEmpty(datos.HistorialCabecera.RazonSocialEmpresa) && datos.HistorialCabecera.RazonSocialEmpresa == item1.NombreDenunciante)
                        //                numeroAdministroEmpresa.Add(item1.Numero);
                        //        }
                        //        numeroAdministroEmpresa = numeroAdministroEmpresa.Distinct().ToList();
                        //        datos.FiscaliaEmpresa.ProcesosActoAdministrativo = datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Where(x => numeroAdministroEmpresa.Contains(x.Numero)).Select(x => x).ToList();
                        //    }
                        //    else
                        //        datos.FiscaliaEmpresa.ProcesosActoAdministrativo.Clear();
                        //}
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                #endregion DatosDelitos ProcesadosSospechosos


                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteFiscaliaDelitos.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteFiscaliaDelitos.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSuperBancos")]
        public async Task<IActionResult> ObtenerDatosSuperBancos(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                ViewBag.MsjErrorSuperBancos = string.Empty;
                var datos = new SuperBancosViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado, null, m => m.Include(m => m.Historial), true);
                if (detalleHistorial != null)
                {
                    datos.SuperBancos = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }

                var detalleHistorialNatural = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural && m.Generado, null, null, true);
                if (detalleHistorialNatural != null)
                {
                    datos.SuperBancosNatural = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorialNatural.Data);
                    datos.BusquedaNuevaNatural = detalleHistorialNatural.Cache;
                }

                var detalleHistorialEmpresa = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa && m.Generado, null, null, true);
                if (detalleHistorialEmpresa != null)
                {
                    datos.SuperBancosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorialEmpresa.Data);
                    datos.BusquedaNuevaEmpresa = detalleHistorialEmpresa.Cache;
                }

                if (detalleHistorial != null && detalleHistorial.Historial != null && string.IsNullOrEmpty(detalleHistorial.Historial.FechaExpedicionCedula))
                    ViewBag.MsjErrorSuperBancos = "No se pudo obtener la Fecha de Cedulación.";

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSuperBancos.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteSuperBancos.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSuperBancosCedula")]
        public async Task<IActionResult> ObtenerDatosSuperBancosCedula(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                ViewBag.MsjErrorSuperBancos = string.Empty;
                var datos = new InformacionSuperBancosViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancos && m.Generado, null, m => m.Include(m => m.Historial), true);
                if (detalleHistorial != null)
                {
                    datos.SuperBancos = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }
                datos.TipoConsulta = 1;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteInformacionSuperBancos.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteInformacionSuperBancos.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSuperBancosNatural")]
        public async Task<IActionResult> ObtenerDatosSuperBancosNatural(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                ViewBag.MsjErrorSuperBancos = string.Empty;
                var datos = new InformacionSuperBancosViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosNatural && m.Generado, null, m => m.Include(m => m.Historial), true);
                if (detalleHistorial != null)
                {
                    datos.SuperBancos = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }
                datos.TipoConsulta = 2;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteInformacionSuperBancos.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteInformacionSuperBancos.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosSuperBancosEmpresa")]
        public async Task<IActionResult> ObtenerDatosSuperBancosEmpresa(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                ViewBag.MsjErrorSuperBancos = string.Empty;
                var datos = new InformacionSuperBancosViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.SuperBancosEmpresa && m.Generado, null, m => m.Include(m => m.Historial), true);
                if (detalleHistorial != null)
                {
                    datos.SuperBancos = JsonConvert.DeserializeObject<Externos.Logica.SuperBancos.Modelos.Resultado>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }
                datos.TipoConsulta = 3;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteInformacionSuperBancos.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteInformacionSuperBancos.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosAntecedentesPenales")]
        public async Task<IActionResult> ObtenerDatosAntecedentesPenales(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new AntecedentesPenalesViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.AntecedentesPenales && m.Generado, null, null, true);
                if (detalleHistorial != null)
                {
                    datos.Antecedentes = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.Resultado>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }
                datos.FuenteActiva = detalleHistorial != null && detalleHistorial.FuenteActiva.HasValue ? detalleHistorial.FuenteActiva.Value : false;

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteAntecedentesPenales.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteAntecedentesPenales.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosFuerzasArmadas")]
        public async Task<IActionResult> ObtenerDatosFuerzasArmadas(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new FuerzasArmadasViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.FuerzaArmada && m.Generado, null, null, true);
                if (detalleHistorial != null)
                {
                    datos.FuerzasArmadas = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.PersonaFuerzaArmada>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteFuerzasArmadas.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteFuerzasArmadas.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosDeNoBaja")]
        public async Task<IActionResult> ObtenerDatosDeNoBaja(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new DeNoBajaViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DeNoBaja && m.Generado, null, null, true);
                if (detalleHistorial != null)
                {
                    datos.DeNoBaja = JsonConvert.DeserializeObject<Externos.Logica.AntecedentesPenales.Modelos.ResultadoNoPolicia>(detalleHistorial.Data);
                    datos.BusquedaNueva = detalleHistorial.Cache;
                }

                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteDeNoBaja.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteDeNoBaja.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosPredios")]
        public async Task<IActionResult> ObtenerDatosPredios(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;

                var datos = new PrediosViewModel();
                if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresa)))
                {
                    var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorial != null)
                    {
                        datos.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(detalleHistorial.Data);
                        datos.BusquedaNuevaRepresentante = detalleHistorial.Cache;
                        datos.HistorialCabecera = detalleHistorial.Historial;
                    }

                    var detalleHistorialEmpresa = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresa && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresa != null)
                    {
                        datos.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(detalleHistorialEmpresa.Data);
                        datos.BusquedaNuevaEmpresa = detalleHistorialEmpresa.Cache;
                        if (datos.HistorialCabecera == null)
                            datos.HistorialCabecera = detalleHistorialEmpresa.Historial;
                    }

                    var detallesPredios = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DetallePredios && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detallesPredios != null)
                    {
                        datos.DetallePrediosRepresentante = new DetallePrediosViewModel
                        {
                            Detalle = JsonConvert.DeserializeObject<List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm>>(detallesPredios.Data),
                            BusquedaNueva = detallesPredios.Cache
                        };
                    }

                    var detallesPrediosEmpresa = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.DetallePrediosEmpresa && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detallesPrediosEmpresa != null)
                    {
                        datos.DetallePrediosEmpresa = new DetallePrediosViewModel
                        {
                            Detalle = JsonConvert.DeserializeObject<List<Externos.Logica.PredioMunicipio.Modelos.DetallePredioIrm>>(detallesPrediosEmpresa.Data),
                            BusquedaNueva = detallesPrediosEmpresa.Cache
                        };
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePredios.cshtml", datos);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca)))
                {
                    var datosCuenca = new PrediosCuencaViewModel();
                    var detalleHistorialCuenca = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialCuenca != null)
                    {
                        datosCuenca.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(detalleHistorialCuenca.Data);
                        datosCuenca.BusquedaNuevaRepresentante = detalleHistorialCuenca.Cache;
                        datosCuenca.HistorialCabecera = detalleHistorialCuenca.Historial;
                    }

                    var detalleHistorialEmpresaCuenca = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCuenca && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaCuenca != null)
                    {
                        datosCuenca.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(detalleHistorialEmpresaCuenca.Data);
                        datosCuenca.BusquedaNuevaEmpresa = detalleHistorialEmpresaCuenca.Cache;
                        if (datosCuenca.HistorialCabecera == null)
                            datosCuenca.HistorialCabecera = detalleHistorialEmpresaCuenca.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosCuenca.cshtml", datosCuenca);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo)))
                {
                    var datosStoDomingo = new PrediosSantoDomingoViewModel();
                    var detalleHistorialStoDomingo = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialStoDomingo != null)
                    {
                        datosStoDomingo.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(detalleHistorialStoDomingo.Data);
                        datosStoDomingo.BusquedaNuevaRepresentante = detalleHistorialStoDomingo.Cache;
                        datosStoDomingo.HistorialCabecera = detalleHistorialStoDomingo.Historial;
                    }

                    var detalleHistorialEmpresaStoDomingo = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantoDomingo && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaStoDomingo != null)
                    {
                        datosStoDomingo.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(detalleHistorialEmpresaStoDomingo.Data);
                        datosStoDomingo.BusquedaNuevaEmpresa = detalleHistorialEmpresaStoDomingo.Cache;
                        if (datosStoDomingo.HistorialCabecera == null)
                            datosStoDomingo.HistorialCabecera = detalleHistorialEmpresaStoDomingo.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosSantoDomingo.cshtml", datosStoDomingo);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui)))
                {
                    var datosRuminahui = new PrediosRuminahuiViewModel();
                    var detalleHistorialRuminahui = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialRuminahui != null)
                    {
                        datosRuminahui.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(detalleHistorialRuminahui.Data);
                        datosRuminahui.BusquedaNuevaRepresentante = detalleHistorialRuminahui.Cache;
                        datosRuminahui.HistorialCabecera = detalleHistorialRuminahui.Historial;
                    }

                    var detalleHistorialEmpresaRuminahui = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaRuminahui && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaRuminahui != null)
                    {
                        datosRuminahui.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(detalleHistorialEmpresaRuminahui.Data);
                        datosRuminahui.BusquedaNuevaEmpresa = detalleHistorialEmpresaRuminahui.Cache;
                        if (datosRuminahui.HistorialCabecera == null)
                            datosRuminahui.HistorialCabecera = detalleHistorialEmpresaRuminahui.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosRuminahui.cshtml", datosRuminahui);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde)))
                {
                    var datosQuininde = new PrediosQuinindeViewModel();
                    var detalleHistorialQuininde = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialQuininde != null)
                    {
                        datosQuininde.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(detalleHistorialQuininde.Data);
                        datosQuininde.BusquedaNuevaRepresentante = detalleHistorialQuininde.Cache;
                        datosQuininde.HistorialCabecera = detalleHistorialQuininde.Historial;
                    }

                    var detalleHistorialEmpresaQuininde = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaQuininde && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaQuininde != null)
                    {
                        datosQuininde.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(detalleHistorialEmpresaQuininde.Data);
                        datosQuininde.BusquedaNuevaEmpresa = detalleHistorialEmpresaQuininde.Cache;
                        if (datosQuininde.HistorialCabecera == null)
                            datosQuininde.HistorialCabecera = detalleHistorialEmpresaQuininde.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosQuininde.cshtml", datosQuininde);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLatacunga || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga)))
                {
                    var datosLatacunga = new PrediosLatacungaViewModel();
                    var detalleHistorialLatacunga = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLatacunga && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialLatacunga != null)
                    {
                        datosLatacunga.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga>(detalleHistorialLatacunga.Data);
                        datosLatacunga.BusquedaNuevaRepresentante = detalleHistorialLatacunga.Cache;
                        datosLatacunga.HistorialCabecera = detalleHistorialLatacunga.Historial;
                    }

                    var detalleHistorialEmpresaLatacunga = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLatacunga && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaLatacunga != null)
                    {
                        datosLatacunga.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLatacunga>(detalleHistorialEmpresaLatacunga.Data);
                        datosLatacunga.BusquedaNuevaEmpresa = detalleHistorialEmpresaLatacunga.Cache;
                        if (datosLatacunga.HistorialCabecera == null)
                            datosLatacunga.HistorialCabecera = detalleHistorialEmpresaLatacunga.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosLatacunga.cshtml", datosLatacunga);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioManta || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta)))
                {
                    var datosManta = new PrediosMantaViewModel();
                    var detalleHistorialManta = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioManta && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialManta != null)
                    {
                        datosManta.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosManta>(detalleHistorialManta.Data);
                        datosManta.BusquedaNuevaRepresentante = detalleHistorialManta.Cache;
                        datosManta.HistorialCabecera = detalleHistorialManta.Historial;
                    }

                    var detalleHistorialEmpresaManta = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaManta && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaManta != null)
                    {
                        datosManta.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosManta>(detalleHistorialEmpresaManta.Data);
                        datosManta.BusquedaNuevaEmpresa = detalleHistorialEmpresaManta.Cache;
                        if (datosManta.HistorialCabecera == null)
                            datosManta.HistorialCabecera = detalleHistorialEmpresaManta.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosManta.cshtml", datosManta);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAmbato || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato)))
                {
                    var datosAmbato = new PrediosAmbatoViewModel();
                    var detalleHistorialAmbato = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAmbato && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialAmbato != null)
                    {
                        datosAmbato.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato>(detalleHistorialAmbato.Data);
                        datosAmbato.BusquedaNuevaRepresentante = detalleHistorialAmbato.Cache;
                        datosAmbato.HistorialCabecera = detalleHistorialAmbato.Historial;
                    }

                    var detalleHistorialEmpresaAmbato = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAmbato && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaAmbato != null)
                    {
                        datosAmbato.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAmbato>(detalleHistorialEmpresaAmbato.Data);
                        datosAmbato.BusquedaNuevaEmpresa = detalleHistorialEmpresaAmbato.Cache;
                        if (datosAmbato.HistorialCabecera == null)
                            datosAmbato.HistorialCabecera = detalleHistorialEmpresaAmbato.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosAmbato.cshtml", datosAmbato);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioIbarra || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra)))
                {
                    var datosIbarra = new PrediosIbarraViewModel();
                    var detalleHistorialIbarra = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioIbarra && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialIbarra != null)
                    {
                        datosIbarra.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra>(detalleHistorialIbarra.Data);
                        datosIbarra.BusquedaNuevaRepresentante = detalleHistorialIbarra.Cache;
                        datosIbarra.HistorialCabecera = detalleHistorialIbarra.Historial;
                    }

                    var detalleHistorialEmpresaIbarra = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaIbarra && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaIbarra != null)
                    {
                        datosIbarra.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosIbarra>(detalleHistorialEmpresaIbarra.Data);
                        datosIbarra.BusquedaNuevaEmpresa = detalleHistorialEmpresaIbarra.Cache;
                        if (datosIbarra.HistorialCabecera == null)
                            datosIbarra.HistorialCabecera = detalleHistorialEmpresaIbarra.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosIbarra.cshtml", datosIbarra);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal)))
                {
                    var datosSanCristobal = new PrediosSanCristobalViewModel();
                    var detalleHistorialSanCristobal = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSanCristobal && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialSanCristobal != null)
                    {
                        datosSanCristobal.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal>(detalleHistorialSanCristobal.Data);
                        datosSanCristobal.BusquedaNuevaRepresentante = detalleHistorialSanCristobal.Cache;
                        datosSanCristobal.HistorialCabecera = detalleHistorialSanCristobal.Historial;
                    }

                    var detalleHistorialEmpresaSanCristobal = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSanCristobal && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaSanCristobal != null)
                    {
                        datosSanCristobal.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSanCristobal>(detalleHistorialEmpresaSanCristobal.Data);
                        datosSanCristobal.BusquedaNuevaEmpresa = detalleHistorialEmpresaSanCristobal.Cache;
                        if (datosSanCristobal.HistorialCabecera == null)
                            datosSanCristobal.HistorialCabecera = detalleHistorialEmpresaSanCristobal.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosSanCristobal.cshtml", datosSanCristobal);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDuran || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran)))
                {
                    var datosDuran = new PrediosDuranViewModel();
                    var detalleHistorialDuran = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDuran && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialDuran != null)
                    {
                        datosDuran.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDuran>(detalleHistorialDuran.Data);
                        datosDuran.BusquedaNuevaRepresentante = detalleHistorialDuran.Cache;
                        datosDuran.HistorialCabecera = detalleHistorialDuran.Historial;
                    }

                    var detalleHistorialEmpresaDuran = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDuran && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaDuran != null)
                    {
                        datosDuran.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDuran>(detalleHistorialEmpresaDuran.Data);
                        datosDuran.BusquedaNuevaEmpresa = detalleHistorialEmpresaDuran.Cache;
                        if (datosDuran.HistorialCabecera == null)
                            datosDuran.HistorialCabecera = detalleHistorialEmpresaDuran.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosDuran.cshtml", datosDuran);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio)))
                {
                    var datosLagoAgrio = new PrediosLagoAgrioViewModel();
                    var detalleHistorialLagoAgrio = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLagoAgrio && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialLagoAgrio != null)
                    {
                        datosLagoAgrio.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio>(detalleHistorialLagoAgrio.Data);
                        datosLagoAgrio.BusquedaNuevaRepresentante = detalleHistorialLagoAgrio.Cache;
                        datosLagoAgrio.HistorialCabecera = detalleHistorialLagoAgrio.Historial;
                    }

                    var detalleHistorialEmpresaLagoAgrio = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLagoAgrio && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaLagoAgrio != null)
                    {
                        datosLagoAgrio.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLagoAgrio>(detalleHistorialEmpresaLagoAgrio.Data);
                        datosLagoAgrio.BusquedaNuevaEmpresa = detalleHistorialEmpresaLagoAgrio.Cache;
                        if (datosLagoAgrio.HistorialCabecera == null)
                            datosLagoAgrio.HistorialCabecera = detalleHistorialEmpresaLagoAgrio.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosLagoAgrio.cshtml", datosLagoAgrio);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa)))
                {
                    var datosSantaRosa = new PrediosSantaRosaViewModel();
                    var detalleHistorialSantaRosa = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantaRosa && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialSantaRosa != null)
                    {
                        datosSantaRosa.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa>(detalleHistorialSantaRosa.Data);
                        datosSantaRosa.BusquedaNuevaRepresentante = detalleHistorialSantaRosa.Cache;
                        datosSantaRosa.HistorialCabecera = detalleHistorialSantaRosa.Historial;
                    }

                    var detalleHistorialEmpresaSantaRosa = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSantaRosa && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaSantaRosa != null)
                    {
                        datosSantaRosa.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantaRosa>(detalleHistorialEmpresaSantaRosa.Data);
                        datosSantaRosa.BusquedaNuevaEmpresa = detalleHistorialEmpresaSantaRosa.Cache;
                        if (datosSantaRosa.HistorialCabecera == null)
                            datosSantaRosa.HistorialCabecera = detalleHistorialEmpresaSantaRosa.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosSantaRosa.cshtml", datosSantaRosa);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSucua || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua)))
                {
                    var datosSucua = new PrediosSucuaViewModel();
                    var detalleHistorialSucua = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSucua && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialSucua != null)
                    {
                        datosSucua.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSucua>(detalleHistorialSucua.Data);
                        datosSucua.BusquedaNuevaRepresentante = detalleHistorialSucua.Cache;
                        datosSucua.HistorialCabecera = detalleHistorialSucua.Historial;
                    }

                    var detalleHistorialEmpresaSucua = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSucua && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaSucua != null)
                    {
                        datosSucua.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSucua>(detalleHistorialEmpresaSucua.Data);
                        datosSucua.BusquedaNuevaEmpresa = detalleHistorialEmpresaSucua.Cache;
                        if (datosSucua.HistorialCabecera == null)
                            datosSucua.HistorialCabecera = detalleHistorialEmpresaSucua.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosSucua.cshtml", datosSucua);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSigSig || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig)))
                {
                    var datosSigSig = new PrediosSigSigViewModel();
                    var detalleHistorialSigSig = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSigSig && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialSigSig != null)
                    {
                        datosSigSig.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig>(detalleHistorialSigSig.Data);
                        datosSigSig.BusquedaNuevaRepresentante = detalleHistorialSigSig.Cache;
                        datosSigSig.HistorialCabecera = detalleHistorialSigSig.Historial;
                    }

                    var detalleHistorialEmpresaSigSig = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSigSig && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaSigSig != null)
                    {
                        datosSigSig.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSigSig>(detalleHistorialEmpresaSigSig.Data);
                        datosSigSig.BusquedaNuevaEmpresa = detalleHistorialEmpresaSigSig.Cache;
                        if (datosSigSig.HistorialCabecera == null)
                            datosSigSig.HistorialCabecera = detalleHistorialEmpresaSigSig.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosSigSig.cshtml", datosSigSig);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMejia || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia)))
                {
                    var datosMejia = new PrediosMejiaViewModel();
                    var detalleHistorialMejia = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMejia && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialMejia != null)
                    {
                        datosMejia.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMejia>(detalleHistorialMejia.Data);
                        datosMejia.BusquedaNuevaRepresentante = detalleHistorialMejia.Cache;
                        datosMejia.HistorialCabecera = detalleHistorialMejia.Historial;
                    }

                    var detalleHistorialEmpresaMejia = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMejia && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaMejia != null)
                    {
                        datosMejia.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMejia>(detalleHistorialEmpresaMejia.Data);
                        datosMejia.BusquedaNuevaEmpresa = detalleHistorialEmpresaMejia.Cache;
                        if (datosMejia.HistorialCabecera == null)
                            datosMejia.HistorialCabecera = detalleHistorialEmpresaMejia.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosMejia.cshtml", datosMejia);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMorona || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona)))
                {
                    var datosMorona = new PrediosMoronaViewModel();
                    var detalleHistorialMorona = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioMorona && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialMorona != null)
                    {
                        datosMorona.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMorona>(detalleHistorialMorona.Data);
                        datosMorona.BusquedaNuevaRepresentante = detalleHistorialMorona.Cache;
                        datosMorona.HistorialCabecera = detalleHistorialMorona.Historial;
                    }

                    var detalleHistorialEmpresaMorona = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaMorona && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaMorona != null)
                    {
                        datosMorona.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosMorona>(detalleHistorialEmpresaMorona.Data);
                        datosMorona.BusquedaNuevaEmpresa = detalleHistorialEmpresaMorona.Cache;
                        if (datosMorona.HistorialCabecera == null)
                            datosMorona.HistorialCabecera = detalleHistorialEmpresaMorona.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosMorona.cshtml", datosMorona);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioTena || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena)))
                {
                    var datosTena = new PrediosTenaViewModel();
                    var detalleHistorialTena = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioTena && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialTena != null)
                    {
                        datosTena.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosTena>(detalleHistorialTena.Data);
                        datosTena.BusquedaNuevaRepresentante = detalleHistorialTena.Cache;
                        datosTena.HistorialCabecera = detalleHistorialTena.Historial;
                    }

                    var detalleHistorialEmpresaTena = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaTena && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaTena != null)
                    {
                        datosTena.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosTena>(detalleHistorialEmpresaTena.Data);
                        datosTena.BusquedaNuevaEmpresa = detalleHistorialEmpresaTena.Cache;
                        if (datosTena.HistorialCabecera == null)
                            datosTena.HistorialCabecera = detalleHistorialEmpresaTena.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosTena.cshtml", datosTena);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCatamayo || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo)))
                {
                    var datosCatamayo = new PrediosCatamayoViewModel();
                    var detalleHistorialCatamayo = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCatamayo && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialCatamayo != null)
                    {
                        datosCatamayo.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo>(detalleHistorialCatamayo.Data);
                        datosCatamayo.BusquedaNuevaRepresentante = detalleHistorialCatamayo.Cache;
                        datosCatamayo.HistorialCabecera = detalleHistorialCatamayo.Historial;
                    }

                    var detalleHistorialEmpresaCatamayo = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCatamayo && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaCatamayo != null)
                    {
                        datosCatamayo.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCatamayo>(detalleHistorialEmpresaCatamayo.Data);
                        datosCatamayo.BusquedaNuevaEmpresa = detalleHistorialEmpresaCatamayo.Cache;
                        if (datosCatamayo.HistorialCabecera == null)
                            datosCatamayo.HistorialCabecera = detalleHistorialEmpresaCatamayo.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosCatamayo.cshtml", datosCatamayo);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLoja || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja)))
                {
                    var datosLoja = new PrediosLojaViewModel();
                    var detalleHistorialLoja = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioLoja && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialLoja != null)
                    {
                        datosLoja.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLoja>(detalleHistorialLoja.Data);
                        datosLoja.BusquedaNuevaRepresentante = detalleHistorialLoja.Cache;
                        datosLoja.HistorialCabecera = detalleHistorialLoja.Historial;
                    }

                    var detalleHistorialEmpresaLoja = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaLoja && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaLoja != null)
                    {
                        datosLoja.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosLoja>(detalleHistorialEmpresaLoja.Data);
                        datosLoja.BusquedaNuevaEmpresa = detalleHistorialEmpresaLoja.Cache;
                        if (datosLoja.HistorialCabecera == null)
                            datosLoja.HistorialCabecera = detalleHistorialEmpresaLoja.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosLoja.cshtml", datosLoja);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSamborondon || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon)))
                {
                    var datosSamborondon = new PrediosSamborondonViewModel();
                    var detalleHistorialSamborondon = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSamborondon && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialSamborondon != null)
                    {
                        datosSamborondon.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon>(detalleHistorialSamborondon.Data);
                        datosSamborondon.BusquedaNuevaRepresentante = detalleHistorialSamborondon.Cache;
                        datosSamborondon.HistorialCabecera = detalleHistorialSamborondon.Historial;
                    }

                    var detalleHistorialEmpresaSamborondon = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaSamborondon && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaSamborondon != null)
                    {
                        datosSamborondon.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSamborondon>(detalleHistorialEmpresaSamborondon.Data);
                        datosSamborondon.BusquedaNuevaEmpresa = detalleHistorialEmpresaSamborondon.Cache;
                        if (datosSamborondon.HistorialCabecera == null)
                            datosSamborondon.HistorialCabecera = detalleHistorialEmpresaSamborondon.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosSamborondon.cshtml", datosSamborondon);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDaule || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDaule)))
                {
                    var datosDaule = new PrediosDauleViewModel();
                    var detalleHistorialDaule = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioDaule && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialDaule != null)
                    {
                        datosDaule.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDaule>(detalleHistorialDaule.Data);
                        datosDaule.BusquedaNuevaRepresentante = detalleHistorialDaule.Cache;
                        datosDaule.HistorialCabecera = detalleHistorialDaule.Historial;
                    }

                    var detalleHistorialEmpresaDaule = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaDaule && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaDaule != null)
                    {
                        datosDaule.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosDaule>(detalleHistorialEmpresaDaule.Data);
                        datosDaule.BusquedaNuevaEmpresa = detalleHistorialEmpresaDaule.Cache;
                        if (datosDaule.HistorialCabecera == null)
                            datosDaule.HistorialCabecera = detalleHistorialEmpresaDaule.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosDaule.cshtml", datosDaule);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCayambe || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCayambe)))
                {
                    var datosCayambe = new PrediosCayambeViewModel();
                    var detalleHistorialCayambe = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCayambe && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialCayambe != null)
                    {
                        datosCayambe.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe>(detalleHistorialCayambe.Data);
                        datosCayambe.BusquedaNuevaRepresentante = detalleHistorialCayambe.Cache;
                        datosCayambe.HistorialCabecera = detalleHistorialCayambe.Historial;
                    }

                    var detalleHistorialEmpresaCayambe = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCayambe && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaCayambe != null)
                    {
                        datosCayambe.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCayambe>(detalleHistorialEmpresaCayambe.Data);
                        datosCayambe.BusquedaNuevaEmpresa = detalleHistorialEmpresaCayambe.Cache;
                        if (datosCayambe.HistorialCabecera == null)
                            datosCayambe.HistorialCabecera = detalleHistorialEmpresaCayambe.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosCayambe.cshtml", datosCayambe);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAzogues || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAzogues)))
                {
                    var datosAzogues = new PrediosAzoguesViewModel();
                    var detalleHistorialAzogues = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioAzogues && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialAzogues != null)
                    {
                        datosAzogues.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues>(detalleHistorialAzogues.Data);
                        datosAzogues.BusquedaNuevaRepresentante = detalleHistorialAzogues.Cache;
                        datosAzogues.HistorialCabecera = detalleHistorialAzogues.Historial;
                    }

                    var detalleHistorialEmpresaAzogues = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaAzogues && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaAzogues != null)
                    {
                        datosAzogues.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosAzogues>(detalleHistorialEmpresaAzogues.Data);
                        datosAzogues.BusquedaNuevaEmpresa = detalleHistorialEmpresaAzogues.Cache;
                        if (datosAzogues.HistorialCabecera == null)
                            datosAzogues.HistorialCabecera = detalleHistorialEmpresaAzogues.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosAzogues.cshtml", datosAzogues);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEsmeraldas || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaEsmeraldas)))
                {
                    var datosEsmeraldas = new PrediosEsmeraldasViewModel();
                    var detalleHistorialEsmeraldas = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEsmeraldas && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEsmeraldas != null)
                    {
                        datosEsmeraldas.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas>(detalleHistorialEsmeraldas.Data);
                        datosEsmeraldas.BusquedaNuevaRepresentante = detalleHistorialEsmeraldas.Cache;
                        datosEsmeraldas.HistorialCabecera = detalleHistorialEsmeraldas.Historial;
                    }

                    var detalleHistorialEmpresaEsmeraldas = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaEsmeraldas && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaEsmeraldas != null)
                    {
                        datosEsmeraldas.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosEsmeraldas>(detalleHistorialEmpresaEsmeraldas.Data);
                        datosEsmeraldas.BusquedaNuevaEmpresa = detalleHistorialEmpresaEsmeraldas.Cache;
                        if (datosEsmeraldas.HistorialCabecera == null)
                            datosEsmeraldas.HistorialCabecera = detalleHistorialEmpresaEsmeraldas.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosEsmeraldas.cshtml", datosEsmeraldas);
                }
                else if (await _detalleHistorial.AnyAsync(m => m.IdHistorial == idHistorial && (m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCotacachi || m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCotacachi)))
                {
                    var datosCotacachi = new PrediosCotacachiViewModel();
                    var detalleHistorialCotacachi = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCotacachi && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialCotacachi != null)
                    {
                        datosCotacachi.PrediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi>(detalleHistorialCotacachi.Data);
                        datosCotacachi.BusquedaNuevaRepresentante = detalleHistorialCotacachi.Cache;
                        datosCotacachi.HistorialCabecera = detalleHistorialCotacachi.Historial;
                    }

                    var detalleHistorialEmpresaCotacachi = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioEmpresaCotacachi && m.Generado, null, i => i.Include(m => m.Historial), true);
                    if (detalleHistorialEmpresaCotacachi != null)
                    {
                        datosCotacachi.PrediosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCotacachi>(detalleHistorialEmpresaCotacachi.Data);
                        datosCotacachi.BusquedaNuevaEmpresa = detalleHistorialEmpresaCotacachi.Cache;
                        if (datosCotacachi.HistorialCabecera == null)
                            datosCotacachi.HistorialCabecera = detalleHistorialEmpresaCotacachi.Historial;
                    }
                    return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePrediosCotacachi.cshtml", datosCotacachi);
                }
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePredios.cshtml", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuentePredios.cshtml", null);
            }
        }

        [Route("ObtenerDatosUafe")]
        public async Task<IActionResult> ObtenerDatosUafe(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new UafeViewModel();
                var fuentes = new[] { Dominio.Tipos.Fuentes.UafeOnu, Dominio.Tipos.Fuentes.UafeOnu2206, Dominio.Tipos.Fuentes.UafeInterpol, Dominio.Tipos.Fuentes.UafeOfac };
                var detallesHistorial = await _detalleHistorial.ReadAsync(m => m, m => m.IdHistorial == idHistorial && fuentes.Contains(m.TipoFuente), null, i => i.Include(m => m.Historial.Usuario), 0, null, true);

                if (!detallesHistorial.Any())
                    datos = null;
                else
                {
                    datos.HistorialCabecera = detallesHistorial.FirstOrDefault()?.Historial;
                    if (datos.HistorialCabecera != null)
                    {
                        var detalleHistorialOnu = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.UafeOnu && m.Generado);
                        if (detalleHistorialOnu != null)
                        {
                            datos.ONU = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.Resultado>(detalleHistorialOnu.Data);
                            datos.BusquedaNuevaOnu = detalleHistorialOnu.Cache;
                        }

                        var detalleHistorialOnu2206 = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.UafeOnu2206 && m.Generado);
                        if (detalleHistorialOnu2206 != null)
                        {
                            datos.ONU2206 = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.Resultado>(detalleHistorialOnu2206.Data);
                            datos.BusquedaNuevaOnu2206 = detalleHistorialOnu2206.Cache;
                        }
                        datos.AccesoOnu = true;
                    }

                    if (datos.HistorialCabecera != null)
                    {
                        var detalleHistorialOnuInterpol = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.UafeInterpol && m.Generado);
                        if (detalleHistorialOnuInterpol != null)
                        {
                            datos.Interpol = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.ResultadoInterpol>(detalleHistorialOnuInterpol.Data);
                            datos.BusquedaNuevaInterpol = detalleHistorialOnuInterpol.Cache;
                        }
                        datos.AccesoInterpol = true;
                    }

                    if (datos.HistorialCabecera != null)
                    {
                        var detalleHistorialOfac = detallesHistorial.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.UafeOfac && m.Generado);
                        if (detalleHistorialOfac != null)
                        {
                            datos.OFAC = JsonConvert.DeserializeObject<Externos.Logica.UAFE.Modelos.ResultadoOfac>(detalleHistorialOfac.Data);
                            datos.BusquedaNuevaOfac = detalleHistorialOfac.Cache;
                        }
                        datos.AccesoOfac = true;
                    }

                    if (datos.HistorialCabecera != null && (ValidacionViewModel.ValidarRucJuridico(datos.HistorialCabecera.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(datos.HistorialCabecera.Identificacion)))
                        datos.BusquedaJuridica = true;
                    else if (datos.HistorialCabecera != null && datos.HistorialCabecera.Usuario != null && datos.HistorialCabecera.Usuario.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0190150496001)
                    {
                        var valorFlexiPlast = await _parametrosClientesHistoriales.FirstOrDefaultAsync(m => m.Valor, m => m.IdHistorial == idHistorial && m.Valor.Trim() == "True" && m.Parametro == ParametrosClientes.ProveedorInternacional);
                        if (!string.IsNullOrEmpty(valorFlexiPlast?.Trim()) && bool.Parse(valorFlexiPlast.Trim()))
                            datos.BusquedaJuridica = true;
                    }
                }
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteUafe.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteUafe.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerDatosCalificacion")]
        public async Task<IActionResult> ObtenerDatosCalificacion(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                ViewBag.VisualizarBuro = true;
                ViewBag.EvaluarBuro = true;
                ViewBag.ClienteMicrocredito1790325083001 = string.Empty;
                ViewBag.ClienteConsumo1790325083001 = string.Empty;
                var datosCalificacion = await _calificaciones.ReadAsync(m => m, m => m.IdHistorial == idHistorial, null, i => i.Include(m => m.DetalleCalificacion).ThenInclude(m => m.Politica), 0, null, true);
                if (!datosCalificacion.Any())
                    throw new Exception("No se encontraron datos para calificación.");

                var valorParametro = await _parametrosClientesHistoriales.FirstOrDefaultAsync(m => m.Valor, m => m.IdHistorial == idHistorial, null, null, true);
                if (!string.IsNullOrEmpty(valorParametro))
                {
                    switch (valorParametro)
                    {
                        case var parametro when parametro == ((short)Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.MicroCredito).ToString():
                            ViewBag.ClienteMicrocredito1790325083001 = "Modelo Microcrédito";
                            break;
                        case var parametro when parametro == ((short)Dominio.Tipos.Clientes.Cliente1790325083001.SegmentoCartera.Consumo).ToString():
                            ViewBag.ClienteConsumo1790325083001 = "Modelo Consumo";
                            break;
                        default:
                            break;
                    }
                }

                var evaluacion = datosCalificacion.FirstOrDefault(x => x.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Evaluacion);
                var periodo = string.Empty;
                var lstImpuestoRenta = new[] { Dominio.Tipos.Politicas.ImpuestoRentaCedula, Dominio.Tipos.Politicas.ImpuestoRentaJuridico, Dominio.Tipos.Politicas.ImpuestoRentaNatural };

                if (evaluacion != null)
                {
                    periodo = evaluacion.DetalleCalificacion.FirstOrDefault(x => lstImpuestoRenta.Contains(x.Politica.Tipo))?.Observacion;

                    if (!string.IsNullOrEmpty(periodo))
                    {
                        var periodoValor = Regex.Matches(periodo, @"[0-9]+");
                        if (periodoValor != null && periodoValor.Any() && periodoValor[0].Length == 4)
                            periodo = periodoValor[0].ToString();
                    }
                }

                var calificacion = datosCalificacion.Select(m => new CalificacionViewModel
                {
                    IdCalificacion = m.Id,
                    IdHistorial = m.IdHistorial,
                    Aprobado = m.Aprobado,
                    TotalValidados = m.TotalVerificados,
                    TotalAprobados = m.NumeroAprobados,
                    TotalRechazados = m.NumeroRechazados,
                    Calificacion = m.Puntaje,
                    TipoCalificacion = m.TipoCalificacion ?? Dominio.Tipos.TiposCalificaciones.Desconocido,
                    Score = m.Score != null ? m.Score : 0,
                    CupoEstimado = m.CupoEstimado != null ? m.CupoEstimado : 0,
                    VentasEmpresa = m.VentasEmpresa != null ? m.VentasEmpresa : 0,
                    PatrimonioEmpresa = m.PatrimonioEmpresa != null ? m.PatrimonioEmpresa : 0,
                    RangoIngreso = m.RangoIngreso != null ? m.RangoIngreso : null,
                    GastoFinanciero = m.GastoFinanciero != null ? m.GastoFinanciero : 0,
                    TipoFuente = m.TipoFuenteBuro,
                    DetalleCalificacion = m.DetalleCalificacion.Select(x => new DetalleCalificacionViewModel
                    {
                        IdPolitica = x.IdPolitica,
                        Politica = $"{x.Politica.Nombre} {(lstImpuestoRenta.Contains(x.Politica.Tipo) ? periodo : string.Empty)}",
                        ReferenciaMinima = x.ReferenciaMinima,
                        ValorResultado = x.Datos,
                        Valor = x.Valor,
                        Parametro = x.Parametro,
                        ResultadoPolitica = x.Aprobado,
                        FechaCorte = x.FechaCorte,
                        Instituciones = x.Instituciones,
                        Tipo = x.Politica.Tipo,
                        Excepcional = x.Politica.Excepcional
                    }).ToList()
                }).ToList();

                var historial = await _historiales.FirstOrDefaultAsync(m => new { m.IdPlanBuroCredito, m.IdPlanEvaluacion, m.TipoIdentificacion, IdentificacionEmpresa = m.Usuario.Empresa.Identificacion, m.TipoFuenteBuro, m.Identificacion, m.Usuario.IdEmpresa, ModeloCooperativas = m.PlanBuroCredito != null ? m.PlanBuroCredito.ModeloCooperativas : false }, m => m.Id == idHistorial);
                if (!calificacion.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro) && historial != null && historial.IdPlanEvaluacion.HasValue && historial.IdPlanBuroCredito.HasValue)
                {
                    ViewBag.VisualizarBuro = true;
                    if (historial.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historial.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                        ViewBag.MensajeEvaluarBuro = Dominio.Constantes.PlanesBuroEstados.EmpresaSinInformacion;
                    else if (historial.TipoIdentificacion == Dominio.Constantes.General.Cedula || historial.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                        ViewBag.MensajeEvaluarBuro = Dominio.Constantes.PlanesBuroEstados.SujetoSinInformacion;

                }
                else if (!calificacion.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro) && historial != null && historial.IdPlanEvaluacion.HasValue)
                    ViewBag.VisualizarBuro = false;

                if (historial != null && calificacion.Any(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro))
                {
                    if (historial.IdentificacionEmpresa == Dominio.Constantes.Clientes.Cliente1792899036001 && historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval)
                    {
                        var dataBuro = await _detalleHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, null, null, true);
                        if (!string.IsNullOrEmpty(dataBuro))
                        {
                            var dataAval = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(dataBuro);
                            foreach (var item in calificacion.Where(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro))
                            {
                                item.Identificacion = historial?.Identificacion;
                                item.ModeloAutomotrizaAyasa = dataAval.Result.ModeloAutomotrizAyasa.FirstOrDefault();
                                item.CalificacionClienteAyasa = true;
                            }
                        }
                    }
                    else if (historial.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990304211001)
                    {
                        var dataBuro = await _detalleHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, null, null, true);
                        if (!string.IsNullOrEmpty(dataBuro))
                        {
                            var dataEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(dataBuro);
                            if (dataEquifax != null)
                                foreach (var item in calificacion.Where(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro))
                                {
                                    item.Identificacion = historial?.Identificacion;
                                    item.ModeloIndumot = dataEquifax.ResultadosIndumot;
                                    item.CalificacionClienteIndumot = true;
                                }
                        }
                    }
                    else if (historial.IdentificacionEmpresa == Dominio.Constantes.Clientes.Cliente1090105244001)
                    {
                        var dataBuro = await _detalleHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, null, null, true);
                        if (!string.IsNullOrEmpty(dataBuro))
                        {
                            var dataAval = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(dataBuro);
                            foreach (var item in calificacion.Where(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro))
                            {
                                item.Identificacion = historial?.Identificacion;
                                item.ModeloBCapital = dataAval;
                                item.CalificacionBCapital = true;
                            }
                        }
                    }
                    else if (historial.IdentificacionEmpresa == Dominio.Constantes.Clientes.Cliente1091796789001)
                    {
                        var dataBuro = await _detalleHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, null, null, true);
                        if (!string.IsNullOrEmpty(dataBuro))
                        {
                            var dataAval = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(dataBuro);
                            foreach (var item in calificacion.Where(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro))
                            {
                                item.Identificacion = historial?.Identificacion;
                                item.ModeloRagui = dataAval;
                                item.CalificacionRagui = true;
                            }
                        }
                    }
                    else if (historial.IdentificacionEmpresa == Dominio.Constantes.Clientes.Cliente0993382609001)
                    {
                        if (calificacion.Any(x => x.TipoCalificacion == TiposCalificaciones.Buro))
                            calificacion.Where(x => x.TipoCalificacion == TiposCalificaciones.Buro).FirstOrDefault().CalificacionMMotasa = true;
                    }
                    else if (historial.IdentificacionEmpresa == Dominio.Constantes.Clientes.Cliente0190372820001)
                    {
                        if (calificacion.Any(x => x.TipoCalificacion == TiposCalificaciones.Buro))
                            calificacion.Where(x => x.TipoCalificacion == TiposCalificaciones.Buro).FirstOrDefault().CalificacionMercaMovil = true;
                    }
                    else if (historial.ModeloCooperativas)
                    {
                        var dataBuro = await _detalleHistorial.FirstOrDefaultAsync(m => m.Data, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, null, null, true);
                        if (!string.IsNullOrEmpty(dataBuro))
                        {
                            var dataAval = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(dataBuro);
                            foreach (var item in calificacion.Where(m => m.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro))
                            {
                                item.Identificacion = historial?.Identificacion;
                                item.ModeloCooperativas = dataAval;
                                item.CalificacionCooperativas = true;
                            }
                        }
                    }
                    else if (historial.IdentificacionEmpresa == Dominio.Constantes.Clientes.Cliente0190386465001 && calificacion != null && calificacion.Any())
                    {
                        var idUsuario = User.GetUserId<int>();
                        var usuarioDatos = await _usuarios.FirstOrDefaultAsync(m => m, m => m.Id == idUsuario && m.Estado == Dominio.Tipos.EstadosUsuarios.Activo, null, i => i.Include(m => m.UsuariosRoles).ThenInclude(m => m.Rol));
                        var usuarioRol = usuarioDatos.UsuariosRoles.FirstOrDefault().Rol.Id;
                        if (usuarioRol == (short)Dominio.Tipos.Roles.VendedorEmpresa)
                        {
                            if (calificacion.Any(x => x.TipoCalificacion == TiposCalificaciones.Buro))
                                calificacion.Where(x => x.TipoCalificacion == TiposCalificaciones.Buro).FirstOrDefault().CalificacionVendedorAlmespana = true;
                        }
                    }
                }

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/_FuenteCalificacion.cshtml", calificacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView("~/Areas/Consultas/Views/Shared/Fuentes/_FuenteCalificacion.cshtml", null);
            }
        }
        #endregion Fuentes Detalle Historial

        #region FuentesEquifaqx

        [HttpPost]
        [Route("ObtenerNivelTotalDeudaHistoricaEquifax")]
        public async Task<IActionResult> ObtenerNivelTotalDeudaHistoricaEquifax(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new NivelTotalDeudaHistoricaViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelTotalDeudaHistorica, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null)
                    datos.TotalDeudaHistorica = detalleHistorial.Data != null ? JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoTotalDeudaHistorica>(detalleHistorial.Data) : null;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesTotalDeudaHistorica.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesTotalDeudaHistorica.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelEvolucionHistoricaDistribucionEndeudamientoEquifax")]
        public async Task<IActionResult> ObtenerNivelEvolucionHistoricaDistribucionEndeudamientoEquifax(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new NivelEvolucionHistoricaViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelEvolucionHistoricaDistribucionEndeudamiento, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null)
                    datos.EvolucionHistorica = detalleHistorial.Data != null ? JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoEvolucionHistorico>(detalleHistorial.Data) : null;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesEvolucionHistorica.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesTotalDeudaHistorica.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelHistoricoEstructuraVencimientosEquifax")]
        public async Task<IActionResult> ObtenerNivelHistoricoEstructuraVencimientosEquifax(int idHistorial)
        {
            try
            {
                if (idHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new HistoricoEstructuraVencimientoViewModel();
                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == idHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelHistoricoEstructuraVencimientos, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null)
                    datos.HistoricoEstructuraVencimiento = detalleHistorial.Data != null ? JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.ResultadoHistoricoEstructuraVencimiento>(detalleHistorial.Data) : null;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesHistoricoEstructuraVencimiento.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesHistoricoEstructuraVencimiento.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelSaldoPorVencerPorInstitucionEquifax")]
        public async Task<IActionResult> ObtenerNivelSaldoPorVencerPorInstitucionEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("El modelo esta vacio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new NivelSaldoPorVencerPorInstitucionViewModel();
                var lstDatos = new List<NivelSaldoPorVencerPorInstitucionViewModel>();

                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelSaldoPorVencerPorInstitucion, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Data))
                    lstDatos = JsonConvert.DeserializeObject<List<NivelSaldoPorVencerPorInstitucionViewModel>>(detalleHistorial.Data);

                if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()))
                    datos.SaldoVencerInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()).SaldoVencerInstitucion;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesSaldoVencerInstitucion.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesSaldoVencerInstitucion.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelOperacionesInstitucionEquifax")]
        public async Task<IActionResult> ObtenerNivelOperacionesInstitucionEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("El modelo esta vacio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new NivelOperacionInstitucionViewModel();
                var lstDatos = new List<NivelOperacionInstitucionViewModel>();
                var lstDatosPV = new List<NivelOperacionInstitucionViewModel>();

                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucion, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);
                var detalleHistorialPV = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelOperacionesPorInstitucionPV, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Data))
                    lstDatos = JsonConvert.DeserializeObject<List<NivelOperacionInstitucionViewModel>>(detalleHistorial.Data);

                if (detalleHistorialPV != null && !string.IsNullOrEmpty(detalleHistorialPV.Data))
                    lstDatosPV = JsonConvert.DeserializeObject<List<NivelOperacionInstitucionViewModel>>(detalleHistorialPV.Data);

                if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")))
                    datos.OperacionInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")).OperacionInstitucion;

                if (lstDatosPV != null && lstDatosPV.Any() && lstDatosPV.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")))
                    datos.OperacionInstitucionPV = lstDatosPV.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim() && x.TipoCredito == modelo.TipoCredito.Trim() && x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd")).OperacionInstitucionPV;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesOperacionInstitucion.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesOperacionInstitucion.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelDetalleVencidoPorInstitucionEquifax")]
        public async Task<IActionResult> ObtenerNivelDetalleVencidoPorInstitucionEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("El modelo esta vacio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new NivelDetalleVencidoInstitucionViewModel();
                var lstDatos = new List<NivelDetalleVencidoInstitucionViewModel>();

                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleVencidoPorInstitucion, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Data))
                    lstDatos = JsonConvert.DeserializeObject<List<NivelDetalleVencidoInstitucionViewModel>>(detalleHistorial.Data);

                if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()))
                    datos.DetalleVencidoInstitucion = lstDatos.FirstOrDefault(x => x.CodigoInstitucion == modelo.CodigoInstitucion.Trim()).DetalleVencidoInstitucion;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesDetalleVencidoPorInstitucion.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesDetalleVencidoPorInstitucion.cshtml", null);
            }
        }

        [HttpPost]
        [Route("ObtenerNivelDetalleOperacionesEntidadesEquifax")]
        public async Task<IActionResult> ObtenerNivelDetalleOperacionesEntidadesEquifax(ReporteEquifaxViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("El modelo esta vacio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El historial ingresado no se encuentra registrado");

                ViewBag.Historial = true;
                var datos = new NivelDetalleOperacionEntidadViewModel();
                var lstDatos = new List<NivelDetalleOperacionEntidadViewModel>();

                var detalleHistorial = await _detalleHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.NivelDetalleOperacionesYEntidades, null, s => s.Include(x => x.Historial.Usuario.Empresa).Include(x => x.Historial.PlanBuroCredito), true);

                if (detalleHistorial != null && !string.IsNullOrEmpty(detalleHistorial.Data))
                    lstDatos = JsonConvert.DeserializeObject<List<NivelDetalleOperacionEntidadViewModel>>(detalleHistorial.Data);

                if (lstDatos != null && lstDatos.Any() && lstDatos.Any(x => x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd") && x.SistemaCrediticio == modelo.SistemaCrediticio.Trim()))
                    datos.DetalleOperacionEntidad = lstDatos.FirstOrDefault(x => x.FechaCorte == modelo.FechaCorte.Value.ToString("yyyy-MM-dd") && x.SistemaCrediticio == modelo.SistemaCrediticio.Trim()).DetalleOperacionEntidad;

                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesDetalleOperacionesEntidades.cshtml", datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return PartialView($"~/Areas/Consultas/Views/Shared/Fuentes/FuentesEquifax/_FuentesDetalleOperacionesEntidades.cshtml", null);
            }
        }

        #endregion FuentesEquifaqx
    }
}
