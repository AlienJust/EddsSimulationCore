using System;
using System.Collections.Generic;
using System.Linq;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using Audience;
using PollServiceProxy.Contracts;

namespace Controllers.AttachedVirtual {
  public class VirtualControllersSystem : CompositionPartBase, ISubSystem {
    private const string CommonName = "VirtualConrollersSystem";
    private ICompositionRoot _compositionRoot;

    private ICompositionPart _scadaPollGatewayPart;
    private IPollGateway _scadaPollGateway;

    private readonly bool _isDataProcessingEnabled = false;

    public override void SetCompositionRoot(ICompositionRoot root) {
      _compositionRoot = root;

      _scadaPollGatewayPart = _compositionRoot.GetPartByName("PollGateWay");
      _scadaPollGateway = _scadaPollGatewayPart as IPollGateway;
      if (_scadaPollGateway == null) throw new Exception("Не удалось найти PollGateWay через composition root");
      _scadaPollGatewayPart.AddRef();
    }

    public override string Name => CommonName;

    public string SystemName => CommonName;

    public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, IReadOnlyList<byte> data,
      Action notifyOperationComplete, Action<int, IReadOnlyList<byte>> sendReplyAction) {
      try {
        if (!_isDataProcessingEnabled) return;
        if (commandCode == 6 && data.Count >= 8) {
          var channel = data[0];
          var type = data[1];
          if (type == 250) throw new Exception("Виртуальные данные не будут отправлены");

          var number = data[2];
          var result = data.ToList();

          var minutes = result[3] == 0x06 ? 0 : 30;
          var hour = result[4];
          var day = result[5];
          var month = result[6];
          var year = 2000 + result[7];
          var certainTime = new DateTime(year, month, day, hour, minutes, 0);
          var nowTime = DateTime.Now;

          var requesthh = hour * 2 + (minutes >= 30 ? 1 : 0);
          var currenthh = nowTime.Hour * 2 + (nowTime.Minute > 30 ? 1 : 0);

          //const float floatZero = 0f;
          if (result[3] == 0) {
            // Чтение текущих данных
            result.AddRange(((float) (requesthh * 1.0)).ToBytes());
            result.AddRange(((float) (currenthh * 1.0)).ToBytes());
            result.AddRange(((float) (channel))
              .ToBytes()); // Канал используется как индикатор прибора (что посылка записана в архив верхушки для нужного контроллера)
            result.AddRange(((float) (number)).ToBytes());
            result.AddRange(((float) (type)).ToBytes());
            result.Add(0);
          }
          else if ((result[3] & 0x06) == 0x06) {
            // Чтение получасовок	
            result.AddRange(((float) (channel))
              .ToBytes()); // Канал используется как индикатор прибора (что посылка записана в архив верхушки для нужного контроллера)
            result.AddRange(((float) (number)).ToBytes());
            result.AddRange(((float) (type)).ToBytes());
            result.AddRange(BitConverter.GetBytes(requesthh));
            result.AddRange(BitConverter.GetBytes(currenthh));
            result.AddRange(
              new byte[] {
                0, 0, 0, 0,
                0, 0, 0, 0,
                channel, 0, 0, 0, 0, 0, 0
              });
          }

          sendReplyAction.Invoke(16, result);
        }
      }
      catch {
        // TODO: log
      }
      finally {
        notifyOperationComplete();
      }
    }

    public override void BecameUnused() {
      _scadaPollGatewayPart.Release();
    }
  }
}