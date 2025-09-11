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
using System.Data;
using Web.Areas.Consultas.Models;
using Microsoft.AspNetCore.Identity;
using Dominio.Entidades.Identidad;
using Persistencia.Repositorios.Balance;
using Dominio.Entidades.Balances;
using Persistencia.Repositorios.Identidad;
using Dominio.Tipos;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers.API.Clientes.AMC
{
    [Route("api/Clientes/AMC/Consulta")]
    [ApiController]
    public class ConsultaController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<Usuario> _userManager;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detalleHistorial;
        private readonly IReportesConsolidados _reporteConsolidado;
        private readonly IUsuarios _usuarios;

        public ConsultaController(IConfiguration configuration, ILoggerFactory loggerFactory,
            UserManager<Usuario> userManager,
            IHistoriales historiales,
            IDetallesHistorial detallehistoriales,
            IReportesConsolidados reportesConsolidados,
            IUsuarios usuarios)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _userManager = userManager;
            _historiales = historiales;
            _detalleHistorial = detallehistoriales;
            _usuarios = usuarios;
            _reporteConsolidado = reportesConsolidados;
        }

        [HttpPost("RegistrarConsulta")]
        public async Task<IActionResult> RegistrarConsulta(ApiClienteAMCViewModel modelo)
        {
            try
            {
                _logger.LogInformation($"Registrando consulta API AMC {modelo.Identificacion}: {modelo.Referencia}...");
                modelo.Periodos = 1;
                var usuario = string.Empty;
                var clave = string.Empty;
                Usuario user = null;
                ReporteConsolidado historialConsolidado = null;

                try
                {
                    var request = HttpContext.Request;
                    var authHeader = request.Headers["Authorization"].ToString();
                    if (authHeader != null)
                    {
                        var authHeaderVal = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);
                        if (authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) && authHeaderVal.Parameter != null)
                        {
                            var encoding = System.Text.Encoding.GetEncoding("iso-8859-1");
                            var credentials = encoding.GetString(Convert.FromBase64String(authHeaderVal.Parameter));
                            var separator = credentials.IndexOf(':');
                            usuario = credentials.Substring(0, separator);
                            clave = credentials.Substring(separator + 1);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
                        throw new UnauthorizedAccessException();

                    user = await _usuarios.FirstOrDefaultAsync(m => m, m => m.UserName == usuario, null, m => m.Include(m => m.Empresa.PlanesEmpresas).Include(m => m.Empresa.PlanesBuroCredito).Include(m => m.Empresa.PlanesEvaluaciones));

                    if (user == null)
                        throw new Exception("El usuario no se encuentra registrado.");

                    if (user.Estado != Dominio.Tipos.EstadosUsuarios.Activo)
                        throw new Exception("Cuenta de usuario inactiva.");

                    var passwordVerification = new PasswordHasher<Usuario>().VerifyHashedPassword(user, user.PasswordHash, clave);
                    if (passwordVerification == PasswordVerificationResult.Failed)
                        throw new Exception("La clave ingresada es incorrecta");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    return Unauthorized();
                }

                var usuarioActual = user;
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión");

                if (usuarioActual.Empresa.Estado != EstadosEmpresas.Activo)
                    throw new Exception("La empresa asociada al usuario no está activa");

                if (modelo == null)
                    throw new Exception("No se han ingresado los datos de la consulta.");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("La identificación ingresada no es válida.");

                var identificacionOriginal = modelo.Identificacion?.Trim();
                var planesVigentes = usuarioActual.Empresa.PlanesEmpresas.Where(m => m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo).ToList();
                if (!planesVigentes.Any())
                    throw new Exception("No es posible realizar esta consulta ya que no tiene planes activos vigentes.");

                var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Identificacion == identificacionOriginal && m.IdUsuario == user.Id && m.Observacion == modelo.Referencia, null, i => i.Include(m => m.DetalleHistorial));
                if (historial != null)
                {
                    historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == historial.Id);
                    _logger.LogInformation($"Configurando historial existente consulta API AMC {modelo.Identificacion}: {modelo.Referencia}...");
                    var detalleHistorial = new List<DetalleHistorial>();
                    if (modelo.Data.ContainsKey(FuentesApiAmc.Sri))
                    {
                        var data = modelo.Data[FuentesApiAmc.Sri];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.Sri);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }

                        if (!string.IsNullOrEmpty(data) && (string.IsNullOrEmpty(historial.NombresPersona) || string.IsNullOrEmpty(historial.IdentificacionSecundaria)))
                        {
                            var r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(data);
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural)
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
                            }
                        }
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Iess))
                    {
                        var data = modelo.Data[FuentesApiAmc.Iess];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.Iess);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.LegalEmpresa))
                    {
                        var data = modelo.Data[FuentesApiAmc.LegalEmpresa];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.FJEmpresa);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.LegalRepresentante))
                    {
                        var data = modelo.Data[FuentesApiAmc.LegalRepresentante];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.FJudicial);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Civil))
                    {
                        var data = modelo.Data[FuentesApiAmc.Civil];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.Ciudadano);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }

                        if (!string.IsNullOrEmpty(data) && (string.IsNullOrEmpty(historial.NombresPersona) || string.IsNullOrEmpty(historial.IdentificacionSecundaria)))
                        {
                            var r_garancheck = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(data);
                            if (r_garancheck != null && !string.IsNullOrEmpty(r_garancheck.Identificacion?.Trim()) && !string.IsNullOrEmpty(r_garancheck.Nombres?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = r_garancheck.Nombres?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = r_garancheck.Identificacion?.Trim().ToUpper();
                            }
                        }
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Societario))
                    {
                        var data = modelo.Data[FuentesApiAmc.Societario];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.BalancesAmc);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.BuroAval))
                    {
                        var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                        if (planBuroCredito != null)
                            historial.IdPlanBuroCredito = planBuroCredito.Id;

                        historial.TipoFuenteBuro = FuentesBuro.Aval;

                        var data = modelo.Data[FuentesApiAmc.BuroAval];
                        var detalle = historial.DetalleHistorial.FirstOrDefault(m => m.TipoFuente == Fuentes.BuroCredito);
                        if (detalle != null && !string.IsNullOrEmpty(data))
                        {
                            detalle.Generado = true;
                            detalle.Data = data;
                            detalle.FechaRegistro = DateTime.Now;
                            await _detalleHistorial.UpdateAsync(detalle);
                        }
                    }
                    await _historiales.UpdateAsync(historial);
                    if (historialConsolidado != null)
                    {
                        historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                        historialConsolidado.NombrePersona = historial.NombresPersona;
                        await _reporteConsolidado.UpdateAsync(historialConsolidado);
                    }
                }
                else
                {
                    _logger.LogInformation($"Configurando historial nuevo consulta API AMC {modelo.Identificacion}: {modelo.Referencia}...");
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
                        idPlan = planEmpresaRucs.Id;
                    }
                    else
                        throw new Exception("La identificación ingresada no corresponde ni a cédulas ni a RUCs");

                    if (idPlan == 0)
                        throw new Exception("No es posible realizar esta consulta ya que no tiene planes vigentes.");

                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    _logger.LogInformation($"Procesando historial de usuario: {user.Id}. Identificación: {identificacionOriginal}. Periodo: {modelo.Periodos}. IP: {ip}.");

                    if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
                    {
                        parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { modelo.Periodos } });
                        if (modelo.Periodos == 1)
                        {
                            var infoPeriodos = _configuration.GetSection("AppSettings:PeriodosDinamicos").Get<PeriodosDinamicosViewModel>();
                            if (infoPeriodos != null)
                            {
                                var ultimosPeriodos = infoPeriodos.Periodos.Select(m => m.Valor).ToList();
                                parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = ultimosPeriodos });
                            }
                        }
                    }
                    else
                    {
                        parametros = JsonConvert.SerializeObject(new { Identificacion = identificacionOriginal, Periodos = new int[] { 0 } });
                        modelo.Periodos = 0;
                    }

                    var idPlanBuro = 0;
                    var detalleHistorial = new List<DetalleHistorial>();
                    historial = new Historial()
                    {
                        IdUsuario = user.Id,
                        DireccionIp = ip?.Trim().ToUpper(),
                        Identificacion = modelo.Identificacion?.Trim().ToUpper(),
                        Periodo = modelo.Periodos,
                        Fecha = DateTime.Now,
                        TipoConsulta = Consultas.Api,
                        ParametrosBusqueda = parametros,
                        IdPlanEmpresa = idPlan,
                        TipoIdentificacion = tipoIdentificacion,
                        Observacion = modelo.Referencia
                    };

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Sri))
                    {
                        var data = modelo.Data[FuentesApiAmc.Sri];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.Sri,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });

                        if (!string.IsNullOrEmpty(data))
                        {
                            var r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(data);
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural)
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
                            }
                        }
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.Sri,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Iess))
                    {
                        var data = modelo.Data[FuentesApiAmc.Iess];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.Iess,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.Iess,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.LegalEmpresa))
                    {
                        var data = modelo.Data[FuentesApiAmc.LegalEmpresa];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.FJEmpresa,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.FJEmpresa,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.LegalRepresentante))
                    {
                        var data = modelo.Data[FuentesApiAmc.LegalRepresentante];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.FJudicial,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.FJudicial,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Civil))
                    {
                        var data = modelo.Data[FuentesApiAmc.Civil];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.Ciudadano,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });

                        if (!string.IsNullOrEmpty(data))
                        {
                            var r_garancheck = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(data);
                            if (r_garancheck != null && !string.IsNullOrEmpty(r_garancheck.Identificacion?.Trim()) && !string.IsNullOrEmpty(r_garancheck.Nombres?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = r_garancheck.Nombres?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = r_garancheck.Identificacion?.Trim().ToUpper();
                            }
                        }
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.Ciudadano,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.CivilLinea))
                    {
                        var data = modelo.Data[FuentesApiAmc.CivilLinea];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.RegistroCivil,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });

                        if (!string.IsNullOrEmpty(data))
                        {
                            var registroCivilLinea = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(data);
                            if (registroCivilLinea != null && !string.IsNullOrEmpty(registroCivilLinea.Cedula?.Trim()) && !string.IsNullOrEmpty(registroCivilLinea.Nombre?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = registroCivilLinea.Nombre?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = registroCivilLinea.Cedula?.Trim().ToUpper();
                            }
                        }
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.RegistroCivil,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.Societario))
                    {
                        var data = modelo.Data[FuentesApiAmc.Societario];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.BalancesAmc,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.BalancesAmc,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (modelo.Data.ContainsKey(FuentesApiAmc.BuroAval))
                    {
                        var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                        if (planBuroCredito != null)
                            idPlanBuro = planBuroCredito.Id;

                        historial.TipoFuenteBuro = FuentesBuro.Aval;
                        var data = modelo.Data[FuentesApiAmc.BuroAval];
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.BuroCredito,
                            Generado = !string.IsNullOrEmpty(data),
                            Data = data,
                            FechaRegistro = DateTime.Now,
                        });
                    }
                    else
                    {
                        detalleHistorial.Add(new DetalleHistorial()
                        {
                            TipoFuente = Fuentes.BuroCredito,
                            Generado = false,
                            FechaRegistro = DateTime.Now,
                        });
                    }

                    if (idPlanBuro > 0)
                        historial.IdPlanBuroCredito = idPlanBuro;
                    historial.DetalleHistorial = detalleHistorial;
                    await _historiales.GuardarHistorialAsync(historial);
                    if (historialConsolidado != null)
                    {
                        historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                        historialConsolidado.NombrePersona = historial.NombresPersona;
                        await _reporteConsolidado.UpdateAsync(historialConsolidado);
                    }
                }

                _logger.LogInformation($"Registro exitoso de consulta API AMC {modelo.Identificacion}: {modelo.Referencia}.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message);
            }
        }
    }
}
