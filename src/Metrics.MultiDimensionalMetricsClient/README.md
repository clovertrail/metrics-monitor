# About Metrics.MultiDimensionalMetricsClient and Metrics.Serialization

The two projects are copied from [EngSys-MDA-MetricsAndHealth](https://msazure.visualstudio.com/DefaultCollection/One/_git//EngSys-MDA-MetricsAndHealth).

We need the support from package `Microsoft.Cloud.Metrics.Client`, however, it was built againts .NET Framework and not compatible with .NET Core.
So we copied the relative source from the Metrics repo, and fixed the code so that it works in .NET Core.

The following are the list of modifications:

* Nuget dependencies for both project
* Metrics.MultiDimensionalMetricsClient
   * Utility/UserAccessTokenRefresher.cs
      * Line 81, `new PlatformParameters()` - removed constructor arguments
   * Utility/HttpClientHelper.cs
      * `WebRequestHandler` to `HttpClientHandler`
      * `WebRequestHandler.ServerCertificateValidationCallback` to `HttpClientHandler.ServerCertificateCustomValidationCallback`