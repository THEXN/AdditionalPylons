using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Terraria.GameContent.NetModules;
using TShockAPI.Net;
using TShockAPI.Hooks;
using Terraria.GameContent;

namespace AdditionalPylons
{
    [ApiVersion(2, 1)]
  public class AdditionalPylonsPlugin : TerrariaPlugin
  {

    private const string permission_infiniteplace = "AdditionalPylons";

    private readonly HashSet<int> pylonItemIDList = new HashSet<int>() { 4875, 4876, 4916, 4917, 4918, 4919, 4920, 4921, 4951 };
    private readonly HashSet<int> playersHoldingPylon = new HashSet<int>();
        internal static Configuration Config;
        private static void LoadConfig()
        {

            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);

        }
        private static void ReloadConfig(ReloadEventArgs args)
        {
            LoadConfig();
            args.Player?.SendSuccessMessage("[{0}]重新加载配置完毕。", typeof(AdditionalPylonsPlugin).Name);
        }

        #region Plugin Overrides
        public override void Initialize()
    {
      GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
      GetDataHandlers.PlaceTileEntity.Register(OnPlaceTileEntity, HandlerPriority.High);
            GetDataHandlers.SendTileRect.Register(OnSendTileRect, HandlerPriority.High);
           GeneralHooks.ReloadEvent += ReloadConfig;
        }
    #endregion // Plugin overrides

    #region Plugin Hooks
    private void OnSendTileRect(object sender, GetDataHandlers.SendTileRectEventArgs e)
    {
      // Respect Highest priority plugin if they really needed it...
      if (this.isDisposed || e.Handled)
        return;

      // if player doesn't even have the permissions, no need to check data
      if (!e.Player.HasPermission(permission_infiniteplace))
        return;

      // Minimum sanity checks this STR is *probably* pylon
      if (e.Width != 3 || e.Length != 4)
        return;

      long savePosition = e.Data.Position;
      NetTile[,] tiles = new NetTile[e.Width, e.Length];

      for (int x = 0; x < e.Width; x++)
      {
        for (int y = 0; y < e.Length; y++)
        {
          tiles[x, y] = new NetTile(e.Data);
          if (tiles[x, y].Type != Terraria.ID.TileID.TeleportationPylon)
          {
            e.Data.Seek(savePosition, System.IO.SeekOrigin.Begin);
            return;
          }
        }
      }

      // Reset back the data
      e.Data.Seek(savePosition, System.IO.SeekOrigin.Begin);

      // Simply clear the Main system's pylon network to fool server >:DD
      // This works simply because the pylon system is refreshed anyways when it gets placed.
      // This section is required because TShock reimplmented STR with bouncer,
      // which then calls PlaceEntityNet which rejects the pylon because internally in Main.PylonSystem already contained a pylon of this type
      Main.PylonSystem._pylons.Clear();
    }

    private void OnPlayerUpdate(object sender, TShockAPI.GetDataHandlers.PlayerUpdateEventArgs e)
    {
      if (this.isDisposed || e.Handled)
        return;

      if (!e.Player.HasPermission(permission_infiniteplace))
        return;

      int holdingItem = e.Player.TPlayer.inventory[e.SelectedItem].netID;
      bool alreadyHoldingPylon = playersHoldingPylon.Contains(e.PlayerId);
      bool isHoldingPylon = pylonItemIDList.Contains(holdingItem);

      if (alreadyHoldingPylon)
      {
        if (!isHoldingPylon)
        {
          // stopped holding pylon
          playersHoldingPylon.Remove(e.PlayerId);

          // Reload the Pylon system for player client
          SendPlayerPylonSystem(e.PlayerId, true);
        }
      }
      else
      {
        if (isHoldingPylon)
        {
          // Started holding pylon
          playersHoldingPylon.Add(e.PlayerId);

          // Clear Pylon System for player client
          SendPlayerPylonSystem(e.PlayerId, false);
        }
      }
    }

