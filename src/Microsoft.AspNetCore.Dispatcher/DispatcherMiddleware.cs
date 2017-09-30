﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherMiddleware
    {
        private readonly ILogger _logger;
        private readonly DispatcherOptions _options;
        private readonly RequestDelegate _next;

        public DispatcherMiddleware(IOptions<DispatcherOptions> options, ILogger<DispatcherMiddleware> logger, RequestDelegate next)
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

        public async Task Invoke(HttpContext httpContext)
        {
            var feature = new DispatcherFeature();
            httpContext.Features.Set<IDispatcherFeature>(feature);

            foreach (var entry in _options.Dispatchers)
            {
                await entry.Dispatcher(httpContext);

                if (feature.RequestDelegate != null)
                {
                    _logger.LogInformation("Executing short-circuit response from dispatcher");
                    try
                    {
                        await feature.RequestDelegate(httpContext);
                    }
                    finally
                    {
                        _logger.LogInformation("Executed short-circuit response from dispatcher");
                    }

                    return;
                }

                if (feature.Endpoint != null)
                {
                    _logger.LogInformation("Matched endpoint {Endpoint}", feature.Endpoint.DisplayName);
                    break;
                }
            }

            await _next(httpContext);
        }
    }
}
