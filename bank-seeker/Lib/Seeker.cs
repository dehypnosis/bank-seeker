using System;
using System.Collections.Generic;
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

        public Seeker()
        {
            Console.WriteLine($"{this.GetType().ToString()} Seeker Made");
        }
        public abstract Task<List<Packet>> Fetch(Account account, uint timeoutInterval); // 구현시 비동기 한정자 필요
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
            public override Task<List<Packet>> Fetch(Account account, uint timeoutInterval)
            {
                throw new NotImplementedException();
            }
        }
    }
}
