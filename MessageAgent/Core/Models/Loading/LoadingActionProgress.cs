namespace MessageAgent.Core.Models.Loading
{
    internal sealed class LoadingActionProgress(LoadingActionState state) : IProgress<double>
    {
        public void Report(double value)
        {
            MainThread.BeginInvokeOnMainThread(() => state.ReportProgress(value));
        }
    }
}
