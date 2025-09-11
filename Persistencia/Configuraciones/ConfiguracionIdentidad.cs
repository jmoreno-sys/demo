using Dominio.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dominio.Entidades.Identidad;
using Persistencia;
using System;
using System.Security.Claims;


namespace Persistencia.Configuraciones
{
    public sealed class IdentityRoleConfiguration : IEntityTypeConfiguration<Rol>
    {
        public void Configure(EntityTypeBuilder<Rol> e)
        {
            e.ToTable("Roles", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.Name).HasMaxLength(Longitudes.Key).HasColumnName("Nombre");
            e.Property(m => m.NormalizedName).HasMaxLength(Longitudes.Key).HasColumnName("NombreNormalizado");
            e.Property(m => m.ConcurrencyStamp).HasMaxLength(Longitudes.Key).HasColumnName("HashConcurrencia");
            e.Property(m => m.Descripcion).HasMaxLength(Longitudes.Descripcion);

            e.HasIndex(m => m.NormalizedName).IsUnique().HasDatabaseName("RoleNameIndex").HasFilter("[NombreNormalizado] IS NOT NULL");

            #region Seed Data
            e.HasData(new[] {
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.Administrador,
                    ConcurrencyStamp = "c320f649-0757-4f16-8b46-9f92f183f53f",
                    Name = "ADMINISTRADOR GARANCHECK",
                    NormalizedName = "ADMINISTRADOR GARANCHECK",
                    Descripcion = "ADMINISTRADOR GARANCHECK",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.Operador,
                    ConcurrencyStamp = "1dc98c14-d17d-470a-ba35-f1cdbe3ada14",
                    Name = "OPERADOR GARANCHECK",
                    NormalizedName = "OPERADOR GARANCHECK",
                    Descripcion = "OPERADOR GARANCHECK",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.AdministradorEmpresa,
                    ConcurrencyStamp = "827bdbee-1dcf-4516-9750-a6266b272f87",
                    Name = "ADMINISTRADOR CLIENTE",
                    NormalizedName = "ADMINISTRADOR CLIENTE",
                    Descripcion = "ADMINISTRADOR CLIENTE",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.OperadorEmpresa,
                    ConcurrencyStamp = "6d75be1d-4212-4f6b-b424-e6c1e40733dd",
                    Name = "OPERADOR CLIENTE",
                    NormalizedName = "OPERADOR CLIENTE",
                    Descripcion = "OPERADOR CLIENTE",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.VendedorEmpresa,
                    ConcurrencyStamp = "52c20176-3079-47d3-b4b4-7a137ceddaa4",
                    Name = "VENDEDOR CLIENTE",
                    NormalizedName = "VENDEDOR CLIENTE",
                    Descripcion = "VENDEDOR CLIENTE",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2023, 9, 13, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.ContactabilidadEmpresa,
                    ConcurrencyStamp = "5f0c590a-ffeb-4ddb-a74e-10aadc4ffbe7",
                    Name = "CONTACTABILIDAD CLIENTE",
                    NormalizedName = "CONTACTABILIDAD CLIENTE",
                    Descripcion = "CONTACTABILIDAD CLIENTE",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2024, 4, 8, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.AdministradorCooprogreso,
                    ConcurrencyStamp = "67b5c23a-feb2-4fdd-8bf9-e5b99dc53e34",
                    Name = "ADMINISTRADOR COOPROGRESO",
                    NormalizedName = "ADMINISTRADOR COOPROGRESO",
                    Descripcion = "ADMINISTRADOR COOPROGRESO",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2024, 4, 9, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Rol() {
                    Id = (short)Dominio.Tipos.Roles.Reporteria,
                    ConcurrencyStamp = "3778b622-1a94-471f-b8c1-de2ce7e97939",
                    Name = "REPORTERIA",
                    NormalizedName = "REPORTERIA",
                    Descripcion = "REPORTERIA",
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2025, 1, 24, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                }
            });
            #endregion Seed Data
        }

    }
    public sealed class IdentityUserConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> e)
        {
            e.ToTable("Usuarios", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.UserName).HasMaxLength(Longitudes.Key).HasColumnName("Usuario").IsRequired();
            e.Property(m => m.NormalizedUserName).HasMaxLength(Longitudes.Key).HasColumnName("UsuarioNormalizado");
            e.Property(m => m.Email).HasMaxLength(Longitudes.Correo).HasColumnName("Correo").IsRequired();
            e.Property(m => m.NormalizedEmail).HasMaxLength(Longitudes.Correo).HasColumnName("CorreoNormalizado");
            e.Property(m => m.EmailConfirmed).HasColumnName("CorreoConfirmado");
            e.Property(m => m.PhoneNumber).HasMaxLength(Longitudes.Telefono).HasColumnName("Telefono");
            e.Property(m => m.PasswordHash).HasMaxLength(Longitudes.Key).HasColumnName("Clave").IsRequired();

            e.Property(m => m.AccessFailedCount).HasColumnName("FallasInicio");
            e.Property(m => m.ConcurrencyStamp).HasMaxLength(Longitudes.Key).HasColumnName("HashConcurrencia");
            e.Property(m => m.LockoutEnabled).HasColumnName("BloqueoHabilitado");
            e.Property(m => m.LockoutEnd).HasColumnName("BloqueoFechaFin");
            e.Property(m => m.PhoneNumberConfirmed).HasColumnName("TelefonoConfirmado");
            e.Property(m => m.SecurityStamp).HasMaxLength(Longitudes.Key).HasColumnName("HashSeguridad");
            e.Property(m => m.TwoFactorEnabled).HasColumnName("A2FHabilitado");

            e.Property(m => m.NombreCompleto).HasMaxLength(Longitudes.NombreCompleto).IsRequired();
            e.Property(m => m.Identificacion).HasMaxLength(Longitudes.Identificacion).IsRequired();
            e.Property(m => m.TelefonoMovil).HasMaxLength(Longitudes.Telefono);
            e.Property(m => m.Direccion).HasMaxLength(Longitudes.Direccion);

            e.HasIndex(m => m.UserName).IsUnique();
            e.HasIndex(m => m.NormalizedEmail).HasDatabaseName("EmailIndex");
            e.HasIndex(m => m.NormalizedUserName).IsUnique().HasDatabaseName("UserNameIndex").HasFilter("[UsuarioNormalizado] IS NOT NULL");

            e.HasOne(m => m.Empresa).WithMany(m => m.Usuarios).HasForeignKey(m => m.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.Historial).WithOne(m => m.Usuario).HasForeignKey(m => m.IdUsuario).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(m => m.Accesos).WithOne(m => m.Usuario).HasForeignKey(m => m.IdUsuario).OnDelete(DeleteBehavior.Restrict);

            #region Seed Data
            e.HasData(new[] {
                //Usuario Mantenimiento
                new Usuario()
                {
                    Id = 1,
                    IdEmpresa = 1,
                    Identificacion = "ADMIN",
                    UserName = "ADMIN",
                    NormalizedUserName = "ADMIN",
                    NombreCompleto = "ADMINISTRADOR SISTEMA",
                    Email = "DESARROLLO@GARANCHECK.COM.EC",
                    NormalizedEmail = "DESARROLLO@GARANCHECK.COM.EC",
                    ConcurrencyStamp = "82c846cd-46b3-4f51-86d9-62121659c516",
                    SecurityStamp = "2fa6728d-f291-43be-944b-8f7385ebe115",
                    PasswordHash = "AQAAAAEAACcQAAAAEMx4RSIzDgHOTKYcLQWYPvUReSaNV139pnTYp2Eg9+hmAAy9Y8z1m8K126d+ybXpag==",
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                },
                new Usuario()
                {
                    Id = 2,
                    IdEmpresa = 1,
                    Identificacion = "1792491428001",
                    UserName = "1792491428001",
                    NormalizedUserName = "1792491428001",
                    NombreCompleto = "OPERADOR GARANCHECK GENERAL",
                    Email = "DESARROLLO@GARANCHECK.COM.EC",
                    NormalizedEmail = "DESARROLLO@GARANCHECK.COM.EC",
                    ConcurrencyStamp = "82c846cd-46b3-4f51-86d9-62121659c516",
                    SecurityStamp = "2fa6728d-f291-43be-944b-8f7385ebe115",
                    PasswordHash = "AQAAAAEAACcQAAAAEMD+Rgg+tUqgN4B9Z+VG3ezEsuBWPwM75c85/jBuBCfgaN/3mXC2sGnEsiUBWDAGSg==",
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    UsuarioCreacion = 1,
                    FechaCreacion = new DateTime(2022, 4, 27, 0, 0, 0, 0, DateTimeKind.Local).AddTicks(8033)
                }
            });

            #endregion Seed Data
        }
    }

    public sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<UsuarioReclamo>
    {
        public void Configure(EntityTypeBuilder<UsuarioReclamo> e)
        {
            e.ToTable("UsuariosReclamos", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.UserId).HasColumnName("IdUsuario");
            e.Property(m => m.ClaimType).HasMaxLength(Longitudes.Key).HasColumnName("Tipo");
            e.Property(m => m.ClaimValue).HasMaxLength(Longitudes.Key).HasColumnName("Valor");

            e.HasOne(m => m.Usuario).WithMany(m => m.ReclamosUsuario).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<UsuarioRol>
    {
        public void Configure(EntityTypeBuilder<UsuarioRol> e)
        {
            e.ToTable("UsuariosRoles", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.RoleId).HasColumnName("IdRol");
            e.Property(m => m.UserId).HasColumnName("IdUsuario");

            e.HasOne(m => m.Usuario).WithMany(m => m.UsuariosRoles).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Rol).WithMany(m => m.UsuariosRoles).HasForeignKey(m => m.RoleId).OnDelete(DeleteBehavior.Restrict);

            #region Seed Data
            e.HasData(new[] {
                new UsuarioRol() { Id = 1, RoleId = (short)Dominio.Tipos.Roles.Administrador, UserId = 1 },
                new UsuarioRol() { Id = 2, RoleId = (short)Dominio.Tipos.Roles.Operador, UserId = 2 },
            });
            #endregion Seed Data
        }
    }

    public sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<UsuarioAcceso>
    {
        public void Configure(EntityTypeBuilder<UsuarioAcceso> e)
        {
            e.ToTable("UsuariosAccesos", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.UserId).HasColumnName("IdUsuario");
            e.Property(m => m.LoginProvider).HasMaxLength(Longitudes.Key).HasColumnName("Proveedor");
            e.Property(m => m.ProviderDisplayName).HasMaxLength(Longitudes.Key).HasColumnName("ProveedorNombre");
            e.Property(m => m.ProviderKey).HasMaxLength(Longitudes.Key).HasColumnName("ProveedorClave");

            e.HasOne(m => m.Usuario).WithMany(m => m.AccesosUsuario).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<RolReclamo>
    {
        public void Configure(EntityTypeBuilder<RolReclamo> e)
        {
            e.ToTable("RolesReclamos", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.RoleId).HasColumnName("IdRol");
            e.Property(m => m.ClaimType).HasMaxLength(Longitudes.Key).HasColumnName("Tipo");
            e.Property(m => m.ClaimValue).HasMaxLength(Longitudes.Key).HasColumnName("Valor");

            e.HasOne(m => m.Rol).WithMany(m => m.ReclamosRol).HasForeignKey(m => m.RoleId).OnDelete(DeleteBehavior.Restrict);

            #region Seed Data
            e.HasData(new[] {

                //Administrador
                new RolReclamo() {
                    Id = 1,
                    RoleId = (short) Dominio.Tipos.Roles.Administrador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Administracion,
                },
                new RolReclamo() {
                    Id = 2,
                    RoleId = (short) Dominio.Tipos.Roles.Administrador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 3,
                    RoleId = (short) Dominio.Tipos.Roles.Administrador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialGeneral,
                },
                new RolReclamo() {
                    Id = 4,
                    RoleId = (short) Dominio.Tipos.Roles.Administrador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialEmpresa,
                },
                new RolReclamo() {
                    Id = 5,
                    RoleId = (short) Dominio.Tipos.Roles.Administrador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialUsuario,
                },
                new RolReclamo() {
                    Id = 12,
                    RoleId = (short) Dominio.Tipos.Roles.Administrador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.ReporteResumenConsultas,
                },

                //Operador Garancheck
                new RolReclamo() {
                    Id = 6,
                    RoleId = (short) Dominio.Tipos.Roles.Operador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 7,
                    RoleId = (short) Dominio.Tipos.Roles.Operador,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialGeneral,
                },
                
                //Administrador Empresa
                new RolReclamo() {
                    Id = 8,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 9,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialEmpresa,
                },
                new RolReclamo() {
                    Id = 15,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.ReporteResumenCliente,
                },

                //Operador Empresa
                new RolReclamo() {
                    Id = 10,
                    RoleId = (short) Dominio.Tipos.Roles.OperadorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 11,
                    RoleId = (short) Dominio.Tipos.Roles.OperadorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialUsuario,
                },

                //Vendedor Empresa
                new RolReclamo() {
                    Id = 13,
                    RoleId = (short) Dominio.Tipos.Roles.VendedorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 14,
                    RoleId = (short) Dominio.Tipos.Roles.VendedorEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialUsuario,
                },

                //Contactabilidad Empresa
                new RolReclamo() {
                    Id = 16,
                    RoleId = (short) Dominio.Tipos.Roles.ContactabilidadEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 17,
                    RoleId = (short) Dominio.Tipos.Roles.ContactabilidadEmpresa,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialUsuario,
                },

                //Administrador COOPROGRESO
                new RolReclamo() {
                    Id = 18,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorCooprogreso,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.Consultas,
                },
                new RolReclamo() {
                    Id = 19,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorCooprogreso,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialEmpresa,
                },
                new RolReclamo() {
                    Id = 20,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorCooprogreso,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.ReporteResumenCliente,
                },
                new RolReclamo() {
                    Id = 21,
                    RoleId = (short) Dominio.Tipos.Roles.AdministradorCooprogreso,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.AdministracionUsuariosCooprogreso,
                },

                //Reporteria
                new RolReclamo() {
                    Id = 22,
                    RoleId = (short) Dominio.Tipos.Roles.Reporteria,
                    ClaimType = CustomClaimTypes.Screen,
                    ClaimValue = Dominio.Tipos.Pantallas.HistorialEmpresa,
                },
            });
            #endregion Seed Data
        }
    }

    public sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<UsuarioToken>
    {
        public void Configure(EntityTypeBuilder<UsuarioToken> e)
        {
            e.ToTable("UsuariosTokens", Esquemas.Identidad);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.UserId).HasColumnName("IdUsuario");
            e.Property(m => m.LoginProvider).HasMaxLength(Longitudes.Key).HasColumnName("Proveedor");
            e.Property(m => m.Name).HasMaxLength(Longitudes.Key).HasColumnName("Nombre");
            e.Property(m => m.Value).HasMaxLength(Longitudes.Key).HasColumnName("Valor");

            e.HasOne(m => m.Usuario).WithMany(m => m.TokensUsuario).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
