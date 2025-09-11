using Microsoft.AspNetCore.Identity;

namespace Dominio.Entidades.Identidad
{
    public class UsuarioRol : IdentityUserRole<int>
    {
        public int Id { get; set; }
        public virtual Usuario Usuario { get; set; }
        public virtual Rol Rol { get; set; }
    }
}
