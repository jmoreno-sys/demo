// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dominio.Entidades.Balances;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Balance
{
    public interface IEmpresas : IRepositorio<Empresa>
    {
        Task CrearAsync(Empresa registro, byte[] contrato);
        Task EditarAsync(Empresa registro, byte[] contrato, bool habilitarPlanEvaluacion = false, bool habilitarPlanBuro = false);
        Task ActivarEmpresaAsync(int id, int idUsuario);
        Task InactivarEmpresaAsync(int id, int idUsuario);
        Task EliminarHistorialAsync(int idEmpresa);
        Task EliminarHistorial7AniosAsync(int idEmpresa);

        Task ActivarPlanEmpresaAsync(int id, int idUsuario);
        Task InactivarPlanEmpresaAsync(int id, int idUsuario);

        Task ActivarPlanBuroAsync(int id, int idUsuario);
        Task InactivarPlanBuroAsync(int id, int idUsuario);

        Task ActivarPlanEvaluacionAsync(int id, int idUsuario);
        Task InactivarPlanEvaluacionAsync(int id, int idUsuario);
    }
    public class RepositorioEmpresas : Repositorio<Empresa>, IEmpresas
    {
        private readonly ILogger _logger;
        private readonly IPlanesEmpresas _planesEmpresas;
        private readonly IPlanesEvaluaciones _planesEvaluaciones;
        private readonly IPlanesBuroCredito _planesBuroCredito;
        protected readonly IDbContextFactory<ContextoPrincipal> ContextFactory;
        public RepositorioEmpresas(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger, IPlanesEmpresas planesEmpresas, IPlanesBuroCredito planesBuroCredito, IPlanesEvaluaciones planesEvaluaciones) : base(context, logger)
        {
            _logger = logger.CreateLogger(GetType());
            _planesEmpresas = planesEmpresas;
            _planesEvaluaciones = planesEvaluaciones;
            _planesBuroCredito = planesBuroCredito;
            ContextFactory = context;
        }

        #region Empresa
        public async Task CrearAsync(Empresa registro, byte[] contrato)
        {
            await Validar(registro);
            registro = Normalizar(registro);
            registro.FechaCreacion = DateTime.Now;
            registro.Estado = Dominio.Tipos.EstadosEmpresas.Activo;
            if (registro.UsuarioCreacion.HasValue)
                registro.IdUsuarioRegistro = registro.UsuarioCreacion;
            registro.PlanesEmpresas = registro.PlanesEmpresas.Select(m => new PlanEmpresa()
            {
                NombrePlan = m.NombrePlan.Trim().ToUpper(),
                FechaInicioPlan = DateTime.Now,
                FechaFinPlan = DateTime.Now.AddYears(1),
                Estado = Dominio.Tipos.EstadosPlanesEmpresas.Activo,
                BloquearConsultas = m.BloquearConsultas,
                FechaCreacion = DateTime.Now,
                UsuarioCreacion = registro.UsuarioCreacion,
                NumeroConsultasCedula = m.NumeroConsultasCedula,
                NumeroConsultasRuc = m.NumeroConsultasRuc,
                ValorPlanMensualCedula = m.ValorPlanMensualCedula,
                ValorPlanMensualRuc = m.ValorPlanMensualRuc,
                ValorPorConsultaRucs = m.ValorPorConsultaRucs,
                ValorPorConsultaCedulas = m.ValorPorConsultaCedulas,
                ValorConsultaAdicionalRucs = m.ValorConsultaAdicionalRucs,
                ValorConsultaAdicionalCedulas = m.ValorConsultaAdicionalCedulas,
                ValorPlanAnualCedula = m.ValorPlanAnualCedula,
                ValorPlanAnualRuc = m.ValorPlanAnualRuc,
                ValorPlanAnual = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? m.ValorPlanAnual : null,
                ValorPlanMensual = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? m.ValorPlanMensual : null,
                ValorPorConsulta = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? m.ValorPorConsulta : null,
                ValorConsultaAdicional = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? m.ValorConsultaAdicional : null,
                NumeroConsultas = m.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? m.NumeroConsultas : null,
                TipoPlan = m.TipoPlan,
                PlanDemostracion = m.PlanDemostracion
            }).ToList();

            if (registro.PlanesEvaluaciones != null && registro.PlanesEvaluaciones.Any())
                registro.PlanesEvaluaciones = registro.PlanesEvaluaciones.Select(m => new PlanEvaluacion()
                {
                    FechaInicioPlan = DateTime.Now,
                    FechaFinPlan = DateTime.Now.AddYears(1),
                    Estado = Dominio.Tipos.EstadosPlanesEvaluaciones.Activo,
                    BloquearConsultas = m.BloquearConsultas,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreacion = registro.UsuarioCreacion,
                    NumeroConsultas = m.NumeroConsultas,
                    ValorConsulta = m.ValorConsulta,
                    ValorConsultaAdicional = m.ValorConsultaAdicional,
                }).ToList();

            if (registro.PlanesBuroCredito != null && registro.PlanesBuroCredito.Any())

                registro.PlanesBuroCredito = registro.PlanesBuroCredito.Select(m => new PlanBuroCredito()
                {
                    FechaInicioPlan = DateTime.Now,
                    FechaFinPlan = DateTime.Now.AddYears(1),
                    Estado = Dominio.Tipos.EstadosPlanesBuroCredito.Activo,
                    BloquearConsultas = m.BloquearConsultas,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreacion = registro.UsuarioCreacion,
                    PersistenciaCache = m.PersistenciaCache,
                    NumeroMaximoConsultas = m.NumeroMaximoConsultas,
                    ValorConsulta = m.ValorConsulta,
                    ValorConsultaAdicional = m.ValorConsultaAdicional,
                    ValorPlanBuroCredito = m.ValorPlanBuroCredito,
                    Fuente = m.Fuente,
                    ConsultasCompartidas = m.ConsultasCompartidas,
                    NumeroMaximoConsultasCompartidas = m.NumeroMaximoConsultasCompartidas,
                    ModeloCooperativas = m.ModeloCooperativas
                }).ToList();

            var pathBase = System.IO.Path.Combine("wwwroot", "data");
            var pathPoliticas = Path.Combine(pathBase, "politicasEmpresa.json");
            var politicas = JsonConvert.DeserializeObject<List<Politica>>(System.IO.File.ReadAllText(pathPoliticas));

            if (politicas != null && politicas.Any())
                registro.Politicas = politicas;

            if (!string.IsNullOrEmpty(registro.RutaLogo))
                registro.RutaLogo = await WriteImageAsync(registro.Identificacion, registro.RutaLogo);

            if (contrato != null && contrato.Length > 0)
                registro.RutaContrato = await RegistrarContrato($"{registro.Identificacion}.pdf", contrato);

            await base.CreateAsync(registro);
        }

        public async Task EditarAsync(Empresa registro, byte[] contrato, bool habilitarPlanEvaluacion = false, bool habilitarPlanBuro = false)
        {
            await Validar(registro);
            registro = Normalizar(registro);
            await using var context = ContextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var dataRegistro = await base.FirstOrDefaultAsync(t => t, t => t.Id == registro.Id, null, i => i.Include(m => m.PlanesEmpresas).Include(m => m.PlanesEvaluaciones).Include(m => m.PlanesBuroCredito));
                if (dataRegistro == null)
                    throw new Exception("La empresa ingresada no se encuentra registrada");

                #region Empresa
                dataRegistro.Identificacion = registro.Identificacion;
                dataRegistro.RazonSocial = registro.RazonSocial;
                dataRegistro.PersonaContacto = registro.PersonaContacto;
                dataRegistro.CorreoPersonaContacto = registro.CorreoPersonaContacto;
                dataRegistro.Telefono = registro.Telefono;
                dataRegistro.Correo = registro.CorreoPersonaContacto;
                dataRegistro.Direccion = registro.Direccion;
                dataRegistro.Observaciones = registro.Observaciones;
                dataRegistro.IdAsesorComercialConfiable = registro.IdAsesorComercialConfiable;
                dataRegistro.FechaCobroRecurrente = registro.FechaCobroRecurrente;
                dataRegistro.UsuarioModificacion = registro.UsuarioModificacion;
                dataRegistro.FechaModificacion = DateTime.Now;
                if (!string.IsNullOrEmpty(registro.RutaLogo))
                    dataRegistro.RutaLogo = await WriteImageAsync(registro.Identificacion, registro.RutaLogo);

                if (contrato != null && contrato.Length > 0)
                    dataRegistro.RutaContrato = await RegistrarContrato($"{registro.Identificacion}.pdf", contrato);

                await base.UpdateAsync(dataRegistro);
                #endregion Empresa

                #region Plan Empresa
                var planEmpresa = registro.PlanesEmpresas.FirstOrDefault();
                if (planEmpresa != null)
                {
                    if (planEmpresa.Id == 0)
                    {
                        //Inactivación
                        foreach (var item in dataRegistro.PlanesEmpresas.Where(m => m.Estado != Dominio.Tipos.EstadosPlanesEmpresas.Inactivo))
                        {
                            item.Estado = Dominio.Tipos.EstadosPlanesEmpresas.Inactivo;
                            item.FechaModificacion = DateTime.Now;
                            item.UsuarioModificacion = dataRegistro.UsuarioModificacion;
                        }
                        await _planesEmpresas.UpdateAsync(dataRegistro.PlanesEmpresas.ToArray());

                        //Registro Nuevo
                        planEmpresa.Id = 0;
                        planEmpresa.IdEmpresa = dataRegistro.Id;
                        planEmpresa.NombrePlan = planEmpresa.NombrePlan.Trim().ToUpper();
                        planEmpresa.FechaInicioPlan = DateTime.Now;
                        planEmpresa.FechaFinPlan = DateTime.Now.AddYears(1);
                        planEmpresa.Estado = Dominio.Tipos.EstadosPlanesEmpresas.Activo;
                        planEmpresa.UsuarioCreacion = registro.UsuarioCreacion;
                        planEmpresa.FechaCreacion = DateTime.Now;
                        await _planesEmpresas.CreateAsync(planEmpresa);
                    }
                    else
                    {
                        //Edición
                        var planEmpresaActual = dataRegistro.PlanesEmpresas.FirstOrDefault(m => m.Id == planEmpresa.Id);
                        if (planEmpresaActual != null)
                        {
                            planEmpresaActual.NombrePlan = planEmpresa.NombrePlan.Trim().ToUpper();
                            planEmpresaActual.BloquearConsultas = planEmpresa.BloquearConsultas;
                            planEmpresaActual.NumeroConsultasCedula = planEmpresa.NumeroConsultasCedula;
                            planEmpresaActual.NumeroConsultasRuc = planEmpresa.NumeroConsultasRuc;
                            planEmpresaActual.ValorPlanMensualCedula = planEmpresa.ValorPlanMensualCedula;
                            planEmpresaActual.ValorPlanMensualRuc = planEmpresa.ValorPlanMensualRuc;
                            planEmpresaActual.ValorPorConsultaRucs = planEmpresa.ValorPorConsultaRucs;
                            planEmpresaActual.ValorPorConsultaCedulas = planEmpresa.ValorPorConsultaCedulas;
                            planEmpresaActual.ValorConsultaAdicionalRucs = planEmpresa.ValorConsultaAdicionalRucs;
                            planEmpresaActual.ValorConsultaAdicionalCedulas = planEmpresa.ValorConsultaAdicionalCedulas;
                            planEmpresaActual.ValorPlanAnualCedula = planEmpresa.ValorPlanAnualCedula;
                            planEmpresaActual.ValorPlanAnualRuc = planEmpresa.ValorPlanAnualRuc;
                            planEmpresaActual.NumeroConsultas = planEmpresa.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? planEmpresa.NumeroConsultas : null;
                            planEmpresaActual.ValorPlanMensual = planEmpresa.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? planEmpresa.ValorPlanMensual : null;
                            planEmpresaActual.ValorPorConsulta = planEmpresa.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? planEmpresa.ValorPorConsulta : null;
                            planEmpresaActual.ValorConsultaAdicional = planEmpresa.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? planEmpresa.ValorConsultaAdicional : null;
                            planEmpresaActual.ValorPlanAnual = planEmpresa.TipoPlan == Dominio.Tipos.PlanesIdentificaciones.Unificado ? planEmpresa.ValorPlanAnual : null;
                            planEmpresaActual.TipoPlan = planEmpresa.TipoPlan;
                            planEmpresaActual.PlanDemostracion = planEmpresa.PlanDemostracion;
                            planEmpresaActual.FechaModificacion = DateTime.Now;
                            planEmpresaActual.UsuarioModificacion = registro.UsuarioModificacion;
                            await _planesEmpresas.UpdateAsync(planEmpresaActual);
                        }
                    }
                }
                #endregion PlanEmpresa

                #region PlanEvaluacion
                if (registro.PlanesEvaluaciones != null && registro.PlanesEvaluaciones.Any())
                {
                    var planEvaluacion = registro.PlanesEvaluaciones.FirstOrDefault();
                    if (planEvaluacion != null)
                    {
                        if (planEvaluacion.Id == 0)
                        {
                            //Inactivación
                            foreach (var item in dataRegistro.PlanesEvaluaciones.Where(m => m.Estado != Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo))
                            {
                                item.Estado = Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo;
                                item.FechaModificacion = DateTime.Now;
                                item.UsuarioModificacion = dataRegistro.UsuarioModificacion;
                            }
                            await _planesEvaluaciones.UpdateAsync(dataRegistro.PlanesEvaluaciones.ToArray());

                            //Registro Nuevo
                            planEvaluacion.Id = 0;
                            planEvaluacion.IdEmpresa = dataRegistro.Id;
                            planEvaluacion.FechaInicioPlan = DateTime.Now;
                            planEvaluacion.FechaFinPlan = DateTime.Now.AddYears(1);
                            planEvaluacion.Estado = Dominio.Tipos.EstadosPlanesEvaluaciones.Activo;
                            planEvaluacion.FechaCreacion = DateTime.Now;
                            planEvaluacion.UsuarioCreacion = registro.UsuarioCreacion;
                            await _planesEvaluaciones.CreateAsync(planEvaluacion);
                        }
                        else
                        {
                            //Edición
                            var planEvaluacionActual = dataRegistro.PlanesEvaluaciones.FirstOrDefault(m => m.Id == planEvaluacion.Id);
                            if (planEvaluacionActual != null)
                            {
                                planEvaluacionActual.BloquearConsultas = planEvaluacion.BloquearConsultas;
                                planEvaluacionActual.NumeroConsultas = planEvaluacion.NumeroConsultas;
                                planEvaluacionActual.ValorConsulta = planEvaluacion.ValorConsulta;
                                planEvaluacionActual.ValorConsultaAdicional = planEvaluacion.ValorConsultaAdicional;
                                planEvaluacionActual.FechaModificacion = DateTime.Now;
                                planEvaluacionActual.UsuarioModificacion = registro.UsuarioModificacion;
                                await _planesEvaluaciones.UpdateAsync(planEvaluacionActual);
                            }
                        }
                    }
                }
                else if (!habilitarPlanEvaluacion && dataRegistro.PlanesEvaluaciones != null && dataRegistro.PlanesEvaluaciones.Any())
                {
                    //Inactivación Completa (Switch apagado)
                    foreach (var item in dataRegistro.PlanesEvaluaciones.Where(m => m.Estado != Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo))
                    {
                        item.Estado = Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo;
                        item.FechaModificacion = DateTime.Now;
                        item.UsuarioModificacion = registro.UsuarioModificacion;
                    }
                    await _planesEvaluaciones.UpdateAsync(dataRegistro.PlanesEvaluaciones.ToArray());
                }
                #endregion PlanEvaluacion

                #region PlanBuro
                if (registro.PlanesBuroCredito != null && registro.PlanesBuroCredito.Any())
                {
                    var planBuroCredito = registro.PlanesBuroCredito.FirstOrDefault();
                    if (planBuroCredito != null)
                    {
                        if (planBuroCredito.Id == 0)
                        {
                            //Inactivación
                            foreach (var item in dataRegistro.PlanesBuroCredito.Where(m => m.Estado != Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo))
                            {
                                item.Estado = Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
                                item.FechaModificacion = DateTime.Now;
                                item.UsuarioModificacion = dataRegistro.UsuarioModificacion;
                            }
                            await _planesBuroCredito.UpdateAsync(dataRegistro.PlanesBuroCredito.ToArray());

                            //Registro Nuevo
                            planBuroCredito.Id = 0;
                            planBuroCredito.IdEmpresa = dataRegistro.Id;
                            planBuroCredito.FechaInicioPlan = DateTime.Now;
                            planBuroCredito.FechaFinPlan = DateTime.Now.AddYears(1);
                            planBuroCredito.Estado = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;
                            planBuroCredito.FechaCreacion = DateTime.Now;
                            planBuroCredito.UsuarioCreacion = registro.UsuarioCreacion;
                            await _planesBuroCredito.CreateAsync(planBuroCredito);
                        }
                        else
                        {
                            //Edición
                            var planBuroCreditoActual = dataRegistro.PlanesBuroCredito.FirstOrDefault(m => m.Id == planBuroCredito.Id);
                            if (planBuroCreditoActual != null)
                            {
                                planBuroCreditoActual.BloquearConsultas = planBuroCredito.BloquearConsultas;
                                planBuroCreditoActual.NumeroMaximoConsultas = planBuroCredito.NumeroMaximoConsultas;
                                planBuroCreditoActual.ValorPlanBuroCredito = planBuroCredito.ValorPlanBuroCredito;
                                planBuroCreditoActual.ValorConsulta = planBuroCredito.ValorConsulta;
                                planBuroCreditoActual.ValorConsultaAdicional = planBuroCredito.ValorConsultaAdicional;
                                planBuroCreditoActual.PersistenciaCache = planBuroCredito.PersistenciaCache;
                                planBuroCreditoActual.FechaModificacion = DateTime.Now;
                                planBuroCreditoActual.UsuarioModificacion = registro.UsuarioModificacion;
                                planBuroCreditoActual.Fuente = planBuroCredito.Fuente;
                                planBuroCreditoActual.ConsultasCompartidas = planBuroCredito.ConsultasCompartidas;
                                planBuroCreditoActual.NumeroMaximoConsultasCompartidas = planBuroCredito.NumeroMaximoConsultasCompartidas;
                                planBuroCreditoActual.ModeloCooperativas = planBuroCredito.ModeloCooperativas;
                                await _planesBuroCredito.UpdateAsync(planBuroCreditoActual);
                            }
                        }
                    }
                }
                else if (!habilitarPlanBuro && dataRegistro.PlanesBuroCredito != null && dataRegistro.PlanesBuroCredito.Any())
                {
                    //Inactivación Completa (Switch apagado)
                    foreach (var item in dataRegistro.PlanesBuroCredito.Where(m => m.Estado != Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo))
                    {
                        item.Estado = Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
                        item.FechaModificacion = DateTime.Now;
                        item.UsuarioModificacion = registro.UsuarioModificacion;
                    }
                    await _planesBuroCredito.UpdateAsync(dataRegistro.PlanesBuroCredito.ToArray());
                }
                #endregion PlanBuro
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }

        }

        public async Task InactivarEmpresaAsync(int id, int idUsuario)
        {
            var empresa = await base.FirstOrDefaultAsync(t => t, t => t.Id == id);
            if (empresa == null)
                throw new Exception("La empresa ingresada no se encuentra registrada");

            empresa.Estado = Dominio.Tipos.EstadosEmpresas.Inactivo;
            empresa.UsuarioModificacion = idUsuario;
            empresa.FechaModificacion = DateTime.Now;
            await base.UpdateAsync(empresa);
        }

        public async Task ActivarEmpresaAsync(int id, int idUsuario)
        {
            var empresa = await base.FirstOrDefaultAsync(t => t, t => t.Id == id);
            if (empresa == null)
                throw new Exception("La empresa ingresada no se encuentra registrada");

            empresa.Estado = Dominio.Tipos.EstadosEmpresas.Activo;
            empresa.UsuarioModificacion = idUsuario;
            empresa.FechaModificacion = DateTime.Now;
            await base.UpdateAsync(empresa);
        }

        public async Task EliminarHistorialAsync(int idEmpresa)
        {
            if (idEmpresa != Dominio.Constantes.General.IdEmpresaDemo)
                throw new Exception("No es posible eliminar el historial de esta empresa");

            await using var context = ContextFactory.CreateDbContext();

            var historiales = await context.Set<Historial>().Include(m => m.DetalleHistorial).Where(m => m.PlanEmpresa.Empresa.Id == idEmpresa && m.Fecha.Month == DateTime.Today.Month && m.Fecha.Year == DateTime.Today.Year).ToListAsync();
            if (historiales.Any())
            {
                foreach (var item in historiales)
                    context.Set<DetalleHistorial>().RemoveRange(item.DetalleHistorial);

                context.Set<Historial>().RemoveRange(historiales);
            }

            await context.SaveChangesAsync();
        }

        public async Task EliminarHistorial7AniosAsync(int idEmpresa)
        {
            var fechaLimite = DateTime.Today.AddYears(-7);
            await using var context = ContextFactory.CreateDbContext();

            var historiales = await context.Set<Historial>().Include(m => m.DetalleHistorial).Include(i => i.PlanEmpresa).Where(m => m.PlanEmpresa.IdEmpresa == idEmpresa && m.Fecha < fechaLimite).ToListAsync();
            var calificaciones = await context.Set<Calificacion>().Include(m => m.DetalleCalificacion).Include(i => i.Historial).ThenInclude(i => i.PlanEmpresa).Where(m => m.Historial.PlanEmpresa.IdEmpresa == idEmpresa && m.FechaCreacion < fechaLimite).ToListAsync();
            var parametros = await context.Set<ParametroClienteHistorial>().Include(i => i.Historial).ThenInclude(t => t.PlanEmpresa).Where(m => m.Historial.PlanEmpresa.IdEmpresa == idEmpresa && m.FechaCreacion < fechaLimite).ToListAsync();
            if (historiales.Any())
            {
                foreach (var item in historiales)
                    context.Set<DetalleHistorial>().RemoveRange(item.DetalleHistorial);
            }
            if (calificaciones.Any())
            {
                foreach (var item in calificaciones)
                    context.Set<DetalleCalificacion>().RemoveRange(item.DetalleCalificacion);
            }
            if (parametros.Any())
            {
                context.Set<ParametroClienteHistorial>().RemoveRange(parametros);
            }
            if (calificaciones.Any()) context.Set<Calificacion>().RemoveRange(calificaciones);
            if (historiales.Any()) context.Set<Historial>().RemoveRange(historiales);


            await context.SaveChangesAsync();
        }

        #endregion Empresa

        #region Plan Empresa
        public async Task InactivarPlanEmpresaAsync(int id, int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            var planEmpresa = await context.Set<PlanEmpresa>().FirstOrDefaultAsync(t => t.Id == id);
            if (planEmpresa == null)
                throw new Exception("El plan de empresa no se encuentra registrado");

            planEmpresa.Estado = Dominio.Tipos.EstadosPlanesEmpresas.Inactivo;
            planEmpresa.UsuarioModificacion = idUsuario;
            planEmpresa.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
        }

        public async Task ActivarPlanEmpresaAsync(int id, int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            var planEmpresa = await context.Set<PlanEmpresa>().FirstOrDefaultAsync(t => t.Id == id);
            if (planEmpresa == null)
                throw new Exception("El plan de empresa no se encuentra registrado");

            if (await context.Set<PlanEmpresa>().AnyAsync(m => m.Estado == Dominio.Tipos.EstadosPlanesEmpresas.Activo && m.Id != id && m.IdEmpresa == planEmpresa.IdEmpresa))
                throw new Exception("No es posible tener más de un plan activo. Inactive uno de los planes activos vigentes");

            planEmpresa.Estado = Dominio.Tipos.EstadosPlanesEmpresas.Activo;
            planEmpresa.UsuarioModificacion = idUsuario;
            planEmpresa.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
        }
        #endregion Plan Empresa

        #region Plan Buró
        public async Task InactivarPlanBuroAsync(int id, int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            var planEmpresaBuro = await context.Set<PlanBuroCredito>().FirstOrDefaultAsync(t => t.Id == id);
            if (planEmpresaBuro == null)
                throw new Exception("El plan de empresa no se encuentra registrado");

            planEmpresaBuro.Estado = Dominio.Tipos.EstadosPlanesBuroCredito.Inactivo;
            planEmpresaBuro.UsuarioModificacion = idUsuario;
            planEmpresaBuro.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
        }

        public async Task ActivarPlanBuroAsync(int id, int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            var planEmpresaBuro = await context.Set<PlanBuroCredito>().FirstOrDefaultAsync(t => t.Id == id);
            if (planEmpresaBuro == null)
                throw new Exception("El plan de empresa no se encuentra registrado");

            if (await context.Set<PlanBuroCredito>().AnyAsync(m => m.Estado == Dominio.Tipos.EstadosPlanesBuroCredito.Activo && m.Id != id && m.IdEmpresa == planEmpresaBuro.IdEmpresa))
                throw new Exception("No es posible tener más de un plan activo. Inactive uno de los planes activos vigentes");

            planEmpresaBuro.Estado = Dominio.Tipos.EstadosPlanesBuroCredito.Activo;
            planEmpresaBuro.UsuarioModificacion = idUsuario;
            planEmpresaBuro.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
        }
        #endregion Plan Buró

        #region Plan Evaluación
        public async Task InactivarPlanEvaluacionAsync(int id, int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            var planEvaluacion = await context.Set<PlanEvaluacion>().FirstOrDefaultAsync(t => t.Id == id);
            if (planEvaluacion == null)
                throw new Exception("El plan de evaluación no se encuentra registrado");

            planEvaluacion.Estado = Dominio.Tipos.EstadosPlanesEvaluaciones.Inactivo;
            planEvaluacion.UsuarioModificacion = idUsuario;
            planEvaluacion.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
        }

        public async Task ActivarPlanEvaluacionAsync(int id, int idUsuario)
        {
            await using var context = ContextFactory.CreateDbContext();
            var planEvaluacion = await context.Set<PlanEvaluacion>().FirstOrDefaultAsync(t => t.Id == id);
            if (planEvaluacion == null)
                throw new Exception("El plan de evaluación no se encuentra registrado");

            if (await context.Set<PlanEvaluacion>().AnyAsync(m => m.Estado == Dominio.Tipos.EstadosPlanesEvaluaciones.Activo && m.Id != id && m.IdEmpresa == planEvaluacion.IdEmpresa))
                throw new Exception("No es posible tener más de un plan activo. Inactive uno de los planes activos vigentes");

            planEvaluacion.Estado = Dominio.Tipos.EstadosPlanesEvaluaciones.Activo;
            planEvaluacion.UsuarioModificacion = idUsuario;
            planEvaluacion.FechaModificacion = DateTime.Now;
            await context.SaveChangesAsync();
        }
        #endregion Plan Evaluación

        #region Varios
        private Empresa Normalizar(Empresa registro)
        {
            registro.Identificacion = registro.Identificacion?.ToUpper().Trim();
            registro.RazonSocial = registro.RazonSocial?.ToUpper().Trim();
            registro.PersonaContacto = registro.PersonaContacto?.ToUpper().Trim();
            registro.Correo = registro.Correo?.Trim().ToUpper();
            registro.CorreoPersonaContacto = registro.Correo?.ToUpper().Trim();
            registro.Telefono = registro.Telefono?.Trim().ToUpper();
            registro.TelefonoPersonaContacto = registro.TelefonoPersonaContacto?.Trim().ToUpper();
            registro.Direccion = registro.Direccion?.ToUpper().Trim();
            registro.Observaciones = registro.Observaciones?.ToUpper().Trim();
            registro.AsesorComercial = registro.AsesorComercial?.ToUpper().Trim();
            return registro;
        }

        private async Task Validar(Empresa registro)
        {
            if (registro == null)
                throw new Exception("No se ha encontrado el registro");

            if (string.IsNullOrWhiteSpace(registro.Identificacion?.Trim()))
                throw new Exception("Debe ingresar la identificación");

            if (string.IsNullOrWhiteSpace(registro.RazonSocial?.Trim()))
                throw new Exception("Debe ingresar la razón social");

            if (string.IsNullOrWhiteSpace(registro.PersonaContacto?.Trim()))
                throw new Exception("Debe ingresar una persona de contacto");

            if (string.IsNullOrWhiteSpace(registro.Telefono?.Trim()))
                throw new Exception("Debe ingresar un teléfono de contacto");

            if (string.IsNullOrWhiteSpace(registro.Correo?.Trim()))
                throw new Exception("Debe ingresar un correo de contacto");

            if (!registro.FechaCobroRecurrente.HasValue)
                throw new Exception("Debe seleccionar una fecha de inicio de cobro recurrente");

            if (registro.FechaCobroRecurrente.Value == default)
                throw new Exception("La fecha de inicio de cobro recurrente no es válida");

            if (registro.IdAsesorComercialConfiable.HasValue && registro.IdAsesorComercialConfiable.Value == 0)
                throw new Exception("El asesor comercial seleccionado no es válido");

            if (registro.Id == 0)
            {
                if (await base.AnyAsync(t => t.Identificacion.Trim() == registro.Identificacion.Trim()))
                    throw new Exception("La empresa ya se encuentra registrada.");

                if (registro.PlanesEmpresas == null)
                    throw new Exception("No se ha ingresado el Plan de la Empresa correctamente");

                if (registro.PlanesEmpresas.Any(m => string.IsNullOrEmpty(m.NombrePlan?.Trim())))
                    throw new Exception("No se ha ingresado el Nombre del Plan");
            }
            else
            {
                if (await base.AnyAsync(t => t.Identificacion.Trim() == registro.Identificacion.Trim() && t.Id != registro.Id))
                    throw new Exception("La empresa ya se encuentra registrada.");
            }

            if (registro.PlanesBuroCredito != null && registro.PlanesBuroCredito.Any() && registro.PlanesBuroCredito.Any(m => m.ConsultasCompartidas && (!m.NumeroMaximoConsultasCompartidas.HasValue || m.NumeroMaximoConsultasCompartidas == 0)))
                throw new Exception("El Número Máximo de Consultas (Credenciales Internas) para el Buró de Crédito no puede ir vacio");
        }

        private async Task<string> WriteImageAsync(string code, string base64String)
        {
            var match = Regex.Match(base64String, @"data:image/(?<type>.+?),(?<data>.+)");
            var type = match.Groups["type"].Value.Split(';').FirstOrDefault()?.ToLower();
            var ext = "jpg";

            switch (type)
            {
                case "png":
                    ext = "png";
                    break;

                case "tiff":
                    ext = "tiff";
                    break;

                default:
                    ext = "jpg";
                    break;
            }
            var base64Data = match.Groups["data"].Value;
            var imageBytes = Convert.FromBase64String(base64Data);

            var fileName = $"{code}.{ext}".ToLower();
            var absolutePath = Path.Combine("wwwroot", "app", "logos");
            var filePath = Path.Combine(absolutePath, fileName);

            try
            {
                if (!Directory.Exists(absolutePath)) Directory.CreateDirectory(absolutePath);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                return File.Exists(filePath) ? fileName : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return null;
            }
        }

        private async Task<string> RegistrarContrato(string nombreContrato, byte[] contrato)
        {
            try
            {
                if (string.IsNullOrEmpty(nombreContrato?.Trim())) throw new Exception("Es obligatorio ingresar el nombre del contrato");
                if (contrato == null) throw new Exception("Se ha cargado un archivo vacío, revisar el mismo por favor.");
                if (contrato.Length == 0) throw new Exception("Se ha cargado un archivo vacío, revisar el mismo por favor.");

                var absolutePath = Path.Combine("wwwroot", "app", "contratos");
                var filePath = Path.Combine(absolutePath, nombreContrato);

                if (!Directory.Exists(absolutePath)) Directory.CreateDirectory(absolutePath);

                await File.WriteAllBytesAsync(filePath, contrato);
                return nombreContrato;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }

        private string GetFeatureImage(string name)
        {
            try
            {
                var absolutePath = Path.Combine("wwwroot", "app", "logos");
                var relativePath = Path.Combine("app", "logos");
                var result = string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    var fileName = Path.Combine(absolutePath, name);
                    if (File.Exists(fileName))
                        result = Path.DirectorySeparatorChar + Path.Combine(relativePath, name);
                }
                return result?.Replace(Path.DirectorySeparatorChar, '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            return null;
        }
        #endregion Varios
    }
}
