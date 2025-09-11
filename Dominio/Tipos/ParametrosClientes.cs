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
    public enum ParametrosClientes : short
    {

        // BANCO CAPITAL
        TipoPrestamoBCapital = 20,
        PropiedadesBCapital = 21,
        MesesExperienciaActividadBCapital = 22,
        IdentificacionConyugeBCapital = 23,
        PlazoBCapital = 24,
        MontoSolicitadoBCapital = 25,
        GastosBCapital = 26,
        IngresosBCapital = 27,
        TipoIdentificacionConyugeBCapital = 28,


        Desconocido = 0,
        /// <summary>
        /// Empresa: 1790325083001
        /// </summary>
        SegmentoCartera = 1,

        /// <summary>
        /// Ingresos Web Service COAC Aval
        /// </summary>
        IngresosCoac = 2,
        /// <summary>
        /// Gastos Web Service COAC Aval
        /// </summary>
        GastosCoac = 3,
        /// <summary>
        /// Monto Solicitado Web Service COAC Aval
        /// </summary>
        MontoSolicitadoCoac = 4,
        /// <summary>
        /// Plazo Web Service COAC Aval
        /// </summary>
        PlazoCoac = 5,
        /// <summary>
        /// Tipo Prestamo Web Service COAC Aval
        /// </summary>
        TipoPrestamoCoac = 8,

        #region Indumot
        /// <summary>
        /// Conyuge Web Service Indumot Equifax
        /// </summary>
        IdentificacionConyugeIndumot = 9,
        /// <summary>
        /// Garante Web Service Indumot Equifax
        /// </summary>
        IdentificacionGaranteIndumot = 10,
        /// <summary>
        /// Tipo Producto Web Service Indumot Equifax
        /// </summary>
        TipoProductoIndumot = 11,
        /// <summary>
        /// Ingresos Web Service Indumot Equifax
        /// </summary>
        IngresosIndumot = 12,
        /// <summary>
        /// Gastos Web Service Indumot Equifax
        /// </summary>
        GastosIndumot = 13,
        /// <summary>
        /// Monto Web Service Indumot Equifax
        /// </summary>
        MontoIndumot = 14,
        /// <summary>
        /// Plazo Web Service Indumot Equifax
        /// </summary>
        PlazoIndumot = 15,
        /// <summary>
        /// Valor Entrada Web Service Indumot Equifax
        /// </summary>
        ValorEntrada = 16,
        #endregion IndumotS

        #region Banco Litoral
        /// <summary>
        /// Linea Banco Litoral
        /// </summary>
        LineaBLitoral = 17,
        /// <summary>
        /// Tipo Crédito Banco Litoral
        /// </summary>
        TipoCreditoBLitoral = 18,
        /// <summary>
        /// Plazo Banco Litoral
        /// </summary>
        PlazoBLitoral = 19,
        /// <summary>
        /// Monto Banco Litoral
        /// </summary>
        MontoBLitoral = 20,
        /// <summary>
        /// Ingreso Banco Litoral
        /// </summary>
        IngresoBLitoral = 21,
        /// <summary>
        /// Gasto Hogar Banco Litoral
        /// </summary>
        GastoHogarBLitoral = 22,
        /// <summary>
        /// Resta Gasto Financiero Banco Litoral
        /// </summary>
        RestaGastoFinancieroBLitoral = 23,
        /// <summary>
        /// Tipo Documento Banco Litoral
        /// </summary>
        TipoDocumentoBLitoral = 24,
        /// <summary>
        /// Numero Documento Banco Litoral
        /// </summary>
        NumeroDocumentoBLitoral = 25,
        #endregion Banco Litoral

        #region Cooperatriva Tena
        /// <summary>
        /// Tipo Crédito Cooperatriva Tena
        /// </summary>
        TipoCreditoCoopTena = 40,
        /// <summary>
        /// Plazo Cooperatriva Tena
        /// </summary>
        PlazoCoopTena = 41,
        /// <summary>
        /// Gasto Hogar Cooperatriva Tena
        /// </summary>
        GastoHogarCoopTena = 42,
        /// <summary>
        /// Monto Cooperatriva Tena
        /// </summary>
        MontoCoopTena = 43,
        /// <summary>
        /// Ingreso Cooperatriva Tena
        /// </summary>
        IngresoCoopTena = 44,
        /// <summary>
        /// Resta Gasto Financiero Cooperatriva Tena
        /// </summary>
        RestaGastoFinancieroCoopTena = 45,
        /// <summary>
        /// Tipo Documento Cooperatriva Tena
        /// </summary>
        TipoDocumentoCoopTena = 46,
        /// <summary>
        /// Numero Documento Cooperatriva Tena
        /// </summary>
        NumeroDocumentoCoopTena = 47,
        #endregion Cooperatriva Tena

        #region Banco Litoral Microfinanza
        /// <summary>
        /// Tipo Crédito Banco Litoral Microfinanza
        /// </summary>
        TipoCreditoBLitoralMicrofinanza = 50,
        /// <summary>
        /// Plazo Banco Litoral Microfinanza
        /// </summary>
        PlazoBLitoralMicrofinanza = 51,
        /// <summary>
        /// Monto Banco Litoral Microfinanza
        /// </summary>
        MontoBLitoralMicrofinanza = 52,
        /// <summary>
        /// Ingreso Banco Litoral Microfinanza
        /// </summary>
        IngresoBLitoralMicrofinanza = 53,
        /// <summary>
        /// Gasto Hogar Banco Litoral Microfinanza
        /// </summary>
        GastoHogarBLitoralMicrofinanza = 54,
        /// <summary>
        /// Resta Gasto Financiero Banco Litoral Microfinanza
        /// </summary>
        RestaGastoFinancieroBLitoralMicrofinanza = 55,
        /// <summary>
        /// Tipo Documento Banco Litoral Microfinanza
        /// </summary>
        TipoDocumentoBLitoralMicrofinanza = 56,
        /// <summary>
        /// Numero Documento Banco Litoral Microfinanza
        /// </summary>
        NumeroDocumentoBLitoralMicrofinanza = 57,
        #endregion Banco Litoral Microfinanza

        #region Banco DMiro
        /// <summary>
        /// Identificacion Sujeto Banco DMiro
        /// </summary>
        IdentificacionSujetoBancoDMiro = 58,
        /// <summary>
        /// Identificacion Conyuge Banco DMiro
        /// </summary>
        IdentificacionConyugeBancoDMiro = 59,
        /// <summary>
        /// Estado Civil Banco DMiro
        /// </summary>
        EstadoCivilBancoDMiro = 60,
        /// <summary>
        /// Antiguedad Laboral Banco DMiro
        /// </summary>
        AntiguedadLaboralBancoDMiro = 61,
        /// <summary>
        /// Instruccion Banco DMiro
        /// </summary>
        InstruccionBancoDMiro = 62,
        /// <summary>
        /// Tipo Prestamo Banco DMiro
        /// </summary>
        TipoPrestamoBancoDMiro = 63,
        /// <summary>
        /// Monto Solicitado Banco DMiro
        /// </summary>
        MontoSolicitadoBancoDMiro = 64,
        /// <summary>
        /// Plazo Banco DMiro
        /// </summary>
        PlazoBancoDMiro = 65,
        /// <summary>
        /// Ingreso Banco DMiro
        /// </summary>
        IngresoBancoDMiro = 66,
        /// <summary>
        /// Gastos Personales Banco DMiro
        /// </summary>
        GastosPersonalesBancoDMiro = 67,
        /// <summary>
        /// Identificacion Banco DMiro
        /// </summary>
        IdentificacionBancoDMiro = 68,
        /// <summary>
        /// Tipo Modelo Banco DMiro
        /// </summary>
        TipoModeloBancoDMiro = 69,

        #endregion Banco DMiro

        ///<summary>
        ///Empresa = 0190150496001
        ///</summary>
        ProveedorInternacional = 30
    }
}
