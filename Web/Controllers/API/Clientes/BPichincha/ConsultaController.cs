//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using Web.Models;
//using Microsoft.Extensions.Configuration;
//using System.Collections.Generic;
//using System.Linq;
//using Newtonsoft.Json;
//using System.IO;
//using System.Data;
//using Newtonsoft.Json.Serialization;
//using System.Text.RegularExpressions;
//using Microsoft.AspNetCore.Identity;
//using Dominio.Entidades.Identidad;
//using Persistencia.Repositorios.Balance;
//using Dominio.Entidades.Balances;
//using Persistencia.Repositorios.Identidad;
//using Dominio.Tipos;
//using System.Threading;
//using Persistencia.Migraciones.Principal;
//using Externos.Logica.IESS.Modelos;
//using Externos.Logica.PensionesAlimenticias.Modelos;

//namespace Web.Controllers.API.Clientes.BPichincha
//{
//    [Route("api/Clientes/BPichincha")]
//    [ApiController]
//    public class ConsultaController : Controller
//    {
//        private readonly ILogger _logger;
//        private readonly IConfiguration _configuration;
//        private readonly Externos.Logica.SRi.Controlador _sri;
//        private readonly Externos.Logica.Balances.Controlador _balances;
//        private readonly Externos.Logica.IESS.Controlador _iess;
//        private readonly Externos.Logica.ANT.Controlador _ant;
//        private readonly Externos.Logica.PensionesAlimenticias.Controlador _pension;
//        private readonly Externos.Logica.PredioMunicipio.Controlador _predios;
//        private readonly UserManager<Usuario> _userManager;
//        private readonly IHistoriales _historiales;
//        private readonly IDetallesHistorial _detallesHistorial;
//        private readonly IUsuarios _usuarios;
//        private readonly IReportesConsolidados _reporteConsolidado;
//        private readonly bool _cache = false;

//        public ConsultaController(IConfiguration configuration,
//            ILoggerFactory loggerFactory,
//            Externos.Logica.SRi.Controlador sri,
//            Externos.Logica.Balances.Controlador balances,
//            Externos.Logica.IESS.Controlador iess,
//            Externos.Logica.ANT.Controlador ant,
//            Externos.Logica.PensionesAlimenticias.Controlador pension,
//            Externos.Logica.PredioMunicipio.Controlador predios,
//            UserManager<Usuario> userManager,
//            IHistoriales historiales,
//            IDetallesHistorial detallehistoriales,
//            IReportesConsolidados reportesConsolidados,
//            IUsuarios usuarios)
//        {
//            _logger = loggerFactory.CreateLogger(GetType());
//            _configuration = configuration;
//            _sri = sri;
//            _balances = balances;
//            _iess = iess;
//            _ant = ant;
//            _pension = pension;
//            _predios = predios;
//            _userManager = userManager;
//            _historiales = historiales;
//            _detallesHistorial = detallehistoriales;
//            _usuarios = usuarios;
//            _reporteConsolidado = reportesConsolidados;
//            _cache = _configuration.GetSection("AppSettings:Consultas:Cache").Get<bool>();
//        }

//        [HttpGet("ObtenerInformacion/{identificacion}")]
//        public async Task<IActionResult> Consultar(string identificacion)
//        {
//            try
//            {
//                var modelo = new ApiViewModel_1790010937001();
//                modelo.Identificacion = identificacion;

//                var usuario = string.Empty;
//                var clave = string.Empty;
//                Usuario user = null;

//                var apiSri = new SriApiViewModel();
//                var apiIess = new IessApiMetodoViewModel();
//                var apiAnt = new AntApiViewModel();
//                var apiPension = new PensionAlimenticiaApiViewModel();
//                var apiPrediosQuito = new PrediosApiViewModel();
//                var apiPrediosCuenca = new PrediosCuencaApiViewModel();
//                var apiPrediosStoDomingo = new PrediosSantoDomingoApiViewModel();
//                var apiPrediosRuminahui = new PrediosRuminahuiApiViewModel();
//                var apiPrediosQuininde = new PrediosQuinindeApiViewModel();

//                try
//                {
//                    var request = HttpContext.Request;
//                    var authHeader = request.Headers["Authorization"].ToString();
//                    if (authHeader != null)
//                    {
//                        var authHeaderVal = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);
//                        if (authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) && authHeaderVal.Parameter != null)
//                        {
//                            var encoding = System.Text.Encoding.GetEncoding("iso-8859-1");
//                            var credentials = encoding.GetString(Convert.FromBase64String(authHeaderVal.Parameter));
//                            var separator = credentials.IndexOf(':');
//                            usuario = credentials.Substring(0, separator);
//                            clave = credentials.Substring(separator + 1);
//                        }
//                    }

//                    if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
//                        return BadRequest(new
//                        {
//                            codigo = (short)Dominio.Tipos.ErroresApi.CredencialesFaltantes,
//                            mensaje = "No se han ingresado las credenciales"
//                        });

//                    user = await _userManager.FindByNameAsync(usuario);
//                    if (user == null)
//                        return BadRequest(new
//                        {
//                            codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
//                            mensaje = "El usuario no se encuentra registrado."
//                        });

//                    if (user.Estado != Dominio.Tipos.EstadosUsuarios.Activo)
//                        return BadRequest(new
//                        {
//                            codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
//                            mensaje = "Cuenta de usuario inactiva."
//                        });

