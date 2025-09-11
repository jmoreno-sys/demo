// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Externos.Logica.Balances.Modelos;
using Externos.Logica;
using Externos.Logica.FJudicial.Modelos;
using Externos.Logica.FJudicial.Resultados;
using Externos.Logica.Modelos;
using Externos.Logica.PredioMunicipio.Modelos;
using Externos.Logica.PredioMunicipio.Resultados;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace Infraestructura.Servicios
{
    public interface IConsultaService
    {
        Task<Externos.Logica.Senescyt.Modelos.Persona> ObtenerSenescyt(string identificacion);
        Task<ServicioSenescytNuevoViewModel> ObtenerSenescytConsultaExterna(string identificacion, string server);
        Task<byte[]> ObtenerReportePdf(string html);
        Task<PrediosCuenca> ObtenerPrediosCuencaExternos(string identificacion);
        Task<Externos.Logica.FJudicial.Modelos.Persona> GetRespuestaFJAsync(string modelo, params object[] argumentos);
    }

    public class ConsultaService : IConsultaService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly Encoding _encoding;

        public ConsultaService(ILoggerFactory logger, IConfiguration configuration)
        {
            _logger = logger.CreateLogger(GetType());
            _configuration = configuration;
            _encoding = Encoding.GetEncoding("ISO-8859-1");
        }

        public async Task<Externos.Logica.Senescyt.Modelos.Persona> ObtenerSenescyt(string identificacion)
        {
            try
            {
                var credenciales = _configuration.GetSection("ServicioDNF").Get<CredencialesViewModel>();
                if (credenciales == null)
                    throw new Exception("No se han defido las credenciales para el servicio");

                var rest_client = new RestClient(credenciales.Url)
                {
                    Authenticator = new HttpBasicAuthenticator(credenciales.UserName, credenciales.Password)
                };
                var dnf_request = new RestRequest((Method)Enum.Parse<Method>(credenciales.Method));
                dnf_request.AddParameter("Identificacion", identificacion);
                dnf_request.AddParameter("Modulos", $"Senescyt");
                _logger.LogInformation($"Consultando Senescyt de {identificacion} en DNF...");
                var dnf_response = await rest_client.ExecuteAsync(dnf_request);
                if (dnf_response != null && dnf_response.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(dnf_response.Content))
                {
                    _logger.LogInformation($"Consulta Senescyt de {identificacion} en DNF finalizada correctamente.");
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ServicioSenescytViewModel>(dnf_response.Content);
                    if (data != null && data.Senescyt != null)
                        return data.Senescyt;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }

        public async Task<ServicioSenescytNuevoViewModel> ObtenerSenescytConsultaExterna(string identificacion, string server = null)
        {
            try
            {
                var servidor = !string.IsNullOrWhiteSpace(server) ? server : "https://petroliaecuador.ec/ConsultasExternas/";
                var datos_client = new RestClient($"{servidor}api/Fuentes/GetSenescytNuevo");
                var datos_request = new RestRequest(Method.POST);
                datos_request.AddHeader("Content-Type", "application/json");
                datos_request.AddHeader("Username", "1792957702001$U$3r_SnT");
                datos_request.AddHeader("Password", "c79410c4-c148-47f2-b385-d13b51def9c7");
                datos_request.AddJsonBody(new
                {
                    Identificacion = identificacion,
                });
                _logger.LogInformation($"Consultando Senescyt de {identificacion} en Consultas Externas...");

                var datosResponse = await datos_client.ExecuteAsync(datos_request);
                if (datosResponse != null && datosResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(datosResponse.Content))
                {
                    _logger.LogInformation($"Consulta Senescyt de {identificacion} en Consultas Externas finalizada correctamente.");
                    var contenido = Newtonsoft.Json.JsonConvert.DeserializeObject<ServicioSenescytNuevoViewModel>(datosResponse.Content);
                    if (contenido != null)
                        return contenido;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }

        public async Task<byte[]> ObtenerReportePdf(string html)
        {
            if (string.IsNullOrEmpty(html?.Trim()))
                return null;

            try
            {
                var datos_client = new RestClient($"https://petroliaecuador.ec/ConsultasExternas/api/Utilidades/GetReportePdf2");
                var datos_request = new RestRequest(Method.POST);
                datos_request.AddHeader("Content-Type", "application/json");
                datos_request.AddHeader("Username", "1792957702001$U$3r_Ut1");
                datos_request.AddHeader("Password", "5566104c-4765-448c-baf7-86aedcf3d1bb");
                datos_request.AddJsonBody(new
                {
                    Contenido = html,
                });
                _logger.LogInformation($"Generando reporte pdf en Consultas Externas...");

                var datosResponse = await datos_client.ExecuteAsync(datos_request);
                if (datosResponse != null && datosResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    return datosResponse.RawBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }
        private readonly string HostCuenca = "https://enlinea.cuenca.gob.ec/BackendConsultas/";
        private readonly int Espera = 120000;
        public async Task<PrediosCuenca> ObtenerPrediosCuencaExternos(string identificacion)
        {
            try
            {
                var resultado = new PrediosCuenca();
                if (string.IsNullOrEmpty(identificacion?.Trim()))
                    throw new Exception("La identificación es obligatoria");

                //var servidor = !string.IsNullOrWhiteSpace("https://petroliaecuador.ec/ConsultasExternas/");
                var servidor = "http://52.254.73.170/ConsultasExternas/";
                var datos_client = new RestClient($"{servidor}api/Fuentes/GetPrediosCuenca");
                var datos_request = new RestRequest(Method.POST);
                datos_request.AddHeader("Content-Type", "application/json");
                datos_request.AddHeader("Username", "1792957702001$U$3r_IP");
                datos_request.AddHeader("Password", "db7b2457-595d-47a2-9955-7b414070e0bc");
                datos_request.AddJsonBody(new
                {
                    Identificacion = identificacion,
                });
                _logger.LogInformation($"Consultando Predios Cuenca de {identificacion} en Consultas Externas...");

                var datosResponse = await datos_client.ExecuteAsync(datos_request);
                if (datosResponse != null && datosResponse.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(datosResponse.Content))
                {
                    _logger.LogInformation($"Consulta Predios Cuenca de {identificacion} en Consultas Externas finalizada correctamente.");
                    var contenido = Newtonsoft.Json.JsonConvert.DeserializeObject<PrediosCuenca>(datosResponse.Content);
                    if (contenido != null)
                        return contenido;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, ex.Message);
            }
            return null;
        }

        private async Task<Externos.Logica.FJudicial.Modelos.Persona> GetFuncionHJudicialHttpClientAsync(string identificacion, string nombres)
        {
            var resultado = new Externos.Logica.FJudicial.Modelos.Persona();
            try
            {
                var respuestaFJ = new _Respuesta();

                #region Demandado

                var clientDemandado = new HttpClient();
                var requestDemandado = new HttpRequestMessage(HttpMethod.Post, "https://api.funcionjudicial.gob.ec/EXPEL-CONSULTA-CAUSAS-SERVICE/api/consulta-causas/informacion/buscarCausas");
                requestDemandado.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36 OPR/98.0.0.0");
                var causaRequestDemandado = new _Parametros
                {
                    Actor = new _Actor
                    {
                        CedulaActor = "",
                        NombreActor = ""
                    },
                    Demandado = new _Demandado
                    {
                        CedulaDemandado = !string.IsNullOrEmpty(nombres) ? string.Empty : identificacion,
                        NombreDemandado = RemoverCaracteresEspeciales(nombres)
                    },
                    Recaptcha = "verdad",
                };

                string jsonContentDemandado = JsonConvert.SerializeObject(causaRequestDemandado);

                var contentDemandado = new StringContent(jsonContentDemandado, System.Text.Encoding.UTF8, "application/json");
                requestDemandado.Content = contentDemandado;

                var responseDemandado = await clientDemandado.SendAsync(requestDemandado);
                if (responseDemandado.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrEmpty(await responseDemandado.Content.ReadAsStringAsync()))
                        respuestaFJ.Demandado = JsonConvert.DeserializeObject<List<_Proceso>>(await responseDemandado.Content.ReadAsStringAsync());
                    else
                        _logger.LogInformation($"❌ Error: consulta vacía");
                }
                else
                {
                    var error = await responseDemandado.Content.ReadAsStringAsync();
                    _logger.LogInformation($"❌ Error: {(int)responseDemandado.StatusCode} - {responseDemandado.ReasonPhrase}");
                    _logger.LogInformation("Contenido del error:");
                    _logger.LogInformation(error);
                }

                #endregion Demandado

                if (respuestaFJ.Actor.Any())
                    resultado.Actor = respuestaFJ.Actor.Select(m => new Proceso()
                    {
                        Numero = m.Id,
                        Fecha = !string.IsNullOrWhiteSpace(m.FechaIngreso) ? DateTime.ParseExact(m.FechaIngreso, "yyyy-MM-ddTHH:mm:ss.FFF+00:00", CultureInfo.InvariantCulture) : default,
                        Codigo = m.IdJuicio,
                        Descripcion = m.NombreDelito.Trim()
                    }).ToDictionary(k => k.Numero, v => v);

                if (respuestaFJ.Demandado.Any())
                    resultado.Demandado = respuestaFJ.Demandado.Select(m => new Proceso()
                    {
                        Numero = m.Id,
                        Fecha = !string.IsNullOrWhiteSpace(m.FechaIngreso) ? DateTime.ParseExact(m.FechaIngreso, "yyyy-MM-ddTHH:mm:ss.FFF+00:00", CultureInfo.InvariantCulture) : default,
                        Codigo = m.IdJuicio,
                        Descripcion = m.NombreDelito.Trim()
                    }).ToDictionary(k => k.Numero, v => v);

                if (resultado.TotalProcesos == 0)
                    resultado = null;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                resultado = null;
            }
            return resultado;
        }

        public async Task<Externos.Logica.FJudicial.Modelos.Persona> GetRespuestaFJAsync(string modelo, params object[] argumentos)
        {
            if (!Validacion.Validar(modelo))
                return null;

            var cedula = string.Empty;
            var ruc = string.Empty;
            var pasaporte = string.Empty;
            var nombreCompleto = string.Empty;
            if (Validacion.Validar(modelo))
            {
                if (Validacion.Natural(modelo))
                {
                    cedula = modelo.Substring(0, modelo.Length - 3);
                    modelo = cedula;
                }
                else if (Validacion.RUC(modelo))
                    ruc = modelo;
                else if (Validacion.Cedula(modelo))
                    cedula = modelo;
                else if (Validacion.Pasaporte(modelo))
                    pasaporte = modelo;
                else
                    nombreCompleto = modelo;
            }

            Externos.Logica.FJudicial.Modelos.Persona resultado = null;

            if (Espera > 0)
            {
                try
                {
                    switch (3)
                    {
                        //case 1:
                        //    resultado = GetFuncionJudicial(modelo, nombreCompleto);
                        //    break;
                        //case 2:
                        //    resultado = GetFuncionJudicialLegacy(modelo, nombreCompleto, cedula, pasaporte, ruc);
                        //    break;
                        case 3:
                            resultado = await GetFuncionHJudicialHttpClientAsync(modelo, nombreCompleto);
                            break;
                            //default:
                            //    resultado = GetFuncionHJudicialHttpClientAsync(modelo, nombreCompleto);
                            //    break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, ex.Message);
                }
            }
            return resultado;
        }

        private string RemoverCaracteresEspeciales(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var chars1 = "áéíóúàèìòùäëïöüâêîôûñÑÂÊÎÔÛÁÉÍÓÚÀÈÌÒÙÄËÏÖÜ".ToArray();
            var chars2 = "aeiouaeiouaeiouaeiounNAEIOUAEIOUAEIOUAEIOU".ToArray();
            for (int i = 0; i < chars1.Length; i++)
                str = str.Replace(chars1[i], chars2[i]);

            return Regex.Replace(str, "[^a-zA-Z ]+", "");
        }


    }
}
