using System;

namespace Web.Areas.Administracion.Models
{
    public class InformacionViewModel
    {
        public string Nombre { get; set; }
        public string Titulo { get; set; }
        public DateTime Publicacion { get; set; }
        public string Descripcion { get; set; }
        public Version Version { get; set; }
        public string Color { get; set; }
        public string Pruebas { get; set; }
        public string Sucesos { get; set; }
        public bool Dependencia { get; set; }
    }
}
