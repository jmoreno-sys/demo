using Dominio.Tipos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Models
{
    public static class EmailViewModel
    {
        public static string ObtenerSubtemplate(TemplatesCorreo tipoMail)
        {
            var pathTemplate = $"{tipoMail.ToString().ToLower()}.html";
            var fileName = Path.Combine("wwwroot", "templates", pathTemplate);
            return File.ReadAllText(fileName);
        }
    }
}
