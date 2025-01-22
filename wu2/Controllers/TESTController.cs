using System.Linq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using wu2.Models;
public class TestGoogleLoginController : Controller
{
    private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;
    wuEntities1 db =new wuEntities1();


    public void Login()
    {

        AuthenticationManager.Challenge(new AuthenticationProperties { RedirectUri = Url.Action("GoogleLoginCallback") }, "Google");
    }


    public async Task<ActionResult> GoogleLoginCallback(string returnUrl)
    {
        try
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                TempData["Error"] = "登錄為空";
                return RedirectToAction("Login");
            }

     
            var email = loginInfo.Email;
            var googleId = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Name);
            var givenName = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.GivenName);
            var surname = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Surname);
            var pictureUrl = loginInfo.ExternalIdentity.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            var user = db.Users.SingleOrDefault(u => u.Email == email);

            if (user != null)
            {
             
                if (string.IsNullOrEmpty(user.GoogleId))
                {
               
                    user.GoogleId = googleId;
                    user.FullName = name;
                
                    user.ProfilePhoto = pictureUrl;
                }
                else if (user.GoogleId != googleId)
                {
                
                    TempData["Error"] = "此墊子郵件關連到不同的google帳戶";
                    return RedirectToAction("Login");
                }
            }
            else
            {
                user = new Users
                {
                    Email = email,
                    GoogleId = googleId,
                    FullName = name,
                    PasswordHash="1",
                    ProfilePhoto = pictureUrl,
                    Role = "GMember",
                    RegistrationDate = DateTime.Now
                };
                db.Users.Add(user);
            }

            db.SaveChanges();

     
            Session["UserId"] = user.UserId;
            Session["Email"] = user.Email;
            Session["FullName"] = user.FullName;
            Session["Role"] = user.Role;
            Session["ProfilePhotoPath"] = user.ProfilePhoto;


            AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = true }, loginInfo.ExternalIdentity);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("ExternalLoginCallback error: " + ex.Message);
            TempData["Error"] = "錯誤";
            return RedirectToAction("Login");
        }
    }


}
