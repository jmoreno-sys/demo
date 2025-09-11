// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infraestructura.Servicios
{
    public class CredencialesViewModel
    {
        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Method { get; set; }
    }

    public class ServicioSenescytViewModel
    {
        public Externos.Logica.Senescyt.Modelos.Persona Senescyt { get; set; }
    }

    public class ServicioSenescytNuevoViewModel
    {
        public bool TieneTitulos { get; set; }
        public Externos.Logica.Senescyt.Modelos.Persona Data { get; set; }
    }
}
