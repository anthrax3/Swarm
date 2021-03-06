using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swarm.Basic.Entity;

namespace Swarm.Core.Impl
{
    public class SwarmCluster : ISwarmCluster
    {
        private readonly SwarmOptions _options;
        private readonly ISharding _sharding;
        private readonly IClientStore _clientStore;
        private readonly ILogger _logger;

        public SwarmCluster(IOptions<SwarmOptions> options, ISharding sharding, IClientStore clientStore,
            ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _sharding = sharding;
            _clientStore = clientStore;
            _logger = loggerFactory.CreateLogger<SwarmCluster>();
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            // Clean all old client connect information
            await _clientStore.DisconnectAllClients();

            while (!cancellationToken.IsCancellationRequested)
            {
                var node = new Node
                {
                    ConnectionString = _options.QuartzConnectionString,
                    SchedInstanceId = _options.SchedInstanceId,
                    Provider = _options.Provider,
                    SchedName = _options.SchedName,
                    IsConnected = true,
                    TriggerTimes = 0
                };
                await _sharding.RegisterNode(node);
                await Task.Delay(TimeSpan.FromMilliseconds(5000), cancellationToken).ConfigureAwait(true);
                _logger.LogInformation("SSN heartbeat");
            }
        }

        public async Task Shutdown()
        {
            Console.WriteLine("DISCONNECTED");
            await _sharding.DisconnectNode(_options.SchedName, _options.SchedInstanceId);
        }
    }
}