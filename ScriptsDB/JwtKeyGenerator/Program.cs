using System.Security.Cryptography;

var outputDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(
        Environment.CurrentDirectory,
        "TicketsHex.API",
        "Certificados"));

Directory.CreateDirectory(outputDirectory);

using var rsa = RSA.Create(3072);
File.WriteAllText(
    Path.Combine(outputDirectory, "jwt-private.pem"),
    rsa.ExportPkcs8PrivateKeyPem());
File.WriteAllText(
    Path.Combine(outputDirectory, "jwt-public.pem"),
    rsa.ExportSubjectPublicKeyInfoPem());

Console.WriteLine($"Claves JWT RS256 generadas en: {outputDirectory}");
Console.WriteLine("La clave privada está excluida de Git. Protégela como secreto en producción.");
