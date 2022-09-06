namespace ImperfectTools.entity
{
    internal class CheckDomainResponse
    {

        public TencentResponse? Tencent { get; set; }
        public WechatResponse? Wechat { get; set; }

    }

    public class TencentResponse
    {
        public int Code { get; set; }
        public string? Msg { get; set; }
        public string? Wording { get; set; }

    }

    public class WechatResponse
    {
        public int Code { get; set; }
        public string? Msg { get; set; }
        public string? Wording { get; set; }
    }
}
