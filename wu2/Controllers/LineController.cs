using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using wu2.Models; // 确保引入你的数据库模型命名空间
using System.Web.Security;
using System.Data.Entity.Validation;
using Newtonsoft.Json;
using System.IO;
using System.Data.Entity;

namespace YourNamespace.Controllers
{
    public class LineController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly wuEntities1 dbContext; // 你的数据库上下文

        public LineController()
        {
            this.httpClient = new HttpClient();
            this.dbContext = new wuEntities1(); // 初始化你的数据库上下文
        }
        //用户访问 /LoginWithLine
        [HttpPost]
        public async Task<ActionResult> ProcessLiffData(string userId, string displayName)
        {
            // 从数据库中检查用户是否已存在
            var existingUser = dbContext.Users.FirstOrDefault(u => u.LineUserId == userId);
            if (existingUser != null)
            {
                // 用户已存在，更新信息并设置会话
                existingUser.FullName = displayName;
                dbContext.Entry(existingUser).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();

                // 设置 SESSION
                Session["UserId"] = existingUser.UserId;
                Session["FullName"] = existingUser.FullName;
                Session["ProfilePhotoPath"] = existingUser.ProfilePhoto;

                // 登录用户
                FormsAuthentication.SetAuthCookie(existingUser.Email, false);
                return Json(new { success = true });
            }
            else
            {
                // 用户不存在，可能需要执行注册或引导用户完成其他必要步骤
                // 返回适当的状态或引导用户到注册页面
                return Json(new { success = false, message = "User not found or needs registration" });
            }
        }

        [HttpGet]
        public ActionResult LoginWithLine()
        {
            var clientId = "2005979321"; // 你的 Channel ID
            var redirectUri = Url.Action("Callback", "Line", null, Request.Url.Scheme);
            var state = Guid.NewGuid().ToString("N"); // 生成一个随机的状态值防止 CSRF

            var authorizationUrl = $"https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&state={state}&scope=profile%20openid%20email%20phone";

            // 使用日志记录 URL
            Console.WriteLine("Authorization URL: " + authorizationUrl);

            return Redirect(authorizationUrl);
        }

        // 其他代码保持不变...
        public ActionResult SetEmailAndPassword()
        {
            var model = new SetContactInfoViewModel(); // 确保模型已实例化

            // 检查 Session 是否存在，并确保必要的数据已保存
            if (Session == null || Session["UserProfile"] == null || Session["IdToken"] == null)
            {
                // 记录日志或给出提示，说明 Session 数据不可用或已过期
                return new HttpStatusCodeResult(500, "Session is not available or has expired.");
            }

            // 返回视图让用户输入邮箱和密码
            return View(model); // 传递模型实例
        }

