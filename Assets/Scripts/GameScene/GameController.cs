using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using GameScene.Component;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace GameScene
{
    public class GameController : MonoBehaviour
    {
        // SERVICES
        [Header("All Mode")]
        [SerializeField] private bool isTesting;
        [SerializeField] protected GameMode gameMode;
        [SerializeField] protected GameObject loading;
        [SerializeField] protected PlayerController playerController;
        [SerializeField] protected BoardController boardController;

        private int levelIndex;
        private DateTime startTime;
        private ClientService clientService;
        private PlayerService playerService;

        [Header("Game data")]
        protected Vector2 basePlayerPosition = new Vector2();
        protected List<Vector2> targetPosition = new List<Vector2>();
        protected List<Vector2> boardMap = new List<Vector2>();
        protected Vector2 boardSize = new(8, 6);

        protected void Awake()
        {
            if (isTesting)
            {
                return;
            }

            clientService = GameServices.Instance.GetService<ClientService>();
            playerService = GameServices.Instance.GetService<PlayerService>();
            startTime = DateTime.Now;
        }

        protected async Task<bool> LoadData()
        {
            if (isTesting)
            {
                return true;
            }

            loading.SetActive(true);
            var param = PopupHelpers.PassParamPopup();
            levelIndex = param.GetObject<int>(ParamType.LevelIndex);
            var levelData = await clientService.GetLevelData((int)gameMode, levelIndex);
            loading.SetActive(false);
            startTime = DateTime.Now;
            if (levelData != null)
            {
                basePlayerPosition = ConvertIntToVector2(levelData.vStartPosition);
                targetPosition.Clear();
                boardMap.Clear();
                foreach (var detail in levelData.levelDetail)
                {
                    switch (detail.positionType)
                    {
                        case PositionType.Board:
                            boardMap.Add(ConvertIntToVector2(detail.vPosition));
                            break;
                        case PositionType.Target:
                            targetPosition.Add(ConvertIntToVector2(detail.vPosition));
                            break;
                        case PositionType.Rock:
                            break;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private Vector2 ConvertIntToVector2(int value)
        {
            int x = (value - 1) % 8 + 1;
            int y = (value - 1) / 8 + 1;

            return new Vector2(x, y);
        }

        protected async void ShowWinPopup()
        {
            playerService.SaveData((int)gameMode, levelIndex + 1);
            var result = await clientService.FinishLevel((int)gameMode, levelIndex, startTime);
            
            levelIndex++;
            var coinWin = 0;
            var gemWin = 0;
            if (result != null)
            {
                coinWin = result.UserCoin - result.OldCoin;
                gemWin = result.UserGem - result.OldGem;
                clientService.Coin = result.UserCoin;
                clientService.Gem = result.UserGem;
            }

            var parameter = PopupHelpers.PassParamPopup();
            parameter.AddAction(PopupKey.YesOption, OnLoadNextLevel);
            parameter.SaveObject(ParamType.CoinTxt, coinWin);
            parameter.SaveObject(ParamType.GemTxt, gemWin);
            PopupHelpers.Show(Constants.WinPopup);

            if (!clientService.IsLogin)
            {
                if (levelIndex >= Constants.FreeLevel)
                {
                    PopupHelpers.ShowError(
                        "Thanks for playing, You need to register and upgrade to a student account to continue learning without limitations.",
                        "Notification");
                    return;
                }
            }
        }

        private void OnLoadNextLevel()
        {
            if (!clientService.IsLogin)
            {
                if (levelIndex >= Constants.FreeLevel)
                {
                    SceneManager.LoadScene(Constants.MainMenu);
                    return;
                }
            }

            var param = PopupHelpers.PassParamPopup();

            param.SaveObject(ParamType.LevelIndex, levelIndex);
            switch (gameMode)
            {
                case GameMode.Basic:
                    SceneManager.LoadScene(Constants.BasicMode);
                    break;
                case GameMode.Sequence:
                    SceneManager.LoadScene(Constants.SequenceMode);
                    break;
                case GameMode.Loop:
                    SceneManager.LoadScene(Constants.LoopMode);
                    break;
                case GameMode.Function:
                    SceneManager.LoadScene(Constants.FuncMode);
                    break;
                case GameMode.Condition:
                    SceneManager.LoadScene(Constants.ConditionMode);
                    break;
            }
        }

        public void OnClickExit()
        {
            SceneManager.LoadScene(Constants.MainMenu);
        }

        protected IEnumerator MovePlayer(Vector2 targetMove, float moveTime)
        {
            if (targetMove.x < playerController.transform.position.x)
            {
                playerController.RotatePlayer(false, moveTime);
            }
            else if (targetMove.x > playerController.transform.position.x)
            {
                playerController.RotatePlayer(true, moveTime);
            }

            var movePromise = playerController.transform.DOMove(targetMove, moveTime);
            playerController.PlayAnimationMove();
            yield return movePromise.WaitForCompletion();
        }
    }
}