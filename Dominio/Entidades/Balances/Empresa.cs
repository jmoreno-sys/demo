using System;
using System.Collections.Generic;
using Dominio.Abstracciones;
using Dominio.Entidades.Identidad;
using Dominio.Interfaces;
using Dominio.Tipos;

namespace Dominio.Entidades.Balances
{
    public class Empresa : Entidad, IAuditable
    {
        public Empresa()
        {
            Guid = Guid.NewGuid();
            Estado = EstadosEmpresas.Activo;
            Usuarios = new List<Usuario>();
            PlanesEmpresas = new List<PlanEmpresa>();
            PlanesEvaluaciones = new List<PlanEvaluacion>();
            Politicas = new List<Politica>();
            PlanesBuroCredito = new List<PlanBuroCredito>();
            CredencialesBuro = new List<CredencialBuro>();
        }

        public string Identificacion { get; set; }
        public Guid Guid { get; set; }
        public string RazonSocial { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public string PersonaContacto { get; set; }
        public string TelefonoPersonaContacto { get; set; }
        public string CorreoPersonaContacto { get; set; }
        public string RutaLogo { get; set; }
        public EstadosEmpresas Estado { get; set; }
        public DateTime? FechaCobroRecurrente { get; set; }
        public string Observaciones { get; set; }
        public string AsesorComercial { get; set; }
        public virtual Usuario UsuarioRegistro { get; set; }
        public int? IdUsuarioRegistro { get; set; }
        public virtual Usuario AsesorComercialConfiable { get; set; }
        public int? IdAsesorComercialConfiable { get; set; }
        public bool VistaPersonalizada { get; set; }
        public string RutaContrato { get; set; }
        public string DireccionIp { get; set; }

        #region Relaciones
        public IEnumerable<Usuario> Usuarios { get; set; }
        public IEnumerable<PlanEmpresa> PlanesEmpresas { get; set; }
        public IEnumerable<PlanBuroCredito> PlanesBuroCredito { get; set; }
        public IEnumerable<Politica> Politicas { get; set; }
        public IEnumerable<PlanEvaluacion> PlanesEvaluaciones { get; set; }
        public IEnumerable<CredencialBuro> CredencialesBuro { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
