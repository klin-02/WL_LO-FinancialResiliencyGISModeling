using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using LandValueAnalysis.ViewModels;
using LandValueAnalysis.Services;
using LandValueAnalysis.Services.Factories;
using System.Configuration;

namespace LandValueAnalysis.Views
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            try
            {
                var builder = BuildConfigurationBuilder();
                _configuration = builder.Build();

                _serviceProvider = BuildDependencyContainer();

                BuildArcGISRuntimeEnvironment();
            }
            catch (Exception ex)
            {
                throw new Exception($"Building application failed!\n\n{ex.ToString()}");
            }
        }

        private IConfigurationBuilder BuildConfigurationBuilder()
            => new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        private IServiceProvider BuildDependencyContainer()
        {
            IServiceCollection serviceContainer = new ServiceCollection();

            //view models
            serviceContainer.AddSingleton<MainViewModel>();
            serviceContainer.AddTransient<MapViewModel>();
            serviceContainer.AddTransient<StatsViewModel>();

            //Services
            serviceContainer.AddSingleton<LayerFactory>();
            serviceContainer.AddSingleton<NavigationService>(x => new NavigationService(_serviceProvider));

            //View
            serviceContainer.AddSingleton<MainWindow>();

            return serviceContainer.BuildServiceProvider();
        }

        private void BuildArcGISRuntimeEnvironment() 
            => ArcGISRuntimeEnvironment.Initialize(c => c
                .UseApiKey(_configuration.GetConnectionString("ApiKey")
                    ?? throw new InvalidOperationException("Couldn't find api key"))
                .ConfigureHttp(x => x
                    .UseDefaultReferer(new Uri(_configuration.GetConnectionString("Referer")
                        ?? throw new InvalidOperationException("Could not find referer!")))
                ));

        private void Application_Start(object sender, StartupEventArgs e)
        {
            MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            MainWindow.Show();
        }
    }
}
