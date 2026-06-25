# Fix: AES key & IV generation must use a CSPRNG (not `System.Random`)

**Severity: CRITICAL** — affects every AES-256 key and AES-GCM nonce produced by the toolkit.

This is a self-contained task. A coding agent should be able to implement, test, and verify it
from this document alone.

---

## Problem

`AesKeyGenerator` produces AES keys and IVs from `IGenerator.Bytes(...)`, which is backed by
`System.Random` — a **non-cryptographic** PRNG. Any AES-256 key and 96-bit GCM nonce minted by
this class is predictable, low-entropy, and correlated across calls.

### Evidence (current code)

`src/ToolKit/Cryptography/AesKeyGenerator.cs`
```csharp
public class AesKeyGenerator(IGenerator generator) : IAesKeyGenerator
{
    public byte[] CreateIv()
    {
        // AES-GCM standard 96-bit nonce (12 bytes)
        return generator.Bytes(12).ToArray();
    }

    public byte[] CreateKey(AesKeySize keySize)
    {
        return generator.Bytes((int)keySize / 8).ToArray();
    }
}
```

`src/ToolKit/Generator.cs`
```csharp
public IEnumerable<byte> Bytes(int length)
{
    return Faker.RandomBytes(length);   // FatCat.Fakes — a TEST-DATA generator
}
```

`FatCat.Fakes/Faker.cs`
```csharp
public static Random Random { get; } = new Random();   // System.Random, NOT a CSPRNG
public static byte[] RandomBytes(int length)
{
    var bytes = new byte[length];
    Random.NextBytes(bytes);            // single shared, sequential, low-entropy stream
    return bytes;
}
```

### Why this is critical
- `System.Random` is seeded from low-entropy state (~31 bits) and is fully predictable from its
  seed/internal state. AES-256 keys drawn from it do **not** have 256 bits of entropy.
- It is a single shared, sequential instance — keys and nonces across chunks/blocks are correlated.
- **AES-GCM is catastrophically broken on (key, nonce) reuse.** A non-CSPRNG nonce source removes
  the uniqueness guarantee GCM depends on.
- The strong AES-GCM cipher implementation (`FatCatAesEncryption`) is correct, but is keyed with
  weak material — so the cipher's strength is irrelevant.

---

## Required change

Generate key and IV bytes from `System.Security.Cryptography.RandomNumberGenerator` (a CSPRNG).
Remove the `IGenerator` dependency from `AesKeyGenerator` — it is no longer needed.

### `src/ToolKit/Cryptography/AesKeyGenerator.cs` (new contents)
```csharp
using System.Security.Cryptography;

namespace FatCat.Toolkit.Cryptography;

public interface IAesKeyGenerator
{
    public byte[] CreateIv();

    public byte[] CreateKey(AesKeySize keySize);
}

public class AesKeyGenerator : IAesKeyGenerator
{
    public byte[] CreateIv()
    {
        // AES-GCM standard 96-bit nonce (12 bytes), drawn from a cryptographically secure RNG
        return RandomNumberGenerator.GetBytes(12);
    }

    public byte[] CreateKey(AesKeySize keySize)
    {
        return RandomNumberGenerator.GetBytes((int)keySize / 8);
    }
}
```

### Notes
- `RandomNumberGenerator.GetBytes(int)` (static, .NET 6+) is the platform CSPRNG. It is supported on
  **Blazor WebAssembly** — the .NET WASM runtime maps it to the browser's `crypto.getRandomValues`,
  so the DocLokr WASM client continues to work.
- Removing the `IGenerator generator` constructor parameter is safe: Autofac resolves the
  parameterless constructor automatically. No module/registration change is required.
- Do **not** route this through `IGenerator.Bytes`. That method is a test-data helper and must not
  back any security-sensitive byte generation.

---

## Tests

Update `src/Tests.ToolKit/Cryptography/AesKeyGeneratorTests.cs`:

1. Remove any `IGenerator` fake/setup — the class no longer takes a dependency. Construct it directly:
   `var sut = new AesKeyGenerator();`
2. Keep/confirm length assertions:
   - `CreateIv()` returns **12** bytes.
   - `CreateKey(AesKeySize.Aes256)` returns **32** bytes; `Aes192` → 24; `Aes128` → 16.
3. Add an uniqueness/entropy smoke test: two successive `CreateKey`/`CreateIv` calls must **not** be
   equal (e.g. generate 100 keys, assert all distinct). This guards against a future regression back
   to a seeded/shared generator.

A round-trip test already exists for the cipher (`FatCatAesEncryptionTests`); confirm it still passes
with keys/IVs from the updated generator.

---

## Acceptance criteria
- [ ] `AesKeyGenerator` no longer references `IGenerator`, `Generator`, `Faker`, or `System.Random`.
- [ ] Key and IV bytes come from `RandomNumberGenerator`.
- [ ] `AesKeyGeneratorTests` updated and green; length + uniqueness assertions pass.
- [ ] `FatCatAesEncryptionTests` (GCM round-trip) still green.
- [ ] Solution builds for all target frameworks, including the Blazor/WASM consumer.

---

## Out of scope (handled in the consuming repo)
- **Key rotation / re-encryption of already-stored documents.** Any keys minted before this fix must
  be treated as compromised and rotated. That migration lives in the Fog/Brume repo
  (`C:\Code\Fog\tasks\todo\key_fix_brume`), not here.
- **`Generator.Bytes` / `Faker.RandomBytes` themselves.** They remain fine for non-security test data.
  If desired, add an XML-doc remark on `IGenerator.Bytes` warning it is **not** for cryptographic use,
  but that is optional and separate from this fix.
