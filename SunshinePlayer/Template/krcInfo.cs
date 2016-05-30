using System.Runtime.Serialization;

namespace SunshinePlayer.Template {
    [DataContract]
    class krcInfo {
        [DataMember]
        public string kid { get; set; }
        [DataMember]
        public int timelength { get; set; }
        [DataMember]
        public string uid { get; set; }
        [DataMember]
        public int grade { get; set; }
        [DataMember]
        public string singer { get; set; }
        [DataMember]
        public string song { get; set; }
    }
}
