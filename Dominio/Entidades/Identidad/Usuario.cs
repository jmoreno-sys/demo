using Dominio.Entidades.Balances;
using Dominio.Interfaces;
using Dominio.Tipos;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Dominio.Entidades.Identidad
{
    public class Usuario : IdentityUser<int>, IAuditable, IEntidad<int>
    {
        public Usuario()
        {
            Estado = EstadosUsuarios.Activo;
            ReclamosUsuario = new List<UsuarioReclamo>();
            AccesosUsuario = new List<UsuarioAcceso>();
            UsuariosRoles = new List<UsuarioRol>();
            TokensUsuario = new List<UsuarioToken>();
            Historial = new List<Historial>();
            Accesos = new List<AccesoUsuario>();
            Empresas = new List<Empresa>();
            EmpresasAsesores = new List<Empresa>();
        }

        public virtual Empresa Empresa { get; set; }
        public int IdEmpresa { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public string Direccion { get; set; }
        public string TelefonoMovil { get; set; }
        public EstadosUsuarios Estado { get; set; }

        #region Relaciones
        public IEnumerable<UsuarioReclamo> ReclamosUsuario { get; set; }
        public IEnumerable<UsuarioAcceso> AccesosUsuario { get; set; }
        public IEnumerable<UsuarioRol> UsuariosRoles { get; set; }
        public IEnumerable<UsuarioToken> TokensUsuario { get; set; }
        public IEnumerable<Historial> Historial { get; set; }
        public IEnumerable<AccesoUsuario> Accesos { get; set; }
        public IEnumerable<Empresa> Empresas { get; set; }
        public IEnumerable<Empresa> EmpresasAsesores { get; set; }
        #endregion Relaciones

        #region IAuditable
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioModificacion { get; set; }
        #endregion IAuditable
    }
}
