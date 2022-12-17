﻿using ItemService.EventProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ItemService.RabbitMqClient
{
	public class RabbitMqSubscriber : BackgroundService
	{
		private readonly IConfiguration _configuration;
		private readonly string _nomeDaFila;
		private readonly IConnection _connection;
		private readonly IModel _channel;
		private IProcessaEvento _processaEvento;

		public RabbitMqSubscriber(IConfiguration configuration, IProcessaEvento processaEvento)
		{
			_configuration = configuration;
			var hostName = _configuration["RabbitMqHost"];
			var port = Int32.Parse(_configuration["RabbitMqPort"]);

			_connection = new ConnectionFactory()
			{
				HostName = hostName,
				Port = port
			}.CreateConnection();

			_channel = _connection.CreateModel();
			_channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);
			_nomeDaFila = _channel.QueueDeclare().QueueName;
			_channel.QueueBind(queue: _nomeDaFila, exchange: "trigger", routingKey: "");
			_processaEvento = processaEvento;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var consumidor = new EventingBasicConsumer(_channel);

			consumidor.Received += (ModuleHandle, ea) =>
			{
				var body = ea.Body;
				var mensagem = Encoding.UTF8.GetString(body.ToArray());
				_processaEvento.Processa(mensagem);
			};

			_channel.BasicConsume(queue: _nomeDaFila, autoAck: true, consumidor);

			return Task.CompletedTask;
		}
	}
}