// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos.Clientes.Cliente1590001585001
{
    public enum TipoCreditoCoopTena : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("CONSUMO")]
        Consumo = 1,
        [Description("CONSUMO SIN ENCAJE")]
        ConsumoSinEncaje = 2,
        [Description("CREDI CASH")]
        CrediCash = 3,
        [Description("MICROCREDITO")]
        Microcredito = 4,
        [Description("MICROCREDDITO SIN ENCAJE")]
        MicrocreditoSinEncaje = 5,
        [Description("CONAFIPS")]
        CONAFIPS = 6,
        [Description("CONSUMO DPF")]
        ConsumoDPF = 7,
        [Description("CONSUMO RES REF")]
        ConsumoRESREF = 8,
        [Description("MICROCREDITO REF RES")]
        MicrocreditoREFRES = 9,
    }
}
