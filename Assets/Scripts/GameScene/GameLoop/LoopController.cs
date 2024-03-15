using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameScene.Component;
using GameScene.Component.SelectControl;
using JetBrains.Annotations;
using Services;
using Spine.Unity;
using UnityEngine;
using Utilities;

namespace GameScene.GameLoop
{
    public class LoopController : ClickDragController
    {
        [Header("Reference model")]
        [SerializeField] private LoopView view;
        [SerializeField] private LoopModel model;
        [SerializeField] private PadSelectController padSelectController;
        [SerializeField] private BoardController boardController;

        private void Start()
        {
            gameMode = GameMode.Loop;
            // LoadData();
            // CreateSelector();
            // CreateBoard();
            // CreateTarget();
            // CreatePlayer();
            // InitView();
            playButton.onClick.AddListener(OnClickPlay);
            padSelectController.CreateSelector(generateList, model.Resource);
            boardController.CreateBoard(new Vector2(8, 6), model.Resource.BoardCellModel);
            playerController = Instantiate(model.PlayerModel).GetComponent<PlayerController>();
            // Init player model
            currentPlayerPosition = basePlayerPosition;
            boardController.PlaceObjectToBoard(playerController.transform, basePlayerPosition);

            CreateTarget();
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                padSelectController.HandleMouseUp();
            }

            padSelectController.HandleMouseMoveSelected();
        }

        #region Game Flow

        private void HandleMouseUp()
        {
            if (isDelete) // in delete zone
            {
                SimplePool.Despawn(selectedObject!.gameObject);
                isDelete = false;
                selectedObject = null;
                return;
            }

            // If looper, only detect if holding a part which is not a loop
            var looper = CheckInsideLoop();
            if (looper)
            {
                looper.AddItem(selectedObject);
                view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
            }
            else
            {
                if (!storeSelected.Contains(selectedObject))
                {
                    storeSelected.Insert(CalculatedCurrentPosition(Input.mousePosition), selectedObject);
                }

                view.SetParentSelected(selectedObject!.transform);
            }

            FixHeightSelected();
            selectedObject = null;
        }

        private void HandleMouseMoveSelected()
        {
            Vector3 mousePos = Input.mousePosition;
            selectedObject!.RectTransform.position = mousePos;
            // handle if inside delete zone
            isDelete = IsPointInRT(mousePos, deleteZone);

            var extensional = CheckInsideLoop();
            if (extensional)
            {
                extensional.MakeItemSelectedInRightPlace();
                extensional.MatchHeightLooper(selectedObject.RectTransform.sizeDelta, true);
                view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
                return;
            }

            foreach (var selector in storeSelected)
            {
                if (selector.SelectType == SelectType.Loop)
                {
                    var extensional1 = (Extensional)selector;
                    extensional1.MakeItemSelectedInRightPlace();
                    extensional1.MatchHeightLooper(Vector2.zero);
                }
            }

            // check to make space
            HandleDisplayCalculate(mousePos);
        }

        private IEnumerator StartPlayerMove()
        {
            view.ActiveSavePanel();
            var actionList = ConvertToAction();
            foreach (var action in actionList)
            {
                var item = action;
                view.SetParentSelectedToMove(item.transform);
                item.ActiveEffect();
                yield return HandleAction(item);
                item.ActiveEffect(false);
                view.SetParentSelected(item.transform);
            }

            view.ActiveSavePanel(false);
            if (WinChecker())
            {
                ShowWinPopup(800);
                // win
            }
            else
            {
                ResetGame();
            }
        }

        private IEnumerator HandleAction(InteractionItem action)
        {
            var isMove = true;
            var targetMove = currentPlayerPosition;
            switch (action.SelectType)
            {
                case SelectType.Up:
                    targetMove += Vector2.up;
                    break;
                case SelectType.Down:
                    targetMove += Vector2.down;
                    break;
                case SelectType.Left:
                    targetMove += Vector2.left;
                    break;
                case SelectType.Right:
                    targetMove += Vector2.right;
                    break;
                case SelectType.Collect:
                    isMove = false;
                    break;
            }

            if (isMove)
            {
                currentPlayerPosition = targetMove;

                if (IsOutsideBoard(targetMove))
                {
                    // Reset game cuz it fail
                    playerController.PlayAnimationIdle();
                    yield return new WaitForSeconds(1f);
                    ResetGame();
                    yield break;
                }

                targetMove = view.GetPositionFromBoard(targetMove);
                yield return MovePlayer(targetMove, model.PlayerMoveTime);
            }
            else
            {
                var tracker = playerController.PlayAnimationEat();
                if (targetChecker.ContainsKey(currentPlayerPosition))
                {
                    targetChecker[currentPlayerPosition] = true;
                    targetReferences[currentPlayerPosition].gameObject.SetActive(false);
                }

                yield return new WaitForSpineAnimationComplete(tracker);
                playerController.PlayAnimationIdle();
            }
        }

        private void ResetGame()
        {
            // Clear all things selected
            foreach (var selector in storeSelected)
            {
                if (selector.SelectType == SelectType.Loop)
                {
                    var looper = (Extensional)selector;

                    foreach (var itemLooped in looper.StoreSelected)
                    {
                        SimplePool.Despawn(itemLooped.gameObject);
                    }

                    looper.StoreSelected.Clear();
                }

                SimplePool.Despawn(selector.gameObject);
            }

            storeSelected.Clear();

            // Clear win condition
            foreach (var position in targetReferences.Keys)
            {
                targetReferences[position].gameObject.SetActive(true);
                targetChecker[position] = false;
            }

            // Reset player position
            currentPlayerPosition = basePlayerPosition;
            playerController.RotatePlayer(
                targetPosition[0].x >= basePlayerPosition.x
                , 0.1f);
            playerController.PlayAnimationIdle();
            view.PlaceObjectToBoard(playerController.transform, basePlayerPosition);
        }

