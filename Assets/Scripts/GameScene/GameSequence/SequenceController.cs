using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameScene.Component;
using JetBrains.Annotations;
using MainScene.Data;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utilities;

namespace GameScene.GameSequence
{
    public class SequenceController : GameController
    {
        [Header("Reference model")]
        [SerializeField] private SequenceView view;
        [SerializeField] private SequenceModel model;

        [Header("Reference object game")]
        [SerializeField] private RectTransform deleteZone;
        [SerializeField] private RectTransform selectedZone;
        [SerializeField] private Button playButton;

        [Header("Testing only")]
        [SerializeField] private List<SelectType> generateList;

   
        // FOR CONTROL SELECTOR
        private readonly List<Selector> storeSelector = new();
        private readonly List<Selector> storeSelected = new();
        private readonly List<Vector2> storedPosition = new();
        private bool isDelete;
        private float offSet = 0.2f;
        [CanBeNull] private Selector selectedObject;

        // PLAY GROUND
        private GameObject player;
        private Candy candy;

        // GAME DATA
        private LevelItemData levelData;
        private Vector2 playerPosition;
        private Vector2 targetPosition;
        private Vector2 startPosition;
        private Vector2 boardSize;
        private int stageIndex;
        private int levelIndex;
        private int coinWin;
        private int gemWin;
        private bool isPrevious;

        #region INITIALIZE

      
        private void Start()
        {
            var param = PopupHelpers.PassParamPopup();
            levelData = param.GetObject<LevelItemData>(ParamType.LevelData);
            stageIndex = param.GetObject<int>(ParamType.StageIndex);
            levelIndex = param.GetObject<int>(ParamType.LevelIndex);
            if (isPrevious)
            {
                coinWin = 0;
                gemWin = 0;
            }
            else
            {
                var reward1 = levelData.LevelReward.FirstOrDefault(o => o.RewardType == Enums.RewardType.Coin);
                var reward2 = levelData.LevelReward.FirstOrDefault(o => o.RewardType == Enums.RewardType.Coin);
                coinWin = reward1?.Value ?? 0;
                gemWin = reward2?.Value ?? 0;
            }

            boardSize = levelData.BoardSize;
            targetPosition = levelData.TargetPosition;
            startPosition = levelData.PlayerPosition;

            InitScene();
        }

        private void InitScene()
        {
            // Generate objects selector
            foreach (var o in generateList)
            {
                var obj = Instantiate(model.GetSelector(o));
                view.SetParentSelector(obj.transform);
                storeSelector.Add(obj.GetComponent<Selector>());
            }

            // Assign callback for selector
            foreach (var arrow in storeSelector)
            {
                arrow.Init(OnClickedSelector);
            }

            // Play button
            playButton.onClick.AddListener(OnClickPlay);

            // param

            // Init view

            var listBoard = new List<Transform>();

            for (int i = 0; i < boardSize.x * boardSize.y; i++)
            {
                listBoard.Add(Instantiate(model.CellModel).transform);
            }

            view.InitGroundBoard(listBoard, boardSize, model.BlockOffset);
            // Init Candy
            candy = Instantiate(model.CandyModel).GetComponent<Candy>();
            candy.Init(model.CandySprites[Random.Range(0, model.CandySprites.Count)]);
            view.InitTargetPosition(candy.GetComponent<Transform>(), targetPosition);

            // Init player
            player = Instantiate(model.PlayerModel);
            playerPosition = startPosition;
            view.InitPlayerPosition(player.GetComponent<Transform>(), startPosition);
        }

        #endregion

