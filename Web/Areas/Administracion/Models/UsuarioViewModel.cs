// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Dominio.Tipos;

namespace Web.Areas.Administracion.Models
{
    public class UsuarioViewModel
    {
        public int Id { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public int IdEmpresa { get; set; }
        public int IdRol { get; set; }
        public bool AccesoEvaluacion { get; set; }
        public bool AccesoBuroCredito { get; set; }
    }
}
