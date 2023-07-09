using System.Collections.Generic;
using System.Text.RegularExpressions;
using MoonCardTool;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class CardCreatorParserWindow : EditorWindow
{
    private const string Pattern = @"\.";
    private const string CardDataPath = "Assets/MoonCardToolGithub/Plugins/MoonCardTool/Resources/Data/CardData/";
    private const string PrefabsPath = "Assets/_Game/Prefabs/";
    private const string BasePrefabPath = "Assets/MoonCardToolGithub/Plugins/MoonCardTool/Resources/Prefabs/BaseCardButtonUI.prefab";
    private const string CardFrontsPath = "CardFronts/";
        
    private readonly Dictionary<string, CardTypeEnum> _cardTypeDict = new()
    {
        {"A", CardTypeEnum.Ace},
        {"2", CardTypeEnum.Two},
        {"3", CardTypeEnum.Three},
        {"4", CardTypeEnum.Four},
        {"5", CardTypeEnum.Five},
        {"6", CardTypeEnum.Six},
        {"7", CardTypeEnum.Seven},
        {"8", CardTypeEnum.Eight},
        {"9", CardTypeEnum.Nine},
        {"1", CardTypeEnum.Ten},
        {"J", CardTypeEnum.Joker},
        {"K", CardTypeEnum.King},
        {"Q", CardTypeEnum.Queen},
    };

    [MenuItem("Utility/Card/Card Parser")]
    public static void InitWindow()
    {
        var window = GetWindow<CardCreatorParserWindow>();
        window.maxSize = new Vector2(400, 750);
        window.titleContent = new GUIContent("Card Parser & Creator Window");
        window.Show();
    }

    private void OnGUI()
    {
        List<Object> allCards = GetAllNonMetaAssets(CardFrontsPath);
        List<Sprite> cardSprites = ExtractCardSprites(allCards);
        string prefabPath = BasePrefabPath;

        if (GUILayout.Button("Create Card Scriptables"))
        {
            CreateCardScriptables(cardSprites, prefabPath);
        }
    }

    /// <summary>
    /// Obtains all non-meta assets from the defined resource path.
    /// </summary>
    private List<Object> GetAllNonMetaAssets(string resourcePath)
    {
        var allResources = Resources.LoadAll(resourcePath);

        var nonMetaAssetsList = new List<Object>();
        foreach (var resource in allResources)
        {
            string assetPath = AssetDatabase.GetAssetPath(resource);
                
            if (!assetPath.EndsWith(".meta"))
                nonMetaAssetsList.Add(resource);
        }

        return nonMetaAssetsList;
    }
        
    /// <summary>
    /// Extracts all Sprites from the provided List of Objects, assuming they are of Sprite type.
    /// </summary>
    private List<Sprite> ExtractCardSprites(List<Object> allCards)
    {
        var cardSprites = new List<Sprite>();

        foreach (var allCard in allCards)
            if (allCard is Sprite cardSprite)
                cardSprites.Add(cardSprite);

        return cardSprites;
    }

    /// <summary>
    /// Creates CardScriptableObjects for each Sprite in the given list of Sprites. 
    /// </summary>
    private void CreateCardScriptables(List<Sprite> cardSprites, string prefabPath)
    {
        Object originalPrefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
        GameObject sourceObject = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;

        for (int i = 0; i < cardSprites.Count; i++)
        {
            string[] cardName = Regex.Split(cardSprites[i].ToString(), Pattern);
            var cardSo = CreateInstanceAndSetPictures(cardSprites[i], cardName);
            SaveNewCardPrefab(prefabPath, cardName, cardSo, sourceObject);
        }
            
        sourceObject.gameObject.SetActive(false);
    }
        
    /// <summary>
    /// Creates a new instance of CardScriptableObject and sets up its properties.
    /// </summary>
    private CardScriptableObject CreateInstanceAndSetPictures(Sprite cardSprite, string[] cardName)
    {
        var cardSo = CreateInstance<CardScriptableObject>();
        cardSo.cardImage = cardSprite;
        cardSo.cardBorderImage = cardSprite;

        if (_cardTypeDict.TryGetValue(cardName[0], out var cardType))
            cardSo.cardType = cardType;

        return cardSo;
    }

    /// <summary>
    /// Saves a new card prefab to the defined prefab path.
    /// </summary>
    private void SaveNewCardPrefab(string prefabPath, string[] cardName, CardScriptableObject cardSo, GameObject sourceObject)
    {
        string localPath = PrefabsPath + cardName[0] + "." + cardName[1] +".prefab";
           
        var prefabVariant = PrefabUtility.SaveAsPrefabAsset(sourceObject, localPath);
        var cardUiComponent = prefabVariant.GetComponent<CardButtonUI>();
            
        LinkUIAndScriptable(cardName, cardSo, cardUiComponent);
    }

    /// <summary>
    /// Links a provided scriptable object with a CardButtonUI component.
    /// </summary>
    private void LinkUIAndScriptable(string[] cardName, CardScriptableObject cardSo, CardButtonUI cardUiComponent)
    {
        cardUiComponent.CardScriptableObject = cardSo;
        cardSo.cardButtonUI = cardUiComponent;

        string assetPath = CardDataPath + cardName[0] + "." + cardName[1] + ".asset";
            
        AssetDatabase.CreateAsset(cardSo, assetPath);
        DatabaseSaveItem.SaveItemToDatabase(cardSo);
        DatabaseSaveItem.SaveItemToDatabase(cardUiComponent);
    }
}