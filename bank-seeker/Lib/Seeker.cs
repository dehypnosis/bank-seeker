using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankSeeker.Lib
{
    // <summary>은행별 파서의 인터페이스 및 데이터 구조, Fetch 호출시 서비스의 도움을 받아서 비동기로 데이터를 파싱하고 포맷에 맞게 변환</summary>
    abstract class Seeker
    {
        public class Packet
        {
            public DateTime datetime { get; set; }
            public string note { get; set; }
            public string myName { get; set; }
            public string yourName { get; set; }
            public decimal outAmount { get; set; }
            public decimal inAmount { get; set; }
            public decimal balance { get; set; }
            public string bank { get; set; }
            public string type { get; set; }
        }

        private Teller teller;
        internal void setTeller(Teller teller)
        {
            this.teller = teller;
        }

        public void Log(object log)
        {
            this.teller.Log(Convert.ToString(log));
        }

        protected uint TimeoutSeconds;
        public void SetTimeoutSeconds(uint sec)
        {
            TimeoutSeconds = sec;
        }

        public abstract Task<List<Packet>> Fetch(Account account); // 구현시 비동기 한정자 필요
    }

    namespace Seekers
    {
        // 더미용 파서
        class NotImplemented : Seeker
        {
            public NotImplemented()
            {
                throw new NotImplementedException();
            }
            public override Task<List<Packet>> Fetch(Account account)
            {
                throw new NotImplementedException();
            }
        }
    }
}
