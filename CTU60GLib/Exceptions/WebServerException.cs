using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CTU60GLib.Exceptions
{
    public class WebServerException :Exception
    {
        private HttpStatusCode status;
        public HttpStatusCode Status
        {
            get { return status; }
        }
        public WebServerException(string message = default, HttpStatusCode status = default):base(message) 
        {
            this.status = status;
        }
    }
}
