using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OVC19
{
    public class SearchSystem
    {
        const string QUERY = @"[
  {
    ""operationName"": ""vaccineList"",
    ""variables"": {
      ""input"": {
        ""keyword"": ""�ڷγ������Ź�Ƿ���"",
        ""x"": ""{XXX}"",
        ""y"": ""{YYY}""
      },
      ""businessesInput"": {
        ""start"": {START},
        ""display"": {DDD},
        ""deviceType"": ""mobile"",
        ""x"": ""{XXX}"",
        ""y"": ""{YYY}"",
        ""bounds"": ""{XMN};{YMN};{XMX};{YMX}"",
        ""sortingOrder"": ""distance""
      },
      ""isNmap"": false,
      ""isBounds"": false
    },
    ""query"": ""query vaccineList($input: RestsInput, $businessesInput: RestsBusinessesInput, $isNmap: Boolean!, $isBounds: Boolean!) {\n  rests(input: $input) {\n    businesses(input: $businessesInput) {\n      total\n      vaccineLastSave\n      isUpdateDelayed\n      items {\n        id\n        name\n        dbType\n        phone\n        virtualPhone\n        hasBooking\n        hasNPay\n        bookingReviewCount\n        description\n        distance\n        commonAddress\n        roadAddress\n        address\n        imageUrl\n        imageCount\n        tags\n        distance\n        promotionTitle\n        category\n        routeUrl\n        businessHours\n        x\n        y\n        imageMarker @include(if: $isNmap) {\n          marker\n          markerSelected\n          __typename\n        }\n        markerLabel @include(if: $isNmap) {\n          text\n          style\n          __typename\n        }\n        isDelivery\n        isTakeOut\n        isPreOrder\n        isTableOrder\n        naverBookingCategory\n        bookingDisplayName\n        bookingBusinessId\n        bookingVisitId\n        bookingPickupId\n        vaccineOpeningHour {\n          isDayOff\n          standardTime\n          __typename\n        }\n        vaccineQuantity {\n          totalQuantity\n          totalQuantityStatus\n          startTime\n          endTime\n          vaccineOrganizationCode\n          list {\n            quantity\n            quantityStatus\n            vaccineType\n            __typename\n          }\n          __typename\n        }\n        __typename\n      }\n      optionsForMap @include(if: $isBounds) {\n        maxZoom\n        minZoom\n        includeMyLocation\n        maxIncludePoiCount\n        center\n        __typename\n      }\n      __typename\n    }\n    queryResult {\n      keyword\n      vaccineFilter\n      categories\n      region\n      isBrandList\n      filterBooking\n      hasNearQuery\n      isPublicMask\n      __typename\n    }\n    __typename\n  }\n}\n""
  }
]";
        public class RestCollection { public RestData data; }
        public class RestData { public RestListApiResult rests; }
        public class RestListApiResult { public RestBusinesses businesses; }
        public class RestBusinesses
        {
            public int total;
            public long vaccineLastSave;
            public bool isUpdateDelayed;
            public RestListItem[] items;
        }
        public class RestListItem
        {
            public string id;
            public string name;
            public string roadAddress;
            public string x;
            public string y;
            public VaccineOpeningHour vaccineOpeningHour;
            public VaccineQuantityListResult vaccineQuantity;
        }
        public class VaccineOpeningHour
        {
            public bool isDayOff;
            public string standardTime;
        }
        public class VaccineQuantityListResult
        {
            public int totalQuantity;
            public string totalQuantityStatus;
            public string startTime;
            public string endTime;
            public string vaccineOrganizationCode;
            public VaccineQuantity[] list;
        }
        public class VaccineQuantity
        {
            public int quantity;
            public string quantityStatus;
            public string vaccineType;
        }


        public string BrowserBin = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        public string[] VaccineTypes = { "�ƽ�Ʈ������ī", "�Ἶ", "ȭ����", "�����" };
        public bool AutoRestart { get; set; } = true;
        public int ShowDelaySec { get; set; } = 60;
        public double X { get; set; }
        public double Y { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public int SleepTime { get; set; } = 0;
        private int Display { get; set; } = 1000;

        public bool IsRun { get; private set; }
        public DateTime LastResponse { get; private set; }
        private Dictionary<string, DateTime> showHistories = new Dictionary<string, DateTime>();


        public void Start()
        {
            if (IsRun) return;
            IsRun = true;
            Task.Factory.StartNew(Running);
        }

        public void Stop()
        {
            IsRun = false;
            while (LastResponse.Ticks != 0) Thread.Sleep(10);
        }

        private async Task Running()
        {
            try
            {
                using (HttpClient http = new HttpClient())
                {
                    while (true)
                    {
                        Stopwatch st = Stopwatch.StartNew();
                        int call = 0;
                        int error = 0;

                        // �ڷ� ����
                        List<RestListItem> items = new List<RestListItem>(1);
                        while (items.Count < items.Capacity)
                        {
                            string query = QUERY.Replace("{XXX}", X.ToString("n6"))
                                .Replace("{YYY}", Y.ToString("n6"))
                                .Replace("{DDD}", Display.ToString())
                                .Replace("{XMN}", MinX.ToString("n6"))
                                .Replace("{XMX}", MaxX.ToString("n6"))
                                .Replace("{YMN}", MinY.ToString("n6"))
                                .Replace("{YMX}", MaxY.ToString("n6"))
                                .Replace("{START}", items.Count.ToString());

                            using (var content = new StringContent(query, Encoding.UTF8, "application/json"))
                            using (var response = await http.PostAsync("https://api.place.naver.com/graphql", content))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    string jsonTxt = await response.Content.ReadAsStringAsync();
                                    if (jsonTxt.Contains("INTERNAL_SERVER_ERROR"))
                                    {
                                        error++;
                                    }
                                    else
                                    {
                                        var result = JsonMapper.ToObject<RestCollection[]>(jsonTxt);
                                        var bus = result[0].data.rests.businesses;
                                        if (bus.items.Length == 0) break;

                                        items.AddRange(bus.items);
                                        items.Capacity = Math.Max(items.Count, bus.total);
                                        call++;
                                    }
                                }
                            }
                        }

                        // �ڷ� �з�
                        var availables = (from i in items
                                          where i.vaccineQuantity != null
                                          from v in i.vaccineQuantity.list
                                          where v.quantity > 0 && VaccineTypes.Contains(v.vaccineType)
                                          select i).ToArray();

                        // ����
                        bool process = availables.Length > 0;
                        if (process)
                        {
                            Console.WriteLine();
                            foreach (var item in availables)
                            {
                                ProcessSite(item);
                            }
                        }
                        st.Stop();

                        int elapsed = (int)st.ElapsedMilliseconds;
                        Console.Write($"\r[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ������:{items.Count,-3} | ���ɼ���:{availables.Length,-3} | ����:{error,-1} | ȣ��:{call}({elapsed}ms)");

                        // ���
                        if (SleepTime > 100)
                        {
                            if (elapsed < SleepTime)
                            {
                                await Task.Delay(SleepTime - elapsed);
                            }
                            else if (elapsed > SleepTime * 2)
                            {
                                var bef = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("\r�ڡڡ� �ӵ��� �ʹ� �����ϴ�. ������ ���� �������ּ���. �ڡڡ�");
                                Console.ForegroundColor = bef;
                            }
                            else
                            {
                                var bef = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write("\r�١١� �ӵ��� �ʹ� �����ϴ�. ������ ���� �������ּ���. �١١�");
                                Console.ForegroundColor = bef;
                            }
                        }
                        else if (elapsed > 4000)
                        {
                            var bef = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("\r�ڡڡ� �ӵ��� �ʹ� �����ϴ�. ������ ���� �������ּ���. �ڡڡ�");
                            Console.ForegroundColor = bef;
                        }
                        else if (elapsed > 2000)
                        {
                            var bef = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("\r�١١� �ӵ��� �����ϴ�. ������ ���� �������ּ���. �١١�");
                            Console.ForegroundColor = bef;
                        }

                        LastResponse = DateTime.Now;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (AutoRestart)
                {
                    await Running();
                }
            }
            LastResponse = new DateTime(0);
            IsRun = false;
        }

        private bool ProcessSite(RestListItem item)
        {
            if (showHistories.TryGetValue(item.id, out var time) && (DateTime.Now - time).TotalSeconds < 60)
            {
                return false;
            }

            bool success = false;
            try
            {
                Console.WriteLine();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [�ڵ�����] {item.name} : ��û����");
                var driver = Program.browser.Open($"https://m.place.naver.com/hospital/{item.id}/home?entry=ple");

                //---------------------------------------------------------
                // ��������
                //https://m.place.naver.com/hospital/1072949398/home?entry=ple
                var st = Stopwatch.StartNew();
                while (driver.Url.ToLower().StartsWith("https://m.place.naver.com/hospital/"))
                {
                    if (st.ElapsedMilliseconds > 3000) return (success = false);
                    try
                    {
                        var target = driver.FindElementByCssSelector("div._2cgGW._2Ylqf");
                        if (target == null)
                        {
                            continue;
                        }
                        target.Click();
                        break;
                    }
                    catch (Exception e)
                    {
                    }
                }

                //---------------------------------------------------------
                // ��������
                //https://v-search.nid.naver.com/reservation/auth
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [�ڵ�����] {item.name} : ���������");
                st = Stopwatch.StartNew();
                while (driver.Url.ToLower().StartsWith("https://v-search.nid.naver.com/reservation/auth"))
                {
                    if (st.ElapsedMilliseconds > 3000) return (success = false);
                }

                //---------------------------------------------------------
                // ��û
                //https://v-search.nid.naver.com/reservation/info?key=H6X1QDNHE3rv7Zz
                try { File.WriteAllText(Program.PATH_LAST_INFO, driver.Url, Encoding.UTF8); } catch { }
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [�ڵ�����] {item.name} : ����");
                st = Stopwatch.StartNew();
                while (driver.Url.ToLower().StartsWith("https://v-search.nid.naver.com/reservation/info"))
                {
                    if (st.ElapsedMilliseconds > 3000) return (success = false);
                    try
                    {
                        var target = driver.FindElementByCssSelector("#reservation_confirm");
                        if (target == null)
                        {
                            continue;
                        }
                        target.Click();
                        break;
                    }
                    catch (Exception e)
                    {
                    }
                }

                //---------------------------------------------------------
                // ���μ���
                //https://v-search.nid.naver.com/reservation/progress?key=zYh_7wOwNxDnAMM&cd=VEN00015
                st = Stopwatch.StartNew();
                while (driver.Url.ToLower().StartsWith("https://v-search.nid.naver.com/reservation/progress"))
                {
                    if (st.ElapsedMilliseconds > 3000) return (success = false);
                }

                // ���
                //showHistories[item.id] = DateTime.Now;

                //---------------------------------------------------------
                // ���
                //https://v-search.nid.naver.com/reservation/failure?key=66tPbwxKHj2_pra&code=SOLD_OUT
                st = Stopwatch.StartNew();
                while (driver.Url.ToLower().StartsWith("https://v-search.nid.naver.com/reservation/failure")
                    || (driver.Url.ToLower().StartsWith("https://v-search.nid.naver.com/reservation/info"))
                    || (driver.Url.ToLower().StartsWith("https://v-search.nid.naver.com/reservation/auth"))
                    || (driver.Url.ToLower().StartsWith("https://m.place.naver.com/hospital/")))
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [�ڵ�����] {item.name} : ����");
                    return (success = false);
                }

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [�ڵ�����] {item.name} : ����!");
                return (success = true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ���࿡ �����ѰͰ��ƿ�! �����ϼ̴ٸ� ���͸� ���� �ٽ� �������ּ���.");
                    Console.ReadLine();
                }
                Console.WriteLine();

                Program.browser.Open($"https://naver.com/");
            }
        }
    }
}
