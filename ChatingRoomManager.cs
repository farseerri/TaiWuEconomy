using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;
using GameData.Domains.Character.Display;
using GameData.GameDataBridge;
using GameData.Utilities;
using Steamworks;
using System.Collections;
using ChatingRoom;
using System.Reflection;
using UnityEngine.UI;
using LitJson;
using GameData.Domains.Item.Display;
using GameData.Domains.Item;
using FrameWork;
using GameData.Domains;
using GameData.Serializer;
using GameData.Domains.Taiwu;
using GameData.Domains.Character;
using GameData.Domains.Extra;
using UILogic.DisplayDataStructure;
using GameData.Domains.LifeRecord.GeneralRecord;
using System.IO;
using UICommon.Character.Avatar;

namespace TaiWuEconomy
{
    [PluginConfig(pluginName: "联机聊天Mod", creatorId: "木木", pluginVersion: "0.1")]
    public class ChatingRoomManager : TaiwuRemakePlugin
    {
        public static ChatingRoomManager Inst;

        public static Action<CharacterDisplayData, sbyte> GetCharacterDisplayDataAction;

        public static ushort customDomainId = 0x42;

        Harmony harmony;

        public static RectTransform mainUIRT;
        public static string taiWuGender;
        public static string taiWuName;
        public static bool isTaiwuVillage;
        public static List<ItemDisplayData> itemDisplayDataListForSelling;
        public static ItemView itemViewPrefab;
        public static ItemView itemViewForSelling;
        public static ItemView itemViewForBuying;
        public static UI_Bottom uI_Bottom;
        public static ItemKey ikReceived;
        public static ItemKey ikCreate;
        public static int activePlayerCount = 0;
        public static GameObject maskAvatarFramePrefab;
        public static GameObject yourSellingNPCMaskAvatarFrame;
        public static GameObject otherSellingNPCMaskAvatarFrame;
        private struct Lobby
        {
            public CSteamID Id;
            public string Name;
            public string gameName;
            public int playerCount;
            public int maxPlayers;
        }

        private List<Lobby> lobbies = new List<Lobby>();
        public static CSteamID lobbyID;
        static string lobbyName = "";
        static string lobbyNamePre = "TaiWuChatingRoom";

        private Callback<LobbyCreated_t> lobbyCreatedCallback;
        private Callback<LobbyMatchList_t> lobbyMatchListCallback;
        private Callback<LobbyEnter_t> lobbyEnteredCallback;
        private static Callback<LobbyChatMsg_t> lobbyChatMsgCallback;
        private Callback<LobbyDataUpdate_t> lobbyDataUpdateCallback;
        private Callback<AvatarImageLoaded_t> avatarImageLoaded;
        private Callback<P2PSessionRequest_t> p2PSessionRequestCallback;

        public IEnumerator refreshMemberListEnumerator;

        public static GameObject chatingRoomEnterButtonPrefab;
        public static GameObject chatingRoomMainPanelPrefab;
        public static GameObject userbarlPrefab;
        public static GameObject dropdownPrefab;
        public static GameObject taiwuChatingBarPrefab;

        public static ChatingRoomMainPanel chatingRoomMainPanel;
        public static CSteamID targetID;

        public static InputField inputText;

        public static Dictionary<CSteamID, string> members;
        public static string taiwuID;
        public static CharacterDisplayData taiwuCharacterDisplayData;

        public static int receivedGoodsDataAmount;
        public static FullItemItemInfoClass sendItemItemInfo;
        public static List<string> lastMessageList = new List<string>();
        public static int lastMessageHoldCount = 100;
        public static Color32 specialMemberColor = Color.red;

        public class CustomMessage
        {
            public string senderID;
            public string receiveID;
            public string commandID;
            public string commandContent;
        }
        public enum PublishingTypeEnum
        {
            NoPublishing,
            PublishGoods,
            PublishHumans,
            PublishResource
        }
        public class FullItemItemInfoClass
        {
            public List<byte> goodBytes;
            public List<byte> refinedEffectsBytes;
            public List<byte> poisonEffectsBytes;
            public FullItemItemInfoClass()
            {
                goodBytes = new List<byte>();
                refinedEffectsBytes = new List<byte>();
                poisonEffectsBytes = new List<byte>();
            }
        }




        public class CustomPlayerDataClass
        {
            public sbyte ItemType;
            public byte ModificationState;
            public short TemplateId;
            public int Id;
            public int amount;
            public string customPrice;
            public FullItemItemInfoClass fullItemItemInfo;
            public string tradeType;
            public string resourceType;
            public string resourceCount;
            public string npcData;

            public CustomPlayerDataClass()
            {
                tradeType = "";
                ItemType = -1;
                ModificationState = 0;
                TemplateId = -1;
                Id = -1;
                amount = 0;
                customPrice = "0";
                fullItemItemInfo = new FullItemItemInfoClass();
                resourceType = "食物";
                resourceCount = "0";
                npcData = "";
            }

        }

        public static CustomMessage sendMessage;
        public static CustomMessage receiveMessage;

