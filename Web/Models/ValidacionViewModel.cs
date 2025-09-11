// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Web.Models
{
    public class ValidacionViewModel
    {
        public static bool ValidarCedula(string identificacion)
        {
            if (string.IsNullOrEmpty(identificacion?.Trim()))
                return false;

            if (identificacion.Length == 10)
                return true;
            else
                return false;
        }

        public static bool ValidarRuc(string identificacion)
        {
            if (string.IsNullOrEmpty(identificacion?.Trim()))
                return false;

            if (identificacion.Length == 13)
                return true;
            else
                return false;
        }

        public static bool ValidarRucSectorPublico(string identificacion)
        {
            if (string.IsNullOrEmpty(identificacion?.Trim()))
                return false;

            if (identificacion.Length == 13)
            {
                if (int.Parse(identificacion[2].ToString()) == 6)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public static bool ValidarRucJuridico(string identificacion)
        {
            if (string.IsNullOrEmpty(identificacion?.Trim()))
                return false;

            if (identificacion.Length == 13)
            {
                if (int.Parse(identificacion[2].ToString()) == 9)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public static string ObtenerTipoIdentificacion(string identificacion)
        {
            if (string.IsNullOrEmpty(identificacion?.Trim()))
                return null;

            if (identificacion.Length == 10)
                return Dominio.Constantes.General.Cedula;
            else
            {
                if (identificacion.Length == 13)
                {
                    if (int.Parse(identificacion[2].ToString()) == 6)
                        return Dominio.Constantes.General.SectorPublico;
                    else if (int.Parse(identificacion[2].ToString()) == 9)
                        return Dominio.Constantes.General.RucJuridico;
                    else
                        return Dominio.Constantes.General.RucNatural;
                }
                else
                    return null;
            }
        }

        public static bool VerificaCedula(char[] validarCedula)
        {
            int aux = 0, par = 0, impar = 0, verifi;
            for (int i = 0; i < 9; i += 2)
            {
                aux = 2 * int.Parse(validarCedula[i].ToString());
                if (aux > 9)
                    aux -= 9;
                par += aux;
            }
            for (int i = 1; i < 9; i += 2)
            {
                impar += int.Parse(validarCedula[i].ToString());
            }

            aux = par + impar;
            if (aux % 10 != 0)
            {
                verifi = 10 - (aux % 10);
            }
            else
                verifi = 0;
            if (verifi == int.Parse(validarCedula[9].ToString()))
                return true;
            else
                return false;
        }

        public static bool VerificaPersonaJuridica(char[] validarCedula)
        {
            int aux = 0, prod, veri;
            veri = int.Parse(validarCedula[10].ToString()) + int.Parse(validarCedula[11].ToString()) + int.Parse(validarCedula[12].ToString());
            if (veri > 0)
            {
                int[] coeficiente = new int[9] { 4, 3, 2, 7, 6, 5, 4, 3, 2 };
                for (int i = 0; i < 9; i++)
                {
                    prod = int.Parse(validarCedula[i].ToString()) * coeficiente[i];
                    aux += prod;
                }
                if (aux % 11 == 0)
                {
                    veri = 0;
                }
                else if (aux % 11 == 1)
                {
                    return false;
                }
                else
                {
                    aux = aux % 11;
                    veri = 11 - aux;
                }

                if (veri == int.Parse(validarCedula[9].ToString()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool VerificaSectorPublico(char[] validarCedula)
        {
            int aux = 0, prod, veri;
            veri = int.Parse(validarCedula[9].ToString()) + int.Parse(validarCedula[10].ToString()) + int.Parse(validarCedula[11].ToString()) + int.Parse(validarCedula[12].ToString());
            if (veri > 0)
            {
                int[] coeficiente = new int[8] { 3, 2, 7, 6, 5, 4, 3, 2 };

                for (int i = 0; i < 8; i++)
                {
                    prod = int.Parse(validarCedula[i].ToString()) * coeficiente[i];
                    aux += prod;
                }

                if (aux % 11 == 0)
                {
                    veri = 0;
                }
                else if (aux % 11 == 1)
                {
                    return false;
                }
                else
                {
                    aux = aux % 11;
                    veri = 11 - aux;
                }

                if (veri == int.Parse(validarCedula[8].ToString()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
