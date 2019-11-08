// <copyright file="FtpConnectionIdleCheck.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using FubarDev.FtpServer.Events;

using Microsoft.Extensions.Options;

namespace FubarDev.FtpServer.ConnectionChecks
{
    /// <summary>
    /// An activity-based keep-alive detection.
    /// </summary>
    public class FtpConnectionIdleCheck : IFtpConnectionCheck, IDisposable
    {
        /// <summary>
        /// The lock to be acquired when the timeout information gets set or read.
        /// </summary>
        private readonly object _inactivityTimeoutLock = new object();

        /// <summary>
        /// The timeout for the detection of inactivity.
        /// </summary>
        private readonly TimeSpan _inactivityTimeout;

        /// <summary>
        /// Indicator if a data transfer is ongoing.
        /// </summary>
        private readonly HashSet<string> _activeDataTransfers = new HashSet<string>();

        private readonly IDisposable? _subscription;

        /// <summary>
        /// The timestamp of the last activity on the connection.
        /// </summary>
        private DateTime _utcLastActiveTime;

        /// <summary>
        /// The timestamp where the connection expires.
        /// </summary>
        private DateTime? _expirationTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpConnectionIdleCheck"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The FTP connection accessor.</param>
        /// <param name="options">FTP connection options.</param>
        public FtpConnectionIdleCheck(
            IFtpConnectionAccessor connectionAccessor,
            IOptions<FtpConnectionOptions> options)
        {
            var connection = connectionAccessor.FtpConnection;
            _inactivityTimeout = options.Value.InactivityTimeout ?? TimeSpan.MaxValue;
            UpdateLastActiveTime();

            if (connection is IObservable<IFtpConnectionEvent> observable)
            {
                _subscription = observable.Subscribe(new EventObserver(this));
            }
            else
            {
                _subscription = null;
            }
        }

        /// <inheritdoc />
        public FtpConnectionCheckResult Check(FtpConnectionCheckContext context)
        {
            FtpConnectionCheckResult result;
            if (_subscription == null)
            {
                result = new FtpConnectionCheckResult(true);
            }
            else
            {
                lock (_inactivityTimeoutLock)
                {
                    if (_expirationTimeout == null)
                    {
                        result = new FtpConnectionCheckResult(true);
                    }
                    else if (_activeDataTransfers.Count != 0)
                    {
                        UpdateLastActiveTime();
                        result = new FtpConnectionCheckResult(true);
                    }
                    else if (DateTime.UtcNow <= _expirationTimeout.Value)
                    {
                        result = new FtpConnectionCheckResult(true);
                    }
                    else
                    {
                        result = new FtpConnectionCheckResult(false);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _subscription?.Dispose();
        }

        private void UpdateLastActiveTime()
        {
            _utcLastActiveTime = DateTime.UtcNow;
            _expirationTimeout = (_inactivityTimeout == TimeSpan.MaxValue)
                ? (DateTime?)null
                : _utcLastActiveTime.Add(_inactivityTimeout);
        }

        private class EventObserver : IObserver<IFtpConnectionEvent>
        {
            private readonly FtpConnectionIdleCheck _check;

            public EventObserver(FtpConnectionIdleCheck check)
            {
                _check = check;
            }

            /// <inheritdoc />
            public void OnCompleted()
            {
                // Ignore, connection was closed.
            }

            /// <inheritdoc />
            public void OnError(Exception error)
            {
                // Ignore
            }

            /// <inheritdoc />
            public void OnNext(IFtpConnectionEvent value)
            {
                switch (value)
                {
                    case FtpConnectionCommandReceivedEvent _:
                        lock (_check._inactivityTimeoutLock)
                        {
                            _check.UpdateLastActiveTime();
                        }

                        break;

                    case FtpConnectionDataTransferStartedEvent e:
                        lock (_check._inactivityTimeoutLock)
                        {
                            _check.UpdateLastActiveTime();
                            _check._activeDataTransfers.Add(e.TransferId);
                        }

                        break;

                    case FtpConnectionDataTransferStoppedEvent e:
                        lock (_check._inactivityTimeoutLock)
                        {
                            _check.UpdateLastActiveTime();
                            _check._activeDataTransfers.Remove(e.TransferId);
                        }

                        break;
                }
            }
        }
    }
}
