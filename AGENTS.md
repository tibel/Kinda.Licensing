# Agent instructions

- Preserve the XMLDSIG format, algorithms, and SubjectPublicKeyInfo PEM public-key format unless a breaking format change is explicitly requested; cover changes with interoperability tests.
- Preserve `Kinda.Licensing.Public.snk`; changing it changes the assembly identity. Keep only the public key in the repository.
- Build and test through `Kinda.Licensing.slnx`; TUnit requires Microsoft.Testing.Platform, selected by `global.json`.
