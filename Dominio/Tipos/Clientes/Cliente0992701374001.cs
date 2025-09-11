// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos.Clientes.Cliente0992701374001
{
    public enum TipoBuroCredito : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("Modelo Financial")]
        ModeloFinancial = 1,
        [Description("Modelo DMiro")]
        ModeloDMiro = 2
    }

    public enum TipoPrestamo : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("microcredito")]
        PrestamoMicrocredito = 1
    }

    public enum EstadoCivil : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("CASADO/A")]
        EstadoCivilCasado = 1,
        [Description("DIVORCIADO/A")]
        EstadoCivilDivorciado = 2,
        [Description("SOLTERO/A")]
        EstadoCivilSoltero = 3,
        [Description("UNION LIBRE")]
        EstadoCivilUnionLibre = 4,
        [Description("VIUDO/A")]
        EstadoCivilViudo = 5
    }

    public enum TipoInstruccion : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("Primaria")]
        InstruccionPrimaria = 1,
        [Description("Postgrado")]
        InstruccionPostgrado = 2,
        [Description("Secundaria")]
        InstruccionSecundaria = 3,
        [Description("Sin estudios")]
        InstruccionSinEstudios = 4,
        [Description("Superior")]
        InstruccionSuperior = 5,
        [Description("Universitaria")]
        InstruccionUniversitaria = 6
    }
}
