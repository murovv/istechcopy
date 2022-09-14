using System.Runtime.Serialization;

namespace SourceGenerator.services
{
    public enum HttpMethod
    {
        [EnumMember(Value = "HttpGet")]
       GET,
       [EnumMember(Value = "HttpPost")]
       POST,
    }
}