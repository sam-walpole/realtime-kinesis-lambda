using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.KinesisEvents;
using ClickstreamProcessorFunction;

var serializer = new SourceGeneratorLambdaJsonSerializer<LambdaJsonContext>();

var handler = async (KinesisEvent input, ILambdaContext context) =>
{
    var function = new Function();
    return await function.FunctionHandler(input, context);
};

await LambdaBootstrapBuilder.Create(handler, serializer)
    .Build()
    .RunAsync();
