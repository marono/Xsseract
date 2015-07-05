#region

using System;

#endregion

namespace Xsseract.Droid
{
  public class DataConnectionException : ApplicationException
  {
    #region .ctors

    public DataConnectionException(string message)
      : base(message) {}

    public DataConnectionException(string message, Exception innerException)
      : base(message, innerException) {}

    #endregion
  }
}