//                    var passwordVerification = new PasswordHasher<Usuario>().VerifyHashedPassword(user, user.PasswordHash, clave);
//                    if (passwordVerification == PasswordVerificationResult.Failed)
//                        return BadRequest(new
//                        {
//                            codigo = (short)Dominio.Tipos.ErroresApi.CredencialesIncorrectas,
//                            mensaje = "Credenciales incorrectas"
//                        });
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                    return Unauthorized(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.CredencialesIncorrectas,
//                        mensaje = ex.Message
//                    });
//                }

//                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(user.Id);
//                if (usuarioActual == null)
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
//                        mensaje = "Cuenta de usuario inactiva."
//                    });

//                if (usuarioActual.Empresa.Estado != EstadosEmpresas.Activo)
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
//                        mensaje = "La empresa asociada al usuario no está activa"
//                    });

//#if !DEBUG
//                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790010937001)
//                    return Unauthorized();
//#endif

//                if (modelo == null)
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.ParametrosFaltantes,
//                        mensaje = "No se han ingresado los datos de la consulta."
//                    });

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.IdentificacionInvalida,
//                        mensaje = "La identificación ingresada no es válida."
//                    });

//                var identificacionOriginal = modelo.Identificacion?.Trim();
//                var planesVigentes = usuarioActual.Empresa.PlanesEmpresas.Where(m => m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo).ToList();
//                if (!planesVigentes.Any())
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                        mensaje = "No es posible realizar esta consulta ya que no tiene planes activos vigentes."
//                    });

//                var idPlan = 0;
//                var tipoIdentificacion = string.Empty;
//                if (ValidacionViewModel.ValidarCedula(identificacionOriginal))
//                {
//                    tipoIdentificacion = Dominio.Constantes.General.Cedula;
//                    var planEmpresaCedula = usuarioActual.Empresa.PlanesEmpresas.FirstOrDefault(m => (m.NumeroConsultasCedula > 0 || (m.NumeroConsultas.HasValue && m.NumeroConsultas.Value > 0)) && m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo);
//                    if (planEmpresaCedula == null)
//                        return BadRequest(new
//                        {
//                            codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                            mensaje = "No es posible realizar esta consulta ya que no tiene un plan activo para cédulas."
//                        });

//                    if (planEmpresaCedula.BloquearConsultas)
//                    {
//                        if (planEmpresaCedula.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Separado)
//                        {
//                            var fechaActual = DateTime.Today;
//                            var historialCedulas = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaCedula.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year && m.TipoIdentificacion == Dominio.Constantes.General.Cedula);
//                            if (historialCedulas >= planEmpresaCedula.NumeroConsultasCedula)
//                                return BadRequest(new
//                                {
//                                    codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                    mensaje = $"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas para cédulas ({planEmpresaCedula.NumeroConsultasCedula}) en su plan."
//                                });
//                        }
//                        else if (planEmpresaCedula.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado)
//                        {
//                            if (planEmpresaCedula.NumeroConsultas.HasValue && planEmpresaCedula.NumeroConsultas.Value > 0)
//                            {
//                                var fechaActual = DateTime.Today;
//                                var historialUnificado = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaCedula.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year);
//                                if (historialUnificado >= planEmpresaCedula.NumeroConsultas)
//                                    return BadRequest(new
//                                    {
//                                        codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                        mensaje = $"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas ({planEmpresaCedula.NumeroConsultas}) en su plan."
//                                    });
//                            }
//                            else
//                                return BadRequest(new
//                                {
//                                    codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                    mensaje = "El plan contratado no tiene definido un número de consultas"
//                                });
//                        }
//                        else
//                            return BadRequest(new
//                            {
//                                codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                mensaje = "El plan contratado no tiene definido un número de consultas"
//                            });
//                    }
//                    idPlan = planEmpresaCedula.Id;
//                }
//                else if (ValidacionViewModel.ValidarRuc(identificacionOriginal) || ValidacionViewModel.ValidarRucJuridico(identificacionOriginal) || ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
//                {
//                    if (ValidacionViewModel.ValidarRuc(identificacionOriginal))
//                        tipoIdentificacion = Dominio.Constantes.General.RucNatural;

//                    if (ValidacionViewModel.ValidarRucJuridico(identificacionOriginal))
//                        tipoIdentificacion = Dominio.Constantes.General.RucJuridico;

//                    if (ValidacionViewModel.ValidarRucSectorPublico(identificacionOriginal))
//                        tipoIdentificacion = Dominio.Constantes.General.SectorPublico;

//                    var planEmpresaRucs = usuarioActual.Empresa.PlanesEmpresas.FirstOrDefault(m => (m.NumeroConsultasRuc > 0 || (m.NumeroConsultas.HasValue && m.NumeroConsultas.Value > 0)) && m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo);
//                    if (planEmpresaRucs == null)
//                        return BadRequest(new
//                        {
//                            codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                            mensaje = "No es posible realizar esta consulta ya que no tiene un plan activo para RUCs naturales o jurídicos."
//                        });

//                    if (planEmpresaRucs.BloquearConsultas)
//                    {
//                        if (planEmpresaRucs.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Separado)
//                        {
//                            var fechaActual = DateTime.Today;
//                            var historialCedulas = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaRucs.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year && (m.TipoIdentificacion == Dominio.Constantes.General.RucNatural || m.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || m.TipoIdentificacion == Dominio.Constantes.General.SectorPublico));
//                            if (historialCedulas >= planEmpresaRucs.NumeroConsultasRuc)
//                                return BadRequest(new
//                                {
//                                    codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                    mensaje = $"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas para RUCs ({planEmpresaRucs.NumeroConsultasRuc}) en su plan."
//                                });
//                        }
//                        else if (planEmpresaRucs.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado)
//                        {
//                            if (planEmpresaRucs.NumeroConsultas.HasValue && planEmpresaRucs.NumeroConsultas.Value > 0)
//                            {
//                                var fechaActual = DateTime.Today;
//                                var historialUnificado = await _historiales.CountAsync(m => m.IdPlanEmpresa == planEmpresaRucs.Id && m.Fecha.Month == fechaActual.Month && m.Fecha.Year == fechaActual.Year);
//                                if (historialUnificado >= planEmpresaRucs.NumeroConsultas)
//                                    return BadRequest(new
//                                    {
//                                        codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                        mensaje = $"No es posible realizar esta consulta ya que alcanzó el límite máximo de consultas ({planEmpresaRucs.NumeroConsultas}) en su plan."
//                                    });
//                            }
//                            else
//                                return BadRequest(new
//                                {
//                                    codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                    mensaje = "El plan contratado no tiene definido un número de consultas"
//                                });
//                        }
//                        else
//                            return BadRequest(new
//                            {
//                                codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                                mensaje = "El plan contratado no tiene definido un tipo de consultas"
//                            });
//                    }
//                    idPlan = planEmpresaRucs.Id;
//                }
//                else
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.IdentificacionInvalida,
//                        mensaje = "La identificación ingresada no corresponde ni a cédulas ni a RUCs"
//                    });

//                if (idPlan == 0)
//                    return BadRequest(new
//                    {
//                        codigo = (short)Dominio.Tipos.ErroresApi.PlanesConsultas,
//                        mensaje = "No es posible realizar esta consulta ya que no tiene planes vigentes."
//                    });

//                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
//                _logger.LogInformation($"Procesando historial de usuario: {user.Id}. Identificación: {identificacionOriginal}. IP: {ip}.");

//                _logger.LogInformation("Registrando historial de usuarios en base de datos");
//                var idHistorial = await _historiales.GuardarHistorialAsync(new Historial()
//                {
//                    IdUsuario = user.Id,
//                    DireccionIp = ip?.Trim().ToUpper(),
//                    Identificacion = modelo.Identificacion?.Trim().ToUpper(),
//                    Periodo = 0,
//                    Fecha = DateTime.Now,
//                    TipoConsulta = Dominio.Tipos.Consultas.Api,
//                    ParametrosBusqueda = JsonConvert.SerializeObject(new { Identificacion = modelo.Identificacion, Periodo = new[] { 0 } }),
//                    IdPlanEmpresa = idPlan,
//                    TipoIdentificacion = tipoIdentificacion
//                });
//                _logger.LogInformation($"Registro de historial exitoso. Id Historial: {idHistorial}");

//                var datos = new RespuestaApiViewModel_1790010937001();
//                var contFuentes = 0;
//                var contFuentesPredios = 0;

//                #region SRI
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] SRI...");
//                var tSri = new Thread(async () =>
//                {
//                    try
//                    {
//                        Thread.Sleep(500);
//                        apiSri = await ObtenerReporteSRI(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id,
//                            IdEmpresa = user.IdEmpresa
//                        });
//                        datos.Sri = apiSri != null && apiSri.Empresa != null ? new SriApiViewModel_1790010937001()
//                        {
//                            RUC = apiSri.Empresa.RUC,
//                            RazonSocial = apiSri.Empresa.RazonSocial,
//                            NombreComercial = apiSri.Empresa.NombreComercial,
//                            Estado = apiSri.Empresa.Estado,
//                            EstadoContribuyente = apiSri.Empresa.EstadoContribuyente,
//                            Actividad = apiSri.Empresa.Actividad,
//                            FechaInicio = apiSri.Empresa.FechaInicio,
//                            FechaCese = apiSri.Empresa.FechaCese,
//                            FechaReinicio = apiSri.Empresa.FechaReinicio,
//                            FechaActualizacion = apiSri.Empresa.FechaActualizacion,
//                            RepresentanteLegal = apiSri.Empresa.RepresentanteLegal,
//                            Tipo = apiSri.Empresa.Tipo,
//                            Clasificacion = apiSri.Empresa.Clasificacion,
//                            Deudas = apiSri.Empresa.Deudas,
//                            Rentas = apiSri.Empresa.Rentas,
//                            Anexos = apiSri.Empresa.Anexos,
//                            Representantes = apiSri.Empresa.Representantes,
//                            UltimoPeriodoImpuestos = apiSri.Empresa.UltimoPeriodoImpuestos,
//                            UltimoValorRenta = apiSri.Empresa.UltimoValorRenta,
//                            PenultimoValorRenta = apiSri.Empresa.PenultimoValorRenta,
//                            AntepenultimoValorRenta = apiSri.Empresa.AntepenultimoValorRenta
//                        } : null;
//                        contFuentes++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] SRI terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentes++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] SRI ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tSri.Start();
//                #endregion SRI

