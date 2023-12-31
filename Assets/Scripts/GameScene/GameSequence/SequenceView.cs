using System.Collections.Generic;
using UnityEngine;

namespace GameScene.GameSequence
{
    public class SequenceView : GameView
    {
        [Header("2D references")]
        [SerializeField] private Transform container;
        [SerializeField] private Transform startGroundPosition;
        [SerializeField] private Transform blockContainer;

        [Header("Canvas references")]
        [SerializeField] private Transform selectorContainer;
        [SerializeField] private Transform selectedContainer;
        [SerializeField] private Transform movingContainer;

        [Header("Cache")]
        private readonly List<Vector2> positions = new();
        private Vector2 boardSize;

        #region Initialize

        public void InitGroundBoard(List<Transform> groundItems, Vector2 board, float offSet)
        {
            boardSize = board;
            var sizeY = (int)board.y;
            var sizeX = (int)board.x;
            for (int i = 0; i < sizeY; i++) // vertical
            {
                for (int j = 0; j < sizeX; j++) // horizontal
                {
                    var positionNew = startGroundPosition.position;
                    positionNew.x += offSet * j;
                    positionNew.y += offSet * i;
                    groundItems[i * sizeX + j].position = positionNew;
                    positions.Add(positionNew);
                    groundItems[i * sizeX + j].SetParent(blockContainer);
                }
            }
        }

        #endregion

        /// <summary>
        /// Place any object to board position
        /// </summary>
        /// <param name="objectToSet"></param>
        /// <param name="playerPos"></param>
        public void PlaceObjectToBoard(Transform objectToSet, Vector2 playerPos)
        {
            objectToSet.SetParent(container);
            objectToSet.position = GetPositionFromBoard(playerPos);
        }

        #region Canvas

        public void SetParentSelector(Transform child)
        {
            child.SetParent(selectorContainer);
            child.localScale = Vector3.one;
        }

        public void SetParentSelected(Transform child)
        {
            child.SetParent(selectedContainer);
            child.localScale = Vector3.one;
        }

        public void SetParentSelectedToMove(Transform child)
        {
            child.SetParent(movingContainer);
            child.localScale = Vector3.one;
        }

        /// <summary>
        /// Add and object to selected list with index
        /// </summary>
        private void SetPositionSelected(RectTransform item, int index)
        {
            var yPosition = -item.sizeDelta.y * (index - 0.5f);
            item.anchoredPosition = new Vector3(0f, yPosition, 0f);
        }

        /// <summary>
        /// Sorted all selected object to its position
        /// </summary>
        /// <param name="items"></param>
        public void ReSortItemsSelected(List<RectTransform> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                SetPositionSelected(items[i], i + 1);
            }
        }

        public void MakeEmptySpace(List<RectTransform> items, int indexToMakeSpace)
        {
            var itemIndex = 1;
            for (int i = 0; i < items.Count; i++)
            {
                if (indexToMakeSpace == i)
                {
                    itemIndex++;
                }

                SetPositionSelected(items[i], itemIndex);
                itemIndex++;
            }
        }

        #endregion

        #region 2D

        public Vector2 GetPositionFromBoard(Vector2 position)
        {
            var index = (int)((position.y - 1) * boardSize.x + (position.x - 1));
       
            return positions[index];
        }

        #endregion
    }
}