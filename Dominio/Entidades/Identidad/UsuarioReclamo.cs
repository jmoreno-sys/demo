using Microsoft.AspNetCore.Identity;

namespace Dominio.Entidades.Identidad
{
    public class UsuarioReclamo : IdentityUserClaim<int>
    {
        public virtual Usuario Usuario { get; set; }
    }
}