//                #region IESS
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] IESS...");
//                var tIess = new Thread(async () =>
//                {
//                    try
//                    {
//                        Thread.Sleep(1000);
//                        apiIess = await ObtenerReporteIESS(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id,
//                            IdEmpresa = usuarioActual.IdEmpresa
//                        });
//                        datos.Iess = apiIess != null && apiIess.Afiliado != null ? new IessApiViewModel_1790010937001()
//                        {
//                            Empresas = apiIess.Afiliado.EmpresasAfiliado.Select(m => new EmpresaAfiliadoIessApiViewModel_1790010937001()
//                            {
//                                RucEmpresa = m.IdentificacionEmpresa,
//                                NombreEmpresa = m.NombreEmpresa
//                            }).ToList(),
//                            TipoAfiliacion = apiIess.Afiliado.Estado
//                        } : null;
//                        contFuentes++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] IESS terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentes++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] IESS ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tIess.Start();
//                #endregion IESS

//                #region ANT
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] ANT...");
//                var tAnt = new Thread(async () =>
//                {
//                    try
//                    {
//                        Thread.Sleep(1500);
//                        apiAnt = await ObtenerReporteANT(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        datos.Ant = apiAnt != null && apiAnt.Licencia != null ? new AntApiViewModel_1790010937001()
//                        {
//                            Puntos = apiAnt.Licencia.Puntos,
//                            PuntosOriginal = apiAnt.Licencia.PuntosOriginal,
//                            Autos = apiAnt.Licencia.Autos != null && apiAnt.Licencia.Autos.Any() ? apiAnt.Licencia.Autos.Select(m => new AutoAntApiViewModel_1790010937001()
//                            {
//                                Placa = m.Placa,
//                                FechaUltimaMatricula = m.FechaUltimaMatricula,
//                                TotalMatricula = m.TotalMatricula
//                            }).ToList() : new List<AutoAntApiViewModel_1790010937001>(),
//                            Multas = apiAnt.Licencia.Autos != null && apiAnt.Licencia.Multas.Any() ? apiAnt.Licencia.Multas.Select(m => new MultaAntApiViewModel_1790010937001()
//                            {
//                                Citacion = m.Citacion,
//                                Placa = m.Placa,
//                                FechaRegistro = m.FechaRegistro,
//                                Puntos = m.Puntos,
//                                Saldo = m.Saldo,
//                                ValorEmision = m.ValorEmision,
//                                ValorInteres = m.ValorInteres,
//                                ValorTotal = m.ValorTotal
//                            }).ToList() : new List<MultaAntApiViewModel_1790010937001>(),
//                        } : null;
//                        contFuentes++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] ANT terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentes++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] ANT ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tAnt.Start();
//                #endregion ANT

//                #region PENSIONES
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] PENSIONES...");
//                var tPensiones = new Thread(async () =>
//                {
//                    try
//                    {
//                        Thread.Sleep(2000);
//                        apiPension = await ObtenerReportePensionAlimenticia(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        datos.PensionAlimenticia = apiPension != null && apiPension.PensionAlimenticia != null && apiPension.PensionAlimenticia.Resultados != null && apiPension.PensionAlimenticia.Resultados.Any() ? new PensionAlimenticiaApiViewModel_1790010937001()
//                        {
//                            Procesos = apiPension.PensionAlimenticia.Resultados.Select(m => new ProcesoPensionAlimenticiaApiViewModel_1790010937001()
//                            {
//                                AlDia = m.AlDia,
//                                PensionActual = m.PensionActual,
//                                PensionActualOriginal = m.PensionActualOriginal,
//                                TipoPension = m.TipoPension,
//                                TotalDeuda = m.TotalDeuda,
//                                TotalPagado = m.TotalPagado
//                            }).ToList()
//                        } : null;
//                        contFuentes++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] PENSIONES terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentes++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] PENSIONES ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPensiones.Start();
//                #endregion PENSIONES

//                #region PREDIOS
//                var predios = new List<DetallePredioApiViewModel_1790010937001>();
//                #region QUITO
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] PREDIOS QUITO ...");
//                var tPQuito = new Thread(async () =>
//                {
//                    try
//                    {
//                        Thread.Sleep(2500);
//                        apiPrediosQuito = await ObtenerReportePrediosQuito(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        if (apiPrediosQuito != null && apiPrediosQuito.PrediosRepresentante != null && apiPrediosQuito.PrediosRepresentante.Detalle != null && apiPrediosQuito.PrediosRepresentante.Detalle.Any())
//                        {
//                            predios.AddRange(apiPrediosQuito.PrediosRepresentante.Detalle.Select(m => new DetallePredioApiViewModel_1790010937001
//                            {
//                                Municipio = "QUITO",
//                                Direccion = m.Direccion,
//                                Estado = m.Estado,
//                                Fecha = new DateTime(m.Anio, 1, 1),
//                                Valor = (double)m.Valor,
//                                RNum = m.NumeroTitulo
//                            }).ToList());
//                        }
//                        contFuentesPredios++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] PREDIOS QUITO terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentesPredios++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] PREDIOS QUITO ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPQuito.Start();
//                #endregion QUITO

