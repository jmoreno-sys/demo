// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.Excel;
using Externos.Logica.PredioMunicipio.Modelos;
using Newtonsoft.Json;

namespace Web.Areas.Historiales.Models
{
    public class HistorialFiltrosViewModel
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public bool? Meses { get; set; }
    }

    public class HistorialGeneralFiltrosViewModel
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public string Empresas { get; set; }
        public bool? Meses { get; set; }
        public bool? FiltroApagado { get; set; }
        public DataTableViewModel? FiltroDatatable { get; set; }
        [JsonProperty("columns")]
        public Column[] Columns { get; set; }
    }

    public class DatosJsonViewModel
    {
        public string Datos { get; set; }
        public bool DatosCache { get; set; }
    }
    public class DataTableViewModel
    {
        [JsonProperty("draw")]
        public int Draw { get; set; }

        [JsonProperty("columns")]
        public Column[] Columns { get; set; }

        [JsonProperty("order")]
        public Order[] Order { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; } = 50;
        [JsonProperty("search")]
        public Search Search { get; set; }
    }
    public class Column
    {
        public Column()
        {
            Search = new Search();
        }
        
        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("searchable")]
        public bool Searchable { get; set; }

        [JsonProperty("orderable")]
        public bool Orderable { get; set; }

        [JsonProperty("search")]
        public Search Search { get; set; }
    }
    public class Order
    {
        [JsonProperty("column")]
        public long Column { get; set; }

        [JsonProperty("dir")]
        public string Dir { get; set; }
    }
    public class Search
    {
        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;

        [JsonProperty("regex")]
        public bool Regex { get; set; } = false;
    }
    public class DatosColumnasQuery
    {
        public string Data { get; set; }
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

}
