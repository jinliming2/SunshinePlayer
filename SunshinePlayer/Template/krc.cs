using System.Runtime.Serialization;

namespace SunshinePlayer.Template {
    [DataContract]
    class krc {
        /// <summary>
        /// 返回码
        /// </summary>
        [DataMember]
        public int status { get; }
        /// <summary>
        /// 结果条数
        /// </summary>
        [DataMember]
        public int recordcount { get; }
        /// <summary>
        /// 结果数组
        /// </summary>
        [DataMember]
        public krcInfo[] data { get; }
        /// <summary>
        /// 默认id
        /// </summary>
        [DataMember]
        public string @default { get; }
    }
}