//                #region CUENCA
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] PREDIOS CUENCA...");
//                var tPCuenca = new Thread(async () =>
//                {
//                    try
//                    {
//                        await Task.Yield();
//                        Thread.Sleep(3000);
//                        apiPrediosCuenca = ObtenerReportePrediosCuenca(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        if (apiPrediosCuenca != null && apiPrediosCuenca.PrediosRepresentante != null && apiPrediosCuenca.PrediosRepresentante.InformePredial != null && apiPrediosCuenca.PrediosRepresentante.InformePredial.Detalle != null && apiPrediosCuenca.PrediosRepresentante.InformePredial.Detalle.Any())
//                        {
//                            predios.AddRange(apiPrediosCuenca.PrediosRepresentante.InformePredial.Detalle.Select(m => new DetallePredioApiViewModel_1790010937001
//                            {
//                                Municipio = "CUENCA",
//                                Direccion = $"{m.Parroquia} / {m.Calle}",
//                                Fecha = m.FechaInscripcion,
//                                Valor = (double)m.AvaluoConstruccion + ((double?)m.AvaluoTerreno ?? 0),
//                                RNum = m.RNum,
//                            }).ToList());
//                        }
//                        contFuentesPredios++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] PREDIOS CUENCA terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentesPredios++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] PREDIOS CUENCA ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPCuenca.Start();
//                #endregion CUENCA

//                #region STO. DOMINGO
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] PREDIOS STO. DOMINGO...");
//                var tPStoDom = new Thread(async () =>
//                {
//                    try
//                    {
//                        await Task.Yield();
//                        Thread.Sleep(3000);
//                        apiPrediosStoDomingo = ObtenerReportePrediosSantoDomingo(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        if (apiPrediosStoDomingo != null && apiPrediosStoDomingo.PrediosRepresentante != null && apiPrediosStoDomingo.PrediosRepresentante.Detalle != null && apiPrediosStoDomingo.PrediosRepresentante.Detalle.Any())
//                        {
//                            predios.AddRange(apiPrediosStoDomingo.PrediosRepresentante.Detalle.Select(m => new DetallePredioApiViewModel_1790010937001
//                            {
//                                Municipio = "STO. DOMINGO",
//                                Fecha = m.Fecha,
//                                Valor = (double)m.Emitido,
//                                RNum = m.Ciu
//                            }).ToList());
//                        }
//                        contFuentesPredios++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] PREDIOS STO. DOMINGO terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentesPredios++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] PREDIOS STO. DOMINGO ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPStoDom.Start();
//                #endregion STO. DOMINGO

//                #region RUMINAHUI
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] PREDIOS RUMINAHUI...");
//                var tPRum = new Thread(async () =>
//                {
//                    try
//                    {
//                        await Task.Yield();
//                        Thread.Sleep(3500);
//                        apiPrediosRuminahui = ObtenerReportePrediosRuminahui(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        if (apiPrediosRuminahui != null && apiPrediosRuminahui.PrediosRepresentante != null && apiPrediosRuminahui.PrediosRepresentante.Detalle != null && apiPrediosRuminahui.PrediosRepresentante.Detalle.Any())
//                        {
//                            predios.AddRange(apiPrediosRuminahui.PrediosRepresentante.Detalle.Select(m => new DetallePredioApiViewModel_1790010937001
//                            {
//                                Municipio = "RUMIÑAHUI",
//                                Fecha = new DateTime(m.Anio, 1, 1),
//                                Valor = double.Parse(m.ValorPagar.Replace(",", "").Replace(".", ",")),
//                                RNum = m.NumeroComprobante
//                            }).ToList());
//                        }
//                        contFuentesPredios++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] PREDIOS RUMINAHUI terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentesPredios++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] PREDIOS RUMINAHUI ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPRum.Start();
//                #endregion RUMINAHUI

//                #region QUININDE
//                _logger.LogInformation($"Consultando [{modelo.Identificacion}] PREDIOS QUININDE...");
//                var tPQuin = new Thread(async () =>
//                {
//                    try
//                    {
//                        await Task.Yield();
//                        Thread.Sleep(4000);
//                        apiPrediosQuininde = ObtenerReportePrediosQuininde(new ApiViewModel_1790010937001()
//                        {
//                            IdHistorial = idHistorial,
//                            Identificacion = identificacionOriginal,
//                            IdUsuario = user.Id
//                        });
//                        if (apiPrediosQuininde != null && apiPrediosQuininde.PrediosRepresentante != null && apiPrediosQuininde.PrediosRepresentante.ValoresCancelados != null && apiPrediosQuininde.PrediosRepresentante.ValoresCancelados.Any())
//                        {
//                            predios.AddRange(apiPrediosQuininde.PrediosRepresentante.ValoresCancelados.Select(m => new DetallePredioApiViewModel_1790010937001
//                            {
//                                Municipio = "QUININDE",
//                                Valor = double.Parse(m.SubTotalValor.Replace(",", "").Replace(".", ",")),
//                                RNum = m.DescripcionPredio
//                            }).ToList());
//                        }

//                        if (apiPrediosQuininde != null && apiPrediosQuininde.PrediosRepresentante != null && apiPrediosQuininde.PrediosRepresentante.ValoresPendientes != null && apiPrediosQuininde.PrediosRepresentante.ValoresPendientes.Any())
//                        {
//                            predios.AddRange(apiPrediosQuininde.PrediosRepresentante.ValoresPendientes.Select(m => new DetallePredioApiViewModel_1790010937001
//                            {
//                                Municipio = "QUININDE",
//                                Valor = double.Parse(m.SubTotalValor.Replace(",", "").Replace(".", ",")),
//                                RNum = m.DescripcionPredio
//                            }).ToList());
//                        }
//                        contFuentesPredios++;
//                        _logger.LogInformation($"Consulta [{modelo.Identificacion}] PREDIOS QUININDE terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        contFuentesPredios++;
//                        _logger.LogError($"Consulta [{modelo.Identificacion}] PREDIOS QUININDE ocurrió un problema al procesar {ex.Message}.");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPQuin.Start();
//                #endregion QUININDE

//                _logger.LogInformation($"Verificando [{modelo.Identificacion}] PREDIOS...");
//                var tPredios = new Thread(async () =>
//                {
//                    try
//                    {
//                        Thread.Sleep(4500);
//                        var swPredios = false;
//                        do
//                        {
//                            if (contFuentesPredios != 5)
//                                await Task.Delay(500);
//                            else
//                            {
//                                swPredios = true;
//                                contFuentes++;
//                                if (predios.Any())
//                                    datos.Predios = new PredioApiViewModel_1790010937001()
//                                    {
//                                        Detalle = predios
//                                    };
//                            }
//                        } while (!swPredios);
//                        _logger.LogInformation($"Verificación [{modelo.Identificacion}] PREDIOS terminada.");
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Verificación [{modelo.Identificacion}] PREDIOS ocurrió un problema al procesar {ex.Message}");
//                        _logger.LogError(ex, ex.Message);
//                    }
//                });
//                tPredios.Start();
//                #endregion PREDIOS

//                _logger.LogInformation($"Verificando [{modelo.Identificacion}] FUENTES...");
//                var swFuentes = false;
//                do
//                {
//                    if (contFuentes != 5)
//                        await Task.Delay(500);
//                    else
//                        swFuentes = true;
//                } while (!swFuentes);
//                _logger.LogInformation($"Verificación [{modelo.Identificacion}] FUENTES terminada.");

