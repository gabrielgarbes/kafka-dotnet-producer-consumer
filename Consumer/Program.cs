using Confluent.Kafka;

var config = new ConsumerConfig
{
    BootstrapServers = "amq-source-kafka-bootstrap-project-source.apps-crc.testing:443",
    GroupId = "group-id",
    SslCaLocation = "certs/CARoot.pem",
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.ScramSha512,
    SaslUsername = "operation-consumer",
    SaslPassword = "MYkxLYA5swMSW48NcBrdQ1xWlK4OjqOa",
    AutoOffsetReset = AutoOffsetReset.Earliest,
};

using (var c = new ConsumerBuilder<Ignore, string>(config).Build())
{
    c.Subscribe("topic-public");

    CancellationTokenSource cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true; // prevent the process from terminating.
        cts.Cancel();
    };
    try
    {
        while (true)
        {
            try
            {
                var cr = c.Consume(cts.Token);
                Console.WriteLine($"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Error occurred: {e.Error.Reason}");
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Ensure the consumer leaves the group cleanly and final offsets are committed.
        c.Close();
    }
}