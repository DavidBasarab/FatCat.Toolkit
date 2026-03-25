# FatCat.Toolkit

A comprehensive C# utility library and ASP.NET Core web framework for .NET 10.0. Provides common helpers, extension methods, dependency injection, data access, cryptography, caching, messaging, TCP communication, and a configurable web server with SignalR and JWT authentication.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Installation

```bash
# Core library
dotnet add package FatCat.Toolkit

# Web server (includes core)
dotnet add package FatCat.Toolkit.WebServer
```

## Packages

| Package | Description |
|---------|-------------|
| **FatCat.Toolkit** | Core utilities, extensions, DI, caching, crypto, data access, messaging, threading |
| **FatCat.Toolkit.WebServer** | ASP.NET Core web framework with authentication, SignalR, CORS, and static files |

---

## Quick Start

### Dependency Injection with SystemScope

FatCat.Toolkit uses Autofac for dependency injection via `SystemScope`, a global singleton container.

```csharp
// Initialize the container with your assemblies
var builder = new ContainerBuilder();

var assemblies = new List<Assembly>
{
    typeof(MyService).Assembly,
    typeof(Program).Assembly
};

SystemScope.Initialize(builder, assemblies, new ScopeOptions { SetLifetimeScope = true });

// Resolve services
var myService = SystemScope.Container.Resolve<IMyService>();

// Safe resolution when the service might not be registered
if (SystemScope.Container.TryResolve<IOptionalService>(out var service))
{
    service.DoWork();
}
```

Register custom Autofac modules for advanced scenarios:

```csharp
public class MyModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MyService>().As<IMyService>().SingleInstance();

        // Factory registration
        builder.Register<CacheManager>((scope) =>
        {
            var connection = scope.Resolve<IConnection>();
            return new CacheManager(connection);
        }).SingleInstance();

        // Open generic registration
        builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>));
    }
}
```

---

## Web Server

### Basic Web Application

```csharp
var settings = new ToolkitWebApplicationSettings
{
    Options = WebApplicationOptions.CommonOptions,
    ContainerAssemblies = new List<Assembly> { typeof(Program).Assembly }
};

ToolkitWebApplication.Run(settings);
```

### With Authentication and SignalR

```csharp
var settings = new ToolkitWebApplicationSettings
{
    Options = WebApplicationOptions.Authentication | WebApplicationOptions.SignalR | WebApplicationOptions.Cors,
    ContainerAssemblies = new List<Assembly> { typeof(Program).Assembly },
    SignalRPath = "/hub",
    CorsSevers = new List<string> { "https://myapp.com", "https://admin.myapp.com" },
    ToolkitTokenParameters = new ToolkitTokenParameters
    {
        Issuer = "MyApp",
        Audience = "MyAppUsers",
        SecretKey = "your-secret-key-at-least-32-characters-long"
    },
    OnWebApplicationStarted = () =>
    {
        Console.WriteLine("Server is running!");
    },
    OnOnClientDataBufferMessage = async (message, buffer) =>
    {
        // Handle SignalR messages from clients
        Console.WriteLine($"Received message type: {message.MessageType}");
        return "acknowledged";
    }
};

ToolkitWebApplication.Run(settings);
```

### Web Results in Controllers

```csharp
[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public WebResult GetUser(string id)
    {
        var user = FindUser(id);

        if (user == null) return WebResult.NotFound();

        return WebResult.Ok(user);
    }

    [HttpPost]
    public WebResult CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return WebResult.BadRequest("Name is required");

        var user = SaveUser(request);

        return WebResult.Ok(user);
    }
}
```

---

## Data Access

### MongoDB Repository

All repository types implement `IDataRepository<T>` with full CRUD and filtering.