//                _logger.LogInformation($"Procesamiento consulta [{modelo.Identificacion}] terminada.");
//                return Json(datos, new JsonSerializerSettings
//                {
//                    ContractResolver = new CamelCasePropertyNamesContractResolver()
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, ex.Message);
//                return BadRequest(new
//                {
//                    codigoError = (short)Dominio.Tipos.ErroresApi.ProcesamientoConsulta,
//                    mensaje = ex.Message
//                });
//            }
//        }
//        private async Task<SriApiViewModel> ObtenerReporteSRI(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                var identificacionOriginal = modelo.Identificacion;
//                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
//                var cacheSri = false;
//                var impuestosRenta = new List<Externos.Logica.SRi.Modelos.Anexo>();

//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            modelo.Identificacion = $"{modelo.Identificacion}001";
//                        }

//                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                        {
//                            r_sri = await _sri.GetContribuyenteBPichinchaSri(modelo.Identificacion);
//                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
//                            {
//                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);
//                                if (string.IsNullOrEmpty(r_sri.AgenteRepresentante))
//                                    r_sri.AgenteRepresentante = await _balances.GetRepresentanteLegalEmpresaAccionistaAsync(modelo.Identificacion);
//                            }
//                        }

//                        if (r_sri != null)
//                        {
//                            if (r_sri.Anexos != null && r_sri.Anexos.Any())
//                                impuestosRenta.AddRange(r_sri.Anexos.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
//                                {
//                                    Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
//                                    Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
//                                    Formulario = m.Formulario,
//                                    Periodo = m.Periodo
//                                }).ToList());

//                            if (r_sri.Rentas != null && r_sri.Rentas.Any())
//                            {
//                                if (impuestosRenta.Any())
//                                {
//                                    foreach (var item in impuestosRenta)
//                                    {
//                                        var impuesto = r_sri.Rentas.FirstOrDefault(m => m.Periodo == item.Periodo);
//                                        if (impuesto != null && impuesto.Causado.HasValue && impuesto.Causado >= 0)
//                                        {
//                                            item.Formulario = impuesto.Formulario;
//                                            item.Causado = impuesto.Causado;
//                                        }

//                                        if (impuesto != null && impuesto.Formulario != item.Formulario)
//                                            item.Formulario = impuesto.Formulario;
//                                    }
//                                }
//                                else
//                                {
//                                    impuestosRenta.AddRange(r_sri.Rentas.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
//                                    {
//                                        Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
//                                        Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
//                                        Formulario = m.Formulario,
//                                        Periodo = m.Periodo
//                                    }).ToList());
//                                }
//                            }

//                            if (r_sri.Divisas != null && r_sri.Divisas.Any())
//                            {
//                                if (impuestosRenta.Any())
//                                {
//                                    foreach (var item in impuestosRenta)
//                                    {
//                                        var impuesto = r_sri.Divisas.FirstOrDefault(m => m.Periodo == item.Periodo);
//                                        if (impuesto != null && impuesto.Divisas.HasValue && impuesto.Divisas >= 0)
//                                            item.Divisas = impuesto.Divisas;
//                                    }
//                                }
//                                else
//                                {
//                                    impuestosRenta.AddRange(r_sri.Divisas.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
//                                    {
//                                        Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
//                                        Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
//                                        Formulario = m.Formulario,
//                                        Periodo = m.Periodo
//                                    }).ToList());
//                                }
//                            }

//                            r_sri.Anexos = impuestosRenta.OrderByDescending(m => m.Periodo).ToList();
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
//                    }
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
//                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));
//                }

//                try
//                {
//                    if (r_sri == null)
//                    {
//                        var datosDetalleSri = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                        if (datosDetalleSri != null)
//                        {
//                            cacheSri = true;
//                            r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }

//                var datos = new SriApiViewModel()
//                {
//                    Empresa = r_sri,
//                };

//                _logger.LogInformation("Fuente de SRI procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");
//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        try
//                        {
//                            var historial = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial);
//                            var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
//                            if (historial != null)
//                            {
//                                if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
//                                {
//                                    historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
//                                    if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
//                                        historial.IdentificacionSecundaria = r_sri.RUC;
//                                }
//                                if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante?.Trim()) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
//                                    historial.NombresPersona = r_sri.AgenteRepresentante.Trim().ToUpper();

//                                if (r_sri != null && (historial.TipoIdentificacion == Dominio.Constantes.General.Cedula || historial.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historial.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && !string.IsNullOrEmpty(r_sri.RepresentanteLegal?.Trim()) && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
//                                {
//                                    if (ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal) && ValidacionViewModel.ValidarRuc(historial.Identificacion))
//                                        historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
//                                    else
//                                        historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Trim();
//                                }
//                                else
//                                {
//                                    if (r_sri != null && r_sri.PersonaSociedad == "SCD" && historial.TipoIdentificacion == Dominio.Constantes.General.Cedula && ValidacionViewModel.ValidarRuc(r_sri.RepresentanteLegal))
//                                        historial.IdentificacionSecundaria = r_sri.RepresentanteLegal.Substring(0, 10).Trim();
//                                    else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
//                                        historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
//                                }
//                                _historiales.Update(historial);
//                                if (historialConsolidado != null)
//                                {
//                                    historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
//                                    historialConsolidado.NombrePersona = historial.NombresPersona;
//                                    await _reporteConsolidado.UpdateAsync(historialConsolidado);
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogError(ex, ex.Message);
//                        }

//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.Sri,
//                            Generado = datos.Empresa != null,
//                            Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null,
//                            Cache = cacheSri,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = null
//                        });
//                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        private async Task<IessApiMetodoViewModel> ObtenerReporteIESS(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.IESS.Modelos.Afiliacion r_afiliacion = null;
//                //V2
//                ResultadoListAfiliado resultadoAfiliado = null;
//                ResultadoListEmpleado resultadoListEmpleado = null;
//                Externos.Logica.IESS.Modelos.ResultadoAfiliacion resultadoAfiliacion = null;
//                Externos.Logica.IESS.Modelos.ResultadoPersona resultadoIess = null;
//                //V2
//                var datos = new IessApiMetodoViewModel();
//                Historial historialTemp = null;
//                var cacheAfiliado = false;
//                var cedulaEntidades = false;

//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente IESS identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            cedulaEntidades = true;
//                            modelo.Identificacion = $"{modelo.Identificacion}001";
//                        }

