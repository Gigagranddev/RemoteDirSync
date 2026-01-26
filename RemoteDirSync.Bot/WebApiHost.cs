using RemoteDirSync.Bot.Controllers.DTOs;
using RemoteDirSync.Bot.Jobs;
using RemoteDirSync.Bot.Services;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RemoteDirSync.Bot
{
  public static class WebApiHost
  {
    public static async Task<IHost> StartAsync(
        string[] args,
        int port = 5000,
        CancellationToken ct = default)
    {
      var _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
              web.UseKestrel()
                 .UseUrls($"http://0.0.0.0:{port}")
                 .ConfigureServices(services =>
                 {
                   services.AddControllers();
                   services.AddEndpointsApiExplorer();
                   services.AddSwaggerGen();

                   services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
                   services.AddSingleton<DirScannerService>();
                   services.AddHostedService<BackgroundJobWorker>();
                   services.AddHttpClient("fileTransferClient")
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                      ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                 })
                 .Configure(app =>
                 {
                   app.UseRouting();
                   app.UseSwagger();
                   app.UseSwaggerUI();

                   app.UseEndpoints(endpoints =>
                   {
                     endpoints.MapControllers();
                     endpoints.MapGet("/Health", () => Results.Ok("ok"));
                   });
                 });
            })
            .Build();

      await _host.StartAsync(ct);
      return _host;
    }

    // Creates a self-signed cert for localhost + given LAN host/IP
    private static X509Certificate2 CreateAdHocCertificate(string subjectName, string? lanHostOrIp)
    {
      using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

      var distinguishedName = new X500DistinguishedName(subjectName);

      var request = new CertificateRequest(
          distinguishedName,
          ecdsa,
          HashAlgorithmName.SHA256);

      // End-entity cert (not a CA)
      request.CertificateExtensions.Add(
          new X509BasicConstraintsExtension(false, false, 0, false));

      // Key usage: digital signature, key encipherment
      request.CertificateExtensions.Add(
          new X509KeyUsageExtension(
              X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
              critical: false));

      // Enhanced key usage: server auth
      var eku = new OidCollection
      {
        new Oid("1.3.6.1.5.5.7.3.1") // Server Authentication
      };
      request.CertificateExtensions.Add(
          new X509EnhancedKeyUsageExtension(eku, critical: false));

      // Subject Alternative Name: localhost + optional LAN IP/DNS
      var sanBuilder = new SubjectAlternativeNameBuilder();
      sanBuilder.AddDnsName("localhost");
      sanBuilder.AddDnsName("127.0.0.1");

      if (!string.IsNullOrWhiteSpace(lanHostOrIp))
      {
        if (IPAddress.TryParse(lanHostOrIp, out var ip))
        {
          sanBuilder.AddIpAddress(ip);
        }
        else
        {
          sanBuilder.AddDnsName(lanHostOrIp);
        }
      }

      request.CertificateExtensions.Add(sanBuilder.Build());

      var now = DateTimeOffset.UtcNow;
      var cert = request.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(1));

      return cert;
    }
  }
}
