/* 基于Zenject(依赖注入)的UI管理 */

/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;
using UnityEngine.SceneManagement;
using UniRx;

namespace Carc
{
    public class DialogManager : MonoBehaviour, IGameReset
    {
        [SerializeField]
        private Canvas canvas_Fixed;

        [SerializeField]
        private Canvas canvas_Normal;

        [SerializeField]
        private Canvas canvas_PopUp;

        [SerializeField]
        private Canvas canvas_Message;

        [SerializeField]
        private Canvas canvas_System;

        private Dictionary<string, Dialog> dialog_normalDic = new Dictionary<string, Dialog>();

        private Dictionary<string, Dialog> dialog_fixedDic = new Dictionary<string, Dialog>();

        private Dictionary<string, Dialog> dialog_popupDic = new Dictionary<string, Dialog>();

        private Dictionary<string, Dialog> dialog_messageDic = new Dictionary<string, Dialog>();

        private Dictionary<string, Dialog> dialog_systemDic = new Dictionary<string, Dialog>();


        //[Inject]
        //private BackKeyManager backKeyManager;
        [Inject]
        private DiContainer container_project;


        private string dialogDilePathInfoJson = "DialogIndData/DialogFilePathInfo";

        private List<Dictionary<string, Dialog>> dialogDicList = new List<Dictionary<string, Dialog>>();

        private Camera _uiCamera;

        CameraManager _cameraManager;

        private CompositeDisposable _disposables = new CompositeDisposable();

        Dialog _lastShowDialog;


        public void Reset()
        {
            // 游戏重新加载时要清除所有对话框缓存
            for (int i = 0; i < dialogDicList.Count; i++)
            {
                var dict = dialogDicList[i];
                foreach (var e in dict)
                {
                    if (e.Value != null)
                        Destroy(e.Value.gameObject);
                }
                dict.Clear();
            }
        }

        private void Awake()
        {
            dialogDicList.Add(dialog_fixedDic);
            dialogDicList.Add(dialog_normalDic);
            dialogDicList.Add(dialog_popupDic);
            dialogDicList.Add(dialog_messageDic);
            dialogDicList.Add(dialog_systemDic);
        }

        void Start()
        {
            //this.backKeyManager.AddBackHandler(OnBackPressed, 0);
            ReadDialogDataInfo();
            MessageBroker.Default.Receive<Dialog.ShowCompleteEvent>()
                .Subscribe(evt => _lastShowDialog = evt.dialog)
                .AddTo(_disposables);
        }

        void OnDestroy()
        {
            //this.backKeyManager.RemoveBackHandler(OnBackPressed);
            _disposables.Dispose();
        }

