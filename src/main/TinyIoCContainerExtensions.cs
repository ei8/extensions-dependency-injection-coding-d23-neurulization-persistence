using ei8.Cortex.Coding;
using ei8.Cortex.Coding.d23.neurULization;
using ei8.Cortex.Coding.d23.neurULization.Persistence;
using ei8.Cortex.Coding.Persistence;
using ei8.Cortex.IdentityAccess.Client.Out;
using ei8.EventSourcing.Client;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ei8.Extensions.DependencyInjection.Coding.d23.neurULization.Persistence
{
    public static class TinyIoCContainerExtensions
    {
        public static async Task<bool> AddMirrors(
            this TinyIoCContainer container,
            IEnumerable<object> initMirrorKeys,
            bool shouldInitializeMirrors,
            Guid userNeuronId
        )
        {
            bool result = false;

            var mirrorRepository = container.Resolve<IMirrorRepository>();
            var missingInitMirrorConfigs = await mirrorRepository.GetAllMissingAsync(initMirrorKeys);

            var initialized = shouldInitializeMirrors &&
                missingInitMirrorConfigs.Any() &&
                await TinyIoCContainerExtensions.InitializeMirrors(
                    container.Resolve<ITransaction>(),
                    userNeuronId,
                    mirrorRepository,
                    missingInitMirrorConfigs.Select(mimc => mimc.Key)
                );

            if (!shouldInitializeMirrors || !initialized)
            {
                Trace.WriteLine("Not initializing mirrors or Mirrors exist. Continuing app initialization...");

                var d23Keys = typeof(MirrorSet).GetProperties().Select(p => p.Name);
                var refs = await mirrorRepository.GetByKeysAsync(d23Keys, false);
                if (refs.Any() && d23Keys.Count() == refs.Count())
                {
                    IMirrorSet mirrorSet = new MirrorSet();

                    foreach (var pk in d23Keys)
                        mirrorSet.GetType().GetProperty(pk.ToString()).SetValue(
                            mirrorSet,
                            refs[pk]
                        );

                    container.Register(mirrorSet);

                    result = true;
                }
            }
            else
            {
                Trace.WriteLine("Mirrors initialized successfully. Shutting down application...");
                Environment.Exit(0);
            }

            return result;
        }

        private static async Task<bool> InitializeMirrors(
            ITransaction transaction, 
            Guid userNeuronId, 
            IMirrorRepository mirrorRepository,
            IEnumerable<string> keys = null
            )
        {
            await transaction.BeginAsync(userNeuronId);
            bool initialized = await mirrorRepository.Initialize(keys);
            await transaction.CommitAsync();

            return initialized;
        }

        public static void AddGrannyService(this TinyIoCContainer container, string identityAccessOutBaseUrl, string appUserId)
        {
            container.Register<IGrannyService>(
                (tic, npo) => new GrannyService(
                    container.Resolve<IServiceProvider>(),
                    container.Resolve<INetworkRepository>(),
                    container.Resolve<IDictionary<string, Network>>(),
                    container.Resolve<ITransaction>(),
                    container.Resolve<INetworkTransactionService>(),
                    container.Resolve<IValidationClient>(),
                    identityAccessOutBaseUrl,
                    appUserId
                )
            );
        }
    }
}
