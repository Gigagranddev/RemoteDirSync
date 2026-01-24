// RemoteDirSync.Bot/WebApiHost.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDirSync.Bot;

public static class WebApiHost
{
    public static async Task<IHost> StartAsync(
        string[] args,
        int port = 5000,
        CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder(args);

        var cert = CreateAdHocCertificate("CN=RemoteDirSyncBot", "192.168.1.10"); // LAN IP or hostname

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, port, listenOptions =>
            {
                listenOptions.UseHttps(cert); // no password string needed here
            });
        });

        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync(cancellationToken);
        return app;
    }

  // Creates a self-signed cert for localhost + optional LAN IP/hostname.
  private static X509Certificate2 CreateAdHocCertificate(string subjectName, string? lanHostOrIp)
  {
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    var distinguishedName = new X500DistinguishedName(subjectName);

    var request = new CertificateRequest(
        distinguishedName,
        ecdsa,
        HashAlgorithmName.SHA256);

    // Basic constraints: end-entity cert (not a CA)
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

    // In some environments you may want the key to be exportable; you can clone if needed:
    // return new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
    return cert;
  }
}