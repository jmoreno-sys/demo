using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Dominio.Entidades.Identidad
{
    public class UsuarioAcceso : IdentityUserLogin<int>
    {
        public int Id { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
}
