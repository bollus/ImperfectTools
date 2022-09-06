using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinAuth;

namespace ImperfectTools.entity
{
    public class GoogleAuthenticatorCache
    {
        public string? Issuer { get; set; }
        public GoogleAuthenticator? GoogleAuthenticator { get; set; }

        // 有参构造函数 
        public GoogleAuthenticatorCache(string? issuer, GoogleAuthenticator? googleAuthenticator)
        {
            Issuer = issuer;
            GoogleAuthenticator = googleAuthenticator;
        }
    }
}
