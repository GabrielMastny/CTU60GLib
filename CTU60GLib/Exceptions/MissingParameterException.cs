using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib.Exceptions
{
    public class MissingParameterException : Exception
    {
        public MissingParameterException(string message):base(message)
        {
        }

    }
}
