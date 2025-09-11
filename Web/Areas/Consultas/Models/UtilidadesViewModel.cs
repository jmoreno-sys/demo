// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Web.Areas.Consultas.Models
{
    public class EmpresaPersonalizadaViewModel
    {
        public int Id { get; set; }
        public string Identificacion { get; set; }
        public string RazonSocial { get; set; }
    }

    public class ParametroFuentesInternasViewModel
    {
        public FuentesInternasViewModel FuentesInternas { get; set; }
    }

    public class FuentesInternasViewModel
    {
        public int Alimentos { get; set; }
        public int RegistroCivil { get; set; }
        public int Sri { get; set; }
        public int Iess { get; set; }
        public int FJ { get; set; }
        public int Sercop { get; set; }
    }

    public class SalarioViewModel
    {
        public decimal SalarioBasico { get; set; }
    }

    public class EmpleadoBAmazonasViewModel
    {
        public string Identificacion { get; set; }
    }
}
