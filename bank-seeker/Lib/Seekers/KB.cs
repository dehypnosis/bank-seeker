using System;
using System.Threading.Tasks;
using System.Drawing;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using BankSeeker.Helper;
using Shipwreck.Phash;
// using Driver = OpenQA.Selenium.Chrome.ChromeDriver;
using Driver = OpenQA.Selenium.PhantomJS.PhantomJSDriver;
using OpenQA.Selenium.Interactions;

namespace BankSeeker.Lib.Seekers
{

    // <summary>국민은행 파서</summary>
    public class SeekerKB : Seeker
    {
        // headless browser
        private Driver driver = null;
        
        public override void Dispose()
        {
            if (driver != null)
                driver.Dispose();
        }

        protected override List<Packet> FetchPackets(Account account)
        {
            // open page
            try
            {
                driver = Seeker.Service.GetDriver();
                Teller.Log($"[{account.Name}] 페이지를 초기화...");
                driver.Manage().Window.Size = new Size(765, 1000);
                driver.Navigate().GoToUrl("https://obank1.kbstar.com/quics?page=C025255&cc=b028364:b028702");// fill the form

                Teller.Log($"[{account.Name}] 로딩이 끝나기를 기다림...");
                var timeoutSeconds = account.IntervalSeconds;
                if (timeoutSeconds < 30) timeoutSeconds = 30;
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
                var loading = By.Id("loading");
                wait.Until(ExpectedConditions.InvisibilityOfElementLocated(loading));

                Teller.Log($"[{account.Name}] 계좌 정보 입력중...");
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
                Teller.Log($"[{account.Name}] 비밀번호 키패드 이미지 추출...");
                var password = By.Id("비밀번호");
                wait.Until(ExpectedConditions.ElementToBeClickable(password));
                driver.FindElement(password).Click();

                // save keypad image
                var keypadImagePath = ContentManager.getPath(@"KB/KB_keypad.bmp");
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
                    var SIGMA = 1;
                    var GAMMA = 1.5;
                    var DEGREE = 180;
                    var solDigests = new Dictionary<string, Digest>() {
                        {"num5", ImagePhash.ComputeDigest(ContentManager.getPath(@"KB/KB_keypad_sol_5.bmp"), SIGMA, GAMMA, DEGREE) },
                        {"num7", ImagePhash.ComputeDigest(ContentManager.getPath(@"KB/KB_keypad_sol_7.bmp"), SIGMA, GAMMA, DEGREE) },
                        {"num8", ImagePhash.ComputeDigest(ContentManager.getPath(@"KB/KB_keypad_sol_8.bmp"), SIGMA, GAMMA, DEGREE) },
                        {"num9", ImagePhash.ComputeDigest(ContentManager.getPath(@"KB/KB_keypad_sol_9.bmp"), SIGMA, GAMMA, DEGREE) },
                        {"num0", ImagePhash.ComputeDigest(ContentManager.getPath(@"KB/KB_keypad_sol_0.bmp"), SIGMA, GAMMA, DEGREE) },
                    };

                    // find each digit of btns by correlation with solution image
                    Teller.Log($"[{account.Name}] 번호별로 이미지 추출, 해싱을 통해 상관관계 분석...");
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
                        var btnImagePath = ContentManager.getPath($@"KB/KB_keypad_{btnLocation.Key}.bmp");
                        var rect = new Rectangle(btnLocation.Value.X - 22, btnLocation.Value.Y - 22, 44, 44);
                        using (var bitmap = new Bitmap(rect.Width, rect.Height))
                        using (var graphic = Graphics.FromImage(bitmap))
                        {
                            graphic.DrawImage(bitmapKeypad, 0, 0, rect, GraphicsUnit.Pixel);
                            bitmap.Save(btnImagePath);

                            // find each digit for btns
                            var btnDigest = ImagePhash.ComputeDigest(btnImagePath, SIGMA, GAMMA, DEGREE);
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
                                Teller.Log($"[{account.Name}] {answerKey} 분석 완료...");
                            }
                            catch (ArgumentException)
                            {
                                Teller.Log($"[{account.Name}] {btnLocation.Key}를 확정 할 수 없음... 재시작");
                                throw new NeedToRefetchError();
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
                        Teller.Log($"[{account.Name}] 번호 ({locationTo.X + dx}, {locationTo.Y + dy}) 클릭...");
                    }
                    locationTo = btnLocations["submit"]; // (373, 554).
                    new Actions(driver).MoveToElement(zero).MoveByOffset(locationTo.X + dx, locationTo.Y + dy).Click().Perform();
                    Teller.Log($"[{account.Name}] 확인 ({locationTo.X + dx}, {locationTo.Y + dy}) 클릭...");


                    // submit the form
                    wait.Until(ExpectedConditions.InvisibilityOfElementLocated(keypad));
                    var submit = By.CssSelector("input[type=submit]");
                    driver.FindElement(submit).Click();
                }

                // check page loaded
                var table = By.CssSelector(".tType01");
                wait.Until(ExpectedConditions.ElementIsVisible(table));

                // reprocessing as Packet def
                List<Packet> packets = new List<Packet>();
                for (var pageNum = 0; true; pageNum++)
                {
                    Teller.Log($"[{account.Name}] {pageNum + 1} 페이지 파싱 및 분석...");
                    //driver.GetScreenshot().SaveAsFile(ContentManager.getPath($@"KB/KB_result_{pageNum}.bmp"), ScreenshotImageFormat.Bmp);

                    var trs = driver.FindElementsByCssSelector(".tType01 tbody tr");
                    if (trs.Count < 2)
                    {
                        Teller.Log($"[{account.Name}] {pageNum + 1} 페이지 거래 내역 없음. 페이지 종료...");
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
                                Date = Convert.ToDateTime(datetimeTemp),
                                Note = tr.FindElement(By.CssSelector("td:nth-child(2)")).GetAttribute("textContent").Trim(),
                                OutName = tr.FindElement(By.CssSelector("td:nth-child(3)")).GetAttribute("textContent").Trim(),
                                OutAmount = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(4)")).GetAttribute("textContent").Trim()),
                                InAmount = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(5)")).GetAttribute("textContent").Trim()),
                                Balance = Convert.ToDecimal(tr.FindElement(By.CssSelector("td:nth-child(6)")).GetAttribute("textContent").Trim()),
                                Bank = tr.FindElement(By.CssSelector("td:nth-child(7)")).GetAttribute("textContent").Trim(),
                                Type = tr.FindElement(By.CssSelector("td:nth-child(8)")).GetAttribute("textContent").Trim(),
                            };
                            packets.Add(packet);
                        }
                        else
                        {
                            packets[(i - 1) / 2].InName = tr.GetAttribute("textContent").Trim();
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
                        Teller.Log($"[{account.Name}] 페이지 종료...");
                        break;
                    }
                }

                return packets;
            }
            catch (WebDriverException e)
            {
                Teller.Log($"[{account.Name}] 가상 브라우저 비정상 종료...\n{e.Message}");
                return null;
            }
            catch (NeedToRefetchError)
            {
                throw;
            }
            catch (Exception e)
            {
                Teller.Log($"[{account.Name}] 처리되지 않은 예외 발생...");
                Teller.Log(e);
                return null;
            }
            finally
            {
                Dispose();
            }
        }
    }
}
