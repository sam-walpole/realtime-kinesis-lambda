using Amazon.CDK;

namespace Infrastructure;

class Program
{
    static void Main(string[] args)
    {
        var app = new App();

        new ClickstreamStack(app, "ClickstreamStack", new StackProps
        {
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
            }
        });

        app.Synth();
    }
}
