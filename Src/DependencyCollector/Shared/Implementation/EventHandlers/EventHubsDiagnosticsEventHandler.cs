﻿namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.EventHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implements EventHubs DiagnosticSource events handling.
    /// </summary>
    internal class EventHubsDiagnosticsEventHandler : DiagnosticsEventHandlerBase
    {
        public const string DiagnosticSourceName = "Microsoft.Azure.EventHubs";
        private const string EntityPropertyName = "Entity";
        private const string EndpointPropertyName = "Endpoint";

        internal EventHubsDiagnosticsEventHandler(TelemetryConfiguration configuration) : base(configuration)
        {
        }

        public override void OnEvent(KeyValuePair<string, object> evnt, DiagnosticListener ignored)
        {
            if (evnt.Key.EndsWith(".Stop", StringComparison.OrdinalIgnoreCase))
            {
                Activity currentActivity = Activity.Current;
                this.OnDependency(evnt.Key, evnt.Value, currentActivity);
            }
        }

        private void OnDependency(string name, object payload, Activity activity)
        {
            DependencyTelemetry telemetry = new DependencyTelemetry { Type = RemoteDependencyConstants.AzureEventHubs };

            this.SetCommonProperties(name, payload, activity, telemetry);

            // Endpoint is URL of particular EventHub, e.g. sb://eventhubname.servicebus.windows.net/
            string endpoint = this.FetchPayloadProperty<Uri>(name, EndpointPropertyName, payload)?.ToString();

            // Queue/Topic name, e.g. myqueue/mytopic
            string queueName = this.FetchPayloadProperty<string>(name, EntityPropertyName, payload);

            // Target uniquely identifies the resource, we use both: queueName and endpoint 
            // with schema used for SQL-dependencies
            telemetry.Target = string.Join(" | ", endpoint, queueName);

            this.TelemetryClient.TrackDependency(telemetry);
        }
    }
}
