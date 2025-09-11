using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Persistencia.Repositorios.Balance;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Web.Areas.Historiales.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace Web.Areas.Reportes.Controllers
{
    [Area("Reportes")]
    [Route("Reportes/ReporteResumenGraficas")]
    [Authorize(Policy = "ReporteResumenConsultas")]
    public class ReporteResumenGraficasController : Controller
    {
        private readonly ILogger _logger;
        private readonly ICalificaciones _calificaciones;
        private readonly IDetalleCalificaciones _detalleCalificaciones;

        public ReporteResumenGraficasController(ILoggerFactory loggerFactory, ICalificaciones calificaciones, IDetalleCalificaciones detalleCalificaciones)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _calificaciones = calificaciones;
            _detalleCalificaciones = detalleCalificaciones;
        }

        [Route("Inicio")]
        public IActionResult Inicio()
        {
            if (!User.IsInRole(Dominio.Tipos.Roles.Administrador))
                return Unauthorized();

            return View();
        }

        [HttpPost]
        [Route("ReporteResumenGraficas")]
        public async Task<IActionResult> ReporteResumenGraficas()
        {
            try
            {
                var resumenEvaluaciones = (await _calificaciones.ReadAsync(x => new { x.Id, x.TipoCalificacion, x.Aprobado }, x => x.FechaCreacion.Date >= DateTime.Now.Date.AddYears(-1) && x.FechaCreacion.Date <= DateTime.Now.Date, null, null, null, null, true)).ToList();
                var evaluacionAprobado = resumenEvaluaciones.Count(x => x.Aprobado);
                var evaluacionRechazado = resumenEvaluaciones.Count(x => !x.Aprobado);

                var graficoResumen = new List<ReporteResumenGraficasViewModel>
                {
                    new ReporteResumenGraficasViewModel { Name = "Aprobado", Y = evaluacionAprobado },
                    new ReporteResumenGraficasViewModel { Name = "Rechazado", Y = evaluacionRechazado }
                };

                return Json(graficoResumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ListadoPoliticasRechazo")]
        public async Task<IActionResult> ListadoPoliticasRechazo()
        {
            try
            {
                var resumenPoliticas = (await _detalleCalificaciones.ReadAsync(x => new { Nombre = x.Politica.Nombre.Trim(), x.Politica.Excepcional }, x => x.FechaCreacion.Date >= DateTime.Now.Date.AddYears(-1) && x.FechaCreacion.Date <= DateTime.Now.Date && !x.Aprobado && x.Calificacion.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Evaluacion && !x.Calificacion.Aprobado && x.Calificacion.Id == x.IdCalificacion, null, null, null, null, true)).ToList();
                var lstNombresPoliticas = resumenPoliticas.ToArray();
                var politicasAgrupadas = lstNombresPoliticas.GroupBy(x => new { x.Nombre, x.Excepcional }).Select(x => new { Politicas = x.Key.Nombre, Excepcional = x.Key.Excepcional, Cantidad = x.Count() }).ToList();

                return Json(politicasAgrupadas.OrderByDescending(x => x.Excepcional).ThenByDescending(x => x.Cantidad).Select(x => new { x.Politicas, x.Cantidad }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ListadoEvaluacionRechazo")]
        public async Task<IActionResult> ListadoEvaluacionRechazo()
        {
            try
            {
                var resumenPoliticas = (await _calificaciones.ReadAsync(x => x.TipoCalificacion, x => x.FechaCreacion.Date >= DateTime.Now.Date.AddYears(-1) && x.FechaCreacion.Date <= DateTime.Now.Date && !x.Aprobado, null, null, null, null, true)).ToList();
                var resumenEvaluacion = resumenPoliticas.GroupBy(x => x).Select(y => new { TipoCalificacion = y.Key, Cantidad = y.Count() }).ToList();

                return Json(resumenEvaluacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ReporteResumenGraficasBuro")]
        public async Task<IActionResult> ReporteResumenGraficasBuro()
        {
            try
            {
                var resumenEvaluacionesBuro = (await _calificaciones.ReadAsync(x => x.Score, x => x.FechaCreacion.Date >= DateTime.Now.Date.AddYears(-1) && x.FechaCreacion.Date <= DateTime.Now.Date && x.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro && !x.Aprobado && x.Score.HasValue, null, null, null, null, true)).ToList();
                var agruparScoreMenor = resumenEvaluacionesBuro.Where(x => x <= 749).Count();
                var agruparScoreB = resumenEvaluacionesBuro.Where(x => x >= 750 && x < 801).Count();
                var agruparScoreA = resumenEvaluacionesBuro.Where(x => x >= 801 && x < 876).Count();
                var agruparScoreAA = resumenEvaluacionesBuro.Where(x => x >= 876 && x < 951).Count();
                var agruparScoreAAA = resumenEvaluacionesBuro.Where(x => x >= 951).Count();

                var graficoResumenBuro = new List<ReporteResumenGraficasViewModel>
                {
                    new ReporteResumenGraficasViewModel { Name = "AAA", Y = agruparScoreAAA },
                    new ReporteResumenGraficasViewModel { Name = "AA", Y = agruparScoreAA },
                    new ReporteResumenGraficasViewModel { Name = "A", Y = agruparScoreA },
                    new ReporteResumenGraficasViewModel { Name = "B", Y = agruparScoreB },
                    new ReporteResumenGraficasViewModel { Name = "Menor a 750", Y = agruparScoreMenor },
                };

                return Json(graficoResumenBuro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("ListadoEvaluacionRechazoBuro")]
        public async Task<IActionResult> ListadoEvaluacionRechazoBuro()
        {
            try
            {
                var resumenPoliticasRechazoBuro = (await _calificaciones.ReadAsync(x => new { x.IdHistorial, x.Score, x.TipoCalificacion, x.Aprobado }, x => x.FechaCreacion.Date >= DateTime.Now.Date.AddYears(-1) && x.FechaCreacion.Date <= DateTime.Now.Date, null, null, null, null, true)).ToList();
                var rechazadoExternos = resumenPoliticasRechazoBuro.Where(x => !x.Aprobado && x.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Evaluacion).Select(x => new { x.IdHistorial, x.Score, x.TipoCalificacion, x.Aprobado }).ToList();
                var aprobadoBuro = resumenPoliticasRechazoBuro.Where(x => x.Aprobado && x.TipoCalificacion == Dominio.Tipos.TiposCalificaciones.Buro).Select(x => new { x.IdHistorial, x.Score, x.TipoCalificacion, x.Aprobado }).ToList();

                var agrupacionDatos = rechazadoExternos.Join(aprobadoBuro, x => x.IdHistorial, y => y.IdHistorial, (x, y) => new { x, y }).Where(z => z.x.IdHistorial == z.y.IdHistorial).ToList();

                var agruparTipoB = agrupacionDatos.Where(n => !n.x.Aprobado && n.y.Aprobado && n.y.Score >= 750 && n.y.Score < 801).Count();
                var agruparTipoA = agrupacionDatos.Where(n => !n.x.Aprobado && n.y.Aprobado && n.y.Score >= 801 && n.y.Score < 876).Count();
                var agruparTipoAA = agrupacionDatos.Where(n => !n.x.Aprobado && n.y.Aprobado && n.y.Score >= 876 && n.y.Score < 951).Count();
                var agruparTipoAAA = agrupacionDatos.Where(n => !n.x.Aprobado && n.y.Aprobado && n.y.Score >= 951).Count();

                var datosScoreRechazados = new List<ReporteScoreViewModel>
                {
                    new ReporteScoreViewModel{Calificacion = "AAA", Cantidad = agruparTipoAAA},
                    new ReporteScoreViewModel{Calificacion = "AA", Cantidad = agruparTipoAA},
                    new ReporteScoreViewModel{Calificacion = "A", Cantidad = agruparTipoA},
                    new ReporteScoreViewModel{Calificacion = "B", Cantidad = agruparTipoB},
                };

                return Json(datosScoreRechazados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }
    }
}
