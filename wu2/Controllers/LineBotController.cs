using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc; // 使用 MVC 控制器命名空间
using Line.Messaging;
using Line.Messaging.Webhooks;
using wu2.Models;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Text;
using static System.Net.WebRequestMethods;
using System.Web.Helpers;
using System.Security.Cryptography;
using System.Drawing.Drawing2D;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Data.Entity.Migrations;
using Google;
using static wu2.Models.GroupDetailsViewModel;
using System.Data.Entity;





namespace wu2.Controllers
{
    public class LineBotController : Controller // 使用 MVC 的 Controller 类
    {
        private readonly string _channelAccessToken = "kn3aVDXBvl0HDP+8u+4MvWsHdnoJHJapXEux6xuXlBBpCPRRZXJWMbI7tJiE4J2K3Zz7zW93hkrxVzIr43r8NKC5p48vo0WS5tsI+73Jd4Ya2ujxu7SiiygmKzP9sxy/hgCUw6w+STl2wZO0LJAJeQdB04t89/1O/w1cDnyilFU=";
        private readonly LineMessagingClient _lineMessagingClient;
        //string localhost = "https://dd0a-180-177-142-40.ngrok-free.app";
        string localhost = "https://justsplit.imd.pccu.edu.tw";
        wuEntities1 db = new wuEntities1();
        public LineBotController()
        {
            // 使用你的 Channel Access Token 创建客户端
            _lineMessagingClient = new LineMessagingClient(_channelAccessToken);
        }
        [HttpPost]
        public async Task<ActionResult> Webhook()
        {
            var json = await new StreamReader(Request.InputStream).ReadToEndAsync();
            Debug.WriteLine("Received Webhook JSON: " + json);
            var events = WebhookEventParser.Parse(json);
            Debug.WriteLine(events);

                foreach (var ev in events)
                {
                    // 处理机器人加入群组的事件
                    if (ev is JoinEvent joinEvent)
                    {
                        await HandleJoinEventAsync(joinEvent);
                    }
                    else if (ev is MemberJoinEvent memberJoinedEvent)
                    {
                        await HandleMemberJoinEventAsync(memberJoinedEvent);
                    }
                    // 处理普通消息事件
                    else if (ev is MessageEvent messageEvent)
                    {

                        // 檢查是否是文本消息
                        if (messageEvent.Message is TextEventMessage textMessage)
                        {
                            var userMessage = textMessage.Text.Trim();  // 獲取用戶發送的消息
                            var userId = messageEvent.Source.UserId;  // 抓取使用者的 UserId
                            var chatId = messageEvent.Source.Id;
                            var replyToken = messageEvent.ReplyToken;
                            var user = db.Users.FirstOrDefault(u => u.LineUserId == userId);
                            Console.WriteLine($"Received User Message: {userMessage}");
                            Trace.WriteLine($"UserId: {userId}");
                        // 處理 "加入群組" 指令
                        // 檢查是否為 "新增" 關鍵字開頭的訊息，例如 "新增 午餐 300"
                        if (userMessage.StartsWith("帳款"))
                        {
                            // 解析使用者輸入，假設格式為 "新增 項目 金額"
                            var messageParts = userMessage.Split(' ');
                            if (messageParts.Length == 3 && decimal.TryParse(messageParts[2], out decimal amount))
                            {
                                string item = messageParts[1]; // 獲取項目名稱
                                                               // 查找該用戶的群組 ID
                                int groupId = GetGroupIdForUserInChat(userId, chatId);

                                // 如果沒有找到群組，回應相應的錯誤訊息
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }
                                else
                                {
                                    // 調用 AddExpense 方法來新增支出
                                    await AddExpense(chatId,userId , item, amount);
                                    var successMessage = new TextMessage($"成功新增支出：{item} {amount} 元 均分給所有人");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { successMessage });
                                }
                            }
                            else
                            {
                                // 如果格式不正確，回應錯誤訊息
                                var errorMessage = new TextMessage("請使用正確的格式：帳款 項目 金額，例如：帳款 午餐 300");
                                await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                            }
                        }


                        if (userMessage == "加入群組")
                            {
                                // 呼叫加入群組邏輯，並傳遞 chatId 以保證唯一群組
                                string responseMessage = await JoinGroupAsync(userId, chatId);

                                // 回應用戶是否成功加入群組或已經在群組中
                                var replyMessage = new TextMessage(responseMessage);
                                await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { replyMessage });
                            }
                            if (userMessage == "消費統計")
                            {
                                // 获取群组ID
                                int groupId = GetGroupIdForUserInChat(userId, chatId);
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }
                                // 获取群组支出数据和未支付账目数据
                                var expenses = GetGroupExpenses(groupId);
                                var debts = GetGroupDebts(groupId);
                                // 查找该用户的群组 ID                        
                                // 调用统计方法，发送统计信息
                                await SendGroupSummaryAsTextAsync(_lineMessagingClient, replyToken, expenses, debts, groupId);
                            }
                            // 如果用户发送的消息是“債務提醒”
                            if (userMessage == "債務提醒")
                            {
                                int groupId = GetGroupIdForUserInChat(userId, chatId);
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }

