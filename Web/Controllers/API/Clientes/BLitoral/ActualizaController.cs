// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Dominio.Entidades.Identidad;
using Externos.Logica.Garancheck.Modelos;
using Infraestructura.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Persistencia.Repositorios.Balance;
using Persistencia.Repositorios.Identidad;
using Web.Models;
using Dominio.Tipos;
using System.Collections.Generic;
using Dominio.Entidades.Balances;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Web.Areas.Consultas.Models;
using System.IO;
using Externos.Logica.SRi.Modelos;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.RegularExpressions;
using Dominio.Tipos.Clientes.Cliente0990981930001;

namespace Web.Controllers.API
{
    [Route("api/Clientes/BLitoral/Actualiza")]
    [ApiController]
    public class ActualizaController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly Externos.Logica.SRi.Controlador _sri;
        private readonly Externos.Logica.Balances.Controlador _balances;
        private readonly Externos.Logica.IESS.Controlador _iess;
        private readonly Externos.Logica.FJudicial.Controlador _fjudicial;
        private readonly Externos.Logica.ANT.Controlador _ant;
        private readonly Externos.Logica.PensionesAlimenticias.Controlador _pension;
        private readonly Externos.Logica.Garancheck.Controlador _garancheck;
        private readonly Externos.Logica.SERCOP.Controlador _sercop;
        private readonly Externos.Logica.BuroCredito.Controlador _burocredito;
        private readonly Externos.Logica.Equifax.Controlador _buroCreditoEquifax;
        private readonly Externos.Logica.SuperBancos.Controlador _superBancos;
        private readonly Externos.Logica.AntecedentesPenales.Controlador _antecedentes;
        private readonly Externos.Logica.PredioMunicipio.Controlador _predios;
        private readonly Externos.Logica.FiscaliaDelitos.Controlador _fiscaliaDelitos;
        private readonly Externos.Logica.UAFE.Controlador _uafe;
        private readonly Externos.Logica.RegistroCivilWS.Controlador _registroWS;
        private readonly IHttpContextAccessor _httpContext;
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IHistoriales _historiales;
        private readonly IDetallesHistorial _detallesHistorial;
        private readonly IConsultaService _consulta;
        private readonly IUsuarios _usuarios;
        private readonly IPoliticas _politicas;
        private readonly ICalificaciones _calificaciones;
        private readonly IDetalleCalificaciones _detalleCalificaciones;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly IAccesos _accesos;
        private readonly ICredencialesBuro _credencialesBuro;
        private readonly IParametrosClientesHistoriales _parametrosClientesHistoriales;
        private readonly IEmailService _emailSender;
        private readonly IReportesConsolidados _reporteConsolidado;
        private bool _cache = false;

