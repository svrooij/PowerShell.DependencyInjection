using System;
using System.Collections.Generic;
using System.Text;

namespace Svrooij.PowerShell.DI.Generator;

internal class ThreadSynchronisation
{
    internal const string ThreadAffinitiveSynchronizationContext = @"
/*
Source: https://github.com/NTTLimitedRD/OctopusDeploy.Powershell/blob/7653993ffbf3ddfc7381e1196dbaa6fdf43cd982/OctopusDeploy.Powershell/ThreadAffinitiveSynchronizationContext.cs
Original License:
The MIT License (MIT)

Copyright (c) 2014 Dimension Data R&D

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */



using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Svrooij.PowerShell.DI
{
    /// <summary>
    /// A synchronisation context that runs all calls scheduled on it (via <see cref=""SynchronizationContext.Post""/>) on a single thread.
    /// </summary>
    /// <remarks>This part is taken from <see href=""https://github.com/NTTLimitedRD/OctopusDeploy.Powershell/blob/7653993ffbf3ddfc7381e1196dbaa6fdf43cd982/OctopusDeploy.Powershell/ThreadAffinitiveSynchronizationContext.cs"">OctopusDeploy.Powershell</see> licensed under <see href=""https://github.com/NTTLimitedRD/OctopusDeploy.Powershell/blob/7653993ffbf3ddfc7381e1196dbaa6fdf43cd982/LICENSE"">MIT</see>. And was then optimized using Github Copilot.</remarks>
    public sealed class ThreadAffinitiveSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>> _workItemQueue =
            new ConcurrentQueue<KeyValuePair<SendOrPostCallback, object>>();
        private readonly ManualResetEventSlim _workAvailable = new ManualResetEventSlim(false);
        private volatile bool _addingCompleted;
        private volatile bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _addingCompleted = true;
            _workAvailable.Set();
            _workAvailable.Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadAffinitiveSynchronizationContext));
        }

        private void RunMessagePump()
        {
            CheckDisposed();

            while (true)
            {
                KeyValuePair<SendOrPostCallback, object> workItem;
                while (_workItemQueue.TryDequeue(out workItem))
                {
                    workItem.Key(workItem.Value);
                }

                if (_addingCompleted)
                    break;

                _workAvailable.Wait();
                _workAvailable.Reset();
            }
        }

        private void TerminateMessagePump()
        {
            CheckDisposed();
            _addingCompleted = true;
            _workAvailable.Set();
        }

        public override void Post(SendOrPostCallback callback, object callbackState)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            CheckDisposed();

            if (_addingCompleted)
            {
                throw new InvalidOperationException(
                    ""Cannot enqueue the specified callback because the synchronization context's message pump has already been terminated.""
                );
            }

            _workItemQueue.Enqueue(new KeyValuePair<SendOrPostCallback, object>(callback, callbackState));
            _workAvailable.Set();
        }

        public static void RunSynchronized(Func<Task> asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            SynchronizationContext savedContext = Current;
            try
            {
                using (var synchronizationContext = new ThreadAffinitiveSynchronizationContext())
                {
                    SetSynchronizationContext(synchronizationContext);

                    Task rootOperationTask = asyncOperation();
                    if (rootOperationTask == null)
                        throw new InvalidOperationException(""The asynchronous operation delegate cannot return null."");

                    rootOperationTask.ContinueWith(
                        operationTask => synchronizationContext.TerminateMessagePump(),
                        TaskScheduler.Default
                    );

                    synchronizationContext.RunMessagePump();

                    try
                    {
                        rootOperationTask.GetAwaiter().GetResult();
                    }
                    catch (AggregateException eWaitForTask)
                    {
                        AggregateException flattenedAggregate = eWaitForTask.Flatten();
                        if (flattenedAggregate.InnerExceptions.Count != 1)
                            throw;

                        ExceptionDispatchInfo.Capture(flattenedAggregate.InnerExceptions[0]).Throw();
                    }
                }
            }
            finally
            {
                SetSynchronizationContext(savedContext);
            }
        }

        public static TResult RunSynchronized<TResult>(Func<Task<TResult>> asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            SynchronizationContext savedContext = Current;
            try
            {
                using (var synchronizationContext = new ThreadAffinitiveSynchronizationContext())
                {
                    SetSynchronizationContext(synchronizationContext);

                    Task<TResult> rootOperationTask = asyncOperation();
                    if (rootOperationTask == null)
                        throw new InvalidOperationException(""The asynchronous operation delegate cannot return null."");

                    rootOperationTask.ContinueWith(
                        operationTask => synchronizationContext.TerminateMessagePump(),
                        TaskScheduler.Default
                    );

                    synchronizationContext.RunMessagePump();

                    try
                    {
                        return rootOperationTask.GetAwaiter().GetResult();
                    }
                    catch (AggregateException eWaitForTask)
                    {
                        AggregateException flattenedAggregate = eWaitForTask.Flatten();
                        if (flattenedAggregate.InnerExceptions.Count != 1)
                            throw;

                        ExceptionDispatchInfo.Capture(flattenedAggregate.InnerExceptions[0]).Throw();
                        throw; // Never reached.
                    }
                }
            }
            finally
            {
                SetSynchronizationContext(savedContext);
            }
        }
    }
}";
}
