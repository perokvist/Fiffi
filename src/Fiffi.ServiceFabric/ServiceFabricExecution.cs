using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class ServiceFabricExecution
	{
		public static async Task RunAsync(Func<CancellationToken, Task> f, CancellationToken cancellationToken, ILogger logger)
		{
			try
			{
				await f(cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException e)
			{
				logger.LogError(e, "RunAsync canceled");
				throw;
			}
			catch (Exception e) when (IsCancellation(e))
			{
				logger.LogError(e, "Cancellation Exception");
				cancellationToken.ThrowIfCancellationRequested();
				throw;
			}
			catch (Exception e)
			{
				logger.LogError(e, "Unhandled Exception");
				throw;
			}
		}

		public static async Task WhileAsync(
			Func<CancellationToken, Task> f, string functionName, CancellationToken cancellationToken, ILogger logger)
		{
			logger.LogInformation($"Run {functionName}");
			var retryDelay = TimeSpan.FromMinutes(1);

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					await f(cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();
				}
				catch (Exception e) when (IsCancellation(e))
				{
					logger.LogInformation(e, $"Cancellation Exception in {functionName}");
					cancellationToken.ThrowIfCancellationRequested();
				}
				catch (FabricTransientException e)
				{
				    logger.LogInformation(e, $"FabricTransientException in {functionName}");
				}
				catch (FabricNotPrimaryException e)
				{
					logger.LogInformation(e, "Service fabric is not primary");
					return;
				}
				catch (FabricException e)
				{
					logger.LogInformation(e, $"Fabric Exception in {functionName}");
					throw;
				}
				catch (Exception e)
				{
					logger.LogError(e, $"Application Exception in {functionName}");
				}

				await Task.Delay(retryDelay, cancellationToken);
				logger.LogInformation($"Retrying task {functionName}");
			}
		}

		static readonly HashSet<Type> CancellationExceptions = new HashSet<Type>
		{
			typeof(OperationCanceledException),
			typeof(TaskCanceledException),
			typeof(TimeoutException)
		};

		static bool IsCancellation(Exception exception)
		{
			if (exception is AggregateException aggregateException)
			{
				return aggregateException.InnerExceptions.Any(IsCancellation);
			}

			return CancellationExceptions.Contains(exception.GetType());
		}
	}
}
