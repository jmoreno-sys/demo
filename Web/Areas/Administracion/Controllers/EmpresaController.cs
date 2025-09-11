using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Persistencia.Repositorios.Balance;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Web.Areas.Administracion.Models;
using Dominio.Entidades.Balances;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Infraestructura.Servicios;
using System.IO;

namespace Web.Areas.Administracion.Controllers
{
    [Area("Administracion")]
    [Route("Administracion/Empresa")]
    [Authorize(Policy = "Administracion")]
    public class EmpresaController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IEmpresas _empresas;
        private readonly IPlanesEmpresas _planesEmpresas;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly Externos.Logica.SRi.Controlador _sri;
        private readonly IEmailService _emailSender;
        private readonly IPlanesBuroCredito _planesBuroCredito;
        public EmpresaController(IConfiguration configuration, ILoggerFactory loggerFactory, IHttpContextAccessor httpContext, IEmpresas empresas, IPlanesEmpresas planesEmpresas, IPlanesBuroCredito planesBuroCredito, IPlanesEvaluaciones planesEvaluaciones, Externos.Logica.SRi.Controlador sri, IEmailService emailSender)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _httpContext = httpContext;
            _empresas = empresas;
            _planesEmpresas = planesEmpresas;
            _planesEvaluaciones = planesEvaluaciones;
            _planesBuroCredito = planesBuroCredito;
            _sri = sri;
            _emailSender = emailSender;
        }

        public IActionResult Inicio()
        {
            return View();
        }

        #region CRUD Empresa
        [Route("Listado")]
        [HttpPost]
        public async Task<IActionResult> Listado()
        {
            try
            {
                var data = await _empresas.ReadAsync(m => new
                {
                    m.Id,
                    m.Identificacion,
                    m.RazonSocial,
                    m.Direccion,
                    m.PersonaContacto,
                    Correo = string.IsNullOrEmpty(m.Correo) ? m.CorreoPersonaContacto : m.Correo,
                    Telefono = string.IsNullOrEmpty(m.Telefono) ? m.TelefonoPersonaContacto : m.Telefono,
                    m.RutaLogo,
                    m.RutaContrato,
                    m.Estado,
                    BaseImagen = !string.IsNullOrEmpty(m.RutaLogo) ? $"/app/logos/{m.RutaLogo}" : null,
                    PlanesEmpresas = m.PlanesEmpresas.FirstOrDefault(t => t.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo),
                    PlanDemo = m.PlanesEmpresas.Any(t => t.PlanDemostracion && t.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo),
                    PlanesEvaluaciones = m.PlanesEvaluaciones.FirstOrDefault(t => t.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo),
                    PlanBuroCredito = m.PlanesBuroCredito.FirstOrDefault(t => t.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo),
                    m.FechaCobroRecurrente,
                    m.Observaciones,
                    m.IdAsesorComercialConfiable,
                    AsesorComercialConfiable = m.AsesorComercialConfiable != null ? m.AsesorComercialConfiable.NombreCompleto : string.Empty,
                    NombreUsuarioRegistro = m.IdUsuarioRegistro.HasValue && m.UsuarioRegistro != null ? m.UsuarioRegistro.NombreCompleto : string.Empty,
                    m.FechaCreacion,
                    m.FechaModificacion
                }, null, o => o.OrderBy(t => (short)t.Estado).ThenByDescending(t => t.FechaCreacion), null, null, null, true);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("Guardar")]
        public async Task<IActionResult> Guardar(EmpresaViewModel modelo)
        {
            try
            {
                _logger.LogInformation("Registrando empresa...");
                if (modelo == null)
                    throw new Exception("No se ha ingresado los datos de la empresa");

                var idUsuario = User.GetUserId<int>();

                var empresa = new Empresa()
                {
                    Id = modelo.Id,
                    Identificacion = modelo.Identificacion,
                    RazonSocial = modelo.RazonSocial,
                    PersonaContacto = modelo.PersonaContacto,
                    Telefono = modelo.Telefono,
                    Correo = modelo.Correo,
                    Direccion = modelo.Direccion,
                    FechaCobroRecurrente = modelo.FechaCobroRecurrente,
                    Observaciones = modelo.Observaciones,
                    RutaLogo = modelo.BaseImagen,
                    UsuarioCreacion = idUsuario,
                    UsuarioModificacion = idUsuario,
                    IdAsesorComercialConfiable = modelo.IdAsesorComercialConfiable,
                    PlanesEmpresas = new List<PlanEmpresa>()
                    {
                        new PlanEmpresa()
                        {
                            Id = modelo.PlanEmpresa.Id,
                            NombrePlan = modelo.PlanEmpresa.NombrePlan,
                            ValorPlanAnualRuc = modelo.PlanEmpresa.ValorPlanAnualRuc,
                            ValorPlanAnualCedula = modelo.PlanEmpresa.ValorPlanAnualCedula,
                            ValorPlanMensualRuc = modelo.PlanEmpresa.ValorPlanMensualRuc,
                            ValorPlanMensualCedula = modelo.PlanEmpresa.ValorPlanMensualCedula,
                            NumeroConsultasRuc = modelo.PlanEmpresa.MaximoNumeroConsultas ? int.MaxValue : modelo.PlanEmpresa.NumeroConsultasRuc,
                            NumeroConsultasCedula = modelo.PlanEmpresa.MaximoNumeroConsultas ? int.MaxValue : modelo.PlanEmpresa.NumeroConsultasCedula,
                            ValorPorConsultaRucs = modelo.PlanEmpresa.ValorPorConsultaRucs,
                            ValorPorConsultaCedulas = modelo.PlanEmpresa.ValorPorConsultaCedulas,
                            ValorConsultaAdicionalRucs = modelo.PlanEmpresa.ValorConsultaAdicionalRucs,
                            ValorConsultaAdicionalCedulas = modelo.PlanEmpresa.ValorConsultaAdicionalCedulas,
                            TipoPlan = modelo.PlanUnificado ? Dominio.Tipos.PlanesIdentificaciones.Unificado : Dominio.Tipos.PlanesIdentificaciones.Separado,
                            NumeroConsultas = modelo.PlanEmpresa.NumeroConsultas,
                            BloquearConsultas = modelo.PlanEmpresa.BloquearConsultas,
                            ValorPlanAnual = modelo.PlanEmpresa.ValorPlanAnual,
                            ValorPlanMensual = modelo.PlanEmpresa.ValorPlanMensual,
                            ValorPorConsulta = modelo.PlanEmpresa.ValorPorConsulta,
                            ValorConsultaAdicional = modelo.PlanEmpresa.ValorConsultaAdicional,
                            PlanDemostracion = modelo.PlanDemostracion
                        }
                    }
                };

                if (modelo.HabilitarPlanEvaluacion && modelo.PlanEvaluacion != null)
                {
                    empresa.PlanesEvaluaciones = new List<PlanEvaluacion>()
                    {
                        new PlanEvaluacion()
                        {
                            Id = modelo.PlanEvaluacion.Id,
                            BloquearConsultas = modelo.PlanEvaluacion.BloquearConsultas,
                            NumeroConsultas = modelo.PlanEvaluacion.MaximoNumeroConsultas ? int.MaxValue : modelo.PlanEvaluacion.NumeroConsultas,
                            ValorConsulta = modelo.PlanEvaluacion.ValorConsulta,
                            ValorConsultaAdicional = modelo.PlanEvaluacion.ValorConsultaAdicional
                        }
                    };
                }
                else
                    empresa.PlanesEvaluaciones = null;

                if (modelo.HabilitarPlanBuroCredito && modelo.PlanBuroCredito != null)
                {
                    if (modelo.PlanBuroCredito.Fuente == Dominio.Tipos.FuentesBuro.Desconocido)
                        throw new Exception("La fuente para consultar Buró de Crédito no se encuentra registrada");

                    empresa.PlanesBuroCredito = new List<PlanBuroCredito>()
                    {
                        new PlanBuroCredito()
                        {
                            Id = modelo.PlanBuroCredito.Id,
                            BloquearConsultas = modelo.PlanBuroCredito.BloquearConsultas,
                            NumeroMaximoConsultas = modelo.PlanBuroCredito.NumeroMaximoConsultas,
                            ValorConsulta = modelo.PlanBuroCredito.ValorConsulta,
                            ValorConsultaAdicional = modelo.PlanEmpresa.ValorConsultaAdicional.HasValue && modelo.PlanEmpresa.ValorConsultaAdicional.Value > 0 ? modelo.PlanEmpresa.ValorConsultaAdicional.Value : modelo.PlanEmpresa.ValorConsultaAdicionalRucs > 0 ? modelo.PlanEmpresa.ValorConsultaAdicionalRucs: null,
                            PersistenciaCache = modelo.PlanBuroCredito.PersistenciaCache,
                            ValorPlanBuroCredito = modelo.PlanBuroCredito.ValorPlanBuroCredito,
                            Fuente = modelo.PlanBuroCredito.Fuente,
                            ConsultasCompartidas = modelo.PlanBuroCredito.ConsultasCompartidas,
                            NumeroMaximoConsultasCompartidas = !modelo.PlanBuroCredito.ConsultasCompartidas ? null : modelo.PlanBuroCredito.NumeroMaximoConsultasCompartidas,
                            ModeloCooperativas = modelo.PlanBuroCredito.ModeloCooperativas
                        }
                    };
                }
                else
                    empresa.PlanesBuroCredito = null;

                #region AdjuntoContrato
                byte[] contrato = null;

                if (Request.Form.Files.Any())
                {
                    _logger.LogInformation($"Procesando contrato de empresa...");
                    var fileContrato = Request.Form.Files.FirstOrDefault(m => m.Name.Contains("input-contrato"));
                    if (fileContrato != null)
                    {
                        if (fileContrato.Length == 0)
                            throw new Exception("Se ha cargado un archivo vacío, revisar el mismo por favor.");

                        if (Path.GetExtension(fileContrato.FileName.Trim().ToLower()) != ".pdf")
                            throw new Exception("La extensión del contrato no es válida, ingresar un contrato con la siguiente extensión: .pdf");

                        using (var binaryReader = new BinaryReader(fileContrato.OpenReadStream()))
                        {
                            contrato = binaryReader.ReadBytes((int)fileContrato.Length);
                        }
                    }
                    _logger.LogInformation($"Fin de procesamiento del contrato de empresa.");
                }
                #endregion AdjuntoContrato

                if (empresa.Id == 0)
                {
                    _logger.LogInformation("Registrando nueva empresa...");
                    empresa.UsuarioCreacion = idUsuario;
                    await _empresas.CrearAsync(empresa, contrato);
                }
                else
                {
                    _logger.LogInformation($"Editando empresa: {empresa.Id}...");
                    empresa.UsuarioModificacion = idUsuario;
                    await _empresas.EditarAsync(empresa, contrato, modelo.HabilitarPlanEvaluacion, modelo.HabilitarPlanBuroCredito);
                }

                _logger.LogInformation($"Registro de empresa: {empresa.Id} realizado correctamente");
                return Created(string.Empty, empresa.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("InactivarEmpresa")]
        public async Task<IActionResult> InactivarEmpresa(int id)
        {
            try
            {
                _logger.LogInformation($"Inactivando empresa: {id}...");
                if (id == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                var idUsuario = User.GetUserId<int>();
                await _empresas.InactivarEmpresaAsync(id, idUsuario);
                _logger.LogInformation($"Empresa: {id} inactivada correctamente");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("ActivarEmpresa")]
        public async Task<IActionResult> ActivarEmpresa(int id)
        {
            try
            {
                _logger.LogInformation($"Activando empresa: {id}...");
                if (id == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                var idUsuario = User.GetUserId<int>();
                await _empresas.ActivarEmpresaAsync(id, idUsuario);
                _logger.LogInformation($"Empresa: {id} activada correctamente");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("EliminarHistorial")]
        public async Task<IActionResult> EliminarHistorial(int id)
        {
            try
            {
                _logger.LogInformation($"Eliminando historial de empresa {id}...");
                if (id == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                await _empresas.EliminarHistorialAsync(id);
                _logger.LogInformation($"El historial de la empresa: {id} se ha eliminado correctamente");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }
        [HttpPost]
        [Route("EliminarHistorial7Anios")]
        public async Task<IActionResult> EliminarHistorial7Anios(int id)
        {
            try
            {
                _logger.LogInformation($"Eliminando historial de empresa {id}...");
                if (id == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                await _empresas.EliminarHistorial7AniosAsync(id);
                _logger.LogInformation($"El historial de la empresa: {id} se ha eliminado correctamente");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }
        #endregion CRUD Empresa

        #region Plan Empresa
        [HttpPost]
        [Route("ListadoPlanesEmpresa")]
        public async Task<IActionResult> ListadoPlanesEmpresa(int idEmpresa)
        {
            try
            {
                _logger.LogInformation($"Listado de planes de empresa...");
                if (idEmpresa == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                var data = await _planesEmpresas.ReadAsync(s => new
                {
                    s.Id,
                    s.NombrePlan,
                    EsValorMaximoRuc = s.NumeroConsultasRuc == int.MaxValue,
                    EsValorMaximoCedula = s.NumeroConsultasCedula == int.MaxValue,
                    EsValorMaximo = s.NumeroConsultas.HasValue ? s.NumeroConsultas == int.MaxValue : false,
                    s.NumeroConsultasRuc,
                    s.NumeroConsultasCedula,
                    s.ValorPlanAnualRuc,
                    s.ValorPlanAnualCedula,
                    s.ValorPlanMensualRuc,
                    s.ValorPlanMensualCedula,
                    s.ValorPorConsultaRucs,
                    s.ValorPorConsultaCedulas,
                    s.ValorConsultaAdicionalRucs,
                    s.ValorConsultaAdicionalCedulas,
                    s.FechaCreacion,
                    s.Estado,
                    s.TipoPlan,
                    s.NumeroConsultas,
                    s.ValorPlanAnual,
                    s.ValorPlanMensual,
                    s.ValorPorConsulta,
                    s.ValorConsultaAdicional,
                    s.BloquearConsultas,
                    s.PlanDemostracion
                }, p => p.IdEmpresa == idEmpresa, null, null, null, null, true);
                _logger.LogInformation($"Listado cargado de planes de empresa.");
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("InactivarPlan")]
        public async Task<IActionResult> InactivarPlanEmpresa(int idPlanEmpresa)
        {
            try
            {
                _logger.LogInformation($"Inactivando plan de empresa: {idPlanEmpresa}...");
                if (idPlanEmpresa == 0)
                    throw new Exception("No se ha podido encontrar el plan asociado");

                var idUsuario = User.GetUserId<int>();
                await _empresas.InactivarPlanEmpresaAsync(idPlanEmpresa, idUsuario);
                _logger.LogInformation($"Plan Empresa: {idPlanEmpresa} inactivado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("ActivarPlan")]
        public async Task<IActionResult> ActivarPlanEmpresa(int idPlanEmpresa)
        {
            try
            {
                _logger.LogInformation($"Activando plan de empresa: {idPlanEmpresa}...");
                if (idPlanEmpresa == 0)
                    throw new Exception("No se ha podido encontrar el plan asociado");

                var idUsuario = User.GetUserId<int>();
                await _empresas.ActivarPlanEmpresaAsync(idPlanEmpresa, idUsuario);
                _logger.LogInformation($"Plan Empresa: {idPlanEmpresa} activado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }
        #endregion Plan Empresa

        #region Plan Evaluación
        [HttpPost]
        [Route("ListadoPlanesEvaluacion")]
        public async Task<IActionResult> ListadoPlanesEvaluacion(int idEmpresa)
        {
            try
            {
                _logger.LogInformation($"Listado de planes evaluación...");
                if (idEmpresa == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                var data = await _planesEvaluaciones.ReadAsync(s => new
                {
                    Id = s.Id,
                    s.NumeroConsultas,
                    s.ValorConsulta,
                    s.ValorConsultaAdicional,
                    s.Estado,
                    FechaCreacion = s.FechaCreacion,
                    s.BloquearConsultas
                }, p => p.IdEmpresa == idEmpresa, null, null, null, null, true);
                _logger.LogInformation($"Listado cargado de planes de evaluación.");
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("InactivarPlanEvaluacion")]
        public async Task<IActionResult> InactivarPlanEvaluacion(int idPlanEvaluacion)
        {
            try
            {
                _logger.LogInformation($"Inactivando plan de evaluación: {idPlanEvaluacion}...");
                if (idPlanEvaluacion == 0)
                    throw new Exception("No se ha podido encontrar el plan asociado");

                var idUsuario = User.GetUserId<int>();
                await _empresas.InactivarPlanEvaluacionAsync(idPlanEvaluacion, idUsuario);
                _logger.LogInformation($"Plan de Evaluación: {idPlanEvaluacion} inactivado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("ActivarPlanEvaluacion")]
        public async Task<IActionResult> ActivarPlanEvaluacion(int idPlanEvaluacion)
        {
            try
            {
                _logger.LogInformation($"Activando plan de evaluación: {idPlanEvaluacion}...");
                if (idPlanEvaluacion == 0)
                    throw new Exception("No se ha podido encontrar el plan asociado");

                var idUsuario = User.GetUserId<int>();
                await _empresas.ActivarPlanEvaluacionAsync(idPlanEvaluacion, idUsuario);
                _logger.LogInformation($"Plan de Evaluación: {idPlanEvaluacion} activado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }
        #endregion Plan Evalución

        #region Plan Buró de Crédito
        [HttpPost]
        [Route("ListadoPlanesBuro")]
        public async Task<IActionResult> ListadoPlanesBuro(int idEmpresa)
        {
            try
            {
                _logger.LogInformation($"Listado de planes de buró de crédito...");
                if (idEmpresa == 0)
                    throw new Exception("No se ha podido encontrar la empresa");

                var data = await _planesBuroCredito.ReadAsync(s => new
                {
                    s.Id,
                    EsNumMaxConsulta = s.NumeroMaximoConsultas == int.MaxValue,
                    EsPersistenciaCache = s.PersistenciaCache == int.MaxValue,
                    s.ValorConsulta,
                    s.NumeroMaximoConsultas,
                    s.PersistenciaCache,
                    s.ValorPlanBuroCredito,
                    s.Estado,
                    s.FechaCreacion,
                    s.BloquearConsultas,
                    Fuente = s.Fuente.GetEnumDescription()
                }, s => s.IdEmpresa == idEmpresa, null, null, null, null, true);
                _logger.LogInformation($"Listado cargado de planes de buró de crédito.");
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Json(new List<dynamic>());
            }
        }

        [HttpPost]
        [Route("InactivarPlanBuro")]
        public async Task<IActionResult> InactivarPlanBuro(int idPlanBuro)
        {
            try
            {
                _logger.LogInformation($"Inactivando plan de buró de crédito: {idPlanBuro}...");
                if (idPlanBuro == 0)
                    throw new Exception("No se ha podido encontrar el plan asociado");

                var idUsuario = User.GetUserId<int>();
                await _empresas.InactivarPlanBuroAsync(idPlanBuro, idUsuario);
                _logger.LogInformation($"Plan Buró de Crédito: {idPlanBuro} inactivado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("ActivarPlanBuro")]
        public async Task<IActionResult> ActivarPlanBuro(int idPlanBuro)
        {
            try
            {
                _logger.LogInformation($"Activando plan de buró de crédito: {idPlanBuro}...");
                if (idPlanBuro == 0)
                    throw new Exception("No se ha podido encontrar el plan asociado");

                var idUsuario = User.GetUserId<int>();
                await _empresas.ActivarPlanBuroAsync(idPlanBuro, idUsuario);
                _logger.LogInformation($"Plan Buró de Crédito: {idPlanBuro} activado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }
        #endregion Plan Buró de Crédito

        #region Varios
        [HttpPost]
        [Route("BuscarIdentificacion")]
        public async Task<IActionResult> BuscarIdentificacion(string identificacion)
        {
            try
            {
                _logger.LogInformation($"Buscando información de empresa: {identificacion}...");
                if (string.IsNullOrWhiteSpace(identificacion?.Trim()))
                    throw new Exception("No se encontró información de esta identificación");

                var data = await _sri.GetContribuyenteBasico360Sri(identificacion.Trim());
                if (data == null)
                    throw new Exception("No se encontró información de esta identificación");

                _logger.LogInformation($"Búsqueda finalizada de información de empresa: {identificacion}");
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("ObtenerContratoEmpresa")]
        public async Task<IActionResult> ObtenerContratoEmpresa(int idEmpresa)
        {
            try
            {
                var empresa = await _empresas.FirstOrDefaultAsync(x => x, x => x.Id == idEmpresa);
                if (empresa == null)
                    throw new Exception("No se pudo obtener información de la empresa");

                if (string.IsNullOrEmpty(empresa.RutaContrato))
                    throw new Exception("No se encontró el nombre del contrato");

                var adjunto = Path.Combine("wwwroot", "app", "contratos", empresa.RutaContrato);

                if (!System.IO.File.Exists(adjunto))
                    throw new Exception("No se encontró el archivo del contrato");

                return File(System.IO.File.ReadAllBytes(adjunto), "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(EmpresaController), StatusCodes.Status500InternalServerError);
            }
        }
        #endregion Varios
    }
}