    private void OnPlaceTileEntity(object sender, TShockAPI.GetDataHandlers.PlaceTileEntityEventArgs e)
    {
      if (this.isDisposed || e.Handled)
        return;

      if (e.Type != 7)
        return;

      // Send STR to update non-inf pylons players's first pylon placement
      if (!e.Player.HasPermission(permission_infiniteplace))
      {
        TShockAPI.TSPlayer.All.SendTileRect((short)e.X, (short)e.Y, 3, 4);
        return;
      }
            Terraria.GameContent.Tile_Entities.TETeleportationPylon.Place(e.X, e.Y);

      // This is required to update the Server on the pylon list.
      // NOTE: Reset will broadcast changes to all players.
      Main.PylonSystem.Reset();

      // Send STR after manually doing TETeleportationPylon.Place() since other clients don't know about this pylon
      TShockAPI.TSPlayer.All.SendTileRect((short)e.X, (short)e.Y, 3, 4);

      playersHoldingPylon.Remove(e.Player.Index);

      //e.Handled = true;
    }
        #endregion // Plugin Hooks

        private void SendPlayerPylonSystem(int playerId, bool addPylons)
        {
            // 检测每种晶塔的数量是否达到指定值
            foreach (int pylonItemId in pylonItemIDList)
            {
                int count = Main.PylonSystem.Pylons.Count(pylon => pylon.TypeOfPylon == GetPylonTypeFromItemId(pylonItemId));

                // 根据晶塔类型进行不同的处理
                switch (pylonItemId)
                {
                    case 4875:
                        if (count >= Config.丛林晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("丛林晶塔数量已达到上限。");
                            return;
                        }
                        break;

                    case 4876:
                        if (count >= Config.森林晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("森林晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4916:
                        if (count >= Config.神圣晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("神圣晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4917:
                        if (count >= Config.洞穴晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("洞穴晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4918:
                        if (count >= Config.海洋晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("海洋晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4919:
                        if (count >= Config.沙漠晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("沙漠晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4920:
                        if (count >= Config.雪原晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("雪原晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4921:
                        if (count >= Config.蘑菇晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("蘑菇晶塔数量已达到上限。");
                            return;
                        }
                        break;
                    case 4951:
                        if (count >= Config.万能晶塔数量上限)
                        {
                            TShock.Players[playerId].SendErrorMessage("万能晶塔数量已达到上限。");
                            return;
                        }
                        break;
                }
            }

            // 如果未中断，继续发送晶塔信息
            foreach (TeleportPylonInfo pylon in Main.PylonSystem.Pylons)
            {
                Terraria.Net.NetManager.Instance.SendToClient(
                    NetTeleportPylonModule.SerializePylonWasAddedOrRemoved(
                        pylon,
                        addPylons ? NetTeleportPylonModule.SubPacketType.PylonWasAdded : NetTeleportPylonModule.SubPacketType.PylonWasRemoved
                    ),
                    playerId
                );
            }
        }

        // 根据晶塔物品ID获取对应的晶塔类型
        private TeleportPylonType GetPylonTypeFromItemId(int pylonItemId)
        {
            switch (pylonItemId)
            {
                case 4875: return TeleportPylonType.Jungle;
                case 4876: return TeleportPylonType.SurfacePurity;
                case 4916: return TeleportPylonType.Hallow;
                case 4917: return TeleportPylonType.Underground;
                case 4918: return TeleportPylonType.Beach;
                case 4919: return TeleportPylonType.Desert;
                case 4920: return TeleportPylonType.Snow;
                case 4921: return TeleportPylonType.GlowingMushroom;
                case 4951: return TeleportPylonType.Victory;
                default: return TeleportPylonType.Count; // 或者根据实际情况返回一个默认值
            }
        }






        #region Plugin Properties
        public override string Name => "[无限晶塔] AdditionalPylons";

        public override Version Version => System.Reflection.Assembly.GetAssembly(typeof(AdditionalPylonsPlugin)).GetName().Version;

        public override string Author => "Stealownz，肝帝熙恩优化1449";

        public override string Description => "晶塔环境不受限制，数量可提升";


        public AdditionalPylonsPlugin(Main game): base(game)
        {
            LoadConfig();
        }
    #endregion // Plugin Properties

    #region [IDisposable Implementation]
    private bool isDisposed = false;

    public bool IsDisposed
    {
      get { return this.isDisposed; }
    }

    protected override void Dispose(bool isDisposing)
    {
      if (this.IsDisposed)
        return;

      if (isDisposing)
      {
        GetDataHandlers.PlayerUpdate.UnRegister(OnPlayerUpdate);
        GetDataHandlers.PlaceTileEntity.UnRegister(OnPlaceTileEntity);
        GetDataHandlers.SendTileRect.UnRegister(OnSendTileRect);
      }

      base.Dispose(isDisposing);
      this.isDisposed = true;
    }
    #endregion // [IDisposable Implementation]
  }
}