        public static RectTransform chatingContent;
        public static Button chatingRoomEnterButton;
        public static string steamModPath = "";
        private string Path
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
            }
        }

        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
            DestoryChatingRoomPanel();
        }

        public override void Initialize()
        {
            lobbyName = lobbyNamePre;
            ChatingRoomManager.Inst = this;


            Console.WriteLine("太吾联机聊天mod启动");

            harmony = Harmony.CreateAndPatchAll(typeof(ChatingRoomManager));

            Game.Instance.StartCoroutine(LoadAssetAsync());


            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
            lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            lobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
            avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
            p2PSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            GetCharacterDisplayDataAction += GetCharacterDisplayData;


            GameObject itemViewGo = FrameWork.ModSystem.GameObjectCreationUtils.GetPrefab("ItemDetailView");
            if (itemViewGo != null)
            {
                itemViewPrefab = itemViewGo.GetComponent<ItemView>();
                if (itemViewPrefab == null)
                {
                    itemViewPrefab = itemViewGo.AddComponent<ItemView>();
                }

            }
        }

        public IEnumerator LoadAssetAsync()
        {
            steamModPath = Application.dataPath + "/../../../workshop/content/838350/2944926888";
            //Console.WriteLine(this.Path);

            AssetBundleCreateRequest mumuAssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(steamModPath + "/Plugins/taiwuresource");
            yield return mumuAssetBundleCreateRequest;
            AssetBundle mumuAssetBundle = mumuAssetBundleCreateRequest.assetBundle;
            if (mumuAssetBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle:taiwuresource!");
                yield break;
            }

            chatingRoomEnterButtonPrefab = mumuAssetBundle.LoadAsset<GameObject>("TaiWuChatingRoomEnterButton");
            chatingRoomMainPanelPrefab = mumuAssetBundle.LoadAsset<GameObject>("TaiWuChatingRoomMainPanel");
            userbarlPrefab = mumuAssetBundle.LoadAsset<GameObject>("TaiWuUserbar");
            dropdownPrefab = mumuAssetBundle.LoadAsset<GameObject>("TaiWuDropdown");
            taiwuChatingBarPrefab = mumuAssetBundle.LoadAsset<GameObject>("TaiwuChatingBar");


            ResLoader.Load<GameObject>("RemakeResources/Prefab/Core/Character/MaskAvatarFrame", delegate (GameObject obj)
            {
                PoolItem _maskAvatarPool = new PoolItem("RemakeResources/Prefab/Core/Character/MaskAvatarFrame", obj);
                maskAvatarFramePrefab = _maskAvatarPool.GetObject();
            }, null);
            yield break;
        }



        [HarmonyPrefix, HarmonyPatch(declaringType: typeof(UI_Bottom), methodName: "OnEnable")]
        public static bool UI_Bottom_OnEnable_Prefix(ref UI_Bottom __instance)
        {
            uI_Bottom = __instance;
            //isTaiwuVillage = System.Convert.ToBoolean(Traverse.Create(__instance).Field("_isTaiwuVillage").GetValue());
            GetCharacterDisplayDataAndBirthMonth(SingletonObject.getInstance<BasicGameData>().TaiwuCharId.ToString(), GetCharacterDisplayDataAction);
            //if (isTaiwuVillage)
            //{
            List<RectTransform> rtList = Camera.main.GetComponentInChildren<Canvas>().GetComponentsInChildren<RectTransform>().ToList();

            mainUIRT = __instance.gameObject.GetComponent<RectTransform>();


            if (chatingRoomEnterButton == null)
            {
                GameObject go = UnityEngine.GameObject.Instantiate<GameObject>(chatingRoomEnterButtonPrefab, mainUIRT);

                chatingRoomEnterButton = go.GetComponent<Button>();
                chatingRoomEnterButton.transform.localPosition = new Vector3(-1030, 50, 0);
                chatingRoomEnterButton.transform.localScale = Vector3.one;
                chatingRoomEnterButton.name = "ChatingRoomEnterButton";
                go.GetComponent<RectTransform>().SetAsLastSibling();
                SeachingExistingChatingRoom(lobbyName);
                chatingRoomEnterButton.onClick.AddListener(() => PopChatingRoomMainPanel());


            }
            //}



            return true;
        }

        [HarmonyPrefix, HarmonyPatch(declaringType: typeof(UI_MainMenu), methodName: "OnEnable")]
        public static bool UI_MainMenu_OnEnable_Prefix(UI_Bottom __instance)
        {
            DestoryChatingRoomPanel();
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(declaringType: typeof(UI_Warehouse), methodName: "OnEnable")]
        public static bool UI_Warehouse_OnEnable_Prefix(ref UI_Warehouse __instance)
        {
            //Console.WriteLine("打开了公库，下架拍品和资源公告");
            //下架卖品();
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(declaringType: typeof(UI_Shop), methodName: "OnDisable")]
        public static bool UI_Shop_OnDisable_Prefix(ref UI_Shop __instance)
        {
            //Console.WriteLine("与商人交易，下架拍品和资源公告");
            //下架卖品();

            if (ChatingRoomManager.chatingRoomMainPanel != null)
            {
                itemDisplayDataListForSelling = Traverse.Create(__instance).Field("_treasuryItems").GetValue<List<ItemDisplayData>>();
                if (itemDisplayDataListForSelling.Count == 0)
                {
                    ChatingRoomMainPanel.sellingTips.text = "公库里的物品空了，清物品售卖状态";
                    ChatingRoomMainPanel.publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
                    ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                }

            }



            return true;
        }


        [HarmonyPostfix, HarmonyPatch(declaringType: typeof(UI_Warehouse), methodName: "OnDisable")]
        public static void UI_Warehouse_OnDisable_Postfix(ref UI_Warehouse __instance)
        {
            if (ChatingRoomManager.chatingRoomMainPanel != null)
            {
                itemDisplayDataListForSelling = Traverse.Create(__instance).Field("_treasuryItems").GetValue<List<ItemDisplayData>>();
                if (itemDisplayDataListForSelling.Count == 1)
                {
                    if (ChatingRoomManager.itemViewForSelling != null)
                    {
                        GameObject.Destroy(ChatingRoomManager.itemViewForSelling.gameObject);
                        ChatingRoomManager.itemViewForSelling = null;
                    }

                    itemViewForSelling = ItemView.Instantiate(itemViewPrefab, ChatingRoomMainPanel.yourWTSItemItemCell.transform);

                    itemViewForSelling.transform.SetParent(ChatingRoomMainPanel.yourWTSItemItemCell.transform);
                    itemViewForSelling.transform.localPosition = Vector3.zero;
                    itemViewForSelling.SetData(itemDisplayDataListForSelling[0], true);
                    ChatingRoomMainPanel.sellPriceInputField.text = ((int)(ChatingRoomManager.itemViewForSelling.Data.Price * 0.9f) * ChatingRoomManager.itemViewForSelling.Data.Amount).ToString();

                    int listener0 = -1;
                    listener0 = GameData.GameDataBridge.GameDataBridge.RegisterListener(delegate (List<NotificationWrapper> notifications)
                    {
                        NotificationWrapper wrapper = notifications[0];
                        string goodString = "";
                        RawDataPool dataPool = wrapper.DataPool;
                        int valueOffset = wrapper.Notification.ValueOffset;
                        valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref goodString);

                        sendItemItemInfo = JsonMapper.ToObject<FullItemItemInfoClass>(goodString);

                        Console.WriteLine("获取到了完整的sendItemItemInfo");

                        GameData.GameDataBridge.GameDataBridge.UnregisterListener(listener0);
                    });
                    GameData.GameDataBridge.GameDataBridge.AddMethodCall<ItemKey>(listener0, customDomainId, 50, itemDisplayDataListForSelling[0].Key);

                }
                else
                {
                    ChatingRoomMainPanel.sellingTips.text = "当公库只有一格物品时，才能摆摊";
                    ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                    ChatingRoomMainPanel.publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
                }
            }

            if (chatingRoomMainPanel != null)
            {
                Traverse.Create(UIManager.Instance).Field("_blockHotKey").SetValue(true);
            }
        }

        [HarmonyPrefix, HarmonyPatch(declaringType: typeof(UI_Bottom), methodName: "OnClick", argumentTypes: new Type[] { typeof(CButton) })]
        public static bool UI_Bottom_OnClick_Prefix(ref UI_Bottom __instance, CButton btn)
        {
            string btnName = btn.name;

            if (btnName == "AdvanceSolarTerm")
            {
                //ChatingRoomManager.下架卖品();
                //if (chatingRoomMainPanel != null)
                //{
                //    ChatingRoomManager.chatingRoomMainPanel.Close();
                //}

            }

            return true;
        }






        public void CreateLobby()
        {
            // 创建Lobby
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 250);
            //SteamNetworking.SendP2PPacket(,, EP2PSend.k_EP2PSendReliable);
        }


        public void JoinLobby(CSteamID lobbyId)
        {
            SteamMatchmaking.JoinLobby(lobbyId);
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t request)
        {
            CSteamID clientId = request.m_steamIDRemote;
            //if (ExpectingClient(clientId))
            //{
            SteamNetworking.AcceptP2PSessionWithUser(clientId);
            //}
            //else
            //{
            //    Debug.LogWarning("Unexpected session request from " + clientId);
            //}
        }

        private void OnLobbyCreated(LobbyCreated_t callback)
        {

            if (callback.m_eResult == EResult.k_EResultOK)
            {
                Console.WriteLine("Lobby created,LobbyName:" + lobbyName + " Lobby ID: " + callback.m_ulSteamIDLobby);
                lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
                SteamMatchmaking.SetLobbyData(lobbyID, "name", lobbyName); ;



            }
            else
            {
                Debug.LogError("Failed to create lobby: " + callback.m_eResult);
            }

        }


        private void OnLobbyEntered(LobbyEnter_t callback)
        {
            EChatRoomEnterResponse response = (EChatRoomEnterResponse)Enum.ToObject(typeof(EChatRoomEnterResponse), callback.m_EChatRoomEnterResponse);

            if (response == EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Console.WriteLine("Entered lobby: " + callback.m_ulSteamIDLobby);
                lobbyID = new CSteamID(callback.m_ulSteamIDLobby);


                if (lobbyChatMsgCallback == null)
                {
                    lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
                }

                Type uIManager = typeof(UIManager);
                //FieldInfo myPrivateVariableInfo = UIManager.GetField("BlockHotKey", BindingFlags.NonPublic | BindingFlags.Instance);
                if (ChatingRoomManager.Inst.refreshMemberListEnumerator != null)
                {
                    Game.Instance.StopCoroutine(ChatingRoomManager.Inst.refreshMemberListEnumerator);
                }
                ChatingRoomManager.Inst.refreshMemberListEnumerator = ChatingRoomManager.Inst.RefreshMemberListEnumerator();
                Game.Instance.StartCoroutine(ChatingRoomManager.Inst.refreshMemberListEnumerator);

            }
            else
            {
                Debug.LogError("Failed to enter lobby: " + callback.m_EChatRoomEnterResponse);
            }

        }



        public static void SeachingExistingChatingRoom(string lobbyName)
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter("name", lobbyName, ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(10);
            SteamMatchmaking.RequestLobbyList();
        }
        private class PlayerCountComparer : IComparer<Lobby>
        {
            public int Compare(Lobby x, Lobby y)
            {
                if (x.playerCount < y.playerCount)
                {
                    return 1;
                }
                else if (x.playerCount > y.playerCount)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private void OnLobbyMatchList(LobbyMatchList_t pCallback)
        {
            lobbies.Clear();

            // 显示搜索到的Lobby数量
            Console.WriteLine("搜索到 " + pCallback.m_nLobbiesMatching + " 个Lobby");

            // 遍历所有搜索到的Lobby
            for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
            {
                // 获取Lobby信息
                CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name");
                string gameName = SteamMatchmaking.GetLobbyData(lobbyId, "game");
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

                // 将Lobby添加到列表中
                lobbies.Add(new Lobby { Id = lobbyId, Name = lobbyName, playerCount = playerCount, maxPlayers = maxPlayers, gameName = gameName });

            }


            if (lobbies.Count > 0)
            {
                lobbies.Sort(new PlayerCountComparer());

                foreach (Lobby lobby in lobbies)
                {
                    Console.WriteLine("搜到Lobby:" + lobby.Name + " Id=" + lobby.Id + " 人数" + lobby.playerCount + "/" + lobby.maxPlayers);
                }

                if (lobbies[0].Name == lobbyName)
                {
                    if (lobbies[0].playerCount < lobbies[0].maxPlayers)
                    {
                        Console.WriteLine("Lobby未满，加入");
                        JoinLobby(lobbies[0].Id);

                    }
                    else
                    {
                        Console.WriteLine("Lobby已满，请稍后尝试");
                    }
                }
            }
            else
            {
                Console.WriteLine("没有搜索到存在的Lobby,创建Lobby");
                CreateLobby();
            }

        }





        private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
        {
            if (callback.m_ulSteamIDLobby == lobbyID.m_SteamID)
            {

            }

        }

        private void OnLobbyChatMessage(LobbyChatMsg_t chatMsg)
        {
            if (chatMsg.m_ulSteamIDLobby == lobbyID.m_SteamID)
            {
                byte[] data = new byte[4096];
                CSteamID steamID;
                EChatEntryType chatEntryType;

                int dataSize = SteamMatchmaking.GetLobbyChatEntry(lobbyID, (int)chatMsg.m_iChatID, out steamID, data, data.Length, out chatEntryType);
                if (dataSize > 0)
                {
                    //string userName = SteamFriends.GetFriendPersonaName(steamID);
                    string message = System.Text.Encoding.UTF8.GetString(data, 0, dataSize);
                    PraseReceivedChatingRoomMessage(message);
                    chatingRoomMainPanel.StartCoroutine(AutoScrollChatingBar());
                }

            }
        }

        public IEnumerator AutoScrollChatingBar()
        {
            yield return null;

            if (Input.GetMouseButton(0))
            {

            }
            else if (chatingRoomMainPanel.chatingScrollRect.verticalScrollbar.value > 0.9)
            {

                chatingRoomMainPanel.chatingScrollRect.verticalNormalizedPosition = 0;
            }
            else if (chatingRoomMainPanel.chatingScrollRect.verticalScrollbar.value > 0.2f && chatingRoomMainPanel.chatingScrollRect.verticalScrollbar.value <= 0.9)
            {

            }
            else if (chatingRoomMainPanel.chatingScrollRect.verticalScrollbar.value > 0f && chatingRoomMainPanel.chatingScrollRect.verticalScrollbar.value <= 0.2f)
            {


                chatingRoomMainPanel.chatingScrollRect.verticalNormalizedPosition = 0;
            }

        }





        public void PraseReceivedChatingRoomMessage(string message)
        {
            try
            {
                CustomMessage messgeJsonObj = JsonMapper.ToObject<CustomMessage>(message);

                switch (messgeJsonObj.commandID)
                {
                    case "普通消息":
                        {
                            ulong otherID = ulong.Parse(messgeJsonObj.senderID);
                            //string memberName = SteamMatchmaking.GetLobbyMemberData(lobbyID, (CSteamID)otherID, "Name");
                            string memberName = SteamMatchmaking.GetLobbyMemberData(lobbyID, (CSteamID)otherID, "taiwuZhenMing");
                            string commandContent = DateTime.Now.ToString("[HH:mm]") + memberName + "说:" + messgeJsonObj.commandContent;

                            Console.WriteLine(commandContent);

                            while (lastMessageList.Count > lastMessageHoldCount)
                            {
                                lastMessageList.RemoveAt(0);
                            }

                            lastMessageList.Add(commandContent);


                            Console.WriteLine("当前聊天记录数:" + lastMessageList.Count);
                            if (ChatingRoomManager.chatingRoomMainPanel == null)
                            {
                                return;
                            }

                            if (ChatingRoomManager.chatingRoomMainPanel != null && chatingRoomMainPanel.isActiveAndEnabled)
                            {
                                if (ChatingRoomManager.chatingContent.childCount > 200)
                                {
                                    foreach (Transform child in ChatingRoomManager.chatingContent)
                                    {
                                        GameObject.Destroy(child.gameObject);
                                    }
                                }


                                Image taiwuChatingBar = GameObject.Instantiate(ChatingRoomManager.taiwuChatingBarPrefab, ChatingRoomManager.chatingContent).GetComponent<Image>();
                                Text taiwuChatingText = taiwuChatingBar.GetComponentInChildren<Text>();

                                if (ChatingRoomMainPanel.lastChatingColor == null)
                                {
                                    ChatingRoomMainPanel.lastChatingColor = Color.black;
                                }

                                if (ChatingRoomMainPanel.lastChatingColor == Color.white)
                                {
                                    taiwuChatingBar.color = Color.black;
                                }
                                else
                                {
                                    taiwuChatingBar.color = Color.white;
                                }
                                ChatingRoomMainPanel.lastChatingColor = taiwuChatingBar.color;

                                taiwuChatingText.text = SensitiveWordsSystem.Instance.GetLegalResult(commandContent) + "\r\n";


                            }
                        }
                        break;

                    case "请求摆摊数据":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            string commandContent = senderName + "向" + receiveName + "请求摆摊数据";
                            Console.WriteLine(commandContent);
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {

                                if (ChatingRoomMainPanel.publishingType != ChatingRoomManager.PublishingTypeEnum.NoPublishing)
                                {
                                    Console.WriteLine("接收到了摆摊数据请求，回复摆摊信息");
                                    SendChatMessage(messgeJsonObj.senderID.ToString(), "回复摆摊信息", ChatingRoomMainPanel.sellingString);
                                }
                                else
                                {
                                    Console.WriteLine("接收到了摆摊数据请求，拒绝回复摆摊信息");
                                    SendChatMessage(messgeJsonObj.senderID.ToString(), "拒绝回复摆摊信息", "");
                                }
                            }
                        }
                        break;

                    case "回复摆摊信息":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            string commandContent = senderName + "向" + receiveName + "回复摆摊信息";
                            Console.WriteLine(commandContent);
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                Console.WriteLine(messgeJsonObj.commandContent);
                                ChatingRoomManager.CustomPlayerDataClass receivedData = JsonMapper.ToObject<ChatingRoomManager.CustomPlayerDataClass>(messgeJsonObj.commandContent);

                                //if (receivedData.tradeType != ChatingRoomMainPanel.currentTadeType)
                                //{
                                //    Console.WriteLine("买卖需求不对");
                                //    return;
                                //}

                                if (ChatingRoomManager.itemViewForBuying != null)
                                {
                                    GameObject.Destroy(ChatingRoomManager.itemViewForBuying.gameObject);
                                }

                                if (receivedData.tradeType == "发布摆摊信息")
                                {
                                    ChatingRoomMainPanel.otherPlayerItemCell.gameObject.SetActive(true);
                                    ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(false);
                                    ChatingRoomMainPanel.otherPlayerSellingNPCBlock.SetActive(false);

                                    ikReceived = new ItemKey(receivedData.ItemType, receivedData.ModificationState, receivedData.TemplateId, receivedData.Id);
                                    receivedGoodsDataAmount = receivedData.amount;


                                    string receivedItemString = JsonMapper.ToJson(receivedData.fullItemItemInfo);


                                    int listener0 = -1;
                                    listener0 = GameData.GameDataBridge.GameDataBridge.RegisterListener(delegate (List<NotificationWrapper> notifications)
                                    {
                                        NotificationWrapper wrapper = notifications[0];
                                        ikCreate = new ItemKey();
                                        RawDataPool dataPool = wrapper.DataPool;
                                        int valueOffset = wrapper.Notification.ValueOffset;
                                        valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref ikCreate);
                                        Console.WriteLine("生成新ik:" + ikCreate.ToString());
                                        ChatingRoomManager.uI_Bottom.AsynchMethodCall(DomainHelper.DomainIds.Item, ItemDomainHelper.MethodIds.GetItemDisplayData, ikCreate, int.Parse(ChatingRoomManager.taiwuID), GetItemDisplayDataCallback);
                                        ChatingRoomMainPanel.otherPriceText.text = receivedData.customPrice;
                                        ChatingRoomMainPanel.buyingString = messgeJsonObj.commandContent;
                                        ChatingRoomMainPanel.buyButton.interactable = true;
                                        GameData.GameDataBridge.GameDataBridge.UnregisterListener(listener0);
                                    });
                                    Console.WriteLine("获取到旧ik:" + ikReceived.ToString());
                                    GameData.GameDataBridge.GameDataBridge.AddMethodCall<ItemKey, string>(listener0, customDomainId, 51, ikReceived, receivedItemString);
                                }
                                else if (receivedData.tradeType == "求购资源")
                                {
                                    ChatingRoomMainPanel.resourceNeedPublishButton.interactable = true;
                                    ChatingRoomMainPanel.otherPlayerItemCell.gameObject.SetActive(false);
                                    ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(true);
                                    ChatingRoomMainPanel.otherPlayerSellingNPCBlock.SetActive(false);
                                    ChatingRoomMainPanel.otherTadeResourceType = receivedData.resourceType;
                                    ChatingRoomMainPanel.otherResourceText.text = "对方正在求购" + receivedData.resourceCount + "数量的" + ChatingRoomMainPanel.otherTadeResourceType + "资源\r\n他将以以下的金钱兑换";
                                    ChatingRoomMainPanel.otherResourceTradePrice.text = (int.Parse(receivedData.resourceCount) * ChatingRoomMainPanel.resourceSellingRate).ToString();

                                    ChatingRoomMainPanel.buyingString = messgeJsonObj.commandContent;
                                    Console.WriteLine("对方需求资源，装载buyingString:" + ChatingRoomMainPanel.buyingString);
                                }
                                else if (receivedData.tradeType == "贩卖人口")
                                {
                                    ChatingRoomMainPanel.otherPlayerItemCell.gameObject.SetActive(false);
                                    ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(false);
                                    ChatingRoomMainPanel.otherPlayerSellingNPCBlock.SetActive(true);
                                    ChatingRoomMainPanel.otherPlayerSellingNPCBuyButton.interactable = true;
                                    ChatingRoomMainPanel.CreateNewNonIntelligentCharacterByB64(receivedData.npcData);
                                    ChatingRoomMainPanel.otherNPCPriceText.text = receivedData.customPrice;
                                    ChatingRoomMainPanel.buyingString = messgeJsonObj.commandContent;
                                    Console.WriteLine("对方正在贩卖贩卖人口:" + receivedData.npcData);
                                }


                                ChatingRoomMainPanel.addFriendButton.onClick.RemoveAllListeners();
                                ChatingRoomMainPanel.addFriendButton.onClick.AddListener(() => AddSteamFirend(targetID));


                            }


                        }
                        break;

                    case "拒绝回复摆摊信息":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            Console.WriteLine(senderName + "拒绝回复" + receiveName + "摆摊信息");
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                if (ChatingRoomManager.chatingRoomMainPanel != null)
                                {
                                    ChatingRoomMainPanel.otherPlayerItemCell.gameObject.SetActive(false);
                                    ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(false);
                                }
                                ulong otherID = ulong.Parse(messgeJsonObj.senderID);
                                string memberName = SteamFriends.GetFriendPersonaName((CSteamID)otherID);

                                ClearBuyingGoodsInfo();
                                ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                            }

                            ChatingRoomMainPanel.addFriendButton.onClick.RemoveAllListeners();
                            ChatingRoomMainPanel.addFriendButton.onClick.AddListener(() => AddSteamFirend(targetID));
                        }
                        break;

                    case "请求交易":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            string commandContent = senderName + "向" + receiveName + "请求交易";
                            Console.WriteLine("messgeJsonObj.commandContent:" + messgeJsonObj.commandContent);
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {

                                CustomPlayerDataClass buyerData = JsonMapper.ToObject<CustomPlayerDataClass>(messgeJsonObj.commandContent);
                                if (buyerData.tradeType == "发布摆摊信息")
                                {

                                    if (itemDisplayDataListForSelling.Count == 0)
                                    {
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "物品被抢", "");
                                        ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                                        UnloadSelliingGoods();
                                        return;
                                    }

                                    if (ChatingRoomMainPanel.publishingType == ChatingRoomManager.PublishingTypeEnum.PublishGoods)
                                    {
                                        CustomPlayerDataClass playerSellingData = JsonMapper.ToObject<CustomPlayerDataClass>(ChatingRoomMainPanel.sellingString);
                                        ItemKey ikSelling = new ItemKey(playerSellingData.ItemType, playerSellingData.ModificationState, playerSellingData.TemplateId, playerSellingData.Id);

                                        Console.WriteLine("我卖的物品的itemkeyID:" + ikSelling.Id);
                                        Console.WriteLine("对方回传的物品itemkeyID:" + buyerData.Id);
                                        if (ikSelling.TemplateId == buyerData.TemplateId && playerSellingData.amount == buyerData.amount)
                                        {

                                            Console.WriteLine(receiveName + "接收请求并回复" + senderName);
                                            ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "确认交易", ChatingRoomMainPanel.sellingString);

                                            ClearBuyingGoodsInfo();
                                            ChatingRoomManager.CustomAddResource(ResourceType.Money, ChatingRoomMainPanel.lastSellingPrice, 0.0f);
                                            UnloadSelliingGoods();

                                            if (ikSelling.ItemType == 0 || ikSelling.ItemType == 1 || ikSelling.ItemType == 2 || ikSelling.ItemType == 3 || ikSelling.ItemType == 4)
                                            {
                                                GameDataBridge.AddMethodCall<ItemKey, int, bool>(-1, DomainHelper.DomainIds.Extra, ExtraDomainHelper.MethodIds.TreasuryRemove, ikSelling, playerSellingData.amount, false);
                                            }
                                            else
                                            {
                                                GameDataBridge.AddMethodCall<ItemKey, int, bool>(-1, DomainHelper.DomainIds.Extra, ExtraDomainHelper.MethodIds.TreasuryRemove, ikSelling, playerSellingData.amount, true);
                                            }


                                        }
                                        else
                                        {
                                            Console.WriteLine("物品TemplateId或数量不对");
                                        }
                                    }
                                    else
                                    {
                                        //if (ChatingRoomManager.itemViewForSelling == null || itemDisplayDataListForSelling.Count == 0)
                                        //{
                                        Console.WriteLine("物品已被下架或已被抢,回复被抢");
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "物品被抢", "");
                                        ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                                        //}
                                    }


                                }

                                if (buyerData.tradeType == "贩卖人口")
                                {
                                    if (ChatingRoomManager.yourSellingNPCMaskAvatarFrame == null)
                                    {
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "NPC已被卖出", "");
                                        ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                                    }

                                    if (ChatingRoomMainPanel.publishingType == ChatingRoomManager.PublishingTypeEnum.PublishHumans)
                                    {
                                        CustomPlayerDataClass playerSellingData = JsonMapper.ToObject<CustomPlayerDataClass>(ChatingRoomMainPanel.sellingString);
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "确认卖人", ChatingRoomMainPanel.sellingString);
                                        ChatingRoomManager.CustomAddResource(ResourceType.Money, ChatingRoomMainPanel.lastSellingPrice, 0.0f);
                                        UnloadSelliingGoods();
                                        Console.WriteLine("接收到了贩卖人口请求, 卖掉了NPC");
                                        ChatingRoomMainPanel.SellNPC();
                                    }
                                    else
                                    {
                                        Console.WriteLine("NPC已被卖出,回复被抢");
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "NPC被抢", "");
                                        ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                                    }
                                }

                                if (buyerData.tradeType == "求购资源")
                                {

                                    if (ChatingRoomMainPanel.publishingType == ChatingRoomManager.PublishingTypeEnum.PublishResource)
                                    {
                                        ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                                        ChatingRoomMainPanel.publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
                                        Console.WriteLine("资源已够得，下架求购信息");
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "确认资源交易", "");
                                        switch (buyerData.resourceType)
                                        {
                                            case "食物":
                                                {
                                                    ChatingRoomManager.CustomAddResource(ResourceType.Food, int.Parse(buyerData.resourceCount), 0.0f);
                                                }
                                                break;
                                            case "木材":
                                                {
                                                    ChatingRoomManager.CustomAddResource(ResourceType.Wood, int.Parse(buyerData.resourceCount), 0.0f);
                                                }
                                                break;
                                            case "金铁":
                                                {
                                                    ChatingRoomManager.CustomAddResource(ResourceType.Metal, int.Parse(buyerData.resourceCount), 0.0f);
                                                }
                                                break;
                                            case "玉石":
                                                {
                                                    ChatingRoomManager.CustomAddResource(ResourceType.Jade, int.Parse(buyerData.resourceCount), 0.0f);
                                                }
                                                break;
                                            case "草药":
                                                {
                                                    ChatingRoomManager.CustomAddResource(ResourceType.Herb, int.Parse(buyerData.resourceCount), 0.0f);
                                                }
                                                break;
                                            case "织物":
                                                {
                                                    ChatingRoomManager.CustomAddResource(ResourceType.Fabric, int.Parse(buyerData.resourceCount), 0.0f);
                                                }
                                                break;
                                        }

                                        ChatingRoomManager.CustomAddResource(ResourceType.Money, -int.Parse(buyerData.resourceCount) * 5, 0.2f);

                                    }
                                    else
                                    {
                                        Console.WriteLine("资源求购已被别人完成");
                                        ChatingRoomManager.SendChatMessage(messgeJsonObj.senderID, "求购过期", "");
                                        ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
                                    }

                                }
                            }
                        }
                        break;

                    case "确认交易":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            string commandContent = senderName + "向" + receiveName + "确认购买物品";
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                //Console.WriteLine("确认交易,卖方数据:" + ChatingRoomMainPanel.buyingString);
                                CustomPlayerDataClass buyingobj = JsonMapper.ToObject<CustomPlayerDataClass>(ChatingRoomMainPanel.buyingString);

                                int price = int.Parse(buyingobj.customPrice);
                                ChatingRoomManager.CustomAddResource(GameData.Domains.Character.ResourceType.Money, -price, 0.0f);
                                ChatingRoomManager.CustomAddItem(ikCreate, buyingobj.amount);
                                ChatingRoomManager.ClearItemKey(ChatingRoomManager.ikReceived);
                                Console.WriteLine("接收到了确认购买:" + messgeJsonObj.commandContent);
                            }
                        }
                        break;

                    case "确认资源交易":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            string commandContent = senderName + "向" + receiveName + "确认资源交易";
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                int reducedMoney = int.Parse(ChatingRoomMainPanel.otherResourceTradePrice.text);
                                ChatingRoomMainPanel.otherResourceTradePrice.text = "0";
                                Console.WriteLine("接收到了确认资源购买:" + messgeJsonObj.commandContent);
                                ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(false);



                                switch (ChatingRoomMainPanel.otherTadeResourceType)
                                {
                                    case "食物":
                                        {
                                            ChatingRoomManager.CustomAddResource(ResourceType.Food, -reducedMoney / 5, 0.0f);
                                        }
                                        break;
                                    case "木材":
                                        {
                                            ChatingRoomManager.CustomAddResource(ResourceType.Wood, -reducedMoney / 5, 0.0f);
                                        }
                                        break;
                                    case "金铁":
                                        {
                                            ChatingRoomManager.CustomAddResource(ResourceType.Metal, -reducedMoney / 5, 0.0f);
                                        }
                                        break;
                                    case "玉石":
                                        {
                                            ChatingRoomManager.CustomAddResource(ResourceType.Jade, -reducedMoney / 5, 0.0f);
                                        }
                                        break;
                                    case "草药":
                                        {
                                            ChatingRoomManager.CustomAddResource(ResourceType.Herb, -reducedMoney / 5, 0.0f);
                                        }
                                        break;
                                    case "织物":
                                        {
                                            ChatingRoomManager.CustomAddResource(ResourceType.Fabric, -reducedMoney / 5, 0.0f);
                                        }
                                        break;
                                }

                                ChatingRoomManager.CustomAddResource(ResourceType.Money, reducedMoney, 0.2f);





                            }

                        }
                        break;

                    case "确认卖人":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            string commandContent = senderName + "向" + receiveName + "确认卖人";
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {

                                CustomPlayerDataClass buyingobj = JsonMapper.ToObject<CustomPlayerDataClass>(ChatingRoomMainPanel.buyingString);

                                int price = int.Parse(buyingobj.customPrice);
                                ChatingRoomManager.CustomAddResource(GameData.Domains.Character.ResourceType.Money, -price, 0.0f);
                                ChatingRoomMainPanel.CreateTrueCharacter();
                                Console.WriteLine("接收到了确认卖人:" + messgeJsonObj.commandContent);
                            }
                        }
                        break;
                    case "物品被抢":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            Console.WriteLine(senderName + "告知" + receiveName + "物品已物品被抢");
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                if (ChatingRoomManager.itemViewForBuying != null)
                                {
                                    GameObject.Destroy(ChatingRoomManager.itemViewForBuying.gameObject);
                                }

                                ChatingRoomMainPanel.otherPriceText.text = "0";
                            }

                        }
                        break;

                    case "求购过期":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            Console.WriteLine(senderName + "告知" + receiveName + "资源公告已过期");
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(false);
                                ChatingRoomMainPanel.otherResourceTradePrice.text = "0";
                            }
                        }
                        break;


                    case "NPC被抢":
                        {
                            string senderName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.senderID));
                            string receiveName = SteamFriends.GetFriendPersonaName((CSteamID)ulong.Parse(messgeJsonObj.receiveID));
                            Console.WriteLine(senderName + "告知" + receiveName + "NPC被抢");
                            if (messgeJsonObj.receiveID == SteamUser.GetSteamID().m_SteamID.ToString())
                            {
                                if (ChatingRoomManager.otherSellingNPCMaskAvatarFrame != null)
                                {
                                    ChatingRoomMainPanel.ClearOtherSellingAvatar();
                                }

                                ChatingRoomMainPanel.otherNPCPriceText.text = "0";
                            }
                        }
                        break;

                    default:
                        {

                        }
                        break;
                }


            }
            catch (Exception e)
            {
                Console.WriteLine("命令解析失败:" + e.ToString());
            }

        }

        public static void UnloadSelliingGoods()
        {
            ChatingRoomMainPanel.publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
            ChatingRoomManager.SetLobbyMemberData("isPublishing", "false");
            ChatingRoomMainPanel.lastSellingPrice = 0;
            ChatingRoomMainPanel.sellingString = "";

            ChatingRoomMainPanel.ClearYourSellingAvatar();

            if (ChatingRoomManager.itemViewForSelling != null)
            {
                GameObject.Destroy(ChatingRoomManager.itemViewForSelling.gameObject);
            }



            if (ChatingRoomManager.chatingRoomMainPanel != null)
            {
                ChatingRoomMainPanel.sellingTips.text = "有玩家购买了你卖的物品";
                ChatingRoomMainPanel.sellNPCPriceInputField.text = "0";
                ChatingRoomMainPanel.sellPriceInputField.text = "0";

            }
        }

        public static void ClearBuyingGoodsInfo()
        {
            if (ChatingRoomMainPanel.otherPriceText != null)
            {
                ChatingRoomMainPanel.otherPriceText.text = "0";
            }
            // ChatingRoomMainPanel.buyingString = "";
            if (ChatingRoomManager.itemViewForBuying != null)
            {
                GameObject.Destroy(ChatingRoomManager.itemViewForBuying.gameObject);
            }

            ChatingRoomMainPanel.ClearOtherSellingAvatar();
        }




        public void GetCharacterDisplayData(CharacterDisplayData item, sbyte num)
        {


            if (item.Gender == 0)
            {
                taiWuGender = "女";
            }
            else if (item.Gender == 1)
            {
                taiWuGender = "男";
            }

            taiWuName = NameCenter.GetNameByDisplayData(item, true, true);

            taiwuID = item.CharacterId.ToString();

            Debug.Log("获取到太吾名:" + taiWuName + "性别:" + taiWuGender);
            taiwuCharacterDisplayData = item;
        }



        public static void GetCharacterDisplayDataAndBirthMonth(string charNameOrId, Action<CharacterDisplayData, sbyte> action)
        {
            int listener0 = -1;
            listener0 = GameData.GameDataBridge.GameDataBridge.RegisterListener(delegate (List<NotificationWrapper> notifications)
            {
                NotificationWrapper wrapper = notifications[0];
                CharacterDisplayData item = new CharacterDisplayData();
                sbyte num = 0;
                RawDataPool dataPool = wrapper.DataPool;
                int valueOffset = wrapper.Notification.ValueOffset;
                valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref item);
                valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref num);
                action(item, num);
                GameData.GameDataBridge.GameDataBridge.UnregisterListener(listener0);
            });
            GameData.GameDataBridge.GameDataBridge.AddMethodCall<string>(listener0, customDomainId, 53, charNameOrId);
        }


        public static void PopChatingRoomMainPanel()
        {
            if (chatingRoomMainPanel == null)
            {
                //GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(chatingRoomMainPanelPrefab, UnityEngine.Object.FindObjectOfType<NewUICanvas>().transform);


                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(chatingRoomMainPanelPrefab, mainUIRT);
                gameObject.transform.localPosition = new Vector3(150, 880, 0);
                gameObject.transform.localScale = new Vector3(1, 1, 1);
                gameObject.name = "ChatingRoomMainPanel";
                chatingRoomMainPanel = gameObject.AddComponent<ChatingRoomMainPanel>();
                Console.WriteLine("弹出聊天室主菜单");
                Traverse.Create(UIManager.Instance).Field("_blockHotKey").SetValue(true);
                chatingRoomMainPanel.GetComponent<RectTransform>().SetAsFirstSibling();
            }

        }

        public GameObject GetChild<T>(GameObject gameObject, string name, bool showError = true) where T : Component
        {
            foreach (T t in gameObject.GetComponentsInChildren<T>(true))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
            if (showError)
            {
                Debug.LogError("对象" + gameObject.name + "不存在子对象" + name);
            }
            return null;
        }

        public static GameObject GetGameObjectStatic<T>(GameObject gameObject, string name, bool showError = true) where T : Component
        {
            List<T> gos = new List<T>();

            if (gameObject == null)
            {
                gos = UnityEngine.Object.FindObjectsOfType<T>().ToList();
            }
            else
            {
                gos = gameObject.GetComponentsInChildren<T>(true).ToList();
            }

            foreach (T t in gos)
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
            if (showError)
            {
                Debug.LogError("对象" + gameObject.name + "不存在子对象" + name);
            }
            return null;
        }

        public static void ExitLobby()
        {
            ChatingRoomMainPanel.publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
            Console.WriteLine("退出Lobby:" + lobbyID);
            SteamMatchmaking.LeaveLobby(lobbyID);
            if (lobbyChatMsgCallback != null)
            {
                lobbyChatMsgCallback.Dispose();
                lobbyChatMsgCallback = null;
            }

        }

        private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
        {
            // 将Steam头像转换为Unity Texture2D对象
            Console.WriteLine("图片回调触发,callback.m_iImage:" + callback.m_iImage);
            Texture2D avatarTexture = GetSteamImageAsTexture(callback.m_iImage);
            chatingRoomMainPanel.otherAvatarImage.texture = avatarTexture;
        }

        private Texture2D GetSteamImageAsTexture(int imageID)
        {
            // 获取图像大小
            Texture2D texture = null;
            uint width = 0;
            uint height = 0;
            if (SteamUtils.GetImageSize(imageID, out width, out height))
            {
                byte[] imageBuffer = new byte[width * height * 4];
                SteamUtils.GetImageRGBA(imageID, imageBuffer, (int)(width * height * 4));

                // 创建纹理并返回
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(imageBuffer);
                texture.Apply();
            }
            return texture;

        }
        public IEnumerator RefreshMemberListEnumerator()
        {
            while (true)
            {
                SteamMatchmaking.SetLobbyMemberData(lobbyID, "taiwuZhenMing", taiWuName);
                members = GetLobbyMembers();

                //Console.WriteLine("正在刷新成员面板");
                activePlayerCount = 0;
                foreach (KeyValuePair<CSteamID, string> keyValuePair in members)
                {
                    string isChating = SteamMatchmaking.GetLobbyMemberData(lobbyID, keyValuePair.Key, "isChating");
                    if (isChating == "true")
                    {
                        activePlayerCount++;
                    }
                }


                if (chatingRoomEnterButton != null)
                {

                    string htmlString = "<color=#" + ColorUtility.ToHtmlStringRGB(Color.green) + ">" + activePlayerCount + "</color>" + "/" + "<color=#" + ColorUtility.ToHtmlStringRGB(Color.red) + ">" + members.Count + "</color>";
                    chatingRoomEnterButton.GetComponentInChildren<Text>().text = htmlString;
                }


                if (chatingRoomMainPanel != null && chatingRoomMainPanel.isActiveAndEnabled)
                {
                    SteamMatchmaking.SetLobbyMemberData(lobbyID, "isChating", "true");

                    RectTransform chatingPanelContent = GetChild<RectTransform>(chatingRoomMainPanel.gameObject, "ChatingPanel", true).GetComponentInChildren<VerticalLayoutGroup>().GetComponent<RectTransform>();
                    RectTransform memberPanelContent = GetChild<RectTransform>(chatingRoomMainPanel.gameObject, "MemberPanel", true).GetComponentInChildren<VerticalLayoutGroup>().GetComponent<RectTransform>();
                    foreach (Transform child in memberPanelContent)
                    {
                        UnityEngine.GameObject.Destroy(child.gameObject);
                    }

                    foreach (KeyValuePair<CSteamID, string> keyValuePair in members)
                    {

                        GameObject userbarGo = UnityEngine.Object.Instantiate<GameObject>(userbarlPrefab, memberPanelContent);
                        Image IsSelling = GetChild<Image>(userbarGo, "IsSelling", true).GetComponent<Image>();
                        userbarGo.name = keyValuePair.Value;
                        List<Text> texts = userbarGo.GetComponentsInChildren<Text>().ToList();



                        string taiwuZhenMing = SteamMatchmaking.GetLobbyMemberData(lobbyID, keyValuePair.Key, "taiwuZhenMing");
                        //Console.WriteLine("获取到太吾:" + taiwuZhenMing);
                        string result = SteamMatchmaking.GetLobbyMemberData(lobbyID, keyValuePair.Key, "isPublishing");
                        string isChating = SteamMatchmaking.GetLobbyMemberData(lobbyID, keyValuePair.Key, "isChating");
                        //Console.WriteLine("发现成员:" + keyValuePair.Value + ":" + keyValuePair.Key + " isChating"+ isChating);
                        if (result == "true")
                        {
                            IsSelling.gameObject.SetActive(true);
                        }
                        else
                        {
                            IsSelling.gameObject.SetActive(false);
                        }

                        foreach (Text t in texts)
                        {
                            if (t.name == "Name")
                            {
                                t.text = keyValuePair.Value;
                            }

                            if (t.name == "TaiWuName")
                            {
                                if (isChating == "true")
                                {

                                    t.color = Color.white;
                                }
                                else
                                {
                                    t.color = Color.gray;
                                }
                                t.text = taiwuZhenMing;
                            }

                        }


                        Button button = userbarGo.GetComponent<Button>();

                        button.onClick.AddListener(() => SelectSomeOne(keyValuePair));
                        chatingRoomMainPanel.GetComponent<RectTransform>().SetAsLastSibling();


                    }


                }
                else
                {
                    SteamMatchmaking.SetLobbyMemberData(lobbyID, "isChating", "false");
                }

                yield return new WaitForSeconds(10f);
            }

        }
        public void AddSteamFirend(CSteamID targetID)
        {
            string targetName = SteamFriends.GetFriendPersonaName(targetID);
            Console.WriteLine("弹出向" + targetName + "发送好友邀请对话框");
            //SteamFriends.ActivateGameOverlayToUser("steamid", targetID);
            SteamFriends.ActivateGameOverlayToUser("friendadd", targetID);
        }

        public void SelectSomeOne(KeyValuePair<CSteamID, string> keyValuePair)
        {

            Console.WriteLine("选中了:" + keyValuePair.Value + ":" + keyValuePair.Key);
            targetID = keyValuePair.Key;

            SendChatMessage(targetID.m_SteamID.ToString(), "请求摆摊数据", "");


            chatingRoomMainPanel.otherAvatarImage.texture = null;
            SteamFriends.GetLargeFriendAvatar(targetID);
            ChatingRoomMainPanel.ClearOtherSellingAvatar();
        }

        public Dictionary<CSteamID, string> GetLobbyMembers()
        {
            Dictionary<CSteamID, string> memberNames = new Dictionary<CSteamID, string>();
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
            for (int i = 0; i < numMembers; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
                if (memberId.IsValid())
                {
                    string memberName = SteamFriends.GetFriendPersonaName(memberId);
                    memberNames.Add(memberId, memberName);
                }
            }

            return memberNames;
        }


        public void SendNormalMessage()
        {
            if (chatingRoomMainPanel != null)
            {
                if (SteamUser.GetSteamID().m_SteamID.ToString() == "76561198091009835")
                {
                    if (chatingRoomMainPanel.currentHexColor == ColorUtility.ToHtmlStringRGB(Color.white))
                    {
                        chatingRoomMainPanel.currentHexColor = ColorUtility.ToHtmlStringRGB(Color.red);
                    }

                }
                if (SteamUser.GetSteamID().m_SteamID.ToString() == "76561198287470005")
                {
                    if (chatingRoomMainPanel.currentHexColor == ColorUtility.ToHtmlStringRGB(Color.white))
                    {
                        chatingRoomMainPanel.currentHexColor = ColorUtility.ToHtmlStringRGB(specialMemberColor);
                    }
                }


                string htmlString = "<color=#" + chatingRoomMainPanel.currentHexColor + ">" + inputText.text + "</color>";

                SendChatMessage("", "普通消息", htmlString);
                inputText.text = "";
            }
        }


        public void SendPrivateMessage()
        {
            if (chatingRoomMainPanel != null)
            {
                chatingRoomMainPanel.currentHexColor = ColorUtility.ToHtmlStringRGB(Color.red);
                CSteamID receiver = ChatingRoomManager.targetID;
                string htmlString = "<color=#" + chatingRoomMainPanel.currentHexColor + ">" + inputText.text + "</color>";

                SendP2PMessage(receiver, "普通消息", htmlString);
            }

        }

        public static void SendP2PMessage(CSteamID receiver, string commandID, string commandContent)
        {
            string senderID = SteamUser.GetSteamID().m_SteamID.ToString();

            sendMessage = new CustomMessage();
            sendMessage.senderID = senderID;
            sendMessage.receiveID = receiver.m_SteamID.ToString();
            sendMessage.commandID = commandID;
            sendMessage.commandContent = commandContent;

            string inputString = JsonMapper.ToJson(sendMessage);

            //byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(inputString);

            // allocate new bytes array and copy string characters as bytes
            byte[] inputBytes = new byte[inputString.Length * sizeof(char)];
            System.Buffer.BlockCopy(inputString.ToCharArray(), 0, inputBytes, 0, inputBytes.Length);

            SteamNetworking.SendP2PPacket(receiver, inputBytes, (uint)inputBytes.Length, EP2PSend.k_EP2PSendReliable);
        }

        public static void SendChatMessage(string target, string commandID, string commandContent)
        {
            string senderID = SteamUser.GetSteamID().m_SteamID.ToString();

            sendMessage = new CustomMessage();
            sendMessage.senderID = senderID;
            sendMessage.receiveID = target;
            sendMessage.commandID = commandID;
            sendMessage.commandContent = commandContent;

            string inputString = JsonMapper.ToJson(sendMessage);

            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(inputString);

            SteamMatchmaking.SendLobbyChatMsg(lobbyID, inputBytes, inputBytes.Length);
        }


        public static void DestoryChatingRoomPanel()
        {
            ChatingRoomManager.UnloadSelliingGoods();
            if (chatingRoomEnterButton != null)
            {
                UnityEngine.GameObject.Destroy(chatingRoomEnterButton.gameObject);
                chatingRoomEnterButton = null;
            }

            if (chatingRoomMainPanel != null)
            {
                chatingRoomMainPanel.Close();
            }
            ChatingRoomManager.ExitLobby();
        }


        void GetItemDisplayDataCallback(int offset, RawDataPool datapool)
        {

            ItemDisplayData data = null;
            Serializer.Deserialize(datapool, offset, ref data);
            ChatingRoomManager.itemViewForBuying = ItemView.Instantiate(ChatingRoomManager.itemViewPrefab, ChatingRoomMainPanel.otherPlayerItemCell.transform);
            data.Amount = receivedGoodsDataAmount;
            ChatingRoomManager.itemViewForBuying.transform.SetParent(ChatingRoomMainPanel.otherPlayerItemCell.transform);
            ChatingRoomManager.itemViewForBuying.transform.localPosition = Vector3.zero;
            ChatingRoomManager.itemViewForBuying.SetData(data, true);


        }





        public static bool CheckItemKey(ItemKey ik)
        {
            if (ik.TemplateId > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ClearItemKey(ItemKey ik)
        {
            ik.Id = -1;
            ik.ItemType = -1;
            ik.ModificationState = 0;
            ik.TemplateId = -1;
        }

        public static void CustomAddResource(sbyte type, int count, float delayTime)
        {
            Game.Instance.StartCoroutine(CustomAddResourceCoroutine(type, count, delayTime));
        }



        public static IEnumerator CustomAddResourceCoroutine(sbyte type, int count, float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            GameDataBridge.AddMethodCall<sbyte, int>(-1, DomainHelper.DomainIds.Taiwu, TaiwuDomainHelper.MethodIds.GmCmd_AddResource, type, count);
            GameDataBridge.AddMethodCall<sbyte, int>(-1, customDomainId, 54, type, count);
        }



        public static void CustomAddItem(ItemKey ik, int count)
        {
            GameDataBridge.AddMethodCall<ItemKey, int>(-1, customDomainId, 52, ik, count);
        }

        public static void SetLobbyMemberData(string key, string value)
        {
            SteamMatchmaking.SetLobbyMemberData(lobbyID, key, value);
        }

        public static void SetSelfCharacterView(CharacterDisplayData cDD)
        {
            if (chatingRoomMainPanel != null)
            {
                if (yourSellingNPCMaskAvatarFrame == null)
                {
                    yourSellingNPCMaskAvatarFrame = GameObject.Instantiate(maskAvatarFramePrefab.gameObject, ChatingRoomMainPanel.sellingNpcAvater.transform);
                    yourSellingNPCMaskAvatarFrame.transform.localPosition = Vector3.zero;
                    yourSellingNPCMaskAvatarFrame.transform.localScale = Vector3.one;
                }


                Avatar sellingNpcAvatar = yourSellingNPCMaskAvatarFrame.GetComponentInChildren<Avatar>();
                sellingNpcAvatar.Refresh(cDD);
                Button avatarButton = ChatingRoomMainPanel.sellingNpcAvater.GetComponent<Button>();
                avatarButton.onClick.RemoveAllListeners();
                avatarButton.onClick.AddListener(() => GMFunc.EnterCharacterMenu(cDD.CharacterId));
            }
        }


        public static void SetOtherCharacterView(CharacterDisplayData cDD)
        {
            if (chatingRoomMainPanel != null)
            {
                if (otherSellingNPCMaskAvatarFrame == null)
                {
                    otherSellingNPCMaskAvatarFrame = GameObject.Instantiate(maskAvatarFramePrefab.gameObject, ChatingRoomMainPanel.otherNpcAvater.transform);
                    otherSellingNPCMaskAvatarFrame.transform.localPosition = Vector3.zero;
                    otherSellingNPCMaskAvatarFrame.transform.localScale = Vector3.one;
                }


                Avatar sellingNpcAvatar = otherSellingNPCMaskAvatarFrame.GetComponentInChildren<Avatar>();
                sellingNpcAvatar.Refresh(cDD);
                Button avatarButton = ChatingRoomMainPanel.otherNpcAvater.GetComponent<Button>();
                avatarButton.onClick.RemoveAllListeners();
                avatarButton.onClick.AddListener(() => GMFunc.EnterCharacterMenu(cDD.CharacterId));
            }
        }
    }

}
