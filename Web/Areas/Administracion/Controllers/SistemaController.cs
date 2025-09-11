using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Web.Areas.Administracion.Models;
using Microsoft.AspNetCore.Identity;

namespace Web.Areas.Administracion.Controllers
{
    [Area("Administracion")]
    [Route("Administracion/Sistema")]
    [Authorize(Policy = "Administracion")]
    public class SistemaController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Informacion")]
        public IActionResult Informacion()
        {
            if (!User.IsInRole(Dominio.Tipos.Roles.Administrador))
                return Unauthorized();

            var dominio = Assembly.GetAssembly(typeof(Dominio.Extensiones));
            var persistencia = Assembly.GetAssembly(typeof(Persistencia.Extensiones));
            var web = Assembly.GetAssembly(typeof(Program));

            var models = new List<InformacionViewModel>
            {
                new InformacionViewModel()
                {
                    Nombre = dominio.GetName().Name,
                    Titulo = dominio.GetCustomAttributes(true).OfType<AssemblyTitleAttribute>().FirstOrDefault()?.Title,
                    Descripcion = dominio.GetCustomAttributes(true).OfType<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description,
                    Version = dominio.GetName().Version,
                    Publicacion = System.IO.File.GetCreationTime(dominio.Location),
                    Color = "#28487e"
                },

                new InformacionViewModel()
                {
                    Nombre = persistencia.GetName().Name,
                    Titulo = persistencia.GetCustomAttributes(true).OfType<AssemblyTitleAttribute>().FirstOrDefault()?.Title,
                    Descripcion = persistencia.GetCustomAttributes(true).OfType<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description,
                    Version = persistencia.GetName().Version,
                    Publicacion = System.IO.File.GetCreationTime(persistencia.Location),
                    Color = "#3d5a8a",
                },

                new InformacionViewModel()
                {
                    Nombre = web.GetName().Name,
                    Titulo = web.GetCustomAttributes(true).OfType<AssemblyTitleAttribute>().FirstOrDefault()?.Title,
                    Descripcion = web.GetCustomAttributes(true).OfType<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description,
                    Version = web.GetName().Version,
                    Publicacion = System.IO.File.GetCreationTime(web.Location),
                    Color = "#526c97",
                }
            };

            var assemblies = this.GetType().Assembly.GetReferencedAssemblies();

            foreach (var item in assemblies)
            {
                var model = new InformacionViewModel()
                {
                    Nombre = item.Name,
                    Version = item.Version,
                    Dependencia = true,
                };
                if (!models.Any(m => m.Nombre == model.Nombre))
                    models.Add(model);
            }

            return View(models);
        }
    }
}
