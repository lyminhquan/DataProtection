﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.KeyManagement;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.DataProtection
{
    public class DefaultDataProtectionProvider : IDataProtectionProvider
    {
        private readonly IDataProtectionProvider _innerProvider;

        public DefaultDataProtectionProvider()
        {
            // use DI defaults
            var serviceProvider = new ServiceCollection().AddDataProtection().BuildServiceProvider();

            _innerProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        }

        public DefaultDataProtectionProvider(
            [NotNull] IOptions<DataProtectionOptions> optionsAccessor,
            [NotNull] IKeyManager keyManager)
        {
            KeyRingBasedDataProtectionProvider rootProvider = new KeyRingBasedDataProtectionProvider(new KeyRingProvider(keyManager));
            var options = optionsAccessor.Options;
            _innerProvider = (!String.IsNullOrEmpty(options.ApplicationDiscriminator))
                ? (IDataProtectionProvider)rootProvider.CreateProtector(options.ApplicationDiscriminator)
                : rootProvider;
        }

        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            return _innerProvider.CreateProtector(purpose);
        }
    }
}