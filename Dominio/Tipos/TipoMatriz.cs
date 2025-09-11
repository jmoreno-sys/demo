// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos
{
    public enum Matriz : short
    {
        Desconocido = 0,
        Bancarizado = 1,
        NoBancarizado = 2
    }

    public enum Segmento : short
    {
        Desconocido = 0,
        DIAMANTE = 1,
        ORO = 2,
        PLATA = 3,
        BRONCE = 4,
        COBRE = 5,
        NoBancarizado = 6,
    }
}
