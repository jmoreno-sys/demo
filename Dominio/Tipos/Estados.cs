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
    public enum EstadosEmpresas : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2
    }

    public enum EstadosUsuarios : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2
    }

    public enum EstadosPlanes : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2,
    }

    public enum EstadosPlanesEmpresas : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2,
        Bloqueado = 3
    }

    public enum EstadosAccesos : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2
    }

    public enum EstadosPlanesBuroCredito : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2,
        Bloqueado = 3
    }

    public enum EstadosPlanesEvaluaciones : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2,
        Bloqueado = 3
    }

    public enum EstadosCredenciales : short
    {
        Desconocido = 0,
        Activo = 1,
        Inactivo = 2,
        Bloqueado = 3
    }
}
