using System;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class MatrizDualResultado : Entidad, IAuditable
    {
        public Matriz Tipo { get; set; }
        public Segmento Segmento { get; set; }
        public double? MontoDesde { get; set; }
        public double? MontoHasta { get; set; }
        public int Plazo { get; set; }
        public string Requisitos { get; set; }
        public double? Tasa { get; set; }
        public double? Encaje { get; set; }
        public bool? Casa { get; set; }
        public bool Estado { get; set; }
        public int ScoreInclusion { get; set; }
        public int ScoreBuro { get; set; }
        public int IdHistorial { get; set; }


        public bool InstitucionesFinancierasEstado { get; set; }
        public bool DeudaCasaComercialEstado { get; set; }
        public bool RegistroVencidoEstado { get; set; }
        public bool CreditoCastigadoEstado { get; set; }
        public string Rangos { get; set; }


        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
