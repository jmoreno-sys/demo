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
using Microsoft.AspNetCore.Identity;

namespace Web.Areas.Historiales.Controllers
{
    [Area("Reportes")]
    [Route("Reportes/ReporteResumenEvaluaciones")]
    [Authorize(Policy = "ReporteResumenConsultas")]
    public class ReporteResumenEvaluacionesController : Controller
    {
        private readonly ILogger _logger;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;

        public ReporteResumenEvaluacionesController(ILoggerFactory loggerFactory, IPlanesEvaluaciones planesEvaluaciones)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _planesEvaluaciones = planesEvaluaciones;
        }

        [Route("Inicio")]
        public IActionResult Inicio()
        {
            return NotFound();

            //if (!User.IsInRole(Dominio.Tipos.Roles.Administrador))
            //    return Unauthorized();

            //return View();
        }

        [HttpPost]
        [Route("ListadoReporte")]
        public IActionResult ListadoReporte([FromBody] ReporteResumenConsultasFiltrosViewModel filtros)
        {
            try
            {
                return NotFound();

                //if (filtros == null)
                //    throw new Exception("No se ha ingresado ningún filtro");

                //if (filtros.FechaHasta.Date == default)
                //    throw new Exception("Las fechas ingresadas no son válidas");

                //var planEvaluacion = (await _planesEvaluaciones.ReadAsync(x => x, s => s.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo && s.Empresa.Estado == Dominio.Tipos.EstadosEmpresas.Activo,
                //    null, i => i.Include(m => m.Empresa).Include(m => m.Historial.Where(m => m.Fecha.Date.Year == filtros.FechaHasta.Date.Year && m.Fecha.Date.Month == filtros.FechaHasta.Date.Month)))).ToList();

                //var historialPlanEvaluacion = planEvaluacion.Select(m => new
                //{
                //    m.Empresa.RazonSocial,
                //    m.Empresa.Identificacion,
                //    m.Empresa.FechaCreacion,
                //    NumeroMaximoConsultas = m.NumeroConsultas,
                //    ConsultasActualesMensuales = m.Historial.Count(c => c.IdPlanEvaluacion == m.Id),
                //    m.ValorConsulta,
                //    ValorAdicional = m.ValorConsultaAdicional
                //}).ToList();

                //var reporte = historialPlanEvaluacion.Select(m => new
                //{
                //    m.RazonSocial,
                //    m.Identificacion,
                //    m.FechaCreacion,
                //    EsValorMaximoRuc = m.NumeroMaximoConsultas == int.MaxValue,
                //    m.NumeroMaximoConsultas,
                //    m.ConsultasActualesMensuales,
                //    Saldo = m.NumeroMaximoConsultas - m.ConsultasActualesMensuales > 0 ? m.NumeroMaximoConsultas - m.ConsultasActualesMensuales : 0,
                //    m.ValorConsulta,
                //    ValorTotalConsultas = m.NumeroMaximoConsultas >= m.ConsultasActualesMensuales ? m.ConsultasActualesMensuales * m.ValorConsulta : m.NumeroMaximoConsultas * m.ValorConsulta,
                //    ConsultasAdicionalesMensuales = m.ConsultasActualesMensuales > m.NumeroMaximoConsultas ? (m.ConsultasActualesMensuales - m.NumeroMaximoConsultas) : 0,
                //    m.ValorAdicional,
                //    ValorTotalConsultasAdicionales = m.ConsultasActualesMensuales > m.NumeroMaximoConsultas ? (m.ConsultasActualesMensuales - m.NumeroMaximoConsultas) * m.ValorAdicional : 0,
                //}).Select(m => new
                //{
                //    Datos = m,
                //    ValorTotal = m.ValorTotalConsultas + m.ValorTotalConsultasAdicionales
                //}).OrderByDescending(m => m.Datos.FechaCreacion).ToList();

                //return Json(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }
    }
}
