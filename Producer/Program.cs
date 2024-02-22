using Confluent.Kafka;

var config = new ProducerConfig
{
    BootstrapServers = "amq-source-kafka-bootstrap-project-source.apps-crc.testing:443",
    SslCaLocation = "certs/CARoot.pem",
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.ScramSha512,
    SaslUsername = "operation-producer",
    SaslPassword = "fnU9eNZ0aWR7b0OzB9SNzCvyzSjHuThg",
};

using (var p = new ProducerBuilder<Null, string>(config).Build()){
    while(true)
    {
        try
        {
            DateTime date = DateTime.Now;
            var dr = await p.ProduceAsync("topic-public", new Message<Null, string> { Value="test c# " + date.ToString() });
            Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
        }
        catch (ProduceException<Null, string> e)
        {
            Console.WriteLine($"Delivery failed: {e.Error.Reason}");
        }

        Thread.Sleep(3000);
    }
}

        