        private void Update()
        {
            if (selectedObject)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    HandleMouseUp();
                }
                else
                {
                    HandleMouseMoveSelected();
                }
            }
        }

        private void HandleMouseUp()
        {
            if (isDelete) // in delete zone
            {
                SimplePool.Despawn(selectedObject!.gameObject);
                selectedObject = null;
                isDelete = false;
            }
            else // Valid pos
            {
                if (!storeSelected.Contains(selectedObject))
                {
                    storeSelected.Insert(CalculatedCurrentPosition(Input.mousePosition), selectedObject);
                }

                view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
                selectedObject = null;
            }
        }

        private void HandleMouseMoveSelected()
        {
            Vector3 mousePos = Input.mousePosition;
            selectedObject!.RectTransform.position = mousePos;
            // handle if inside delete zone
            isDelete = IsPointInRT(mousePos, deleteZone);
            // check to make space
            HandleDisplayCalculate(mousePos);
        }

        private void ResetGame()
        {
            // Clear all things
            foreach (var selector in storeSelected)
            {
                SimplePool.Despawn(selector.gameObject);
            }

            playerPosition = startPosition;
            storeSelected.Clear();

            // Reset player position and candy
            view.InitPlayerPosition(player.GetComponent<Transform>(), startPosition);
            view.InitTargetPosition(candy.GetComponent<Transform>(), targetPosition);
        }

        #region Calulate func

        private bool CheckWin()
        {
            foreach (var item in storeSelected)
            {
                switch (item.SelectType)
                {
                    case SelectType.Up:
                        playerPosition += Vector2.up;
                        break;
                    case SelectType.Down:
                        playerPosition += Vector2.down;
                        break;
                    case SelectType.Left:
                        playerPosition += Vector2.left;
                        break;
                    case SelectType.Right:
                        playerPosition += Vector2.right;
                        break;
                    case SelectType.Collect:
                        if (playerPosition == targetPosition)
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private int CalculatedCurrentPosition(Vector2 mousePos)
        {
            for (int i = 0; i < storedPosition.Count; i++)
            {
                if (i == 0 && storedPosition[i].y - offSet < mousePos.y) // first item
                {
                    return 0;
                }

                if (i == storedPosition.Count - 1) // last item
                {
                    return storedPosition.Count;
                }

                if (storedPosition[i].y + offSet > mousePos.y
                    && storedPosition[i + 1].y - offSet < mousePos.y)
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
                view.MakeEmptySpace(storeSelected.Select(o => o.RectTransform).ToList(),
                    CalculatedCurrentPosition(mousePos));
            }
            else
            {
                view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
            }
        }

        private bool IsPointInRT(Vector2 point, RectTransform rt)
        {
            // Get the rectangular bounding box of your UI element
            var rect = rt.rect;
            var anchoredPosition = rt.position;
            // Get the left, right, top, and bottom boundaries of the rect
            float leftSide = anchoredPosition.x - rect.width / 2f;
            float rightSide = anchoredPosition.x + rect.width / 2f;
            float topSide = anchoredPosition.y + rect.height / 2f;
            float bottomSide = anchoredPosition.y - rect.height / 2f;

            // Check to see if the point is in the calculated bounds
            if (point.x >= leftSide &&
                point.x <= rightSide &&
                point.y >= bottomSide &&
                point.y <= topSide)
            {
                return true;
            }

            return false;
        }

        private void StoreTempPosition()
        {
            storedPosition.Clear();
            foreach (var item in storeSelected)
            {
                storedPosition.Add(item.RectTransform.position);
            }
        }

        #endregion

        #region CALL BACK

        // Event clicked selector
        private void OnClickedSelector(Selector selectedObj)
        {
            // Generate new selected
            var obj = SimplePool.Spawn(model.GetSelected(selectedObj.SelectType));
            view.SetParentSelected(obj.transform);
            // Generate init selected
            var arrow = obj.GetComponent<Selector>();
            arrow.Init(OnClickedSelected);
            // assign to control
            selectedObject = arrow;
            StoreTempPosition();
        }

        private void OnClickedSelected(Selector selectedObj)
        {
            storeSelected.Remove(selectedObj);
            selectedObject = selectedObj;
            view.ReSortItemsSelected(storeSelected.Select(o => o.RectTransform).ToList());
            StoreTempPosition();
        }

        // Start Moving
        private async void OnClickPlay()
        {
            playButton.interactable = false;
            var isWin = CheckWin();
            view.MovePlayer(
                storeSelected.Select(o => o.SelectType).ToList()
                , model.PlayerMoveTime, OnMoveFail);

            await Task.Delay((int)(model.PlayerMoveTime * storeSelected.Count * 1000));
            if (isWin)
            {
                candy.gameObject.SetActive(false);
                var starWin = 3;
                // Save data
                playerService.SaveHistoryStar(stageIndex, levelIndex, starWin);
                if (playerService.CurrentLevel[stageIndex] == levelIndex)
                {
                    playerService.CurrentLevel[stageIndex]++;
                    playerService.SaveData();
                }

                // Show popup
                ShowWinPopup(starWin, coinWin, gemWin);
            }
            else
            {
                ResetGame();
                playButton.interactable = true;
            }
        }

        private void OnMoveFail()
        {
        }

        /// <summary>
        /// Win popup
        /// </summary>
        private void OnClickClaim()
        {
            // Save data
            playerService.UserCoin += coinWin;
            playerService.UserDiamond += gemWin;
            playerService.SaveData();
            // Load level

            var param = PopupHelpers.PassParamPopup();
            param.SaveObject(ParamType.StageIndex, stageIndex);
            param.SaveObject("OpenPopup", true);
            SceneManager.LoadScene(Constants.MainMenu);
        }

        /// <summary>
        /// Win popups
        /// </summary>
        private void OnClickClaimAds()
        {
            // Load level
            var param = PopupHelpers.PassParamPopup();
            param.SaveObject(ParamType.StageIndex, stageIndex);
            SceneManager.LoadScene(Constants.MainMenu);
        }

        private void OnClickExit()
        {
            SceneManager.LoadScene(Constants.MainMenu);
        }

        #endregion

        private void ShowWinPopup(int numOfStar, int coinReward, int gemReward)
        {
            var param = PopupHelpers.PassParamPopup();
            param.SaveObject("Coin", coinReward);
            param.SaveObject("Gem", gemReward);
            param.SaveObject("NumberOfStars", numOfStar);
            param.SaveObject("Title", "Stage clear!");
            param.AddAction(ActionType.YesOption, OnClickClaim);
            param.AddAction(ActionType.AdsOption, OnClickClaimAds);
            param.AddAction(ActionType.QuitOption, OnClickExit);
            PopupHelpers.Show(Constants.WinPopup);
        }
    }
}