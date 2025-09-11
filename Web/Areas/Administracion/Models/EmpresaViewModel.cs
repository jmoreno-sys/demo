// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Dominio.Tipos;

namespace Web.Areas.Administracion.Models
{
    public class EmpresaViewModel
    {
        public EmpresaViewModel()
        {
            PlanEmpresa = new PlanEmpresaViewModel();
        }

        public int Id { get; set; }
        public int? IdAsesorComercialConfiable { get; set; }
        public string Identificacion { get; set; }
        public string RazonSocial { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public string PersonaContacto { get; set; }
        public string TelefonoPersonaContacto { get; set; }
        public string CorreoPersonaContacto { get; set; }
        public string BaseImagen { get; set; }
        public EstadosEmpresas Estado { get; set; }
        public DateTime? FechaCobroRecurrente { get; set; }
        public string Observaciones { get; set; }
        public bool HabilitarPlanBuroCredito { get; set; }
        public bool HabilitarPlanEvaluacion { get; set; }
        public bool PlanUnificado { get; set; }
        public bool PlanDemostracion { get; set; }
        public PlanEmpresaViewModel PlanEmpresa { get; set; }
        public PlanBuroCreditoViewModel PlanBuroCredito { get; set; }
        public PlanEvaluacionViewModel PlanEvaluacion { get; set; }
    }

    public class PlanEmpresaViewModel
    {
        public int Id { get; set; }
        public string NombrePlan { get; set; }
        //Separada
        public int NumeroConsultasRuc { get; set; }
        public int NumeroConsultasCedula { get; set; }
        public decimal ValorPlanAnualRuc { get; set; }
        public decimal ValorPlanAnualCedula { get; set; }
        public decimal ValorPlanMensualRuc { get; set; }
        public decimal ValorPlanMensualCedula { get; set; }
        public decimal ValorPorConsultaRucs { get; set; }
        public decimal ValorPorConsultaCedulas { get; set; }
        public decimal ValorConsultaAdicionalRucs { get; set; }
        public decimal ValorConsultaAdicionalCedulas { get; set; }
        //Unificado
        public int? NumeroConsultas { get; set; }
        public decimal? ValorPlanAnual { get; set; }
        public decimal? ValorPlanMensual { get; set; }
        public decimal? ValorPorConsulta { get; set; }
        public decimal? ValorConsultaAdicional { get; set; }
        public bool BloquearConsultas { get; set; }
        public bool MaximoNumeroConsultas { get; set; }
    }

    public class PlanBuroCreditoViewModel
    {
        public int Id { get; set; }
        public int PersistenciaCache { get; set; }
        public int NumeroMaximoConsultas { get; set; }
        public int? NumeroMaximoConsultasCompartidas { get; set; }
        public decimal ValorPlanBuroCredito { get; set; }
        public decimal ValorConsulta { get; set; }
        public decimal? ValorConsultaAdicional { get; set; }
        public FuentesBuro Fuente { get; set; }
        public bool BloquearConsultas { get; set; }
        public bool ConsultasCompartidas { get; set; }
        public bool ModeloCooperativas { get; set; }
    }

    public class PlanEvaluacionViewModel
    {
        public int Id { get; set; }
        public int NumeroConsultas { get; set; }
        public decimal ValorConsulta { get; set; }
        public decimal ValorConsultaAdicional { get; set; }
        public bool BloquearConsultas { get; set; }
        public bool MaximoNumeroConsultas { get; set; }
    }

    public class FuenteBuroViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
