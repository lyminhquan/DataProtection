// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up data protection services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class DataProtectionServiceCollectionExtensions
    {
        /// <summary>
        /// Adds data protection services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        public static IDataProtectionBuilder AddDataProtection(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IActivator, TypeForwardingActivator>();
            services.AddOptions();
            AddDataProtectionServices(services);

            return new DataProtectionBuilder(services);
        }

        /// <summary>
        /// Adds data protection services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{DataProtectionOptions}"/> to configure the provided <see cref="DataProtectionOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IDataProtectionBuilder AddDataProtection(this IServiceCollection services, Action<DataProtectionOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            var builder = services.AddDataProtection();
            services.Configure(setupAction);
            return builder;
        }

        private static void AddDataProtectionServices(IServiceCollection services)
        {
            services.TryAddSingleton<ILoggerFactory>(DataProtectionProviderFactory.GetDefaultLoggerFactory());

            if (OSVersionUtil.IsWindows())
            {
                services.TryAddSingleton<RegistryPolicyResolver>();
            }

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IConfigureOptions<KeyManagementOptions>, KeyManagementOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<DataProtectionOptions>, DataProtectionOptionsSetup>());

            services.TryAddSingleton<IKeyManager, XmlKeyManager>();

            // Internal services
            services.TryAddSingleton<IDefaultKeyResolver, DefaultKeyResolver>();
            services.TryAddSingleton<IKeyRingProvider, KeyRingProvider>();

            services.TryAddSingleton<IDataProtectionProvider>(s =>
            {
                var dpOptions = s.GetRequiredService<IOptions<DataProtectionOptions>>();
                var keyRingProvider = s.GetRequiredService<IKeyRingProvider>();
                var loggerFactory = s.GetRequiredService<ILoggerFactory>();

                IDataProtectionProvider dataProtectionProvider = null;
                dataProtectionProvider = new KeyRingBasedDataProtectionProvider(keyRingProvider, loggerFactory);

                // Link the provider to the supplied discriminator
                if (!string.IsNullOrEmpty(dpOptions.Value.ApplicationDiscriminator))
                {
                    dataProtectionProvider = dataProtectionProvider.CreateProtector(dpOptions.Value.ApplicationDiscriminator);
                }

                return dataProtectionProvider;
            });

#if NET46 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
            services.TryAddSingleton<ICertificateResolver, CertificateResolver>();
#elif NETSTANDARD1_3
#else
#error target frameworks need to be updated.
#endif
        }
    }
}
