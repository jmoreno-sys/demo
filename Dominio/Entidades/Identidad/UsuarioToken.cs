using Microsoft.AspNetCore.Identity;

namespace Dominio.Entidades.Identidad
{
    public class UsuarioToken : IdentityUserToken<int>
    {
        public int Id { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}
