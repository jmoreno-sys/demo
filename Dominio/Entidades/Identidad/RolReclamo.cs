using Microsoft.AspNetCore.Identity;

namespace Dominio.Entidades.Identidad
{
    public class RolReclamo : IdentityRoleClaim<int>
    {
        public virtual Rol Rol { get; set; }
    }
}
