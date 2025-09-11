// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos
{
    public enum FuentesBuro : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("AVAL")]
        Aval = 1,
        [Description("EQUIFAX")]
        Equifax = 2
    }

    public enum TipoPrestamoCooperativasAval : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("microcredito")]
        Microcredito = 1,
        [Description("consumo")]
        Consumo = 2
    }
}
