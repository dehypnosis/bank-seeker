using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BankSeeker.Lib
{
    // <summary>파싱 할 계좌 정보</summary>
    public class Account
    {
        public Bank.Type? BankType { get; set; }
        public string BankName => Bank.GetName(BankType);
        public string Number { get; set; } = null;
        public string UserId { get; set; } = null;
        public string Password { get; set; } = null;

        // 후에 Bank별로 추가정보가 필요하면 이 해시를 이용할 수 있겠음 
        public Dictionary<string, string> extra = new Dictionary<string, string>();

        // validate
        internal void Validate()
        {
            if (BankType == null
                || Number == null
                || UserId == null
                || Password == null)
            {
                throw new Exception("ACCOUNT_VALIDATION_FAILED");
            }
        }
    }
}