        public ActualizaController(IConfiguration configuration, ILoggerFactory loggerFactory,
            Externos.Logica.SRi.Controlador sri,
            Externos.Logica.Balances.Controlador balances,
            Externos.Logica.IESS.Controlador iess,
            Externos.Logica.FJudicial.Controlador fjudicial,
            Externos.Logica.ANT.Controlador ant,
            Externos.Logica.PensionesAlimenticias.Controlador pension,
            Externos.Logica.Garancheck.Controlador garancheck,
            Externos.Logica.SERCOP.Controlador sercop,
            Externos.Logica.BuroCredito.Controlador burocredito,
            Externos.Logica.Equifax.Controlador buroCreditoEquifax,
            Externos.Logica.SuperBancos.Controlador superBancos,
            Externos.Logica.AntecedentesPenales.Controlador antecedentes,
            Externos.Logica.PredioMunicipio.Controlador predios,
            Externos.Logica.FiscaliaDelitos.Controlador fiscaliaDelitos,
            Externos.Logica.UAFE.Controlador uafe,
            Externos.Logica.RegistroCivilWS.Controlador registroWS,
            SignInManager<Usuario> signInManager,
            UserManager<Usuario> userManager,
            IHistoriales historiales,
            IDetallesHistorial detallehistoriales,
            IHttpContextAccessor httpContext,
            IConsultaService consulta,
            IPoliticas politicas,
            ICalificaciones calificaciones,
            IDetalleCalificaciones detalleCalificaciones,
            IPlanesEvaluaciones planesEvaluaciones,
            IAccesos accesos,
            IUsuarios usuarios,
            ICredencialesBuro credencialesBuro,
            IParametrosClientesHistoriales parametrosClientesHistoriales,
            IReportesConsolidados reportesConsolidados,
            IEmailService emailSender)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _sri = sri;
            _balances = balances;
            _iess = iess;
            _fjudicial = fjudicial;
            _ant = ant;
            _pension = pension;
            _garancheck = garancheck;
            _sercop = sercop;
            _burocredito = burocredito;
            _buroCreditoEquifax = buroCreditoEquifax;
            _superBancos = superBancos;
            _antecedentes = antecedentes;
            _predios = predios;
            _uafe = uafe;
            _registroWS = registroWS;
            _fiscaliaDelitos = fiscaliaDelitos;
            _httpContext = httpContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _historiales = historiales;
            _detallesHistorial = detallehistoriales;
            _consulta = consulta;
            _usuarios = usuarios;
            _calificaciones = calificaciones;
            _politicas = politicas;
            _detalleCalificaciones = detalleCalificaciones;
            _planesEvaluaciones = planesEvaluaciones;
            _accesos = accesos;
            _credencialesBuro = credencialesBuro;
            _parametrosClientesHistoriales = parametrosClientesHistoriales;
            _emailSender = emailSender;
            _reporteConsolidado = reportesConsolidados;
            _cache = _configuration.GetSection("AppSettings:Consultas:Cache").Get<bool>();
        }

        [HttpPost("ActualizarInformacion")]
        public async Task<IActionResult> ActualizarInformacion(ApiActualizarViewModel modelo)
        {
            try
            {
                var usuario = string.Empty;
                var clave = string.Empty;
                Usuario user = null;
                var fuenteSocietario = false;
                var fuenteIess = false;
                var fuenteSercop = false;
                var fuenteJudicial = false;
                var fuenteSenescyt = false;
                var fuenteCivil = false;
                var fuenteSri = false;
                var fuenteAnt = false;
                var fuentePension = false;
                var fuenteSuperBancos = false;
                var fuenteAntecedentes = false;
                var fuenteFuerzasArmadas = false;
                var fuenteDeNoBaja = false;
                var fuentePredios = false;
                var fuentePrediosCuenca = false;
                var fuentePrediosStoDomingo = false;
                var fuentePrediosRuminahui = false;
                var fuentePrediosQuininde = false;
                var fuentePrediosLatacunga = false;
                var fuentePrediosManta = false;
                var fuentePrediosAmbato = false;
                var fuentePrediosIbarra = false;
                var fuentePrediosSanCristobal = false;
                var fuentePrediosDuran = false;
                var fuentePrediosLagoAgrio = false;
                var fuentePrediosSantaRosa = false;
                var fuentePrediosSucua = false;
                var fuentePrediosSigSig = false;
                var fuentePrediosMejia = false;
                var fuentePrediosMorona = false;
                var fuentePrediosTena = false;
                var fuentePrediosCatamayo = false;
                var fuentePrediosLoja = false;
                var fuentePrediosSamborondon = false;
                var fuentePrediosDaule = false;
                var fuentePrediosCayambe = false;
                var fuentePrediosAzogues = false;
                var fuentePrediosEsmeraldas = false;
                var fuentePrediosCotacachi = false;
                var fuenteFiscaliaDelitos = false;
                var fuenteUafe = false;
                var fuenteImpedimento = false;
                var fuenteTodas = false;
                var fuenteSriBasico = false;
                var fuenteSriHistorico = false;
                var fuenteCivilBasico = false;
                var fuenteCivilHistorico = false;
                var apiSri = new SriApiViewModel();
                var apiCivil = new CivilApiMetodoViewModel();
                var apiSocietario = new BalancesApiMetodoViewModel();
                var apiIess = new IessApiMetodoViewModel();
                var apiSenescyt = new SenescytApiViewModel();
                var apiLegal = new JudicialApiViewModel();
                var apiImpedimento = new ImpedimentoMetodoApiViewModel();
                var apiSercop = new SercopApiViewModel();
                var apiAnt = new AntApiViewModel();
                var apiPension = new PensionAlimenticiaApiViewModel();
                var apiSuperBancos = new SuperBancosApiViewModel();
                var apiAntecedentes = new AntecedentesPenalesApiViewModel();
                var apiFuerzasArmadas = new FuerzasArmadasApiViewModel();
                var apiDeNoBaja = new DeNoBajaApiViewModel();
                var apiPredios = new PrediosApiViewModel();
                var apiPrediosCuenca = new PrediosCuencaApiViewModel();
                var apiPrediosStoDomingo = new PrediosSantoDomingoApiViewModel();
                var apiPrediosRuminahui = new PrediosRuminahuiApiViewModel();
                var apiPrediosQuininde = new PrediosQuinindeApiViewModel();
                var apiPrediosLatacunga = new PrediosLatacungaApiViewModel();
                var apiPrediosManta = new PrediosMantaApiViewModel();
                var apiPrediosAmbato = new PrediosAmbatoApiViewModel();
                var apiPrediosIbarra = new PrediosIbarraApiViewModel();
                var apiPrediosSanCristobal = new PrediosSanCristobalApiViewModel();
                var apiPrediosDuran = new PrediosDuranApiViewModel();
                var apiPrediosLagoAgrio = new PrediosLagoAgrioApiViewModel();
                var apiPrediosSantaRosa = new PrediosSantaRosaApiViewModel();
                var apiPrediosSucua = new PrediosSucuaApiViewModel();
                var apiPrediosSigSig = new PrediosSigSigApiViewModel();
                var apiPrediosMejia = new PrediosMejiaApiViewModel();
                var apiPrediosMorona = new PrediosMoronaApiViewModel();
                var apiPrediosTena = new PrediosTenaApiViewModel();
                var apiPrediosCatamayo = new PrediosCatamayoApiViewModel();
                var apiPrediosLoja = new PrediosLojaApiViewModel();
                var apiPrediosSamborondon = new PrediosSamborondonApiViewModel();
                var apiPrediosDaule = new PrediosDauleApiViewModel();
                var apiPrediosCayambe = new PrediosCayambeApiViewModel();
                var apiPrediosAzogues = new PrediosAzoguesApiViewModel();
                var apiPrediosEsmeraldas = new PrediosEsmeraldasApiViewModel();
                var apiPrediosCotacachi = new PrediosCotacachiApiViewModel();
                var apiFiscaliaDelitos = new FiscaliaDelitosApiViewModel();
                var apiUafe = new UafeApiViewModel();
                var apiBuroCredito = new BuroCreditoMetodoViewModel();
                var apiEvaluacion = new List<CalificacionApiMetodoViewModel>();
                var fuentes = new[] { FuentesApi.TodasFuentes, FuentesApi.Societario, FuentesApi.Iess, FuentesApi.Sercop,
                                      FuentesApi.Legal,FuentesApi.Senescyt, FuentesApi.Civil,FuentesApi.Sri,
                                      FuentesApi.Ant, FuentesApi.PensionAlimenticia, FuentesApi.SuperBancos, FuentesApi.AntecedentesPenales, FuentesApi.FuerzasArmadas, FuentesApi.DeNoBaja,
                                      FuentesApi.Predios, FuentesApi.FiscaliaDelitos, FuentesApi.PrediosCuenca, FuentesApi.SriBasico,
                                      FuentesApi.SriHistorico, FuentesApi.CivilBasico, FuentesApi.CivilHistorico, FuentesApi.PrediosStoDomingo,
                                      FuentesApi.PrediosRuminahui, FuentesApi.PrediosQuininde, FuentesApi.PrediosLatacunga, FuentesApi.PrediosManta, FuentesApi.PrediosAmbato,
                                      FuentesApi.PrediosIbarra, FuentesApi.PrediosSanCristobal, FuentesApi.PrediosDuran, FuentesApi.PrediosLagoAgrio,
                                      FuentesApi.PrediosSantaRosa, FuentesApi.PrediosSucua, FuentesApi.PrediosSigSig, FuentesApi.PrediosMejia, FuentesApi.PrediosMorona,
                                      FuentesApi.PrediosTena,FuentesApi.PrediosCatamayo,FuentesApi.PrediosLoja, FuentesApi.PrediosSamborondon, FuentesApi.PrediosDaule,
                                      FuentesApi.PrediosCayambe, FuentesApi.PrediosAzogues, FuentesApi.PrediosEsmeraldas,FuentesApi.PrediosCotacachi, FuentesApi.Uafe, FuentesApi.Impedimento };
                CalificacionApiViewModel evaluacion = null;
                DetalleCalificacionApi detalleCalificacion = null;
                PrestamoApi prestamo = null;
                var porcentajeEmpresa = 0.00;
                var clasificacionEmpresa = string.Empty;
                var calificacionCliente = string.Empty;
                var fuentesSri = new[] { FuentesApi.Sri, FuentesApi.SriBasico, FuentesApi.SriHistorico };
                var fuentesCivil = new[] { FuentesApi.Civil, FuentesApi.CivilBasico, FuentesApi.CivilHistorico };
                var tipoDocumentoBLitoral = string.Empty;

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
                        return BadRequest(new
                        {
                            codigo = (short)Dominio.Tipos.ErroresApi.CredencialesFaltantes,
                            mensaje = "No se han ingresado las credenciales"
                        });

                    user = await _usuarios.FirstOrDefaultAsync(m => m, m => m.UserName == usuario, null, m => m.Include(m => m.Empresa.PlanesEmpresas).Include(m => m.Empresa.PlanesBuroCredito).Include(m => m.Empresa.PlanesEvaluaciones));
                    if (user == null)
                        return BadRequest(new
                        {
                            codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
                            mensaje = "El usuario no se encuentra registrado."
                        });

                    if (user.Estado != Dominio.Tipos.EstadosUsuarios.Activo)
                        return BadRequest(new
                        {
                            codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
                            mensaje = "Cuenta de usuario inactiva."
                        });

                    var passwordVerification = new PasswordHasher<Usuario>().VerifyHashedPassword(user, user.PasswordHash, clave);
                    if (passwordVerification == PasswordVerificationResult.Failed)
                        return BadRequest(new
                        {
                            codigo = (short)Dominio.Tipos.ErroresApi.CredencialesIncorrectas,
                            mensaje = "Credenciales incorrectas"
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    return Unauthorized(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.CredencialesIncorrectas,
                        mensaje = ex.Message
                    });
                }

                var usuarioActual = user;
                if (usuarioActual == null)
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
                        mensaje = "Cuenta de usuario inactiva."
                    });

                if (usuarioActual.Empresa.Estado != EstadosEmpresas.Activo)
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.UsuarioInactivo,
                        mensaje = "La empresa asociada al usuario no está activa"
                    });

                #region Permisos Clientes API Propia
                if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1790010937001)
                    return Unauthorized(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.NoAutorizado,
                        mensaje = "No autorizado para acceder a este recurso"
                    });
                #endregion Permisos Clientes API Propia

                if (modelo == null)
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.ParametrosFaltantes,
                        mensaje = "No se han ingresado los datos de la consulta."
                    });

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.IdentificacionInvalida,
                        mensaje = "La identificación ingresada no es válida."
                    });

                if (modelo.IdPadre == 0)
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.IdentificacionInvalida,
                        mensaje = "El ID Padre no se ha ingresado."
                    });

                //if (modelo.Fuente != null && fuentes.Intersect(modelo.Fuente).Count() != modelo.Fuente.Length)
                //    return BadRequest(new
                //    {
                //        codigo = (short)Dominio.Tipos.ErroresApi.FuentesIncorrectas,
                //        mensaje = "Uno o varios números no coinciden con ninguna Fuente Existente."
                //    });

                //if (modelo.Fuente != null && fuentesSri.Intersect(modelo.Fuente).Count() >= 2)
                //    return BadRequest(new
                //    {
                //        codigo = (short)Dominio.Tipos.ErroresApi.FuentesIncorrectas,
                //        mensaje = "Solo puede consultar una fuente para el SRI: 2, 201 o 202."
                //    });

                //if (modelo.Fuente != null && fuentesCivil.Intersect(modelo.Fuente).Count() >= 2)
                //    return BadRequest(new
                //    {
                //        codigo = (short)Dominio.Tipos.ErroresApi.FuentesIncorrectas,
                //        mensaje = "Solo puede consultar una fuente para Civil: 3, 301 o 302."
                //    });

                //if (modelo.Fuente == null)
                //    modelo.Fuente = new FuentesApi[] { FuentesApi.TodasFuentes };

                var historialCabecera = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdPadre, null, m => m.Include(i => i.Usuario), true);

                if (historialCabecera == null)
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.ParametrosFaltantes,
                        mensaje = "No se han ingresado los datos de la consulta."
                    });

                if (historialCabecera.Usuario.Id != usuarioActual.Id)
                    return Unauthorized(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.NoAutorizado,
                        mensaje = "No autorizado para acceder a este recurso"
                    });

                if (historialCabecera.Identificacion != modelo.Identificacion.Trim())
                    return BadRequest(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.ParametrosFaltantes,
                        mensaje = "La identificación ingresada no es igual a la del historial."
                    });

                if (DateTime.Now > historialCabecera.Fecha.AddHours(3))
                    return Unauthorized(new
                    {
                        codigo = (short)Dominio.Tipos.ErroresApi.ParametrosFaltantes,
                        mensaje = "No se puede realizar la actualización el tiempo de espera a caducado"
                    });

                var identificacionOriginal = modelo.Identificacion?.Trim();

                //fuenteTodas = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.TodasFuentes);
                //fuenteSocietario = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Societario);
                //fuenteIess = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Iess);
                //fuenteSercop = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Sercop);
                //fuenteJudicial = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Legal);
                //fuenteSenescyt = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Senescyt);
                //fuenteCivil = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Civil);
                //fuenteSri = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Sri);
                //fuenteAnt = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Ant);
                //fuentePension = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PensionAlimenticia);
                //fuenteSuperBancos = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.SuperBancos);
                //fuenteAntecedentes = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.AntecedentesPenales);
                //fuenteFuerzasArmadas = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.FuerzasArmadas);
                //fuenteDeNoBaja = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.DeNoBaja);
                //fuentePredios = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Predios);
                //fuenteFiscaliaDelitos = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.FiscaliaDelitos);
                //fuentePrediosCuenca = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCuenca);
                //fuentePrediosStoDomingo = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosStoDomingo);
                //fuentePrediosRuminahui = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosRuminahui);
                //fuentePrediosQuininde = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosQuininde);
                //fuentePrediosLatacunga = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosLatacunga);
                //fuentePrediosManta = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosManta);
                //fuentePrediosAmbato = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosAmbato);
                //fuentePrediosIbarra = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosIbarra);
                //fuentePrediosSanCristobal = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSanCristobal);
                //fuentePrediosDuran = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosDuran);
                //fuentePrediosLagoAgrio = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosLagoAgrio);
                //fuentePrediosSantaRosa = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSantaRosa);
                //fuentePrediosSucua = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSucua);
                //fuentePrediosSigSig = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSigSig);
                //fuentePrediosMejia = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosMejia);
                //fuentePrediosMorona = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosMorona);
                //fuentePrediosTena = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosTena);
                //fuentePrediosCatamayo = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCatamayo);
                //fuentePrediosLoja = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosLoja);
                //fuentePrediosSamborondon = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosSamborondon);
                //fuentePrediosDaule = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosDaule);
                //fuentePrediosCayambe = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCayambe);
                //fuentePrediosAzogues = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosAzogues);
                //fuentePrediosEsmeraldas = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosEsmeraldas);
                //fuentePrediosCotacachi = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.PrediosCotacachi);
                //fuenteSriBasico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.SriBasico);
                //fuenteSriHistorico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.SriHistorico);
                //fuenteCivilBasico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.CivilBasico);
                //fuenteCivilHistorico = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.CivilHistorico);
                //fuenteUafe = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Uafe);
                //fuenteImpedimento = modelo.Fuente.Contains(Dominio.Tipos.FuentesApi.Impedimento);

                var datos = new RespuestaActualizacionApiViewModel();

                datos.IdPadre = modelo.IdPadre;

                if (modelo.ConsultarBuro)
                {
                    if (!await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdPadre && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito))
                    {
                        _logger.LogInformation($"No se encontró en el historial la consulta del Buró de Crédito para el Id: {modelo.IdPadre}");
                        throw new Exception("La fuente Buró de Crédito no puede ser actualizada");
                    }

                    if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001)
                    {
                        var datosAutomotriz = new[]
                               {
                                    Dominio.Tipos.ParametrosClientes.LineaBLitoral,
                                    Dominio.Tipos.ParametrosClientes.TipoCreditoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.PlazoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.MontoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.IngresoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.GastoHogarBLitoral,
                                    Dominio.Tipos.ParametrosClientes.RestaGastoFinancieroBLitoral,
                                    Dominio.Tipos.ParametrosClientes.TipoDocumentoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.NumeroDocumentoBLitoral
                                };

                        var datosMicroFinanzas = new[]
                                {
                                    Dominio.Tipos.ParametrosClientes.TipoCreditoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.PlazoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.MontoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.IngresoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.GastoHogarBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.RestaGastoFinancieroBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.TipoDocumentoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.NumeroDocumentoBLitoralMicrofinanza
                                };

                        var parametrosBLitorial = await _parametrosClientesHistoriales.ReadAsync(x => x, x => x.IdHistorial == modelo.IdPadre, null, null, 0, null, true);
                        if (parametrosBLitorial != null && !datosAutomotriz.Except(parametrosBLitorial.Select(x => x.Parametro).ToList()).Any() && !parametrosBLitorial.Select(x => x.Parametro).ToList().Except(datosAutomotriz).Any())
                        {
                            apiBuroCredito = await ActualizarReporteBuroCredito(new ApiActualizarViewModel()
                            {
                                IdPadre = modelo.IdPadre,
                                Identificacion = identificacionOriginal,
                                IdUsuario = user.Id,
                                ModeloBuro = TipoBuroModelo.ModeloAutomotriz
                            });

                            datos.BuroCreditoAutomotriz = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null && apiBuroCredito.BuroCreditoEquifax.ResultadosBancoLitoral != null ? new BuroCreditoApi_0990981930001()
                            {
                                BuroCreditoEquifax = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null && apiBuroCredito.BuroCreditoEquifax.ResultadosBancoLitoral != null ? apiBuroCredito.BuroCreditoEquifax.ResultadosBancoLitoral : null
                            } : null;
                        }
                        else if (parametrosBLitorial != null && !datosMicroFinanzas.Except(parametrosBLitorial.Select(x => x.Parametro).ToList()).Any() && !parametrosBLitorial.Select(x => x.Parametro).ToList().Except(datosMicroFinanzas).Any())
                        {
                            apiBuroCredito = await ActualizarReporteBuroCredito(new ApiActualizarViewModel()
                            {
                                IdPadre = modelo.IdPadre,
                                Identificacion = identificacionOriginal,
                                IdUsuario = user.Id,
                                ModeloBuro = TipoBuroModelo.ModeloMicrofinanzas
                            });

                            datos.BuroCreditoMicrofinanza = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null && apiBuroCredito.BuroCreditoEquifax.ResultadosBancoLitoralMicrofinanza != null ? new BuroCreditoApiMicrofinanza_0990981930001()
                            {
                                BuroCreditoEquifax = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null && apiBuroCredito.BuroCreditoEquifax.ResultadosBancoLitoralMicrofinanza != null ? apiBuroCredito.BuroCreditoEquifax.ResultadosBancoLitoralMicrofinanza : null
                            } : null;
                        }
                        else
                        {
                            apiBuroCredito = await ActualizarReporteBuroCredito(new ApiActualizarViewModel()
                            {
                                IdPadre = modelo.IdPadre,
                                Identificacion = identificacionOriginal,
                                IdUsuario = user.Id,
                                ModeloBuro = TipoBuroModelo.ModeloCovid360
                            });

                            datos.BuroCredito = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null ? new BuroCreditoApiCovid360_0990981930001()
                            {
                                BuroCreditoEquifax = apiBuroCredito != null && apiBuroCredito.BuroCreditoEquifax != null ? apiBuroCredito.BuroCreditoEquifax : null
                            } : null;
                        }
                    }
                }

                return Json(datos, new JsonSerializerSettings
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
        private async Task<SriApiViewModel> ActualizarReporteSRI(ApiViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                if (!await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Sri))
                {
                    _logger.LogInformation($"No se encontró en el historial la consulta del SRI para el Id: {modelo.IdHistorial}");
                    throw new Exception("La fuente SRI no puede ser actualizada");
                }

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                List<Externos.Logica.Balances.Modelos.Similares> r_similares = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                Externos.Logica.Balances.Modelos.CatastroFantasma catastroFantasma = null;
                var cacheSri = false;
                var cacheContactosEmpresa = false;
                var cacheEmpSimilares = false;
                var cacheCatastrosFantasmas = false;
                var cedulaEntidades = false;
                var consultaFantasma = false;
                var impuestosRenta = new List<Externos.Logica.SRi.Modelos.Anexo>();
                var datos = new SriApiViewModel();

                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.Sri;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (cedulaEntidades)
                            {
                                switch (tipoFuente)
                                {
                                    case 1:
                                        r_sri = await _sri.GetRespuestaAsync(modelo.Identificacion);
                                        break;
                                    case 2:
                                        r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 3:
                                        r_sri = await _sri.GetEmpresaAccionistaAsync(modelo.Identificacion);
                                        if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                            r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);

                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 4:
                                        r_sri = await _sri.GetContribuyenteHistoricoAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;
                                        break;
                                    default:
                                        r_sri = await _sri.GetRespuestaAsync(modelo.Identificacion);
                                        break;
                                }
                                contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                if (r_sri != null)
                                {
                                    r_similares = await _balances.GetEmpresasSimilaresAsync(modelo.Identificacion);
                                    if (r_sri.Fantasma)
                                    {
                                        catastroFantasma = await _balances.GetCatastrosFantasmasAsync(modelo.Identificacion);
                                        consultaFantasma = true;
                                    }
                                }
                            }
                            else
                            {
                                switch (tipoFuente)
                                {
                                    case 1:
                                        r_sri = await _sri.GetRespuestaAsync(modelo.Identificacion);
                                        break;
                                    case 2:
                                        r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 3:
                                        r_sri = await _sri.GetEmpresaAccionistaAsync(modelo.Identificacion);
                                        if (r_sri != null && !string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                            r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);

                                        if (r_sri != null)
                                            cacheSri = true;

                                        break;
                                    case 4:
                                        r_sri = await _sri.GetContribuyenteHistoricoAsync(modelo.Identificacion);
                                        if (r_sri != null)
                                            cacheSri = true;
                                        break;
                                    default:
                                        r_sri = await _sri.GetRespuestaAsync(modelo.Identificacion);
                                        break;
                                }
                                contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                if (r_sri != null)
                                {
                                    r_similares = await _balances.GetEmpresasSimilaresAsync(modelo.Identificacion);
                                    if (r_sri.Fantasma)
                                    {
                                        catastroFantasma = await _balances.GetCatastrosFantasmasAsync(modelo.Identificacion);
                                        consultaFantasma = true;
                                    }
                                }
                            }

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                            {
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);
                                if (string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                    r_sri.AgenteRepresentante = await _balances.GetRepresentanteLegalEmpresaAccionistaAsync(modelo.Identificacion);

                                if (!string.IsNullOrEmpty(r_sri.AgenteRepresentante))
                                    r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);
                            }
                        }

                        if (r_sri != null)
                        {
                            if (r_sri.Anexos != null && r_sri.Anexos.Any())
                                impuestosRenta.AddRange(r_sri.Anexos.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
                                {
                                    Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
                                    Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
                                    Formulario = m.Formulario,
                                    Periodo = m.Periodo
                                }).ToList());

                            if (r_sri.Rentas != null && r_sri.Rentas.Any())
                            {
                                if (impuestosRenta.Any())
                                {
                                    foreach (var item in impuestosRenta)
                                    {
                                        var impuesto = r_sri.Rentas.FirstOrDefault(m => m.Periodo == item.Periodo);
                                        if (impuesto != null && impuesto.Causado.HasValue && impuesto.Causado >= 0)
                                        {
                                            item.Formulario = impuesto.Formulario;
                                            item.Causado = impuesto.Causado;
                                        }

                                        if (impuesto != null && impuesto.Formulario != item.Formulario)
                                            item.Formulario = impuesto.Formulario;
                                    }
                                }
                                else
                                {
                                    impuestosRenta.AddRange(r_sri.Rentas.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
                                    {
                                        Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
                                        Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
                                        Formulario = m.Formulario,
                                        Periodo = m.Periodo
                                    }).ToList());
                                }
                            }

                            if (r_sri.Divisas != null && r_sri.Divisas.Any())
                            {
                                if (impuestosRenta.Any())
                                {
                                    foreach (var item in impuestosRenta)
                                    {
                                        var impuesto = r_sri.Divisas.FirstOrDefault(m => m.Periodo == item.Periodo);
                                        if (impuesto != null && impuesto.Divisas.HasValue && impuesto.Divisas >= 0)
                                            item.Divisas = impuesto.Divisas;
                                    }
                                }
                                else
                                {
                                    impuestosRenta.AddRange(r_sri.Divisas.Select(m => new Externos.Logica.SRi.Modelos.Anexo()
                                    {
                                        Causado = m.Causado.HasValue ? m.Causado.Value : 0d,
                                        Divisas = m.Divisas.HasValue ? m.Divisas.Value : 0d,
                                        Formulario = m.Formulario,
                                        Periodo = m.Periodo
                                    }).ToList());
                                }
                            }

                            r_sri.Anexos = impuestosRenta.OrderByDescending(m => m.Periodo).ToList();
                        }

                        if (r_sri == null)
                        {
                            var datosDetalleSri = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleSri != null)
                            {
                                cacheSri = true;
                                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
                            }
                        }
                        if (contactosEmpresa == null)
                        {
                            var datosDetalleContEmpresa = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleContEmpresa != null)
                            {
                                cacheContactosEmpresa = true;
                                contactosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContEmpresa);
                            }
                        }
                        if (r_similares == null)
                        {
                            var datosDetalleEmpSimilares = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasSimilares && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleEmpSimilares != null)
                            {
                                cacheEmpSimilares = true;
                                r_similares = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Similares>>(datosDetalleEmpSimilares);
                            }
                        }
                        if (catastroFantasma == null && consultaFantasma)
                        {
                            var datosDetalleCatastroFantasma = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.CatastroFantasma && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleCatastroFantasma != null)
                            {
                                cacheCatastrosFantasmas = true;
                                catastroFantasma = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.CatastroFantasma>(datosDetalleCatastroFantasma);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
                    var pathContactoEmpresa = Path.Combine(pathFuentes, "sriContactoEmpresaDemo.json");
                    var pathSimilares = Path.Combine(pathFuentes, "sriSimilaresDemo.json");
                    var pathCatastroFantasma = Path.Combine(pathFuentes, "sriCatastrosDemo.json");
                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));
                    r_similares = JsonConvert.DeserializeObject<List<Externos.Logica.Balances.Modelos.Similares>>(System.IO.File.ReadAllText(pathSimilares));
                    contactosEmpresa = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(System.IO.File.ReadAllText(pathContactoEmpresa));
                    catastroFantasma = JsonConvert.DeserializeObject<Externos.Logica.Balances.Modelos.CatastroFantasma>(System.IO.File.ReadAllText(pathCatastroFantasma));
                }

                datos = new SriApiViewModel()
                {
                    Empresa = r_sri,
                    Contactos = contactosEmpresa,
                    EmpresasSimilares = r_similares,
                    CatastroFantasma = catastroFantasma
                };

                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesEmpresas = new[] { Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.ContactosEmpresa, Dominio.Tipos.Fuentes.EmpresasSimilares, Dominio.Tipos.Fuentes.CatastroFantasma };
                        var historialesEmpresa = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesEmpresas.Contains(m.TipoFuente));
                        var historialSri = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri);
                        var historialContEmpresas = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa);
                        var historialEmpSimilares = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.EmpresasSimilares);
                        var historialCatastroFantasma = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.CatastroFantasma);

                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
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
                                else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
                                    historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
                            }
                            await _historiales.UpdateAsync(historial);
                            if (historialConsolidado != null)
                            {
                                historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                                historialConsolidado.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidado);
                            }
                        }

                        if (historialSri != null && !historialSri.Generado)
                        {
                            historialSri.IdHistorial = modelo.IdHistorial;
                            historialSri.TipoFuente = Dominio.Tipos.Fuentes.Sri;
                            historialSri.Generado = datos.Empresa != null;
                            historialSri.Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null;
                            historialSri.Cache = cacheSri;
                            historialSri.FechaRegistro = DateTime.Now;
                            historialSri.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSri);
                            _logger.LogInformation("Historial de la Fuente SRI actualizado correctamente");
                        }

                        if (historialContEmpresas != null && !historialContEmpresas.Generado)
                        {
                            historialContEmpresas.IdHistorial = modelo.IdHistorial;
                            historialContEmpresas.TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa;
                            historialContEmpresas.Generado = datos.Contactos != null;
                            historialContEmpresas.Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null;
                            historialContEmpresas.Cache = cacheContactosEmpresa;
                            historialContEmpresas.FechaRegistro = DateTime.Now;
                            historialContEmpresas.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContEmpresas);
                            _logger.LogInformation("Historial de la Fuente Contactos Empresa actualizado correctamente");
                        }

                        if (historialEmpSimilares != null && !historialEmpSimilares.Generado)
                        {
                            historialEmpSimilares.IdHistorial = modelo.IdHistorial;
                            historialEmpSimilares.TipoFuente = Dominio.Tipos.Fuentes.EmpresasSimilares;
                            historialEmpSimilares.Generado = datos.EmpresasSimilares != null;
                            historialEmpSimilares.Data = datos.EmpresasSimilares != null ? JsonConvert.SerializeObject(datos.EmpresasSimilares) : null;
                            historialEmpSimilares.Cache = cacheEmpSimilares;
                            historialEmpSimilares.FechaRegistro = DateTime.Now;
                            historialEmpSimilares.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialEmpSimilares);
                            _logger.LogInformation("Historial de la Fuente Empresas Similares actualizado correctamente");
                        }

                        if (consultaFantasma)
                        {
                            if (historialCatastroFantasma != null && !historialCatastroFantasma.Generado)
                            {
                                historialCatastroFantasma.IdHistorial = modelo.IdHistorial;
                                historialCatastroFantasma.TipoFuente = Dominio.Tipos.Fuentes.CatastroFantasma;
                                historialCatastroFantasma.Generado = datos.CatastroFantasma != null;
                                historialCatastroFantasma.Data = datos.CatastroFantasma != null ? JsonConvert.SerializeObject(datos.CatastroFantasma) : null;
                                historialCatastroFantasma.Cache = cacheCatastrosFantasmas;
                                historialCatastroFantasma.FechaRegistro = DateTime.Now;
                                historialCatastroFantasma.Reintento = true;
                                await _detallesHistorial.ActualizarDetalleHistorialAsync(historialCatastroFantasma);
                                _logger.LogInformation("Historial de la Fuente Catastros Fantasmas actualizado correctamente");
                            }
                        }
                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<SriApiViewModel> ActualizarReporteSRIBasico(ApiViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                if (!await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Sri))
                {
                    _logger.LogInformation($"No se encontró en el historial la consulta del SRI para el Id: {modelo.IdHistorial}");
                    throw new Exception("La fuente SRI no puede ser actualizada");
                }

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                var cacheSri = false;
                var cacheContactosEmpresas = false;
                var cedulaEntidades = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                            cedulaEntidades = true;
                        }

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            r_sri = await _sri.GetContribuyenteBasico360Sri(modelo.Identificacion);
                            if (r_sri != null)
                                cacheSri = true;

                            contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                            {
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);
                                r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);
                            }
                        }

                        if (r_sri == null)
                        {
                            var datosDetalleSri = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleSri != null)
                            {
                                cacheSri = true;
                                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
                            }
                        }

                        #region Normalizacion Contactos
                        var cedulaAux = string.Empty;
                        if (cedulaEntidades)
                            cedulaAux = modelo.Identificacion.Substring(0, 10);
                        else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion) && r_sri != null && !string.IsNullOrEmpty(r_sri.RepresentanteLegal))
                            cedulaAux = r_sri.RepresentanteLegal;
                        else
                            cedulaAux = modelo.Identificacion;

                        if (r_sri != null && !string.IsNullOrEmpty(cedulaAux))
                        {
                            var contactosCedula = await _garancheck.GetContactoAsync(cedulaAux);
                            if (contactosEmpresa != null)
                            {
                                if (contactosCedula != null)
                                {
                                    if (contactosCedula.Direcciones != null && contactosCedula.Direcciones.Any())
                                    {
                                        if (contactosEmpresa.Direcciones != null && contactosEmpresa.Direcciones.Any())
                                        {
                                            contactosEmpresa.Direcciones.AddRange(contactosCedula.Direcciones);
                                            contactosEmpresa.Direcciones = contactosEmpresa.Direcciones.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Direcciones = contactosCedula.Direcciones.ToList();
                                    }

                                    if (contactosCedula.Telefonos != null && contactosCedula.Telefonos.Any())
                                    {
                                        if (contactosEmpresa.Telefonos != null && contactosEmpresa.Telefonos.Any())
                                        {
                                            contactosEmpresa.Telefonos.AddRange(contactosCedula.Telefonos);
                                            contactosEmpresa.Telefonos = contactosEmpresa.Telefonos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Telefonos = contactosCedula.Telefonos.ToList();
                                    }

                                    if (contactosCedula.Correos != null && contactosCedula.Correos.Any())
                                    {
                                        if (contactosEmpresa.Correos != null && contactosEmpresa.Correos.Any())
                                        {
                                            contactosEmpresa.Correos.AddRange(contactosCedula.Correos);
                                            contactosEmpresa.Correos = contactosEmpresa.Correos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Correos = contactosCedula.Correos.ToList();
                                    }
                                }
                            }
                            else if (contactosCedula != null)
                                contactosEmpresa = contactosCedula;
                        }
                        #endregion Normalizacion Contactos
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));

                }
                var datos = new SriApiViewModel()
                {
                    Empresa = r_sri,
                    Contactos = contactosEmpresa
                };
                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesEmpresas = new[] { Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.ContactosEmpresa, Dominio.Tipos.Fuentes.EmpresasSimilares, Dominio.Tipos.Fuentes.CatastroFantasma };
                        var historialesEmpresa = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesEmpresas.Contains(m.TipoFuente));
                        var historialSri = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri);
                        var historialContEmpresas = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa);

                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
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
                                else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
                                    historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
                            }
                            await _historiales.UpdateAsync(historial);
                            if (historialConsolidado != null)
                            {
                                historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                                historialConsolidado.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidado);
                            }
                        }

                        if (historialSri != null && !historialSri.Generado)
                        {
                            historialSri.IdHistorial = modelo.IdHistorial;
                            historialSri.TipoFuente = Dominio.Tipos.Fuentes.Sri;
                            historialSri.Generado = datos.Empresa != null;
                            historialSri.Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null;
                            historialSri.Cache = cacheSri;
                            historialSri.FechaRegistro = DateTime.Now;
                            historialSri.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSri);
                            _logger.LogInformation("Historial de la Fuente SRI actualizado correctamente");
                        }

                        if (historialContEmpresas != null && !historialContEmpresas.Generado)
                        {
                            historialContEmpresas.IdHistorial = modelo.IdHistorial;
                            historialContEmpresas.TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa;
                            historialContEmpresas.Generado = datos.Contactos != null;
                            historialContEmpresas.Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null;
                            historialContEmpresas.Cache = cacheContactosEmpresas;
                            historialContEmpresas.FechaRegistro = DateTime.Now;
                            historialContEmpresas.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContEmpresas);
                            _logger.LogInformation("Historial de la Fuente Contactos Empresa actualizado correctamente");
                        }
                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<SriApiViewModel> ActualizarReporteSRIHistorico(ApiViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                if (!await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Sri))
                {
                    _logger.LogInformation($"No se encontró en el historial la consulta del SRI para el Id: {modelo.IdHistorial}");
                    throw new Exception("La fuente SRI no puede ser actualizada");
                }

                modelo.Identificacion = modelo.Identificacion.Trim();
                var identificacionOriginal = modelo.Identificacion;
                Externos.Logica.SRi.Modelos.Contribuyente r_sri = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosEmpresa = null;
                var cacheSri = false;
                var cacheContactosEmpresa = false;
                var cedulaEntidades = false;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente SRI identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                            cedulaEntidades = true;
                        }

                        if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                            {
                                var directorioEmpresa = _balances.GetDirectorioCompanias(modelo.Identificacion);
                                if (directorioEmpresa != null)
                                {
                                    var direccion = new[] { directorioEmpresa.Provincia, directorioEmpresa.Canton, directorioEmpresa.Ciudad, directorioEmpresa.Calle, directorioEmpresa.Numero, directorioEmpresa.Interseccion };
                                    r_sri = new Contribuyente()
                                    {
                                        RUC = directorioEmpresa.Ruc,
                                        RazonSocial = directorioEmpresa.Nombre,
                                        FechaInicio = directorioEmpresa.FechaConstitucion.HasValue ? directorioEmpresa.FechaConstitucion.Value : DateTime.Now,
                                        AgenteRepresentante = directorioEmpresa.Representante,
                                        Direccion = string.Join(" / ", direccion.Where(m => !string.IsNullOrEmpty(m))),
                                        Estado = directorioEmpresa.SituacionLegal == "ACTIVA" ? "ACT" : "SDE",
                                        EstadoContribuyente = directorioEmpresa.SituacionLegal == "ACTIVA" ? "ACTIVO" : "SUSPENDIDO",
                                        Clase = "OTROS",
                                        Tipo = "SOCIEDAD",
                                        PersonaSociedad = "SCD",
                                        Subtipo = "BAJO CONTROL DE LA SUPERINTENDENCIA DE COMPAÑIAS"
                                    };
                                }
                                else
                                    r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);
                            }
                            else
                                r_sri = await _sri.GetCatastroAsync(modelo.Identificacion);

                            if (r_sri != null)
                                cacheSri = true;

                            contactosEmpresa = await _garancheck.GetContactoAsync(modelo.Identificacion);
                            if (r_sri != null && string.IsNullOrEmpty(r_sri.AgenteRepresentante) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                                r_sri.AgenteRepresentante = await _balances.GetNombreRepresentanteCompaniasAsync(modelo.Identificacion);

                            if (r_sri != null && string.IsNullOrEmpty(r_sri.RepresentanteLegal) && (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion)))
                                r_sri.RepresentanteLegal = await _garancheck.GetCedulaRepresentanteAsync(r_sri.AgenteRepresentante);
                        }

                        if (r_sri == null)
                        {
                            var datosDetalleSri = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == identificacionOriginal && m.TipoFuente == Dominio.Tipos.Fuentes.Sri && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleSri != null)
                            {
                                cacheSri = true;
                                r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(datosDetalleSri);
                            }
                        }

                        #region Normalizacion Contactos
                        var cedulaAux = string.Empty;
                        if (cedulaEntidades)
                            cedulaAux = modelo.Identificacion.Substring(0, 10);
                        else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion) && r_sri != null && !string.IsNullOrEmpty(r_sri.RepresentanteLegal))
                            cedulaAux = r_sri.RepresentanteLegal;
                        else
                            cedulaAux = modelo.Identificacion.Substring(0, 10);

                        if (r_sri != null && !string.IsNullOrEmpty(cedulaAux))
                        {
                            var contactosCedula = await _garancheck.GetContactoAsync(cedulaAux);
                            if (contactosEmpresa != null)
                            {
                                if (contactosCedula != null)
                                {
                                    if (contactosCedula.Direcciones != null && contactosCedula.Direcciones.Any())
                                    {
                                        if (contactosEmpresa.Direcciones != null && contactosEmpresa.Direcciones.Any())
                                        {
                                            contactosEmpresa.Direcciones.AddRange(contactosCedula.Direcciones);
                                            contactosEmpresa.Direcciones = contactosEmpresa.Direcciones.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Direcciones = contactosCedula.Direcciones.ToList();
                                    }

                                    if (contactosCedula.Telefonos != null && contactosCedula.Telefonos.Any())
                                    {
                                        if (contactosEmpresa.Telefonos != null && contactosEmpresa.Telefonos.Any())
                                        {
                                            contactosEmpresa.Telefonos.AddRange(contactosCedula.Telefonos);
                                            contactosEmpresa.Telefonos = contactosEmpresa.Telefonos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Telefonos = contactosCedula.Telefonos.ToList();
                                    }

                                    if (contactosCedula.Correos != null && contactosCedula.Correos.Any())
                                    {
                                        if (contactosEmpresa.Correos != null && contactosEmpresa.Correos.Any())
                                        {
                                            contactosEmpresa.Correos.AddRange(contactosCedula.Correos);
                                            contactosEmpresa.Correos = contactosEmpresa.Correos.Distinct().ToList();
                                        }
                                        else
                                            contactosEmpresa.Correos = contactosCedula.Correos.ToList();
                                    }
                                }
                            }
                            else if (contactosCedula != null)
                                contactosEmpresa = contactosCedula;
                        }
                        #endregion Normalizacion Contactos
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente SRI con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathSri = Path.Combine(pathFuentes, "sriDemo.json");
                    r_sri = JsonConvert.DeserializeObject<Externos.Logica.SRi.Modelos.Contribuyente>(System.IO.File.ReadAllText(pathSri));

                }
                var datos = new SriApiViewModel()
                {
                    Empresa = r_sri,
                    Contactos = contactosEmpresa
                };
                _logger.LogInformation("Fuente de SRI procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente SRI. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesEmpresas = new[] { Dominio.Tipos.Fuentes.Sri, Dominio.Tipos.Fuentes.ContactosEmpresa, Dominio.Tipos.Fuentes.EmpresasSimilares, Dominio.Tipos.Fuentes.CatastroFantasma };
                        var historialesEmpresa = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesEmpresas.Contains(m.TipoFuente));
                        var historialSri = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Sri);
                        var historialContEmpresas = historialesEmpresa.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosEmpresa);

                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        var historialConsolidado = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (r_sri != null && !string.IsNullOrEmpty(r_sri.RUC?.Trim()) && (!string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) || !string.IsNullOrEmpty(r_sri.NombreComercial?.Trim())))
                            {
                                historial.RazonSocialEmpresa = !string.IsNullOrEmpty(r_sri.RazonSocial?.Trim()) ? r_sri.RazonSocial?.Trim().ToUpper() : r_sri.NombreComercial?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.RucJuridico && historial.TipoIdentificacion != Dominio.Constantes.General.RucNatural && historial.TipoIdentificacion != Dominio.Constantes.General.SectorPublico)
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
                                else if (r_sri != null && ValidacionViewModel.ValidarRuc(r_sri.RUC) && string.IsNullOrEmpty(historial.IdentificacionSecundaria))
                                    historial.IdentificacionSecundaria = r_sri.RUC.Substring(0, 10);
                            }
                            await _historiales.UpdateAsync(historial);
                            if (historialConsolidado != null)
                            {
                                historialConsolidado.RazonSocial = historial.RazonSocialEmpresa;
                                historialConsolidado.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidado);
                            }
                        }

                        if (historialSri != null && !historialSri.Generado)
                        {
                            historialSri.IdHistorial = modelo.IdHistorial;
                            historialSri.TipoFuente = Dominio.Tipos.Fuentes.Sri;
                            historialSri.Generado = datos.Empresa != null;
                            historialSri.Data = datos.Empresa != null ? JsonConvert.SerializeObject(datos.Empresa) : null;
                            historialSri.Cache = cacheSri;
                            historialSri.FechaRegistro = DateTime.Now;
                            historialSri.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialSri);
                            _logger.LogInformation("Historial de la Fuente SRI actualizado correctamente");
                        }

                        if (historialContEmpresas != null && !historialContEmpresas.Generado)
                        {
                            historialContEmpresas.IdHistorial = modelo.IdHistorial;
                            historialContEmpresas.TipoFuente = Dominio.Tipos.Fuentes.ContactosEmpresa;
                            historialContEmpresas.Generado = datos.Contactos != null;
                            historialContEmpresas.Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null;
                            historialContEmpresas.Cache = cacheContactosEmpresa;
                            historialContEmpresas.FechaRegistro = DateTime.Now;
                            historialContEmpresas.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContEmpresas);
                            _logger.LogInformation("Historial de la Fuente Contactos Empresa actualizado correctamente");
                        }
                        _logger.LogInformation("Historial de la Fuente SRI procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<CivilApiMetodoViewModel> ActualizarReporteCivil(ApiViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                var detalleRegistroCivil = await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil);
                var detalleGarancheck = await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Personales);

                if (!detalleRegistroCivil && !detalleGarancheck)
                {
                    _logger.LogInformation($"No se encontró en el historial la consulta de Civil para el Id: {modelo.IdHistorial}");
                    throw new Exception("La fuente SRI no puede ser actualizada");
                }

                modelo.Identificacion = modelo.Identificacion.Trim();

                Externos.Logica.Garancheck.Modelos.Persona r_garancheck = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactos = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactosIess = null;
                Externos.Logica.Garancheck.Modelos.Personal datosPersonal = null;
                Externos.Logica.Garancheck.Modelos.Familia familiares = null;
                Externos.Logica.Garancheck.Modelos.RegistroCivil registroCivil = null;
                Externos.Logica.RegistroCivilWS.Modelos.RegistroCivil r_registroWS = null;
                var datos = new CivilApiMetodoViewModel();
                Historial historialTemp = null;
                ReporteConsolidado historialConsolidadoTemp = null;
                var cedulaEntidades = false;
                var cacheCivil = false;
                var cachePersonal = false;
                var cacheContactos = false;
                var cacheContactosIess = false;
                var cacheRegistroCivil = false;
                var cacheFamiliares = false;
                var pathTipoFuente = Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.RegistroCivil;

                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Civil identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            cedulaEntidades = true;
                            modelo.Identificacion = $"{modelo.Identificacion}001";
                        }

                        historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, n => n.Include(t => t.PlanEmpresa), true);
                        historialConsolidadoTemp = await _reporteConsolidado.FirstOrDefaultAsync(m => m, m => m.HistorialId == modelo.IdHistorial);
                        //CONSULTA CHEVYPLAN
                        if (historialTemp.PlanEmpresa.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1791927966001)
                        {
                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                if (cedulaEntidades)
                                {
                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
                                        contactos = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                        contactosIess = await _garancheck.GetContactoAfiliadoAsync(modelo.Identificacion);
                                        datosPersonal = await _garancheck.GetInformacionPersonalAsync(modelo.Identificacion);
                                        r_registroWS = await _registroWS.GetRespuestaChevyplan(modelo.Identificacion);

                                        if (r_registroWS != null)
                                            registroCivil = new Externos.Logica.Garancheck.Modelos.RegistroCivil()
                                            {
                                                Cedula = r_registroWS.Cedula,
                                                Nombre = r_registroWS.Nombre,
                                                Genero = r_registroWS.Genero,
                                                FechaNacimiento = r_registroWS.FechaNacimiento,
                                                EstadoCivil = r_registroWS.EstadoCivil,
                                                Conyuge = r_registroWS.Conyuge,
                                                CedulaConyuge = r_registroWS.CedulaConyuge,
                                                Nacionalidad = r_registroWS.Nacionalidad,
                                                FechaCedulacion = r_registroWS.FechaCedulacion,
                                                CedulaPadre = r_registroWS.CedulaPadre,
                                                NombrePadre = r_registroWS.NombrePadre,
                                                CedulaMadre = r_registroWS.CedulaMadre,
                                                NombreMadre = r_registroWS.NombreMadre,
                                                Instruccion = r_registroWS.Instruccion,
                                                Profesion = r_registroWS.Profesion
                                            };
                                    }
                                }
                                else
                                {
                                    if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                        {
                                            if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                            {
                                                contactos = await _garancheck.GetContactoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                contactosIess = await _garancheck.GetContactoAfiliadoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                datosPersonal = await _garancheck.GetInformacionPersonalAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                r_registroWS = await _registroWS.GetRespuestaChevyplan(historialTemp.IdentificacionSecundaria.Trim());

                                                if (r_registroWS != null)
                                                    registroCivil = new Externos.Logica.Garancheck.Modelos.RegistroCivil()
                                                    {
                                                        Cedula = r_registroWS.Cedula,
                                                        Nombre = r_registroWS.Nombre,
                                                        Genero = r_registroWS.Genero,
                                                        FechaNacimiento = r_registroWS.FechaNacimiento,
                                                        EstadoCivil = r_registroWS.EstadoCivil,
                                                        Conyuge = r_registroWS.Conyuge,
                                                        CedulaConyuge = r_registroWS.CedulaConyuge,
                                                        Nacionalidad = r_registroWS.Nacionalidad,
                                                        FechaCedulacion = r_registroWS.FechaCedulacion,
                                                        CedulaPadre = r_registroWS.CedulaPadre,
                                                        NombrePadre = r_registroWS.NombrePadre,
                                                        CedulaMadre = r_registroWS.CedulaMadre,
                                                        NombreMadre = r_registroWS.NombreMadre,
                                                        Instruccion = r_registroWS.Instruccion,
                                                        Profesion = r_registroWS.Profesion
                                                    };
                                            }
                                        }
                                    }
                                    else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                    {
                                        var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                        if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        {
                                            contactos = await _garancheck.GetContactoAsync(cedulaTemp);
                                            contactosIess = await _garancheck.GetContactoAfiliadoAsync(cedulaTemp);
                                            datosPersonal = await _garancheck.GetInformacionPersonalAsync(cedulaTemp);
                                            r_registroWS = await _registroWS.GetRespuestaChevyplan(cedulaTemp);

                                            if (r_registroWS != null)
                                                registroCivil = new Externos.Logica.Garancheck.Modelos.RegistroCivil()
                                                {
                                                    Cedula = r_registroWS.Cedula,
                                                    Nombre = r_registroWS.Nombre,
                                                    Genero = r_registroWS.Genero,
                                                    FechaNacimiento = r_registroWS.FechaNacimiento,
                                                    EstadoCivil = r_registroWS.EstadoCivil,
                                                    Conyuge = r_registroWS.Conyuge,
                                                    CedulaConyuge = r_registroWS.CedulaConyuge,
                                                    Nacionalidad = r_registroWS.Nacionalidad,
                                                    FechaCedulacion = r_registroWS.FechaCedulacion,
                                                    CedulaPadre = r_registroWS.CedulaPadre,
                                                    NombrePadre = r_registroWS.NombrePadre,
                                                    CedulaMadre = r_registroWS.CedulaMadre,
                                                    NombreMadre = r_registroWS.NombreMadre,
                                                    Instruccion = r_registroWS.Instruccion,
                                                    Profesion = r_registroWS.Profesion
                                                };
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                            {
                                if (cedulaEntidades)
                                {
                                    modelo.Identificacion = modelo.Identificacion.Substring(0, 10);
                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
                                        contactos = await _garancheck.GetContactoAsync(modelo.Identificacion);
                                        contactosIess = await _garancheck.GetContactoAfiliadoAsync(modelo.Identificacion);
                                        datosPersonal = await _garancheck.GetInformacionPersonalAsync(modelo.Identificacion);

                                        switch (tipoFuente)
                                        {
                                            case 1:
                                                registroCivil = await _garancheck.GetRegistroCivilLineaAsync(modelo.Identificacion);
                                                if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                }
                                                else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                    registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                    registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                    registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                }

                                                if (registroCivil == null)
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);

                                                if (registroCivil != null)
                                                    familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                if (familiares == null)
                                                    familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);

                                                break;
                                            case 2:
                                                r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);
                                                if (datosPersonal != null)
                                                    familiares = await _garancheck.GetFamiliaLineaAsync(modelo.Identificacion, datosPersonal.CedulaConyuge, datosPersonal.NombreConyuge, datosPersonal.CedMadre, datosPersonal.NombreMadre, datosPersonal.CedPadre, datosPersonal.NombrePadre);

                                                if (familiares == null)
                                                    familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);

                                                break;
                                            case 3:
                                                registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(modelo.Identificacion);
                                                if (registroCivil != null)
                                                    cacheCivil = true;
                                                familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);
                                                break;
                                            case 4:
                                                registroCivil = await _garancheck.GetRegistroCivilLineaAsync(modelo.Identificacion);
                                                if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                }
                                                else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                {
                                                    registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                    registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                    registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                    registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                    registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                }

                                                if (registroCivil == null)
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);

                                                if (registroCivil != null)
                                                    familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                if (familiares == null)
                                                    familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);

                                                break;
                                            default:
                                                r_garancheck = await _garancheck.GetRespuestaAsync(modelo.Identificacion);
                                                familiares = await _garancheck.GetFamiliaAsync(modelo.Identificacion);
                                                break;
                                        }
                                    }
                                }
                                else
                                {

                                    if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
                                        if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                        {
                                            if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                            {
                                                contactos = await _garancheck.GetContactoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                contactosIess = await _garancheck.GetContactoAfiliadoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                datosPersonal = await _garancheck.GetInformacionPersonalAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                switch (tipoFuente)
                                                {
                                                    case 1:
                                                        registroCivil = await _garancheck.GetRegistroCivilLineaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                        {
                                                            registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                            registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                        }
                                                        else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                        {
                                                            registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                            registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                            registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                            registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                            registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                            registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                        }

                                                        if (registroCivil == null)
                                                            r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                        if (registroCivil != null)
                                                            familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                        if (familiares == null)
                                                            familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                        break;
                                                    case 2:
                                                        r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        if (datosPersonal != null)
                                                            familiares = await _garancheck.GetFamiliaLineaAsync(historialTemp.IdentificacionSecundaria.Trim(), datosPersonal.CedulaConyuge, datosPersonal.NombreConyuge, datosPersonal.CedMadre, datosPersonal.NombreMadre, datosPersonal.CedPadre, datosPersonal.NombrePadre);

                                                        if (familiares == null)
                                                            familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                        break;
                                                    case 3:
                                                        registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        if (registroCivil != null)
                                                            cacheCivil = true;
                                                        familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        break;
                                                    case 4:
                                                        registroCivil = await _garancheck.GetRegistroCivilLineaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                        {
                                                            registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                            registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                        }
                                                        else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                        {
                                                            registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                            registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                            registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                            registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                            registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                            registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                        }

                                                        if (registroCivil == null)
                                                            r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                        if (registroCivil != null)
                                                            familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                        if (familiares == null)
                                                            familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());

                                                        break;
                                                    default:
                                                        r_garancheck = await _garancheck.GetRespuestaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        familiares = await _garancheck.GetFamiliaAsync(historialTemp.IdentificacionSecundaria.Trim());
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                                    {
                                        var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                        if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                        {
                                            contactos = await _garancheck.GetContactoAsync(cedulaTemp);
                                            contactosIess = await _garancheck.GetContactoAfiliadoAsync(cedulaTemp);
                                            datosPersonal = await _garancheck.GetInformacionPersonalAsync(cedulaTemp);

                                            switch (tipoFuente)
                                            {
                                                case 1:
                                                    registroCivil = await _garancheck.GetRegistroCivilLineaAsync(cedulaTemp);
                                                    if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    }
                                                    else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                        registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                        registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                        registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                        registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                    }

                                                    if (registroCivil == null)
                                                        r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);

                                                    if (registroCivil != null)
                                                        familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                    if (familiares == null)
                                                        familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);

                                                    break;
                                                case 2:
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);
                                                    if (datosPersonal != null)
                                                        familiares = await _garancheck.GetFamiliaLineaAsync(cedulaTemp, datosPersonal.CedulaConyuge, datosPersonal.NombreConyuge, datosPersonal.CedMadre, datosPersonal.NombreMadre, datosPersonal.CedPadre, datosPersonal.NombrePadre);

                                                    if (familiares == null)
                                                        familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);

                                                    break;
                                                case 3:
                                                    registroCivil = await _garancheck.GetRegistroCivilHistoricoAsync(cedulaTemp);
                                                    if (registroCivil != null)
                                                        cacheCivil = true;

                                                    familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);
                                                    break;
                                                case 4:
                                                    registroCivil = await _garancheck.GetRegistroCivilLineaAsync(cedulaTemp);
                                                    if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) && !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                    }
                                                    else if (registroCivil != null && (string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) || string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) || !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim())))
                                                    {
                                                        registroCivil.CedulaConyuge = !string.IsNullOrEmpty(registroCivil.CedulaConyuge?.Trim()) ? registroCivil.CedulaConyuge.Trim() : string.Empty;
                                                        registroCivil.Conyuge = !string.IsNullOrEmpty(registroCivil.Conyuge?.Trim()) ? registroCivil.Conyuge.Trim() : string.Empty;
                                                        registroCivil.CedulaMadre = !string.IsNullOrEmpty(registroCivil.CedulaMadre?.Trim()) ? registroCivil.CedulaMadre.Trim() : string.Empty;
                                                        registroCivil.NombreMadre = !string.IsNullOrEmpty(registroCivil.NombreMadre?.Trim()) ? registroCivil.NombreMadre.Trim() : string.Empty;
                                                        registroCivil.CedulaPadre = !string.IsNullOrEmpty(registroCivil.CedulaPadre?.Trim()) ? registroCivil.CedulaPadre.Trim() : string.Empty;
                                                        registroCivil.NombrePadre = !string.IsNullOrEmpty(registroCivil.NombrePadre?.Trim()) ? registroCivil.NombrePadre.Trim() : string.Empty;
                                                    }

                                                    if (registroCivil == null)
                                                        r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);

                                                    if (registroCivil != null)
                                                        familiares = await _garancheck.GetFamiliaLineaAsync(registroCivil.Cedula, registroCivil.CedulaConyuge, registroCivil.Conyuge, registroCivil.CedulaMadre, registroCivil.NombreMadre, registroCivil.CedulaPadre, registroCivil.NombrePadre);

                                                    if (familiares == null)
                                                        familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);

                                                    break;
                                                default:
                                                    r_garancheck = await _garancheck.GetRespuestaAsync(cedulaTemp);
                                                    familiares = await _garancheck.GetFamiliaAsync(cedulaTemp);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (registroCivil == null && r_garancheck == null)
                        {
                            var datosDetalleGarancheck = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleGarancheck != null)
                            {
                                cacheCivil = true;
                                r_garancheck = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Persona>(datosDetalleGarancheck);
                            }
                        }
                        if (datosPersonal == null)
                        {
                            var datosDetallePersonales = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Personales && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetallePersonales != null)
                            {
                                cachePersonal = true;
                                datosPersonal = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Personal>(datosDetallePersonales);
                            }
                        }
                        if (contactos == null)
                        {
                            var datosDetalleContactos = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Contactos && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleContactos != null)
                            {
                                cacheContactos = true;
                                contactos = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContactos);
                            }
                        }
                        if (contactosIess == null)
                        {
                            var datosDetalleContactosIess = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleContactosIess != null)
                            {
                                cacheContactosIess = true;
                                contactosIess = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Contacto>(datosDetalleContactosIess);
                            }
                        }
                        if (familiares == null)
                        {
                            var datosDetalleFamiliares = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.Familiares && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleFamiliares != null)
                            {
                                cacheFamiliares = true;
                                familiares = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.Familia>(datosDetalleFamiliares);
                            }
                        }

                        if (historialTemp.PlanEmpresa.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1791927966001)
                        {
                            if (registroCivil == null)
                            {
                                var datosDetalleRegistroCivil = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanEmpresa.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1791927966001 && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id), null, true);
                                if (datosDetalleRegistroCivil != null)
                                {
                                    cacheRegistroCivil = true;
                                    registroCivil = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(datosDetalleRegistroCivil);
                                }
                            }
                        }
                        else
                        {
                            if (registroCivil == null)
                            {
                                var datosDetalleRegistroCivil = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanEmpresa.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1791927966001 && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                                if (datosDetalleRegistroCivil != null)
                                {
                                    cacheRegistroCivil = true;
                                    registroCivil = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(datosDetalleRegistroCivil);
                                }
                            }
                        }

                        var nacionalidades = new List<NacionalidadViewModel>();
                        var pathNacionalidades = Path.Combine("wwwroot", "data", "dataNacionalidades.json");
                        var nacMadre = new NacionalidadViewModel();
                        var nacPadre = new NacionalidadViewModel();
                        if (System.IO.File.Exists(pathNacionalidades))
                        {
                            nacionalidades = JsonConvert.DeserializeObject<List<NacionalidadViewModel>>(System.IO.File.ReadAllText(pathNacionalidades));
                            if (datosPersonal != null && nacionalidades != null && nacionalidades.Any())
                            {
                                nacMadre = nacionalidades.FirstOrDefault(x => x.Codigo?.Trim().ToUpper() == datosPersonal.NacMadre?.Trim().ToUpper());
                                nacPadre = nacionalidades.FirstOrDefault(x => x.Codigo?.Trim().ToUpper() == datosPersonal.NacPadre?.Trim().ToUpper());

                                datosPersonal.NacPadre = nacPadre != null ? nacPadre.Nacionalidad : "DESCONOCIDO";
                                datosPersonal.NacMadre = nacMadre != null ? nacMadre.Nacionalidad : "DESCONOCIDO";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Civil con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                    datos = new CivilApiMetodoViewModel()
                    {
                        General = r_garancheck,
                        Familiares = familiares,
                        Personal = datosPersonal,
                        Contactos = contactos,
                        RegistroCivil = registroCivil
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                    datos = JsonConvert.DeserializeObject<CivilApiMetodoViewModel>(System.IO.File.ReadAllText(pathCivil));
                }

                _logger.LogInformation("Fuente de Civil procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Civil. Id Historial: {modelo.IdHistorial}");
                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var fuentesCivil = new[] { Dominio.Tipos.Fuentes.Ciudadano, Dominio.Tipos.Fuentes.Personales, Dominio.Tipos.Fuentes.Contactos, Dominio.Tipos.Fuentes.RegistroCivil, Dominio.Tipos.Fuentes.ContactosIess };
                        var historialesCivil = await _detallesHistorial.ReadAsync(m => m, m => m.IdHistorial == modelo.IdHistorial && fuentesCivil.Contains(m.TipoFuente));
                        var historialCiudadano = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano);
                        var historialPersonal = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Personales);
                        var historialContactos = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.Contactos);
                        var historialRegCivil = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil);
                        var historialContactosIess = historialesCivil.FirstOrDefault(m => m.TipoFuente == Dominio.Tipos.Fuentes.ContactosIess);

                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.Cedula?.Trim()) && !string.IsNullOrEmpty(registroCivil.Nombre?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = registroCivil.Nombre?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = registroCivil.Cedula?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }

                            if (registroCivil != null && registroCivil.FechaCedulacion != default && string.IsNullOrEmpty(historial.FechaExpedicionCedula?.Trim()))
                            {
                                historial.FechaExpedicionCedula = registroCivil.FechaCedulacion.ToString("dd/MM/yyyy");
                                await _historiales.UpdateAsync(historial);
                            }

                            if (r_garancheck != null && !string.IsNullOrEmpty(r_garancheck.Identificacion?.Trim()) && !string.IsNullOrEmpty(r_garancheck.Nombres?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = r_garancheck.Nombres?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = r_garancheck.Identificacion?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }
                            if (historialConsolidadoTemp != null)
                            {
                                historialConsolidadoTemp.NombrePersona = historial.NombresPersona;
                                await _reporteConsolidado.UpdateAsync(historialConsolidadoTemp);
                            }
                        }

                        if (historialCiudadano != null && !historialCiudadano.Generado)
                        {
                            historialCiudadano.IdHistorial = modelo.IdHistorial;
                            historialCiudadano.TipoFuente = Dominio.Tipos.Fuentes.Ciudadano;
                            historialCiudadano.Generado = r_garancheck != null;
                            historialCiudadano.Data = r_garancheck != null ? JsonConvert.SerializeObject(r_garancheck) : null;
                            historialCiudadano.Cache = cacheCivil;
                            historialCiudadano.FechaRegistro = DateTime.Now;
                            historialCiudadano.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialCiudadano);
                            _logger.LogInformation("Historial de la Fuente Garancheck para Civil se actualizado correctamente");
                        }

                        if (historialPersonal != null && !historialPersonal.Generado)
                        {
                            historialPersonal.IdHistorial = modelo.IdHistorial;
                            historialPersonal.TipoFuente = Dominio.Tipos.Fuentes.Personales;
                            historialPersonal.Generado = datosPersonal != null;
                            historialPersonal.Data = datosPersonal != null ? JsonConvert.SerializeObject(datosPersonal) : null;
                            historialPersonal.Cache = cachePersonal;
                            historialPersonal.FechaRegistro = DateTime.Now;
                            historialPersonal.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialPersonal);
                            _logger.LogInformation("Historial de la Fuente Datos Personales para Civil se actualizado correctamente");
                        }

                        if (historialContactos != null && !historialContactos.Generado)
                        {
                            historialContactos.IdHistorial = modelo.IdHistorial;
                            historialContactos.TipoFuente = Dominio.Tipos.Fuentes.Contactos;
                            historialContactos.Generado = contactos != null;
                            historialContactos.Data = contactos != null ? JsonConvert.SerializeObject(contactos) : null;
                            historialContactos.Cache = cacheContactos;
                            historialContactos.FechaRegistro = DateTime.Now;
                            historialContactos.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContactos);
                            _logger.LogInformation("Historial de la Fuente Contactos para Civil se actualizado correctamente");
                        }

                        if (historialRegCivil != null && !historialRegCivil.Generado)
                        {
                            historialRegCivil.IdHistorial = modelo.IdHistorial;
                            historialRegCivil.TipoFuente = Dominio.Tipos.Fuentes.RegistroCivil;
                            historialRegCivil.Generado = registroCivil != null;
                            historialRegCivil.Data = registroCivil != null ? JsonConvert.SerializeObject(registroCivil) : null;
                            historialRegCivil.Cache = cacheRegistroCivil;
                            historialRegCivil.FechaRegistro = DateTime.Now;
                            historialRegCivil.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialRegCivil);
                            _logger.LogInformation("Historial de la Fuente Reg. Civil para Civil se actualizado correctamente");
                        }

                        if (historialContactosIess != null && !historialContactosIess.Generado)
                        {
                            historialContactosIess.IdHistorial = modelo.IdHistorial;
                            historialContactosIess.TipoFuente = Dominio.Tipos.Fuentes.ContactosIess;
                            historialContactosIess.Generado = contactosIess != null;
                            historialContactosIess.Data = contactosIess != null ? JsonConvert.SerializeObject(contactosIess) : null;
                            historialContactosIess.Cache = cacheContactosIess;
                            historialContactosIess.FechaRegistro = DateTime.Now;
                            historialContactosIess.Reintento = true;
                            await _detallesHistorial.ActualizarDetalleHistorialAsync(historialContactosIess);
                            _logger.LogInformation("Historial de la Fuente Contactos Iess para Civil se actualizado correctamente");
                        }
                        _logger.LogInformation("Historial de la Fuente Civil procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<CivilApiMetodoViewModel> ActualizarReporteCivilBasico(ApiViewModel modelo)
        {
            try
            {
                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.IdHistorial == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                var detalleRegistroCivil = await _detallesHistorial.AnyAsync(m => m.Historial.Id == modelo.IdHistorial && m.TipoFuente == Dominio.Tipos.Fuentes.Ciudadano);

                if (!detalleRegistroCivil)
                {
                    _logger.LogInformation($"No se encontró en el historial la consulta de Civil para el Id: {modelo.IdHistorial}");
                    throw new Exception("La fuente SRI no puede ser actualizada");
                }

                modelo.Identificacion = modelo.Identificacion.Trim();
                Externos.Logica.Garancheck.Modelos.RegistroCivil registroCivil = null;
                Externos.Logica.Garancheck.Modelos.Contacto contactos = null;
                var datos = new CivilApiMetodoViewModel();
                Historial historialTemp = null;
                var cacheCivil = false;
                if (!_cache)
                {
                    try
                    {
                        _logger.LogInformation($"Procesando Fuente Civil identificación: {modelo.Identificacion}");
                        if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                        {
                            registroCivil = await _garancheck.GetRegistroCivilLineaBasicoAsync(modelo.Identificacion);
                            contactos = await _garancheck.GetContactoAsync(modelo.Identificacion);
                        }
                        else if (ValidacionViewModel.ValidarRuc(modelo.Identificacion))
                        {
                            historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial, null, null, true);
                            if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                            {
                                if (historialTemp != null && !string.IsNullOrEmpty(historialTemp.IdentificacionSecundaria))
                                {
                                    if (ValidacionViewModel.ValidarCedula(historialTemp.IdentificacionSecundaria))
                                    {
                                        registroCivil = await _garancheck.GetRegistroCivilLineaBasicoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                        contactos = await _garancheck.GetContactoAsync(historialTemp.IdentificacionSecundaria.Trim());
                                    }
                                }
                            }
                            else
                            {
                                var cedulaTemp = modelo.Identificacion.Substring(0, 10);
                                if (ValidacionViewModel.ValidarCedula(cedulaTemp))
                                {
                                    registroCivil = await _garancheck.GetRegistroCivilLineaBasicoAsync(cedulaTemp);
                                    contactos = await _garancheck.GetContactoAsync(cedulaTemp);
                                }
                            }
                        }
                        if (registroCivil != null)
                            cacheCivil = true;

                        if (registroCivil == null)
                        {
                            var datosDetalleRegistroCivil = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.TipoFuente == Dominio.Tipos.Fuentes.RegistroCivil && m.Generado && !m.Cache, o => o.OrderByDescending(m => m.Id));
                            if (datosDetalleRegistroCivil != null)
                            {
                                cacheCivil = true;
                                registroCivil = JsonConvert.DeserializeObject<Externos.Logica.Garancheck.Modelos.RegistroCivil>(datosDetalleRegistroCivil);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al consultar fuente Civil con identificación {modelo.Identificacion}: {ex.Message}");
                    }
                    datos = new CivilApiMetodoViewModel()
                    {
                        RegistroCivil = registroCivil,
                        Contactos = contactos
                    };
                }
                else
                {
                    var pathBase = System.IO.Path.Combine("wwwroot", "data");
                    var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                    var pathCivil = Path.Combine(pathFuentes, "civilDemo.json");
                    datos = JsonConvert.DeserializeObject<CivilApiMetodoViewModel>(System.IO.File.ReadAllText(pathCivil));
                }
                _logger.LogInformation("Fuente de Civil procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Civil. Id Historial: {modelo.IdHistorial}");

                try
                {
                    if (modelo.IdHistorial > 0)
                    {
                        var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdHistorial);
                        if (historial != null)
                        {
                            if (registroCivil != null && !string.IsNullOrEmpty(registroCivil.Cedula?.Trim()) && !string.IsNullOrEmpty(registroCivil.Nombre?.Trim()) && string.IsNullOrEmpty(historial.NombresPersona?.Trim()))
                            {
                                historial.NombresPersona = registroCivil.Nombre?.Trim().ToUpper();
                                if (historial.TipoIdentificacion != Dominio.Constantes.General.Cedula && string.IsNullOrEmpty(historial.IdentificacionSecundaria?.Trim()))
                                    historial.IdentificacionSecundaria = registroCivil.Cedula?.Trim().ToUpper();
                                await _historiales.UpdateAsync(historial);
                            }

                            if (registroCivil != null && registroCivil.FechaCedulacion != default && string.IsNullOrEmpty(historial.FechaExpedicionCedula?.Trim()))
                            {
                                historial.FechaExpedicionCedula = registroCivil.FechaCedulacion.ToString("dd/MM/yyyy");
                                await _historiales.UpdateAsync(historial);
                            }
                        }

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.RegistroCivil,
                            Generado = datos.RegistroCivil != null,
                            Data = datos.RegistroCivil != null ? JsonConvert.SerializeObject(datos.RegistroCivil) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = false
                        });

                        await _detallesHistorial.GuardarDetalleHistorialAsync(new DetalleHistorial()
                        {
                            IdHistorial = modelo.IdHistorial,
                            TipoFuente = Dominio.Tipos.Fuentes.Contactos,
                            Generado = datos.Contactos != null,
                            Data = datos.Contactos != null ? JsonConvert.SerializeObject(datos.Contactos) : null,
                            Cache = cacheCivil,
                            FechaRegistro = DateTime.Now,
                            Reintento = null
                        });
                        _logger.LogInformation("Historial de la Fuente Civil procesado correctamente");
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
        private async Task<BuroCreditoMetodoViewModel> ActualizarReporteBuroCredito(ApiActualizarViewModel modelo)
        {
            try
            {
                var identificacionBuro = string.Empty;

                if (modelo == null)
                    throw new Exception("No se han enviado parámetros para obtener el reporte");

                if (string.IsNullOrEmpty(modelo.Identificacion?.Trim()))
                    throw new Exception("El campo RUC es obligatorio");

                if (modelo.IdPadre == 0)
                    throw new Exception("El campo IdHistorial es obligatorio");

                modelo.Identificacion = modelo.Identificacion.Trim();

                if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                    identificacionBuro = $"{modelo.Identificacion}001";

                if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                    identificacionBuro = modelo.Identificacion.Substring(0, 10);

                Externos.Logica.BuroCredito.Modelos.CreditoRespuesta r_burocredito = null;
                Externos.Logica.Equifax.Modelos.Resultado r_burocreditoEquifax = null;
                Historial historialTemp = null;
                var cacheBuroCredito = false;
                var busquedaNuevaBuroCredito = false;
                var idUsuario = modelo.IdUsuario;
                var idPlanBuro = 0;
                var datos = new BuroCreditoMetodoViewModel();
                var dataError = string.Empty;
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                var aplicaConsultaBuroCompartida = false;
                var tipoDocumentoBLitoral = string.Empty;

                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuario);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                var planBuroCredito = usuarioActual.Empresa.PlanesBuroCredito.FirstOrDefault(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo);
                if (planBuroCredito == null)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene un plan activo de Buró de Crédito.");

                var permisoPlanBuro = await _accesos.AnyAsync(m => m.IdUsuario == idUsuario && m.Estado == Dominio.Tipos.EstadosAccesos.Activo && m.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito);
                if (!permisoPlanBuro)
                    throw new Exception("El usuario no tiene permiso para realizar consultas al Buró de Crédito.");

                idPlanBuro = planBuroCredito.Id;

                if (idPlanBuro == 0)
                    throw new Exception("No es posible realizar esta consulta ya que no tiene planes vigentes de Buró de Crédito.");

                var fechaActual = DateTime.Now;
                var primerDiadelMes = new DateTime(fechaActual.Year, fechaActual.Month, 1);
                var ultimoDiadelMes = primerDiadelMes.AddMonths(1).AddDays(-1);

                if (planBuroCredito.ConsultasCompartidas && planBuroCredito.NumeroMaximoConsultasCompartidas.HasValue && planBuroCredito.NumeroMaximoConsultasCompartidas.Value > 0)
                {
                    var numeroHistorialBuroSinComp = await _historiales.CountAsync(s => s.Id != modelo.IdPadre && s.IdPlanBuroCredito == idPlanBuro && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date && !s.ConsultaBuroCompartido);
                    if (numeroHistorialBuroSinComp >= planBuroCredito.NumeroMaximoConsultas)
                    {
                        _logger.LogInformation($"Se ha alcanzado el límite de consultas con las credenciales propias del cliente:  {usuarioActual.IdEmpresa}.");
                        aplicaConsultaBuroCompartida = true;
                    }

                    if (aplicaConsultaBuroCompartida)
                    {
                        var resultadoPermisoCompartido = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;
                        if (planBuroCredito.BloquearConsultas)
                        {
                            var numeroHistorialBuroComp = await _historiales.CountAsync(s => s.Id != modelo.IdPadre && s.IdPlanBuroCredito == idPlanBuro && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date && s.ConsultaBuroCompartido);
                            resultadoPermisoCompartido = planBuroCredito.NumeroMaximoConsultasCompartidas > numeroHistorialBuroComp ? Dominio.Tipos.EstadosPlanesBuroCredito.Activo : Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
                        }

                        if (resultadoPermisoCompartido != Dominio.Tipos.EstadosPlanesBuroCredito.Activo)
                            throw new Exception("No es posible realizar esta consulta ya que excedió el límite de consultas del plan Buró de Crédito.");
                    }

                    #region Mail
                    if ((numeroHistorialBuroSinComp + 1) == planBuroCredito.NumeroMaximoConsultas)
                    {
                        try
                        {
                            var usuarios = new List<string>();
                            var usuarioAdministrador = await _usuarios.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.UsuariosRoles.Any(s => s.RoleId == (short)Dominio.Tipos.Roles.AdministradorEmpresa), null, null, true);
                            if (usuarioAdministrador != null)
                                usuarios.Add(usuarioAdministrador.NormalizedEmail);

                            var usuarioGc = await _usuarios.FirstOrDefaultAsync(m => m, m => m.Identificacion == Dominio.Constantes.General.CedulaPersonaDemo && m.UsuariosRoles.Any(s => s.RoleId == (short)Dominio.Tipos.Roles.Administrador), null, null, true);
                            if (usuarioGc != null)
                                usuarios.Add(usuarioGc.NormalizedEmail);

                            if (usuarios.Any())
                            {
                                var emisor = _configuration.GetValue<string>("SmtpSettings:From");
                                _logger.LogInformation($"Preparando envío de correo consultas de butó ...");
                                var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.LimiteBuroAlcanzado);
                                if (string.IsNullOrEmpty(template))
                                    throw new Exception($"No se ha cargado la plantilla de tipo: {Dominio.Tipos.TemplatesCorreo.LimiteBuroAlcanzado}");

                                var asunto = "Límite de Consultas Alcanzado Buró de Crédito Plataforma Integral de Información 360°";
                                var domainName = new Uri(HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
                                var enlace = $"{domainName}{Url.Action("Inicio", "Cuenta", new { Area = "Identidad" })}";

                                var replacements = new Dictionary<string, object>
                                {
                                    { "{NOMBREEMPRESA}", $"{usuarioActual.Empresa.Identificacion} - {usuarioActual.Empresa.RazonSocial}" },
                                    { "{NUMCONSULTASCLIENTE}", numeroHistorialBuroSinComp.ToString() },
                                    { "{NUMCONSULTASINTERNAS}", planBuroCredito.NumeroMaximoConsultasCompartidas.Value },
                                    { "{ENLACE}", enlace },
                                };
                                var correosBcc = string.Join(';', usuarios.Select(m => m.ToLower().Trim()));
                                await _emailSender.SendEmailAsync(emisor, asunto, template, "USUARIO", replacements, null, correosBcc);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                    #endregion Mail
                }
                else
                {
                    var resultadoPermiso = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;
                    if (planBuroCredito.BloquearConsultas)
                    {
                        var numeroHistorialBuro = await _historiales.CountAsync(s => s.Id != modelo.IdPadre && s.IdPlanBuroCredito == idPlanBuro && s.Fecha.Date >= primerDiadelMes.Date && s.Fecha.Date <= ultimoDiadelMes.Date);
                        resultadoPermiso = planBuroCredito.NumeroMaximoConsultas > numeroHistorialBuro ? Dominio.Tipos.EstadosPlanesBuroCredito.Activo : Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
                    }

                    if (resultadoPermiso != Dominio.Tipos.EstadosPlanesBuroCredito.Activo)
                        throw new Exception("No es posible realizar esta consulta ya que excedió el límite de consultas del plan Buró de Crédito.");
                }

                var historial = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdPadre);
                if (historial != null)
                {
                    historial.IdPlanBuroCredito = idPlanBuro;
                    historial.TipoFuenteBuro = planBuroCredito.Fuente;
                    if (aplicaConsultaBuroCompartida)
                        historial.ConsultaBuroCompartido = true;
                    await _historiales.UpdateAsync(historial);
                }

                try
                {
                    var credencial = await _credencialesBuro.FirstOrDefaultAsync(m => m, m => m.IdEmpresa == usuarioActual.IdEmpresa && m.Estado == Dominio.Tipos.EstadosCredenciales.Activo, null, null, true);
                    historialTemp = await _historiales.FirstOrDefaultAsync(m => m, m => m.Id == modelo.IdPadre, null, null, true);
                    var cacheBuro = _configuration.GetSection("AppSettings:ConsultasBuroCredito:Cache").Get<bool>();
                    var ambiente = _configuration.GetSection("AppSettings:Environment").Get<string>();
                    if (!cacheBuro && ambiente == "Production")
                    {
                        var buroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => new { m.Data, m.Historial.Fecha }, m => m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && (m.Historial.Identificacion == modelo.Identificacion || m.Historial.Identificacion == identificacionBuro) && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Historial.PlanBuroCredito.Fuente == planBuroCredito.Fuente && m.Historial.TipoFuenteBuro == planBuroCredito.Fuente && m.Generado && !m.Cache && planBuroCredito.PersistenciaCache > 0, o => o.OrderByDescending(m => m.Id));

                        if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            string[] credenciales = null;
                            if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                credenciales = new[] { credencial.Usuario, credencial.Clave, credencial.Enlace, credencial.ProductData, credencial.TokenAcceso, credencial.FechaCreacionToken.HasValue && credencial.FechaCreacionToken.Value != default ? credencial.FechaCreacionToken.Value.ToString() : string.Empty };

                            if (aplicaConsultaBuroCompartida && credenciales != null && credenciales.Any())
                                credenciales = null;

                            if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001)
                            {
                                var modeloAutomotriz = new ModeloBuro_0990981930001();
                                var modeloMicroFinanzas = new ModeloBuroMicrofinanza_0990981930001();
                                var datosAutomotriz = new[]
                                {
                                    Dominio.Tipos.ParametrosClientes.LineaBLitoral,
                                    Dominio.Tipos.ParametrosClientes.TipoCreditoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.PlazoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.MontoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.IngresoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.GastoHogarBLitoral,
                                    Dominio.Tipos.ParametrosClientes.RestaGastoFinancieroBLitoral,
                                    Dominio.Tipos.ParametrosClientes.TipoDocumentoBLitoral,
                                    Dominio.Tipos.ParametrosClientes.NumeroDocumentoBLitoral
                                };

                                var datosMicroFinanzas = new[]
                                {
                                    Dominio.Tipos.ParametrosClientes.TipoCreditoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.PlazoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.MontoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.IngresoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.GastoHogarBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.RestaGastoFinancieroBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.TipoDocumentoBLitoralMicrofinanza,
                                    Dominio.Tipos.ParametrosClientes.NumeroDocumentoBLitoralMicrofinanza
                                };

                                var parametrosBLitorial = await _parametrosClientesHistoriales.ReadAsync(x => x, x => x.IdHistorial == modelo.IdPadre, null, null, 0, null, true);

                                if (parametrosBLitorial != null && !datosMicroFinanzas.Except(parametrosBLitorial.Select(x => x.Parametro).ToList()).Any() && !parametrosBLitorial.Select(x => x.Parametro).ToList().Except(datosMicroFinanzas).Any())
                                {
                                    modeloMicroFinanzas = new ModeloBuroMicrofinanza_0990981930001()
                                    {
                                        TipoCreditoBLitoralMicrofinanza = (TipoCreditoMicrofinanzas)short.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.TipoCreditoBLitoralMicrofinanza).Valor),
                                        PlazoBLitoralMicrofinanza = int.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.PlazoBLitoralMicrofinanza).Valor),
                                        MontoBLitoralMicrofinanza = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.MontoBLitoralMicrofinanza).Valor),
                                        IngresoBLitoralMicrofinanza = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.IngresoBLitoralMicrofinanza).Valor),
                                        GastoHogarBLitoralMicrofinanza = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.GastoHogarBLitoralMicrofinanza).Valor),
                                        RestaGastoFinancieroBLitoralMicrofinanza = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.RestaGastoFinancieroBLitoralMicrofinanza).Valor)
                                    };
                                }
                                else if (parametrosBLitorial != null && !datosAutomotriz.Except(parametrosBLitorial.Select(x => x.Parametro).ToList()).Any() && !parametrosBLitorial.Select(x => x.Parametro).ToList().Except(datosAutomotriz).Any())
                                {
                                    modeloAutomotriz = new ModeloBuro_0990981930001()
                                    {
                                        TipoLineaBLitoral = (TipoLinea)short.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.LineaBLitoral).Valor),
                                        TipoCreditoBLitoral = (TipoCredito)short.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.TipoCreditoBLitoral).Valor),
                                        PlazoBLitoral = int.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.PlazoBLitoral).Valor),
                                        MontoBLitoral = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.MontoBLitoral).Valor),
                                        IngresoBLitoral = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.IngresoBLitoral).Valor),
                                        GastoHogarBLitoral = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.GastoHogarBLitoral).Valor),
                                        RestaGastoFinancieroBLitoral = decimal.Parse(parametrosBLitorial.FirstOrDefault(x => x.Parametro == Dominio.Tipos.ParametrosClientes.RestaGastoFinancieroBLitoral).Valor)
                                    };
                                }

                                if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && !datosAutomotriz.Except(parametrosBLitorial.Select(x => x.Parametro).ToList()).Any() && !parametrosBLitorial.Select(x => x.Parametro).ToList().Except(datosAutomotriz).Any())
                                {
                                    _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax (BANCO LITORAL) identificación: {modelo.Identificacion}");
                                    if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                        credenciales = new[] { credencial.Usuario, credencial.Clave, credencial.Enlace };

                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion?.Trim()) || ValidacionViewModel.ValidarRuc(modelo.Identificacion?.Trim()))
                                        tipoDocumentoBLitoral = "C";

                                    if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaBancoLitoralAsync(tipoDocumentoBLitoral, modelo.Identificacion.Substring(0, 10), modeloAutomotriz.TipoLineaBLitoral.GetEnumDescription(), modeloAutomotriz.TipoCreditoBLitoral.GetEnumDescription(), modeloAutomotriz.PlazoBLitoral.Value.ToString(), modeloAutomotriz.MontoBLitoral.Value.ToString(), modeloAutomotriz.IngresoBLitoral.Value.ToString(), modeloAutomotriz.GastoHogarBLitoral.Value.ToString(), modeloAutomotriz.RestaGastoFinancieroBLitoral.Value.ToString(), credenciales);
