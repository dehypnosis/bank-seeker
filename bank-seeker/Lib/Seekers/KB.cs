using System;
using System.Threading.Tasks;
using System.Drawing;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

//using Driver = OpenQA.Selenium.Chrome.ChromeDriver;
//using DriverService = OpenQA.Selenium.Chrome.ChromeDriverService;
using Driver = OpenQA.Selenium.PhantomJS.PhantomJSDriver;
using DriverService = OpenQA.Selenium.PhantomJS.PhantomJSDriverService;

using Shipwreck.Phash;
using System.Collections.Generic;
using OpenQA.Selenium.Interactions;

namespace BankSeeker.Lib.Seekers
{

    // <summary>국민은행 파서</summary>
    class SeekerKB : Seeker
    {
        // headless browser
        private DriverService driverService = null;
        private Driver driver = null;

        ~SeekerKB()
        {
            if (driverService != null)
            {
                if (driver != null)
                {
                    driver.Dispose();
                    driver.Quit();
                }
                driverService.Dispose();
            }
        }

        public override async Task<List<Packet>> Fetch(Account account)
        {
            if (driverService == null)
            {
                Log("가상 브라우저 초기화...");
                driverService = DriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;
            }

            return await Task.Run(() =>
            {
                if (driver != null)
                {
                    Log("가상 브라우저가 아직 작업을 수행 중... 간격이 너무 짧습니다.");
                    return null; // to be a Singleton becauseof race for BMP files
                }

                try
                {
                    // open page
                    Log("웹 페이지를 초기화...");
                    driver = new Driver(driverService);
                    driver.Manage().Window.Size = new Size(765, 1000);
                    driver.Navigate().GoToUrl("https://obank1.kbstar.com/quics?page=C025255&cc=b028364:b028702");

                    // fill the form
                    Log("로딩이 끝나기를 기다림...");
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(TimeoutSeconds));
                    var loading = By.Id("loading");
                    wait.Until(ExpectedConditions.InvisibilityOfElementLocated(loading));

                    Log("계좌 정보 입력중...");
                    var account_num = By.Id("account_num");
                    wait.Until(ExpectedConditions.ElementToBeClickable(account_num));
                    driver.FindElement(account_num).SendKeys(account.Number.Replace("-", ""));

                    var user_id = By.Id("user_id");
                    wait.Until(ExpectedConditions.ElementToBeClickable(user_id));
                    driver.FindElement(user_id).SendKeys(account.UserId);

                    new SelectElement(driver.FindElementById("조회시작년")).SelectByValue(account.From.ToString("yyyy"));
                    new SelectElement(driver.FindElementById("조회시작월")).SelectByValue(account.From.ToString("MM"));
                    new SelectElement(driver.FindElementById("조회시작일")).SelectByValue(account.From.ToString("dd"));
                    new SelectElement(driver.FindElementById("조회끝년")).SelectByValue(account.To.ToString("yyyy"));
                    new SelectElement(driver.FindElementById("조회끝월")).SelectByValue(account.To.ToString("MM"));
                    new SelectElement(driver.FindElementById("조회끝일")).SelectByValue(account.To.ToString("dd"));

                    // open password keypad
                    Log("비밀번호 키패드 이미지 추출...");
                    var password = By.Id("비밀번호");
                    wait.Until(ExpectedConditions.ElementToBeClickable(password));
                    driver.FindElement(password).Click();

                    // save keypad image
                    var keypadImagePath = @"./KB_keypad.bmp";
                    var keypad = By.CssSelector(".keypadWrap img");
                    wait.Until(ExpectedConditions.ElementToBeClickable(keypad));
                    driver.GetScreenshot().SaveAsFile(keypadImagePath, ScreenshotImageFormat.Bmp);
                    using (var bitmapKeypad = Bitmap.FromFile(keypadImagePath))
                    {
                        // analyze keypad image
                        var element = driver.FindElement(keypad);
                        var area = new
                        {
                            left = element.Location.X,
                            center = element.Location.X + element.Size.Width / 2,
                            right = element.Location.X + element.Size.Width,
                            width = element.Size.Width,
                            top = element.Location.Y,
                            height = element.Size.Height,
                        };
                        var btnLocations = new Dictionary<string, Point>() {
                            {"num1", new Point(area.left + 46, area.top + 70)},
                            {"num2", new Point(area.center, area.top + 70)},
                            {"num3", new Point(area.right - 46, area.top + 70)},
                            {"num4", new Point(area.left + 46, area.top + 130)},
                            {"xxx1", new Point(area.center, area.top + 130)},
                            {"num6", new Point(area.right - 46, area.top + 130)},
                            {"xxx2", new Point(area.left + 46, area.top + 188)},
                            {"xxx3", new Point(area.center, area.top + 188)},
                            {"xxx4", new Point(area.right - 46, area.top + 188)},
                            {"delOne", new Point(area.left + 46, area.top + 246)},
                            {"xxx5", new Point(area.center, area.top + 246)},
                            {"delAll", new Point(area.right - 46, area.top + 246)},
                            {"submit", new Point(area.center, area.top + 300)},
                        };

                        // SOLUTION IMAGES' digests
                        var solDigests = new Dictionary<string, Digest>() {
                            {"num5", ImagePhash.ComputeDigest(@"./KB_keypad_sol_5.bmp", 1, 1, 180) },
                            {"num7", ImagePhash.ComputeDigest(@"./KB_keypad_sol_7.bmp", 1, 1, 180) },
                            {"num8", ImagePhash.ComputeDigest(@"./KB_keypad_sol_8.bmp", 1, 1, 180) },
                            {"num9", ImagePhash.ComputeDigest(@"./KB_keypad_sol_9.bmp", 1, 1, 180) },
                            {"num0", ImagePhash.ComputeDigest(@"./KB_keypad_sol_0.bmp", 1, 1, 180) },
                        };

                        // find each digit of btns by correlation with solution image
                        Log("번호별로 이미지 추출, 해싱을 통해 상관관계 분석...");
                        var solBtnLocations = new Dictionary<string, Point>();
                        foreach (var btnLocation in btnLocations)
                        {

                            // compare only xxx btns (order shuffled btns 5,7,8,9,0)
                            if (!btnLocation.Key.StartsWith("xxx"))
                            {
                                solBtnLocations.Add(btnLocation.Key, btnLocation.Value);
                                continue;
                            }
                            
                            // crop image
                            var btnImagePath = @"./KB_keypad_" + btnLocation.Key + ".bmp";
                            var rect = new Rectangle(btnLocation.Value.X - 22, btnLocation.Value.Y - 22, 44, 44);
                            using (var bitmap = new Bitmap(rect.Width, rect.Height))
                            using (var graphic = Graphics.FromImage(bitmap))
                            {
                                graphic.DrawImage(bitmapKeypad, 0, 0, rect, GraphicsUnit.Pixel);
                                bitmap.Save(btnImagePath);

                                // find each digit for btns
                                var btnDigest = ImagePhash.ComputeDigest(btnImagePath, 1, 1, 180);
                                string answerKey = null;
                                double answerCorr = 0;
                                foreach (var solDigest in solDigests)
                                {
                                    var corr = ImagePhash.GetCrossCorrelation(btnDigest, solDigest.Value);
                                    if (corr > answerCorr)
                                    {
                                        answerCorr = corr;
                                        answerKey = solDigest.Key;
                                    }
                                }

                                // add to solved btn locations
                                try
                                {
                                    solBtnLocations.Add(answerKey, btnLocation.Value);
                                    Log($"{answerKey} 분석 완료...");
                                }
                                catch (ArgumentException)
                                {
                                    Log($"{btnLocation.Key}를 확정 할 수 없음... 재시작");
                                    return null;
                                }
                            }
                        }
                        
                        // now tocuh the keypad as the solution
                        Point locationTo;
                        var zero = driver.FindElement(password);
                        var dx = -area.left + 90;
                        var dy = -area.top + 15;

//                        // helper to adjust dx,dy
//                        driver.ExecuteScript(@"
//window.onclick = function(e) {
//    var d = document.createElement('div');
//    d.style.width='2px'; d.style.height='2px';
//    d.style.position='absolute';
//    d.style.display='block';
//    d.style.top=e.clientY+'px';
//    d.style.left=e.clientX+'px';
//    d.style.background='red';
//    d.style.zIndex='10000000';
//    document.body.appendChild(d);
//};
//                        ");

                        foreach (var digit in account.Password.ToCharArray())
                        {
                            locationTo = solBtnLocations["num" + digit];
                            new Actions(driver).MoveToElement(zero).MoveByOffset(locationTo.X + dx, locationTo.Y + dy).Click().Perform();
                            Log($"num{digit} ({locationTo.X+dx}, {locationTo.Y+dy}) 클릭...");
                        }
                        locationTo = btnLocations["submit"]; // (373, 554).
                        new Actions(driver).MoveToElement(zero).MoveByOffset(locationTo.X + dx, locationTo.Y + dy).Click().Perform();
                        Log($"확인 ({locationTo.X+dx}, {locationTo.Y+dy}) 클릭...");

                        // submit the form
                        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(keypad));
                        var submit = By.CssSelector("input[type=submit]");
                        driver.FindElement(submit).Click();

                        // check page loaded
                        var table = By.CssSelector(".tType01");
                        wait.Until(ExpectedConditions.ElementIsVisible(table));

                        // reprocessing as Packet def
                        List<Packet> packets = new List<Packet>();
                        for (var pageNum=0; true; pageNum++)
                        {
                            Log($"{pageNum+1} 페이지 파싱 및 분석...");
                            driver.GetScreenshot().SaveAsFile($@"./KB_result_{pageNum}.bmp", ScreenshotImageFormat.Bmp);

                            var trs = driver.FindElementsByCssSelector(".tType01 tbody tr");
                            if (trs.Count < 2)
                            {
                                Log($"{pageNum+1} 페이지 거래 내역 없음. 파서 종료...");
                                break; // No items
                            }
                            for (var i = 0; i < trs.Count; i++)
                            {
                                var tr = trs[i];
                                if (i % 2 == 0)
                                {
                                    var datetimeTemp = tr.FindElement(By.CssSelector("td:nth-child(1)")).GetAttribute("textContent").Trim();
                                    datetimeTemp = datetimeTemp.Substring(0, 10) + " " + datetimeTemp.Substring(10);
                                    var packet = new Packet
                                    {
                                        datetime = Convert.ToDateTime(datetimeTemp),
                                        note = tr.FindElement(By.CssSelector("td:nth-child(2)")).GetAttribute("textContent").Trim(),
                                        myName = tr.FindElement(By.CssSelector("td:nth-child(3)")).GetAttribute("textContent").Trim(),
                                        outAmount = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(4)")).GetAttribute("textContent").Trim()),
                                        inAmount = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(5)")).GetAttribute("textContent").Trim()),
                                        balance = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(6)")).GetAttribute("textContent").Trim()),
                                        bank = tr.FindElement(By.CssSelector("td:nth-child(7)")).GetAttribute("textContent").Trim(),
                                        type = tr.FindElement(By.CssSelector("td:nth-child(8)")).GetAttribute("textContent").Trim(),
                                    };
                                    packets.Add(packet);
                                }
                                else
                                {
                                    packets[(i - 1) / 2].yourName = tr.GetAttribute("textContent").Trim();
                                }
                            }

                            // for pagination
                            try
                            {
                                driver.FindElementByCssSelector(".optionBtnArea .leftArea .next input").Click();
                                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("loading")));
                                wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.Id("loading")));
                            }
                            catch (NoSuchElementException)
                            {
                                Log($"파서 종료...");
                                break;
                            }
                        }

                        return packets;
                    }
                }
                catch (Exception e)
                {
                    Log("처리되지 않은 예외 발생...");
                    Log(e);
                    return null;
                }
                finally
                {
                    if (driver != null) {
                        driver.Dispose();
                        driver = null;
                    }
                }
            });
        }
    }
}
