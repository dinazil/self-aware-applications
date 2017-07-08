using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Gatos.Web.Startup))]
namespace Gatos.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
