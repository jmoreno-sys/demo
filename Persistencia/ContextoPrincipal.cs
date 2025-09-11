using Dominio.Entidades;
using Dominio.Entidades.Balances;
using Dominio.Entidades.Identidad;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Persistencia
{
    public class ContextoPrincipal : IdentityDbContext<Usuario, Rol, int, UsuarioReclamo, UsuarioRol, UsuarioAcceso, RolReclamo, UsuarioToken>
    {
        public ContextoPrincipal(DbContextOptions<ContextoPrincipal> options) : base(options)
        {
        }

        internal static string ProviderName { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            ProviderName = Database.ProviderName.Split('.').Last();

            // Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoPrincipal).Assembly);

            // Conventions
            modelBuilder.AddProviderTypeConventions(ProviderName, new[] { "Identity" });
            modelBuilder.AddAuditableEntitiesConventions(ProviderName);
        }

#if DEBUG

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
        }

#endif

        #region DbSet
        //Identidad
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> RolesUsuario { get; set; }
        public DbSet<UsuarioRol> UsuariosRoles { get; set; }
        public DbSet<UsuarioReclamo> Reclamos { get; set; }
        public DbSet<RolReclamo> ReclamosRoles { get; set; }

        //Balances
        public DbSet<AccesoUsuario> AccesosUsuarios { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }
        public DbSet<DetalleCalificacion> DetallesCalificaciones { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Historial> Historiales { get; set; }
        public DbSet<PlanBuroCredito> PlanesBurosCreditos { get; set; }
        public DbSet<PlanConsulta> PlanesConsultas { get; set; }
        public DbSet<PlanEmpresa> PlanesEmpresas { get; set; }
        public DbSet<PlanEvaluacion> PlanesEvaluaciones { get; set; }
        public DbSet<PlanIdentificacion> PlanesIdentificaciones { get; set; }
        public DbSet<Politica> Politicas { get; set; }

        #endregion DbSet
    }
}

