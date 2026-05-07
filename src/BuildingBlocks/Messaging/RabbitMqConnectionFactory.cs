// using RabbitMQ.Client;

// namespace BuildingBlocks.Messaging;


// /// <summary>
// /// Factory helper — creates a single RabbitMQ IConnection from configuration.
// /// Register as singleton in DI.
// /// </summary>
// public static class RabbitMqConnectionFactory
// {
//     public static async Task<IConnection> CreateConnectionAsync(
//         string hostName, int port = 5672,
//         string user = "guest", string password = "guest")
//     {
//         var factory = new ConnectionFactory
//         {
//             HostName                = hostName,
//             Port                    = port,
//             UserName                = user,
//             Password                = password,
//             AutomaticRecoveryEnabled = true,
//             NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
//         };
//         return await factory.CreateConnectionAsync();
//     }
// }