                                // 调用函数，查询当前用户的欠款人，并发送提醒
                                await SendReminderMessagesToDebtorsAsync(user.UserId, replyToken, groupId);
                            }
                        // 判斷使用者是否發送了 "下載報表" 關鍵字
                        if (userMessage == "下載報表")
                        {
                            // 查找該用戶的群組 ID
                            int groupId = GetGroupIdForUserInChat(userId, chatId);

                            // 如果沒有找到群組，回應相應的錯誤訊息
                            if (groupId == 0)
                            {
                                // 組織錯誤訊息
                                var errorMessage = new TextMessage("您尚未加入任何群組，無法下載報表。");

                                // 回覆用戶錯誤訊息
                                await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                            }
                            else
                            {
                                // 呼叫匯出 PDF 的方法，並回傳 PDF 下載的連結
                                var responseMessage = await ExportAndSendPdfAsync(chatId, userId);

                                // 回傳包含 PDF 連結或內容的訊息
                                var pdfMessage = new TextMessage(responseMessage);

                                // 回覆用戶下載連結
                                await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { pdfMessage });
                            }
                        }


                        if (userMessage == "統計")
                            {
                                int groupId = GetGroupIdForUserInChat(userId, chatId);
                                var expenses = GetGroupExpenses(groupId);
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }
                                else
                                {
                                    // 生成 LIFF URL，並傳入 groupId
                                    string liffUrl = $"https://liff.line.me/2006127909-ObP6XZR9/Chart/create?groupId={groupId}&chatId={chatId}";
                                    // 回應用戶，並給出點擊鏈接
                                    var replyMessage = new TextMessage($"新增帳款，若有誤請重新打開即可正常: {liffUrl}");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { replyMessage });
                                }

                            }
                            if (userMessage == "結算")
                            {
                                int groupId = GetGroupIdForUserInChat(userId, chatId);
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }

                                var pairwiseDebts = CalculatePairwiseDebts(groupId);

                                // 发送结算结果
                                await SendDetailedSettlementAsTextAsync(_lineMessagingClient, replyToken, pairwiseDebts, groupId);
                            }

                            if (userMessage == "帳目列表")
                            {
                                // 查找该用户的群组 ID
                                int groupId = GetGroupIdForUserInChat(userId, chatId);

                                // 如果没有找到群组，返回相应的错误消息
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }
                                else
                                {
                                    // 获取群组的账目列表
                                    List<Expenses> expenses = GetGroupExpenses(groupId);

                                    // 发送账目列表
                                    await SendAccountSummaryAsFlexMessageAsync(_lineMessagingClient, messageEvent.ReplyToken, expenses);
                                }
                            }


                            if (userMessage == "新增帳款")
                            {
                                // 查找該用戶的群組 ID
                                int groupId = GetGroupIdForUserInChat(userId, chatId);

                                // 如果沒有找到群組，回應相應的錯誤訊息
                                if (groupId == 0)
                                {
                                    var errorMessage = new TextMessage("您還未加入任何群組。");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { errorMessage });
                                }
                                else
                                {
                                    // 生成 LIFF URL，並傳入 groupId
                                    string liffUrl = $"https://liff.line.me/2006127909-ObP6XZR9/expense/create?groupId={groupId}&chatId={chatId}";
                                    // 回應用戶，並給出點擊鏈接
                                    var replyMessage = new TextMessage($"請點擊以下鏈接新增帳款: {liffUrl}");
                                    await _lineMessagingClient.ReplyMessageAsync(messageEvent.ReplyToken, new List<ISendMessage> { replyMessage });
                                }
                            }
                        }
                    }
                }

            return new HttpStatusCodeResult(200);
        }
        private async Task<string> ExportAndSendPdfAsync(string chatId, string userId)
        {
            Trace.WriteLine("開始執行 ExportAndSendPdf 方法...");
            Trace.WriteLine($"參數：chatId={chatId}, userId={userId}");

            // 查詢群組 ID
            int groupId = GetGroupIdForUserInChat(userId, chatId);
            Trace.WriteLine($"取得群組ID：groupId={groupId}");

            if (groupId == 0)
            {
                Trace.WriteLine("找不到對應的群組。");
                return "找不到對應的群組。";
            }

            // 取得群組資訊
            var group = await db.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
            {
                Trace.WriteLine("群組不存在，無法生成報表。");
                return "群組不存在，無法生成報表。";
            }
            Trace.WriteLine($"取得群組資料：GroupName={group.GroupName}");

            // 取得群組成員
            var members = await db.GroupMembers
                                  .Where(m => m.GroupId == groupId)
                                  .Include(m => m.Users)
                                  .Select(m => new GroupMemberViewModel
                                  {
                                      UserId = m.Users.UserId,
                                      FullName = m.Users.FullName,
                                      Email = m.Users.Email,
                                      Role = m.Role,
                                      JoinedDate = m.JoinedDate.HasValue ? m.JoinedDate.Value : DateTime.MinValue,
                                      ProfilePhoto = m.Users.ProfilePhoto ?? "/path/to/default/photo.jpg",
                                      TotalSpent = db.ExpenseDetails
                                                  .Where(d => d.Expenses.GroupId == groupId && d.UserId == m.UserId)
                                                  .Sum(d => (decimal?)d.Amount) ?? 0
                                  })
                                  .ToListAsync() ?? new List<GroupMemberViewModel>();
            Trace.WriteLine($"取得成員資料：成員數量={members.Count}");

            // 取得群組支出
            var expenses = await db.Expenses
                                   .Where(e => e.GroupId == groupId)
                                   .Select(e => new ExpenseViewModel
                                   {
                                       ExpenseId = e.ExpenseId,
                                       TotalAmount = e.TotalAmount,
                                       ExpenseType = e.ExpenseType,
                                       ExpenseItem = e.ExpenseItem,
                                       PaymentMethod = e.PaymentMethod,
                                       Note = e.Note,
                                       Photo = e.Photo,
                                       Date = e.CreatedAt,
                                       CreatedByName = e.Users.FullName
                                   })
                                   .ToListAsync() ?? new List<ExpenseViewModel>();
            Trace.WriteLine($"取得支出資料：支出數量={expenses.Count}");

            // 取得群組債務
            var debts = await db.Debts
                                .Where(d => d.GroupId == groupId)
                                .ToListAsync() ?? new List<Debts>();
            Trace.WriteLine($"取得債務資料：債務數量={debts.Count}");

            // 成員債務統計
            var memberDebtOverview = members.Select(user => new MemberDebtViewModel
            {
                UserId = user.UserId,
                UserName = user.FullName,
                TotalOwed = debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0,
                TotalOwedTo = debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0,
                NetDebt = (debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0)
                          - (debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0)
            }).ToList();
            Trace.WriteLine("成員債務計算完成");

            // 成員間債務細節
            var pairwiseDebts = new List<PairwiseDebtViewModel>();
            foreach (var debtor in memberDebtOverview.Where(m => m.NetDebt < 0))
            {
                foreach (var creditor in memberDebtOverview.Where(m => m.NetDebt > 0))
                {
                    var debtAmount = Math.Min(-debtor.NetDebt, creditor.NetDebt);
                    if (debtAmount > 0)
                    {
                        pairwiseDebts.Add(new PairwiseDebtViewModel
                        {
                            DebtorName = debtor.UserName,
                            CreditorName = creditor.UserName,
                            Amount = debtAmount,
                            DebtorId = debtor.UserId,
                            CreditorId = creditor.UserId,
                            DebtId = debts.FirstOrDefault(d => d.DebtorId == debtor.UserId && d.CreditorId == creditor.UserId && d.Amount == debtAmount)?.DebtId ?? 0
                        });

                        Trace.WriteLine($"新增成員間債務：{debtor.UserName} 欠 {creditor.UserName} {debtAmount} 元");

                        debtor.NetDebt += debtAmount;
                        creditor.NetDebt -= debtAmount;
                    }
                }
            }

            // 成員支出統計
            var memberExpenses = members.Select(m => new MemberExpenseViewModel
            {
                FullName = m.FullName,
                TotalAmount = m.TotalSpent
            }).ToList();
            Trace.WriteLine("成員支出計算完成");

            // 組裝 ViewModel
            var viewModel = new GroupDetailsViewModel
            {
                GroupName = group.GroupName,
                Currency = group.Currency,
                CreateDate = group.CreatedDate.HasValue ? group.CreatedDate.Value : DateTime.MinValue,
                GroupPhoto = group.GroupsPhoto ?? "/path/to/default/photo.jpg",
                Budget = group.Budget.HasValue ? group.Budget.Value : 0,
                Members = members,
                Expenses = expenses,
                MemberExpenses = memberExpenses,
                DebtSettlements = pairwiseDebts
            };
            Trace.WriteLine("ViewModel 準備完成");

            // 生成 PDF
            try
            {
                // 建立 PDF 結果
                var pdfResult = new Rotativa.ViewAsPdf("~/Views/Export/ExportGroupDetailsToPdf.cshtml", viewModel)
                {
                    FileName = $"{group.GroupName}_Details.pdf",
                    PageSize = Rotativa.Options.Size.A4,
                    PageOrientation = Rotativa.Options.Orientation.Portrait
                };

                // 設定保存 PDF 的路徑
                //var pdfFileName = $"{viewModel.GroupName}_Report.pdf";
                var pdfFileName = $"{viewModel.GroupName.Replace(",", "_")}_Report.pdf";
                var pdfFilePath = Path.Combine(Server.MapPath("~/GeneratedReports"), pdfFileName);
                Trace.WriteLine($"PDF 儲存路徑：{pdfFilePath}");

             
                    // 生成 PDF 文件
                    var pdfBytes = pdfResult.BuildFile(ControllerContext);
                    if (ControllerContext == null)
                    {
                        Trace.WriteLine("ControllerContext 為空，無法生成 PDF。");
                        return "系統錯誤，無法生成報表。";
                    }

                    if (pdfBytes == null)
                    {
                        Trace.WriteLine("PDF 生成失敗。");
                        return "報表生成失敗，請稍後再試。";
                    }

                    // 將 PDF 保存至伺服器
                    using (var fileStream = new FileStream(pdfFilePath, FileMode.Create))
                    {
                        await fileStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                    }
                    Trace.WriteLine("PDF 生成並儲存成功。");
                

                // 建立下載 URL
                var pdfUrl = $"https://justsplit.imd.pccu.edu.tw/GeneratedReports/{pdfFileName}";
                Trace.WriteLine($"PDF 下載連結：{pdfUrl}");

                // 返回 URL 給使用者
                return $"{pdfUrl}";
            }
            catch (Exception ex)
            {
                // 捕捉任何錯誤並回傳錯誤訊息
                Trace.WriteLine($"生成或保存 PDF 時發生錯誤：{ex.Message}");
                return "生成或保存 PDF 時發生錯誤，請稍後再試。";
            }
        }

        private async Task<AddExpenseResult> AddExpense(string chatId, string userId, string item, decimal totalAmount)
        {
            var result = new AddExpenseResult(); // 初始化回傳結果物件
            try
            {
                Trace.WriteLine("開始新增支出...");
                Trace.WriteLine($"參數：chatId={chatId}, userId={userId}, item={item}, totalAmount={totalAmount}");

                // 透過 userId 和 chatId 查詢群組 ID
                int groupId = GetGroupIdForUserInChat(userId, chatId);
                var user = db.Users.FirstOrDefault(u => u.LineUserId == userId);
                Trace.WriteLine($"取得群組ID：groupId={groupId}，使用者：{user?.FullName}");

                if (groupId == 0)
                {
                    // 如果群組 ID 為 0，表示該用戶不在任何群組中
                    result.IsSuccess = false;
                    result.Message = "您尚未加入任何群組，無法新增支出。";
                    await SendLineMessage(userId, result.Message);
                    return result;
                }

                // 查找群組成員
                var groupMembers = GetGroupMembers(groupId);
                int memberCount = groupMembers.Count;
                Trace.WriteLine($"取得群組成員成功，成員數量：{memberCount}");

                if (memberCount == 0)
                {
                    // 如果群組中沒有成員，則無法新增支出
                    result.IsSuccess = false;
                    result.Message = "群組中沒有其他成員，無法新增支出。";
                    await SendLineMessage(userId, result.Message);
                    return result;
                }

                // 每個成員分攤金額
                decimal splitAmount = totalAmount / memberCount;
                Trace.WriteLine($"每個成員分攤金額：{splitAmount}");

                // 建立支出記錄
                var expense = new Expenses
                {
                    GroupId = groupId, // 設定群組ID，表示這筆支出歸屬於哪個群組
                    CreatedBy = user.UserId, // 設定建立者ID，表示是誰新增了這筆支出
                    ExpenseItem = item,
                    TotalAmount = totalAmount,
                    ExpenseType = "預設類別", // 如果無法從 LINE 指令中取得類別，設置一個預設類別
                    PaymentMethod = "現金",  // 可以根據需要更改或自動推斷
                    CreatedAt = DateTime.Now,
                    Note = "自動新增的支出項目"
                };

                // 儲存支出到資料庫
                db.Expenses.Add(expense);
                db.SaveChanges();
                int expenseId = expense.ExpenseId;
                Trace.WriteLine($"支出記錄儲存成功，ExpenseId={expenseId}");

                // 儲存付款人（發起者）
                var expensePayer = new ExpensePayers
                {
                    ExpenseId = expenseId,
                    UserId = user.UserId, // 使用userId來表示這筆支出的發起者
                    Amount = totalAmount,  // 發起者支付所有金額
                };
                db.ExpensePayers.Add(expensePayer);

                // 儲存每個成員的分攤金額，並建立通知訊息
                var memberDetails = new List<MemberExpenseDetail>();
                foreach (var member in groupMembers)
                {
                    var expenseDetail = new ExpenseDetails
                    {
                        ExpenseId = expenseId,
                        UserId = member.UserId,
                        Amount = splitAmount,
                        Note = "自動分攤的支出",
                        CreatedAt = DateTime.Now
                    };
                    db.ExpenseDetails.Add(expenseDetail);

                    // 保存每個成員的分攤資訊到返回結果
                    memberDetails.Add(new MemberExpenseDetail
                    {
                        MemberName = member.FullName,
                        Amount = splitAmount
                    });

                    string lineUserId = member.LineUserId;
                    string message = $"您需要支付 {splitAmount} 元作為 '{item}' 的分攤金額。";
                    await SendLineMessage(lineUserId, message);
                }

                // 儲存所有變更
                db.SaveChanges();

                // 計算並儲存債務
                await CalculateAndSaveDebts(expenseId, userId, groupId, totalAmount, splitAmount);

                // 設定成功訊息
                result.IsSuccess = true;
                result.Message = $"支出 '{item}' 已成功新增，總金額為 {totalAmount} 元。";
                result.ExpenseId = expenseId;
                result.MemberDetails = memberDetails; // 回傳成員詳細資料
            }
            catch (Exception ex)
            {
                // 當發生例外狀況時，記錄並返回錯誤訊息
                Trace.WriteLine($"新增支出時發生錯誤：{ex.Message}");
                result.IsSuccess = false;
                result.Message = $"無法新增支出：{ex.Message}";
            }

            return result;
        }


        // 假設這個方法可以獲取群組成員列表



        private async Task CalculateAndSaveDebts(int expenseId, string payerUserId, int groupId, decimal totalAmount, decimal splitAmount)
        {
            Trace.WriteLine("開始計算並儲存債務...");
            Trace.WriteLine($"參數：expenseId={expenseId}, payerUserId={payerUserId}, groupId={groupId}, totalAmount={totalAmount}, splitAmount={splitAmount}");

            // 取得群組成員的付款和分攤明細
            var groupMembers = GetGroupMembers(groupId);
            if (groupMembers == null || !groupMembers.Any())
            {
                Trace.WriteLine($"無法取得群組成員，群組ID：{groupId}");
                return;
            }

            Trace.WriteLine($"取得群組成員成功，成員數量：{groupMembers.Count}");

            // 字典來記錄每個成員的債務情況
            var userDebts = new Dictionary<int, decimal>();

            // 將 payerUserId 轉換為 UserId
            var payer = groupMembers.FirstOrDefault(m => m.LineUserId == payerUserId);
            if (payer == null)
            {
                Trace.WriteLine($"找不到對應的付款人（LineUserId: {payerUserId}），無法計算債務。");
                return;
            }
            int payerUserIdInt = payer.UserId;

            // 初始化每個成員的債務為0
            foreach (var member in groupMembers)
            {
                userDebts[member.UserId] = 0;
                Trace.WriteLine($"初始化債務：成員 {member.FullName} (UserId: {member.UserId}) 的債務設為 0");
            }

            // 付款人應收款（發起者支付了全額，其他人需償還他們的部分）
            userDebts[payerUserIdInt] += totalAmount;
            Trace.WriteLine($"付款人應收款：UserId {payerUserIdInt} 應收款增加至 {totalAmount}");

            // 每個成員的分攤金額
            foreach (var member in groupMembers)
            {
                userDebts[member.UserId] -= splitAmount; // 減去每個成員需支付的部分
                Trace.WriteLine($"扣除分攤金額：成員 {member.FullName} (UserId: {member.UserId}) 需支付 {splitAmount} 元");
            }

            // 使用臨時集合來存儲待添加的債務記錄
            var debtsToAdd = new List<Debts>();

            // 正確計算債務：多支付者（債權人）需要得到錢，少支付者（債務人）需要支付錢
            foreach (var userDebt in userDebts.Where(d => d.Value > 0).ToList())
            {
                Trace.WriteLine($"多支付者：UserId {userDebt.Key}，需收款 {userDebt.Value}");
                foreach (var debtor in userDebts.Where(d => d.Value < 0).ToList())
                {
                    var debtAmount = Math.Min(userDebt.Value, -debtor.Value);
                    Trace.WriteLine($"少支付者：UserId {debtor.Key}，需付款 {debtAmount}");

                    var debt = new Debts
                    {
                        GroupId = groupId,
                        CreditorId = userDebt.Key,  // 多支付者
                        DebtorId = debtor.Key,      // 少支付者
                        Amount = debtAmount,
                        CreatedAt = DateTime.Now,
                        IsPaid = false,
                        ExpenseId = expenseId,  // 關聯的 ExpenseId
                    };

                    debtsToAdd.Add(debt);
                    Trace.WriteLine($"記錄債務：CreditorId = {debt.CreditorId}, DebtorId = {debt.DebtorId}, Amount = {debt.Amount}");

                    userDebts[userDebt.Key] -= debtAmount;
                    userDebts[debtor.Key] += debtAmount;
                    if (userDebts[userDebt.Key] == 0)
                        break;
                }
            }

            if (!debtsToAdd.Any())
            {
                Trace.WriteLine("未計算到任何債務，沒有新增債務記錄。");
                return;
            }

            // 將所有債務記錄儲存到資料庫
            Trace.WriteLine("開始將債務記錄儲存到資料庫...");
            try
            {
                db.Debts.AddRange(debtsToAdd);
                db.SaveChanges();
                Trace.WriteLine("債務記錄儲存成功！");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"儲存債務時發生錯誤：{ex.Message}");
            }

            // 向群組成員發送債務通知
            foreach (var debt in debtsToAdd)
            {
                var creditor = db.Users.Find(debt.CreditorId);
                var debtor = db.Users.Find(debt.DebtorId);

                string creditorMessage = $"您應該從 {debtor?.FullName} 收到 {debt.Amount} 元。";
                string debtorMessage = $"您需要支付 {debt.Amount} 元給 {creditor?.FullName}。";

                Trace.WriteLine($"發送通知：CreditorId = {creditor?.LineUserId}, Message = {creditorMessage}");
                Trace.WriteLine($"發送通知：DebtorId = {debtor?.LineUserId}, Message = {debtorMessage}");

                if (creditor != null)
                {
                    await SendLineMessage(creditor.LineUserId, creditorMessage);
                }
                if (debtor != null)
                {
                    await SendLineMessage(debtor.LineUserId, debtorMessage);
                }
            }
        }

     private List<Users> GetGroupMembers(int groupId)
        {
            return db.GroupMembers
                           .Where(gm => gm.GroupId == groupId)
                           .Select(gm => gm.Users)
                           .ToList();
        }

        // 發送 LINE 訊息的方法
        private async Task SendLineMessage(string userId, string message)
        {
            var lineMessage = new
            {
                to = userId,
                messages = new[]
                {
            new
            {
                type = "text",
                text = message
            }
        }
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);
                var content = new StringContent(JsonConvert.SerializeObject(lineMessage), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.line.me/v2/bot/message/push", content);
            }
        }


        // 计算净结余（净收入/净支出）
        public List<PairwiseDebtViewModel> CalculatePairwiseDebts(int groupId)
        {
            var members = db.GroupMembers
                                  .Where(gm => gm.GroupId == groupId)
                                  .Select(gm => gm.Users)
                                  .ToList();

            var debts = db.Debts
                                  .Where(d => d.GroupId == groupId && !d.IsPaid)
                                  .ToList();

            var pairwiseDebts = new List<PairwiseDebtViewModel>();
            var processedDebts = new HashSet<(int DebtorId, int CreditorId)>();  // 使用 HashSet 來跟踪已处理的债务对

            foreach (var debtor in members)
            {
                foreach (var creditor in members)
                {
                    if (debtor.UserId != creditor.UserId && !processedDebts.Contains((debtor.UserId, creditor.UserId)))
                    {
                        // 计算 debtor 欠 creditor 的总金额
                        var debtorToCreditor = debts
                            .Where(d => d.DebtorId == debtor.UserId && d.CreditorId == creditor.UserId)
                            .Sum(d => d.Amount);

                        // 计算 creditor 欠 debtor 的总金额
                        var creditorToDebtor = debts
                            .Where(d => d.DebtorId == creditor.UserId && d.CreditorId == debtor.UserId)
                            .Sum(d => d.Amount);

                        // 计算净债务，正值表示 debtor 仍然欠 creditor，负值表示 creditor 仍然欠 debtor
                        var netDebt = debtorToCreditor - creditorToDebtor;

                        if (netDebt > 0)
                        {
                            // Debtor 仍然欠 Creditor
                            pairwiseDebts.Add(new PairwiseDebtViewModel
                            {
                                DebtorName = debtor.FullName,
                                CreditorName = creditor.FullName,
                                Amount = netDebt,
                                DebtId = debts.FirstOrDefault(d => d.DebtorId == debtor.UserId && d.CreditorId == creditor.UserId)?.DebtId ?? 0
                            });

                            // 標記這筆债务已處理
                            processedDebts.Add((debtor.UserId, creditor.UserId));
                            processedDebts.Add((creditor.UserId, debtor.UserId));  // 标记相反的组合为已处理
                        }
                        else if (netDebt < 0)
                        {
                            // Creditor 反而欠 Debtor
                            pairwiseDebts.Add(new PairwiseDebtViewModel
                            {
                                DebtorName = creditor.FullName,
                                CreditorName = debtor.FullName,
                                Amount = -netDebt,
                                DebtId = debts.FirstOrDefault(d => d.DebtorId == creditor.UserId && d.CreditorId == debtor.UserId)?.DebtId ?? 0
                            });

                            // 標記這筆债务已處理
                            processedDebts.Add((debtor.UserId, creditor.UserId));
                            processedDebts.Add((creditor.UserId, debtor.UserId));  // 标记相反的组合为已处理
                        }
                        // 如果 netDebt == 0，表示双方债务相抵消，无需记录。
                    }
                }
            }

            return pairwiseDebts;
        }


        private async Task SendReminderMessagesToDebtorsAsync(int creditorId, string replyToken, int groupId)
        {
            // 计算相抵后的债务
            var pairwiseDebts = CalculatePairwiseDebts(groupId);
            var group = db.Groups.Where(d=>d.GroupId ==groupId).FirstOrDefault();
            // 过滤出当前 creditorId 相关的债务记录
            var relevantDebts = pairwiseDebts
                .Where(d => db.Users.Any(u => u.UserId == creditorId && u.FullName == d.CreditorName))
                .ToList();

            if (!relevantDebts.Any())
            {
                // 如果没有欠款，回复一条消息告知用户没有未支付的欠款
                var noDebtMessage = new TextMessage("目前沒有未支付的欠款。");
                await _lineMessagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { noDebtMessage });
                return;
            }

            // 向每个债务人发送催款消息
            foreach (var debt in relevantDebts)
            {
                // 找到债务人和债权人
                var debtor = db.Users.FirstOrDefault(u => u.FullName == debt.DebtorName);
                var creditorName = db.Users.FirstOrDefault(d => d.UserId == creditorId);

                // 确认债务人存在并且债务人有LineUserId
                if (debtor != null && !string.IsNullOrEmpty(debtor.LineUserId))
                {
                    // 创建催款消息
                    var reminderMessage = new TextMessage($"⚠️  {creditorName.FullName}提醒您欠款 {debt.Amount:F0} 元，請盡快支付，來自 {group.GroupName} 前往繳費🔗： https://liff.line.me/2006127909-ObP6XZR9/Settle/PairwiseSettlement?groupId={groupId}");
                    
                    try
                    {
                        // 向债务人发送催款消息
                        await _lineMessagingClient.PushMessageAsync(debtor.LineUserId, new List<ISendMessage> { reminderMessage });
                        Console.WriteLine($"催款消息已發送給: {debtor.FullName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"發送消息時發生錯誤: {ex.Message}");
                    }
                }
            }

            // 构建汇总消息
            var summaryMessage = new StringBuilder();
            summaryMessage.AppendLine("💰 欠款提醒 💰");
            summaryMessage.AppendLine("----------------------");

            foreach (var debt in relevantDebts)
            {
                // 加入欠款信息
                summaryMessage.AppendLine($"{debt.DebtorName} 欠{debt.CreditorName} {debt.Amount:F0} 元，請盡快支付。");
            }
            summaryMessage.AppendLine($"🔗： https://liff.line.me/2006127909-ObP6XZR9/Settle/PairwiseSettlement?groupId={groupId}");

            // 发送汇总消息到聊天室
            var replyMessage = new TextMessage(summaryMessage.ToString());
            await _lineMessagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage });
        }



        private async Task SendDetailedSettlementAsTextAsync(LineMessagingClient client, string replyToken, List<PairwiseDebtViewModel> pairwiseDebts, int groupid)
        {
            if (pairwiseDebts == null || !pairwiseDebts.Any())
            {
                var noDataMessage = new TextMessage("目前沒有任何結算資料。");
                await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { noDataMessage });
                return;
            }

            // 加入結算日期
            var summaryMessage = new StringBuilder();
            var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            summaryMessage.AppendLine("💰結算報告");
            summaryMessage.AppendLine($"📅結算日期：{currentDate}");
            // 加入總金額統計
            var totalDebt = pairwiseDebts.Sum(d => d.Amount);
            summaryMessage.AppendLine($"🔢總金額： ＄{totalDebt:F0}");



            summaryMessage.AppendLine("----------------------");
            foreach (var debt in pairwiseDebts.OrderByDescending(d => d.Amount))  // 根據金額排序
            {
                summaryMessage.AppendLine($"⭐️ {debt.DebtorName} 欠 {debt.CreditorName} 💵 ＄{debt.Amount:F0}");


            }
            // 按使用者統計
            summaryMessage.AppendLine("👥組員欠款統計");
            var userDebts = pairwiseDebts
                .GroupBy(d => d.DebtorName)
                .Select(g => new { UserName = g.Key, TotalOwed = g.Sum(d => d.Amount) })
                .ToList();

            foreach (var user in userDebts)
            {
                summaryMessage.AppendLine($"⭐️{user.UserName}： 欠款 ＄{user.TotalOwed:F0}");
            }
            summaryMessage.AppendLine($"🔗： https://liff.line.me/2006127909-ObP6XZR9/Settle/PairwiseSettlement?groupId={groupid}");
            // 回傳訊息給用戶
            var replyMessage = new TextMessage(summaryMessage.ToString());
            await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage });
        }


        public List<Debts> GetGroupDebts(int groupId)
        {
            return db.Debts
                     .Where(d => d.GroupId == groupId && !d.IsPaid)  // 查询属于该群组且未支付的账目
                     .ToList();
        }

        private async Task SendGroupSummaryAsTextAsync(LineMessagingClient client, string replyToken, List<Expenses> expenses, List<Debts> debts, int groupId)
        {
            if (expenses == null || !expenses.Any())
            {
                var noDataMessage = new TextMessage("⚠️ 目前沒有任何帳目資料。");
                await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { noDataMessage });
                return;
            }

            var summaryMessage = new StringBuilder();

            // 總金額與總筆數
            var totalAmount = expenses.Sum(e => e.TotalAmount);
            var totalCount = expenses.Count;
            var averageAmount = totalAmount / expenses.SelectMany(e => e.ExpenseDetails).GroupBy(ed => ed.Users.FullName).Count();
            var earliestExpenseDate = expenses.Min(e => e.CreatedAt)?.ToString("yyyy-MM-dd") ?? "未知";
            var latestExpenseDate = expenses.Max(e => e.CreatedAt)?.ToString("yyyy-MM-dd") ?? "未知";
            summaryMessage.AppendLine("💰消費總覽");
            summaryMessage.AppendLine($" {earliestExpenseDate} ~ {latestExpenseDate}");
            summaryMessage.AppendLine($"🪄總金額： ＄{totalAmount:F0}");
            summaryMessage.AppendLine($"🪄總筆數： {totalCount} 筆");
            summaryMessage.AppendLine($"🪄人均消費： ＄{averageAmount:F0}");



            summaryMessage.AppendLine("----------------------");

            // 消費類型統計
            summaryMessage.AppendLine("📊消費類型統計");
            var expenseTypeData = expenses
                .GroupBy(e => e.ExpenseType)
                .Select(g => new { Type = g.Key, Count = g.Count(), TotalAmount = g.Sum(e => e.TotalAmount) })
                .ToList();

            foreach (var type in expenseTypeData)
            {
                summaryMessage.AppendLine($"🪄{type.Type}： {type.Count} 筆, ＄ {type.TotalAmount:F0}");
            }
            summaryMessage.AppendLine("----------------------");

            // 組員消費統計
            summaryMessage.AppendLine("👥組員消費統計");
            var userExpenseData = expenses
                .SelectMany(e => e.ExpenseDetails)
                .GroupBy(ed => ed.Users.FullName)
                .Select(g => new { UserName = g.Key, TotalAmount = g.Sum(ed => ed.Amount) })
                .ToList();

            foreach (var userExpense in userExpenseData)
            {
                summaryMessage.AppendLine($" ⭐️{userExpense.UserName}： ＄ {userExpense.TotalAmount:F0}");
            }
            summaryMessage.AppendLine("----------------------");

            // 最高與最低消費
            var highestExpense = expenses.OrderByDescending(e => e.TotalAmount).FirstOrDefault();
            var lowestExpense = expenses.OrderBy(e => e.TotalAmount).FirstOrDefault();

            if (highestExpense != null && lowestExpense != null)
            {
                summaryMessage.AppendLine("🏆消費最高與最低");
                summaryMessage.AppendLine($"🪄最高消費： {highestExpense.ExpenseType}:{highestExpense.ExpenseItem} $ {highestExpense.TotalAmount:F0}");
                summaryMessage.AppendLine($"於 {highestExpense.CreatedAt?.ToString("yyyy-MM-dd")}");
                summaryMessage.AppendLine($"🪄最低消費： {lowestExpense.ExpenseType}：{lowestExpense.ExpenseItem} $ {lowestExpense.TotalAmount:F0}");
                summaryMessage.AppendLine($"於 {lowestExpense.CreatedAt?.ToString("yyyy-MM-dd")}");
            }
            summaryMessage.AppendLine("----------------------");

            // 消費時間段統計
            var currentMonthExpenses = expenses.Where(e => e.CreatedAt?.Month == DateTime.Now.Month).Sum(e => e.TotalAmount);
            var lastMonthExpenses = expenses.Where(e => e.CreatedAt?.Month == DateTime.Now.AddMonths(-1).Month).Sum(e => e.TotalAmount);

            summaryMessage.AppendLine("📅消費時間段統計");
            summaryMessage.AppendLine($" 🪄本月消費總額： ＄ {currentMonthExpenses:F0}");
            summaryMessage.AppendLine($" 🪄上月消費總額： ＄ {lastMonthExpenses:F0}");
            summaryMessage.AppendLine("----------------------");

            // 未支付項目統計
            summaryMessage.AppendLine("🚨未支付項目統計");
            var unpaidDebts = debts
                .Where(d => !d.IsPaid)
                .GroupBy(d => d.DebtorId)
                .Select(g => new { UserId = g.Key, TotalUnpaidAmount = g.Sum(d => d.Amount) })
                .ToList();

            var unpaidMembers = unpaidDebts
                .Join(db.Users, debt => debt.UserId, user => user.UserId, (debt, user) => new { user.FullName, debt.TotalUnpaidAmount })
                .ToList();

            if (unpaidMembers.Any())
            {
                foreach (var member in unpaidMembers)
                {
                    summaryMessage.AppendLine($" ⭐️{member.FullName}： ＄ {member.TotalUnpaidAmount:F0} 未支付");
                }
            }
            else
            {
                summaryMessage.AppendLine("✅所有帳目已支付。");
            }
            summaryMessage.AppendLine("----------------------");

            // 組員淨利統計
            summaryMessage.AppendLine("⚖️組員淨利統計");
            var userDebts = debts
                .GroupBy(d => d.DebtorId)
                .Select(g => new { UserId = g.Key, TotalDebt = g.Sum(d => d.Amount) })
                .ToList();

            var userCredits = debts
                .GroupBy(d => d.CreditorId)
                .Select(g => new { UserId = g.Key, TotalCredit = g.Sum(d => d.Amount) })
                .ToList();

            var allUsers = userDebts.Select(d => d.UserId).Union(userCredits.Select(c => c.UserId)).Distinct();

            foreach (var userId in allUsers)
            {
                var userName = db.Users.FirstOrDefault(u => u.UserId == userId)?.FullName ?? "未知用戶";
                var totalDebt = userDebts.FirstOrDefault(d => d.UserId == userId)?.TotalDebt ?? 0m;
                var totalCredit = userCredits.FirstOrDefault(c => c.UserId == userId)?.TotalCredit ?? 0m;
                var netIncome = totalCredit - totalDebt;

                // 根據淨利顯示應收或應付
                string netIncomeDisplay = netIncome > 0
                    ? $"應收： ＄ {netIncome:F0}"
                    : netIncome < 0
                    ? $"應付： ＄ {Math.Abs(netIncome):F0}"
                    : "⚖️ 無應收或應付";

                summaryMessage.AppendLine($" ⭐️{userName}： {netIncomeDisplay}");
            }
            summaryMessage.AppendLine("----------------------");
            summaryMessage.AppendLine($"🔗： https://liff.line.me/2006127909-ObP6XZR9/Chart/GroupExpenseStatistics?groupId={groupId}");

            // 回傳訊息給用戶
            var replyMessage = new TextMessage(summaryMessage.ToString());
            await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage });
        }

        private async Task SendChartAsFlexMessageAsync(LineMessagingClient client, string replyToken, string chartUrl)
        {
            var bubble = new BubbleContainer
            {
                Hero = new ImageComponent
                {
                    Url = chartUrl,  // 設定生成的圖表 URL
                    Size = ComponentSize.Full,
                    AspectRatio = AspectRatio._3_4,
                    AspectMode = AspectMode.Cover,
                },
                Body = new BoxComponent
                {
                    Layout = BoxLayout.Vertical,
                    Contents = new List<IFlexComponent>
            {
                new TextComponent
                {
                    Text = "帳目統計圖表",

                    Size = ComponentSize.Xs,
                    Align = Align.Center
                }
            }
                }
            };

            var flexMessage = new FlexMessage("帳目統計")
            {
                Contents = bubble
            };

            await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { flexMessage });
        }

        public void GenerateChart(List<Expenses> expenses, string filePath)
        {
            // 统计每种类型的账目总金额
            var expenseData = expenses
                .GroupBy(e => e.ExpenseType)
                .Select(g => new { Type = g.Key, TotalAmount = g.Sum(e => e.TotalAmount) })
                .ToList();

            // 檢查數據是否存在
            if (expenseData == null || !expenseData.Any())
            {
                throw new Exception("沒有帳目資料來生成圖表");
            }

            var chart = new Chart(width: 250, height: 250)
                .AddTitle("帳目統計")
                .AddLegend()
                .AddSeries(
                    chartType: "pie",
                    xValue: expenseData.Select(e => e.Type).ToArray(),
                    yValues: expenseData.Select(e => e.TotalAmount).ToArray());

            // 將圖表保存為圖片 (png 格式)
            chart.Save(filePath, format: "png");
        }
        public List<Expenses> GetGroupExpenses(int groupId)
        {

            return db.Expenses
                     .Where(e => e.GroupId == groupId)
                     .OrderBy(e => e.CreatedAt)
                     .ToList();

        }
        private async Task SendAccountSummaryAsFlexMessageAsync(LineMessagingClient client, string replyToken, List<Expenses> expenses)
        {
            if (expenses == null || !expenses.Any())
            {
                // 如果 expenses 為空或沒有任何分帳
                var noDataMessage = new TextMessage("目前沒有任何分帳資料。");
                await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { noDataMessage });
                return;
            }

            var bubbles = new List<BubbleContainer>();
            var sortedExpenses = expenses.OrderByDescending(e => e.CreatedAt).ToList();

            foreach (var expense in sortedExpenses)
            {
                // 获取付款人列表
                var payers = expense.ExpensePayers.Select(p => p.Users.FullName).ToList();
                var payerNames = string.Join(", ", payers);
                var creatorName = db.Users
     .Where(u => u.UserId == expense.CreatedBy)  // 查找 CreatedBy 对应的用户
     .Select(u => u.FullName)  // 获取用户的 FullName
     .FirstOrDefault();  // 如果找不到，返回 null

                // 获取分攤人列表 (ExpenseDetails)
                var splitters = expense.ExpenseDetails.Select(ed => ed.Users.FullName).ToList();
                var splitterNames = string.Join(", ", splitters);

                // 如果没有照片，使用默认图片
                var photoUrl = string.IsNullOrEmpty(expense.Photo)
                ? $"{localhost}/Images/沒照片.png"
                : $"{localhost}{expense.Photo}";
                var totalAmountInteger = Math.Floor(expense.TotalAmount);
                var bubble = new BubbleContainer
                {
                    Hero = new ImageComponent
                    {
                        BackgroundColor = "#F5F5F5",
                        Url = photoUrl,  // 设置账目照片 URL
                        Size = ComponentSize.Full,
                        AspectRatio = AspectRatio._1_1,
                        AspectMode = AspectMode.Cover,
                    },
                    Body = new BoxComponent
                    {
                        BackgroundColor = "#F5F5F5", // 设置卡片的背景颜色（浅灰色）
                        Layout = BoxLayout.Vertical,
                        Contents = new List<IFlexComponent>
                {
                    new TextComponent
                    {
                        Text = $"{expense.ExpenseItem}",
                        Weight = Weight.Bold,
                        Size = ComponentSize.Xl,
                        Align = Align.Center
                    },
                    new TextComponent
                    {
                        Text = $"金額: $ {totalAmountInteger}",
                        Size = ComponentSize.Lg,  // 放大金額字樣
                        Weight = Weight.Bold,
                        Color = "#FF4500",  // 橘色字樣
                        Align = Align.Center
                    },
                    new SeparatorComponent
                    {
                        Margin = Spacing.Md
                    },
                     // 付款人和分攤人放在同一行
                    new BoxComponent
                    {
                        Layout = BoxLayout.Baseline,  // 使用 Baseline 布局
                        Contents = new List<IFlexComponent>
                        {
                            new TextComponent
                            {
                                Text = $"付款人: {payerNames}",
                                Size = ComponentSize.Sm,
                                Color = "#888888",
                                Flex = 1  // 设置 Flex，使两段文字均匀分布
                            },
                            new TextComponent
                            {
                                Text = $"分攤人: {splitterNames}",
                                Size = ComponentSize.Sm,
                                Color = "#888888",
                                Flex = 1  // 设置 Flex，使两段文字均匀分布
                            }
                        }
                    },      new SeparatorComponent
                    {
                        Margin = Spacing.Md
                    },
                      // 付款人和分攤人放在同一行
                    new BoxComponent
                    {
                        Layout = BoxLayout.Baseline,  // 使用 Baseline 布局
                        Contents = new List<IFlexComponent>
                        {
                            new TextComponent
                            {
                                Text = $"類型: {expense.ExpenseType}",
                                Size = ComponentSize.Sm,
                                Color = "#888888",
                                Flex = 1  // 设置 Flex，使两段文字均匀分布
                            },
                            new TextComponent
                            {
                             Text = $"支付: {expense.PaymentMethod}",
                                Size = ComponentSize.Sm,
                                Color = "#888888",
                                Flex = 1  // 设置 Flex，使两段文字均匀分布
                            }
                        }
                    },
                     // 付款人和分攤人放在同一行
                    new BoxComponent
                    {
                        Layout = BoxLayout.Baseline,  // 使用 Baseline 布局
                        Contents = new List<IFlexComponent>
                        {
                            new TextComponent
                            {
                          Text = $"日期: {expense.CreatedAt?.ToString("yyyy-MM-dd")}",
                                Size = ComponentSize.Sm,
                                Color = "#888888",
                                Flex = 1  // 设置 Flex，使两段文字均匀分布
                            },
                            new TextComponent
                            {
                             Text = $"建立者: {creatorName}",
                                Size = ComponentSize.Sm,
                                Color = "#888888",
                                Flex = 1  // 设置 Flex，使两段文字均匀分布
                            }
                        }
                    },

                }
                    },
                    Footer = new BoxComponent
                    {
                        BackgroundColor = "#F5F5F5",
                        Layout = BoxLayout.Vertical,
                        Contents = new List<IFlexComponent>
                {
                    new ButtonComponent
                    {   Style = ButtonStyle.Primary,  // 设置按钮样式
                        Color = "#007BFF",  // 设置按钮底色（蓝色）
                  
                        Action = new UriTemplateAction("帳目詳情", $"https://liff.line.me/2006127909-ObP6XZR9/expense/details/{expense.ExpenseId}")
                    }
                }
                    },

                };

                bubbles.Add(bubble);
            }

            var carousel = new CarouselContainer
            {
                Contents = bubbles
            };

            var flexMessage = new FlexMessage("帳目總覽")
            {
                Contents = carousel
            };

            await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { flexMessage });
        }

        // 发送 Flex Message 的方法
        private async Task SendFlexMessageAsync(LineMessagingClient client, string replyToken)
        {
            var bubble = new BubbleContainer
            {
                Body = new BoxComponent
                {
                    Layout = BoxLayout.Vertical,
                    Contents = new List<IFlexComponent>
            {
                new TextComponent
                {
                    Text = "Flex Message Test",
                    Weight = Weight.Bold,
                    Size = ComponentSize.Xl,
                    Align = Align.Center
                },
                new TextComponent
                {
                    Text = "This is a test of Flex Message",
                    Size = ComponentSize.Sm,
                    Align = Align.Center,
                    Color = "#888888"
                }
            }
                },
                Footer = new BoxComponent
                {
                    Layout = BoxLayout.Vertical,
                    Spacing = Spacing.Sm,
                    Contents = new List<IFlexComponent>
            {
                new ButtonComponent
                {
                    Style = ButtonStyle.Primary,
                    Action = new UriTemplateAction("Visit Website", "https://example.com")
                }
            }
                }
            };

            var flexMessage = new FlexMessage("Flex Message Example")
            {
                Contents = bubble
            };

            // 回复 Flex Message
            await client.ReplyMessageAsync(replyToken, new List<ISendMessage> { flexMessage });
        }
        private int GetGroupIdForUserInChat(string lineUserId, string chatId)
        {
            // 1. 根據 LineUserId 查找對應的 User
            var user = db.Users.FirstOrDefault(u => u.LineUserId == lineUserId);
            Session["user"] = user;
            Session["chatId"] = chatId;
            // 2. 如果該用戶不存在，則返回 0 或者其他處理
            if (user == null)
            {
                // 可以選擇拋出例外或者回傳 0，表示沒有找到用戶
                return 0;  // 沒有找到對應用戶
            }

            // 3. 查找對應的群組（根據聊天室ID找到對應的群組）
            var group = db.Groups.FirstOrDefault(g => g.ChatId == chatId);
            // 4. 如果該群組不存在，返回 0
            if (group == null)
            {
                return 0; // 沒有找到對應的群組
            }

            // 5. 確認該用戶是否是該群組的成員
            var groupMember = db.GroupMembers.FirstOrDefault(gm => gm.UserId == user.UserId && gm.GroupId == group.GroupId);

            // 6. 如果該用戶是該群組的成員，返回 GroupId，否則返回 0
            return groupMember?.GroupId ?? 0;
        }

        private async Task<string> JoinGroupAsync(string lineUserId, string chatId)
        {
            // 1. 使用 LineMessagingClient 的 GetProfileAsync 方法來獲取使用者的名稱
            var userProfile = await _lineMessagingClient.GetUserProfileAsync(lineUserId);
            var userDisplayName = userProfile.DisplayName;  // 使用者的 LINE 顯示名稱

            // 2. 檢查用戶是否已經存在於 Users 表
            var user = db.Users.FirstOrDefault(u => u.LineUserId == lineUserId);

            // 3. 如果用戶不存在，新增用戶資料
            if (user == null)
            {
                user = new Users
                {
                    LineUserId = lineUserId,
                    ProfilePhoto = userProfile.PictureUrl,
                    FullName = userDisplayName,
                    RegistrationDate = DateTime.Now,
                    Email = $"{lineUserId}@example.com",  // 預設的 email
                    PasswordHash = "defaultPassword123",  // 生成預設密碼哈希
                    Role="Member",
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var jsonInfo = await GetGroupNameAsync(chatId);

            var json = JObject.Parse(jsonInfo);
            var groupName = json["groupName"].ToString() +"(Line)";
            var pictureUrl = json["pictureUrl"]?.ToString();
            Trace.WriteLine($"群組名稱: {groupName}, 圖片URL: {pictureUrl}");


            // 5. 檢查群組是否已經存在
            var group = db.Groups.FirstOrDefault(g => g.ChatId == chatId);

            // 6. 如果群組不存在，創建一個新群組
            if (group == null)
            {
                group = new Groups
                {
                    GroupsPhoto = pictureUrl,
                    GroupName = groupName,
                    CreatorId = user.UserId,
                    Currency = "TWD",
                    CreatedDate = DateTime.Now,
                    JoinLink = Guid.NewGuid().ToString(),
                    Description = "自動創建的群組Line",
                    ChatId = chatId  // 保存群组的 chatId
                };

                db.Groups.Add(group);
                await db.SaveChangesAsync();
            }
            else
            {
                // 更新群組的名稱和圖片 URL
                group.GroupName = groupName;
                group.GroupsPhoto = pictureUrl;
                db.Groups.AddOrUpdate(group);
                await db.SaveChangesAsync();
            }
            var existingMembers = db.GroupMembers.Where(gm => gm.GroupId == group.GroupId).ToList();
            var role = existingMembers.Count == 0 ? "Creator" : "Editor";

            // 7. 檢查該用戶是否已經加入該群組
            var isMemberExists = db.GroupMembers.Any(gm => gm.GroupId == group.GroupId && gm.UserId == user.UserId);

            if (isMemberExists)
            {
                // 用戶已經在群組中，回傳相應訊息
                return $"您已經在群組：{group.GroupName} 中";
            }

            // 8. 如果不是成員，將其加入群組
            var groupMember = new GroupMembers
            {
                GroupId = group.GroupId,
                UserId = user.UserId,
                Role = role,
                JoinedDate = DateTime.Now
            };

            db.GroupMembers.Add(groupMember);
            await db.SaveChangesAsync();

            return $"您已成功加入群組：{group.GroupName} ";
        }

        public async Task<string> GetGroupNameAsync(string groupId)
        {
            using (var client = new HttpClient())
            {
                // 設置 Authorization 標頭，使用您的 Channel Access Token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);

                // 建立 API 呼叫網址
                var url = $"https://api.line.me/v2/bot/group/{groupId}/summary";
                Trace.WriteLine($"即將發送 GET 請求至: {url}");

                try
                {
                    // 發送 GET 請求
                    var response = await client.GetAsync(url);
                    Trace.WriteLine($"收到 HTTP 回應狀態碼: {response.StatusCode}");

                    // 確認回應是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Trace.WriteLine("成功取得群組資料: " + content);

                        // 解析 JSON 資料
                        var json = JObject.Parse(content);
                        var groupName = json["groupName"].ToString();
                        var PictureUrl = json["pictureUrl"]?.ToString();
                        Trace.WriteLine($"群組名稱: {groupName}");
                        return content; // 直接回傳 JSON 字串
                    }
                    else
                    {
                        // 進一步處理不同的 HTTP 錯誤狀態碼
                        Trace.WriteLine($"HTTP 錯誤: {response.StatusCode} - {response.ReasonPhrase}");
                        switch (response.StatusCode)
                        {
                            case System.Net.HttpStatusCode.Forbidden: // 403
                                Trace.WriteLine("權限不足，無法存取該群組資訊。");
                                break;
                            case System.Net.HttpStatusCode.NotFound: // 404
                                Trace.WriteLine("找不到該群組，請檢查群組ID是否正確。");
                                break;

                            default:
                                Trace.WriteLine($"未處理的錯誤: {response.StatusCode} - {response.ReasonPhrase}");
                                break;
                        }
                        return $"無法取得群組名稱";
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    // 特定處理 HTTP 請求異常
                    Trace.WriteLine($"HttpRequestException: {httpEx.Message}");
                    return "網路請求異常，無法取得群組名稱";
                }
                catch (Exception ex)
                {
                    // 錯誤處理：其他例外狀況
                    Trace.WriteLine($"Exception: {ex.Message}, 堆疊追蹤: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Trace.WriteLine($"內部例外: {ex.InnerException.Message}");
                    }
                    return $"無法取得群組名稱";
                }
            }
        }

        //private async Task<string> JoinGroupAsync(string lineUserId, string chatId)
        //{
        //    // 1. 使用 LineMessagingClient 的 GetProfileAsync 方法來獲取使用者的名稱
        //    var userProfile = await _lineMessagingClient.GetUserProfileAsync(lineUserId);
        //    var userDisplayName = userProfile.DisplayName;  // 使用者的 LINE 顯示名稱

        //    // 2. 檢查用戶是否已經存在於 Users 表
        //    var user = db.Users.FirstOrDefault(u => u.LineUserId == lineUserId);

        //    // 3. 如果用戶不存在，新增用戶資料
        //    if (user == null)
        //    {
        //        user = new Users
        //        {
        //            LineUserId = lineUserId,
        //            ProfilePhoto = userProfile.PictureUrl,
        //            FullName = userDisplayName,
        //            RegistrationDate = DateTime.Now,
        //            Email = $"{lineUserId}@example.com",  // 預設的 email
        //            PasswordHash = "defaultPassword123",  // 生成預設密碼哈希
        //        };

        //        db.Users.Add(user);
        //        await db.SaveChangesAsync();
        //    }

        //    // 4. 動態生成群組名稱，使用 chatId 來保證唯一性
        //    string groupName = $"{userDisplayName}的專案群組";  // 使用 ChatId 生成唯一的群組名稱

        //    // 5. 檢查該聊天室唯一群組是否已經存在
        //    // 5. 檢查群組是否已經存在
        //    var group = db.Groups.FirstOrDefault(g => g.ChatId == chatId);

        //    // 6. 如果群組不存在，創建一個新群組
        //    if (group == null)
        //    {
        //        group = new Groups
        //        {
        //            GroupName = groupName,
        //            CreatorId = user.UserId,
        //            Currency = "TWD",
        //            CreatedDate = DateTime.Now,
        //            JoinLink = Guid.NewGuid().ToString(),
        //            Description = "自動創建的群組",
        //            ChatId = chatId  // 保存群组的 chatId
        //        };

        //        db.Groups.Add(group);
        //        await db.SaveChangesAsync();
        //    }
        //    var existingMembers = db.GroupMembers.Where(gm => gm.GroupId == group.GroupId).ToList();
        //    var role = existingMembers.Count == 0 ? "Creator" : "Editor";
        //    // 7. 檢查該用戶是否已經加入該群組
        //    var isMemberExists = db.GroupMembers.Any(gm => gm.GroupId == group.GroupId && gm.UserId == user.UserId);

        //    if (isMemberExists)
        //    {
        //        // 用戶已經在群組中，回傳相應訊息
        //        return $"您已經在群組：{group.GroupName} 中";
        //    }

        //    // 8. 如果不是成員，將其加入群組
        //    var groupMember = new GroupMembers
        //    {
        //        GroupId = group.GroupId,
        //        UserId = user.UserId,
        //        Role = role,
        //        JoinedDate = DateTime.Now
        //    };

        //    db.GroupMembers.Add(groupMember);
        //    await db.SaveChangesAsync();

        //    return $"您已成功加入群組：{group.GroupName} ";
        //}



        private async Task HandleMemberJoinEventAsync(MemberJoinEvent memberJoinedEvent)
        {
            foreach (var member in memberJoinedEvent.Joined.Members)
            {
                var groupId = memberJoinedEvent.Source.Id;
                var userId = member.UserId;
                try
                {
                    var userProfile = await _lineMessagingClient.GetGroupMemberProfileAsync(groupId, userId);

                    if (userProfile != null)
                    {
                        var displayName = userProfile.DisplayName;
                        var replyMessage = new List<ISendMessage>
                    {
                        new TextMessage($"歡迎 {displayName} 加入群組！，嘗試呼叫 地瓜 使用更多功能")
                    };
                        await _lineMessagingClient.ReplyMessageAsync(memberJoinedEvent.ReplyToken, replyMessage);

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error handling member join event: {ex.Message}");
                    // 记录错误日志或处理错误
                }
            }
        }


        private async Task HandleJoinEventAsync(JoinEvent joinEvent)
        {
            var replyMessage = new List<ISendMessage>
            {
                new TextMessage("大家好很高興加入，嘗試呼叫關鍵字 地瓜 使用更多功能")
            };
            await _lineMessagingClient.ReplyMessageAsync(joinEvent.ReplyToken, replyMessage);
        }// 处理机器人加入群组的事件




        private async Task SendButtonsTemplateAsync(string replyToken)
        {
            var buttonsTemplate = new ButtonsTemplate(
                text: "選擇一個選項",
                title: "這是標題",

                actions: new List<ITemplateAction>
                {
                    new MessageTemplateAction("選項 1", "選項 1"),

                    new PostbackTemplateAction("選項 2", "選項 2")
                });

            var templateMessage = new TemplateMessage("這是標題", buttonsTemplate);
            await _lineMessagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { templateMessage });
        }

        private async Task SendConfirmTemplateAsync(string replyToken)
        {
            var confirmTemplate = new ConfirmTemplate(
                text: "您要加入记账群组吗？",
                actions: new List<ITemplateAction>
                {
            new PostbackTemplateAction("是", "JOIN_GROUP"),
            new PostbackTemplateAction("否", "CANCEL_JOIN")
                }
            );

            var templateMessage = new TemplateMessage("加入群组确认", confirmTemplate);

            try
            {
                await _lineMessagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { templateMessage });
                Debug.WriteLine("Confirm template sent successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending confirm template: {ex.Message}");
            }
        }


    }
}
