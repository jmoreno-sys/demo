using Dominio.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Dominio.Entidades.Identidad
{
    public class Rol : IdentityRole<int>, IEntidad<int>, IAuditable
    {
        public Rol()
        {
            ReclamosRol = new List<RolReclamo>();
            UsuariosRoles = new List<UsuarioRol>();
            Activo = true;
        }

        public string Descripcion { get; set; }
        public bool Activo { get; set; }

        #region Relaciones
        public IEnumerable<RolReclamo> ReclamosRol { get; set; }
        public IEnumerable<UsuarioRol> UsuariosRoles { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
