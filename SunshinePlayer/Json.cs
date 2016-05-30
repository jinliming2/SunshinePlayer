using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SunshinePlayer {
    class Json {
        /// <summary>
        /// JSON字符串实例化为对象
        /// </summary>
        /// <typeparam name="T">对象结构类型</typeparam>
        /// <param name="jsonString">JSON字符串</param>
        /// <returns>JSON对象</returns>
        public static T parse<T>(string jsonString) {
            using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }
        /// <summary>
        /// JSON对象序列化为字符串
        /// </summary>
        /// <param name="jsonObject">JSON对象</param>
        /// <returns>JSON字符串</returns>
        public static string stringify(object jsonObject) {
            using(var ms = new MemoryStream()) {
                new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
