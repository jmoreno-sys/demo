using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Web.Areas.Administracion.Models;
using Persistencia.Repositorios.Identidad;
using Dominio.Entidades.Identidad;
using Web.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.Linq;
using Persistencia.Repositorios.Balance;
using Dominio.Entidades.Balances;
using Infraestructura.Servicios;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Web.Areas.Administracion.Controllers
{
    [Area("Administracion")]
    [Route("Administracion/UsuarioCooprogreso")]
    [Authorize(Policy = "AdministracionUsuariosCooprogreso")]
    public class UsuarioCooprogresoController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IUsuarios _usuarios;
        private readonly IRoles _roles;
        private readonly IEmailService _emailSender;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly IPlanesBuroCredito _planesBuroCredito;
        private readonly Externos.Logica.Garancheck.Controlador _garancheck;

        public UsuarioCooprogresoController(IConfiguration configuration, ILoggerFactory loggerFactory, IHttpContextAccessor httpContext, IUsuarios usuarios, IPlanesBuroCredito planesBuroCredito, IPlanesEvaluaciones planesEvaluaciones, IRoles roles, Externos.Logica.Garancheck.Controlador garancheck, IEmailService emailSender)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(GetType());
            _httpContext = httpContext;
            _usuarios = usuarios;
            _emailSender = emailSender;
            _roles = roles;
            _garancheck = garancheck;
            _planesEvaluaciones = planesEvaluaciones;
            _planesBuroCredito = planesBuroCredito;
        }

        public async Task<IActionResult> Inicio()
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                ViewBag.PlanEvaluacion = await _planesEvaluaciones.AnyAsync(s => s.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1790451801001 && s.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo) ? 1 : 0;
                ViewBag.PlanBuro = await _planesBuroCredito.AnyAsync(s => s.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1790451801001 && s.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo) ? 1 : 0;

                return View();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("Listado")]
        public async Task<IActionResult> Listado()
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                var usuario = await _usuarios.ReadAsync(m => new
                {
                    m.Id,
                    m.Identificacion,
                    m.NombreCompleto,
                    m.Email,
                    m.UserName,
                    m.IdEmpresa,
                    IdentificacionEmpresa = m.Empresa.Identificacion,
                    m.Empresa.RazonSocial,
                    m.Estado,
                    IdRol = m.UsuariosRoles.Any() ? m.UsuariosRoles.FirstOrDefault().RoleId : 0,
                    NombreRol = m.UsuariosRoles.Any() ? m.UsuariosRoles.FirstOrDefault().Rol.Descripcion : string.Empty,
                    AccesoEvaluacion = m.Accesos.Any(x => x.Acceso == Dominio.Tipos.TiposAccesos.Evaluacion),
                    AccesoBuroCredito = m.Accesos.Any(x => x.Acceso == Dominio.Tipos.TiposAccesos.BuroCredito)
                }, m => m.IdEmpresa == Dominio.Constantes.Clientes.IdCliente1790451801001, o => o.OrderByDescending(m => m.Id));

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(UsuarioController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("Guardar")]
        public async Task<IActionResult> Guardar(UsuarioViewModel modelo)
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                _logger.LogInformation("Registrando usuario...");
                if (modelo == null)
                    throw new Exception("No se ha ingresado los datos del usuario");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                var rolesValidos = new[] { (short)Dominio.Tipos.Roles.AdministradorCooprogreso, (short)Dominio.Tipos.Roles.AdministradorEmpresa, (short)Dominio.Tipos.Roles.VendedorEmpresa, (short)Dominio.Tipos.Roles.OperadorEmpresa };
                if (!rolesValidos.Contains((short)modelo.IdRol))
                    throw new Exception("Se ha seleccionado un rol no admitido");

                var usuario = new Usuario()
                {
                    Id = modelo.Id,
                    Identificacion = modelo.Identificacion.ToUpper(),
                    NombreCompleto = modelo.NombreCompleto,
                    Email = modelo.Email,
                    IdEmpresa = Dominio.Constantes.Clientes.IdCliente1790451801001,
                    UsuarioCreacion = idUsuarioActual,
                    UsuariosRoles = new List<UsuarioRol>()
                    {
                        new UsuarioRol()
                        {
                            RoleId = modelo.IdRol
                        }
                    },
                };

                if (modelo.AccesoEvaluacion || modelo.AccesoBuroCredito)
                {
                    var accesos = new List<AccesoUsuario>();
                    if (modelo.AccesoBuroCredito)
                        accesos.Add(new AccesoUsuario() { Acceso = Dominio.Tipos.TiposAccesos.BuroCredito });

                    if (modelo.AccesoEvaluacion)
                        accesos.Add(new AccesoUsuario() { Acceso = Dominio.Tipos.TiposAccesos.Evaluacion });

                    usuario.Accesos = accesos;
                }

                if (modelo.Id == 0)
                {
                    _logger.LogInformation("Registrando nuevo usuario...");
                    var clave = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
                    usuario.PasswordHash = clave;
                    await _usuarios.CrearAsync(usuario);

                    #region Mail
                    try
                    {
                        _logger.LogInformation($"Preparando envío de correo para usuario: {usuario.Email}...");
                        var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.Bienvenida);
                        if (string.IsNullOrEmpty(template))
                            throw new Exception($"No se ha cargado la plantilla de tipo: {Dominio.Tipos.TemplatesCorreo.Bienvenida}");

                        var asunto = "Bienvenido/a a la Plataforma Integral de Información 360°";
                        var domainName = new Uri(HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
                        var enlace = $"{domainName}{Url.Action("Inicio", "Cuenta", new { Area = "Identidad" })}";

                        var rol = await _roles.FirstOrDefaultAsync(t => t, p => p.Id == (usuario.UsuariosRoles != null && usuario.UsuariosRoles.Any() ? usuario.UsuariosRoles.FirstOrDefault().RoleId : 0));
                        var replacements = new Dictionary<string, object>
                        {
                            { "{USERNAME}", usuario.UserName },
                            { "{PASSWORD}", clave },
                            { "{ENLACE}", enlace },
                            { "{ROL}", rol != null ? rol.Name : "" }
                        };
                        await _emailSender.SendEmailAsync(usuario.Email.Trim().ToLower(), asunto, template, usuario.NombreCompleto, replacements);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    #endregion Mail
                }
                else
                {
                    _logger.LogInformation($"Editando usuario: {modelo.Id}...");
                    if (usuario.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                        throw new Exception("El usuario no corresponde a su empresa para administrar");

                    await _usuarios.EditarAsync(usuario);
                }
                _logger.LogInformation($"Registro de usuario: {modelo.Id} realizado correctamente");
                return Created(string.Empty, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(UsuarioController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("Inactivar")]
        public async Task<IActionResult> Inactivar(int idUsuario)
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                _logger.LogInformation($"Inactivando el usuario {idUsuario}...");
                if (idUsuario == 0)
                    throw new Exception("No se ha podido encontrar el usuario");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                await _usuarios.InactivarAsync(idUsuario, idUsuarioActual, true);
                _logger.LogInformation($"Usuario: {idUsuario} inactivado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(UsuarioController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("Activar")]
        public async Task<IActionResult> Activar(int idUsuario)
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                _logger.LogInformation($"Activando el usuario {idUsuario}...");
                if (idUsuario == 0)
                    throw new Exception("No se ha podido encontrar el usuario");

                await _usuarios.ActivarAsync(idUsuario, idUsuarioActual, true);
                _logger.LogInformation($"Usuario: {idUsuario} activado correctamente.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(UsuarioController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("BuscarIdentificacion")]
        public async Task<IActionResult> BuscarIdentificacion(string identificacion)
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                _logger.LogInformation($"Buscando identificación {identificacion}...");
                if (string.IsNullOrWhiteSpace(identificacion?.Trim()))
                    throw new Exception("No se ha podido encontrar la identificación del usuario");

                var pathTipoFuente = System.IO.Path.Combine("wwwroot", "data", "fuentesInternas.json");
                var tipoFuente = JsonConvert.DeserializeObject<Web.Areas.Consultas.Models.ParametroFuentesInternasViewModel>(System.IO.File.ReadAllText(pathTipoFuente))?.FuentesInternas.RegistroCivil;

                var data = await _garancheck.GetRespuestaAsync(identificacion?.Trim());
                if (data == null)
                {
                    var dataResp = await _garancheck.GetRegistroCivilLineaBasicoAsync(identificacion?.Trim());
                    if (dataResp == null)
                        throw new Exception("No se ha podido encontrar la identificación del usuario");

                    _logger.LogInformation($"Identificación: {identificacion} consultada correctamente.");
                    var datos = new
                    {
                        Nombres = dataResp.Nombre,
                        Correo = string.Empty
                    };
                    return Ok(datos);
                }
                else
                {
                    _logger.LogInformation($"Identificación: {identificacion} consultada correctamente.");
                    var correos = data.Correos.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Value));
                    var datos = new
                    {
                        data.Nombres,
                        Correo = correos.Value
                    };
                    return Ok(datos);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(UsuarioController), StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Route("ReenviarCredenciales")]
        public async Task<IActionResult> ReenviarCredenciales(string identificacion)
        {
            try
            {
                var idUsuarioActual = User.GetUserId<int>();
                var usuarioActual = await _usuarios.ObtenerInformacionUsuarioAsync(idUsuarioActual);
                if (usuarioActual == null)
                    throw new Exception("Se ha terminado la sesión. Vuelva a actualizar la página por favor...");

                if (usuarioActual.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                    return Unauthorized();

                if (string.IsNullOrEmpty(identificacion?.Trim()))
                    throw new Exception("No se ha ingresado la identificación del usuario");

                var usuario = await _usuarios.FirstOrDefaultAsync(m => m, m => m.Identificacion.Trim().ToUpper() == identificacion.Trim().ToUpper() && m.Estado == Dominio.Tipos.EstadosUsuarios.Activo, null, i => i.Include(m => m.UsuariosRoles).ThenInclude(m => m.Rol));
                if (usuario == null)
                    throw new Exception("No existe un usuario activo registrado con esta identificación");

                var clave = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
                usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, clave);
                usuario.FechaModificacion = DateTime.Now;
                await _usuarios.UpdateAsync(usuario);
                _logger.LogInformation($"Clave de usuario: {usuario.Id} modificada");

                #region Mail
                var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.Bienvenida);
                if (string.IsNullOrEmpty(template))
                    _logger.LogError($"No se ha cargado la plantilla de Tipo: {Dominio.Tipos.TemplatesCorreo.Bienvenida}");
                else
                {
                    var asunto = "Reenvío de Contraseña Plataforma Integral de Información 360°";
                    var domainName = new Uri(HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
                    var enlace = $"{domainName}{Url.Action("Inicio", "Cuenta", new { Area = "Identidad" })}";
                    var rol = usuario.UsuariosRoles.FirstOrDefault();

                    var replacements = new Dictionary<string, object>
                    {
                        { "{USERNAME}", usuario.Identificacion },
                        { "{PASSWORD}", clave },
                        { "{ENLACE}", enlace },
                        { "{ROL}", rol != null ? rol.Rol.Name : "" }
                    };

                    await _emailSender.SendEmailAsync(usuario.Email, asunto, template, usuario.NombreCompleto, replacements, null);
                }
                #endregion Mail
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(UsuarioController), StatusCodes.Status500InternalServerError);
            }
        }
    }
}
