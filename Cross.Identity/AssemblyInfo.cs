[assembly: InternalsVisibleTo("Cross.Identity.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // To create a mock for ILogger<UserService> where UserService is not accessible (likely internal) to the dynamic proxy generator used by Moq.
