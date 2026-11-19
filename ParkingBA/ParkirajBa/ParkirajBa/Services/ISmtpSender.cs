using System.Net;
using System.Net.Mail;

namespace ParkirajBa.Services;

public interface ISmtpSender
{
    void Send(MailMessage mail, NetworkCredential credentials);
}
