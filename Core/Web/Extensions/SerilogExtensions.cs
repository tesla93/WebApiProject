using System.Net;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Core.Web.Extensions
{
    public static class SerilogExtensions
    {
        public static LoggerConfiguration WithDockerContainerId(this LoggerEnrichmentConfiguration config)
        {
            return config.With<DockerContainerIdEnricher>();
        }
    }

    public class DockerContainerIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ContainerID", Dns.GetHostName()));
        }
    }
}