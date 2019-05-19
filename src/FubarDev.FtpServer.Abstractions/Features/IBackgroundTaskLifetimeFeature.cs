// <copyright file="IBackgroundTaskLifetimeFeature.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.Features
{
    /// <summary>
    /// Feature for background tasks (abortable commands).
    /// </summary>
    public interface IBackgroundTaskLifetimeFeature
    {
        /// <summary>
        /// Gets the command that gets run in the background.
        /// </summary>
        [NotNull]
        FtpCommand Command { get; }

        /// <summary>
        /// Gets the FTP command handler.
        /// </summary>
        [NotNull]
        IFtpCommandBase Handler { get; }

        /// <summary>
        /// Gets the abortable task.
        /// </summary>
        [NotNull]
        [ItemCanBeNull]
        Task<IFtpResponse> Task { get; }

        /// <summary>
        /// Aborts the command that is run in the background.
        /// </summary>
        void Abort();
    }
}
