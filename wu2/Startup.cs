using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using Microsoft.AspNet.Identity;
using System.Configuration;

[assembly: OwinStartup(typeof(wu2.Startup))]

namespace wu2
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 配置 SignalR（如果您需要使用 SignalR）
            app.MapSignalR();
          
            ConfigureAuth(app);
        }

        public void ConfigureAuth(IAppBuilder app)
        {
            // 配置 Cookie 认证，这个必须首先配置
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login")
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            // 配置 Google OAuth2 认证
            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            {
                ClientId = "614702974975-dk20fi0n3rgejgvkq7t8rlgu1n7shi6m.apps.googleusercontent.com",
                ClientSecret = "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",
                CallbackPath = new PathString("/signin-google"),
                SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie
            });
        }
    }
}