#endif
                                    }
                                    //                                    else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    //                                    {
                                    //#if !DEBUG
                                    //                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaBancoLitoralAsync(tipoDocumentoBLitoral, modelo.Identificacion?.Trim(), modeloAutomotriz.TipoLineaBLitoral.GetEnumDescription(), modeloAutomotriz.TipoCreditoBLitoral.GetEnumDescription(), modeloAutomotriz.PlazoBLitoral.Value.ToString(), modeloAutomotriz.MontoBLitoral.Value.ToString(), modeloAutomotriz.IngresoBLitoral.Value.ToString(), modeloAutomotriz.GastoHogarBLitoral.Value.ToString(), modeloAutomotriz.RestaGastoFinancieroBLitoral.Value.ToString(), credenciales);
                                    //#endif
                                    //                                    }
                                    else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaBancoLitoralAsync(tipoDocumentoBLitoral, modelo.Identificacion?.Trim(), modeloAutomotriz.TipoLineaBLitoral.GetEnumDescription(), modeloAutomotriz.TipoCreditoBLitoral.GetEnumDescription(), modeloAutomotriz.PlazoBLitoral.Value.ToString(), modeloAutomotriz.MontoBLitoral.Value.ToString(), modeloAutomotriz.IngresoBLitoral.Value.ToString(), modeloAutomotriz.GastoHogarBLitoral.Value.ToString(), modeloAutomotriz.RestaGastoFinancieroBLitoral.Value.ToString(), credenciales);
