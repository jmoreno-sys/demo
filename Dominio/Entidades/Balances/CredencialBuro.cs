// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Dominio.Abstracciones;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class CredencialBuro : Entidad, IAuditable
    {
        public virtual Empresa Empresa { get; set; }
        public int IdEmpresa { get; set; }

        public string Usuario { get; set; }
        public string Clave { get; set; }
        public string Enlace { get; set; }
        public bool CovidRest { get; set; }
        public FuentesBuro TipoFuente { get; set; }
        public EstadosCredenciales Estado { get; set; }
        public string TokenAcceso { get; set; }
        public string ProductData { get; set; }
        public DateTime? FechaCreacionToken { get; set; }

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
