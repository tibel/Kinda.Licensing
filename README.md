# Kinda.Licensing

Kinda.Licensing issues and validates signed XML software licenses. An issuer signs license
criteria using an RSA private key. A client verifies the signature with the public key and
applies validation rules, such as expiration or an application-specific core limit.

The library and demos target **.NET 10**. The library is marked AOT-compatible and enables
the .NET trimming and Native AOT analyzers. The test suite uses TUnit.

## Build and test

Install the .NET 10 SDK or later, then run:

```powershell
dotnet build Kinda.Licensing.slnx -c Release
dotnet test Kinda.Licensing.slnx -c Release
```

`global.json` selects Microsoft.Testing.Platform for `dotnet test`; it does not pin the SDK.

## Demo

The following PowerShell commands create and validate a license on the same machine:

```powershell
$server = Join-Path src Kinda.Licensing.Demo.ServerApp
$client = Join-Path src Kinda.Licensing.Demo.ClientApp
dotnet run --project $server -- demo-license
dotnet run --project $client -- demo-license
```

The issuer writes `public-key.xml` and `license.xml` to the chosen directory and refuses to
overwrite either file. It licenses the issuing machine's current processor count for 30 days.
The demo generates a new private key each run and does **not** save it; it cannot reissue
licenses under the same public key. Choose a new directory for another run.

## Security

- The .NET 10 release drops .NET Standard 2.0 and .NET Core 2.1 support.
- Signatures use a library-specific XML format with RSA PKCS#1 v1.5 and SHA-256, not XML
  Digital Signature. Changing the XML serialization can invalidate signatures.
- Private keys are returned as plaintext XML strings. Keep them out of source control, logs,
  client applications and unsecured storage. Production issuers need protected key storage,
  controlled access and a key rotation strategy.
- Signature validation detects modification of licensed data; it does not prevent a user from
  modifying a client application, bypassing validation or changing the system clock.
- Treat XML license input as untrusted and impose appropriate input size and parsing limits.

## License

MIT; see [LICENSE](LICENSE).
