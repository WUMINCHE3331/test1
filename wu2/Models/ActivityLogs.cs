//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace wu2.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ActivityLogs
    {
        public int ActivityLogId { get; set; }
        public Nullable<int> GroupId { get; set; }
        public Nullable<int> UserId { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDetails { get; set; }
        public System.DateTime Date { get; set; }
        public Nullable<int> ExpenseId { get; set; }
    
        public virtual Groups Groups { get; set; }
        public virtual Users Users { get; set; }
        public virtual Expenses Expenses { get; set; }
    }
}
