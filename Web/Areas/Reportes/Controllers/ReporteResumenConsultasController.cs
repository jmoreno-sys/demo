using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Persistencia.Repositorios.Balance;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Web.Areas.Historiales.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Persistencia.Repositorios.Identidad;
using Microsoft.AspNetCore.Identity;

namespace Web.Areas.Historiales.Controllers
{
    [Area("Reportes")]
    [Route("Reportes/ReporteResumenConsultas")]
    [Authorize(Policy = "ReporteResumenConsultas")]
    public class ReporteResumenConsultasController : Controller
    {
        private readonly ILogger _logger;
        private readonly IPlanesEmpresas _planesEmpresa;
        private readonly IPlanesBuroCredito _planesBuro;
        private readonly IUsuarios _usuarios;

        public ReporteResumenConsultasController(ILoggerFactory loggerFactory, IPlanesEmpresas planesEmpresa, IPlanesBuroCredito planesBuro, IUsuarios usuarios)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _planesEmpresa = planesEmpresa;
            _planesBuro = planesBuro;
            _usuarios = usuarios;
        }

        [Route("Inicio")]
        public IActionResult Inicio()
        {
            ViewBag.Asesores = 0;
            ViewBag.AdministradorCliente = 0;
            return View();
        }

        [AllowAnonymous]
        [Route("InicioAsesores")]
        public async Task<IActionResult> InicioAsesores()
        {
            try
            {
                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.General.IdEmpresaConfiable)
                    throw new Exception("No está autorizado para acceder a este recurso");

                ViewBag.Asesores = 1;
                ViewBag.AdministradorCliente = 0;
                return View("Inicio");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Unauthorized();
            }
        }

        [HttpPost]
        [Route("ListadoReporte")]
        public async Task<IActionResult> ListadoReporte([FromBody] ReporteResumenConsultasFiltrosViewModel filtros)
        {
            try
            {
                if (filtros == null)
                    throw new Exception("No se ha ingresado ningún filtro");

                if (filtros.FechaHasta.Date == default)
                    throw new Exception("Las fechas ingresadas no son válidas");

                var planEmpresa = (await _planesEmpresa.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo,
                    null, i => i.Include(m => m.Empresa).Include(m => m.Historial.Where(m => m.Fecha.Date.Year == filtros.FechaHasta.Date.Year && m.Fecha.Date.Month == filtros.FechaHasta.Date.Month)))).ToList();

                var historialPlanEmpresa = planEmpresa.Select(m => new ReporteResumenConsultasViewModel
                {
                    IdEmpresa = m.Empresa.Id,
                    RazonSocial = m.Empresa.RazonSocial,
                    Identificacion = m.Empresa.Identificacion,
                    //NumeroMaximoConsultasRuc = m.NumeroConsultasRuc,
                    //NumeroMaximoConsultasCedula = m.NumeroConsultasCedula,
                    //ConsultasActualesMensualesRuc = m.Historial != null && m.Historial.Any() ? m.Historial.Count(c => c.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || c.TipoIdentificacion == Dominio.Constantes.General.SectorPublico || c.TipoIdentificacion == Dominio.Constantes.General.RucNatural) : 0,
                    //ConsultasActualesMensualesCedula = m.Historial != null && m.Historial.Any() ? m.Historial.Count(c => c.TipoIdentificacion == Dominio.Constantes.General.Cedula) : 0,
                    //ValorAdicionalRuc = m.ValorConsultaAdicionalRucs,
                    //ValorAdicionalCedula = m.ValorConsultaAdicionalCedulas,
                    NumeroMaximoConsultas = m.NumeroConsultas ?? 0,
                    ConsultasActualesMensuales = m.Historial != null && m.Historial.Any() ? m.Historial.Count() : 0,
                    ValorAdicional = m.ValorConsultaAdicional ?? 0,
                    EsUnificado = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado,
                    FechaCreacion = m.Empresa.FechaCreacion,
                    PlanDemo = m.PlanDemostracion
                }).ToList();

                var planEmpresaBuro = (await _planesBuro.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo,
                    null, i => i.Include(m => m.Empresa).Include(m => m.Historial.Where(m => m.Fecha.Date.Year == filtros.FechaHasta.Date.Year && m.Fecha.Date.Month == filtros.FechaHasta.Date.Month)))).ToList();

                var historialPlanBuro = planEmpresaBuro.Select(m => new ReporteResumenConsultasViewModel
                {
                    IdEmpresa = m.Empresa.Id,
                    RazonSocial = m.Empresa.RazonSocial,
                    Identificacion = m.Empresa.Identificacion,
                    NumeroMaximoConsultasBuro = m.NumeroMaximoConsultas,
                    ConsultasActualesBuro = m.Historial != null && m.Historial.Any() ? m.Historial.Count(c => c.IdPlanBuroCredito == m.Id) : 0
                });

                var datosEmpresaBuro = from x in historialPlanEmpresa
                                       join y in historialPlanBuro
                                       on x.IdEmpresa equals y.IdEmpresa into ps
                                       from y in ps.DefaultIfEmpty()
                                       select new
                                       {
                                           x.IdEmpresa,
                                           x.RazonSocial,
                                           x.Identificacion,
                                           //x.NumeroMaximoConsultasRuc,
                                           //x.NumeroMaximoConsultasCedula,
                                           //x.ConsultasActualesMensualesRuc,
                                           //x.ConsultasActualesMensualesCedula,
                                           //x.ValorAdicionalRuc,
                                           //x.ValorAdicionalCedula,
                                           x.NumeroMaximoConsultas,
                                           x.ConsultasActualesMensuales,
                                           x.ValorAdicional,
                                           x.EsUnificado,
                                           x.FechaCreacion,
                                           x.PlanDemo,
                                           NumeroMaximoConsultasBuro = y != null ? y.NumeroMaximoConsultasBuro : 0,
                                           ConsultasActualesBuro = y != null ? y.ConsultasActualesBuro : 0
                                       };

                var reporte = datosEmpresaBuro.Select(m => new
                {
                    m.RazonSocial,
                    m.Identificacion,
                    //EsValorMaximoRuc = m.NumeroMaximoConsultasRuc == int.MaxValue,
                    //EsValorMaximoCedula = m.NumeroMaximoConsultasCedula == int.MaxValue,
                    EsValorMaximo = m.NumeroMaximoConsultas == int.MaxValue,
                    //m.NumeroMaximoConsultasRuc,
                    //m.ConsultasActualesMensualesRuc,
                    //SaldoRuc = m.NumeroMaximoConsultasRuc - m.ConsultasActualesMensualesRuc,
                    //m.NumeroMaximoConsultasCedula,
                    //m.ConsultasActualesMensualesCedula,
                    //SaldoCedula = m.NumeroMaximoConsultasCedula - m.ConsultasActualesMensualesCedula,
                    //m.ValorAdicionalRuc,
                    //m.ValorAdicionalCedula,
                    m.NumeroMaximoConsultas,
                    m.ConsultasActualesMensuales,
                    Saldo = m.NumeroMaximoConsultas - m.ConsultasActualesMensuales,
                    m.ValorAdicional,
                    EsValorMaximoBuro = m.NumeroMaximoConsultasBuro == int.MaxValue,
                    m.NumeroMaximoConsultasBuro,
                    m.ConsultasActualesBuro,
                    SaldoBuro = m.NumeroMaximoConsultasBuro - m.ConsultasActualesBuro,
                    m.EsUnificado,
                    m.FechaCreacion,
                    m.PlanDemo
                }).OrderByDescending(m => m.FechaCreacion).ToList();
                return Json(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("ListadoReporteAsesores")]
        public async Task<IActionResult> ListadoReporteAsesores([FromBody] ReporteResumenConsultasFiltrosViewModel filtros)
        {
            try
            {
                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.General.IdEmpresaConfiable)
                    throw new Exception("No está autorizado para acceder a este recurso");

                if (filtros == null)
                    throw new Exception("No se ha ingresado ningún filtro");

                if (filtros.FechaHasta.Date == default)
                    throw new Exception("Las fechas ingresadas no son válidas");

                var planEmpresa = (await _planesEmpresa.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo,
                    null, i => i.Include(m => m.Empresa).Include(m => m.Historial.Where(m => m.Fecha.Date.Year == filtros.FechaHasta.Date.Year && m.Fecha.Date.Month == filtros.FechaHasta.Date.Month)))).ToList();

                var historialPlanEmpresa = planEmpresa.Select(m => new ReporteResumenConsultasViewModel
                {
                    IdEmpresa = m.Empresa.Id,
                    IdAsesorComercialConfiable = m.Empresa.IdAsesorComercialConfiable,
                    RazonSocial = m.Empresa.RazonSocial,
                    Identificacion = m.Empresa.Identificacion,
                    NumeroMaximoConsultasRuc = m.NumeroConsultasRuc,
                    NumeroMaximoConsultasCedula = m.NumeroConsultasCedula,
                    ConsultasActualesMensualesRuc = m.Historial != null && m.Historial.Any() ? m.Historial.Count(c => c.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || c.TipoIdentificacion == Dominio.Constantes.General.SectorPublico || c.TipoIdentificacion == Dominio.Constantes.General.RucNatural) : 0,
                    ConsultasActualesMensualesCedula = m.Historial != null && m.Historial.Any() ? m.Historial.Count(c => c.TipoIdentificacion == Dominio.Constantes.General.Cedula) : 0,
                    ValorAdicionalRuc = m.ValorConsultaAdicionalRucs,
                    ValorAdicionalCedula = m.ValorConsultaAdicionalCedulas,
                    NumeroMaximoConsultas = m.NumeroConsultas ?? 0,
                    ConsultasActualesMensuales = m.Historial != null && m.Historial.Any() ? m.Historial.Count() : 0,
                    ValorAdicional = m.ValorConsultaAdicional ?? 0,
                    EsUnificado = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado,
                    FechaCreacion = m.Empresa.FechaCreacion,
                    PlanDemo = m.PlanDemostracion
                }).ToList();

                var planEmpresaBuro = (await _planesBuro.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo,
                    null, i => i.Include(m => m.Empresa).Include(m => m.Historial.Where(m => m.Fecha.Date.Year == filtros.FechaHasta.Date.Year && m.Fecha.Date.Month == filtros.FechaHasta.Date.Month)))).ToList();

                var historialPlanBuro = planEmpresaBuro.Select(m => new ReporteResumenConsultasViewModel
                {
                    IdEmpresa = m.Empresa.Id,
                    RazonSocial = m.Empresa.RazonSocial,
                    Identificacion = m.Empresa.Identificacion,
                    NumeroMaximoConsultasBuro = m.NumeroMaximoConsultas,
                    ConsultasActualesBuro = m.Historial != null && m.Historial.Any() ? m.Historial.Count(c => c.IdPlanBuroCredito == m.Id) : 0
                });

                var datosEmpresaBuro = from x in historialPlanEmpresa
                                       join y in historialPlanBuro
                                       on x.IdEmpresa equals y.IdEmpresa into ps
                                       from y in ps.DefaultIfEmpty()
                                       select new
                                       {
                                           x.IdEmpresa,
                                           x.IdAsesorComercialConfiable,
                                           x.RazonSocial,
                                           x.Identificacion,
                                           x.NumeroMaximoConsultasRuc,
                                           x.NumeroMaximoConsultasCedula,
                                           x.ConsultasActualesMensualesRuc,
                                           x.ConsultasActualesMensualesCedula,
                                           x.ValorAdicionalRuc,
                                           x.ValorAdicionalCedula,
                                           x.NumeroMaximoConsultas,
                                           x.ConsultasActualesMensuales,
                                           x.ValorAdicional,
                                           x.EsUnificado,
                                           x.FechaCreacion,
                                           x.PlanDemo,
                                           NumeroMaximoConsultasBuro = y != null ? y.NumeroMaximoConsultasBuro : 0,
                                           ConsultasActualesBuro = y != null ? y.ConsultasActualesBuro : 0
                                       };

                var reporte = datosEmpresaBuro.Select(m => new
                {
                    m.RazonSocial,
                    m.Identificacion,
                    m.IdAsesorComercialConfiable,
                    EsValorMaximoRuc = m.NumeroMaximoConsultasRuc == int.MaxValue,
                    EsValorMaximoCedula = m.NumeroMaximoConsultasCedula == int.MaxValue,
                    EsValorMaximo = m.NumeroMaximoConsultas == int.MaxValue,
                    m.NumeroMaximoConsultasRuc,
                    m.ConsultasActualesMensualesRuc,
                    SaldoRuc = m.NumeroMaximoConsultasRuc - m.ConsultasActualesMensualesRuc,
                    m.NumeroMaximoConsultasCedula,
                    m.ConsultasActualesMensualesCedula,
                    SaldoCedula = m.NumeroMaximoConsultasCedula - m.ConsultasActualesMensualesCedula,
                    m.ValorAdicionalRuc,
                    m.ValorAdicionalCedula,
                    m.NumeroMaximoConsultas,
                    m.ConsultasActualesMensuales,
                    Saldo = m.NumeroMaximoConsultas - m.ConsultasActualesMensuales,
                    m.ValorAdicional,
                    EsValorMaximoBuro = m.NumeroMaximoConsultasBuro == int.MaxValue,
                    m.NumeroMaximoConsultasBuro,
                    SaldoBuro = m.NumeroMaximoConsultasBuro - m.ConsultasActualesBuro,
                    m.EsUnificado,
                    m.FechaCreacion,
                    m.PlanDemo
                }).Where(m => m.IdAsesorComercialConfiable.HasValue ? m.IdAsesorComercialConfiable.Value == idUsuario : false).OrderByDescending(m => m.FechaCreacion).ToList();
                return Json(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }
    }
}
