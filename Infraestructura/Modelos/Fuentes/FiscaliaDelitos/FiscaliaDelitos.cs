// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Infraestructura.Interfaces;

namespace Infraestructura.Modelos.Fuentes.FiscaliaDelitos
{
    public class NoticiaDelito : IModelo
    {

        public NoticiaDelito()
        {
            ProcesosNoticiaDelito = new List<ProcesoNoticiaDelito>();
            ProcesosActoAdministrativo = new List<ProcesoActoAdministrativo>();
        }

        public List<ProcesoNoticiaDelito> ProcesosNoticiaDelito { get; set; }
        public List<ProcesoActoAdministrativo> ProcesosActoAdministrativo { get; set; }
        public string ErrorNoticiasDelito { get; set; }
        public string ErrorActosAdministrativos { get; set; }
        public string ErrorRespuestaNoticiasDelito { get; set; }
        public string ErrorRespuestaActosAdministrativos { get; set; }

        #region Cache & Almacén

        [XmlIgnore, JsonIgnore]
        public bool Cache { get; set; }

        [XmlIgnore, JsonIgnore]
        public bool Almacen { get; set; }
        #endregion
    }

    public class ProcesoNoticiaDelito
    {
        public ProcesoNoticiaDelito()
        {
            Fecha = DateTime.Now;
            Sujetos = new List<Sujeto>();
            Vehiculos = new List<Vehiculo>();
        }

        public string Codigo { get; set; }
        public string Numero { get; set; }
        public string Tipo { get; set; }
        public string Lugar { get; set; }
        public TimeSpan Hora { get; set; }
        public DateTime Fecha { get; set; }
        public string Digitador { get; set; }
        public string Estado { get; set; }
        public string NumeroOficio { get; set; }
        public string Delito { get; set; }
        public string Unidad { get; set; }
        public string Fiscalia { get; set; }
        public List<Sujeto> Sujetos { get; set; }
        public List<Vehiculo> Vehiculos { get; set; }
    }

    public class Sujeto
    {
        public string Cedula { get; set; }
        public string NombresCompletos { get; set; }
        public string Estado { get; set; }
    }

    public class Vehiculo
    {
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Placa { get; set; }
    }

    public class ProcesoActoAdministrativo
    {
        public ProcesoActoAdministrativo()
        {
            Fecha = DateTime.Now;
        }

        public string Codigo { get; set; }
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Asesor { get; set; }
        public string CedulaDenunciante { get; set; }
        public string NombreDenunciante { get; set; }
        public string Descripcion { get; set; }
        public string Observaciones { get; set; }
        public string Fiscalia { get; set; }
    }
}
