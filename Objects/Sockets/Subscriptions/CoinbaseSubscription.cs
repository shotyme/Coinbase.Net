using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Coinbase.Net.Objects.Models;
using Coinbase.Net.Objects.Internal;
using CryptoExchange.Net.Clients;
using System.Linq;

namespace Coinbase.Net.Objects.Sockets.Subscriptions
{
    /// <inheritdoc />
    internal class CoinbaseSubscription<T> : Subscription<CoinbaseSocketMessage<CoinbaseSubscriptionsUpdate>, CoinbaseSocketMessage<CoinbaseSubscriptionsUpdate>> where T: CoinbaseSocketEvent
    {
        /// <inheritdoc />
        public override HashSet<string> ListenerIdentifiers { get; set; }

        private readonly Action<DataEvent<IEnumerable<T>>> _handler;
        private readonly string _channel;
        private readonly string[]? _symbols;
        private readonly SocketApiClient _client;

        /// <inheritdoc />
        public override Type? GetMessageType(IMessageAccessor message)
        {
            return typeof(CoinbaseSocketMessage<T>);
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="symbols"></param>
        /// <param name="handler"></param>
        /// <param name="auth"></param>
        public CoinbaseSubscription(SocketApiClient client, ILogger logger, string channel, string channelIdentifier, string[]? symbols, Action<DataEvent<IEnumerable<T>>> handler, bool auth) : base(logger, auth)
        {
            _handler = handler;
            _channel = channel;
            _client = client;
            _symbols = symbols;
            ListenerIdentifiers = symbols?.Any() == true ? 
                new HashSet<string>(symbols.Select(x => channelIdentifier + "-" + x)) :
                new HashSet<string>() { channelIdentifier };
        }

        /// <inheritdoc />
        public override Query? GetSubQuery(SocketConnection connection) => new CoinbaseSubscriptionQuery(new CoinbaseSocketRequest
        {
            Channel = _channel,
            Type = "subscribe",
            Symbols = _symbols,
            Jwt = Authenticated ? ((CoinbaseAuthenticationProvider)_client.AuthenticationProvider!).GenerateToken(DateTime.UtcNow.AddSeconds(-5), null) : null
        }, Authenticated);

        /// <inheritdoc />
        public override Query? GetUnsubQuery() => new CoinbaseQuery<CoinbaseSocketMessage>(new CoinbaseSocketRequest
        {
            Channel = _channel,
            Type = "unsubscribe",
            Symbols = _symbols,
            Jwt = Authenticated ? ((CoinbaseAuthenticationProvider)_client.AuthenticationProvider!).GenerateToken(DateTime.UtcNow.AddSeconds(-5), null) : null
        }, Authenticated);

        /// <inheritdoc />
        public override CallResult DoHandleMessage(SocketConnection connection, DataEvent<object> message)
        {
            var data = (CoinbaseSocketMessage<T>)message.Data;
            _handler.Invoke(message.As(data.Events, data.Channel, null, data.Events.First().EventType.Equals("snapshot") ? SocketUpdateType.Snapshot : SocketUpdateType.Update));
            return new CallResult(null);
        }

    }
}
