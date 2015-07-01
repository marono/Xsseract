using System;

namespace Xsseract.Droid
{
  public class DataConnectionException : ApplicationException
  {
    public DataConnectionException(string message)
      : base(message) {}

    public DataConnectionException(string message, Exception innerException)
      : base(message, innerException) {}
  }
}