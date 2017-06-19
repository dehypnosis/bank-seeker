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
        public string Name { get; set; } = "이름 없음";
        public uint IntervalSeconds { get; set; } = 0;
        public uint IntervalMins {
            get
            {
                return IntervalSeconds / 60;
            }
            set
            {
                IntervalSeconds = value * 60;
            }
        }
        public Bank Bank { get; set; } = Bank.ByCode("NH");
        public string Number { get; set; } = null;
        public string UserId { get; set; } = null;
        public string Password { get; set; } = null;
        public DateTime From { get; set; } = DateTime.Today;
        public DateTime To { get; set; } = DateTime.Today;

        // 후에 Bank별로 추가정보가 필요하면 이 해시를 이용할 수 있겠음 
        public Dictionary<string, string> extra = new Dictionary<string, string>();

        // validate
        public void Validate()
        {
            if (Bank == null
                || Number == null
                || UserId == null
                || Password == null)
            {
                throw new AccountError();
            }
        }
        public class AccountError : Exception {};
    }
}