        [HttpPost]
        public async Task<ActionResult> SetEmailAndPassword(SetContactInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 检查数据库中是否存在相同的邮箱
            var emailExists = dbContext.Users.Any(u => u.Email == model.Email);

            if (emailExists)
            {
                // 如果邮箱已存在，设置 ViewBag 并返回视图
                ViewBag.EmailExists = true;
                return View(model);
            }

            // 从 Session 中获取 userProfile 和 idToken
            var userProfile = Session["UserProfile"] as JObject;
            var idToken = Session["IdToken"] as string;

            // 检查 userProfile 和 idToken 是否为 null
            if (userProfile == null || idToken == null)
            {
                return new HttpStatusCodeResult(500, "Session expired or invalid");
            }

            // 获取 LineUserId
            var lineUserId = userProfile["userId"]?.ToString();

            if (string.IsNullOrEmpty(lineUserId))
            {
                return new HttpStatusCodeResult(500, "LineUserId is null or invalid.");
            }

            // 使用用户输入的邮箱和密码来更新用户资料
            var user = new Users
            {
                Email = model.Email,
                FullName = userProfile["displayName"]?.ToString(),
                ProfilePhoto = userProfile["pictureUrl"]?.ToString(),
                PasswordHash = model.Password,
                LineUserId = lineUserId,
                PasswordResetToken = idToken,   
                RegistrationDate = DateTime.Now
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // 清除 session 中的临时数据
            Session.Remove("UserProfile");
            Session.Remove("IdToken");

            // 设置 SESSION
            Session["UserId"] = user.UserId;
            Session["FullName"] = user.FullName;
            Session["ProfilePhotoPath"] = user.ProfilePhoto;

            // 登录用户
            FormsAuthentication.SetAuthCookie(model.Email, false);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<ActionResult> UseDefaultEmailAndPassword()
        {
            // 从 Session 中获取 userProfile 和 idToken
            var userProfile = Session["UserProfile"] as JObject;
            var idToken = Session["IdToken"] as string;

            // 检查 userProfile 和 idToken 是否为 null
            if (userProfile == null || idToken == null)
            {
                return new HttpStatusCodeResult(500, "Session expired or invalid");
            }

            // 生成随机邮箱和密码
            var email = GenerateRandomEmail();
            var password = GenerateRandomPassword();

            // 使用随机邮箱和密码来更新用户资料
            var user = await UpdateUserProfile(userProfile, idToken, password, email);

            // 清除 session 中的临时数据
            Session.Remove("UserProfile");
            Session.Remove("IdToken");

            // 设置 SESSION
            Session["UserId"] = user.UserId;
            Session["FullName"] = user.FullName;
            Session["ProfilePhotoPath"] = user.ProfilePhoto;

            // 登录用户
            FormsAuthentication.SetAuthCookie(email, false);

            return RedirectToAction("Index", "Home");
        }


        public async Task<ActionResult> Callback()
        {
            var query = this.Request.QueryString;
            var code = query["code"];

            if (string.IsNullOrEmpty(code))
            {
                return new HttpStatusCodeResult(400, "Code not found");
            }

            var (accessToken, idToken) = await ExchangeAccessToken(code);

            if (accessToken == null)
            {
                return new HttpStatusCodeResult(400, "Failed to get access token");
            }

            var userProfile = await GetUserProfile(accessToken);
            if (userProfile == null)
            {
                return new HttpStatusCodeResult(400, "Failed to get user profile");
            }

            var lineUserId = userProfile["userId"]?.ToString();

            if (string.IsNullOrEmpty(lineUserId))
            {
                return new HttpStatusCodeResult(400, "LineUserId not found");
            }

            // 檢查 LineUserId 是否已存在
            var existingUser = dbContext.Users.FirstOrDefault(u => u.LineUserId == lineUserId);

            if (existingUser != null)
            {
                // 如果使用者存在，更新使用者資料（若有變更）
                bool isUpdated = false;

                var newFullName = userProfile["displayName"]?.ToString();
                if (newFullName != existingUser.FullName)
                {
                    existingUser.FullName = newFullName;
                    isUpdated = true;
                }

                var newProfilePhoto = userProfile["pictureUrl"]?.ToString();
                if (newProfilePhoto != existingUser.ProfilePhoto)
                {
                    existingUser.ProfilePhoto = newProfilePhoto;
                    isUpdated = true;
                }

                if (isUpdated)
                {
                    dbContext.SaveChanges();
                }

                // 設置 Session 並登入使用者
                SetUserSession(existingUser);
                FormsAuthentication.SetAuthCookie(existingUser.Email, false);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                // 嘗試從 id_token 解碼 Email 並建立新使用者
                var email = DecodeIdTokenForEmail(idToken);
                if (string.IsNullOrEmpty(email))
                {
                    return new HttpStatusCodeResult(400, "Email not found in id_token.");
                }

                // 建立新使用者
                var newUser = new Users
                {
                    Email = email,
                    FullName = userProfile["displayName"]?.ToString(),
                    PasswordHash= "defaul123",
                    ProfilePhoto = userProfile["pictureUrl"]?.ToString(),
                    LineUserId = lineUserId,
                    RegistrationDate = DateTime.Now,
                    Role = "Member" // 可根據需求設置預設角色
                };

                dbContext.Users.Add(newUser);
                await dbContext.SaveChangesAsync();

                // 設置 Session 並登入新使用者
                SetUserSession(newUser);
                FormsAuthentication.SetAuthCookie(newUser.Email, false);

                return RedirectToAction("Index", "Home");
            }
        }

        // 解碼 id_token 取得 Email
        private string DecodeIdTokenForEmail(string idToken)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(idToken);

            // 取得 Email
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        }

        // 設置 Session 的方法
        private void SetUserSession(Users user)
        {
            Session["UserId"] = user.UserId;
            Session["FullName"] = user.FullName;
            Session["ProfilePhotoPath"] = user.ProfilePhoto;
            Session["Role"] = user.Role;
        }



        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateRandomEmail()
        {
            var random = new Random();
            var randomString = new string(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz0123456789", 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"{randomString}@example.com";
        }
        private async Task<(string, string)> ExchangeAccessToken(string code)
        {
            var redirectUri = Url.Action("Callback", "Line", null, Request.Url.Scheme);

            var tokenRequestParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = "2005979321", // 你的 Channel ID
                ["client_secret"] = "1f438e4c179b1634f6a8791a84e38dc1" // 你的 Channel Secret
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/oauth2/v2.1/token")
            {
                Content = requestContent
            };

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return (null, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(content);

            var accessToken = result["access_token"]?.ToString();
            var idToken = result["id_token"]?.ToString();

            return (accessToken, idToken);
        }

        private async Task<JObject> GetUserProfile(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.line.me/v2/profile");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("User Profile: " + content); // 記錄用戶配置文件數據
            var a = content.ToString();
            var userProfile = JObject.Parse(content);

            return userProfile;
        }
        private async Task<Users> UpdateUserProfile(JObject userProfile, string idToken, string password, string email)
        {
            var displayName = userProfile["displayName"]?.ToString() ?? "Unknown";
            var pictureUrl = userProfile["pictureUrl"]?.ToString();
            var lineUserId = userProfile["userId"]?.ToString() ?? Guid.NewGuid().ToString();

            // 检查用户是否已存在
            var user = dbContext.Users.FirstOrDefault(u => u.LineUserId == lineUserId);

            if (user != null)
            {
                // 如果用户存在，更新用户信息
                user.FullName = displayName; // 存储到 FullName 字段
                user.ProfilePhoto = pictureUrl; // 存储到 ProfilePhoto 字段
                user.PasswordResetToken = idToken; // 存储到 PasswordResetToken 字段
                user.TokenExpiration = DateTime.Now.AddHours(1); // 设定令牌过期时间
            }
            else
            {
                // 如果用户不存在，则创建新用户
                user = new Users
                {
                    Email = email,
                    LineUserId = lineUserId,
                    FullName = displayName, // 存储到 FullName 字段
                    ProfilePhoto = pictureUrl, // 存储到 ProfilePhoto 字段
                    PasswordResetToken = idToken, // 存储到 PasswordResetToken 字段
                    RegistrationDate = DateTime.Now,
                    PasswordHash = password // 存储到 PasswordHash 字段
                };

                dbContext.Users.Add(user);
            }

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Console.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        // 或者使用Logger将错误信息记录到日志文件中
                    }
                }
                throw;
            }

            return user;
        }

    }
}
