using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Dominio.Tipos
{
    public enum Roles : short
    {
        [Description("DESCONOCIDO")]
        Desconocido = 0,
        [Description("ADMINISTRADOR GARANCHECK")]
        Administrador = 1,
        [Description("OPERADOR GARANCHECK")]
        Operador = 2,
        [Description("ADMINISTRADOR CLIENTE")]
        AdministradorEmpresa = 3,
        [Description("OPERADOR CLIENTE")]
        OperadorEmpresa = 4,
        [Description("VENDEDOR CLIENTE")]
        VendedorEmpresa = 5,
        [Description("CONTACTABILIDAD CLIENTE")]
        ContactabilidadEmpresa = 6,
        [Description("ADMINISTRADOR COOPROGRESO")]
        AdministradorCooprogreso = 7,
        [Description("REPORTERIA")]
        Reporteria = 8,
    }
}
