// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Dominio.Entidades.Identidad;
using Infraestructura.Servicios;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Persistencia;
using Persistencia.Repositorios.Balance;
using Web.Models;

namespace Web.Controllers.API
{
    [Route("api/ConsultaResumen")]
    [ApiController]
    public class ConsultaResumenController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailSender;
        private readonly IHistoriales _historiales;

        public ConsultaResumenController(IConfiguration configuration, ILoggerFactory loggerFactory, IEmailService emailSender, IHistoriales historiales)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _emailSender = emailSender;
            _historiales = historiales;
        }

        [HttpGet("ObtenerInformacionResumen")]
        public async Task ObtenerInformacionResumen()
        {
            try
            {
                _logger.LogInformation("Inicio de proceso de envío de correos de resumen de consultas");
                var horaCorreoResumen = DateTime.Now.Hour;
#if DEBUG
                horaCorreoResumen = 8;
#endif
                if (horaCorreoResumen != _configuration.GetSection("AppSettings:HoraEnvioCorreosResumen").Get<int>())
                    throw new Exception("No es posible procesar el servicio ya que no cumple con la hora programada");

                var resumenEmpresas = new List<ConsultaResumenViewModel>();
                var tablaEmpresa = string.Empty;
                using (var _contexto = new SqlConnection(_configuration.GetConnectionString("ContextoPrincipal")))
                {
                    try
                    {
                        _logger.LogInformation("Conexión a la base de datos");
                        if (_contexto.State != ConnectionState.Open) await _contexto.OpenAsync();

                        var fechaInicio = DateTime.Now.Day == 1 ? DateTime.Now.Date.AddMonths(-1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        var fechaFin = DateTime.Now.Date;
                        using (var comand = _contexto.CreateCommand())
                        {
                            _logger.LogInformation($"Ejecucion del ObtenerReporteResumenConsulta con: {fechaInicio.ToString("dd-MM-yyyy")} - {fechaFin.ToString("dd-MM-yyyy")}");
                            comand.CommandType = CommandType.StoredProcedure;
                            comand.CommandText = "dbo.ObtenerReporteResumenConsulta";

                            var parameterFechaInicio = comand.CreateParameter();
                            parameterFechaInicio.ParameterName = "fechaInicio";
                            parameterFechaInicio.Value = fechaInicio;
                            parameterFechaInicio.DbType = DbType.Date;
                            comand.Parameters.Add(parameterFechaInicio);

                            var parameterFechaFin = comand.CreateParameter();
                            parameterFechaFin.ParameterName = "fechaFin";
                            parameterFechaFin.Value = fechaFin;
                            parameterFechaFin.DbType = DbType.Date;
                            comand.Parameters.Add(parameterFechaFin);

                            var data = new DataSet();
                            var dtEmpresa = new DataTable("Empresa");
                            data.Tables.Add(dtEmpresa);

                            using (var dr = await comand.ExecuteReaderAsync())
                            {
                                _logger.LogInformation("Obtener datos del procedimiento almacenado");
                                data.Load(dr, LoadOption.OverwriteChanges, dtEmpresa);
                                var empresaResumen = (from item in data.Tables[0].Rows.OfType<DataRow>()
                                                      select new
                                                      {
                                                          RazonSocial = item["RazonSocial"].ToString(),
                                                          ConsultasRealizadas = !string.IsNullOrEmpty(item["ConsultasRealizadas"].ToString()) && int.TryParse(item["ConsultasRealizadas"].ToString(), out _) ? int.Parse(item["ConsultasRealizadas"].ToString()) : 0,
                                                          ConsultasRealizadasBuro = !string.IsNullOrEmpty(item["ConsultasRealizadasBuro"].ToString()) && int.TryParse(item["ConsultasRealizadasBuro"].ToString(), out _) ? int.Parse(item["ConsultasRealizadasBuro"].ToString()) : 0,
                                                          TotalConsultas = !string.IsNullOrEmpty(item["TotalConsultas"].ToString()) && int.TryParse(item["TotalConsultas"].ToString(), out _) ? int.Parse(item["TotalConsultas"].ToString()) : 0,
                                                          SaldoConsultasMes = !string.IsNullOrEmpty(item["SaldoConsultasMes"].ToString()) && int.TryParse(item["SaldoConsultasMes"].ToString(), out _) ? int.Parse(item["SaldoConsultasMes"].ToString()) : 0,
                                                          ConsultasRealizadasDia = !string.IsNullOrEmpty(item["ConsultasRealizadasDia"].ToString()) && int.TryParse(item["ConsultasRealizadasDia"].ToString(), out _) ? int.Parse(item["ConsultasRealizadasDia"].ToString()) : 0,
                                                      }).ToList();

                                if (empresaResumen.Any())
                                {
                                    resumenEmpresas = empresaResumen.Select(x => new ConsultaResumenViewModel
                                    {
                                        RazonSocial = x.RazonSocial,
                                        ConsultasRealizadas = x.ConsultasRealizadas,
                                        ConsultasRealizadasBuro = x.ConsultasRealizadasBuro,
                                        TotalConsultas = x.TotalConsultas,
                                        SaldoConsultasMes = x.SaldoConsultasMes,
                                        ConsultasRealizadasDia = x.ConsultasRealizadasDia
                                    }).ToList();
                                }
                            }
                        }
                        if (_contexto.State != ConnectionState.Closed) _contexto.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    finally { if (_contexto.State != ConnectionState.Closed) _contexto.Close(); }
                }

                _logger.LogInformation("Generando la tabla de datos para el correo electrónico");
                tablaEmpresa = GenerarContenidoMail(resumenEmpresas);
                _logger.LogInformation("Finalización del proceso de generación de datos para el correo electrónico");

                var fechaFinBusqueda = DateTime.Now.Date.AddDays(-1);
                var historialTemp = await _historiales.ReadAsync(x => new { x.IdPlanEmpresa, x.IdPlanBuroCredito }, x => x.Fecha.Date == fechaFinBusqueda.Date, null, null, 0, null, true);

                #region Mail
                try
                {
                    _logger.LogInformation("Iniciando plantillas para el correo electrónico");
                    var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.ConsultasDiarias);
                    if (string.IsNullOrEmpty(template))
                        throw new Exception($"No se ha cargado la plantilla de tipo: {Dominio.Tipos.TemplatesCorreo.ConsultasDiarias}");

                    _logger.LogInformation("Generando información para el correo electrónico");
                    var asunto = "Resumen de Consultas Plataforma Integral de Información 360°";
                    var replacements = new Dictionary<string, object>
                                    {
                                        {"{INFORMACIONEMPRESA}", tablaEmpresa  },
                                        {"{CONSULTASREALIZADAS}", historialTemp.Count(x => x.IdPlanEmpresa > 0) },
                                        {"{CONSULTASREALIZADASBURO}", historialTemp.Count(x => x.IdPlanBuroCredito.HasValue && x.IdPlanBuroCredito.Value > 0) },
                                        {"{FECHABUSQUEDA}", fechaFinBusqueda.Date.ToString("D", CultureInfo.CreateSpecificCulture("es")) },
                                        {"{MESBUSQUEDA}", fechaFinBusqueda.Date.ToString("Y", CultureInfo.CreateSpecificCulture("es")) },
                                    };
                    _logger.LogInformation("Finalización de la información para el correo electrónico");

                    var correosResumen = _configuration.GetSection("AppSettings:CorreosResumenConsultas").Get<List<CorreosViewModel>>();
                    foreach (var item in correosResumen)
                    {
                        _logger.LogInformation($"Correo enviado para el usuario: {item.Correo}");
                        await _emailSender.SendEmailAsync(item.Correo, asunto, template, item.Nombre.ToUpper(), replacements, null, string.Empty, true, false, "reporteResumen", false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                #endregion Mail
                _logger.LogInformation("Fin de proceso de envío de correos de resumen de consultas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private string GenerarContenidoMail(List<ConsultaResumenViewModel> empresaLogs)
        {
            if (!empresaLogs.Any())
                return string.Empty;

            var body = string.Empty;
            var cabeceraAdmin = "<div style='max-height: 210px; overflow-y: scroll'><table style='width:100%;border-color:#a2a5ad; border: #a2a5ad;' border='1'>" +
                                "<thead bgcolor='#28487e' style='color:white;'>" +
                                "<tr style='text-align:center'>" +
                                "<th width='45%'>Cliente</th>" +
                                "<th width='15%'>Número de Consultas en el Día</th>" +
                                "<th width='10%'>Consultas Realizadas</th>" +
                                "<th width='10%'>Consultas Realizadas Buró</th>" +
                                "<th width='10%'>Plan Consultas Mes</th>" +
                                "<th width='10%'>Saldo Consultas Mes</th>" +
                                "</tr>" +
                                "</thead>";

            foreach (var item in empresaLogs)
            {
                body += "<tr>" +
                        $"<td>{item.RazonSocial}</td>" +
                        $"<td>{item.ConsultasRealizadasDia}</td>" +
                        $"<td>{item.ConsultasRealizadas}</td>" +
                        $"<td>{item.ConsultasRealizadasBuro}</td>" +
                        $"<td>{(item.TotalConsultas == int.MaxValue ? "MAX" : item.TotalConsultas)}</td>" +
                        $"<td>{(item.TotalConsultas == int.MaxValue ? "N/A" : item.SaldoConsultasMes)}</td>" +
                        "</tr>";
            }

            body += "</tbody>";
            return $"{cabeceraAdmin}{body}</table></div>";
        }
    }
}
