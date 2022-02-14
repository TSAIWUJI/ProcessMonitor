using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpClientTest.JsonFile
{
    internal class SendInfoEventJson
    {
        public DateTime time { get; set; }
        public string uploadTime { get; set; }
        public string type { get; set; }
        public string priority { get; set; }
        public string target { get; set; }
        public string note { get; set; }
        public string notifyEndTime { get; set; }
        public bool gisNotify { get; set; }
        public Double latitude { get; set; }
        public Double longitude { get; set; }
        public string reportUser { get; set; }
        public string notigyCloseTime { get; set; }
        public int imageCount { get; set; }
        public string expireTime { get; set; }
        public string images { get; set; }
    }
}
