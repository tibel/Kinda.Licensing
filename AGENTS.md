# Agent instructions

- Preserve the XMLDSIG format, algorithms, and SubjectPublicKeyInfo PEM public-key format unless a breaking format change is explicitly requested; cover changes with interoperability tests.
- Build and test through `Kinda.Licensing.slnx`; TUnit requires Microsoft.Testing.Platform, selected by `global.json`.
