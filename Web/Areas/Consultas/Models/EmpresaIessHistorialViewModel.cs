// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Web.Areas.Consultas.Models
{
    public class EmpresaIessHistorialViewModel
    {
        public int Id { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
    }
}
