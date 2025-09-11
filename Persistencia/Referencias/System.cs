// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace System
{
    public static class Extensiones
    {
        public static string RemoverCaracteresEspeciales(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var chars1 = "áéíóúàèìòùäëïöüâêîôûñÑÂÊÎÔÛÁÉÍÓÚÀÈÌÒÙÄËÏÖÜ".ToArray();
            var chars2 = "aeiouaeiouaeiouaeiounNAEIOUAEIOUAEIOUAEIOU".ToArray();
            for (int i = 0; i < chars1.Length; i++)
                str = str.Replace(chars1[i], chars2[i]);

            return Regex.Replace(str, "[^a-zA-Z -]+", "");
        }
    }
}
