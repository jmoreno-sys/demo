using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Abstracciones;
using Dominio.Interfaces;
using Dominio.Tipos;
using Dominio.Tipos.Clientes;
using Dominio.Tipos.Clientes.Cliente1790325083001;

namespace Dominio.Entidades.Balances
{
    public class ParametroClienteHistorial : Entidad, IAuditable
    {
        public virtual Historial Historial { get; set; }
        public int IdHistorial { get; set; }
        public string Valor { get; set; }
        public ParametrosClientes Parametro { get; set; }

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable

    }
}
