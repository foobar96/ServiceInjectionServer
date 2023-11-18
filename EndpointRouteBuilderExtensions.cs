using Microsoft.AspNetCore.Mvc;

namespace settings_injection
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapServiceApi(this IEndpointRouteBuilder builder)
        {
            builder.MapPatch("/service", async (ServiceManager serviceManager, [FromBody] PeriodicHostedServiceRequest state) =>
                await HandlePatchServiceRequestAsync(serviceManager, state));

            return builder;
        }

        /// <summary>
        /// Handles the PATCH service request by either creating a new service or communicating with an existing one.
        /// </summary>
        /// <param name="state">The PeriodicHostedServiceState containing the service ID, termination request, and new service description.</param>
        /// <returns>A string indicating the result of the request.</returns>
        private static async Task<string> HandlePatchServiceRequestAsync(ServiceManager serviceManager, PeriodicHostedServiceRequest request)
        {
            if (request.NewService is not null)
            {
                var instance = request.NewService.CreateInstance();
                var id = await serviceManager.HostServiceAsync(instance);
                return $"Service created with Id: {id}";
            }

            if (request.ServiceId is null)
                return "Nothing happened";

            var service = serviceManager.GetService(request.ServiceId.Value)
                ?? throw new NullReferenceException("Service not found");

            if (!request.RequestTermination)
                return $"Service Type: {service.GetType()}";

            await service.StopAsync(CancellationToken.None);

            return "Service terminated";
        }
    }

    record PeriodicHostedServiceRequest(int? ServiceId, bool RequestTermination, ServiceDescription NewService);
}