```csharp
// 1. Define your data object
public class UserData : MongoObject
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2. Implement connection information
public class MyMongoConnection : IMongoConnectionInformation
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "myapp";
}

// 3. Use the repository (auto-registered via DataModule)
public class UserService(IMongoRepository<UserData> userRepository)
{
    public async Task<UserData> GetById(string id)
    {
        return await userRepository.Get(id);
    }

    public async Task<List<UserData>> GetActiveUsers()
    {
        return await userRepository.GetAllByFilter(u => u.Email != null);
    }

    public async Task<UserData> FindOrCreate(string email)
    {
        return await userRepository.GetFirstOrCreate(
            u => u.Email == email,
            new UserData { Email = email, Name = "New User", CreatedAt = DateTime.UtcNow }
        );
    }

    public async Task Save(UserData user)
    {
        await userRepository.Create(user);
    }

    public async Task SaveMany(List<UserData> users)
    {
        await userRepository.Create(users);
    }

    public async Task Remove(UserData user)
    {
        await userRepository.Delete(user);
    }
}
```

### File System Repository

Store objects as JSON files on disk:

```csharp
public class AppSettings : FileSystemDataObject
{
    public string Theme { get; set; }
    public int MaxRetries { get; set; }
}

// Use just like MongoDB — same IDataRepository<T> interface
public class SettingsService(IFileSystemRepository<AppSettings> settingsRepository)
{
    public async Task<AppSettings> Load()
    {
        return await settingsRepository.GetFirst();
    }

    public async Task Save(AppSettings settings)
    {
        await settingsRepository.Update(settings);
    }
}
```

---

## Caching

Thread-safe in-memory cache with optional expiration:

```csharp
public class ProductCache
{
    private readonly IFatCatCache<Product> cache = new FatCatCache<Product>();

    public void Add(Product product)
    {
        // Cache for 5 minutes
        cache.Add(product, TimeSpan.FromMinutes(5));
    }

    public void AddPermanent(Product product)
    {
        // No expiration
        cache.Add(product);
    }

    public Product Get(string cacheId)
    {
        // Returns null if expired or not found
        return cache.Get(cacheId);
    }

    public bool Exists(string cacheId)
    {
        return cache.InCache(cacheId);
    }

    public void Invalidate(string cacheId)
    {
        cache.Remove(cacheId);
    }

    public void Clear()
    {
        cache.Clear();
    }
}

// Items must implement ICacheItem
public class Product : ICacheItem
{
    public string CacheId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

---

## Messaging (Pub/Sub)

Decoupled async publish/subscribe messaging:

```csharp
// Define a message type
public class OrderPlacedMessage : Message
{
    public string OrderId { get; set; }
    public decimal Total { get; set; }
}

// Subscribe to messages
Messenger.Subscribe<OrderPlacedMessage>(async message =>
{
    Console.WriteLine($"Order {message.OrderId} placed for {message.Total:C}");
    await SendConfirmationEmail(message.OrderId);
});

// Publish a message — all subscribers are notified
Messenger.Send(new OrderPlacedMessage { OrderId = "ORD-123", Total = 99.99m });

// Unsubscribe when done (important to avoid memory leaks)
Messenger.Unsubscribe<OrderPlacedMessage>(handler);
```

---

## HTTP Client (WebCaller)

```csharp
var caller = new WebCaller(new Uri("https://api.example.com"), jsonOps, logger);

// Authentication
caller.UseBasicAuthorization("username", "password");
// or
caller.UserBearerToken("your-jwt-token");

// GET
var response = await caller.Get("/users");

if (response.IsSuccessful)
{
    var users = response.To<List<User>>();
}

// POST with custom timeout
var newUser = new { Name = "Alice", Email = "alice@example.com" };
var response = await caller.Post("/users", newUser, TimeSpan.FromSeconds(30));

// PUT
await caller.Put($"/users/{id}", updatedUser);

// DELETE
await caller.Delete($"/users/{id}");
```

---

## Cryptography

### AES-GCM Encryption

```csharp
var encryption = new FatCatAesEncryption();

var key = new byte[32]; // 256-bit key
var iv = new byte[12];  // 96-bit nonce (must be unique per encryption)

// Fill with cryptographically random bytes
RandomNumberGenerator.Fill(key);
RandomNumberGenerator.Fill(iv);

