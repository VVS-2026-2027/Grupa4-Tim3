using System.Net;
using System.Net.Mail;

namespace ParkirajBa.Services;

public class SmtpSender : ISmtpSender
{
    public void Send(MailMessage mail, NetworkCredential credentials)
    {
        using var smtp = new SmtpClient("smtp.gmail.com", 587);
        smtp.Credentials = credentials;
        smtp.EnableSsl = true;
        smtp.Send(mail);
    }
}
