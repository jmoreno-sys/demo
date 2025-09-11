using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Areas.Identidad.Models
{
    public class CuentaViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "El {0} debe tener al menos {2} caracteres de longitud.", MinimumLength = 4)]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Clave { get; set; }
    }


    public class CambioClaveViewModel
    {
        public string ClaveAnterior { get; set; }

        public string Clave { get; set; }

        public string ConfirmaClave { get; set; }
    }
}
