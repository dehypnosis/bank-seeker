using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BankSeeker.Lib
{
    // <summary>은행 타입 및 이름, <cref="Seeker">해당 파서의 타입</cref></summary>
    public class Bank
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public System.Type SeekerType { get; set; }

        public static List<Bank> Banks = new List<Bank>()
        {
            new Bank {Code = "KB", Name = "국민은행", SeekerType = typeof(Seekers.SeekerKB)},
            new Bank {Code = "NH", Name = "농협중앙회", SeekerType = typeof(Seekers.SeekerNH)},
        };

        public static Bank ByCode(string code)
        {
            return Banks.Find(new Predicate<Bank>(bank => bank.Code == code));
        }
    }

}
