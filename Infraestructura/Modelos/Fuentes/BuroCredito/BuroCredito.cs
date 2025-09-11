// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace Infraestructura.Modelos.Fuentes.BuroCredito
{
    public class BuroCredito
    {
    }
    public class ErrorBuroCredito
    {
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public List<Parameter> Headers { get; set; }
        public string Error { get; set; }
        public string StatusDescription { get; set; }
        public string ResponseStatus { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
