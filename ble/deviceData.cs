using System;
using System.Collections.Generic;
using System.Text;

namespace ble
{
    class deviceData
    {
        public class deviceInfo
        {
            public string address = "";
            public string addressType = "";
            public string type = "";
            public string company = "";
            public string name = "";
            public string txPower = "";
            public string rssi = "";
            public DateTime datetime_nearby;

            public string deviceName = "";
            public string mac = "";
            public int dataLength;
            public string eventType = "";
            public int reportCount;
        }
    }
}