// Encrypt — returns ciphertext with 16-byte auth tag appended
byte[] plainData = Encoding.UTF8.GetBytes("sensitive information");
byte[] encrypted = await encryption.Encrypt(plainData, key, iv);

// Decrypt
byte[] decrypted = await encryption.Decrypt(encrypted, key, iv);
string original = Encoding.UTF8.GetString(decrypted);
```

### Password Hashing (Argon2id)

```csharp
var hashTools = new HashTools();

// Hash a password
byte[] salt = new byte[16];
RandomNumberGenerator.Fill(salt);
byte[] hash = hashTools.HashPassword("user-password", salt);

// Verify a password (constant-time comparison)
byte[] attemptHash = hashTools.HashPassword("user-input", salt);
bool isValid = hashTools.HashEquals(hash, attemptHash);
```

### SHA256 Hashing

```csharp
string hash = HashTools.CalculateHash("data to hash");
byte[] hashBytes = HashTools.CalculateHash(dataBytes);
```

---

## Extension Methods

### String Extensions

```csharp
// Null/empty checking
"hello".IsNullOrEmpty();        // false
"  ".IsNullOrEmpty();           // true (checks whitespace too)
"hello".IsNotNullOrEmpty();     // true

// Type conversions with safe defaults
"42".ToInt();                   // 42
"bad".ToInt();                  // 0 (default)
"true".ToBool();                // true
"3.14".ToDouble();              // 3.14

// Encoding
"hello".ToBase64Encoded();      // "aGVsbG8="
"aGVsbG8=".FromBase64Encoded(); // "hello"
"hello".ToByteArray();          // UTF-8 bytes
"hello".ToAsciiByteArray();     // ASCII bytes

// Text manipulation
"hello world".ToSlug();                  // "hello-world"
"hello world".FirstLetterToUpper();      // "Hello world"
"My:Bad*File".MakeSafeFileName();        // sanitized filename
"hello".FixedLength(10);                 // "hello     "
"  lots  of  spaces  ".RemoveAllWhitespace(); // "lotsofspaces"

// Splitting
"line1\nline2\nline3".SplitByLine();     // ["line1", "line2", "line3"]

// Hex parsing (for protocol work)
"0x02AB÷0x04".WithEmbeddedHexCodesToByteArray(); // parses hex codes to bytes
```

### Object Extensions

```csharp
// Deep clone any serializable object
var original = new MyComplexObject { Name = "test", Items = new List<int> { 1, 2, 3 } };
var clone = original.DeepCopy();
// clone is a completely independent copy
```

### Collection Extensions

```csharp
var list1 = new List<int> { 1, 2, 3 };
var list2 = new List<int> { 1, 2, 3 };

list1.ListsAreEqual(list2);                     // true (order matters by default)
list1.ListsAreEqual(list2, ignoreOrder: true);   // true (ignores order)
```

---

## Threading and Async Utilities

### Retry with Backoff

```csharp
var retry = new FatRetry();

// Retry up to 5 times with 2-second delay between attempts
bool success = await retry.Execute(
    async () => await CallUnreliableApi(),
    maxRetries: 5,
    delay: TimeSpan.FromSeconds(2)
);

// Synchronous version
bool success = retry.Execute(
    () => TryOperation(),
    maxRetries: 10,
    delay: TimeSpan.FromSeconds(5)
);
```

### Polling / Waiting

```csharp
var waiter = new FatWaiter();

// Wait for a condition to become true, polling every 100ms
await waiter.Wait(
    () => server.IsReady,
    TimeSpan.FromMilliseconds(100)
);

// With timeout
await waiter.Wait(
    () => server.IsReady,
    TimeSpan.FromMilliseconds(100),
    TimeSpan.FromSeconds(30)
);

// Async condition with cancellation
await waiter.Wait(
    async () => await CheckHealthEndpoint(),
    TimeSpan.FromSeconds(1),
    TimeSpan.FromMinutes(5),
    cancellationToken
);
```

### Background Threading

```csharp
// Fire-and-forget with exception handling
var thread = new Thread(logger);
thread.Run(async () =>
{
    await ProcessInBackground();
});

