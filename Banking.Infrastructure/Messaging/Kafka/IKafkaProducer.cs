﻿namespace Banking.Infrastructure.Messaging.Kafka;

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, T message);

}
