using Dominio.Tipos;

namespace Web.Areas.Consultas.Models
{
    public class MatrizDualViewModel
    {
        public Matriz Tipo { get; set; }
        public string Segmento { get; set; }
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

        public int InstitucionesFinancieras { get; set; }
        public double? DeudaCasaComercial { get; set; }
        public int RegistroVencido { get; set; }
        public double? CreditoCastigado { get; set; }
    }
}
