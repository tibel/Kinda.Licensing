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

The issuer writes a PEM-encoded SubjectPublicKeyInfo public key (`public-key.pem`) and
`license.xml` to the chosen directory and refuses to overwrite either file. It licenses the
issuing machine's current processor count for seven days.
The demo generates a new private key each run and does **not** save it; it cannot reissue
licenses under the same public key. Choose a new directory for another run.

## Security

- Licenses use an enveloped XML Digital Signature (XMLDSIG) over the whole document, with
  RSA-SHA256, SHA-256 digests and canonical XML. Licenses signed with the previous custom
  format are incompatible and must be reissued.
- The library accepts caller-owned `RSA` keys. Protect private keys in memory and at rest; never
  distribute them to clients or commit them to source control. The host can keep private keys in an
  external KMS and pass the RSA handle to the library. `ValidateWithAnyPublicKey` supports rotation:
  retain trusted retiring public keys until leases they signed have expired.
- The library assembly is public-signed to provide a stable strong-name identity. Public signing
  does not authenticate the publisher or protect the assembly; use NuGet package signing when
  publisher authenticity is required.
- `LicenseOfflineLeaseRule` enforces a seven-day maximum lease by default. `LicenseRevocationRule`
  rejects IDs in a host-provided revocation snapshot; hosts must authenticate and refresh that
  snapshot when clients reconnect. Offline revocation cannot be immediate.
- For an optional online clock check, fetch time from a trusted HTTPS endpoint with
  `HttpDateTimeProvider.GetUtcNowAsync` and pass it to `LicenseSystemClockRule`. HTTP/network errors
  propagate; callers decide whether to deny validation or fall back to local time. This can deter
  casual clock changes, but is not a trusted clock against a user controlling the client.
- `License.Load` parses XML with DTD processing disabled and a one-million-character limit by
  default. Callers that parse XML themselves remain responsible for safe parsing and size limits.
- Signature validation detects modification of licensed data; it does not prevent a user from
  modifying a client application, bypassing validation or changing the system clock. Offline
  expiration is therefore a policy for an uncompromised verifier and clock, not a guarantee against
  a determined user with local control. Enforce valuable operations on a trusted server when
  resistance to local tampering is required.

## License

MIT; see [LICENSE](LICENSE).
