using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistencia.Configuraciones
{
    public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
    {
        public void Configure(EntityTypeBuilder<Empresa> e)
        {
            e.ToTable("Empresas", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.Identificacion).HasMaxLength(Longitudes.Identificacion).IsRequired();
            e.Property(m => m.RazonSocial).HasMaxLength(Longitudes.Descripcion).IsRequired();
            e.Property(m => m.Telefono).HasMaxLength(Longitudes.Telefono);
            e.Property(m => m.Correo).HasMaxLength(Longitudes.Correo);
            e.Property(m => m.Direccion).HasMaxLength(Longitudes.Direccion);
            e.Property(m => m.PersonaContacto).HasMaxLength(Longitudes.NombreCompleto);
            e.Property(m => m.TelefonoPersonaContacto).HasMaxLength(Longitudes.Telefono);
            e.Property(m => m.CorreoPersonaContacto).HasMaxLength(Longitudes.Correo);
            e.Property(m => m.RutaLogo).HasMaxLength(Longitudes.RutaArchivo);
            e.Property(m => m.Observaciones).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.AsesorComercial).HasMaxLength(Longitudes.NombreCompleto);
            e.Property(m => m.RutaContrato).HasMaxLength(Longitudes.RutaArchivo);
            e.Property(m => m.DireccionIp).HasMaxLength(Longitudes.DescripcionCorta);

            e.HasMany(m => m.Politicas).WithOne(m => m.Empresa).HasForeignKey(m => m.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.CredencialesBuro).WithOne(m => m.Empresa).HasForeignKey(m => m.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.UsuarioRegistro).WithMany(m => m.Empresas).HasForeignKey(m => m.IdUsuarioRegistro).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.AsesorComercialConfiable).WithMany(m => m.EmpresasAsesores).HasForeignKey(m => m.IdAsesorComercialConfiable).OnDelete(DeleteBehavior.Restrict);

            e.HasData(new[] {
                new Empresa() {
                    Id = 1,
                    Identificacion = "1792491428001",
                    RazonSocial = "INNOVACION DE INFORMACION GARANCHECK CIA. LTDA.",
                    Correo = "DESARROLLO@GARANCHECK.COM.EC",
                    Direccion = "POMASQUI/MANUELA SAENZ 11 Y PASAJE OE 1C",
                    PersonaContacto = "FELIPE CACERES",
                    TelefonoPersonaContacto = "0123456789",
                    CorreoPersonaContacto = "DESARROLLO@GARANCHECK.COM.EC",
                    RutaLogo = "1792491428001.png",
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosEmpresas.Activo,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033),
                    FechaCobroRecurrente = new DateTime(2022, 6, 1, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033),
                    Guid = Guid.Parse("a19ef7ae-be7e-49ab-bfae-1d54f1820ac8")
                }
            });
        }
    }

    public sealed class HistorialConfiguration : IEntityTypeConfiguration<Historial>
    {
        public void Configure(EntityTypeBuilder<Historial> e)
        {
            e.ToTable("Historiales", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.DireccionIp).HasMaxLength(Longitudes.DescripcionCorta);
            e.Property(m => m.Identificacion).HasMaxLength(Longitudes.Identificacion);
            e.Property(m => m.TipoIdentificacion).HasMaxLength(Longitudes.CodigoCorto);
            e.Property(m => m.Observacion).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.ParametrosBusqueda).HasMaxLength(Longitudes.DescripcionLarga);
            e.Property(m => m.RazonSocialEmpresa).HasMaxLength(Longitudes.Contenido);
            e.Property(m => m.NombresPersona).HasMaxLength(Longitudes.DescripcionLarga);
            e.Property(m => m.IdentificacionSecundaria).HasMaxLength(Longitudes.Identificacion);
            e.Property(m => m.FechaExpedicionCedula).HasMaxLength(Longitudes.Identificacion);

            e.HasMany(m => m.DetalleHistorial).WithOne(m => m.Historial).HasForeignKey(m => m.IdHistorial).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.Calificaciones).WithOne(m => m.Historial).HasForeignKey(m => m.IdHistorial).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.ParametrosClientesHistorial).WithOne(m => m.Historial).HasForeignKey(m => m.IdHistorial).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public sealed class DetalleHistorialConfiguration : IEntityTypeConfiguration<DetalleHistorial>
    {
        public void Configure(EntityTypeBuilder<DetalleHistorial> e)
        {
            e.ToTable("DetallesHistorial", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.Observacion).HasMaxLength(Longitudes.Observacion);
        }
    }

    public sealed class PlanIdentificacionConfiguration : IEntityTypeConfiguration<PlanIdentificacion>
    {
        public void Configure(EntityTypeBuilder<PlanIdentificacion> e)
        {
            e.ToTable("PlanesIdentificacion", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.NombrePlan).HasMaxLength(Longitudes.NombreCompleto).IsRequired();
            e.Property(m => m.Descripcion).HasMaxLength(Longitudes.Descripcion);

            e.HasMany(m => m.Planes).WithOne(m => m.PlanIdentificacion).HasForeignKey(m => m.IdPlanIdentificacion).OnDelete(DeleteBehavior.Restrict);

            e.HasData(new[] {
                new PlanIdentificacion() {
                    Id = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "RUCs Naturales y Jurídicos",
                    ValorPlanAnual = 499,
                    Tipo = Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanIdentificacion() {
                    Id = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Cédulas",
                    ValorPlanAnual = 299,
                    Tipo = Dominio.Tipos.Planes.Cedulas,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                }
            });
        }
    }

    public sealed class PlanConsultaConfiguration : IEntityTypeConfiguration<PlanConsulta>
    {
        public void Configure(EntityTypeBuilder<PlanConsulta> e)
        {
            e.ToTable("PlanesConsulta", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.NombrePlan).HasMaxLength(Longitudes.NombreCompleto).IsRequired();
            e.Property(m => m.Descripcion).HasMaxLength(Longitudes.Descripcion);

            e.HasData(new[] {
                //Planes RUCs Naturales y Juridicos
                new PlanConsulta() {
                    Id = 1,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "Plan 25",
                    ValorPlanMensual = 60m,
                    ValorPorConsultaRucs = 1.6m,
                    ValorPorConsultaCedulas = 0.8m,
                    ValorConsultaAdicionalRucs = 2m,
                    ValorConsultaAdicionalCedulas = 1m,
                    NumeroConsultas = 25,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 2,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "Plan 50",
                    ValorPlanMensual = 105m,
                    ValorPorConsultaRucs = 1.4m,
                    ValorPorConsultaCedulas = 0.7m,
                    ValorConsultaAdicionalRucs = 2m,
                    ValorConsultaAdicionalCedulas = 1m,
                    NumeroConsultas = 50,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 3,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "Plan 75",
                    ValorPlanMensual = 142.5m,
                    ValorPorConsultaRucs = 1.3m,
                    ValorPorConsultaCedulas = 0.6m,
                    ValorConsultaAdicionalRucs = 2m,
                    ValorConsultaAdicionalCedulas = 1m,
                    NumeroConsultas = 75,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 4,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "Plan 100",
                    ValorPlanMensual = 170m,
                    ValorPorConsultaRucs = 1.2m,
                    ValorPorConsultaCedulas = 0.5m,
                    ValorConsultaAdicionalRucs = 2m,
                    ValorConsultaAdicionalCedulas = 1m,
                    NumeroConsultas = 100,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 5,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "Plan 200",
                    ValorPlanMensual = 300m,
                    ValorPorConsultaRucs = 1.1m,
                    ValorPorConsultaCedulas = 0.4m,
                    ValorConsultaAdicionalRucs = 2m,
                    ValorConsultaAdicionalCedulas = 1m,
                    NumeroConsultas = 200,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 6,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.RucsNaturalesJuridicos,
                    NombrePlan = "Plan > 300",
                    ValorPlanMensual = 300m,
                    ValorPorConsultaRucs = 0.90m,
                    ValorPorConsultaCedulas = 0.3m,
                    ValorConsultaAdicionalRucs = 0.90m,
                    ValorConsultaAdicionalCedulas = 0.3m,
                    NumeroConsultas = int.MaxValue,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                //Planes Cédulas
                new PlanConsulta() {
                    Id = 7,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Plan 25",
                    ValorPlanMensual = 20m,
                    ValorPorConsultaCedulas = 0.8m,
                    ValorPorConsultaRucs = 0m,
                    ValorConsultaAdicionalCedulas = 1m,
                    ValorConsultaAdicionalRucs = 0m,
                    NumeroConsultas = 25,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 8,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Plan 50",
                    ValorPlanMensual = 35m,
                    ValorPorConsultaCedulas = 0.7m,
                    ValorPorConsultaRucs = 0m,
                    ValorConsultaAdicionalCedulas = 1m,
                    ValorConsultaAdicionalRucs = 0m,
                    NumeroConsultas = 50,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 9,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Plan 75",
                    ValorPlanMensual = 45m,
                    ValorPorConsultaCedulas = 0.6m,
                    ValorPorConsultaRucs = 0m,
                    ValorConsultaAdicionalCedulas = 1m,
                    ValorConsultaAdicionalRucs = 0m,
                    NumeroConsultas = 75,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 10,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Plan 100",
                    ValorPlanMensual = 50m,
                    ValorPorConsultaCedulas = 0.5m,
                    ValorPorConsultaRucs = 0m,
                    ValorConsultaAdicionalCedulas = 1m,
                    ValorConsultaAdicionalRucs = 0m,
                    NumeroConsultas = 100,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 11,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Plan 200",
                    ValorPlanMensual = 80m,
                    ValorPorConsultaCedulas = 0.4m,
                    ValorPorConsultaRucs = 0m,
                    ValorConsultaAdicionalCedulas = 1m,
                    ValorConsultaAdicionalRucs = 0m,
                    NumeroConsultas = 200,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new PlanConsulta() {
                    Id = 12,
                    IdPlanIdentificacion = (short)Dominio.Tipos.Planes.Cedulas,
                    NombrePlan = "Plan > 300",
                    ValorPlanMensual = 90m,
                    ValorPorConsultaCedulas = 0.3m,
                    ValorPorConsultaRucs = 0m,
                    ValorConsultaAdicionalCedulas = 0m,
                    ValorConsultaAdicionalRucs = 0m,
                    NumeroConsultas = int.MaxValue,
                    UsuarioCreacion = 1,
                    Estado = Dominio.Tipos.EstadosPlanes.Activo,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                }
            });
        }
    }

    public sealed class PlanEmpresaConfiguration : IEntityTypeConfiguration<PlanEmpresa>
    {
        public void Configure(EntityTypeBuilder<PlanEmpresa> e)
        {
            e.ToTable("PlanesEmpresas", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.HasOne(m => m.Empresa).WithMany(m => m.PlanesEmpresas).HasForeignKey(m => m.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.Historial).WithOne(m => m.PlanEmpresa).HasForeignKey(m => m.IdPlanEmpresa).OnDelete(DeleteBehavior.Restrict);

            e.HasData(new[] {
                new PlanEmpresa() {
                    Id = 1,
                    IdEmpresa = 1,
                    NombrePlan = "Plan > 300",
                    FechaInicioPlan = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033),
                    FechaFinPlan = new DateTime(2023, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033),
                    ValorPlanAnualRuc = 499m,
                    ValorPlanAnualCedula = 499m,
                    ValorPlanMensualRuc = 300m,
                    ValorPlanMensualCedula = 300m,
                    ValorPorConsultaRucs = 0.9m,
                    ValorPorConsultaCedulas = 0.3m,
                    ValorConsultaAdicionalRucs = 0.9m,
                    ValorConsultaAdicionalCedulas = 0.3m,
                    NumeroConsultasRuc = int.MaxValue,
                    NumeroConsultasCedula = int.MaxValue,
                    Estado = Dominio.Tipos.EstadosPlanesEmpresas.Activo,
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 5, 16, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                }
            });
        }
    }

    public sealed class PlanBuroCreditoConfiguration : IEntityTypeConfiguration<PlanBuroCredito>
    {
        public void Configure(EntityTypeBuilder<PlanBuroCredito> e)
        {
            e.ToTable("PlanesBuroCredito", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.Observaciones).HasMaxLength(Longitudes.Observacion);

            e.HasOne(m => m.Empresa).WithMany(m => m.PlanesBuroCredito).HasForeignKey(m => m.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.Historial).WithOne(m => m.PlanBuroCredito).HasForeignKey(m => m.IdPlanBuroCredito).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class PoliticaConfiguration : IEntityTypeConfiguration<Politica>
    {
        public void Configure(EntityTypeBuilder<Politica> e)
        {
            e.ToTable("Politicas", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.Nombre).HasMaxLength(Longitudes.NombreLargo).IsRequired();
            e.Property(m => m.Descripcion).HasMaxLength(Longitudes.Descripcion);
            e.Property(m => m.CalificacionMinima).HasMaxLength(Longitudes.CodigoCorto);
            e.Property(m => m.Operador).HasMaxLength(Longitudes.CodigoCorto);
            e.Property(m => m.Variable1).HasMaxLength(Longitudes.CodigoMedio);
            e.Property(m => m.Variable2).HasMaxLength(Longitudes.CodigoMedio);

            e.HasMany(m => m.DetalleCalificaciones).WithOne(m => m.Politica).HasForeignKey(m => m.IdPolitica).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class CalificacionConfiguration : IEntityTypeConfiguration<Calificacion>
    {
        public void Configure(EntityTypeBuilder<Calificacion> e)
        {
            e.ToTable("Calificaciones", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.Observaciones).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.RangoIngreso).HasMaxLength(Longitudes.DescripcionCorta);

            e.HasMany(m => m.DetalleCalificacion).WithOne(m => m.Calificacion).HasForeignKey(m => m.IdCalificacion).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class DetalleCalificacionConfiguration : IEntityTypeConfiguration<DetalleCalificacion>
    {
        public void Configure(EntityTypeBuilder<DetalleCalificacion> e)
        {
            e.ToTable("DetalleCalificaciones", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.Valor).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.Parametro).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.Datos).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.ReferenciaMinima).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.Observacion).HasMaxLength(Longitudes.Observacion);
            e.Property(m => m.Instituciones).HasMaxLength(Longitudes.ContenidoData);

        }
    }

    public sealed class PlanEvaluacionConfiguration : IEntityTypeConfiguration<PlanEvaluacion>
    {
        public void Configure(EntityTypeBuilder<PlanEvaluacion> e)
        {
            e.ToTable("PlanesEvaluaciones", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.HasOne(m => m.Empresa).WithMany(m => m.PlanesEvaluaciones).HasForeignKey(m => m.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.Historial).WithOne(m => m.PlanEvaluacion).HasForeignKey(m => m.IdPlanEvaluacion).OnDelete(DeleteBehavior.Restrict);

        }
    }

    public sealed class AccesosConfiguration : IEntityTypeConfiguration<AccesoUsuario>
    {
        public void Configure(EntityTypeBuilder<AccesoUsuario> e)
        {
            e.ToTable("Accesos", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
        }
    }

    public sealed class CredencialesBuroConfiguration : IEntityTypeConfiguration<CredencialBuro>
    {
        public void Configure(EntityTypeBuilder<CredencialBuro> e)
        {
            e.ToTable("CredencialesBuro", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.Usuario).HasMaxLength(Longitudes.NombreCompleto);
            e.Property(m => m.Clave).HasMaxLength(Longitudes.NombreCompleto);
            e.Property(m => m.Enlace).HasMaxLength(Longitudes.NombreLargo);
            e.Property(m => m.TokenAcceso).HasMaxLength(Longitudes.ContenidoData);
            e.Property(m => m.ProductData).HasMaxLength(Longitudes.ContenidoData);
        }
    }

    public sealed class ParametrosClientesHistorialConfiguration : IEntityTypeConfiguration<ParametroClienteHistorial>
    {
        public void Configure(EntityTypeBuilder<ParametroClienteHistorial> e)
        {
            e.ToTable("ParametrosClientesHistoriales", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.Valor).HasMaxLength(Longitudes.Observacion);
        }
    }

    public sealed class ReporteConsolidadoConfiguration : IEntityTypeConfiguration<ReporteConsolidado>
    {
        public void Configure(EntityTypeBuilder<ReporteConsolidado> e)
        {
            e.ToTable("ReporteConsolidado", Esquemas.Principal);
            e.Property(m => m.Id).ValueGeneratedOnAdd();

            e.Property(m => m.DireccionIp).HasMaxLength(Longitudes.DescripcionCorta);
            e.Property(m => m.Identificacion).HasMaxLength(Longitudes.Identificacion);
            e.Property(m => m.IdentificacionEmpresa).HasMaxLength(Longitudes.Identificacion);
            e.Property(m => m.TipoIdentificacion).HasMaxLength(Longitudes.CodigoCorto);
            e.Property(m => m.RazonSocial).HasMaxLength(Longitudes.Contenido);
            e.Property(m => m.NombreEmpresa).HasMaxLength(Longitudes.Contenido);
        }
    }
}
