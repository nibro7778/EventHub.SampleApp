namespace ConsumerApp.Domain;

public class TransientProcessingException : Exception
{
    public TransientProcessingException(string message, Exception? inner = null) : base(message, inner) { }
}

public class PermanentProcessingException : Exception
{
    public PermanentProcessingException(string message, Exception? inner = null) : base(message, inner) { }
}
