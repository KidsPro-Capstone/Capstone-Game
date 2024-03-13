﻿using System.Collections.Generic;
using UnityEngine;

namespace GameScene.Data
{
    [CreateAssetMenu(fileName = "ControlItem", menuName = "GamePlay/GameControlItemGroupData", order = -999)]
    public class GameControlItemGroupData : ScriptableObject
    {
        [SerializeField] private List<GameControlItemData> gameControlItemData;
        [SerializeField] private GameObject selectorModel;
        [SerializeField] private GameObject selectedModel;
        
        public List<GameControlItemData> GameControlItemData => gameControlItemData;
        public GameObject SelectorModel => selectorModel;
        public GameObject SelectedModel => selectedModel;

        public GameControlItemData GetByType(SelectType selectType)
        {
            foreach (var data in gameControlItemData)
            {
                if (data.ItemType == selectType)
                {
                    return data;
                }
            }

            return null;
        }
    }
}