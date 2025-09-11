using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Tipos
{
    public enum Predios : short
    {
        [Description("Desconocido")]
        Desconocido = 0,
        [Description("Quito")]
        Quito = 1,
        [Description("Cuenca")]
        Cuenca = 2,
        [Description("Santo Domingo")]
        SantoDomingo = 3,
        [Description("Rumiñahui")]
        Ruminahui = 4,
        [Description("Quinindé")]
        Quininde = 5,
        [Description("Latacunga")]
        Latacunga = 6,
        [Description("Manta")]
        Manta = 7,
        [Description("Ambato")]
        Ambato = 8,
        [Description("Ibarra")]
        Ibarra = 9,
        [Description("San Cristóbal")]
        SanCristobal = 10,
        [Description("Durán")]
        Duran = 11,
        [Description("Lago Agrio")]
        LagoAgrio = 12,
        [Description("Santa Rosa")]
        SantaRosa = 13,
        [Description("Sucúa")]
        Sucua = 14,
        [Description("Sígsig")]
        Sigsig = 15,
        [Description("Mejía")]
        Mejia = 16,
        [Description("Morona")]
        Morona = 17,
        [Description("Tena")]
        Tena = 18,
        [Description("Catamayo")]
        Catamayo = 19,
        [Description("Loja")]
        Loja = 20,
        [Description("Samborondón")]
        Samborondon = 21,
        [Description("Daule")]
        Daule = 22,
        [Description("Cayambe")]
        Cayambe = 23,
        [Description("Azogues")]
        Azogues = 24,
        [Description("Esmeraldas")]
        Esmeraldas = 25,
        [Description("Cotacachi")]
        Cotacachi = 26
    }
}
