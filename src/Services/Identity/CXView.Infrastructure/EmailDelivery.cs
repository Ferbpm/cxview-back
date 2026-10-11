using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using CXView.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CXView.Infrastructure;

public sealed class EmailOptions
{
    public string Provider { get; init; } = "Smtp";
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "CXView";
}

public sealed class SmtpOtpDelivery(IOptions<EmailOptions> options) : IOtpDelivery
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string destination, string code, CancellationToken ct)
    {
        var encodedCode = WebUtility.HtmlEncode(code);
        var plainText = $"Tu código de verificación de CXView es: {code}\n\nExpira en 5 minutos. Si no solicitaste este acceso, puedes ignorar el mensaje.";
        var html = $$"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Código de verificación CXView</title>
            </head>
            <body style="margin:0;padding:0;background:#f3f6fa;font-family:Arial,Helvetica,sans-serif;color:#172033;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#f3f6fa;padding:32px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 28px rgba(24,39,75,.10);">
                      <tr>
                        <td style="padding:28px 32px;background:#173b6c;color:#ffffff;">
                          <div style="font-size:13px;letter-spacing:1.6px;text-transform:uppercase;opacity:.78;">Seguridad ViewPlus</div>
                          <div style="margin-top:8px;font-size:26px;font-weight:700;line-height:1.2;">Verificación de acceso</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:34px 32px 30px;">
                          <p style="margin:0 0 12px;font-size:17px;line-height:1.55;">Hola,</p>
                          <p style="margin:0 0 26px;font-size:15px;line-height:1.65;color:#4a5568;">Usa el siguiente código para completar tu inicio de sesión en CXView:</p>
                          <div style="margin:0 auto 26px;padding:18px 20px;background:#edf4ff;border:1px solid #c9ddff;border-radius:12px;text-align:center;">
                            <div style="font-size:12px;letter-spacing:1.4px;text-transform:uppercase;color:#5f6f86;margin-bottom:8px;">Código de verificación</div>
                            <div style="font-size:36px;line-height:1;font-weight:700;letter-spacing:8px;color:#164f9c;">{{encodedCode}}</div>
                          </div>
                          <p style="margin:0 0 20px;text-align:center;font-size:14px;color:#4a5568;">Este código expira en <strong>5 minutos</strong>.</p>
                          <div style="padding:14px 16px;background:#fff8e6;border-left:4px solid #e5a100;border-radius:6px;font-size:13px;line-height:1.55;color:#66501c;">
                            Si no intentaste iniciar sesión, ignora este mensaje. No compartas este código con ninguna persona.
                          </div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:18px 32px;background:#f8fafc;border-top:1px solid #e7edf5;text-align:center;font-size:12px;line-height:1.5;color:#7a8799;">
                          Mensaje automático de Seguridad ViewPlus · Por favor, no respondas este correo.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = "Código de verificación CXView",
            Body = plainText,
            IsBodyHtml = false,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html));
        message.To.Add(new MailAddress(destination));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30_000
        };
        await client.SendMailAsync(message, ct);
    }
}

public sealed class DevelopmentOtpDelivery(ILogger<DevelopmentOtpDelivery> logger) : IOtpDelivery
{
    public Task SendAsync(string destination, string code, CancellationToken ct)
    {
        logger.LogWarning("OTP DE DESARROLLO para {Destination}: {OtpCode}", destination, code);
        return Task.CompletedTask;
    }
}
