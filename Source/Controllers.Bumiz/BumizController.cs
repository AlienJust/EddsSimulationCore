using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using AJ.Std.Time;
using Audience;
using Bumiz.Apply.PulseCounterArchiveReader;

using BumizIoManager.Contracts;
using BumizNetwork.Contracts;
using Commands.Bumiz.Intelecon;
using Controllers.Contracts;
using NCalc;

namespace Controllers.Bumiz {
	internal class BumizController : IController {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGray, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));
		private readonly TimeSpan _cacheTtl;
		private DateTime? _lastCurrentDataRequestTime;
		private IEnumerable<byte> _lastCurrentDataResult;

		private readonly IBumizIoManager _bumiz;
		private readonly IPulseCounterDataStorageHolder _pcStorageHolder;
		private readonly IBumizControllerInfo _bumizControllerInfo;

		public string Name => _bumizControllerInfo.Name;


		public BumizController(IBumizIoManager bumiz, IPulseCounterDataStorageHolder pcStorageHolder, IBumizControllerInfo bumizControllerInfo) {
			_bumiz = bumiz;
			_pcStorageHolder = pcStorageHolder;
			_bumizControllerInfo = bumizControllerInfo;
			_cacheTtl = TimeSpan.FromSeconds(_bumizControllerInfo.CurrentDataCacheTtlSeconds);
		}

		public void GetDataInCallback(int command, IEnumerable<byte> data, Action<Exception, IEnumerable<byte>> callback) {
			if (command == 6) {
				var result = data.ToList();
				if (result[3] == 0) {
					Log.Log(Name + " > " + "Запрос текущих данных через шестерку");
					var cmd = new PollCommand();

					if (_lastCurrentDataRequestTime.HasValue && _lastCurrentDataResult != null && DateTime.Now - _lastCurrentDataRequestTime < _cacheTtl) {
						NamedLog("Данные взяты из кэша (время жизни кэша до: " + (_lastCurrentDataRequestTime.Value + _cacheTtl).ToString("yyyy.MM.dd-HH:mm:ss") + ") и сейчас будут отправлены: " + _lastCurrentDataResult.ToText());
						callback(null, _lastCurrentDataResult);
					}
					else {
						NamedLog("Отправка запроса в менеджер обмена по сети БУМИЗ");
						_bumiz.SendDataAsync(
							_bumizControllerInfo.Name,
							cmd,
							sendResult => {
								try {
									NamedLog("Менеджер обмена БУМИЗ вернул управление");
									if (sendResult.ChannelException == null) {
										var cmdResult = cmd.GetResult(sendResult.Bytes);
										NamedLog("Результат обмена: " + cmdResult);

										var i1 = (float)cmdResult.PhaseAcurrent;
										var i2 = (float)cmdResult.PhaseBcurrent;
										var i3 = (float)cmdResult.PhaseCcurrent;
										var p1 = (float)cmdResult.PowerT1;
										var p2 = (float)cmdResult.PowerT2;

										var bits = (byte)(
											(cmdResult.IsInFault ? 0x01 : 0x00) +
											(cmdResult.IsTurnedOn ? 0x02 : 0x00) +
											(cmdResult.NoLinkWithCounter ? 0x00 : 0x04) +
											(cmdResult.CounterType == "СЕ102" ? 0x08 : 0x00) +
											(cmdResult.IsAutoTurnOffTimerStarted ? 0x10 : 0x00) +
											(cmdResult.NoFramOrCrcError ? 0x20 : 0x00) +
											(cmdResult.NoArchives ? 0x00 : 0x40));
										NamedLog("Упаковка данных в байты ответа...");
										result.AddRange(i1.ToBytes());
										result.AddRange(i2.ToBytes());
										result.AddRange(i3.ToBytes());
										result.AddRange(p1.ToBytes());
										result.AddRange(p2.ToBytes());
										result.Add(bits);

										var channel = result[0];
										var number = result[2];
										result.Add(number);
										result.Add(0);
										result.Add(channel);
										result.Add(0);

										NamedLog("Упаковка текущих данных завершена");

										_lastCurrentDataRequestTime = DateTime.Now;
										_lastCurrentDataResult = result.ToList();

										File.AppendAllText(
											Path.Combine(Env.LogPath, _bumizControllerInfo.Name + ".read.txt"),
											DateTime.Now.ToString("yyyy.MM.dd-HH:mm:ss") + " > " +
											i1.ToString("f2") + " \t" +
											i2.ToString("f2") + " \t" +
											i3.ToString("f2") + " \t" +
											p1.ToString("f2") + " \t" +
											p2.ToString("f2") + " \t" +
											bits + Environment.NewLine);
									}
									else {
										NamedLog("Произошло внутреннее исключение при обмене: " + sendResult.ChannelException);
									}
								}
								catch (Exception ex) {
									NamedLog("После отправки команды, при обработке ответа возникло исключение: " + ex);
								}
								finally {
									NamedLog("В скаду через шлюз будет отправлен результат: " + result.ToText());
									callback(null, result);
								}
							}, IoPriority.High);
					}
				}
				else if ((result[3] & 0x06) == 0x06) {
					try {
						NamedLog("Запрос получасовых данных для " + _bumizControllerInfo.Name);
						var minutes = result[3] == 0x06 ? 0 : 30;
						var hour = result[4];
						var day = result[5];
						var month = result[6];
						var year = 2000 + result[7];
						var certainTime = new DateTime(year, month, day, hour, minutes, 0);

						if (certainTime > DateTime.Now) NamedLog("Запрос за время, которое в системе еще не достигнуто! " + certainTime.ToSimpleString());
						NamedLog("Запрос к хранилищу импульсов за время " + certainTime.ToSimpleString());

						var storedImpulses = _pcStorageHolder.Storage.GetAtomicData(Name, certainTime);
						if (storedImpulses == null) throw new Exception("Не удалось получить информацию по импульсам за время" + certainTime.ToSimpleString() + " для объекта " + Name);
						var storedIntegral = _pcStorageHolder.Storage.GetIntegralData(Name, certainTime);
						if (storedIntegral == null) throw new Exception("Не удалось получить суммарную информацию по импульсам до времени " + certainTime.ToSimpleString() + " для объекта " + Name);
						NamedLog("Запрос к хранилищу выполнен");
						NamedLog("Получены следующие суммарные данные из хранилища: " + storedIntegral);

						var exps = new List<Expression>
						{
							new Expression(_bumizControllerInfo.Pulse1Expression),
							new Expression(_bumizControllerInfo.Pulse2Expression),
							new Expression(_bumizControllerInfo.Pulse3Expression)
						};

						foreach (var expression in exps) {
							expression.Parameters.Add("p1", storedIntegral.ImpulsesCount1);
							expression.Parameters.Add("p2", storedIntegral.ImpulsesCount2);
							expression.Parameters.Add("p3", storedIntegral.ImpulsesCount3);
						}

						var rp1 = (float)((double)exps[0].Evaluate());
						var rp2 = (float)((double)exps[1].Evaluate());
						var rp3 = (float)((double)exps[2].Evaluate());

						result.AddRange(rp1.ToBytes());
						result.AddRange(rp2.ToBytes());
						result.AddRange(rp3.ToBytes());
						NamedLog("В результате применения к импульсам расчетных формул получены значения расходов: (p1, p2, p3): " + rp1.ToString("f2") + "   " + rp2.ToString("f2") + "   " + rp3.ToString("f2"));


						result.AddRange(BitConverter.GetBytes(storedIntegral.RecordsCount));
						result.AddRange(BitConverter.GetBytes(storedIntegral.CorrectRecordsCount));
						result.AddRange(BitConverter.GetBytes(storedIntegral.IncorrectRecordsCount));
						result.AddRange(BitConverter.GetBytes(storedIntegral.SupposedRecordsCount));
						NamedLog("Всего записей в хранилище: " + storedIntegral.RecordsCount);


						result.Add((byte)storedImpulses.Value.PulseCount1);
						result.Add((byte)storedImpulses.Value.PulseCount2);
						result.Add((byte)storedImpulses.Value.PulseCount3);
						result.Add((byte)storedImpulses.Value.Status);
						result.Add((byte)((storedImpulses.Value.StatusX & 0xF0) >> 8));
						result.Add((byte)(storedImpulses.Value.StatusX & 0x0F));
						result.Add((byte)(storedImpulses.Value.IsRecordCorrect ? 0x01 : 0x00));

						var channel = result[0];
						var number = result[2];
						result.Add(number);
						result.Add(0);
						result.Add(channel);
						result.Add(0);

						NamedLog("Упаковка получасовых данных завершена");
					}
					catch (Exception ex) {
						NamedLog("Будет отправлена пустая посылка, т.к. произошло исключение во время составления ответа на получас: " + ex);
					}
					finally {
						callback(null, result);
					}
				}
				else {
					NamedLog("Такая шестерка не поддерживается, будет отправлена пустая посылка");
					callback(null, result);
				}
			}
			else throw new Exception("Такая команда не поддерживается объектом БУМИЗ");
		}

		private void NamedLog(object obj) {
			Log.Log(Name + " > " + obj);
		}
	}
}