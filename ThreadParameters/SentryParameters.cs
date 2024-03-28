
namespace ThornNetworking.ThreadParameters
{
    public class SentryParameters : BaseParameters
    {
        public readonly CancellationTokenSource cancellationTokenSource;
        public SentryParameters() : this(new CancellationTokenSource()) { }
        public SentryParameters(CancellationTokenSource tokenSource) : base(tokenSource.Token) { cancellationTokenSource = tokenSource; }
        public SentryParameters(SentryParameters other) : base(other) { cancellationTokenSource = other.cancellationTokenSource; }
    }
}
