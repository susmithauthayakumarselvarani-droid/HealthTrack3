using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HealthTrack3.Startup))]
namespace HealthTrack3
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