        bool HideDialogsOnCanvas(Canvas canvas)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; --i)
            {
                var transform = canvas.transform.GetChild(i);
                var dlg = transform.gameObject.GetComponent<Dialog>();
                if (dlg != null)
                {
                    if (dlg.IsHidden())
                        continue;

                    if (dlg.IsShown() && dlg.DismissOnBackKey)
                        dlg.Close();

                    // else dialog is showing or hiding

                    return true;
                }
            }
            return false;
        }

        bool OnBackPressed()
        {
            if (HideDialogsOnCanvas(canvas_PopUp) ||
                HideDialogsOnCanvas(canvas_Normal))
            {
                return true;
            }

            return false;
        }

        public void HideAllDialog()
        {
            Dictionary<string, Dialog> tempDic;
            for (int i = 0; i < dialogDicList.Count; i++)
            {
                tempDic = dialogDicList[i];
                foreach (Dialog dialog in tempDic.Values)
                {
                    if (dialog != null)
                    {
                        dialog.Hide();
                    }
                }
            }
        }

        #region 重写下DialogManager

        public T GetDialog<T>() where T : Dialog
        {
            Type t = typeof(T);
            return GetDialog(t) as T;
        }

        //通过窗口名称，显示Dialog
        public T ShowDialog<T>() where T : Dialog
        {
            T tempDialog = GetDialog<T>();
            ShowDialog(tempDialog);
            return tempDialog;
        }

        public Dialog ShowDialog(string _dialogName)
        {
            Type tempType = Type.GetType("Carc." + _dialogName);
            Dialog tempDialog = GetDialog(tempType);
            ShowDialog(tempDialog);
            return tempDialog;
        }

        public void ShowDialog(Dialog dialog)
        {
            if (dialog != null)
            {
                CloseOtherByOpenDialog(dialog);
                dialog.Show();
            }
        }

        public T CloseDialog<T>() where T : Dialog
        {
            T tempDialog = HasDialogByName<T>();
            if (tempDialog != null)
            {
                 tempDialog.Close();
            }
            return tempDialog;
        }


        private T HasDialogByName<T>() where T:Dialog
        {
            string tempDialongName = typeof(T).Name;
            Dialog tempDialog = null;
            Dictionary<string, Dialog> tempDialogDic = null;
            if (dialog_fixedDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_fixedDic;
            }
            else if (dialog_normalDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_normalDic;
            }
            else if (dialog_popupDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_popupDic;
            }
            else if (dialog_messageDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_messageDic;
            }
            else if (dialog_systemDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_systemDic;
            }
            //窗口被销毁，移除
            if (tempDialogDic != null && tempDialog == null)
            {
                tempDialogDic.Remove(tempDialongName);
            }
            return (T)tempDialog;
        }

        private Dialog GetDialog(Type _type)
        {
            SetCamera();
            string tempDialogName = _type.Name;
            Dialog tempDialog = HasDialogByName(_type);

            //已加载到内存，显示
            if (tempDialog != null)
            {
                return tempDialog;
            }

            //获取预制体失败
            string tempDialogFilePath = "";
            if (!dialogInfoDataDic.TryGetValue(tempDialogName, out tempDialogFilePath))
            {
                return null;
            }
            Dialog tempNewDialog = null;
            if (container_project.HasBinding(_type))
            {
                tempNewDialog = (Dialog)container_project.Resolve(_type);
            }
            else
            {
                var sceneContainer = GetSceneContainer();
                if (sceneContainer == null)
                {
                    Debug.LogErrorFormat("Current scene {0} has no DiContainer found. Dialog type {1}.",
                        SceneManager.GetActiveScene().name, _type.Name);
                }
                else
                {
                    if (!sceneContainer.HasBinding(_type))
                    {
                        Debug.LogErrorFormat("Current scene {0} has no binding for dialog type {1}.");
                    }
                    else
                    {
                        tempNewDialog = (Dialog)sceneContainer.Resolve(_type);
                    }
                }
            }

            if (tempNewDialog == null)
            {
                Debug.LogError("创建实例失败。。" + _type.Name);
                return null;
            }

            tempNewDialog = SetDialogParent(tempNewDialog, tempDialogName);

            return tempNewDialog;
        }

        private T SetDialogParent<T>(T _dialog,string _dialogName) where T:Dialog
        {
            T tempNewDialog = _dialog;
            Canvas tempCanvas = null;
            Dictionary<string, Dialog> tempDialogDic = null;

            switch (tempNewDialog._dialogType)
            {
                case Dialog.DialogType.Fixed:
                    {
                        tempCanvas = canvas_Fixed;
                        tempDialogDic = dialog_fixedDic;
                        break;
                    }
                case Dialog.DialogType.Normal:
                    {
                        tempCanvas = canvas_Normal;
                        tempDialogDic = dialog_normalDic;
                        break;
                    }
                case Dialog.DialogType.PopUp:
                    {
                        tempCanvas = canvas_PopUp;
                        tempDialogDic = dialog_popupDic;
                        break;
                    }
                case Dialog.DialogType.Message:
                    {
                        tempCanvas = canvas_Message;
                        tempDialogDic = dialog_messageDic;
                        break;
                    }
                case Dialog.DialogType.System:
                    {
                        tempCanvas = canvas_System;
                        tempDialogDic = dialog_systemDic;
                        break;
                    }
            }
            if (tempDialogDic == null || tempCanvas == null)
            {
                Debug.LogError("窗口显示失败 dialogname = " + tempNewDialog.GetType().Name);
                return null;
            }

            if (!tempDialogDic.ContainsKey(_dialogName))
            {
                tempDialogDic.Add(_dialogName, tempNewDialog);
                tempNewDialog.gameObject.transform.SetParent(tempCanvas.transform, false);
            }
            return tempNewDialog;
        }

        private Dialog HasDialogByName(Type _type)
        {
            string tempDialongName = _type.Name;
            Dialog tempDialog = null;
            Dictionary<string, Dialog> tempDialogDic = null;
            if (dialog_fixedDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_fixedDic;
            }
            else if (dialog_normalDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_normalDic;
            }
            else if (dialog_popupDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_popupDic;
            }
            else if (dialog_messageDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_messageDic;
            }
            else if (dialog_systemDic.TryGetValue(tempDialongName, out tempDialog))
            {
                tempDialogDic = dialog_systemDic;
            }
            //窗口被销毁，移除
            if (tempDialogDic != null && tempDialog == null)
            {
                tempDialogDic.Remove(tempDialongName);
            }
            return (Dialog)tempDialog;
        }


        Dictionary<string, string> dialogInfoDataDic = new Dictionary<string, string>();
        private void ReadDialogDataInfo()
        {
            if (dialogInfoDataDic.Count != 0)
            {
                return;
            }
            TextAsset defaultLangText = Resources.Load<TextAsset>(dialogDilePathInfoJson);
            object obj = Newtonsoft.Json.JsonConvert.DeserializeObject(defaultLangText.text);
            Newtonsoft.Json.Linq.JObject jobj = obj as Newtonsoft.Json.Linq.JObject;

            if (jobj == null)
            {
                Debug.LogError("read Json DialogData.json fail");
                return;
            }

            foreach (var e in jobj)
            {
                dialogInfoDataDic.Add(e.Key,e.Value.ToString());
            }
        }

        //读取当前场景DiContainer
        private DiContainer GetSceneContainer()
        {
            DiContainer tempDicontainer = ProjectContext.Instance.Container.Resolve<SceneContextRegistry>()
            .TryGetContainerForScene(SceneManager.GetActiveScene());
            return tempDicontainer;
        }

        private void CloseOtherByOpenDialog<T>(T _dialog) where T : Dialog
        {
            Dictionary<string, Dialog> tempDic;
            T tempDialog = _dialog;
            //打开loading界面，不关闭其他界面
            if (tempDialog.GetType() == typeof(WaitingDialog))
            {
                _dialog.gameObject.transform.SetAsLastSibling();
                return;
            }
            for (int i = 0; i < dialogDicList.Count; i++)
            {
                tempDic = dialogDicList[i];
                if (tempDic.ContainsValue(tempDialog))//同一层次的关闭，其他先不管
                {
                    foreach (Dialog dialog in tempDic.Values)
                    {
                        if (dialog != tempDialog && dialog._dialogType != Dialog.DialogType.System)
                        {
                            dialog.Close();
                        }
                    }
                    break;
                }
            }
            //有其他界面打开，关闭等待界面
            WaitingDialog tempWaitingDialog = GetDialog<WaitingDialog>();
            if (tempWaitingDialog != null && (tempWaitingDialog.IsShown() || tempWaitingDialog.IsShowing()))
            {
                tempWaitingDialog.Close();
            }
        }

        /// <summary>
        /// 删除上一个场景的对话框
        /// </summary>
        public void DeleteAllDialog()
        {
            Dictionary<string, Dialog> tempDic;
            for (int i = 0; i < dialogDicList.Count; i++)
            {
                tempDic = dialogDicList[i];
                string[] tempKeyAry = tempDic.Keys.ToArray();
                for (int k = 0; k < tempKeyAry.Length; k++)
                {
                    Dialog tempDialog = tempDic[tempKeyAry[k]];
                    if (tempDialog && tempDialog._dialogType != Dialog.DialogType.System)
                    {
                        tempDic.Remove(tempKeyAry[k]);

                        if (tempDialog.gameObject)
                            Destroy(tempDialog.gameObject);
                    }
                }
            }
        }

        private void SetCamera()
        {
            if (_uiCamera != null)
            {
                return;
            }
            if (_cameraManager == null)
            {
                DiContainer tempDicontainer = GetSceneContainer();
                if (tempDicontainer != null && tempDicontainer.HasBinding<CameraManager>())
                {
                    _cameraManager = tempDicontainer.Resolve<CameraManager>() as CameraManager;
                }
            }

            if (_cameraManager != null)
                _uiCamera = _cameraManager.GetCameraByTag(Tags.UICAMERA);

            if (_uiCamera != null)
            {
                canvas_Fixed.renderMode = RenderMode.ScreenSpaceCamera;
                canvas_Normal.renderMode = RenderMode.ScreenSpaceCamera;
                canvas_PopUp.renderMode = RenderMode.ScreenSpaceCamera;
                canvas_Message.renderMode = RenderMode.ScreenSpaceCamera;

                canvas_Fixed.worldCamera = _uiCamera;
                canvas_Normal.worldCamera = _uiCamera;
                canvas_PopUp.worldCamera = _uiCamera;
                canvas_Message.worldCamera = _uiCamera;

                // System dialogs always on top, above scene transition
                canvas_System.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas_System.sortingOrder = short.MaxValue;
                //canvas_System.worldCamera = _uiCamera;
            }
            else
            {
                canvas_Fixed.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas_Normal.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas_PopUp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas_Message.renderMode = RenderMode.ScreenSpaceOverlay;

                canvas_System.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas_System.sortingOrder = short.MaxValue;
            }
        }

        public Canvas GetCanvas(Dialog.DialogType _dialogType)
        {
            Canvas tempCanvas = null;
            switch (_dialogType)
            {
                case Dialog.DialogType.Fixed:
                    tempCanvas = canvas_Fixed;
                    break;
                case Dialog.DialogType.Normal:
                    tempCanvas = canvas_Normal;
                    break;
                case Dialog.DialogType.PopUp:
                    tempCanvas = canvas_PopUp;
                    break;
                case Dialog.DialogType.Message:
                    tempCanvas = canvas_Message;
                    break;
                case Dialog.DialogType.System:
                    tempCanvas = canvas_System;
                    break;
                default:break;
            }
            return tempCanvas;
        }

        /// <summary>
        /// 获得显示中的最上层dialog
        /// </summary>
        /// <returns></returns>
        public Dialog  GetShowPopMaxDialog()
        {
            List<Dialog> tempDialogs = new List<Dialog>();
            Dictionary<string, Dialog> tempDialogDic;
            for (int i = dialogDicList.Count - 1; i > 0; --i)
            {
                tempDialogDic = dialogDicList[i];
                foreach (Dialog item in tempDialogDic.Values)
                {
                    if (item.gameObject.activeSelf)
                    {
                        //正在关闭或者以关闭dialog不算入内
                        if (item.IsHiding() || item.IsHidden())
                        {
                            continue;
                        }
                        if (item as ResourcesGetDialog)
                        {
                            continue;
                        }
                        if (item as WeakConnectionDialog)
                        {
                            continue;
                        }
                        if (item as MessageConfirmDialog)
                        {
                            continue;
                        }
                        tempDialogs.Add(item);
                    }
                }
            }
            Dialog tempPopMaxDialog = null;
            for (int i = 0; i < tempDialogs.Count; i++)
            {
                Dialog tempDialog = tempDialogs[i];
                if (tempPopMaxDialog == null)
                {
                    tempPopMaxDialog = tempDialog;
                }
                else if((int)(tempDialog._dialogType) > (int)(tempPopMaxDialog._dialogType))
                {
                    tempPopMaxDialog = tempDialog;
                }
            }
            return tempPopMaxDialog;
        }

        /// <summary>
        /// 是否有活动的窗口
        /// </summary>
        /// <returns></returns>
        public bool HasActiveDialogs()
        {
            Dictionary<string, Dialog> tempDialogDic;
            for (int i = dialogDicList.Count - 1; i > 0; --i)
            {
                tempDialogDic = dialogDicList[i];
                foreach (Dialog item in tempDialogDic.Values)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    if (item.gameObject.activeSelf)
                    {
                        if (item as ResourcesGetDialog)
                        {
                            continue;
                        }
                        if (item as WeakConnectionDialog)
                        {
                            continue;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
    }
}*/
