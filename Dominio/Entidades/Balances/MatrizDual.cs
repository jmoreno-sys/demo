using System;
using System.Collections.Generic;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class MatrizDual : Entidad, IAuditable
    {
        public MatrizDual()
        {
            //DetalleCalificaciones = new List<DetalleCalificacion>();
        }

        public virtual Empresa Empresa { get; set; }
        public int IdEmpresa { get; set; }
        public Matriz Tipo { get; set; }
        public Segmento Segmento { get; set; }
        public double? MontoDesde { get; set; }
        public double? MontoHasta { get; set; }
        public int Plazo { get; set; }
        public string Requisitos { get; set; }
        public double? Tasa { get; set; }
        public int InstitucionesFinancieras { get; set; }
        public double? DeudaCasaComercial { get; set; }
        public int RegistroVencido { get; set; }
        public double? CreditoCastigado { get; set; }
        public double? Encaje { get; set; }
        public bool? Casa { get; set; }

        public int ScoreInclusionDesde { get; set; }
        public int ScoreInclusionHasta { get; set; }
        public int ScoreBuroDesde { get; set; }
        public int ScoreBuroHasta { get; set; }

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
