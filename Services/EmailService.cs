using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace QuMailClient
{
    public class EmailService
    {
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;

        public async Task SendSecureEmailAsync(string recipient, string subject, string secureBody, string keyId, string userEmail, string appPassword)
        {
            // 1. Validate inputs before starting network operations
            if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(appPassword))
                throw new ArgumentException("Configuration Missing: User Email or App Password not set.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("QuMail Secure Terminal", userEmail));
            message.To.Add(new MailboxAddress("", recipient));
            message.Subject = "SECURE_PACKET: " + subject;

            // Professional formatting for the secure payload
            string finalBody = $"--- BEGIN QUMAIL SECURE PACKET ---\n" +
                               $"TIMESTAMP: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                               $"KEY_ID: {keyId}\n" +
                               $"DATA: {secureBody}\n" +
                               $"--- END QUMAIL SECURE PACKET ---";

            message.Body = new TextPart("plain") { Text = finalBody };

            using (var client = new SmtpClient())
            {
                try
                {
                    // Adding a timeout ensures the UI doesn't freeze forever on bad connections
                    client.Timeout = 10000;

                    await client.ConnectAsync(SmtpServer, SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                    // Specific handling for Authentication
                    await client.AuthenticateAsync(userEmail, appPassword);

                    await client.SendAsync(message);
                }
                catch (MailKit.Security.AuthenticationException ex)
                {
                    throw new Exception("Authentication Failed: Please check your Gmail App Password.", ex);
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    throw new Exception("Network Error: Could not reach SMTP server. Check your internet connection.", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Critical Error: {ex.Message}", ex);
                }
                finally
                {
                    // Ensure we always disconnect gracefully
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true);
                    }
                }
            }
        }
    }
}