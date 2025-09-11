using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Web.Areas.Administracion.Models;
using Persistencia.Repositorios.Identidad;
using Dominio.Entidades.Identidad;
using Web.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.Linq;
using Persistencia.Repositorios.Balance;
using Dominio.Entidades.Balances;
using Infraestructura.Servicios;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Web.Areas.Consultas.Controllers
{
    [Area("Consultas")]
    [Route("Consultas/Documento")]
    [Authorize(Policy = "Consultas")]
    public class DocumentoController : Controller
    {
        private readonly ILogger _logger;

        public DocumentoController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public IActionResult Manuales()
        {
            return View();
        }

        [HttpGet]
        [Route("Manual")]
        public IActionResult GetDocumento(string nombreDocumento)
        {
            try
            {
                var pathBase = Path.Combine("Documentos", "Manuales");
                var pathDocumento = Path.Combine(pathBase, nombreDocumento);
                var extension = Path.GetExtension(nombreDocumento);
                byte[] documento = null;
                if (!System.IO.File.Exists(pathDocumento))
                    throw new Exception("No se encontro el archivo");

                documento = System.IO.File.ReadAllBytes(pathDocumento);

                var MedyaTypeExtension = string.Empty;
                switch (extension)
                {
                    case ".pdf":
                        MedyaTypeExtension = System.Net.Mime.MediaTypeNames.Application.Pdf;
                        break;
                    case ".docx":
                        MedyaTypeExtension = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        break;
                    case ".json":
                        MedyaTypeExtension = System.Net.Mime.MediaTypeNames.Application.Json;
                        break;
                }

                return File(documento, MedyaTypeExtension, $"{nombreDocumento}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return NotFound();
            }
        }



    }
}
