// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Constantes
{
    public abstract class Politicas
    {
        public const string PoliticaLegalCheque = "COBRO DE CHEQUE";
        public const string PoliticaLegalPagare = "COBRO DE PAGARÉ A LA ORDEN";
        public const string PoliticaLegalFactura = "COBRO DE FACTURAS";

        public const string PoliticaLegalRoboDocumento = "ROBO DOCUMENTO";
        public const string PoliticaLegalArrendamiento = "ARRENDAMIENTO";
        public const string PoliticaLegalCobroDinero = "COBRO DE DINERO";
        public const string PoliticaLegalRobo = "ROBO";
        public const string PoliticaLegalPrivacionPatria = "PRIVACIÓN DE LA PATRIA POTESTAD";
        public const string PoliticaLegalPrivacionPatriaTilde = "PRIVACION DE LA PATRIA POTESTAD";
        public const string PoliticaLegalPrescripcionExtraordinaria = "PRESCRIPCION EXTRAORDINARIA ADQUISITIVA DE DOMINIO";
        public const string PoliticaLegalDinero = "DINERO";
        public const string PoliticaLegalOtros = "OTROS";
        public const string PoliticaLegalLetraCambio = "LETRA DE CAMBIO";

        public const string RucActivo = "ACT";
    }

    public abstract class TextoReferencia
    {
        public const string MenorIgual = "Menor o Igual a: {0}";
        public const string MayorIgual = "Mayor o Igual a: {0}";
        public const string Maximo = "Máximo: {0}";
        public const string Minimo = "Mínimo: {0}";
        public const string Menor = "Menor a: {0}";
        public const string Mayor = "Mayor a: {0}";
        public const string MenorMeses = "Mayor a: {0} meses";
        public const string MayorIgualMeses = "Mayor o Igual a: {0} meses";

        public const string MenorIgualMoneda = "Menor o Igual a: ${0:0,0.00}";
        public const string MayorIgualMoneda = "Mayor o Igual a: ${0:0,0.00}";
        public const string MayorMoneda = "Mayor a: ${0:0,0.00}";
        public const string MenorMoneda = "Menor a: ${0:0,0.00}";
        public const string MaximoMoneda = "Máximo: ${0:0,0.00}";
        public const string MinimoMoneda = "Mínimo: ${0:0,0.00}";
        public const string Pasivos = "Menor o Igual al 80% de los Activos: ${0:0,0.00}";
        public const string Pasivos60 = "Menor o Igual al 60% de los Activos: ${0:0,0.00}";
        public const string GastosOperacionales = "Menor o Igual al 80% de Ingresos: ${0:0,0.00}";
        public const string GastosOperacionales60 = "Menor o Igual al 60% de Ingresos: ${0:0,0.00}";
        public const string CuentasXPagar = "Menor o Igual a Cuentas por Cobrar: ${0:0,0.00}";
        public const string GastosFinanciero = "Menor al 80% de Ingresos: ${0:0,0.00} ({1:0,0.00})";
        public const string GastosFinanciero50 = "Menor al 50% de Ingresos: ${0:0,0.00} ({1:0,0.00})";
        public const string GastosFinanciero60 = "Menor al 60% de Ingresos: ${0:0,0.00} ({1:0,0.00})";

        public const string MinimoAnios = "Mínimo: {0} años";
        public const string MayorIgualAnios = "Mayor o Igual a: {0} años";
        public const string MayorIgualAnio = "Mayor o Igual a: {0} año";
        public const string MayorAnios = "Mayor a: {0} años";
        public const string MayorIgualAniosRuc = "Mayor o Igual a: {0} años y estado del RUC ACTIVO";
        public const string MayorIgualAnioRuc = "Mayor o Igual a: {0} año y estado del RUC ACTIVO";
        public const string IgualAnio = "Igual al año: {0}";

        public const string MenorIgualDias = "Menor o Igual a: {0} días";

        public const string FechaAnios = "{0} ({1} años)";
        public const string FechaAniosBancoCapital = "Inicio: {0} - Reinicio: {1} ({2} años)";

        public const string Edad = "{0} años";
        public const string EstadoAfiliacion = "Estado de Afiliación: {0}";
        public const string Titulos = "Títulos: {0}";
        public const string Procesos = "Procesos: {0}";
        public const string Dias = "{0} días";
        public const string Maximodias = "Maximo: {0} días";

        public const string MenorIgualFecha = "Menor o Igual a la fecha: {0}";
        public const string MenorIgualFechaSansion = "Menor o Igual a la fecha: {0}. Fecha de cumplimiento: {1}";

        public const string Observacion = "Política aprobada por no tener datos del buró de crédito: {0}";

        public const string EstadoTributario = "No presentar: Obligaciones Tributarias Pendientes";

        public const string EstadoSuperBancos = "Estado de titulares de cuenta corriente: {0}";
        public const string AntecedentesPenales = "Registra Antecedentes: {0}";
        public const string NoticiaDelito = "Delitos como procesado: {0}";

        public const string Predios = "Predios: {0}";

        public const string EstadoAfiliacionIndumot = "No presentar: RUC INACTIVO y en el estado IESS: INACTIVO O CESANTE";
    }
    public abstract class ConstantesCalificacion
    {
        public const int MinimoScoreBuro = 750;
        public const int MinimoScoreBuroRagui = 801;
        public const int MinimoScoreBuroAlmacenesEsp = 151;
        public const int MinimoScoreBuroOtomRacing = 601;
        public const int MinimoScoreCoopAndina = 500;
        public const int MinimoCalificacion = 70;
    }
}
