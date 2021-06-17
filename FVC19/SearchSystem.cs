using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int Display { get; set; } = 1000;

        public bool IsRun { get; private set; }
        public DateTime LastResponse { get; private set; }


        private Dictionary<string, DateTime> showHistories = new Dictionary<string, DateTime>();


        public void Start()
        {
            if (IsRun) return;
            IsRun = true;
            Running();
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
                                    var result = JsonMapper.ToObject<RestCollection[]>(jsonTxt);
                                    var bus = result[0].data.rests.businesses;
                                    if (bus.items.Length == 0) break;

                                    items.AddRange(bus.items);
                                    items.Capacity = Math.Max(items.Count, bus.total);
                                    call++;
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
                        int launch = 0;
                        foreach (var item in availables)
                        {
                            if (ShowSite(item))
                            {
                                launch++;
                            }
                        }
                        st.Stop();

                        int elapsed = (int)st.ElapsedMilliseconds;
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ������:{items.Count,-6} | ���ɼ���:{availables.Length,-6} | ����:{launch,-6} | ȣ��:({call}/{elapsed}ms)");

                        // ���
                        if (elapsed < 2000)
                        {
                            await Task.Delay(2000 - elapsed);
                        }
                        else if(elapsed > 4000)
                        {
                            var bef = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("�ڡڡ� �ӵ��� �ʹ� �����ϴ�. ������ ���� �������ּ���. �ڡڡ�");
                            Console.WriteLine("�١١�   �ӵ��� ������ ��� �߰� Ȯ���� �پ��ϴ�.   �١١�");
                            Console.ForegroundColor = bef;
                        }
                        else
                        {
                            var bef = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("�١١� �ӵ��� �ʹ� �����ϴ�. ������ ���� �������ּ���. �١١�");
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

        private bool ShowSite(RestListItem item)
        {
            try
            {
                if (showHistories.TryGetValue(item.id, out var time) && (DateTime.Now - time).TotalSeconds < 60)
                {
                    return false;
                }

                using (var p = Process.Start(new ProcessStartInfo
                {
                    FileName = BrowserBin,
                    Arguments = $"https://m.place.naver.com/hospital/{item.id}/home?entry=ple"
                }))
                {
                    p.WaitForInputIdle();
                }
                showHistories[item.id] = DateTime.Now;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