        private bool WinChecker()
        {
            foreach (var value in targetChecker.Values)
            {
                if (!value) // any not get
                {
                    return false;
                }
            }

            return true;
        }

        private void FixHeightSelected()
        {
            foreach (var selector in storeSelected)
            {
                if (selector.SelectType == SelectType.Loop)
                {
                    var xSelector = (Extensional)selector;
                    xSelector.MatchHeightLooper(Vector2.zero);
                }
            }

            view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
        }

        private List<InteractionItem> ConvertToAction()
        {
            var result = new List<InteractionItem>();
            foreach (var item in storeSelected)
            {
                if (item.SelectType == SelectType.Loop)
                {
                    var looper = (Extensional)item;
                    for (int i = 0; i < looper.LoopCount; i++)
                    {
                        foreach (var itemLooped in looper.StoreSelected)
                        {
                            result.Add(itemLooped);
                        }
                    }
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }

        #endregion

        #region Initialized

        private void CreateTarget()
        {
            foreach (var position in targetPosition)
            {
                target = Instantiate(model.TargetPrefab).GetComponent<Target>();
                target.Init(model.CandySprites[Random.Range(0, model.CandySprites.Count)]);
                boardController.PlaceObjectToBoard(target.GetComponent<Transform>(), position);
                targetChecker.Add(position, false);
                targetReferences.Add(position, target.transform);
            }
        }

        #endregion

        #region CALL BACK

        // Event clicked selector
        private void OnClickedSelector(InteractionItem selectedObj)
        {
            // Generate new selected
            if (selectedObj.SelectType == SelectType.Loop)
            {
                var objLoop = SimplePool.Spawn(model.Resource.LoopPrefab);
                Extensional selectedScript = objLoop.GetComponent<Extensional>();
                selectedScript.Init(OnClickedSelected);
                selectedScript.SelectType = selectedObj.SelectType;
                // Moving handler
                selectedObject = selectedScript;
                view.SetParentSelectedToMove(selectedObject.transform);
                StoreTempPosition();
            }
            else
            {
                var obj = SimplePool.Spawn(model.SelectedPrefab);
                Basic selectedScript = obj.GetComponent<Basic>();
                selectedScript.Init(OnClickedSelected);
                selectedScript.ChangeRender(model.GetSelected(selectedObj.SelectType));
                selectedScript.SelectType = selectedObj.SelectType;

                // Moving handler
                selectedObject = selectedScript;
                view.SetParentSelectedToMove(selectedObject.transform);
                StoreTempPosition();
            }
        }

        private void OnClickedSelected(InteractionItem selectedObj)
        {
            // Get object to move
            // not have?
            storeSelected.Remove(selectedObj);
            foreach (var selector in storeSelected)
            {
                if (selector.SelectType == SelectType.Loop)
                {
                    var looper = (Extensional)selector;
                    looper.RemoveItem(selectedObj);
                }
            }

            selectedObject = selectedObj;
            view.SetParentSelectedToMove(selectedObject!.transform);
            view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
            StoreTempPosition();
        }

        // Start Moving
        private void OnClickPlay()
        {
            StartCoroutine(StartPlayerMove());
        }

        #endregion

        #region Calulate func

        private int CalculatedCurrentPosition(Vector2 mousePos)
        {
            for (int i = 0; i < storedPosition.Count; i++)
            {
                if (i == 0 && storedPosition[i].y - OffSet < mousePos.y) // first item
                {
                    return 0;
                }

                if (i == storedPosition.Count - 1) // last item
                {
                    return storedPosition.Count;
                }

                if (storedPosition[i].y + OffSet > mousePos.y
                    && storedPosition[i + 1].y - OffSet < mousePos.y)
                {
                    return i + 1;
                }
            }

            return storedPosition.Count;
        }

        private void HandleDisplayCalculate(Vector2 mousePos)
        {
            if (IsPointInRT(mousePos, selectedZone))
            {
                view.MakeEmptySpace(
                    storeSelected.Select(o => o.RectTransform).ToList(),
                    CalculatedCurrentPosition(mousePos),
                    selectedObject.RectTransform.sizeDelta.y
                );
            }
            else
            {
                view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
            }
        }

        [CanBeNull]
        private Extensional CheckInsideLoop()
        {
            // loop cannot inside loop
            if (selectedObject.SelectType == SelectType.Loop)
            {
                return null;
            }

            var startPosition = (selectedObject.transform.position);
            startPosition.z = -5;
            Ray ray = new Ray(startPosition, Vector3.forward * 100);
            if (Physics.Raycast(ray, out var hit))
            {
                var result = hit.transform.GetComponent<Extensional>();
                return result;
            }

            return null;
        }

        private void StoreTempPosition()
        {
            storedPosition.Clear();
            foreach (var item in storeSelected)
            {
                storedPosition.Add(item.RectTransform.position);
            }
        }

        private bool IsOutsideBoard(Vector2 checkPosition)
        {
            if (boardMap.Contains(checkPosition))
            {
                return false;
            }

            if (targetPosition.Contains(checkPosition))
            {
                return false;
            }

            if (basePlayerPosition == checkPosition)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}