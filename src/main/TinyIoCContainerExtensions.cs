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
        public static async Task AddExternalReferences(
            this TinyIoCContainer container,
            IExternalReferenceRepository externalReferenceRepository,
            IEnumerable<object> externalReferenceKeys,
            bool createExternalReferencesIfNotFound
        )
        {
            if (!await TinyIoCContainerExtensions.InitializeExternalReferencesAsync(
                externalReferenceRepository,
                externalReferenceKeys,
                createExternalReferencesIfNotFound
                ))
            {
                var refs = await externalReferenceRepository.GetByKeysAsync(externalReferenceKeys);
                IExternalReferenceSet ps = new ExternalReferenceSet();

                foreach (var pk in externalReferenceKeys.OfType<Enum>())
                    ps.GetType().GetProperty(pk.ToString()).SetValue(ps, refs[pk]);

                container.Register(ps);
            }
            else
            {
                Trace.WriteLine("ExternalReferences initialized successfully. Shutting down application...");
                Environment.Exit(0);
            }
        }

        public static void AddGrannyService(this TinyIoCContainer container, string identityAccessOutBaseUrl, string appUserId)
        {
            container.Register<IGrannyService>(
                (tic, npo) => new GrannyService(
                    container.Resolve<IServiceProvider>(),
                    container.Resolve<IEnsembleRepository>(),
                    container.Resolve<IDictionary<string, Ensemble>>(),
                    container.Resolve<ITransaction>(),
                    container.Resolve<IEnsembleTransactionService>(),
                    container.Resolve<IValidationClient>(),
                    identityAccessOutBaseUrl,
                    appUserId
                )
            );
        }

        private static async Task<bool> InitializeExternalReferencesAsync(
            IExternalReferenceRepository externalReferenceRepository,
            IEnumerable<object> externalReferenceKeys,
            bool createExternalReferencesIfNotFound
        )
        {
            var result = false;

            if (createExternalReferencesIfNotFound)
            {
                var missingExternalReferences = await externalReferenceRepository.GetAllMissingAsync(externalReferenceKeys);

                if (missingExternalReferences.Any())
                {
                    await externalReferenceRepository.Save(missingExternalReferences);
                    result = true;
                }
            }

            return result;
        }
    }
}
