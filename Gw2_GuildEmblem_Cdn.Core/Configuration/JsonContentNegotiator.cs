﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Gw2_GuildEmblem_Cdn.Core.Configuration
{
    public class JsonContentNegotiator : IContentNegotiator
    {
        private readonly JsonMediaTypeFormatter m_oJsonFormatter;

        public JsonContentNegotiator(JsonMediaTypeFormatter oFormatter)
        {
            m_oJsonFormatter = oFormatter;
        }

        public ContentNegotiationResult Negotiate(
                Type type,
                HttpRequestMessage request,
                IEnumerable<MediaTypeFormatter> formatters)
        {
            return new ContentNegotiationResult(
                m_oJsonFormatter,
                new MediaTypeHeaderValue("application/json"));
        }
    }
}