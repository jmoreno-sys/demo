using System;

namespace Dominio.Interfaces
{
    public interface IAuditable
    {
        int? UsuarioCreacion { get; set; }
        int? UsuarioModificacion { get; set; }
        DateTime FechaCreacion { get; set; }
        DateTime? FechaModificacion { get; set; }
    }
}
