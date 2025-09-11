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
using Microsoft.AspNetCore.Http.Extensions;
using System.Data.SqlClient;

namespace Web.Controllers.API
{
    [Route("api/ConsultaGarancheck340943")]
    [ApiController]
    public class ConsultaGarancheck340943Controller : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly Externos.Logica.Garancheck.Controlador _garancheck;


        public ConsultaGarancheck340943Controller(IConfiguration configuration, ILoggerFactory loggerFactory, Externos.Logica.Garancheck.Controlador garancheck)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _garancheck = garancheck;

        }

        [HttpPost("ObtenerInformacion")]
        public async Task<IActionResult> ObtenerInformacion(ApiViewModel modelo)
        {
            try
            {

                var usuarioActual = modelo.Identificacion;
                if (string.IsNullOrWhiteSpace(usuarioActual))
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.Desconocido,
                        mensaje = "Exito."
                    });
                var r_sujeto = await GetCoincidenciasNombresPruebaAsync(usuarioActual);

                if (r_sujeto == null)
                    throw new Exception("No se ha generado elementos en la búsqueda.");
                else
                {
                    if (r_sujeto.SujetosIdentificaciones != null && r_sujeto.SujetosIdentificaciones.Any())
                    {
                        return Json(r_sujeto.SujetosIdentificaciones, new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                    }
                    else
                        throw new Exception("No se ha generado elementos en la búsqueda.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    codigoError = (short)Dominio.Tipos.ErroresApi.ProcesamientoConsulta,
                    mensaje = ex.Message
                });
            }
        }

        private async Task<Sujeto> GetCoincidenciasNombresPruebaAsync(string nombre)
        {
            try
            {
                _logger.LogInformation($"[COINCIDENCIA NOMBRES] Inicio metodo con datos: {nombre}");
                if (string.IsNullOrEmpty(nombre))
                    return null;

                if (nombre.Length <= 2)
                    return null;

                _logger.LogInformation($"[COINCIDENCIA NOMBRES] Obtener cadena de conexion.");
                var connectionString = _configuration["AppSettings:Fuentes:0:Almacen:Conexiones:General"];
                SqlConnection Conexion = null;
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    if (Conexion == null)
                        Conexion = new SqlConnection(connectionString);

                    if (Conexion.State != System.Data.ConnectionState.Open)
                    {
                        Conexion.Open();
                        _logger.LogInformation($"CONEXION OPEN");
                    }
                    else
                        _logger.LogInformation($"CONEXION ABIERTA");

                    _logger.LogInformation($"[COINCIDENCIA NOMBRES] Crear comando en la conexion.");
                    using (var command = Conexion.CreateCommand())
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.GiradorCedulaPorNombreObtenerV3";
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@Nombres";
                        parameter.Value = nombre;
                        parameter.DbType = System.Data.DbType.AnsiString;

                        command.Parameters.Add(parameter);
                        var data = new DataSet();
                        var dtSujeto = new DataTable("MaestroSujetos");
                        data.Tables.Add(dtSujeto);
                        _logger.LogInformation($"[COINCIDENCIA NOMBRES] Ejecutar comando.");
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            data.Load(reader, LoadOption.OverwriteChanges, dtSujeto);
                            _logger.LogInformation($"[COINCIDENCIA NOMBRES] Seleccionar consulta.");
                            var sujetoIdentificacion = (from item in data.Tables[0].Rows.OfType<DataRow>()
                                                        select new
                                                        {
                                                            Cedula = item["numeroDocumento"].ToString(),
                                                            Nombre = item["nombre"].ToString(),
                                                        }).ToList();
                            _logger.LogInformation($"[COINCIDENCIA NOMBRES] Resultados obtenidos.");
                            if (sujetoIdentificacion.Any())
                            {
                                var resultado = new Sujeto();
                                resultado.SujetosIdentificaciones = sujetoIdentificacion.Select(m => new DatoSujeto { Cedula = m.Cedula, Nombre = m.Nombre }).ToList();
                                return resultado;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }


        [HttpPost("ConsultaNombresBanderas")]
        public async Task<IActionResult> ConsultaNombresBanderas(ApiViewModel modelo)
        {
            try
            {

                var r_sujeto = await _garancheck.GetCoincidenciasNombresPruebaAsync(modelo.Identificacion);
                return Json(r_sujeto, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    codigoError = (short)Dominio.Tipos.ErroresApi.ProcesamientoConsulta,
                    mensaje = ex.Message
                });
            }
        }

    }
}
