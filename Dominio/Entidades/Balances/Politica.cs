using System;
using System.Collections.Generic;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class Politica : Entidad, IAuditable
    {
        public Politica()
        {
            DetalleCalificaciones = new List<DetalleCalificacion>();
        }

        public virtual Empresa Empresa { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal CalificacionMinima { get; set; }
        public string Operador { get; set; }
        public string Variable1 { get; set; }
        public string Variable2 { get; set; }
        public bool Excepcional { get; set; }
        public FuentesPoliticas Fuente { get; set; }
        public Politicas Tipo { get; set; }
        public bool Estado { get; set; }

        #region Relaciones
        public IEnumerable<DetalleCalificacion> DetalleCalificaciones { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