await thread.Sleep(TimeSpan.FromSeconds(5));
```

---

## TCP Communication

### TCP Client

```csharp
var client = new FatTcpClient();

client.TcpMessageReceivedEvent += (data) =>
{
    Console.WriteLine($"Received: {Encoding.UTF8.GetString(data)}");
};

client.Reconnect = true;
client.ReconnectDelay = TimeSpan.FromSeconds(5);

client.Connect("192.168.1.100", 9000, bufferSize: 4096);

client.Send("Hello server!");
client.Send(new byte[] { 0x01, 0x02, 0x03 });

// Cleanup
client.Disconnect();
```

### TCP Server

```csharp
var server = new OpenFatTcpServer();
server.Start(9000, Encoding.UTF8, bufferSize: 4096);

// Handle connections and messages via events

server.Stop();
```

---

## Utility Classes

### ApplicationTools

```csharp
// System information
var exePath = ApplicationTools.ExecutableFullPath;
var machineName = ApplicationTools.MachineName;
var mac = ApplicationTools.MacAddress;
bool inDocker = ApplicationTools.InContainer;

// Network
var ip = ApplicationTools.GetIPAddress();
var allIps = ApplicationTools.GetIPList();
ushort openPort = ApplicationTools.FindNextOpenPort(8000);
```

### Generator

```csharp
var id = Generator.NewId();           // Short unique ID
var guid = Generator.NewGuid();       // Standard GUID
var objectId = Generator.NewObjectId(); // MongoDB-style ObjectId
var randomStr = Generator.RandomString(16);
var randomNum = Generator.RandomNumber(1, 100);
var randomBytes = Generator.Bytes(32);

bool valid = Generator.IsValidObjectId("507f1f77bcf86cd799439011");
```

### FileSystemTools

Abstracted file I/O for testability (uses `System.IO.Abstractions`):

```csharp
var fsTools = new FileSystemTools(fileSystem);

// Async operations
await fsTools.WriteAllText("/data/config.json", jsonContent);
var content = await fsTools.ReadAllText("/data/config.json");
var bytes = await fsTools.ReadAllBytes("/data/image.png");
await fsTools.AppendToFile("/logs/app.log", "New log entry\n");

// Directory management
fsTools.EnsureDirectory("/data/output");
bool exists = fsTools.DirectoryExists("/data/output");
var files = fsTools.GetFiles("/data/output");
var filesWithMeta = fsTools.GetFilesWithMetaData("/data/output");

// File operations
fsTools.MoveFile("/tmp/upload.dat", "/data/upload.dat");
fsTools.DeleteFile("/tmp/upload.dat");
```

### FatResult

Generic result pattern for operations that can succeed or fail:

```csharp
public FatResult<User> GetUser(string id)
{
    var user = FindUser(id);

    if (user == null)
        return FatResult<User>.Failed("User not found");

    return FatResult<User>.Success(user);
}

// Usage
var result = GetUser("123");

if (result.IsSuccessful)
{
    Console.WriteLine(result.Data.Name);
}
else
{
    Console.WriteLine(result.Message);
}
```

### EqualObject

Base class providing automatic value-based equality using reflection:

```csharp
public class Address : EqualObject
{
    public string Street { get; set; }
    public string City { get; set; }

    [CompareExclude] // Excluded from equality comparison
    public DateTime LastUpdated { get; set; }
}

var a = new Address { Street = "123 Main", City = "Springfield" };
var b = new Address { Street = "123 Main", City = "Springfield" };

bool equal = a == b; // true — compares all fields except LastUpdated
```

### JSON Operations

```csharp
var jsonOps = new JsonOperations();

// Serialize / Deserialize
string json = jsonOps.Serialize(myObject, indented: true);
var obj = jsonOps.Deserialize<MyType>(json);

// Safe deserialization
if (jsonOps.TryDeserialize<MyType>(json, out var result))
{
    // use result
}

// Stream variants (for large payloads)
await jsonOps.SerializeToStreamAsync(myObject, stream);
var obj = await jsonOps.DeserializeFromStreamAsync<MyType>(stream);
```

---

## License

MIT
