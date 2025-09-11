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
    public enum Planes : short
    {
        Desconocido = 0,
        RucsNaturalesJuridicos = 1,
        Cedulas = 2
    }

    public enum PlanesIdentificaciones : short
    {
        Desconocido = 0,
        Separado = 1,
        Unificado = 2
    }
}
