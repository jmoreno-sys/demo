// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using Web.Areas.Consultas.Models;
using Persistencia.Repositorios.Balance;
using Persistencia.Repositorios.Identidad;
using ExcelDataReader;
using Web.Areas.Consultas.Controllers;
using Infraestructura.Servicios;
using DocumentFormat.OpenXml.Spreadsheet;
using Web.References;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Web.Models;
using Externos.Logica.Garancheck.Modelos;
using Externos.Logica.ANT;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Dominio.Entidades.Balances;
using Persistencia.Configuraciones;
using Externos.Logica.UAFE.Resultados;
using System.Linq.Expressions;
using System.Threading;
using NuGet.Packaging;
using DocumentFormat.OpenXml.Bibliography;
using Dominio.Entidades.Identidad;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using Externos.Logica.SRi.Modelos;
using DocumentFormat.OpenXml.Office2010.Drawing;
using Externos.Logica.Equifax.Resultados;
using DocumentFormat.OpenXml.Office.CustomUI;

namespace Web.Controllers.API
{
    [Route("api/AlmespanaSoyoda")]
    [ApiController]
    public class BatchAlmespanaController : Controller
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detalleHistorial;
        private readonly IConsultaService _consulta;
        private readonly ICalificaciones _calificaciones;
        private readonly IPlanesBuroCredito _planesBuroCredito;
        private readonly IAccesos _accesos;
        private readonly IUsuarios _usuarios;
        private readonly IPoliticas _politicas;
        private readonly IDetalleCalificaciones _detalleCalificaciones;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly Externos.Logica.Equifax.Controlador _buroCreditoEquifax;

