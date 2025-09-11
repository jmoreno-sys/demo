using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dominio.Entidades.Identidad;
using Infraestructura.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Repositorios.Balance;
using Persistencia.Repositorios.Identidad;
using Web.Areas.Identidad.Models;
using Web.Models;

namespace Web.Areas.Identidad.Controllers
{
    [Area("Identidad")]
    [AllowAnonymous]
    public class CuentaController : Controller
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger _logger;
        private readonly IUsuarios _usuarios;
        private readonly IEmpresas _empresas;
        private readonly IRoles _roles;
        private readonly IEmailService _emailSender;

        public CuentaController(SignInManager<Usuario> signInManager, ILoggerFactory loggerFactory, UserManager<Usuario> userManager, IUsuarios usuarios, IRoles roles, IEmpresas empresas, IEmailService emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger(GetType());
            _usuarios = usuarios;
            _empresas = empresas;
            _emailSender = emailSender;
            _roles = roles;
        }

        public IActionResult Inicio()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Inicio", "Principal", new { Area = "Consultas" });
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inicio(CuentaViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View();
                var user = await _usuarios.FirstOrDefaultAsync(m => m, m => m.UserName == model.Usuario, null, m => m.Include(m => m.Empresa.PlanesEmpresas).Include(m => m.Empresa.PlanesBuroCredito).Include(m => m.Empresa.PlanesEvaluaciones).Include(m => m.UsuariosRoles));
                if (user == null)
                    throw new Exception("Intento de inicio de sesión no válido.");

                if (user != null && user.Estado != Dominio.Tipos.EstadosUsuarios.Activo)
                    throw new Exception("Cuenta de usuario inactiva.");

                if (user != null && user.LockoutEnd.HasValue)
                    throw new Exception("Usuario bloqueado.");

                var empresa = user.Empresa;
                if (empresa != null && empresa.Estado == Dominio.Tipos.EstadosEmpresas.Inactivo)
                    throw new Exception("No es posible ingresar al sistema ya que su empresa se encuentra inactiva.");

                var result = await _signInManager.PasswordSignInAsync(model.Usuario, model.Clave, false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Usuario: {user.Id} conectado.");

                    if (user.Id == Dominio.Constantes.General.IdUsuarioDemo)
                        return Ok(new { url = Url.Action("Inicio", "PrincipalDemo", new { Area = "Consultas" }) });
                    else if (user.UsuariosRoles.FirstOrDefault().RoleId == (short)Dominio.Tipos.Roles.Reporteria)
                        return Ok(new { url = Url.Action("InicioHistorialEmpresa", "Historial", new { Area = "Historiales" }) });
                    else
                        return Ok(new { url = Url.Action("Inicio", "Principal", new { Area = "Consultas" }) });
                }
                else
                    throw new Exception("Intento de inicio de sesión no válido.");

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message, nameof(CuentaController), StatusCodes.Status401Unauthorized);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Salir(string returnUrl = null)
        {
            var userId = User.GetUserId();
            await _signInManager.SignOutAsync();
            _logger.LogInformation($"Usuario: {userId} desconectado");
            if (returnUrl != null)
                return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Inicio));
        }

        [HttpPost]
        public async Task<IActionResult> RecuperarCredenciales(string identificacion, string correo)
        {
            try
            {
                if (string.IsNullOrEmpty(identificacion?.Trim()) || string.IsNullOrEmpty(correo?.Trim()))
                    throw new Exception("No se han ingresado los datos del usuario");

                var usuario = await _usuarios.FirstOrDefaultAsync(m => m, m => m.Identificacion.Trim().ToUpper() == identificacion.Trim().ToUpper() && m.Email.Trim().ToUpper() == correo.Trim().ToUpper() && m.Estado == Dominio.Tipos.EstadosUsuarios.Activo, null, i => i.Include(m => m.UsuariosRoles).ThenInclude(m => m.Rol));
                if (usuario == null)
                    throw new Exception("No existe un usuario activo registrado con estos datos");

                var clave = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
                usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, clave);
                usuario.FechaModificacion = DateTime.Now;
                await _usuarios.UpdateAsync(usuario);
                _logger.LogInformation($"Clave de usuario: {usuario.Id} modificada");

                if (usuario.LockoutEnd.HasValue)
                {
                    await _usuarios.DesbloquearAsync(usuario.Id, usuario.Id);
                    _logger.LogInformation($"Usuario {usuario.Id} desbloqueado y credenciales enviadas");
                }

                #region Mail
                var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.Bienvenida);
                if (string.IsNullOrEmpty(template))
                    _logger.LogError($"No se ha cargado la plantilla de Tipo: {Dominio.Tipos.TemplatesCorreo.Bienvenida}");
                else
                {
                    var asunto = "Recuperación de Contraseña Plataforma Integral de Información 360°";
                    var domainName = new Uri(HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
                    var enlace = $"{domainName}{Url.Action("Inicio", "Cuenta")}";
                    var rol = usuario.UsuariosRoles.FirstOrDefault();

                    var replacements = new Dictionary<string, object>
                    {
                        { "{USERNAME}", usuario.Identificacion },
                        { "{PASSWORD}", clave },
                        { "{ENLACE}", enlace },
                        { "{ROL}", rol != null ? rol.Rol.Name : "" }
                    };
                    await _emailSender.SendEmailAsync(usuario.Email, asunto, template, usuario.NombreCompleto, replacements);
                }
                #endregion Mail
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(CuentaController), StatusCodes.Status500InternalServerError);
            }
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CambiarClave(CambioClaveViewModel model)
        {
            try
            {
                if (model == null)
                    throw new Exception("No se han ingresado los datos del usuario");

                if (string.IsNullOrEmpty(model.Clave?.Trim()) || string.IsNullOrEmpty(model.ConfirmaClave?.Trim()) || string.IsNullOrEmpty(model.ClaveAnterior?.Trim()))
                    throw new Exception("No se han ingresado todos los datos necesarios para el cambio de clave");

                if (model.Clave?.Trim() != model.ConfirmaClave?.Trim())
                    throw new Exception("La clave confirmada no coincide");

                var usuarioActual = User.GetUserId<int>();
                var usuario = await _usuarios.FirstOrDefaultAsync(m => m, m => m.Id == usuarioActual && m.Estado == Dominio.Tipos.EstadosUsuarios.Activo);
                if (usuario == null)
                    throw new Exception("No existe un usuario activo registrado con estos datos");

                var passwordHashAnterior = new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.PasswordHash, model.ClaveAnterior);
                if (passwordHashAnterior == PasswordVerificationResult.Failed)
                    throw new Exception("La contraseña actual no es la correcta");

                var clave = model.Clave;
                usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, clave);
                usuario.FechaModificacion = DateTime.Now;
                await _usuarios.UpdateAsync(usuario);
                _logger.LogInformation($"Clave de usuario: {usuario.Id} modificada");

                #region Mail
                var template = EmailViewModel.ObtenerSubtemplate(Dominio.Tipos.TemplatesCorreo.CambioClave);
                if (string.IsNullOrEmpty(template))
                    _logger.LogError($"No se ha cargado la plantilla de Tipo: {Dominio.Tipos.TemplatesCorreo.CambioClave}");
                else
                {
                    var asunto = "Cambio de Contraseña Plataforma Integral de Información 360°";
                    var domainName = new Uri(HttpContext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority);
                    var enlace = $"{domainName}{Url.Action("Inicio", "Cuenta")}";
                    var replacements = new Dictionary<string, object>
                    {
                        { "{USERNAME}", usuario.Identificacion },
                        { "{ENLACE}", enlace }
                    };
                    await _emailSender.SendEmailAsync(usuario.Email, asunto, template, usuario.NombreCompleto, replacements);
                }
                #endregion Mail
                return Ok(new { url = Url.Action("Inicio", "Principal", new { Area = "Consultas" }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(ex.Message, nameof(CuentaController), StatusCodes.Status500InternalServerError);
            }
        }
    }
}
