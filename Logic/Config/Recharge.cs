using Aop.Api;

namespace Logic.Config
{
    public static class Recharge
    {
        public static string AlipayAppId => "2021005129639025";
        public static string AlipayPrivateKey => "MIIEpAIBAAKCAQEAnF7HODX/nNA9i1sokiYiAJ/u/V1HVTkcKciH/vE2w/uDL94JIyu5l0zUlN0TZpt6pZE+Vnf7eRuZay4SmywOuLR0BEf2VTvlh3FRxWWixVTKJjghLCGU+GhM1Qs5Xjb8bahYylaHyWFdqjp80LJpWiUstEuqWwbq5ZptFYHU3rW7AbnWK8bsz0a0JmbIil/Bv0y5Gp1iU0mGoV9oShegN8DvuKGkBID8SeLqVrb8HHD+FunRf8q6wcco9KZ1m3f6bEq53f8PyjGT9NWImnoAXsJ0s3LhGVK+KkVX3neNhFCrBOAzyKqILXmNYKCcLZCMoqjdhUVx5q0Z+MjME2h8OwIDAQABAoIBAQCXyMw6fPfswJos7kSYcNlqn9Q9LdEzIWd8C4Iu7vfJDxfNXAqkkCnrepGTGnFdlShdFHpdNsPsT+UBC8zVwNNdaRSAn1W2cJihe1bzdG+memJq7OsOSl6vLZb+6ZE7KcanrtTZO1s9F/zyYymK7tOixBj7vcLT7wgN7XNbgBYgQt5XWwCiRYnNHyBjzhqd8MJC/yUPVJ0oNTigQD2sAhVvHkJSaHD/WCVIgyyH1TLpdLcy9Bc0o/weR+Geb69PZZhCQpHXbndsd3Fgj5GfLQik52Qpo54JMJYenJN6bD4/LboVze43+yFxDqX6SGB/pfLWmKaT3UuaP3umLjZ8nX65AoGBAO95J4yPVhlnyK/APaVykKYadkgREFnWv7UdhlQa+YRBbaGnQZ4qK6k4Uqlw3x+7qiZWtwcru3Cm0KMBCT7OSbd2BWqOns85g15r1Y2FkLYRr3Fw2SWMH03KuEopaDlOX6pk/boEkn8LXFC5GGP4daLQBMrsdbvwnfqDzcZoQeinAoGBAKcpauszuupKkxgLp33Sl1PfAYZFaY3D01+hzwjkIKJ4diQIRv2brgkfArOqCFE/F3HtdR7n/izsupZL8sL/HARm+TEGHCnGvgRPn3vLAQq19gnFbaxBnqmpPEd+saaWrOlqMImCURns7xZQ6eNSGQYoupRPqOpg2WFHO+GzbK5NAoGBAN3qbBf5jFQmtPcJMxdqv1juFMZb6ccXFriED8NI7Aj/iNTQ5iHn+mXqZ8/VZS9G/TpiCWJ/yEdwjs8/Wo31JKL6n7JLUCfAqFiLnW50Y9IVOXvqk5AT9b5lKbqv/IF+e3Cv/eCv9AH/SSEVJeEekgS6uHZEAiaTqBJKqho+6zTpAoGAIW5sHrwKzt65Sl7SUZyzfSelk2gAc0TN2ltvH5UYXcX/wrcRE5l2FgbosGv6G92lX7ig6tx0/iEeM/7ef1csEElT3xUcvtIroIsYF1cDT1QS7+NXStMY20+oaZgSCYIq9MezC11PwQKc0na+QCNkM9IjdpPz8WQLNaRceog63SUCgYBT6F4sflwmtIZl9ER1dzd+Svp+1mkj50KN6DiS45GsXgbV2mST7i1qbLEg8NEQWU/FQq/zGl4Z46yNTGbsLs9LFB5s5CAG1iZ/Fys9kXArxZfjoa23l144W1DliW/iMCpdlU+xm+IutxW33z1ZJ0jTzCs7EngSxJdZ/v8BIwrAdw==";
        public static string AlipayPublicKey => "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAs117uu47yZhqC83UFt4H/n545/MctwSAQGsDMn569gf0amhebpOl3b0M1HAETVPrCh1NJGaAHPr52tl6yn6o174yK4rEhewMdlc8Atr138GFKx7XAIoWcLAuAjIL7IDOoTPke8LearqjCyayVPDSmq8VQWMR6UxkWiZ2Dtm6wFjsExNEYwxF1g+BHyTWOllv1VU5cEbe5se8upy/JmgLe+VHct4H4JsvQi3/3Q/f+cE+OgNQmG5XtOrc/uY8SbKnYjf/LzWk3f++7ifrA7lYqEdCJSvmd0aWR8eXE+ZZCbRkrwGWDy+j5ZEQtc0boAZv6/5UJgQ3cPJb1TFaxE+C2QIDAQAB";
        public static string AlipayGatewayUrl => "https://openapi.alipay.com/gateway.do";
        public static string AlipayNotifyUrl => $"http://{Logic.Agent.Instance.InternalIp}/api/alipay/notify";
        public static IAopClient AlipayAopClient => new DefaultAopClient(AlipayGatewayUrl, AlipayAppId, AlipayPrivateKey, "json", "1.0", "RSA2", AlipayPublicKey, "utf-8", false);



    }
}