        public BatchAlmespanaController(IConfiguration configuration,
            ILoggerFactory loggerFactory,
            Externos.Logica.Equifax.Controlador buroCreditoEquifax,
            IHistoriales historiales,
            IDetallesHistorial detallehistoriales,
            IConsultaService consulta,
            ICalificaciones calificaciones,
            IPlanesBuroCredito planesBuroCredito,
            IAccesos accesos,
            IPoliticas politicas,
            IDetalleCalificaciones detalleCalificaciones,
            IPlanesEvaluaciones planesEvaluaciones,
            IUsuarios usuarios
            )
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _historiales = historiales;
            _detalleHistorial = detallehistoriales;
            _consulta = consulta;
            _calificaciones = calificaciones;
            _planesBuroCredito = planesBuroCredito;
            _accesos = accesos;
            _usuarios = usuarios;
            _politicas = politicas;
            _detalleCalificaciones = detalleCalificaciones;
            _planesEvaluaciones = planesEvaluaciones;
            _buroCreditoEquifax = buroCreditoEquifax;
            _loggerFactory = loggerFactory;
        }


        [HttpGet("REPORTEFINAL1")]
        public async Task<IActionResult> REPORTEFINAL1()
        {
            try
            {
                var dt = new DataTable();
                dt.Columns.Add("NOMBRE_USUARIO", typeof(string));
                dt.Columns.Add("FECHA", typeof(string));
                dt.Columns.Add("TIPO_IDENTIFICACION", typeof(string));
                dt.Columns.Add("IDENTIFICACION_CONSULTADA", typeof(string));
                dt.Columns.Add("RAZON_SOCIAL", typeof(string));
                dt.Columns.Add("CANAL_CONSULTA", typeof(string));
                dt.Columns.Add("CONSULTA_BURO", typeof(string));
                dt.Columns.Add("EDAD", typeof(string));
                dt.Columns.Add("IESS_ESTADO", typeof(string));
                dt.Columns.Add("SCORE", typeof(string));
                dt.Columns.Add("CLIENTE_PEOR_SCORE", typeof(string));
                dt.Columns.Add("TASA_MALOS", typeof(string));
                dt.Columns.Add("SEGMENTO", typeof(string));
                dt.Columns.Add("TIEMPO_DESDE_PRIMERCREDITO", typeof(string));
                dt.Columns.Add("CUPO_TARJETA", typeof(string));
                dt.Columns.Add("PRESENCIA_MORA", typeof(string));
                dt.Columns.Add("PRESENCIA_CONSULTAS", typeof(string));
                dt.Columns.Add("DEUDA_TOTAL", typeof(string));
                dt.Columns.Add("MAXIMO_MONTO_OTORGADO", typeof(string));
                dt.Columns.Add("TIEMPO_SIN_OPERACIONES", typeof(string));
                dt.Columns.Add("CANT_OPE_SIN_MORA", typeof(string));
                dt.Columns.Add("PRESENCIA_HIPOTECARIO", typeof(string));
                dt.Columns.Add("VALOR_DEUDAS_VENCIDAS", typeof(string));
                dt.Columns.Add("DIAS_ATRASO", typeof(string));
                dt.Columns.Add("PRESENCIA_DEUDA_CASTIGADA_DEMANDA", typeof(string));
                dt.Columns.Add("CANT_OPE_CON_MORA", typeof(string));
                dt.Columns.Add("Valor_Deudas_Castigadas_Demanda_J", typeof(string));

                var fechainicial = new DateTime(2024, 11, 30);
                var fechafinal = new DateTime(2024, 12, 10);

                IList<Historial> historiales = new List<Historial>();
#if DEBUG

                historiales = await _historiales.ReadAsync(m => m, m => m.IdPlanEmpresa == 266 && m.Fecha > fechainicial && m.Fecha < fechafinal, null, m => m.Include(n => n.DetalleHistorial).Include(n => n.Usuario));
#endif

                var model = new ReporteViewModel();
                foreach (var item in historiales)
                {
                    var NOMBRE_USUARIO = item.Usuario.NombreCompleto;
                    var FECHA = item.Fecha.ToString("d/MM/yyyy HH:mm");
                    var TIPO_IDENTIFICACION = item.TipoIdentificacion;
                    var IDENTIFICACION_CONSULTADA = item.IdPlanEvaluacion != null ? $"{item.Identificacion} - Evaluado" : $"{item.Identificacion}";
                    var RAZON_SOCIAL = item.NombresPersona;
                    var CANAL_CONSULTA = item.TipoConsulta.ToString();
                    var CONSULTA_BURO = item.TipoFuenteBuro != null ? item.TipoFuenteBuro.GetEnumDescription() : "-";
                    var EDAD = string.Empty;
                    var IESS_ESTADO = string.Empty;
                    var SCORE = string.Empty;
                    var CLIENTE_PEOR_SCORE = string.Empty;
                    var TASA_MALOS = string.Empty;
                    var SEGMENTO = string.Empty;
                    var TIEMPO_DESDE_PRIMERCREDITO = string.Empty;
                    var CUPO_TARJETA = string.Empty;
                    var PRESENCIA_MORA = string.Empty;
                    var PRESENCIA_CONSULTAS = string.Empty;
                    var DEUDA_TOTAL = string.Empty;
                    var MAXIMO_MONTO_OTORGADO = string.Empty;
                    var TIEMPO_SIN_OPERACIONES = string.Empty;
                    var CANT_OPE_SIN_MORA = string.Empty;
                    var PRSENCIA_HIPOTECARIO = string.Empty;
                    var VALOR_DEUDAS_VENCIDAS = string.Empty;
                    var DIAS_ATRASO = string.Empty;
                    var PRESENCIA_DEUDA_CASTIGADA_DEMANDA = string.Empty;
                    var CANT_OPE_CON_MORA = string.Empty;
                    var Valor_Deudas_Castigadas_Demanda_J = string.Empty;

                    try
                    {
                        var historial = item;
                        _logger.LogInformation($"Consultando {IDENTIFICACION_CONSULTADA }...");
                        var detallesHistoriales = historial.DetalleHistorial;

                        var civil = new CivilViewModel();
                        if ((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil)) != null && (detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil)).Data != null)
                        {
                            var civil2 = JsonConvert.DeserializeObject<RegistroCivil>((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil).Data));
                            civil.RegistroCivil = civil2;
                        }
                        if ((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano)) != null && (detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano)).Data != null)
                        {
                            var civil2 = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano).Data));
                            civil.Ciudadano = civil2;
                        }

                        var iess = new Externos.Logica.IESS.Modelos.Afiliacion();
                        if ((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado)) != null && (detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado)).Data != null)
                        {
                            var iess2 = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Afiliacion>((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado).Data));
                            iess = iess2;
                            IESS_ESTADO = iess?.Estado;
                        }

                        //Civil
                        if (civil != null && civil.Ciudadano != null && civil.Ciudadano.FechaNacimiento.HasValue)
                        {
                            var birthdate = civil.Ciudadano.FechaNacimiento.Value;
                            var today = DateTime.Today;
                            var age = today.Year - birthdate.Year;
                            if (birthdate.Date > today.AddYears(-age)) age--;
                            EDAD = age.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(EDAD))
                        {
                            if (civil != null && civil.RegistroCivil != null && civil.RegistroCivil.FechaNacimiento >= DateTime.MinValue)
                            {
                                var birthdate = civil.RegistroCivil.FechaNacimiento;
                                var today = DateTime.Today;
                                var age = today.Year - birthdate.Year;
                                if (birthdate.Date > today.AddYears(-age)) age--;
                                EDAD = age.ToString();
                            }
                        }

                        //Buro
                        if ((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito)) != null && (detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito)).Data != null)
                        {
                            var buro = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>((detallesHistoriales.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito).Data));

                            if (item.TipoIdentificacion == "C")
                            {
                                if (buro.Resultados != null && buro.Resultados.ScoreV4V10 != null)
                                {
                                    SCORE = buro.Resultados.ScoreV4V10.Score + "";
                                    CLIENTE_PEOR_SCORE = buro.Resultados.ScoreV4V10.TotalAcum + "%";
                                    TASA_MALOS = buro.Resultados.ScoreV4V10.TasaDeMalosAcum + "%";
                                }
                            }
                            else
                            {
                                if (buro.ResultadosNivelIndexPymes != null && buro.ResultadosNivelIndexPymes.PuntajeyGraficoIndexPymes != null)
                                {
                                    SCORE = buro.ResultadosNivelIndexPymes.PuntajeyGraficoIndexPymes.Score + "";
                                    CLIENTE_PEOR_SCORE = buro.ResultadosNivelIndexPymes.PuntajeyGraficoIndexPymes.TasaMalosAcum + "%";
                                    TASA_MALOS = buro.ResultadosNivelIndexPymes.PuntajeyGraficoIndexPymes.Porcentaje + "";
                                }
                            }

                            if (buro.Resultados != null && buro.Resultados.FactoresQueInfluyenScoreV4 != null)
                            {
                                SEGMENTO = buro.Resultados.FactoresQueInfluyenScoreV4.Segmento;
                                TIEMPO_DESDE_PRIMERCREDITO = "" + buro.Resultados.FactoresQueInfluyenScoreV4.TiempoDesdePrimerCredito;
                                CUPO_TARJETA = "$ " + buro.Resultados.FactoresQueInfluyenScoreV4.CupoTarjetaCredito;
                                PRESENCIA_MORA = "" + buro.Resultados.FactoresQueInfluyenScoreV4.PresenciaMora;
                                PRESENCIA_CONSULTAS = "" + buro.Resultados.FactoresQueInfluyenScoreV4.PresenciaConsultas;
                                DEUDA_TOTAL = "$ " + buro.Resultados.FactoresQueInfluyenScoreV4.DeudaTotal;
                                MAXIMO_MONTO_OTORGADO = "$ " + buro.Resultados.FactoresQueInfluyenScoreV4.MaximoMontoOtorgado;
                                TIEMPO_SIN_OPERACIONES = "" + buro.Resultados.FactoresQueInfluyenScoreV4.TiempoSinOperaciones;
                                CANT_OPE_SIN_MORA = "" + buro.Resultados.FactoresQueInfluyenScoreV4.CantidadOperacionesSinMora;
                                PRSENCIA_HIPOTECARIO = "" + buro.Resultados.FactoresQueInfluyenScoreV4.PresenciaCreditosHipotecarios;
                                VALOR_DEUDAS_VENCIDAS = "$ " + buro.Resultados.FactoresQueInfluyenScoreV4.ValorDeudasVencidas;
                                DIAS_ATRASO = "" + buro.Resultados.FactoresQueInfluyenScoreV4.DiasAtraso;
                                PRESENCIA_DEUDA_CASTIGADA_DEMANDA = "" + buro.Resultados.FactoresQueInfluyenScoreV4.PresenciaDeudaCastigadaDemanda;
                                CANT_OPE_CON_MORA = "" + buro.Resultados.FactoresQueInfluyenScoreV4.CantidadOperacionesConMora;
                                Valor_Deudas_Castigadas_Demanda_J = "" + buro.Resultados.FactoresQueInfluyenScoreV4.ValorDeudasCastigadasDemandaJudicial;
                            }

                        }

                        //Excel
                        dt.Rows.Add(
                           NOMBRE_USUARIO,
                           FECHA,
                           TIPO_IDENTIFICACION,
                           IDENTIFICACION_CONSULTADA,
                           RAZON_SOCIAL,
                           CANAL_CONSULTA,
                           CONSULTA_BURO,
                           EDAD,
                           IESS_ESTADO,
                           SCORE,
                           CLIENTE_PEOR_SCORE,
                           TASA_MALOS,
                           SEGMENTO,
                           TIEMPO_DESDE_PRIMERCREDITO,
                           CUPO_TARJETA,
                           PRESENCIA_MORA,
                           PRESENCIA_CONSULTAS,
                           DEUDA_TOTAL,
                           MAXIMO_MONTO_OTORGADO,
                           TIEMPO_SIN_OPERACIONES,
                           CANT_OPE_SIN_MORA,
                           PRSENCIA_HIPOTECARIO,
                           VALOR_DEUDAS_VENCIDAS,
                           DIAS_ATRASO,
                           PRESENCIA_DEUDA_CASTIGADA_DEMANDA,
                           CANT_OPE_CON_MORA,
                           Valor_Deudas_Castigadas_Demanda_J

                        );
                        _logger.LogInformation($"Fin de consulta {IDENTIFICACION_CONSULTADA}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, ex.Message);
                        dt.Rows.Add(
                            NOMBRE_USUARIO,
                           FECHA,
                           TIPO_IDENTIFICACION,
                           IDENTIFICACION_CONSULTADA,
                           RAZON_SOCIAL,
                           CANAL_CONSULTA,
                           CONSULTA_BURO,
                           EDAD,
                           IESS_ESTADO,
                           SCORE,
                           CLIENTE_PEOR_SCORE,
                           TASA_MALOS,
                           SEGMENTO,
                           TIEMPO_DESDE_PRIMERCREDITO,
                           CUPO_TARJETA,
                           PRESENCIA_MORA,
                           PRESENCIA_CONSULTAS,
                           DEUDA_TOTAL,
                           MAXIMO_MONTO_OTORGADO,
                           TIEMPO_SIN_OPERACIONES,
                           CANT_OPE_SIN_MORA,
                           PRSENCIA_HIPOTECARIO,
                           VALOR_DEUDAS_VENCIDAS,
                           DIAS_ATRASO,
                           PRESENCIA_DEUDA_CASTIGADA_DEMANDA,
                           CANT_OPE_CON_MORA,
                           Valor_Deudas_Castigadas_Demanda_J
                            );
                    }
                }
                var estilo = DocumentFormatExtensions.GenerateStylesheet();
                var fileName = Path.GetTempFileName();

                var tformat = new CellFormat { NumberFormatId = 49, ApplyNumberFormat = true };
                estilo.CellFormats.Append(tformat);
                var tformatIndex = (uint)Array.IndexOf(estilo.CellFormats.ToArray(), tformat);

                var styleMap = new Dictionary<string, uint>();
                styleMap.Add("Header", 2);

                using (var stream = new FileStream(fileName, FileMode.Create))
                    dt.ToExcelStream(stream, null, null, estilo, styleMap);

                return File(System.IO.File.ReadAllBytes(fileName), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ReporteAlmespana_{DateTime.Now.Ticks}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message);
            }
        }

    }
}