#endif
                                    }
                                }
                                else if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && !datosMicroFinanzas.Except(parametrosBLitorial.Select(x => x.Parametro).ToList()).Any() && !parametrosBLitorial.Select(x => x.Parametro).ToList().Except(datosMicroFinanzas).Any())
                                {
                                    _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax (BANCO LITORAL) identificación: {modelo.Identificacion}");
                                    if (credencial != null && credencial.TipoFuente == Dominio.Tipos.FuentesBuro.Equifax)
                                        credenciales = new[] { credencial.Usuario, credencial.Clave, credencial.Enlace };

                                    if (ValidacionViewModel.ValidarCedula(modelo.Identificacion?.Trim()) || ValidacionViewModel.ValidarRuc(modelo.Identificacion?.Trim()))
                                        tipoDocumentoBLitoral = "C";

                                    if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaBancoLitoralMicrofinanzaAsync(tipoDocumentoBLitoral, modelo.Identificacion.Substring(0, 10), modeloMicroFinanzas.TipoCreditoBLitoralMicrofinanza.GetEnumDescription(), modeloMicroFinanzas.PlazoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.MontoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.IngresoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.GastoHogarBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.RestaGastoFinancieroBLitoralMicrofinanza.Value.ToString(), credenciales);
#endif
                                    }
                                    //                                    else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    //                                    {
                                    //#if !DEBUG
                                    //                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaBancoLitoralMicrofinanzaAsync(tipoDocumentoBLitoral, modelo.Identificacion?.Trim(), modeloMicroFinanzas.TipoCreditoBLitoralMicrofinanza.GetEnumDescription(), modeloMicroFinanzas.PlazoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.MontoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.IngresoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.GastoHogarBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.RestaGastoFinancieroBLitoralMicrofinanza.Value.ToString(), credenciales);
                                    //#endif
                                    //                                    }
                                    else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