//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (historialTemp != null)
//                        {
//                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                            {
//                                if (cedulaEntidades)
//                                {
//                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                    {
//                                        resultadoAfiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(modelo.Identificacion);
//                                        r_afiliacion = resultadoAfiliacion?.Afiliacion;
//                                    }
//                                    }
//                                else
//                                {
//                                    if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
//                                    {
//                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
//                                        {
//                                            if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
//                                            {
//                                                resultadoAfiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(historialTemp.IdentificacionSecundaria.Trim());
//                                                r_afiliacion = resultadoAfiliacion?.Afiliacion;
//                                            }
//                                        }
//                                    }
//                                    else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                                    {
//                                        var cedulaTemp = modelo.Identificacion.Substring(0, 10);
//                                        if (ValidacionViewModel.ValidarCedula(cedulaTemp))
//                                        {
//                                            resultadoAfiliacion = await _iess.GetAfiliacionCertificadoAsyncV2(cedulaTemp);
//                                            r_afiliacion = resultadoAfiliacion?.Afiliacion;
//                                        }
//                                    }
//                                }
//                            }
//                        }

//                        if (r_afiliacion != null)
//                        {
//                            var empresasAfiliacion = Regex.Matches(r_afiliacion.Empresa, "001").Count();
//                            if (empresasAfiliacion > 1)
//                                r_afiliacion.Empresa = r_afiliacion.Empresa.Replace("001", "001. ");

//                            if (!string.IsNullOrEmpty(r_afiliacion.Reporte))
//                                r_afiliacion.Reporte = null;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente IESS con identificación {modelo.Identificacion}: {ex.Message}");
//                    }

//                    try
//                    {
//                        if (r_afiliacion == null)
//                        {
//                            var datosDetalleAfiliado = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Afiliado && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                            if (datosDetalleAfiliado != null)
//                            {
//                                cacheAfiliado = true;
//                                r_afiliacion = JsonConvert.DeserializeObject<Externos.Logica.IESS.Modelos.Afiliacion>(datosDetalleAfiliado);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, ex.Message);
//                    }

