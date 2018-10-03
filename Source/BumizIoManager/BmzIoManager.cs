using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using BumizIoManager.Contracts;
using BumizNetwork.Contracts;
using BumizNetwork.Shared;
using Commands.Contracts;

namespace BumizIoManager {
	public class BmzIoManager : CompositionPartBase, IBumizIoManager {
		private readonly object _sync = new object();

		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.White, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

		private readonly Dictionary<string, IMonoChannel> _channels;
		private readonly Dictionary<string, IBumizObjectInfo> _objects;
		private readonly IWorker<Action> _sendQueueWorker;
		private readonly IWorker<Action> _notifyQueueWorker;


		public BmzIoManager() {
			_channels = XmlFactory.GetChannelsFromXml(Path.Combine(Env.CfgPath, "BumizChannels.xml"));
			_objects = XmlFactory.GetObjectsFromXml(Path.Combine(Env.CfgPath, "BumizObjects.xml"));
			_sendQueueWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("BmzIoManager.SendThread", a => a(), ThreadPriority.Normal, true, null);
			_notifyQueueWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("BmzIoManager.NotifyThread", a => a(), ThreadPriority.Normal, true, null);

			Log.Log("Система обмена с объектами БУМИЗ запущена");
		}

		private IBumizObjectInfo GetBumizObject(string objectName) {
			lock (_sync) {
				if (_objects.ContainsKey(objectName))
					return _objects[objectName];
				throw new Exception("Не удалось найти объект БУМИЗ " + objectName);
			}
		}

		private IMonoChannel GetBumizChannel(string channelName) {
			lock (_sync) {
				if (_channels.ContainsKey(channelName))
					return _channels[channelName];
				throw new Exception("Не удалось найти канал БУМИЗ " + channelName);
			}
		}

		public override string Name => "BumizIoSubSystem";

		public override void SetCompositionRoot(ICompositionRoot root) { }

		public bool BumizObjectExist(string objectName) {
			lock (_sync) {
				return _objects.ContainsKey(objectName);
			}
		}

		public IEnumerable<string> GetAllBumizObjectNames() {
			lock (_sync) {
				return _objects.Select(o => o.Key);
			}
		}

		public void SendDataAsync(string name, IInteleconCommand cmd, Action<ISendResultWithAddress> callback, IoPriority priority) {
			_sendQueueWorker.AddWork(() => {
				try {
					var bumizObj = GetBumizObject(name);
					var bumizChannel = GetBumizChannel(bumizObj.ChannelName);

					bumizChannel.SendInteleconCommandAsync(cmd, bumizObj.Address, bumizObj.Timeout, result => _notifyQueueWorker.AddWork(() => callback(result)), priority);
				}
				catch (Exception ex) {
					Log.Log("Во время отправки команды возникло исключение: " + ex);
					_notifyQueueWorker.AddWork(() => callback(new SendingResultWithAddress(null, ex, null, 0)));
				}
			});
		}

		public override void BecameUnused() {
			// TODO: stop threads,
		}
	}
}