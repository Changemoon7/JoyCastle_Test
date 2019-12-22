/*竞技场逻辑*/

/*using SprotoType;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using Zenject;
using DG.Tweening;


namespace Carc
{
    public class PKPlayerListDialog : Dialog
    {
        [Inject]
        DialogManager _dialogmanager;

        [Inject]
        IGameService _gameService;

        [Inject]
        TournamentManager _tournamentManager;

        [Inject]
        GameTime _gameTime;

        [Inject]
        GameDataLoader _gameData;

        public Slider _stageRewardSlider;

        public GameObject _playerListObject;

        public LocalizedText _stageNumText;

        public LocalizedText _selectEnemyDesText;

        public LocalizedText _stageDownTimeText;

        public List<LocalizedText> _stageRewardNumTextList = new List<LocalizedText>();

        public Button _operationButton;

        public PlayerKillingFightInfoManager _fightInfoManager;

        private CompositeDisposable _disposables = new CompositeDisposable();

        private List<PlayerListItemInfo> _playerItemList = new List<PlayerListItemInfo>();

        private Tweener _countDownTimeTween;

        private int _endTime;


        protected override void Awake()
        {
            base.Awake();

            InitWithPlayerListPosition();

            //新一轮战斗开始
            MessageBroker.Default.Receive<TournamentRoundStartEvent>()
            .Subscribe(evt => BattleResponse())
            .AddTo(_disposables);

            //比赛结束
            MessageBroker.Default.Receive<TournamentCompleteEvent>()
            .Subscribe(evt=> BattleResponse())
            .AddTo(_disposables);

            //领取奖励
            MessageBroker.Default.Receive<TournamentRewardClaimEvent>()
            .Subscribe(evt => ShowRewards())
            .AddTo(_disposables);

            //战斗记录返回
            MessageBroker.Default.Receive<TournamentFightRecordEvent>()
            .Subscribe(evt => ShowFightRecord())
            .AddTo(_disposables);

            _operationButton.onClick.AddListener(OperationButton);
        }

        public override void Show()
        {
            base.Show();
            BattleResponse();
        }

        private void OperationButton()
        {
            if (_tournamentManager._battleCompleteResponse != null &&
                _tournamentManager._battleCompleteResponse.result == (int)Enums.RpcResponse.TournamentComplete)
            {
                _tournamentManager.TournamentClaim();
                _dialogmanager.ShowDialog<WaitingDialog>().Show();
            }
            if (_tournamentManager._matchRoundStartResponse != null && 
                _tournamentManager._matchRoundStartResponse.result == (int)Enums.RpcResponse.TournamentInProgress)
            {
                this.Close();
                return;
            }
        }

        public void BattleResponse()
        {
            //每一轮新数据下来，关闭玩家队伍详情界面
            PlayerSoldierListInfo tempPlayerSoldierListInfo = _dialogmanager.GetDialog<PlayerSoldierListInfo>();
            if (tempPlayerSoldierListInfo != null && (tempPlayerSoldierListInfo.IsShowing() || tempPlayerSoldierListInfo.IsShown()))
            {
                tempPlayerSoldierListInfo.Close();
            }
            if (_tournamentManager._battleCompleteResponse != null)
            {
                RefreshPlayerList();
                RefreshStage();
                return;
            }
            if (_tournamentManager._matchRoundStartResponse == null)
            {
                Debug.Log("比赛已经结束，重登陆_matchRoundStartResponse没有数据");
                return;
            }
            //比赛已经结束 
            if (_tournamentManager._matchRoundStartResponse.result == (int)Enums.RpcResponse.TournamentComplete)
            {
                RefreshPlayerList();
                RefreshStage();
                return;
            }
            //出现未知错误
            if (_tournamentManager._matchRoundStartResponse.result == (int)Enums.RpcResponse.Error)
            {
                Debug.Log("比赛没有进行也没有完成，或者request.round超出范围");
                return;
            }
            _operationButton.GetComponentInChildren<LocalizedText>().SetTextByKey(LocalizationKeys.tournament_gohome);
            RefreshPlayerList();
            RefreshStage();
            int tempBattleResult =  _tournamentManager.GetPlayerBattleResultState(GameInfo.Instance.UserId.Value);
            //本轮已经打开，显示下一轮倒计时描述
            if (_tournamentManager.GetTournamentComplate())
            {
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_searching_tips_2);
            }
            if (tempBattleResult != Constants.BATTLE_NO)
            {
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_battledes1);
            }
            else
            {
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_battledes2);
            }
        }


        /// <summary>
        /// 刷新阶段显示
        /// </summary>
        private void RefreshStage()
        {
            int tempTotalRound = (int)_tournamentManager.Config.totalRounds;

            if (_tournamentManager._battleCompleteResponse != null)
            {
                _stageDownTimeText.text = "00:00";
                _stageRewardSlider.value = 1.0f;
                int tempMyselfRank = _tournamentManager.GetMyselfRank();
                _stageNumText.SetTextByKey(LocalizationKeys.tournament_rankdes, tempMyselfRank);
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_searching_tips_2);
                _operationButton.GetComponentInChildren<LocalizedText>().SetTextByKey(LocalizationKeys.claim_reward);
            }
            else if (_tournamentManager._matchRoundStartResponse != null && 
                _tournamentManager._matchRoundStartResponse.result == (int)Enums.RpcResponse.TournamentInProgress)
            {
                int tempRound = (int)_tournamentManager._matchRoundStartResponse.round;
                //倒计时
                long tempEndTime = _tournamentManager._matchRoundStartResponse.endGameTime - _gameTime.Time;
                _endTime = (int)tempEndTime;
                if (_countDownTimeTween != null)
                {
                    _countDownTimeTween.Kill(true);
                    _countDownTimeTween = null;
                }
                _countDownTimeTween = DOTween.To(() => _endTime, value => {
                    _stageDownTimeText.text = value.ToString();
                }, 0, _endTime);
                _countDownTimeTween.SetEase(Ease.Linear);
                _stageRewardSlider.value = (tempRound - 1) * 1.0f / tempTotalRound;
                _stageNumText.SetTextByKey(LocalizationKeys.tournament_stageNum, tempRound, tempTotalRound);
            }
        }

        /// <summary>
        /// 数据返回，刷新玩家列表界面
        /// </summary>
        private void RefreshPlayerList()
        {
            int tempTournamentPlayersCount = _tournamentManager.TournamentPlayerCount();
            for (int i = 0; i < _playerItemList.Count; i++)
            {
                PlayerListItemInfo tempPlayerListItemInfo = _playerItemList[i];
                if (i < tempTournamentPlayersCount)
                {
                    tempPlayerListItemInfo.gameObject.SetActive(true);
                }
                else
                {
                    tempPlayerListItemInfo.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _playerItemList.Count; i++)
            {
                PlayerListItemInfo tempPlayerListItem = _playerItemList[i];
                if (tempPlayerListItem.gameObject.activeSelf)
                {
                    tempPlayerListItem.RefreshPlayerInfo();
                }
            }
        }

        /// <summary>
        /// 初始化玩家列表坐标
        /// </summary>
        private void InitWithPlayerListPosition()
        {
            _playerItemList.Clear();
            PlayerListItemInfo[] tempPlayerListItemInfoList =  _playerListObject.GetComponentsInChildren<PlayerListItemInfo>();
            _playerItemList.AddRange(tempPlayerListItemInfoList);

            for (int i = 0; i < tempPlayerListItemInfoList.Length; i++)
            {
                PlayerListItemInfo tempPlayerListItemInfo = _playerItemList[i];
                //List<TournamentPlayer>  index就是玩家排名
                tempPlayerListItemInfo.SetListRank(i + 1);
                EventTriggerListener.Get(tempPlayerListItemInfo._backGround.gameObject).onClick = ShowPlayerSoldierListInfo;
                tempPlayerListItemInfo.gameObject.SetActive(false);
            }
        }

        private void ShowPlayerSoldierListInfo(GameObject _gameObject)
        {
            PlayerListItemInfo tempPlayerListItemInfo = _gameObject.transform.parent.GetComponent<PlayerListItemInfo>();
            if (tempPlayerListItemInfo.GetRankIndex() == _tournamentManager.GetMyselfRank())
            {
                Debug.Log("自己不能查看。。。");
                return;
            }
            if (_tournamentManager.GetTournamentComplate())
            {
                return;
            }
            PlayerSoldierListInfo tempPlayerSoldierListInfo = _dialogmanager.ShowDialog<PlayerSoldierListInfo>();
            tempPlayerSoldierListInfo.InitWithSoldierListInfo(tempPlayerListItemInfo.GetRankIndex());
        }

        private void ShowRewards()
        {
            this.Close();
            _dialogmanager.ShowDialog<PlayerKillingRewardDialog>();
        }

        /// <summary>
        /// 显示聊天记录
        /// </summary>
        private void ShowFightRecord()
        {
            List<TournamentFightInfo> tempTournamentFightInfos = _tournamentManager._tournamentFightInfos;
            TournamentFightInfo tempLastFightInfo = tempTournamentFightInfos[tempTournamentFightInfos.Count - 1];
            string tempLastAttackName = _tournamentManager.GetPlayerNameByUserId(tempLastFightInfo.userId);
            string tempLastName = _tournamentManager.GetPlayerNameByUserId(tempLastFightInfo.opponentId);
            Debug.Log("最新战报 = " + tempLastAttackName + " " + tempLastName);

            RefreshPlayerList();
            _fightInfoManager.ShowLastFightInfo();
            int tempBattleResult = _tournamentManager.GetPlayerBattleResultState(GameInfo.Instance.UserId.Value);
            //本轮已经打开，显示下一轮倒计时描述
            if (_tournamentManager.GetTournamentComplate())
            {
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_searching_tips_2);
            }
            else if (tempBattleResult != Constants.BATTLE_NO)
            {
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_battledes1);
            }
            else
            {
                _selectEnemyDesText.SetTextByKey(LocalizationKeys.tournament_battledes2);
            }
        }


        protected override void OnDestroy()
        {
            _disposables.Dispose();
            base.OnDestroy();
        }
    }
}*/