//                    datos = new IessApiMetodoViewModel()
//                    {
//                        Afiliado = r_afiliacion,
//                    };
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathIess = Path.Combine(pathFuentes, "iessDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathIess);
//                    datos = JsonConvert.DeserializeObject<IessApiMetodoViewModel>(archivo);
//                    datos.Afiliado.Reporte = null;
//                }
//                _logger.LogInformation("Fuente de IESS procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente IESS. Id Historial: {modelo.IdHistorial}");
//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.Afiliado,
//                            Generado = datos.Afiliado != null,
//                            Data = datos.Afiliado != null ? JsonConvert.SerializeObject(datos.Afiliado) : null,
//                            Cache = cacheAfiliado,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = null,
//                            DataError = resultadoAfiliado?.Error,
//                            FuenteActiva = resultadoAfiliado?.FuenteActiva
//                        });
//                        _logger.LogInformation("Historial de la Fuente IESS procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        private async Task<AntApiViewModel> ObtenerReporteANT(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.ANT.Modelos.Licencia r_ant = null;
//                var datos = new AntApiViewModel();
//                Historial historialTemp = null;
//                var cacheAnt = false;

//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente ANT identificación: {modelo.Identificacion}");
//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (historialTemp != null)
//                        {
//                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                            {
//                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                    r_ant = await _ant.GetRespuestaAsync(modelo.Identificacion);

//                                if (r_ant == null && historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
//                                    r_ant = await _ant.GetRespuestaAsync(historialTemp.IdentificacionSecundaria);

//                                if (r_ant != null && (string.IsNullOrEmpty(r_ant.Cedula) || string.IsNullOrEmpty(r_ant.Titular)))
//                                    r_ant = null;
//                            }
//                            else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                r_ant = await _ant.GetRespuestaAsync(modelo.Identificacion);
//                        }

//                        try
//                        {
//                            if (r_ant == null)
//                            {
//                                var datosDetalleAnt = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Ant && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                                if (datosDetalleAnt != null)
//                                {
//                                    cacheAnt = true;
//                                    r_ant = JsonConvert.DeserializeObject<Externos.Logica.ANT.Modelos.Licencia>(datosDetalleAnt);
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogError(ex, ex.Message);
//                        }

//                        if (r_ant != null && historialTemp != null)
//                        {
//                            if ((r_ant.Cedula?.Trim() == historialTemp.Identificacion?.Trim() || r_ant.Cedula?.Trim() == historialTemp.IdentificacionSecundaria?.Trim()) && !string.IsNullOrEmpty(historialTemp.NombresPersona) && !string.IsNullOrEmpty(r_ant.Titular) && historialTemp.NombresPersona != r_ant.Titular)
//                                r_ant.Titular = historialTemp.NombresPersona;
//                        }

//                        datos = new AntApiViewModel()
//                        {
//                            Licencia = r_ant,
//                        };
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente ANT con identificación {modelo.Identificacion}: {ex.Message}");
//                    }
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathAnt = Path.Combine(pathFuentes, "antDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathAnt);
//                    var licencia = JsonConvert.DeserializeObject<AntApiViewModel>(archivo);
//                    datos = new AntApiViewModel()
//                    {
//                        Licencia = licencia.Licencia,
//                    };
//                }

//                _logger.LogInformation("Fuente de ANT procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente ANT. Id Historial: {modelo.IdHistorial}");
//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.Ant,
//                            Generado = datos.Licencia != null,
//                            Data = datos.Licencia != null ? JsonConvert.SerializeObject(datos.Licencia) : null,
//                            Cache = cacheAnt,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = null
//                        });
//                        _logger.LogInformation("Historial de la Fuente ANT procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        private async Task<PrediosApiViewModel> ObtenerReportePrediosQuito(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.PredioMunicipio.Modelos.Resultado r_prediosRepresentante = null;
//                var datos = new PrediosApiViewModel();
//                Historial historialTemp = null;
//                var cachePredios = false;

//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente Predios identificación: {modelo.Identificacion}");
//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (historialTemp != null)
//                        {
//                            if ((ValidacionViewModel.ValidarCedula(historialTemp.Identificacion) || ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria)) && !string.IsNullOrEmpty(historialTemp.NombresPersona))
//                                r_prediosRepresentante = await _predios.GetRespuestaAsync(historialTemp.NombresPersona);
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente Predios con identificación {modelo.Identificacion}: {ex.Message}");
//                    }

//                    try
//                    {
//                        if (r_prediosRepresentante == null)
//                        {
//                            var datosPredios = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipio && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                            if (datosPredios != null)
//                            {
//                                cachePredios = true;
//                                r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.Resultado>(datosPredios);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, ex.Message);
//                    }

//                    datos = new PrediosApiViewModel()
//                    {
//                        PrediosRepresentante = r_prediosRepresentante,
//                    };
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathPredios = Path.Combine(pathFuentes, "prediosDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathPredios);
//                    var datosCache = JsonConvert.DeserializeObject<Areas.Consultas.Models.PrediosViewModel>(archivo);
//                    datos = new PrediosApiViewModel()
//                    {
//                        PrediosRepresentante = datosCache.PrediosRepresentante
//                    };
//                }

//                _logger.LogInformation("Fuente de Predio procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio. Id Historial: {modelo.IdHistorial}");

//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipio,
//                            Generado = datos.PrediosRepresentante != null,
//                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
//                            Cache = cachePredios,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = false
//                        });
//                        _logger.LogInformation("Historial de la Fuente Predio procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        private PrediosCuencaApiViewModel ObtenerReportePrediosCuenca(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca r_prediosRepresentante = null;
//                var datos = new PrediosCuencaApiViewModel();
//                Historial historialTemp = null;
//                var cachePredios = false;
//                var cedulaEntidades = false;

//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente Predios Cuenca identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            cedulaEntidades = true;
//                            modelo.Identificacion = $"{modelo.Identificacion}001";
//                        }

//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (historialTemp != null)
//                        {
//                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                            {
//                                if (cedulaEntidades)
//                                {
//                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                        r_prediosRepresentante = _predios.GetPrediosCuenca(modelo.Identificacion);
//                                }
//                                else
//                                {
//                                    if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
//                                    {
//                                        if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
//                                            r_prediosRepresentante = _predios.GetPrediosCuenca(historialTemp.IdentificacionSecundaria.Trim());

//                                    }
//                                }
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente Predios Cuenca con identificación {modelo.Identificacion}: {ex.Message}");
//                    }

//                    try
//                    {
//                        if (r_prediosRepresentante == null)
//                        {
//                            var datosPredios = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioCuenca && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                            if (datosPredios != null)
//                            {
//                                cachePredios = true;
//                                r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosCuenca>(datosPredios);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, ex.Message);
//                    }

//                    datos = new PrediosCuencaApiViewModel()
//                    {
//                        PrediosRepresentante = r_prediosRepresentante,
//                    };
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathPredios = Path.Combine(pathFuentes, "prediosCuencaDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathPredios);
//                    var datosCache = JsonConvert.DeserializeObject<Areas.Consultas.Models.PrediosCuencaViewModel>(archivo);
//                    datos = new PrediosCuencaApiViewModel()
//                    {
//                        PrediosRepresentante = datosCache.PrediosRepresentante
//                    };
//                }

//                _logger.LogInformation("Fuente de Predio Cuenca procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Cuenca. Id Historial: {modelo.IdHistorial}");

//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioCuenca,
//                            Generado = datos.PrediosRepresentante != null,
//                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
//                            Cache = cachePredios,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = false
//                        });
//                        _logger.LogInformation("Historial de la Fuente Predio Cuenca procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        public PrediosSantoDomingoApiViewModel ObtenerReportePrediosSantoDomingo(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo r_prediosRepresentante = null;
//                var datos = new PrediosSantoDomingoApiViewModel();
//                Historial historialTemp = null;
//                var cachePredios = false;
//                var cedulaEntidades = false;
//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente Santo Domingo Cuenca identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            cedulaEntidades = true;
//                            modelo.Identificacion = $"{modelo.Identificacion}001";
//                        }
//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                        {
//                            if (cedulaEntidades)
//                            {
//                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                    r_prediosRepresentante = _predios.GetPrediosSantoDomingo(modelo.Identificacion);
//                            }
//                            else
//                            {
//                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
//                                {
//                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
//                                        r_prediosRepresentante = _predios.GetPrediosSantoDomingo(historialTemp.IdentificacionSecundaria.Trim());

//                                }
//                            }
//                        }

//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente Predios Santo Domingo con identificación {modelo.Identificacion}: {ex.Message}");
//                    }

//                    try
//                    {
//                        if (r_prediosRepresentante == null || (r_prediosRepresentante != null && r_prediosRepresentante.Detalle != null && !r_prediosRepresentante.Detalle.Any()))
//                        {
//                            var datosPredios = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                            if (datosPredios != null)
//                            {
//                                cachePredios = true;
//                                r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosSantoDomingo>(datosPredios);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, ex.Message);
//                    }

//                    datos = new PrediosSantoDomingoApiViewModel()
//                    {
//                        PrediosRepresentante = r_prediosRepresentante,
//                    };
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathPredios = Path.Combine(pathFuentes, "prediosSantoDomingoDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathPredios);
//                    var datosCache = JsonConvert.DeserializeObject<Areas.Consultas.Models.PrediosSantoDomingoViewModel>(archivo);

//                    datos = new PrediosSantoDomingoApiViewModel()
//                    {
//                        PrediosRepresentante = datosCache.PrediosRepresentante
//                    };
//                }

//                _logger.LogInformation("Fuente de Predio Santo Domingo procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Santo Domingo. Id Historial: {modelo.IdHistorial}");

//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioSantoDomingo,
//                            Generado = datos.PrediosRepresentante != null,
//                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
//                            Cache = cachePredios,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = false
//                        });
//                        _logger.LogInformation("Historial de la Fuente Predio Santo Domingo procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        public PrediosRuminahuiApiViewModel ObtenerReportePrediosRuminahui(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui r_prediosRepresentante = null;
//                var datos = new PrediosRuminahuiApiViewModel();
//                Historial historialTemp = null;
//                var cachePredios = false;
//                var cedulaEntidades = false;
//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente Rumiñahui Cuenca identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            cedulaEntidades = true;
//                            modelo.Identificacion = $"{modelo.Identificacion}001";
//                        }
//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                        {
//                            if (cedulaEntidades)
//                            {
//                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                    r_prediosRepresentante = _predios.GetPrediosRuminahui(modelo.Identificacion);
//                            }
//                            else
//                            {
//                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
//                                {
//                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
//                                        r_prediosRepresentante = _predios.GetPrediosRuminahui(historialTemp.IdentificacionSecundaria.Trim());
//                                }
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente Predios Rumiñahui con identificación {modelo.Identificacion}: {ex.Message}");
//                    }

//                    try
//                    {
//                        if (r_prediosRepresentante == null || (r_prediosRepresentante != null && r_prediosRepresentante.Detalle != null && !r_prediosRepresentante.Detalle.Any()))
//                        {
//                            var datosPredios = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioRuminahui && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                            if (datosPredios != null)
//                            {
//                                cachePredios = true;
//                                r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosRuminahui>(datosPredios);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, ex.Message);
//                    }

//                    datos = new PrediosRuminahuiApiViewModel()
//                    {
//                        PrediosRepresentante = r_prediosRepresentante,
//                    };
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathPredios = Path.Combine(pathFuentes, "prediosRuminahuiDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathPredios);
//                    var datosCache = JsonConvert.DeserializeObject<Areas.Consultas.Models.PrediosRuminahuiViewModel>(archivo);

//                    datos = new PrediosRuminahuiApiViewModel()
//                    {
//                        PrediosRepresentante = datosCache.PrediosRepresentante
//                    };
//                }

//                _logger.LogInformation("Fuente de Predio Rumiñahui procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Rumiñahui. Id Historial: {modelo.IdHistorial}");

//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioRuminahui,
//                            Generado = datos.PrediosRepresentante != null,
//                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
//                            Cache = cachePredios,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = false
//                        });
//                        _logger.LogInformation("Historial de la Fuente Predio Rumiñahui procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        public PrediosQuinindeApiViewModel ObtenerReportePrediosQuininde(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde r_prediosRepresentante = null;
//                var datos = new PrediosQuinindeApiViewModel();
//                Historial historialTemp = null;
//                var cachePredios = false;
//                var cedulaEntidades = false;
//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente Quinindé Cuenca identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            cedulaEntidades = true;
//                            modelo.Identificacion = $"{modelo.Identificacion}001";
//                        }
//                        historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                        {
//                            if (cedulaEntidades)
//                            {
//                                modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                                    r_prediosRepresentante = _predios.GetPrediosQuininde(modelo.Identificacion);
//                            }
//                            else
//                            {
//                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria) && (ValidacionViewModel.ValidarRucJuridico(historialTemp.Identificacion) || ValidacionViewModel.ValidarRuc(historialTemp.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(historialTemp.Identificacion)))
//                                {
//                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
//                                        r_prediosRepresentante = _predios.GetPrediosQuininde(historialTemp.IdentificacionSecundaria.Trim());

//                                }
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente Predios Quinindé con identificación {modelo.Identificacion}: {ex.Message}");
//                    }

//                    try
//                    {
//                        if (r_prediosRepresentante == null)
//                        {
//                            var datosPredios = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PredioMunicipioQuininde && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                            if (datosPredios != null)
//                            {
//                                cachePredios = true;
//                                r_prediosRepresentante = JsonConvert.DeserializeObject<Externos.Logica.PredioMunicipio.Modelos.PrediosQuininde>(datosPredios);
//                            }
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError(ex, ex.Message);
//                    }

//                    datos = new PrediosQuinindeApiViewModel()
//                    {
//                        PrediosRepresentante = r_prediosRepresentante,
//                    };
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathPredios = Path.Combine(pathFuentes, "prediosQuinindeDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathPredios);
//                    var datosCache = JsonConvert.DeserializeObject<Areas.Consultas.Models.PrediosQuinindeViewModel>(archivo);

//                    datos = new PrediosQuinindeApiViewModel()
//                    {
//                        PrediosRepresentante = datosCache.PrediosRepresentante
//                    };
//                }

//                _logger.LogInformation("Fuente de Predio Quinindé procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente Predio Quinindé. Id Historial: {modelo.IdHistorial}");

//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.PredioMunicipioQuininde,
//                            Generado = datos.PrediosRepresentante != null,
//                            Data = datos.PrediosRepresentante != null ? JsonConvert.SerializeObject(datos.PrediosRepresentante) : null,
//                            Cache = cachePredios,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = false
//                        });
//                        _logger.LogInformation("Historial de la Fuente Predio Quinindé procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//        private async Task<PensionAlimenticiaApiViewModel> ObtenerReportePensionAlimenticia(ApiViewModel_1790010937001 modelo)
//        {
//            try
//            {
//                if (modelo == null)
//                    throw new Exception("No se han enviado parámetros para obtener el reporte");

//                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
//                    throw new Exception("El campo RUC es obligatorio");

//                modelo.Identificacion = modelo.Identificacion.Trim();
//                Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia r_pension = null;
//                var datos = new PensionAlimenticiaApiViewModel();
//                var cachePension = false;
//                var historialTemp = _historiales.FirstOrDefault(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
//                ResultadoResponsePensionAlmenticia resultadoPAlimenticia = null;

//                if (!_cache)
//                {
//                    try
//                    {
//                        _logger.LogInformation($"Procesando Fuente Pension alimenticia identificación: {modelo.Identificacion}");
//                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
//                        {
//                            modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
//                            if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                            {
//                                resultadoPAlimenticia = await _pension.GetRespuestaAsyncV2(modelo.Identificacion);
//                                r_pension = resultadoPAlimenticia?.PensionAlimenticia;
//                            }

//                            if (r_pension == null && historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
//                            {
//                                resultadoPAlimenticia = await _pension.GetRespuestaAsyncV2(historialTemp.IdentificacionSecundaria);
//                                r_pension = resultadoPAlimenticia?.PensionAlimenticia;
//                            }
//                        }
//                        else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
//                        {
//                            resultadoPAlimenticia = await _pension.GetRespuestaAsyncV2(modelo.Identificacion);
//                            r_pension = resultadoPAlimenticia?.PensionAlimenticia;
//                        }

//                        if (r_pension != null && r_pension.Resultados == null)
//                            r_pension = null;

//                        try
//                        {
//                            if (r_pension == null)
//                            {
//                                var datosDetallePension = _detallesHistorial.FirstOrDefault(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.PensionAlimenticia && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
//                                if (datosDetallePension != null)
//                                {
//                                    cachePension = true;
//                                    r_pension = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(datosDetallePension);
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogError(ex, ex.Message);
//                        }

//                        datos = new PensionAlimenticiaApiViewModel()
//                        {
//                            PensionAlimenticia = r_pension
//                        };

//                        if (datos.PensionAlimenticia != null && datos.PensionAlimenticia.Resultados != null)
//                            foreach (var item in datos.PensionAlimenticia.Resultados)
//                            {
//                                item.Nombre = historialTemp.NombresPersona;
//                                item.Cedula = (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico) ? historialTemp.IdentificacionSecundaria : historialTemp.Identificacion;
//                            }
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"Error al consultar fuente Pension Alimenticia con identificación {modelo.Identificacion}: {ex.Message}");
//                    }
//                }
//                else
//                {
//                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
//                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
//                    var pathAnt = Path.Combine(pathFuentes, "pensionAlimenticiaDemo.json");
//                    var archivo = System.IO.File.ReadAllText(pathAnt);
//                    var pension = JsonConvert.DeserializeObject<Externos.Logica.PensionesAlimenticias.Modelos.PensionAlimenticia>(archivo);
//                    datos = new PensionAlimenticiaApiViewModel()
//                    {
//                        PensionAlimenticia = pension,
//                    };
//                }

//                _logger.LogInformation("Fuente de Pension alimenticia procesada correctamente");
//                _logger.LogInformation($"Procesando registro de historiales de la fuente Pension alimenticia. Id Historial: {modelo.IdHistorial}");
//                try
//                {
//                    if (modelo.IdHistorial > 0)
//                    {
//                        _detallesHistorial.GuardarDetalleHistorial(new DetalleHistorial()
//                        {
//                            IdHistorial = modelo.IdHistorial,
//                            TipoFuente = Dominio.Tipos.Fuentes.PensionAlimenticia,
//                            Generado = datos.PensionAlimenticia != null,
//                            Data = datos.PensionAlimenticia != null ? JsonConvert.SerializeObject(datos.PensionAlimenticia) : null,
//                            Cache = cachePension,
//                            FechaRegistro = DateTime.Now,
//                            Reintento = null,
//                            DataError = resultadoPAlimenticia != null ? resultadoPAlimenticia.Error : null,
//                            FuenteActiva = resultadoPAlimenticia != null ? resultadoPAlimenticia.FuenteActiva : null
//                        });
//                        _logger.LogInformation("Historial de la Fuente Pension alimenticia procesado correctamente");
//                    }
//                    else
//                        throw new Exception("El Id del Historial no se ha generado correctamente");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, ex.Message);
//                }
//                return datos;
//            }
//            catch (Exception e)
//            {
//                _logger.LogError(e, e.Message);
//                return null;
//            }
//        }
//    }
//}
