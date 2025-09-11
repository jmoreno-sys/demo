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
    public enum ErroresApi : short
    {
        Desconocido = 0,
        CredencialesFaltantes = 1,
        UsuarioInactivo = 2,
        CredencialesIncorrectas = 3,
        ParametrosFaltantes = 4,
        IdentificacionInvalida = 5,
        FuentesIncorrectas = 6,
        PlanesConsultas = 7,
        ProcesamientoConsulta = 8,
        NoAutorizado = 9,
    }
}
