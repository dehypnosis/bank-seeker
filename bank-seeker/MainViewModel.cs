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
        // 콜백 모델
        public class Callback : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void Update(string property)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
            }

            private string url = "";
            public string URL
            {
                get
                {
                    return url;
                }
                set
                {
                    url = value;
                    Update("URL");
                }
            }
            private string secretKey = "ANY_SECRET_KEY";
            public string SecretKey
            {
                get
                {
                    return secretKey;
                }
                set
                {
                    secretKey = value;
                    Update("SecretKey");
                }
            }
            private bool automaticEnabled = true;
            public bool AutomaticEnabled
            {
                get
                {
                    return automaticEnabled;
                }
                set
                {
                    automaticEnabled = value;
                    Update("AutomaticEnabled");
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

        public void LoadData()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Callback>));
                using (StreamReader rd = new StreamReader(ContentManager.getPath("callbacks.xml")))
                {
                    Callbacks = xs.Deserialize(rd) as ObservableCollection<Callback>;
                }
            }
            catch (Exception)
            {
                Callbacks = new ObservableCollection<Callback>();
            }
            if (Callbacks == null) Callbacks = new ObservableCollection<Callback>();
            if (Callbacks.Count > 0)
            {
                SelectedCallback = Callbacks[0];
            }

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
            XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<Callback>));
            using (StreamWriter wr = new StreamWriter(ContentManager.getPath("callbacks.xml")))
            {
                xs.Serialize(wr, Callbacks);
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

        // status 체크용 웹서버
        private StatusServer server;

        public MainViewModel()
        {
            // 조회 내역 처리
            Teller.TellerPackageEvent += packages =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    for(var i=packages.Count-1; i>=0; i--) // reverse order
                    {
                        var pack = packages[i];
                        try
                        {
                            Packages.First(new Func<Package, bool>(package => package.Hash == pack.Hash));
                        }
                        catch (InvalidOperationException)
                        {
                            ProcessPackage(pack);
                        }
                    }
                    Update("Packages");
                }));
            };
            Teller.TellerStopEvent += () =>
            {
                IsFetching = false;
                server.Update("OFF");
            };

            // 로그 처리
            Teller.TellerLogEvent += log =>
            {
                Log += $"\n[{DateTime.Now.ToString()}] {log}";
                Update("Log");
            };

            // 서버 시작
            server = new StatusServer();
            Teller.Log($"http://0.0.0.0:{server.Port} 에서 서비스 상태 웹서버 시작...");
        }

        // 콜백 설정
        private ObservableCollection<Callback> callbacks;
        public ObservableCollection<Callback> Callbacks
        {
            get
            {
                return callbacks;
            }
            set
            {
                callbacks = value;
                Update("Callbacks");
            }
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
            Accounts.Remove(SelectedAccount);
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
            server.Update("ON");
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
        private Callback selectedCallback = null;
        public Callback SelectedCallback
        {
            get { return selectedCallback; }
            set
            {
                selectedCallback = value;
                Update("SelectedCallback");
                Update("SelectedCallbackVisibility");
            }
        }
        public Visibility SelectedCallbackVisibility => SelectedCallback == null ? Visibility.Collapsed : Visibility.Visible;

        internal void AddCallback()
        {
            var callback = new Callback();
            Callbacks.Add(callback);
            SelectedCallback = callback;
        }

        internal void RemoveCallback()
        {
            Callbacks.Remove(SelectedCallback);
            SelectedCallback = null;
        }

        public void ProcessPackage(Package package, bool forcelyCallback = false)
        {
            foreach(var callback in Callbacks)
            {
                if (forcelyCallback || callback.AutomaticEnabled)
                    teller.Callback(package, callback.URL, callback.SecretKey);
            }

            var index = Packages.IndexOf(package);
            if (index == -1)
            {
                Packages.Insert(0, package);
            }
            else
            {
                Packages.RemoveAt(index);
                Packages.Insert(index, package);
            }
        }

    }
}
