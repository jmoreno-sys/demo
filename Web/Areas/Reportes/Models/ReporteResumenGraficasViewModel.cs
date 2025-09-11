// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Web.Areas.Historiales.Models
{
    public class ReporteResumenGraficasViewModel
    {
        public string Name { get; set; }
        public double Y { get; set; }
    }

    public class ReporteScoreViewModel
    {
        public string Calificacion { get; set; }
        public int Cantidad { get; set; }
    }
}
