using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class Calificacion : Entidad, IAuditable
    {
        public Calificacion()
        {
            DetalleCalificacion = new List<DetalleCalificacion>();
        }
        public virtual Historial Historial { get; set; }
        public int IdHistorial { get; set; }
        public decimal Puntaje { get; set; }
        public bool Aprobado { get; set; }
        public string Observaciones { get; set; }
        public int NumeroAprobados { get; set; }
        public int NumeroRechazados { get; set; }
        public int TotalVerificados { get; set; }
        public double? Score { get; set; }
        public double? CupoEstimado { get; set; }
        public double? VentasEmpresa { get; set; }
        public double? PatrimonioEmpresa { get; set; }
        public string RangoIngreso { get; set; }
        public double? GastoFinanciero { get; set; }
        public TiposCalificaciones? TipoCalificacion { get; set; }
        public FuentesBuro? TipoFuenteBuro { get; set; }
        public bool CalificacionPersonalizada { get; set; }

        #region Relaciones
        public IEnumerable<DetalleCalificacion> DetalleCalificacion { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
