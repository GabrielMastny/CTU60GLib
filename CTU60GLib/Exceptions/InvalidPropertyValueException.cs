using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib.Exceptions
{
    class InvalidPropertyValueException : Exception
    {
        public string ExpectedVauleInfo { get; }
        public string CurrentValue { get; }
        public InvalidPropertyValueException(string expectedValueInfo,string currentValue)
        {
            ExpectedVauleInfo = expectedValueInfo;
            CurrentValue = currentValue;
        }
    }
}
