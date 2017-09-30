﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class EndpointMiddleware
    {
        private readonly ILogger _logger;
        private readonly DispatcherOptions _options;
        private readonly RequestDelegate _next;

        public EndpointMiddleware(IOptions<DispatcherOptions> options, ILogger<DispatcherMiddleware> logger, RequestDelegate next)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _options = options.Value;
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var feature = context.Features.Get<IDispatcherFeature>();
            if (feature.Endpoint != null && feature.RequestDelegate == null)
            {
                for (var i = 0; i < _options.HandlerFactories.Count; i++)
                {
                    var handler = _options.HandlerFactories[i](feature.Endpoint);
                    if (handler != null)
                    {
                        feature.RequestDelegate = handler(_next);
                        break;
                    }
                }
            }

            if (feature.RequestDelegate == null)
            {
                _logger.LogWarning("Could not create a handler for endpoint {Endpoint}");
                await _next(context);
                return;
            }

            _logger.LogInformation("Executing endpoint {Endpoint}", feature.Endpoint.DisplayName);
            try
            {
                await feature.RequestDelegate(context);
            }
            finally
            {
                _logger.LogInformation("Executed endpoint {Endpoint}", feature.Endpoint.DisplayName);
            }
        }
    }
}
