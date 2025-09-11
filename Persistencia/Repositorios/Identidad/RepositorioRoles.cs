using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio.Entidades.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistencia.Abstracciones;
using Persistencia.Interfaces;

namespace Persistencia.Repositorios.Identidad
{
    public interface IRoles : IRepositorio<Rol>
    {
    }
    public class RepositorioRoles : Repositorio<Rol>, IRoles
    {
        public RepositorioRoles(IDbContextFactory<ContextoPrincipal> context, ILoggerFactory logger) : base(context, logger)
        {
        }
    }
}
