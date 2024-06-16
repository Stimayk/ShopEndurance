using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Newtonsoft.Json.Linq;
using ShopAPI;

namespace ShopEndurance
{
    public class ShopEndurance : BasePlugin
    {
        public override string ModuleName => "[SHOP] Endurance";
        public override string ModuleDescription => "";
        public override string ModuleAuthor => "E!N";
        public override string ModuleVersion => "v1.0.0";

        private IShopApi? SHOP_API;
        private const string CategoryName = "Endurance";
        public static JObject? JsonEndurance { get; private set; }
        private readonly PlayerEndurance[] playerEndurances = new PlayerEndurance[65];

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            SHOP_API = IShopApi.Capability.Get();
            if (SHOP_API == null) return;

            LoadConfig();
            InitializeShopItems();
            SetupTimersAndListeners();
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(ModuleDirectory, "../../configs/plugins/Shop/Endurance.json");
            if (File.Exists(configPath))
            {
                JsonEndurance = JObject.Parse(File.ReadAllText(configPath));
            }
        }

        private void InitializeShopItems()
        {
            if (JsonEndurance == null || SHOP_API == null) return;

            SHOP_API.CreateCategory(CategoryName, "Выносливость");

            foreach (var item in JsonEndurance.Properties().Where(p => p.Value is JObject))
            {
                Task.Run(async () =>
                {
                    int itemId = await SHOP_API.AddItem(
                        item.Name,
                        (string)item.Value["name"]!,
                        CategoryName,
                        (int)item.Value["price"]!,
                        (int)item.Value["sellprice"]!,
                        (int)item.Value["duration"]!
                    );
                    SHOP_API.SetItemCallbacks(itemId, OnClientBuyItem, OnClientSellItem, OnClientToggleItem);
                }).Wait();
            }
        }

        private void SetupTimersAndListeners()
        {
            RegisterListener<Listeners.OnClientDisconnect>(playerSlot => playerEndurances[playerSlot] = null!);

            RegisterListener<Listeners.OnTick>(() =>
            {
                for (int i = 1; i <= Server.MaxPlayers; i++)
                {
                    var player = Utilities.GetPlayerFromSlot(i);
                    if (player != null && playerEndurances[i] != null && player.PawnIsAlive)
                    {
                        var playerPawn = player.PlayerPawn.Value;
                        if (playerPawn != null && playerPawn.VelocityModifier < 1.0f)
                        {
                            playerPawn.VelocityModifier = 1.0f;
                        }
                    }
                }
            });
        }

        public void OnClientBuyItem(CCSPlayerController player, int itemId, string categoryName, string uniqueName, int buyPrice, int sellPrice, int duration, int count)
        {
            playerEndurances[player.Slot] = new PlayerEndurance(itemId);
        }

        public void OnClientToggleItem(CCSPlayerController player, int itemId, string uniqueName, int state)
        {
            if (state == 1)
            {
                playerEndurances[player.Slot] = new PlayerEndurance(itemId);
            }
            else if (state == 0)
            {
                OnClientSellItem(player, itemId, uniqueName, 0);
            }
        }

        public void OnClientSellItem(CCSPlayerController player, int itemId, string uniqueName, int sellPrice)
        {
            playerEndurances[player.Slot] = null!;
        }

        public record class PlayerEndurance(int ItemID);
    }
}