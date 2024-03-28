namespace ThornNetworking.ThreadParameters
{
    public class BaseParameters : IDisposable
    {
        public readonly CancellationToken token;
        public BaseParameters(CancellationToken _token) { token = _token; }
        public BaseParameters(BaseParameters other) { token = other.token; }

        public void Dispose() {}
    }
}
