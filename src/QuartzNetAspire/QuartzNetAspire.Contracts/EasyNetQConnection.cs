using EasyNetQ;

namespace QuartzNetAspire.Contracts;

public static class EasyNetQConnection
{
    /// <summary>
    /// Aspire hands out RabbitMQ as an AMQP URI (amqp://user:pass@host:port); EasyNetQ wants key=value pairs or a
    /// <see cref="ConnectionConfiguration"/>. This bridges the two.
    /// ponytail: user/password/host/port only; add VirtualHost/Ssl parsing when a non-default vhost or TLS shows up.
    /// </summary>
    public static Action<ConnectionConfiguration> FromAmqpUri(string amqpUri) => configuration =>
    {
        var uri = new Uri(amqpUri);
        var userInfo = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        configuration.Hosts.Add(new HostConfiguration(uri.Host, (ushort)uri.Port));
        configuration.UserName = userInfo[0];
        configuration.Password = userInfo.Length > 1 ? userInfo[1] : "guest";
        configuration.PublisherConfirms = true;
    };
}
