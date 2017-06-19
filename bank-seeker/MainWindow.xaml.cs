using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BankSeeker.Lib;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;
using static BankSeeker.Lib.Seeker;
using System.Collections.Specialized;

namespace BankSeeker
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Update(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
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
            get {
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
            get { return canStop;  }
            private set {
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
                    MyName = "내이름",
                    YourName = "니이름",
                    OutAmount = 0,
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
                    MyName = "내이름2",
                    YourName = "니이름2",
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
                    MyName = "내이름",
                    YourName = "니이름",
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
                    foreach(var pack in packages)
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
    }

    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;
        public MainWindow()
        {
            // 뷰 모델 생성
            DataContext = new MainViewModel();
            viewModel = this.DataContext as MainViewModel;

            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 로깅시 스크롤 내림
            Teller.TellerLogEvent += log => Dispatcher.Invoke(new Action(() => LogScrollViewer.ScrollToEnd()));

            // 계좌 날짜 컨트롤 제약 설정
            this.AccountTo.DisplayDateEnd = this.AccountFrom.DisplayDateEnd = DateTime.Today;
            this.AccountTo.SelectedDateChanged += AccountTo_SelectedDateChanged;
            this.AccountFrom.SelectedDateChanged += AccountFrom_SelectedDateChanged;

            // 도움말 파일 로드
            var textRange = new TextRange(HelpTextBox.Document.ContentStart, HelpTextBox.Document.ContentEnd);
            using (var fileStream = new System.IO.FileStream("help.rtf", System.IO.FileMode.Open))
                textRange.Load(fileStream, System.Windows.DataFormats.Rtf);
        }

        private void AccountFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.AccountTo.SelectedDate < this.AccountFrom.SelectedDate)
            {
                AccountTo.SelectedDate = this.AccountFrom.SelectedDate;
            }
        }
        private void AccountTo_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.AccountTo.SelectedDate < this.AccountFrom.SelectedDate)
            {
                AccountFrom.SelectedDate = this.AccountTo.SelectedDate;
            }
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddAccount();
        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm("정말 삭제하시겠습니까?"))
                viewModel.RemoveAccount();
        }

        private void Fetch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.Fetch();
            }
            catch (Account.AccountError)
            {
                System.Windows.MessageBox.Show("계좌 정보를 입력하세요.", "오류");
            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Stop();
        }

        private void PackageCallback_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Callback((Package)((Button)sender).Tag);
        }

        private bool Confirm(string message)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(message, "확인", MessageBoxButton.OKCancel);
            return messageBoxResult == MessageBoxResult.OK;
        }
    }


    namespace Helper
    {
        // For Password Binding
        public static class PasswordBoxAssistant
        {
            public static readonly DependencyProperty BoundPasswordProperty =
                DependencyProperty.RegisterAttached("BoundPassword", typeof(string)
                , typeof(PasswordBoxAssistant), new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

            public static readonly DependencyProperty BindPasswordProperty = DependencyProperty.RegisterAttached(
                "BindPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false, OnBindPasswordChanged));

            private static readonly DependencyProperty UpdatingPasswordProperty =
                DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant));

            private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                PasswordBox box = d as PasswordBox;

                // only handle this event when the property is attached to a PasswordBox  
                // and when the BindPassword attached property has been set to true  
                var ignoreBindProperty = false;

                if (d == null || !(GetBindPassword(d) || ignoreBindProperty))
                {
                    return;
                }

                // avoid recursive updating by ignoring the box's changed event  
                box.PasswordChanged -= HandlePasswordChanged;

                string newPassword = (string)e.NewValue;

                if (!GetUpdatingPassword(box))
                {
                    box.Password = newPassword;
                }

                box.PasswordChanged += HandlePasswordChanged;
            }

            private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
            {
                // when the BindPassword attached property is set on a PasswordBox,  
                // start listening to its PasswordChanged event  

                PasswordBox box = dp as PasswordBox;

                if (box == null)
                {
                    return;
                }

                bool wasBound = (bool)(e.OldValue);
                bool needToBind = (bool)(e.NewValue);

                if (wasBound)
                {
                    box.PasswordChanged -= HandlePasswordChanged;
                }

                if (needToBind)
                {
                    box.PasswordChanged += HandlePasswordChanged;
                }
            }

            private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
            {
                PasswordBox box = sender as PasswordBox;

                // set a flag to indicate that we're updating the password  
                SetUpdatingPassword(box, true);
                // push the new password into the BoundPassword property  
                SetBoundPassword(box, box.Password);
                SetUpdatingPassword(box, false);
            }

            public static void SetBindPassword(DependencyObject dp, bool value)
            {
                dp.SetValue(BindPasswordProperty, value);
            }

            public static bool GetBindPassword(DependencyObject dp)
            {
                return (bool)dp.GetValue(BindPasswordProperty);
            }

            public static string GetBoundPassword(DependencyObject dp)
            {
                return (string)dp.GetValue(BoundPasswordProperty);
            }

            public static void SetBoundPassword(DependencyObject dp, string value)
            {
                dp.SetValue(BoundPasswordProperty, value);
            }

            private static bool GetUpdatingPassword(DependencyObject dp)
            {
                return (bool)dp.GetValue(UpdatingPasswordProperty);
            }

            private static void SetUpdatingPassword(DependencyObject dp, bool value)
            {
                dp.SetValue(UpdatingPasswordProperty, value);
            }
        }
    }
}