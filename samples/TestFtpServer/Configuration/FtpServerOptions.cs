// <copyright file="FtpServerOptions.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

namespace TestFtpServer.Configuration
{
    /// <summary>
    /// Gets or sets server options.
    /// </summary>
    public class FtpServerOptions
    {
        /// <summary>
        /// Gets or sets the FTP server address.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Gets or sets the PASV options.
        /// </summary>
        public FtpServerPasvOptions Pasv { get; set; } = new FtpServerPasvOptions();
    }
}
