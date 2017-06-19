using BankSeeker.Lib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BankSeeker
{

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Update(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public void LoadData()
        {

        }

        // <summary>은행 타입 및 이름, <cref="Seeker">해당 파서의 타입</cref></summary>
        public class BB
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public System.Type SeekerType { get; set; }

            public static List<BB> Banks = new List<BB>()
            {
                new BB {Code = "KB", Name = "국민은행", SeekerType = typeof(Lib.Seekers.SeekerKB)},
                new BB {Code = "NH", Name = "농협중앙회", SeekerType = typeof(Lib.Seekers.SeekerNH)},
            };

            public static BB ByCode(string code)
            {
                return Banks.Find(new Predicate<BB>(bank => bank.Code == code));
            }
        }
        public class ABC
        {
            public string Name { get; set; } = "이름 없음";
            public BB Bank { get; set; } = null;

        }

        public void SaveData()
        {
            XmlSerializer xs = new XmlSerializer(typeof(ABC));
            using (StreamWriter wr = new StreamWriter("customers.xml"))
            {
                xs.Serialize(wr, new ABC() { Name="Test"});
            }
        }

        public MainViewModel()
        {
            // 계좌 초기화
            Accounts.Add(new Account
            {
                Name = "국민계좌",
                IntervalMins = 1,
                Bank = Bank.ByCode("KB"),
                Number = "957502-01-427751",
                UserId = "YEO7311",
                Password = "1524",
                From = DateTime.Today.AddDays(-4),
            });
            Accounts.Add(new Account
            {
                Name = "농협계좌",
                IntervalMins = 0,
                Bank = Bank.ByCode("NH"),
                Number = "957502-01-427752",
                UserId = "YEO7331",
                Password = "777",
                From = DateTime.Today.AddDays(-2)
            });
            if (Accounts.Count > 0)
            {
                SelectedAccount = Accounts[0];
            }

            // 콜백 초기화
            CallbackURL = "https://benzen.io/json.php";
            CallbackSecret = "ANY_STRING";
            callbackAutomatic = true;

            // 조회 내역 초기화
            Packages.Add(new Package()
            {
                Hash = "1",
                Account = Accounts[0],
                Packet = new Packet()
                {
                    Date = DateTime.Now,
                    Note = "테스트",
                    OutName = "내이름",
                    OutAmount = 0,
                    InName = "니이름",
                    InAmount = 100000,
                    Balance = 130000,
                    Bank = "어디영업점",
                    Type = null
                }
            });
            Packages.Add(new Package()
            {
                Hash = "2",
                Account = Accounts[0],
                Packet = new Packet()
                {
                    Date = DateTime.Now,
                    Note = "테스트2",
                    OutName = "내이름2",
                    InName = "니이름2",
                    OutAmount = 0,
                    InAmount = 100000,
                    Balance = 230000,
                    Bank = "어디영업점2",
                    Type = null
                }
            });
            Packages.Add(new Package()
            {
                Hash = "3",
                Account = Accounts[0],
                Packet = new Packet()
                {
                    Date = DateTime.Now,
                    Note = "테스트",
                    OutName = "내이름",
                    InName = "니이름",
                    OutAmount = 0,
                    InAmount = 100000,
                    Balance = 130000,
                    Bank = "어디영업점",
                    Type = null
                }
            });
            Teller.TellerPackageEvent += packages =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (var pack in packages)
                    {
                        try
                        {
                            Packages.First(new Func<Package, bool>(package => package.Hash == pack.Hash));
                        }
                        catch (InvalidOperationException)
                        {
                            Callback(pack);
                        }
                    }
                }));
            };
            Teller.TellerStopEvent += () =>
            {
                IsFetching = false;
            };

            // 로깅 초기화
            Teller.TellerLogEvent += log =>
            {
                Log += $"\n[{DateTime.Now.ToString()}] {log}";
                Update("Log");
            };
        }

        // 계좌
        public ObservableCollection<Account> Accounts { get; } = new ObservableCollection<Account>();
        private Account selectedAccount = null;
        public Account SelectedAccount
        {
            get { return selectedAccount; }
            set
            {
                selectedAccount = value;
                Update("SelectedAccount");
                Update("SelectedAccountVisibility");
            }
        }
        public Visibility SelectedAccountVisibility => SelectedAccount == null ? Visibility.Collapsed : Visibility.Visible;
        public void AddAccount()
        {
            var account = new Account();
            Accounts.Add(account);
            SelectedAccount = account;
        }
        public void RemoveAccount()
        {
            Accounts.Remove(selectedAccount);
            SelectedAccount = null;
        }

        public List<Bank> Banks { get; } = Bank.Banks;


        // 거래 내역 조회
        private Teller teller = new Teller();
        private bool isFetching = false;
        public bool IsFetching
        {
            get
            {
                return isFetching;
            }
            set
            {
                isFetching = value;
                Update("IsFetching");
                Update("IsNotFetching");
                Update("VisibleWhenFetching");
                Update("VisibleWhenNotFetching");
            }
        }
        public bool IsNotFetching => !IsFetching;
        public Visibility VisibleWhenFetching => IsFetching ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibleWhenNotFetching => IsFetching ? Visibility.Collapsed : Visibility.Visible;
        public void Fetch()
        {
            teller.Fetch(SelectedAccount);
            IsFetching = true;
        }

        private bool canStop = true;
        public bool CanStop
        {
            get { return canStop; }
            private set
            {
                canStop = value;
                Update("CanStop");
            }
        }
        public void Stop()
        {
            if (!CanStop) return;
            CanStop = false;
            Task.Run(() =>
            {
                teller.Stop();
                CanStop = true;
                IsFetching = false;
            });
        }

        // 조회 및 콜백 내역 (패키지)
        public ObservableCollection<Package> Packages { get; } = new ObservableCollection<Package>();

        // 로깅
        public string Log { get; private set; }

        private string callbackURL;
        public string CallbackURL
        {
            get { return callbackURL; }
            set
            {
                callbackURL = value;
                Update("CallbackURL");
            }
        }
        private string callbackSecret;
        public string CallbackSecret
        {
            get { return callbackSecret; }
            set
            {
                callbackSecret = value;
                Update("CallbackSecret");
            }
        }
        private bool callbackAutomatic;
        public bool CallbackAutomatic
        {
            get { return callbackAutomatic; }
            set
            {
                callbackAutomatic = value;
                Update("CallbackAutomatic");
            }
        }
        public void Callback(Package package)
        {
            if (CallbackAutomatic)
                teller.Callback(package, CallbackURL, CallbackSecret);

            var index = Packages.IndexOf(package);
            if (index == -1)
            {
                Packages.Add(package);
            }
            else
            {
                Packages.RemoveAt(index);
                Packages.Insert(index, package);
            }
        }

    }
}