#if !DEBUG
                                        r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaBancoLitoralMicrofinanzaAsync(tipoDocumentoBLitoral, modelo.Identificacion?.Trim(), modeloMicroFinanzas.TipoCreditoBLitoralMicrofinanza.GetEnumDescription(), modeloMicroFinanzas.PlazoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.MontoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.IngresoBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.GastoHogarBLitoralMicrofinanza.Value.ToString(), modeloMicroFinanzas.RestaGastoFinancieroBLitoralMicrofinanza.Value.ToString(), credenciales);
#endif
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax identificación: {modelo.Identificacion}");
                                    if (ValidacionViewModel.ValidarRuc(modelo.Identificacion) && !ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) && !ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                       if (credencial == null || string.IsNullOrWhiteSpace(credencial.ProductData?.Trim()))
                                            r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaAsync(modelo.Identificacion.Substring(0, 10), credenciales);
                                        else
                                            r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaV2Async(modelo.Identificacion.Substring(0, 10), credenciales);
#endif
                                    }
                                    else if (ValidacionViewModel.ValidarRucJuridico(modelo.Identificacion) || ValidacionViewModel.ValidarRucSectorPublico(modelo.Identificacion))
                                    {
#if !DEBUG
                                        if (credencial == null || string.IsNullOrWhiteSpace(credencial.ProductData?.Trim()))
                                            r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaAsync(modelo.Identificacion, credenciales);
                                        else
                                            r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaV2Async(modelo.Identificacion, credenciales);
#endif
                                    }
                                    else if (ValidacionViewModel.ValidarCedula(modelo.Identificacion))
                                    {
#if !DEBUG
                                        if (credencial == null || string.IsNullOrWhiteSpace(credencial.ProductData?.Trim()))
                                            r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaAsync(modelo.Identificacion, credenciales);
                                        else
                                            r_burocreditoEquifax = await _buroCreditoEquifax.GetRespuestaV2Async(modelo.Identificacion, credenciales);
#endif
                                    }
                                }

                                if (r_burocreditoEquifax != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(credencial.TokenAcceso?.Trim()) && !string.IsNullOrWhiteSpace(r_burocreditoEquifax.TokenAcceso?.Trim()) && credencial.TokenAcceso.Trim() != r_burocreditoEquifax.TokenAcceso.Trim())
                                    {
                                        credencial.TokenAcceso = r_burocreditoEquifax.TokenAcceso;
                                        credencial.FechaCreacionToken = r_burocreditoEquifax.FechaCreacionToken;
                                        await _credencialesBuro.UpdateAsync(credencial);
                                    }
                                    else if (!string.IsNullOrWhiteSpace(r_burocreditoEquifax.TokenAcceso?.Trim()) && string.IsNullOrWhiteSpace(credencial.TokenAcceso?.Trim()))
                                    {
                                        credencial.TokenAcceso = r_burocreditoEquifax.TokenAcceso;
                                        credencial.FechaCreacionToken = r_burocreditoEquifax.FechaCreacionToken;
                                        await _credencialesBuro.UpdateAsync(credencial);
                                    }
                                }

                                if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados == null)
                                {
                                    dataError = JsonConvert.SerializeObject(r_burocreditoEquifax);
                                    r_burocreditoEquifax = null;
                                }
                            }
                        }
                        else
                            throw new Exception("No se pudo realizar la consulta de Buró de Crédito en ninguna de las fuentes");
                    }
                    else
                    {
                        var pathBase = System.IO.Path.Combine("wwwroot", "data");
                        var pathFuentes = System.IO.Path.Combine(pathBase, "Fuentes");
                        if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                        {
                            if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                            {
                                //var pathBuroEmpresa = Path.Combine(pathFuentes, "buroAvalEmpresaAyasaDemo.json");
                                var pathBuroEmpresa = Path.Combine(pathFuentes, "buroAvalEmpresaDemo.json");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroEmpresa));
                            }
                            else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                //var pathBuroCedula = Path.Combine(pathFuentes, "buroAvalCedulaAyasaDemo.json");
                                var pathBuroCedula = Path.Combine(pathFuentes, "buroAvalCedulaDemo.json");
                                r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(System.IO.File.ReadAllText(pathBuroCedula));
                            }
                        }
                        else if (planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural)
                            {
                                var pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaDemo.json");
                                if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && modelo.ModeloBuro.HasValue && modelo.ModeloBuro.Value == TipoBuroModelo.ModeloAutomotriz)
                                    pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaBancoLitoralDemo.json");
                                else if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && modelo.ModeloBuro.HasValue && modelo.ModeloBuro.Value == TipoBuroModelo.ModeloMicrofinanzas)
                                    pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxCedulaBancoLitoralMicrofinanzaDemo.json");
                                r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(System.IO.File.ReadAllText(pathBuroEquifax));
                            }
                            else if (historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucJuridico || historialTemp.TipoIdentificacion == Dominio.Constantes.General.SectorPublico)
                            {
                                var pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaDemo.json");
                                if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && modelo.ModeloBuro.HasValue && modelo.ModeloBuro.Value == TipoBuroModelo.ModeloAutomotriz)
                                    pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaBancoLitoralDemo.json");
                                else if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001 && modelo.ModeloBuro.HasValue && modelo.ModeloBuro.Value == TipoBuroModelo.ModeloMicrofinanzas)
                                    pathBuroEquifax = Path.Combine(pathFuentes, "buroEquifaxEmpresaBancoLitoralMicrofinanzaDemo.json");
                                r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(System.IO.File.ReadAllText(pathBuroEquifax));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consultar fuente Buró de Crédito con identificación {modelo.Identificacion}: {ex.Message}");
                }

                if (r_burocredito == null && planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                {
                    var datosDetalleBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Aval && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosDetalleBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                    {
                        _logger.LogInformation($"Procesando Fuente Buró de Crédito Aval con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheBuroCredito = true;
                        r_burocredito = JsonConvert.DeserializeObject<Externos.Logica.BuroCredito.Modelos.CreditoRespuesta>(datosDetalleBuroCredito);
                        busquedaNuevaBuroCredito = true;
                    }
                }
                else if (r_burocreditoEquifax == null && planBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                {
                    var datosDetalleBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m.Data, m => m.Historial.Identificacion == modelo.Identificacion && m.Historial.PlanBuroCredito.IdEmpresa == usuarioActual.IdEmpresa && m.Historial.TipoFuenteBuro == Dominio.Tipos.FuentesBuro.Equifax && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito && m.Generado, o => o.OrderByDescending(m => m.Id));
                    if (datosDetalleBuroCredito != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                    {
                        _logger.LogInformation($"Procesando Fuente Buró de Crédito Equifax con la memoria caché de la base de datos para la identificación: {modelo.Identificacion}");
                        cacheBuroCredito = true;
                        r_burocreditoEquifax = JsonConvert.DeserializeObject<Externos.Logica.Equifax.Modelos.Resultado>(datosDetalleBuroCredito);
                        busquedaNuevaBuroCredito = true;

                        if (usuarioActual.IdEmpresa == Dominio.Constantes.Clientes.IdCliente0990981930001)
                        {
                            r_burocreditoEquifax = null;
                            cacheBuroCredito = false;
                            busquedaNuevaBuroCredito = false;
                        }
                    }
                }

                try
                {
                    if ((historialTemp.TipoIdentificacion == Dominio.Constantes.General.Cedula || historialTemp.TipoIdentificacion == Dominio.Constantes.General.RucNatural) && r_burocreditoEquifax != null && planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                    {
                        var deudaSumaEquifax = 0.00;
                        var ingresoPrevioEquifax = 0.00;
                        var ingresoEstimadoEquifax = 0.00;
                        if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360 != null && r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Any())
                        {
                            var deudaVencido = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != string.Empty).Sum(x => x.Vencido);
                            var deudaDemandaJudicial = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != string.Empty).Sum(x => x.DemandaJudicial);
                            var deudaCarteraCastigada = r_burocreditoEquifax.Resultados.ValorDeudaTotalEnLos3SegmentosSinIESS360.Where(x => x.Titulo != string.Empty).Sum(x => x.CarteraCastigada);
                            deudaSumaEquifax = (double)(deudaVencido + deudaDemandaJudicial + deudaCarteraCastigada);
                        }
                        if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago > 0 && deudaSumaEquifax == 0)
                        {
                            ingresoPrevioEquifax = r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago * 1.40;
                            if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 != null && r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor > 0)
                            {
                                ingresoEstimadoEquifax = (double)r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor;
                                if (ingresoEstimadoEquifax > 0 && (double)ingresoEstimadoEquifax > ingresoPrevioEquifax)
                                    r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoEstimadoEquifax;
                                else
                                    r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                            }
                            else if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 == null)
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0 = new Externos.Logica.Equifax.Resultados._IndicadorCOVID0() { IncomePredictor = (decimal)ingresoPrevioEquifax };
                            else
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                        }
                        else if (r_burocreditoEquifax != null && r_burocreditoEquifax.Resultados != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb != null && r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago > 0 && deudaSumaEquifax > 0)
                        {
                            ingresoPrevioEquifax = r_burocreditoEquifax.Resultados.CuotaEstimadaMensualWeb.Pago * 1.40;
                            if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 != null && r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor > 0)
                            {
                                ingresoEstimadoEquifax = (double)r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor;
                                if (ingresoEstimadoEquifax > 0)
                                    r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoEstimadoEquifax;
                                else
                                    r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                            }
                            else if (r_burocreditoEquifax.Resultados.IndicadorCOVID0 == null)
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0 = new Externos.Logica.Equifax.Resultados._IndicadorCOVID0() { IncomePredictor = (decimal)ingresoPrevioEquifax };
                            else
                                r_burocreditoEquifax.Resultados.IndicadorCOVID0.IncomePredictor = (decimal)ingresoPrevioEquifax;
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                datos = new BuroCreditoMetodoViewModel()
                {
                    BuroCredito = r_burocredito,
                    DatosCache = cacheBuroCredito,
                    Fuente = planBuroCredito.Fuente,
                    BuroCreditoEquifax = r_burocreditoEquifax
                };

                _logger.LogInformation("Fuente de Buró de Crédito procesada correctamente");
                _logger.LogInformation($"Procesando registro de historiales de la fuente Buró de Crédito. Id Historial: {modelo.IdPadre}");

                try
                {
                    if (modelo.IdPadre > 0)
                    {
                        if (planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Aval)
                        {
                            var codigoBuro = new List<string> { "A401", "A402", "A410", "A404", "A405", "A406", "A407", "A408", "A409", "A411", "A412", "A415", "A416", "A418", "A419", "A420", "A909", "A999" };
                            var historialBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdPadre && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito);
                            if (historialBuroCredito != null)
                            {
                                if (!historialBuroCredito.Generado || !busquedaNuevaBuroCredito)
                                {
                                    historialBuroCredito.IdHistorial = modelo.IdPadre;
                                    historialBuroCredito.TipoFuente = Dominio.Tipos.Fuentes.BuroCredito;
                                    historialBuroCredito.Generado = datos.BuroCredito != null && !codigoBuro.Contains(datos.BuroCredito.ResponseCode);
                                    historialBuroCredito.Data = datos.BuroCredito != null ? JsonConvert.SerializeObject(datos.BuroCredito) : null;
                                    historialBuroCredito.Cache = cacheBuroCredito;
                                    historialBuroCredito.DataError = !string.IsNullOrEmpty(dataError) ? dataError : null;
                                    historialBuroCredito.FechaRegistro = DateTime.Now;
                                    historialBuroCredito.Reintento = true;
                                    historialBuroCredito.Observacion = datos.BuroCredito != null && !string.IsNullOrEmpty(datos.BuroCredito.Usuario) ? $"Usuario WS AVAL: {datos.BuroCredito.Usuario}" : null;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialBuroCredito);
                                    _logger.LogInformation("Historial de la Fuente Aval Buró de Crédito actualizado correctamente");
                                }
                            }
                            _logger.LogInformation("Historial de la Fuente Aval Buró de Crédito procesado correctamente");

                        }
                        else if (planBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Equifax)
                        {
                            var historialBuroCredito = await _detallesHistorial.FirstOrDefaultAsync(m => m, m => m.IdHistorial == modelo.IdPadre && m.TipoFuente == Dominio.Tipos.Fuentes.BuroCredito);
                            if (historialBuroCredito != null)
                            {
                                if (!historialBuroCredito.Generado || !busquedaNuevaBuroCredito)
                                {
                                    historialBuroCredito.IdHistorial = modelo.IdPadre;
                                    historialBuroCredito.TipoFuente = Dominio.Tipos.Fuentes.BuroCredito;
                                    historialBuroCredito.Generado = datos.BuroCreditoEquifax != null;
                                    historialBuroCredito.Data = datos.BuroCreditoEquifax != null ? JsonConvert.SerializeObject(datos.BuroCreditoEquifax) : null;
                                    historialBuroCredito.Cache = cacheBuroCredito;
                                    historialBuroCredito.DataError = !string.IsNullOrEmpty(dataError) ? dataError : null;
                                    historialBuroCredito.FechaRegistro = DateTime.Now;
                                    historialBuroCredito.Reintento = true;
                                    historialBuroCredito.Observacion = datos.BuroCreditoEquifax != null && !string.IsNullOrEmpty(datos.BuroCreditoEquifax.Usuario) ? $"Usuario WS Equifax: {datos.BuroCreditoEquifax.Usuario}" : null;
                                    await _detallesHistorial.ActualizarDetalleHistorialAsync(historialBuroCredito);
                                    _logger.LogInformation("Historial de la Fuente Equifax Buró de Crédito actualizado correctamente");
                                }
                            }
                            _logger.LogInformation("Historial de la Fuente Equifax Buró de Crédito procesado correctamente");
                        }
                    }
                    else
                        throw new Exception("El Id del Historial no se ha generado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                if (datos != null)
                {
                    if (datos.BuroCreditoEquifax != null)
                    {
                        datos.BuroCreditoEquifax.Usuario = null;
                        datos.BuroCreditoEquifax.Clave = null;
                    }

                    if (datos.BuroCredito != null)
                    {
                        datos.BuroCredito.Usuario = null;
                        datos.BuroCredito.Clave = null;
                    }
                }
                return datos;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return null;
            }
        }
    }
}
