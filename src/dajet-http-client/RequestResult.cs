namespace DaJet.Http.Client
{
    public sealed class RequestResult
    {
        internal RequestResult(bool success)
        {
            Success = success;
        }
        internal RequestResult(bool success, in string message) : this(success)
        {
            Message = message;
        }
        public bool Success { get; }
        public string Message { get; } = string.Empty;
    }

    public sealed class RequestResult<T>
    {
        internal RequestResult(T result)
        {
            Success = true;
            Result = result;
        }
        internal RequestResult(in string error)
        {
            Message = error;
        }
        public T Result { get; } = default;
        public bool Success { get; }
        public string Message { get; } = string.Empty;
    }
}