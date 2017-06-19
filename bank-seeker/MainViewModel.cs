using BankSeeker.Helper;
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
        // 설정 모델
        public class ConfigureModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void Update(string property)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
            }

            private string callbackURL = "https://benzen.io/json.php";
            public string CallbackURL
            {
                get
                {
                    return callbackURL;
                }
                set
                {
                    callbackURL = value;
                    Update("CallbackURL");
                }
            }
            private string callbackSecret = "ANY_SECRET_KEY";
            public string CallbackSecret
            {
                get
                {
                    return callbackSecret;
                }
                set
                {
                    callbackSecret = value;
                    Update("CallbackSecret");
                }
            }
            private bool callbackAutomatic = true;
            public bool CallbackAutomatic
            {
                get
                {
                    return callbackAutomatic;
                }
                set
                {
                    callbackAutomatic = value;
                    Update("CallbackAutomatic");
                }
            }
        }

        // 메인 윈도우 뷰모델 속성들
        public event PropertyChangedEventHandler PropertyChanged;
        private void Update(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        private ConfigureModel configure;
        public ConfigureModel Configure {
            get
            {
                return configure;
            }
            set
            {
                configure = value;
                Update("Configure");
            }
        }

        public void LoadData()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ConfigureModel));
                using (StreamReader rd = new StreamReader(ContentManager.getPath("configure.xml")))
                {
                    Configure = xs.Deserialize(rd) as ConfigureModel;
                }
            }
            catch (Exception)
            {
                Configure = new ConfigureModel();
            }
            if (Accounts == null) Accounts = new ObservableCollection<Account>();

            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Account>));
                using (StreamReader rd = new StreamReader(ContentManager.getPath("accounts.xml")))
                {
                    Accounts = xs.Deserialize(rd) as ObservableCollection<Account>;
                }
            }
            catch (Exception)
            {
                Accounts = new ObservableCollection<Account>();
            }
            if (Accounts == null) Accounts = new ObservableCollection<Account>();
            if (Accounts.Count > 0)
            {
                SelectedAccount = Accounts[0];
            }

            try
            {
                var xs = new XmlSerializer(typeof(ObservableCollection<Package>));
                using (StreamReader rd = new StreamReader(ContentManager.getPath("packages.xml")))
                {
                    Packages = xs.Deserialize(rd) as ObservableCollection<Package>;
                }
            }
            catch (Exception)
            {
                Packages = new ObservableCollection<Package>();
            }
            if (Packages == null) Packages = new ObservableCollection<Package>();
        }

        public void SaveData()
        {
            XmlSerializer xs = new XmlSerializer(typeof(ConfigureModel));
            using (StreamWriter wr = new StreamWriter(ContentManager.getPath("configure.xml")))
            {
                xs.Serialize(wr, Configure);
            }

            xs = new XmlSerializer(typeof(ObservableCollection<Account>));
            using (StreamWriter wr = new StreamWriter(ContentManager.getPath("accounts.xml")))
            {
                xs.Serialize(wr, Accounts);
            }

            xs = new XmlSerializer(typeof(ObservableCollection<Package>));
            using (StreamWriter wr = new StreamWriter(ContentManager.getPath("packages.xml")))
            {
                xs.Serialize(wr, Packages);
            }
        }

        public MainViewModel()
        {
            // 조회 내역 처리
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
                    Update("Packages");
                }));
            };
            Teller.TellerStopEvent += () =>
            {
                IsFetching = false;
            };

            // 로그 처리
            Teller.TellerLogEvent += log =>
            {
                Log += $"\n[{DateTime.Now.ToString()}] {log}";
                Update("Log");
            };
        }

        // 계좌
        private ObservableCollection<Account> accounts;
        public ObservableCollection<Account> Accounts {
            get
            {
                return accounts;
            }
            set
            {
                accounts = value;
                Update("Accounts");
            }
        }


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
        private ObservableCollection<Package> packages;
        public ObservableCollection<Package> Packages
        {
            get
            {
                return packages;
            }
            set
            {
                packages = value;
                Update("Packages");
            }
        }
        public void ClearPackages()
        {
            Packages.Clear();
        }

        // 로깅
        public string Log { get; private set; }

        // 콜백
        public void Callback(Package package)
        {
            if (Configure.CallbackAutomatic)
                teller.Callback(package, Configure.CallbackURL, Configure.CallbackSecret);

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
