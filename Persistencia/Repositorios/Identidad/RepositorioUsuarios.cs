// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Dominio.Entidades.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;
using Persistencia.Repositorios.Balance;

namespace Persistencia.Repositorios.Identidad
{
    public interface IUsuarios : IRepositorio<Usuario>
    {
        Task<Usuario> ObtenerInformacionUsuarioAsync(int idUsuario);
        Task CrearAsync(Usuario registro);
        Task EditarAsync(Usuario registro);
        Task ActivarAsync(int id, int idUsuario, bool usuarioCooprogreso = false);
        Task InactivarAsync(int id, int idUsuario, bool usuarioCooprogreso = false);
        Task DesbloquearAsync(int id, int idUsuario, bool usuarioCooprogreso = false);
    }

    public class RepositorioUsuario : Repositorio<Usuario>, IUsuarios
    {
        private readonly ILogger _logger;
        private readonly IAccesos _accesos;

        public RepositorioUsuario(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger, IAccesos accesos) : base(context, logger)
        {
            _logger = logger.CreateLogger(GetType());
            _accesos = accesos;
        }

        public async Task<Usuario> ObtenerInformacionUsuarioAsync(int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            return await context.Set<Usuario>()
                .Include(m => m.Empresa.PlanesEmpresas)
                .Include(m => m.Empresa.PlanesBuroCredito)
                .Include(m => m.Empresa.PlanesEvaluaciones)
                .FirstOrDefaultAsync(m => m.Id == idUsuario);
        }

        public async Task CrearAsync(Usuario registro)
        {
            await Validar(registro);

            registro = Normalizar(registro);
            registro.Estado = Dominio.Tipos.EstadosUsuarios.Activo;
            var clave = registro.PasswordHash;
            if (string.IsNullOrEmpty(clave))
                clave = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);

            registro.ConcurrencyStamp = Guid.NewGuid().ToString();
            registro.SecurityStamp = Guid.NewGuid().ToString();
            registro.PasswordHash = new PasswordHasher<Usuario>().HashPassword(registro, clave);
            registro.FechaCreacion = DateTime.Now;

            if (registro.Accesos != null && registro.Accesos.Any())
                registro.Accesos = registro.Accesos.Select(m => new AccesoUsuario()
                {
                    Estado = Dominio.Tipos.EstadosAccesos.Activo,
                    Acceso = m.Acceso,
                    UsuarioCreacion = registro.UsuarioCreacion,
                    FechaCreacion = registro.FechaCreacion,
                }).ToList();

            await base.CreateAsync(registro);
        }

