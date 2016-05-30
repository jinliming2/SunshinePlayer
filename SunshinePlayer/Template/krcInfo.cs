using System.Runtime.Serialization;

namespace SunshinePlayer.Template {
    [DataContract]
    class krcInfo {
        [DataMember]
        public string kid { get; }
        [DataMember]
        public int timelength { get; }
        [DataMember]
        public string uid { get; }
        [DataMember]
        public int grade { get; }
        [DataMember]
        public string singer { get; }
        [DataMember]
        public string song { get; }
    }
}
