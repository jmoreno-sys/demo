// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos.Clientes.Cliente1790325083001
{
    public enum SegmentoCartera : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("MICROCRÉDITO")]
        MicroCredito = 1,
        [Description("CONSUMO")]
        Consumo = 2
    }
}
