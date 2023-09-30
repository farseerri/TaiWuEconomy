using Config;
using FrameWork;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Display;
using GameData.Domains.Extra;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Domains.Taiwu;
using GameData.GameDataBridge;
using GameData.Serializer;
using GameData.Utilities;
using HarmonyLib;
using LitJson;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaiWuEconomy;
using UILogic.DisplayDataStructure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChatingRoom
{
    public class ChatingRoomMainPanel : MonoBehaviour
    {

        public static ChatingRoomItemCell yourWTSItemItemCell;
        public static ChatingRoomItemCell otherPlayerItemCell;
        public static GameObject otherResourceTadeBlock;
        public static GameObject otherPlayerSellingNPCBlock;

        public static InputField sellPriceInputField;
        public static InputField sellNPCPriceInputField;
        public static ChatingRoomManager.PublishingTypeEnum publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;

        public ScrollRect chatingScrollRect;
        public RawImage otherAvatarImage;
        public GameObject setColorPanel;
        public string currentHexColor;
        public GameObject chatingPanel;
        public static Text otherPriceText;
        public static Text otherNPCPriceText;
        public Transform dropdownParent;    // Dropdown的父物体

        private GameObject dropdown;        // Dropdown
        private Dropdown dropdownComponent; // Dropdown组件
        private Match match;                // 正则表达式匹配结果
        private bool selectingName;         // 是否正在选择名字

        public Button lunDaoButton;
        public static GameObject messageBox;
        public static int lastSellingPrice = 0;

        public static Button sellGoodsTab;
        public static Button getResourceTab;
        public static Button humanTraffickingTab;
        public static Button getFoodButton;
        public static Button getWoodButton;
        public static Button getMetalButton;
        public static Button getJadeButton;
        public static Button getHerbButton;
        public static Button getFabricButton;
        public static Text resourceType;

        public static GameObject resourceTradeBlock;
        public static GameObject sellingNpcBlock;
        public static InputField resourceNeedCountInputField;
        public static Text sellingTips;
        public static string sellingString = "";
        public static string buyingString = "";



        public static Text otherResourceText;
        public static Text otherResourceTradePrice;
        public static Button otherResourceTradButton;
        public static Button resourceNeedPublishButton;
        public static string currentTadeType = "";
        public static string otherTadeResourceType = "";
        public static int resourceSellingRate = 5;
        public static Button buyButton;
        public static Button otherPlayerSellingNPCBuyButton;
        public static Color lastChatingColor;
        public static Button addFriendButton;

        public static GameObject sellingNpcAvater;
        public static GameObject otherNpcAvater;
        public static string npcData;
        public static CharacterDisplayData newNonIntelligentCharacterDisplayData;
        public static CharacterDisplayData yourSellingCharacterDisplayData;
        public static int kidnappedCharacterCount;
        public void Close()
        {
            ChatingRoomMainPanel.ClearOtherSellingAvatar();
            UnityEngine.Object.Destroy(base.gameObject);
            Traverse.Create(UIManager.Instance).Field("_blockHotKey").SetValue(false);

        }


        public void Awake()
        {
            chatingPanel = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "ChatingPanel", true);
            chatingScrollRect = ChatingRoomManager.Inst.GetChild<ScrollRect>(chatingPanel.gameObject, "Scroll View", true).GetComponent<ScrollRect>();
            dropdownParent = ChatingRoomManager.Inst.GetChild<RectTransform>(chatingPanel.gameObject, "DropdownParent", true).transform;
            messageBox = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "MessageBox", true);
        }




        public void Start()
        {

            Button quitBtn = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "QuitBtn", true).GetComponent<Button>();
            Button sendBtn = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "SendButton", true).GetComponent<Button>();
            Button sendP2PMessageButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "SendP2PMessageButton", true).GetComponent<Button>();
            Button setGoodsButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "SetGoodsButton", true).GetComponent<Button>();
            Button publishItemButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "PublishItemButton", true).GetComponent<Button>();
            buyButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "BuyButton", true).GetComponent<Button>();

            sellPriceInputField = ChatingRoomManager.Inst.GetChild<InputField>(this.gameObject, "SellPriceInputField", true).GetComponent<InputField>();
            sellNPCPriceInputField = ChatingRoomManager.Inst.GetChild<InputField>(this.gameObject, "SellNPCPriceInputField", true).GetComponent<InputField>();
            otherAvatarImage = ChatingRoomManager.Inst.GetChild<RawImage>(this.gameObject, "AvatarImage", true).GetComponent<RawImage>();

            ChatingRoomManager.inputText = ChatingRoomManager.Inst.GetChild<InputField>(this.gameObject, "InputText", true).GetComponent<InputField>();

            yourWTSItemItemCell = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "YourWTSItem", true).AddComponent<ChatingRoomItemCell>();
            sellingTips = ChatingRoomManager.Inst.GetChild<Text>(this.gameObject, "YourItemSellTips", true).GetComponent<Text>();

            otherPlayerItemCell = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "OtherPlayerWTSItem", true).AddComponent<ChatingRoomItemCell>();
            otherPriceText = ChatingRoomManager.Inst.GetChild<Text>(otherPlayerItemCell.gameObject, "Price", true).GetComponent<Text>();

            sellGoodsTab = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "SellGoodsTab", true).GetComponent<Button>();
            getResourceTab = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "GetResourceTab", true).GetComponent<Button>();
            humanTraffickingTab = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "HumanTraffickingTab", true).GetComponent<Button>();

            sellGoodsTab.onClick.AddListener(() => TradeTypeSelect("发布摆摊信息"));
            getResourceTab.onClick.AddListener(() => TradeTypeSelect("求购资源"));
            humanTraffickingTab.onClick.AddListener(() => TradeTypeSelect("贩卖人口"));

            resourceTradeBlock = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "ResourceTradeBlock", true);
            sellingNpcBlock = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "SellingNpcBlock", true);
            Button setNPCButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "SetNPCButton", true).GetComponent<Button>();
            Button publishNPCButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "PublishNPCButton", true).GetComponent<Button>();

            setNPCButton.onClick.AddListener(() => SetNPC());
            publishNPCButton.onClick.AddListener(() => PublishNPC());

            resourceType = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "ResourceTypeText", true).GetComponent<Text>();
            getFoodButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "GetFood", true).GetComponent<Button>();
            getWoodButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "GetWood", true).GetComponent<Button>();
            getMetalButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "GetMetal", true).GetComponent<Button>();
            getJadeButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "GetJade", true).GetComponent<Button>();
            getHerbButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "GetHerb", true).GetComponent<Button>();
            getFabricButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "GetFabric", true).GetComponent<Button>();

            resourceNeedPublishButton = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "ResourceNeedPublishButton", true).GetComponent<Button>();
            resourceNeedPublishButton.onClick.AddListener(() => PublishResourceMessage());
            resourceNeedCountInputField = ChatingRoomManager.Inst.GetChild<RectTransform>(resourceTradeBlock.gameObject, "ResourceNeedCountInputField", true).GetComponent<InputField>();

            getFoodButton.onClick.AddListener(() => OnRequestingResourceChanged("食物"));
            getWoodButton.onClick.AddListener(() => OnRequestingResourceChanged("木材"));
            getMetalButton.onClick.AddListener(() => OnRequestingResourceChanged("金铁"));
            getJadeButton.onClick.AddListener(() => OnRequestingResourceChanged("玉石"));
            getHerbButton.onClick.AddListener(() => OnRequestingResourceChanged("草药"));
            getFabricButton.onClick.AddListener(() => OnRequestingResourceChanged("织物"));

            sellingNpcAvater = ChatingRoomManager.Inst.GetChild<RectTransform>(sellingNpcBlock, "NPCAvater", true);


            otherResourceTadeBlock = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "OtherResourceTadeBlock", true);
            otherResourceText = ChatingRoomManager.Inst.GetChild<RectTransform>(otherResourceTadeBlock.gameObject, "OtherResourceText", true).GetComponent<Text>();
            otherResourceTradePrice = ChatingRoomManager.Inst.GetChild<RectTransform>(otherResourceTadeBlock.gameObject, "OtherResourceTradePrice", true).GetComponent<Text>();
            otherResourceTradButton = ChatingRoomManager.Inst.GetChild<RectTransform>(otherResourceTadeBlock.gameObject, "OtherResourceTradButton", true).GetComponent<Button>();
            otherResourceTradButton.onClick.AddListener(() => TradeResource());

            otherPlayerSellingNPCBlock = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "OtherPlayerSellingNPCBlock", true);
            otherNPCPriceText = ChatingRoomManager.Inst.GetChild<Text>(this.gameObject, "OtherPlayerSellingNPCPrice", true).GetComponent<Text>();
            otherPlayerSellingNPCBuyButton = ChatingRoomManager.Inst.GetChild<Button>(this.gameObject, "OtherPlayerSellingNPCBuyButton", true).GetComponent<Button>();
            otherNpcAvater = ChatingRoomManager.Inst.GetChild<RectTransform>(otherPlayerSellingNPCBlock, "NPCAvater", true);
            otherPlayerSellingNPCBuyButton.onClick.AddListener(() => BuyNpc());

            quitBtn.onClick.AddListener(() => Close());
            sendBtn.onClick.AddListener(() => ChatingRoomManager.Inst.SendNormalMessage());
            sendP2PMessageButton.onClick.AddListener(() => ChatingRoomManager.Inst.SendPrivateMessage());
            setGoodsButton.onClick.AddListener(() => SetGoods());
            publishItemButton.onClick.AddListener(() => PublishItem());
            buyButton.onClick.AddListener(() => BuyGoods(ChatingRoomManager.targetID));

            setColorPanel = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "SetColorButtonsPanel", true);

            foreach (Transform child in setColorPanel.transform)
            {
                Button bt = child.GetComponent<Button>();
                bt.onClick.AddListener(() => currentHexColor = ColorUtility.ToHtmlStringRGB(bt.image.color));
            }




            // 给输入框添加OnValueChanged事件监听
            //ChatingRoomManager.inputText.onValueChanged.AddListener(OnInputValueChanged);

            currentHexColor = ColorUtility.ToHtmlStringRGB(Color.white);

            yourWTSItemItemCell.gameObject.SetActive(false);
            resourceTradeBlock.SetActive(false);

            ChatingRoomMainPanel.otherPlayerItemCell.gameObject.SetActive(false);
            ChatingRoomMainPanel.otherResourceTadeBlock.SetActive(false);


            ChatingRoomManager.chatingContent = ChatingRoomManager.Inst.GetChild<RectTransform>(this.gameObject, "ChatingPanel", true).GetComponentInChildren<ScrollRect>().content;


            foreach (Transform child in ChatingRoomManager.chatingContent)
            {
                GameObject.Destroy(child.gameObject);
            }

            foreach (string str in ChatingRoomManager.lastMessageList)
            {
                Image taiwuChatingBar = Instantiate(ChatingRoomManager.taiwuChatingBarPrefab, ChatingRoomManager.chatingContent).GetComponent<Image>();
                Text taiwuChatingText = taiwuChatingBar.GetComponentInChildren<Text>();

                if (lastChatingColor == null)
                {
                    lastChatingColor = Color.black;
                }

                if (lastChatingColor == Color.white)
                {
                    taiwuChatingBar.color = Color.black;
                }
                else
                {
                    taiwuChatingBar.color = Color.white;
                }
                lastChatingColor = taiwuChatingBar.color;
                taiwuChatingText.text = SensitiveWordsSystem.Instance.GetLegalResult(str) + "\r\n";
            }

            addFriendButton = ChatingRoomManager.Inst.GetChild<Button>(ChatingRoomManager.chatingRoomMainPanel.gameObject, "AddFriend", true).GetComponent<Button>();
        }





        public void OnRequestingResourceChanged(string resourceTypeString)
        {
            resourceType.text = resourceTypeString;
            publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
        }

        private void TradeTypeSelect(string value)
        {
            publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
            currentTadeType = value;
            switch (value)
            {
                case "":
                    {
                        yourWTSItemItemCell.gameObject.SetActive(false);
                        resourceTradeBlock.SetActive(false);
                        sellingNpcBlock.SetActive(false);


                    }
                    break;
                case "发布摆摊信息":
                    {
                        yourWTSItemItemCell.gameObject.SetActive(true);
                        resourceTradeBlock.SetActive(false);
                        sellingNpcBlock.SetActive(false);

                    }
                    break;
                case "求购资源":
                    {
                        yourWTSItemItemCell.gameObject.SetActive(false);
                        resourceTradeBlock.SetActive(true);
                        sellingNpcBlock.SetActive(false);
                    }
                    break;
                case "贩卖人口":
                    {
                        yourWTSItemItemCell.gameObject.SetActive(false);
                        resourceTradeBlock.SetActive(false);
                        sellingNpcBlock.SetActive(true);
                    }
                    break;
            }
            ClearYourSellingAvatar();
        }

        public void Update()
        {

            uint p2PreceiveMessagesCount;

            // repeat while there's a P2P message available
            // will write its size to size variable
            while (SteamNetworking.IsP2PPacketAvailable(out p2PreceiveMessagesCount))
            {
                // allocate buffer and needed variables
                var buffer = new byte[p2PreceiveMessagesCount];
                uint bytesRead;
                CSteamID remoteId;

                // read the message into the buffer
                if (SteamNetworking.ReadP2PPacket(buffer, p2PreceiveMessagesCount, out bytesRead, out remoteId))
                {
                    // convert to string
                    char[] chars = new char[bytesRead / sizeof(char)];
                    Buffer.BlockCopy(buffer, 0, chars, 0, (int)p2PreceiveMessagesCount);

                    string message = new string(chars, 0, chars.Length);
                    UnityEngine.Debug.Log("Received a P2P message: " + message);
                    ChatingRoomManager.Inst.PraseReceivedChatingRoomMessage(message);
                }
            }











            if (Input.GetKeyUp(KeyCode.Return))
            {
                ChatingRoomManager.Inst.SendNormalMessage();
            }




            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.F4))
            {

                Close();
            }

            if (Input.GetKey(KeyCode.Escape))
            {
                Close();
            }

        }


        public void OnDestroy()
        {

            if (ChatingRoomManager.Inst.refreshMemberListEnumerator != null)
            {

                StopCoroutine(ChatingRoomManager.Inst.refreshMemberListEnumerator);
            }

            // 移除输入框的事件监听
            ChatingRoomManager.inputText.onValueChanged.RemoveListener(OnInputValueChanged);

            if (dropdownComponent != null)
            {
                dropdownComponent.onValueChanged.RemoveListener(OnNameSelected);
            }


        }





        void OnInputValueChanged(string message)
        {
            // 判断是否正在选择名字
            if (selectingName)
            {
                return;
            }
            // 匹配@开头的字符串
            Regex regex = new Regex("^@[\\w\\d]*$");
            match = regex.Match(message);
            if (match.Success)
            {
                // 获取筛选关键字
                string query = match.Groups[1].Value;
                // 根据关键字筛选名字列表
                List<string> names = new List<string>();
                foreach (KeyValuePair<CSteamID, string> keyValuePair in ChatingRoomManager.members)
                {
                    names.Add(keyValuePair.Value);
                }

                // 实例化Dropdown
                dropdown = Instantiate(ChatingRoomManager.dropdownPrefab, dropdownParent);
                // 获取Dropdown组件
                dropdownComponent = dropdown.GetComponent<Dropdown>();

                List<string> filteredNames = names.FindAll(n => n.StartsWith(query));
                // 更新Dropdown选项
                dropdownComponent.ClearOptions();
                dropdownComponent.AddOptions(filteredNames);

                // 打开Dropdown
                //dropdown.SetActive(true);
                // 设置Dropdown位置
                RectTransform inputTransform = ChatingRoomManager.inputText.GetComponent<RectTransform>();
                dropdown.transform.parent = dropdownParent;
                dropdown.transform.localPosition = Vector3.one;

                // 添加Dropdown的事件监听
                dropdownComponent.onValueChanged.AddListener(OnNameSelected);
                dropdown.transform.SetAsLastSibling();


            }
            else
            {

                foreach (Transform tr in dropdownParent)
                {
                    //dropdown.SetActive(false);
                    // 移除Dropdown的事件监听
                    //dropdownComponent.onValueChanged.RemoveListener(OnNameSelected);
                    tr.GetComponent<Dropdown>().onValueChanged.RemoveListener(OnNameSelected);
                    Destroy(tr.gameObject);
                    //dropdown = null;
                }


            }
        }








        void OnNameSelected(int index)
        {
            // 获取选择的名字
            string name = dropdownComponent.options[index].text;
            // 在输入框中插入名字
            string message = ChatingRoomManager.inputText.text;
            message = message.Substring(0, match.Index + 1) + name + " " + message.Substring(match.Index + match.Length);
            ChatingRoomManager.inputText.text = message;

            //dropdown.SetActive(false);
            //// 移除Dropdown的事件监听
            //dropdownComponent.onValueChanged.RemoveListener(OnNameSelected);
            //Destroy(dropdown.gameObject);
            //dropdown = null;
            foreach (Transform tr in dropdownParent)
            {
                tr.GetComponent<Dropdown>().onValueChanged.RemoveListener(OnNameSelected);
                Destroy(tr.gameObject);
            }
            // 设置选择名字标记
            selectingName = true;
            // 将光标移动到名字后面
            ChatingRoomManager.inputText.caretPosition = match.Index + name.Length + 2;
            // 延迟一帧再将选择名字标记设为false
            StartCoroutine(ResetSelectingName());
        }






        IEnumerator ResetSelectingName()
        {
            yield return null;
            selectingName = false;
        }




        public void SetGoods()
        {
            UIManager.Instance.ShowUI(UIElement.Warehouse);
            publishingType = ChatingRoomManager.PublishingTypeEnum.NoPublishing;
            //SteamMatchmaking.SetLobbyMemberData(ChatingRoomManager.lobbyID, "isPublishing", "false");
            sellingTips.text = "还未设定价格，未上架";
        }

        public void SetNPC()
        {
            int listener0 = -1;

            listener0 = GameData.GameDataBridge.GameDataBridge.RegisterListener(delegate (List<NotificationWrapper> notifications)
            {
                Console.WriteLine("测试6");
                NotificationWrapper wrapper = notifications[0];
                yourSellingCharacterDisplayData = null;
                npcData = "";
                kidnappedCharacterCount = 0;
                RawDataPool dataPool = wrapper.DataPool;
                Console.WriteLine("测试8");
                int valueOffset = wrapper.Notification.ValueOffset;
                valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref kidnappedCharacterCount);
                valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref yourSellingCharacterDisplayData);
                valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref npcData);

                Console.WriteLine("kidnappedCharacterCount:" + kidnappedCharacterCount);
                if (yourSellingCharacterDisplayData == null || kidnappedCharacterCount == 0)
                {
                    Console.WriteLine("测试9");
                    sellingTips.text = "关押栏为空，无法设定NPC";
                }
                else
                {
                    Console.WriteLine("测试10");
                    ChatingRoomManager.SetSelfCharacterView(yourSellingCharacterDisplayData);
                    Console.WriteLine("NPC数据:" + npcData);
                    sellingTips.text = "npc已经设定好，请点击发布";
                    Console.WriteLine("测试11");
                }
                GameData.GameDataBridge.GameDataBridge.UnregisterListener(listener0);
            });
            Console.WriteLine("测试7");
            GameData.GameDataBridge.GameDataBridge.AddMethodCall<string>(listener0, ChatingRoomManager.customDomainId, 56, ChatingRoomManager.taiwuCharacterDisplayData.CharacterId.ToString());
        }




        public void BuyGoods(CSteamID targetID)
        {
            buyButton.interactable = false;
            if (ChatingRoomManager.itemViewForBuying == null)
            {
                Console.WriteLine("对方没在卖东西");
            }
            else
            {
                GetGroupCharDisplayData();
            }
        }


        public void TradeResource()
        {
            GetGroupCharDisplayData();
        }




        public static void GetGroupCharDisplayData()
        {

            List<int> charIdList = new List<int>();
            charIdList.Add(int.Parse(ChatingRoomManager.taiwuID));
            ChatingRoomManager.uI_Bottom.AsynchMethodCall<List<int>>(DomainHelper.DomainIds.Character, CharacterDomainHelper.MethodIds.GetGroupCharDisplayDataList, charIdList, OnGetGroupCharDisplayData);

        }

        public static void OnGetGroupCharDisplayData(int offset, RawDataPool dataPool)
        {
            List<GroupCharDisplayData> dataList = null;
            Serializer.Deserialize(dataPool, offset, ref dataList);


            if (ChatingRoomMainPanel.otherPlayerSellingNPCBlock.activeInHierarchy)
            {
                int playerMoney = dataList[0].Resources.Get(GameData.Domains.Character.ResourceType.Money);
                int price = int.Parse(otherNPCPriceText.text);
                if (playerMoney > price)
                {
                    Console.WriteLine("发送了购买NPC请求");
                    ChatingRoomManager.SendChatMessage(ChatingRoomManager.targetID.m_SteamID.ToString(), "请求交易", buyingString);
                }
                else
                {
                    Console.WriteLine("钱不够");
                }
            }


            if (ChatingRoomMainPanel.otherPlayerItemCell.gameObject.activeInHierarchy)
            {
                int playerMoney = dataList[0].Resources.Get(GameData.Domains.Character.ResourceType.Money);
                int price = int.Parse(otherPriceText.text);
                if (playerMoney > price)
                {
                    Console.WriteLine("ikReceived.TemplateId:" + ChatingRoomManager.ikReceived.TemplateId);
                    ChatingRoomManager.SendChatMessage(ChatingRoomManager.targetID.m_SteamID.ToString(), "请求交易", buyingString);
                }
                else
                {
                    Console.WriteLine("钱不够");
                }
            }



            if (ChatingRoomMainPanel.otherResourceTadeBlock.activeInHierarchy)
            {
                int playerResource = GetResource(ChatingRoomMainPanel.otherTadeResourceType, dataList[0]);


                int tradeResource = int.Parse(otherResourceTradePrice.text) / ChatingRoomMainPanel.resourceSellingRate;
                if (playerResource >= tradeResource)
                {
                    Console.WriteLine("发送交易请求,buyingString:" + buyingString);
                    ChatingRoomManager.SendChatMessage(ChatingRoomManager.targetID.m_SteamID.ToString(), "请求交易", buyingString);
                }
                else
                {
                    Console.WriteLine("你的资源不够");
                }

            }
        }


        public unsafe void PublishItem()
        {
            if (ChatingRoomManager.itemViewForSelling == null)
            {
                ChatingRoomMainPanel.sellingTips.text = "请先在公库里放置想要售卖的物品";
                return;
            }

            if (ChatingRoomManager.itemDisplayDataListForSelling.Count == 1)
            {
                ChatingRoomManager.CustomPlayerDataClass customPlayerData = new ChatingRoomManager.CustomPlayerDataClass();
                ItemKey ikSend = ChatingRoomManager.itemViewForSelling.Data.Key;
                customPlayerData.tradeType = "发布摆摊信息";
                customPlayerData.ItemType = ikSend.ItemType;
                customPlayerData.ModificationState = ikSend.ModificationState;
                customPlayerData.TemplateId = ikSend.TemplateId;
                customPlayerData.Id = ikSend.Id;
                customPlayerData.amount = ChatingRoomManager.itemViewForSelling.Data.Amount;
                customPlayerData.fullItemItemInfo = ChatingRoomManager.sendItemItemInfo;
                int miniPrice = (int)(ChatingRoomManager.itemViewForSelling.Data.Price * 0.9f);
                int maxPrice = (int)(ChatingRoomManager.itemViewForSelling.Data.Price * 5f);

                lastSellingPrice = 0;
                int tempPrice = 0;
                try
                {
                    tempPrice = Mathf.FloorToInt(float.Parse(sellPriceInputField.text) / customPlayerData.amount);
                }
                catch
                {
                    tempPrice = maxPrice;
                }

                tempPrice = Mathf.Clamp(tempPrice, miniPrice, maxPrice);

                lastSellingPrice = tempPrice * customPlayerData.amount;
                customPlayerData.customPrice = lastSellingPrice.ToString();
                sellPriceInputField.text = lastSellingPrice.ToString();

                //string hexString = BitConverter.ToString(customPlayerData.goodBytes.ToArray()).Replace("-", " ");
                sellingString = JsonMapper.ToJson(customPlayerData);
                Console.WriteLine("发布了货品，其他玩家现在可以看到了:" + sellingString);
                ChatingRoomMainPanel.sellingTips.text = "发布了货品，其他玩家现在可以看到了";
                publishingType = ChatingRoomManager.PublishingTypeEnum.PublishGoods;
                ChatingRoomManager.SetLobbyMemberData("isPublishing", "true");


            }
            else
            {
                ChatingRoomMainPanel.sellingTips.text = "当公库只有一格物品时，才能摆摊";
            }

        }

        public unsafe void PublishNPC()
        {
            if (npcData == "" || ChatingRoomManager.yourSellingNPCMaskAvatarFrame == null || yourSellingCharacterDisplayData == null || kidnappedCharacterCount == 0)
            {
                ChatingRoomMainPanel.sellingTips.text = "请先在确保关押栏里有人,并上了货";
                return;
            }

            ChatingRoomManager.CustomPlayerDataClass customPlayerData = new ChatingRoomManager.CustomPlayerDataClass();
            customPlayerData.tradeType = "贩卖人口";
            int miniPrice = 200000;
            int maxPrice = 2000000;
            lastSellingPrice = 0;
            int tempPrice = 0;
            try
            {
                tempPrice = Mathf.FloorToInt(float.Parse(sellNPCPriceInputField.text));
            }
            catch
            {
                tempPrice = maxPrice;
            }
            lastSellingPrice = Mathf.Clamp(tempPrice, miniPrice, maxPrice);
            customPlayerData.customPrice = lastSellingPrice.ToString();
            sellNPCPriceInputField.text = lastSellingPrice.ToString();
            customPlayerData.npcData = npcData;
            sellingString = JsonMapper.ToJson(customPlayerData);
            Console.WriteLine("发布了人口贩卖信息，其他玩家现在可以看到了:" + sellingString);
            ChatingRoomMainPanel.sellingTips.text = "发布了人口贩卖信息，其他玩家现在可以看到了";
            publishingType = ChatingRoomManager.PublishingTypeEnum.PublishHumans;
            ChatingRoomManager.SetLobbyMemberData("isPublishing", "true");
        }

        public void BuyNpc()
        {

            otherPlayerSellingNPCBuyButton.interactable = false;
            if (ChatingRoomManager.otherSellingNPCMaskAvatarFrame == null)
            {
                Console.WriteLine("对方没在卖人口");
            }
            else
            {
                GetGroupCharDisplayData();
            }
        }

        public static void CreateTrueCharacter()
        {
            if (newNonIntelligentCharacterDisplayData != null)
            {
                int listener0 = -1;
                GameData.GameDataBridge.GameDataBridge.AddMethodCall<int>(listener0, ChatingRoomManager.customDomainId, 59, newNonIntelligentCharacterDisplayData.CharacterId);
            }
        }
        public static void SellNPC()
        {
            if (yourSellingCharacterDisplayData != null)
            {
                int listener0 = -1;
                GameData.GameDataBridge.GameDataBridge.AddMethodCall<int>(listener0, ChatingRoomManager.customDomainId, 60, yourSellingCharacterDisplayData.CharacterId);
                yourSellingCharacterDisplayData = null;
            }

        }
        public void PublishResourceMessage()
        {
            resourceNeedPublishButton.interactable = false;
            List<int> charIdList = new List<int>();
            charIdList.Add(int.Parse(ChatingRoomManager.taiwuID));
            ChatingRoomManager.uI_Bottom.AsynchMethodCall<List<int>>(DomainHelper.DomainIds.Character, CharacterDomainHelper.MethodIds.GetGroupCharDisplayDataList, charIdList, OnGetGroupCharDisplayDataForResource);
        }




        public static void OnGetGroupCharDisplayDataForResource(int offset, RawDataPool dataPool)
        {
            int resourceCount = int.Parse(resourceNeedCountInputField.text);
            int tradeNeedMoney = resourceCount * resourceSellingRate;

            List<GroupCharDisplayData> dataList = null;
            Serializer.Deserialize(dataPool, offset, ref dataList);


            int playerMoney = dataList[0].Resources.Get(GameData.Domains.Character.ResourceType.Money);

            if (playerMoney >= tradeNeedMoney)
            {

                ChatingRoomManager.CustomPlayerDataClass customPlayerData = new ChatingRoomManager.CustomPlayerDataClass();
                customPlayerData.tradeType = "求购资源";
                customPlayerData.resourceType = resourceType.text;
                customPlayerData.resourceCount = resourceCount.ToString();

                sellingString = JsonMapper.ToJson(customPlayerData);
                Console.WriteLine("发布了资源求购告示:" + sellingString);

                ChatingRoomMainPanel.sellingTips.text = "你以" + tradeNeedMoney.ToString() + "发布告示收购" + resourceType.text + "资源";
                ChatingRoomManager.SetLobbyMemberData("isPublishing", "true");
                publishingType = ChatingRoomManager.PublishingTypeEnum.PublishResource;
            }
            else
            {
                Console.WriteLine("你的钱不够");
            }
        }

        public static int GetResource(string resourceType, GroupCharDisplayData data)
        {
            int playerResource = 0;
            switch (resourceType)
            {
                case "食物":
                    {
                        playerResource = data.Resources.Get(GameData.Domains.Character.ResourceType.Food);
                    }
                    break;
                case "木材":
                    {
                        playerResource = data.Resources.Get(GameData.Domains.Character.ResourceType.Wood);
                    }
                    break;
                case "金铁":
                    {
                        playerResource = data.Resources.Get(GameData.Domains.Character.ResourceType.Metal);
                    }
                    break;
                case "玉石":
                    {
                        playerResource = data.Resources.Get(GameData.Domains.Character.ResourceType.Jade);
                    }
                    break;
                case "草药":
                    {
                        playerResource = data.Resources.Get(GameData.Domains.Character.ResourceType.Herb);
                    }
                    break;
                case "织物":
                    {
                        playerResource = data.Resources.Get(GameData.Domains.Character.ResourceType.Fabric);
                    }
                    break;

            }

            return playerResource;
        }

        public static void CreateNewNonIntelligentCharacterByB64(string npcData)
        {
            int listener0 = -1;
            listener0 = GameData.GameDataBridge.GameDataBridge.RegisterListener(delegate (List<NotificationWrapper> notifications)
            {

                NotificationWrapper wrapper = notifications[0];
                newNonIntelligentCharacterDisplayData = null;
                RawDataPool dataPool = wrapper.DataPool;
                int valueOffset = wrapper.Notification.ValueOffset;
                valueOffset += GameData.Serializer.Serializer.Deserialize(dataPool, valueOffset, ref newNonIntelligentCharacterDisplayData);
                if (newNonIntelligentCharacterDisplayData != null)
                {

                    ChatingRoomManager.SetOtherCharacterView(newNonIntelligentCharacterDisplayData);
                    Console.WriteLine("新建临时NPC:" + npcData);
                }
                GameData.GameDataBridge.GameDataBridge.UnregisterListener(listener0);
            });

            GameData.GameDataBridge.GameDataBridge.AddMethodCall<string>(listener0, ChatingRoomManager.customDomainId, 57, npcData);

        }

        public static void ClearYourSellingAvatar()
        {
            if (ChatingRoomManager.yourSellingNPCMaskAvatarFrame != null)
            {
                GameObject.Destroy(ChatingRoomManager.yourSellingNPCMaskAvatarFrame.gameObject);
            }

        }

        public static void RemoveNewNonIntelligentCharacter()
        {
            if (newNonIntelligentCharacterDisplayData != null)
            {
                int listener0 = -1;
                GameData.GameDataBridge.GameDataBridge.AddMethodCall<int>(listener0, ChatingRoomManager.customDomainId, 58, newNonIntelligentCharacterDisplayData.CharacterId);
                newNonIntelligentCharacterDisplayData = null;
            }

        }
        public static void ClearOtherSellingAvatar()
        {
            if (ChatingRoomManager.otherSellingNPCMaskAvatarFrame != null)
            {
                GameObject.Destroy(ChatingRoomManager.otherSellingNPCMaskAvatarFrame.gameObject);
            }

            RemoveNewNonIntelligentCharacter();
        }


    }
}
