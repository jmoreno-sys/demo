using System;
using Dominio.Abstracciones;
using Dominio.Interfaces;

namespace Dominio.Entidades.Balances
{
    public class DetalleCalificacion : Entidad, IAuditable
    {
        public DetalleCalificacion()
        {
            Aprobado = false;
        }

        public virtual Politica Politica { get; set; }
        public virtual Calificacion Calificacion { get; set; }
        public int IdCalificacion { get; set; }
        public int IdPolitica { get; set; }
        public string Valor { get; set; }
        public string Parametro { get; set; }
        public string Datos { get; set; }
        public string ReferenciaMinima { get; set; }
        public bool Aprobado { get; set; }
        public string Observacion { get; set; }
        public DateTime? FechaCorte { get; set; }
        public string Instituciones { get; set; }

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
