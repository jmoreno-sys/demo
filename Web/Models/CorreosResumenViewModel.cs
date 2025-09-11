// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;
using Web.Areas.Consultas.Models;

namespace Web.Models
{
    public class ConsultaResumenViewModel
    {
        public string RazonSocial { get; set; }
        public int ConsultasRealizadas { get; set; }
        public int ConsultasRealizadasBuro { get; set; }
        public int TotalConsultas { get; set; }
        public int SaldoConsultasMes { get; set; }
        public int ConsultasRealizadasDia { get; set; }
    }

    public class CorreosViewModel
    {
        public string Nombre { get; set; }
        public string Correo { get; set; }
    }
}
