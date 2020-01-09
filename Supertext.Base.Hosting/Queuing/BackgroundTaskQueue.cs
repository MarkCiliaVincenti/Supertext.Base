﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Supertext.Base.BackgroundTasks;
using Supertext.Base.Common;

namespace Supertext.Base.Hosting.Queuing
{
    internal class BackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
    {
        private readonly ConcurrentQueue<Func<ILifetimeScope, CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<ILifetimeScope, CancellationToken, Task>>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private volatile bool _taskPending;

        public void QueueBackgroundWorkItem(Func<ILifetimeScope, CancellationToken, Task> workItem)
        {
            Validate.NotNull(workItem);
            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<ILifetimeScope, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            _workItems.TryDequeue(out var workItem);
            _taskPending = true;
            return workItem;
        }

        public void WorkItemFinished()
        {
            _taskPending = false;
        }

        public bool IsQueueEmpty()
        {
            return _workItems.IsEmpty && !_taskPending;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _signal?.Dispose();
            }
        }
    }
}