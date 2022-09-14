using System.Runtime.Serialization;

namespace Lab2.services
{
    public enum HttpMethod
    {
        [EnumMember(Value = "HttpGet")]
       GET,
       [EnumMember(Value = "HttpPost")]
       POST,
    }
}