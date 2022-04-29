﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Models;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface IKubeWebHookConfigurationWriter
    {
        Task<V1Secret?> FetchCurrentCertificate();
        Task UpdateClusterWebHookConfiguration(TlsCertificateChainExport chainExport);
    }

    public class KubeWebHookConfigurationWriter : IKubeWebHookConfigurationWriter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly TlsStorageOptions _tlsStorageOptions;
        private readonly MutatingWebHookOptions _mutatingWebHookOptions;
        private readonly IKubernetesClient _kubernetesClient;
        private readonly IResourcePatcher _resourcePatcher;

        public KubeWebHookConfigurationWriter(TlsStorageOptions tlsStorageOptions,
                                              MutatingWebHookOptions mutatingWebHookOptions,
                                              IKubernetesClient kubernetesClient,
                                              IResourcePatcher resourcePatcher)
        {
            _tlsStorageOptions = tlsStorageOptions;
            _mutatingWebHookOptions = mutatingWebHookOptions;
            _kubernetesClient = kubernetesClient;
            _resourcePatcher = resourcePatcher;
        }

        public async Task<V1Secret?> FetchCurrentCertificate()
        {
            var existingSecret = await _kubernetesClient.Get<V1Secret>(_tlsStorageOptions.SecretName, _tlsStorageOptions.SecretNamespace);
            return existingSecret;
        }

        public async Task UpdateClusterWebHookConfiguration(TlsCertificateChainExport chainExport)
        {
            await PublishCertificateSecret(chainExport);
            await UpdateWebHookConfiguration(chainExport);
        }

        private async Task PublishCertificateSecret(TlsCertificateChainExport chainExport)
        {
            var (caCertificatePfx, caPublicPem, serverCertificatePfx) = chainExport;

            Logger.Info($"Ensuring certificates in '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}' are correct.");
            var secret = new V1Secret
            {
                Kind = V1Secret.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = _tlsStorageOptions.SecretName,
                    NamespaceProperty = _tlsStorageOptions.SecretNamespace,
                },
                Data = new Dictionary<string, byte[]>
                {
                    { _tlsStorageOptions.CaCertificateName, caCertificatePfx },
                    { _tlsStorageOptions.CaPublicName, caPublicPem },
                    { _tlsStorageOptions.ServerCertificateName, serverCertificatePfx }
                }
            };

            await _kubernetesClient.Save(secret);
        }

        private async Task UpdateWebHookConfiguration(TlsCertificateChainExport chainExport)
        {
            Logger.Info($"Ensuring web hook ca bundle in '{_mutatingWebHookOptions.ConfigurationName}' is correct.");
            var webHookConfiguration = await _kubernetesClient.Get<V1MutatingWebhookConfiguration>(
                _mutatingWebHookOptions.ConfigurationName
            );
            if (webHookConfiguration != null)
            {
                await _resourcePatcher.Patch(webHookConfiguration, configuration =>
                {
                    var webHook = configuration.Webhooks
                                               .FirstOrDefault(
                                                   x => string.Equals(x.Name, _mutatingWebHookOptions.WebHookName, StringComparison.OrdinalIgnoreCase)
                                               );
                    if (webHook != null)
                    {
                        webHook.ClientConfig.CaBundle = chainExport.CaPublicPem;
                    }
                });
            }
            else
            {
                Logger.Warn($"MutatingWebhookConfiguration '{_mutatingWebHookOptions.ConfigurationName}' "
                            + "was not found, web hooks will likely be broken.");
            }
        }
    }
}