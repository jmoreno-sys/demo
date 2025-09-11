// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Infraestructura.Servicios
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage, string template = "generic");
        Task SendEmailAsync(string email, string subject, string htmlMessage, string displayName = null, Dictionary<string, object> replacements = null, Dictionary<string, byte[]> attachments = null, string bcc = "", bool thread = true, bool replaceInvalidChards = false, string template = "generic", bool headerImage = true);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage, string template = "generic")
        {
            if (!_configuration.GetValue<bool>("SmtpSettings:Enabled")) return;
            await SendEmailFromTemplateAsync(email.ToLower(), subject, template, htmlMessage);
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage, string displayName = null, Dictionary<string, object> replacements = null, Dictionary<string, byte[]> attachments = null, string bcc = "", bool thread = true, bool replaceInvalidChards = false, string template = "generic", bool headerImage = true)
        {
            if (!_configuration.GetValue<bool>("SmtpSettings:Enabled")) return;
            await SendEmailFromTemplateAsync(email.ToLower(), subject, template, displayName, htmlMessage, replacements, attachments, bcc, thread, replaceInvalidChards, headerImage);
        }

        public async Task<bool> SendEmailFromTemplateAsync(string email, string subject, string template = "generic", string displayName = "", string htmlMessage = "", Dictionary<string, object> replacements = null, Dictionary<string, byte[]> attachments = null, string bcc = "", bool thread = true, bool replaceInvalidChards = false, bool headerImage = true)
        {
            if (!_configuration.GetValue<bool>("SmtpSettings:Enabled")) return false;
            var server = _configuration.GetValue<string>("SmtpSettings:Server");
            var port = _configuration.GetValue<int>("SmtpSettings:Port");
            var enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl");
            var useDefaultCredentials = _configuration.GetValue<bool>("SmtpSettings:UseDefaultCredentials");
            var username = _configuration.GetValue<string>("SmtpSettings:Username");
            var password = _configuration.GetValue<string>("SmtpSettings:Password");
            var @from = _configuration.GetValue<string>("SmtpSettings:From");
            var name = _configuration.GetValue<string>("SmtpSettings:Name");
            var bccList = bcc.Split(';').Where(m => !string.IsNullOrWhiteSpace(m)).ToList();

            var client = new SmtpClient(server, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = useDefaultCredentials
            };
            if (!client.UseDefaultCredentials)
                client.Credentials = new NetworkCredential(username, password);

            var sender = new MailAddress(@from, name, Encoding.UTF8);

            MailMessage message = null;
            MailAddress target = null;
            if (bccList.Any())
            {
                target = new MailAddress(@from);
                if (email != @from && !string.IsNullOrWhiteSpace(email))
                    bccList.Insert(0, email);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(email))
                    email = @from;
                target = new MailAddress(email, displayName, Encoding.UTF8);
            }
            message = await BuidTemplateAsync(template, sender, target, subject, htmlMessage, replacements, attachments, headerImage);
            if (bccList.Any())
                foreach (var item in bccList.Distinct())
                    message.Bcc.Add(item);

            _logger.LogInformation($"Correo [{message.Subject}] enviando a {message.To}");
            var timeOut = 0;
            timeOut = new Random().Next(0, 4);
            if (timeOut == 0) timeOut = 1;
            if (timeOut == 4) timeOut = 3;

            var replacementFunction = new Func<string, string>(m =>
            {
                var result = m;
                //TRANSFORMACION DE CARACTERES NO PERMITIDO
                var ci = "áéíóúñÁÉÍÓÚÑ'".ToCharArray();
                var cv = "aeiounAEIOUN ".ToCharArray();
                for (int i = 0; i < ci.Length; i++)
                    result = m.Replace(ci[i], cv[i]);
                return result;
            });

            if (thread)
            {
                var t1 = new Thread(async () =>
                {
                    try
                    {
                        Thread.Sleep(timeOut * 1000);
                        if (replaceInvalidChards) message.Subject = replacementFunction(message.Subject);
#if !DEBUG
                        await client.SendMailAsync(message);
#else
                        await Task.Yield();
#endif
                        _logger.LogInformation($"Correo [{message.Subject}] enviado a {message.To}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Correo [{message.Subject}] ocurrió un problema al enviar a {message.To}");
                        _logger.LogError(ex, ex.Message);
                    }
                });
                t1.Start();

                return true;
            }

            try
            {
                Thread.Sleep(timeOut * 1000);
                if (replaceInvalidChards) message.Subject = replacementFunction(message.Subject);
#if !DEBUG
                await client.SendMailAsync(message);
#endif
                _logger.LogInformation($"Correo [{message.Subject}] enviado a {message.To}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Correo [{message.Subject}] ocurrió un problema al enviar a {message.To}");
                _logger.LogError(ex, ex.Message);
            }

            return false;
        }

        public async Task<MailMessage> BuidTemplateAsync(string template, MailAddress sender, MailAddress target, string title, string content, Dictionary<string, object> replacements = null, Dictionary<string, byte[]> attachments = null, bool headerImage = true)
        {
            var templatePath = Path.Combine("wwwroot", "templates");
            var fileName = Path.Combine(templatePath, $"{template}.html");
            var templateContent = await File.ReadAllTextAsync(fileName);

            templateContent = templateContent.Replace("{CONTENT}", content);
            templateContent = templateContent.Replace("{TITLE}", title);
            templateContent = templateContent.Replace("{FULLNAME}", target.DisplayName);
            templateContent = templateContent.Replace("{EMAIL}", target.Address);

            if (replacements != null && replacements.Any())
            {
                foreach (var item in replacements)
                {
                    var value = string.Empty;
                    try
                    {
                        if (item.Value != null)
                        {
                            if (item.Value is DateTime)
                                value = ((DateTime)item.Value).ToString("dd/MM/yyyy HH:mm:ss");
                            else if (item.Value is decimal)
                                value = ((decimal)item.Value).ToString("#.##");
                            else if (item.Value is float)
                                value = ((float)item.Value).ToString("#.##");
                            else
                                value = item.Value.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    templateContent = templateContent.Replace(item.Key, value);
                }
            }

            var html = AlternateView.CreateAlternateViewFromString(templateContent, null, MediaTypeNames.Text.Html);
            var filePath = Path.Combine("wwwroot", "images", "cabecera.png");
            var message = new MailMessage(sender, target)
            {
                IsBodyHtml = true,
                Subject = title,
                SubjectEncoding = System.Text.Encoding.UTF8,
            };
            message.AlternateViews.Add(html);

            if (headerImage)
                if (File.Exists(filePath))
                {
                    var inline = new LinkedResource(filePath, MediaTypeNames.Image.Jpeg) { ContentId = "header" };
                    html.LinkedResources.Add(inline);
                }

#if DEBUG
            try
            {
                var mailPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath, "tests");
                if (!Directory.Exists(mailPath))
                    Directory.CreateDirectory(mailPath);

                var htmlFile = Path.Combine(mailPath, DateTime.Now.Ticks.ToString() + ".html");
                File.WriteAllText(htmlFile, templateContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
#endif

            if (attachments != null)
                foreach (var item in attachments)
                    if (item.Value != null)
                        message.Attachments.Add(new Attachment(new MemoryStream(item.Value), item.Key));

            return message;
        }
    }
}
