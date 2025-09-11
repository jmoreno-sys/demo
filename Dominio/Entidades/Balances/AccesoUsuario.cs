using System;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class AccesoUsuario : Entidad, IAuditable
    {
        public AccesoUsuario()
        {
            Estado = Dominio.Tipos.EstadosAccesos.Inactivo;
        }
        public virtual Usuario Usuario { get; set; }
        public int IdUsuario { get; set; }
        public TiposAccesos Acceso { get; set; }
        public EstadosAccesos Estado { get; set; }

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
