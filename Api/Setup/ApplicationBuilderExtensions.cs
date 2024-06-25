using Microsoft.Extensions.ObjectPool;
using Prometheus;
using System.Text;

namespace Api.Setup;

public static class ApplicationBuilderExtensions
{
    private static readonly CounterConfiguration _counterConfiguration = new() { LabelNames = new[] { "method", "path" } };
    /// <summary>
    /// Call after UseRouting
    /// </summary>
    /// <param name="app"></param>
    /// <param name="metricsConfig"></param>
    public static void UsePrometheusCounterWithHttpMetrics(this IApplicationBuilder app)
    {
        var description = $"[ApiCounter][Method/Path][Counter]";

        var counter = Prometheus.Metrics.CreateCounter("ApiCounter", description, _counterConfiguration);

        app.Use((context, next) =>
        {
            var endpoint = context.GetRouteEndpoint();
            counter.WithLabels(context.Request.Method, endpoint).Inc();

            return next();
        });
    }

    private static string GetRouteEndpoint(this HttpContext context)
    {
        // Getting a ObjectPool for StringBuilder, this is used because StringBuilder is a large object, and this benefits the application by reusing the object avoiding creating unecessary object.
        var stringBuilderPool = context.RequestServices.GetRequiredService<ObjectPool<StringBuilder>>();
        var stringBuilder = stringBuilderPool.Get();

        // Here we get all the paths and values from request
        var allPaths = context.Request.Path.Value?.Split('/');

        var routeData = context.GetRouteData();

        // this is a control list of the paths added to stringBuilder
        var keyAdded = new List<string>();

        // This looping is for replace some actual values for some PlaceHolders, this will avoid over counting
        foreach (var path in allPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            stringBuilder.Append('/');

            // searching for the KeyPair for the actual path
            var routeDataKeyValue = routeData.Values.FirstOrDefault(x
                                            => x.Value is not null
                                            && x.Value.ToString().Equals(path)
                                            && !keyAdded.Contains(path));


            // here we need the value of controller
            if (!string.IsNullOrEmpty(routeDataKeyValue.Key) && routeDataKeyValue.Key.Equals("controller", StringComparison.OrdinalIgnoreCase))
            {
                stringBuilder.Append(routeDataKeyValue.Value);
                continue;
            }

            if (!string.IsNullOrEmpty(routeDataKeyValue.Key))
            {

                // we need to check if the value is already added, if exists we need to search for the next one
                if (keyAdded.Contains(routeDataKeyValue.Key))
                {
                    routeDataKeyValue = routeData.Values.FirstOrDefault(x
                        => x.Value is not null
                        && x.Value.ToString().Equals(path)
                        && !x.Key.Equals(routeDataKeyValue.Key));
                }

                keyAdded.Add(routeDataKeyValue.Key);
                // adding the Key PlaceHolder instead of route value, here were we avoid the over counting
                stringBuilder.Append($"{{{routeDataKeyValue.Key}}}");
                continue;
            }

            // append constant path
            stringBuilder.Append(path);
        }

        var endpoint = stringBuilder.ToString();
        stringBuilderPool.Return(stringBuilder);

        return endpoint;
    }
}
