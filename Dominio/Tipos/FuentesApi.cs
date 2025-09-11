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
    public enum FuentesApi : short
    {
        Desconocido = 0,
        TodasFuentes = 1,
        Sri = 2,
        Civil = 3,
        Societario = 4,
        Iess = 5,
        Senescyt = 6,
        Legal = 7,
        Sercop = 8,
        Ant = 9,
        PensionAlimenticia = 10,
        SuperBancos = 11,
        AntecedentesPenales = 12,
        Predios = 13,
        FiscaliaDelitos = 14,
        PrediosCuenca = 15,
        PrediosStoDomingo = 16,
        PrediosRuminahui = 17,
        PrediosQuininde = 18,
        Uafe = 19,
        Impedimento = 20,
        PrediosLatacunga = 21,
        PrediosManta = 22,
        PrediosAmbato = 23,
        PrediosIbarra = 24,
        PrediosSanCristobal = 25,
        PrediosDuran = 26,
        PrediosLagoAgrio = 27,
        PrediosSantaRosa = 28,
        PrediosSucua = 29,
        PrediosSigSig = 30,
        FuerzasArmadas = 31,
        DeNoBaja = 32,
        PrediosMejia = 33,
        PrediosMorona = 34,
        PrediosTena = 35,
        PrediosCatamayo = 36,
        PrediosLoja = 37,
        PrediosSamborondon = 38,
        PrediosDaule = 39,
        PrediosCayambe = 40,
        PrediosAzogues = 41,
        PrediosEsmeraldas = 42,
        PrediosCotacachi = 43,
        SriBasico = 201,
        SriHistorico = 202,
        CivilBasico = 301,
        CivilHistorico = 302
    }

    //Clientes
    //AMC
    public enum FuentesApiAmc : short
    {
        Desconocido = 0,
        Sri = 1,
        Civil = 2,
        Societario = 3,
        Iess = 4,
        LegalEmpresa = 5,
        LegalRepresentante = 6,
        BuroAval = 7,
        CivilLinea = 8
    }
}
