# Creating a producer and consumer in .NET 8 with SaslSsl authentication

## Prerequisites
O Visual Studio Code com a extensão C# instalada.
Kit de Desenvolvimento do C# instalada
O SDK do .NET 8.
Openshift com o AmqStreams configurados

## Creating Producer project
~~~
cd Producer
dotnet new console --framework net8.0 -n Producer
~~~

### Adding the Kafka package
~~~
dotnet add package Confluent.Kafka
~~~

### Log in to OpenShift
~~~
oc login -u kubeadmin
~~~

### Creating the directory for the certificates and generating the .pem file
~~~
mkdir certs && cd certs
oc extract -n project-source secret/amq-source-cluster-ca-cert --keys=ca.crt --to=- > ca.crt
keytool -import -trustcacerts -alias root -file ca.crt -keystore truststore.jks -storepass password -noprompt
keytool -exportcert -alias root -keystore truststore.jks -rfc -file CARoot.pem -storepass password
~~~

### Program.cs

~~~
using Confluent.Kafka;

var config = new ProducerConfig
{
    // Check the exposed route in your Kubernetes
    BootstrapServers = "amq-source-kafka-bootstrap-project-source.apps-crc.testing:443",
    SslCaLocation = "certs/CARoot.pem",
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.ScramSha512,
    
    // Retrieve the username and password that have write access to the topic that will be used
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
~~~


## Creating Consumer project
~~~
dotnet new console --framework net8.0 -n Consumer
~~~

### Adding the Kafka package
~~~
cd Consumer
dotnet add package Confluent.Kafka
~~~

### Creating the directory for the certificates and generating the .pem file
~~~
mkdir certs && cd certs
oc extract -n project-source secret/amq-source-cluster-ca-cert --keys=ca.crt --to=- > ca.crt
keytool -import -trustcacerts -alias root -file ca.crt -keystore truststore.jks -storepass password -noprompt
keytool -exportcert -alias root -keystore truststore.jks -rfc -file CARoot.pem -storepass password
~~~

### Program.cs
~~~
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
~~~