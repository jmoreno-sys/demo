// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Repositorios.Balance;
using Persistencia.Repositorios.Identidad;
using Web.Areas.Historiales.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace Web.Areas.Reportes.Controllers
{
    [Area("Reportes")]
    [Route("Reportes/ReporteResumenClientes")]
    [Authorize(Policy = "ReporteResumenCliente")]
    public class ReporteResumenClientesController : Controller
    {
        private readonly ILogger _logger;
        private readonly IPlanesEmpresas _planesEmpresa;
        private readonly IPlanesBuroCredito _planesBuro;
        private readonly IUsuarios _usuarios;

        public ReporteResumenClientesController(ILoggerFactory loggerFactory, IPlanesEmpresas planesEmpresa, IPlanesBuroCredito planesBuro, IUsuarios usuarios)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _planesEmpresa = planesEmpresa;
            _planesBuro = planesBuro;
            _usuarios = usuarios;
        }

        [Route("Inicio")]
        public IActionResult Inicio()
        {
            ViewBag.AdministradorCliente = 1;
            return View("../ReporteResumenConsultas/Inicio");
        }

        [HttpPost]
        [Route("ListadoReporteCliente")]
        public async Task<IActionResult> ListadoReporteCliente([FromBody] ReporteResumenConsultasFiltrosViewModel filtros)
        {
            try
            {
                if (filtros == null)
                    throw new Exception("No se ha ingresado ningún filtro");

                if (filtros.FechaHasta.Date == default)
                    throw new Exception("Las fechas ingresadas no son válidas");

                var idUsuario = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planEmpresa = (await _planesEmpresa.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo && s.IdEmpresa == usuarioActual.IdEmpresa,
                    null, i => i.Include(m => m.Empresa).Include(m => m.Historial.Where(m => m.Fecha.Date.Year == filtros.FechaHasta.Date.Year && m.Fecha.Date.Month == filtros.FechaHasta.Date.Month)))).ToList();

                var historialPlanEmpresa = planEmpresa.Select(m => new ReporteResumenConsultasViewModel
                {
                    IdEmpresa = m.Empresa.Id,
                    RazonSocial = m.Empresa.RazonSocial,
                    Identificacion = m.Empresa.Identificacion,
                    NumeroMaximoConsultas = m.NumeroConsultas ?? 0,
                    ConsultasActualesMensuales = m.Historial != null && m.Historial.Any() ? m.Historial.Count() : 0,
                    ValorAdicional = m.ValorConsultaAdicional ?? 0,
                    EsUnificado = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado,
                    FechaCreacion = m.Empresa.FechaCreacion,
                    PlanDemo = m.PlanDemostracion
                }).ToList();

                var planEmpresaBuro = (await _planesBuro.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo && s.IdEmpresa == usuarioActual.IdEmpresa,
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
                    EsValorMaximo = m.NumeroMaximoConsultas == int.MaxValue,
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
    }
}