        public async Task EditarAsync(Usuario registro)
        {
            await Validar(registro);

            registro = Normalizar(registro);

            await using var context = ContextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Cargar desde el mismo contexto para que el tracking sea correcto
                var dataRegistro = await context.Set<Usuario>()
                    .Include(m => m.UsuariosRoles)
                    .Include(m => m.Accesos)
                    .FirstOrDefaultAsync(m => m.Id == registro.Id);

                if (dataRegistro == null)
                    throw new Exception("No se ha encontrado el usuario");

                // Actualizar datos principales
                dataRegistro.IdEmpresa = registro.IdEmpresa;
                dataRegistro.NombreCompleto = registro.NombreCompleto;
                dataRegistro.Email = registro.Email;
                dataRegistro.NormalizedEmail = registro.Email;
                dataRegistro.PhoneNumber = registro.PhoneNumber;
                dataRegistro.FechaModificacion = DateTime.Now;

                // Actualizar Rol
                if (dataRegistro.UsuariosRoles.Any())
                    dataRegistro.UsuariosRoles.First().RoleId = registro.UsuariosRoles.First().RoleId;

                // Eliminar accesos anteriores (si existen)
                if (dataRegistro.Accesos != null && dataRegistro.Accesos.Any())
                    await _accesos.DeleteAsync(dataRegistro.Accesos.Select(m => m.Id).ToArray());

                // Agregar nuevos accesos (si se enviaron)
                if (registro.Accesos != null && registro.Accesos.Any())
                {
                    var nuevosAccesos = registro.Accesos.Select(m => new AccesoUsuario
                    {
                        Estado = Dominio.Tipos.EstadosAccesos.Activo,
                        Acceso = m.Acceso,
                        IdUsuario = registro.Id,
                        UsuarioCreacion = registro.UsuarioCreacion,
                        FechaCreacion = registro.FechaCreacion,
                        FechaModificacion = registro.FechaModificacion,
                    }).ToArray();

                    await _accesos.CreateAsync(nuevosAccesos);
                }

                // Adjuntar y actualizar entidad
                context.Update(dataRegistro);

                // Guardar cambios
                await context.SaveChangesAsync();

                // Confirmar transacción
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error de concurrencia al editar el usuario con ID {Id}", registro.Id);
                throw new Exception("El usuario fue modificado por otro usuario. Recargue la página e intente nuevamente.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al editar el usuario con ID {Id}: {Mensaje}", registro.Id, ex.Message);
                throw;
            }
        }



        private Usuario Normalizar(Usuario registro)
        {
            registro.NombreCompleto = registro.NombreCompleto?.ToUpper().Trim();
            registro.Email = registro.Email?.ToUpper().Trim();
            registro.UserName = registro.Identificacion?.ToUpper().Trim();
            registro.NormalizedUserName = registro.UserName?.ToUpper().Trim();
            registro.NormalizedEmail = registro.Email?.ToUpper().Trim();
            registro.PhoneNumber = registro.PhoneNumber?.Trim();
            return registro;
        }

        private async Task Validar(Usuario registro)
        {
            if (registro == null)
                throw new Exception("No se ha encontrado el registro");

            if (string.IsNullOrWhiteSpace(registro.Identificacion?.Trim()))
                throw new Exception("Debe ingresar la identificación");

            if (string.IsNullOrWhiteSpace(registro.NombreCompleto?.Trim()))
                throw new Exception("Debe ingresar el nombre");

            if (string.IsNullOrWhiteSpace(registro.Email?.Trim()))
                throw new Exception("Debe ingresar un email");

            if (registro.IdEmpresa == 0)
                throw new Exception("Debe seleccionar una empresa");

            if (registro.UsuariosRoles == null)
                throw new Exception("Debe seleccionar un rol");

            if (!registro.UsuariosRoles.Any())
                throw new Exception("Debe seleccionar un rol");

            if (registro.UsuariosRoles.Any(m => m.RoleId == 0))
                throw new Exception("Debe seleccionar un rol");

            if (registro.Id == 0)
            {
                if (await base.AnyAsync(t => t.Identificacion == registro.Identificacion))
                    throw new Exception("El usuario ya se encuentra registrado.");
            }
            else
            {
                if (await base.AnyAsync(t => t.Identificacion == registro.Identificacion && t.Id != registro.Id))
                    throw new Exception("El usuario ya se encuentra registrado.");
            }
        }

        public async Task InactivarAsync(int id, int idUsuario, bool usuarioCooprogreso)
        {
            var usuario = await base.FirstOrDefaultAsync(t => t, t => t.Id == id);
            if (usuario == null)
                throw new Exception("El usuario no se encuentra registrado");

            if (usuarioCooprogreso && usuario.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                throw new Exception("El usuario no corresponde a su empresa para administrar");

            usuario.Estado = Dominio.Tipos.EstadosUsuarios.Inactivo;
            usuario.UsuarioModificacion = idUsuario;
            usuario.FechaModificacion = DateTime.Now;
            await base.UpdateAsync(usuario);
        }

        public async Task ActivarAsync(int id, int idUsuario, bool usuarioCooprogreso = false)
        {
            var usuario = await base.FirstOrDefaultAsync(t => t, t => t.Id == id);
            if (usuario == null)
                throw new Exception("El usuario no se encuentra registrado");

            if (usuarioCooprogreso && usuario.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                throw new Exception("El usuario no corresponde a su empresa para administrar");

            usuario.Estado = Dominio.Tipos.EstadosUsuarios.Activo;
            usuario.UsuarioModificacion = idUsuario;
            usuario.FechaModificacion = DateTime.Now;
            await base.UpdateAsync(usuario);
        }

        public async Task DesbloquearAsync(int id, int idUsuario, bool usuarioCooprogreso = false)
        {
            var usuario = await base.FirstOrDefaultAsync(t => t, t => t.Id == id);
            if (usuario == null)
                throw new Exception("El usuario no se encuentra registrado");

            if (usuarioCooprogreso && usuario.IdEmpresa != Dominio.Constantes.Clientes.IdCliente1790451801001)
                throw new Exception("El usuario no corresponde a su empresa para administrar");

            usuario.LockoutEnd = null;
            usuario.AccessFailedCount = 0;
            usuario.UsuarioModificacion = idUsuario;
            usuario.FechaModificacion = DateTime.Now;
            await base.UpdateAsync(usuario);
        }
    }
}
