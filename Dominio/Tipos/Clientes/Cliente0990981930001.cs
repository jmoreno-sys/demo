// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos.Clientes.Cliente0990981930001
{
    public enum TipoLinea : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("Automotriz")]
        Automotriz = 1,
        [Description("Consumo")]
        Consumo = 2
    }

    public enum TipoCredito : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("Automotriz Nuevo BDL")]
        AutomotrizNuevoBDL = 1,
        [Description("Automotriz Usado BDL")]
        AutomotrizUsadoBDL = 2,
        [Description("Camiones Microcréditos BDL")]
        CamionesMicrocréditosBDL = 3
    }

    public enum TipoCreditoMicrofinanzas : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("RENOVACION DE SEGUROS, DEDUCIBLES Y DISPOSITIVOS")]
        RenovacionSeguros = 1,
        [Description("PAGO DE BIENES O SERVICIOS")]
        PagoBienesServicios = 2
    }

    public enum TipoBuroModelo : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("Modelo Covid360")]
        ModeloCovid360 = 1,
        [Description("Modelo Automotriz")]
        ModeloAutomotriz = 2,
        [Description("Modelo Microfinanzas")]
        ModeloMicrofinanzas = 3
    }
}
