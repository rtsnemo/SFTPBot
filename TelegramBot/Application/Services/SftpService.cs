using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace TelegramBot.Application.Services
{
    public class SftpService
    {
        private readonly ILogger<SftpService> _logger;

        public SftpService(ILogger<SftpService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Uploads a file to an SFTP server.
        /// </summary>
        public async Task<bool> UploadFileAsync(string host, int port, string username, string password, string remotePath, string fileContent)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to SFTP server {Host}:{Port} as user {Username}", host, port, username);
                using var client = new SftpClient(host, port, username, password);
                client.Connect();
                _logger.LogInformation("Connected to SFTP server {Host}:{Port}", host, port);

                // Determine remote directory
                string remoteDirectory = Path.GetDirectoryName(remotePath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(remoteDirectory) && !client.Exists(remoteDirectory))
                {
                    client.CreateDirectory(remoteDirectory);
                }

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
                _logger.LogInformation("Starting file upload to {RemotePath}", remotePath);
                client.UploadFile(stream, remotePath, true);
                _logger.LogInformation("File uploaded to {RemotePath}", remotePath);

                client.Disconnect();
                _logger.LogInformation("Disconnected from SFTP server {Host}:{Port}", host, port);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file via SFTP");
                return await Task.FromResult(false);
            }
        }
    }
}
