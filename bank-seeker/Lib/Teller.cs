using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Web.Script.Serialization;

namespace BankSeeker.Lib
{
    // raw data
    public class Packet
    {
        public DateTime Date { get; set; }
        public string Note { get; set; }
        public string OutName { get; set; }
        public string InName { get; set; }
        public decimal OutAmount { get; set; }
        public decimal InAmount { get; set; }
        public decimal Balance { get; set; }
        public string Bank { get; set; }
        public string Type { get; set; }
    }

    // 암호화 및 정제된 데이터
    public class Package
    {
        public Packet Packet { get; set; }
        public Account Account { get; set; }
        public string Hash { get; set; }
        public string CallbackResult { get; set; }
    }

    // <summary>계좌 및 타이머를 설정하고, <see cref="Seeker">은행별 파서</see>를 생성 및 실행하고 이벤트 발행/구독 인터페이스 제공</summary>
    public class Teller
    {
        // 로깅
        public delegate void TellerLogEventHandler(string log);
        public static event TellerLogEventHandler TellerLogEvent;
        public static void Log(object log)
        {
            var msg = Convert.ToString(log);
            if (TellerLogEvent != null)
            {
                TellerLogEvent(msg);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        // 계좌 데이터 가져오기 및 이벤트 발행/구독
        public delegate void TellerPackageEventHandler(List<Package> packages);
        public static event TellerPackageEventHandler TellerPackageEvent = packages => Log($"{packages.Count}개의 내역 조회 완료...");
        public delegate void TellerStopEventHandler();
        public static event TellerStopEventHandler TellerStopEvent = () => Log($"조회 작업 종료...");

        public bool IsFetching => isFetching;
        private bool isFetching = false;
        private Timer timer = null;
        private Seeker seeker = null;
        public void Fetch(Account account)
        {
            if (isFetching) return;

            // init seeker
            account.Validate();
            try
            {

                seeker = (Seeker)Activator.CreateInstance(account.Bank.SeekerType);
            } catch(Exception)
            {
                return;
            }
            
            // repeat or not
            isFetching = true;
            timer = new Timer();
            timer.AutoReset = false;
            timer.Interval = 1000;
            timer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                var pakages = seeker.Fetch(account);
                if (pakages != null)
                    TellerPackageEvent(pakages);
                Console.WriteLine("Sent!");
            };
            if (account.IntervalSeconds >  0)
            {
                timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                {
                    if (isFetching)
                    {
                        timer.Stop();
                        timer.Interval = account.IntervalSeconds * 1000;
                        timer.Start();
                    }
                };
            }
            else
            {
                timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                {
                    if (isFetching)
                    {
                        Stop();
                    }
                };
            }
            timer.Start();
        }

        public void Stop()
        {
            if (timer != null) timer.Dispose();
            if (seeker != null) seeker.Dispose();
            isFetching = false;
            TellerStopEvent();
        }

        // feature for callback
        public void Callback(Package package, string url, string secret = null)
        {
            try
            {
                // serialize package
                MD5 md5 = new MD5CryptoServiceProvider();
                var hashSig = md5.ComputeHash(Encoding.UTF8.GetBytes(package.Hash + secret));
                var stringBuilder = new StringBuilder();
                foreach (byte b in hashSig)
                {
                    stringBuilder.AppendFormat("{0:x2}", b);
                }
                var hashSigStr = stringBuilder.ToString();
                var serializer = new JavaScriptSerializer();
                var data = serializer.Serialize(new
                {
                    Hash = package.Hash,
                    HashSignature = hashSigStr,
                    Account = new
                    {
                        BankCode = package.Account.Bank.Code,
                        Number = package.Account.Number,
                    },
                    Packet = package.Packet
                }).Replace("\"\\/Date(", "").Replace(")\\/\"", ""); // replace wrong date format to just timestamp format

                // create a request
                Log($"POST {url} 거래 내역을 JSON 포맷으로 전송 완료...");
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version11;
                request.Method = "POST";
                byte[] dataEncoded = Encoding.UTF8.GetBytes(data);

                // send the request
                request.ContentType = "application/json";
                request.ContentLength = dataEncoded.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(dataEncoded, 0, dataEncoded.Length);
                requestStream.Close();

                // get the response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var responseBody = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.Write(responseBody);
                package.CallbackResult = response.StatusCode.ToString();
            }
            catch (Exception)
            {
                Log($"POST {url} 거래 내역 전송 실패...");
                package.CallbackResult = "ERR";
            }
        }

        private object MD5(string data)
        {
            throw new NotImplementedException();
        }
    }
}
