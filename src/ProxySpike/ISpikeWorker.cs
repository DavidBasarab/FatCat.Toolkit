namespace ProxySpike;

public interface ISpikeWorker<T>
	where T : class
{
	public Task DoWork(T options);
}
