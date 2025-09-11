// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Abstracciones;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class DetalleHistorial : Entidad
    {
        public virtual Historial Historial { get; set; }
        public int IdHistorial { get; set; }
        public Fuentes TipoFuente { get; set; }
        public bool Generado { get; set; }
        public string Data { get; set; }
        public string Observacion { get; set; }
        public bool Cache { get; set; }
        public bool? FuenteActiva { get; set; }
        public string DataError { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public bool? Reintento { get; set; }
    }
}
