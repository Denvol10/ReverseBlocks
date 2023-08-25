using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReverseBlocks.Infrastructure;

namespace ReverseBlocks.ViewModels
{
    internal class MainWindowViewModel : Base.ViewModel
    {
        private RevitModelForfard _revitModel;

        internal RevitModelForfard RevitModel
        {
            get => _revitModel;
            set => _revitModel = value;
        }

        #region Заголовок
        private string _title = "Изменить пространственную ориентацию блока";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }
        #endregion

        #region Блоки пролетного строения
        private string _blockElementIds;
        public string BlockElementIds
        {
            get => _blockElementIds;
            set => Set(ref _blockElementIds, value);
        }
        #endregion

        #region Развернут ли блок
        private bool _isReversed = Properties.Settings.Default.IsReversed;
        public bool IsReversed
        {
            get => _isReversed;
            set => Set(ref _isReversed, value);
        }
        #endregion

        #region Отзеркален ли блок
        private bool _isMirrored = Properties.Settings.Default.IsMirrored;
        public bool IsMirrored
        {
            get => _isMirrored;
            set => Set(ref _isMirrored, value);
        }
        #endregion

        #region Обращен ли блок
        private bool _isTurned = Properties.Settings.Default.IsTurned;
        public bool IsTurned
        {
            get => _isTurned;
            set => Set(ref _isTurned, value);
        }
        #endregion

        #region Команды

        #region Получение блоков пролетного строения
        public ICommand GetBlockElementsCommand { get; }

        private void OnGetBlockElementsCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetBlockElementsBySelection();
            BlockElementIds = RevitModel.BlockElementIds;
            RevitCommand.mainView.ShowDialog();
        }
        
        private bool CanGetBlockElementsCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Изменить оринтацию блока
        public ICommand ChangeBlocksOrientationCommand { get; }

        private void OnChangeBlocksOrientationCommandExecuted(object parameter)
        {
            RevitModel.ChangeBlocksOrientation(IsReversed, IsMirrored, IsTurned);
        }

        private bool CanChangeBlocksOrientationCommandExecute(object parameter)
        {
            if (string.IsNullOrEmpty(_blockElementIds))
                return false;

            return true;
        }
        #endregion

        #region Закрыть окно
        public ICommand CloseWindowCommand { get; }

        private void OnCloseWindowCommandExecuted(object parameter)
        {
            SaveSettings();
            RevitCommand.mainView.Close();
        }

        private bool CanCloseWindowCommandExecute(object parameter)
        {
            return true;
        }
        #endregion


        #endregion

        private void SaveSettings()
        {
            Properties.Settings.Default.BlockElementIds = BlockElementIds;
            Properties.Settings.Default.IsReversed = IsReversed;
            Properties.Settings.Default.IsMirrored = IsMirrored;
            Properties.Settings.Default.IsTurned = IsTurned;
            Properties.Settings.Default.Save();
        }


        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            #region Инициализация свойств из Settings

            #region Инициализация блоков
            if (!(Properties.Settings.Default.BlockElementIds is null))
            {
                string blockElemIdsInSettings = Properties.Settings.Default.BlockElementIds;
                if (RevitModel.IsBlocksExistInModel(blockElemIdsInSettings) && !string.IsNullOrEmpty(blockElemIdsInSettings))
                {
                    BlockElementIds = blockElemIdsInSettings;
                    RevitModel.GetBlocksBySettings(blockElemIdsInSettings);
                }
            }
            #endregion

            #endregion

            #region Команды
            GetBlockElementsCommand = new LambdaCommand(OnGetBlockElementsCommandExecuted, CanGetBlockElementsCommandExecute);
            ChangeBlocksOrientationCommand = new LambdaCommand(OnChangeBlocksOrientationCommandExecuted, CanChangeBlocksOrientationCommandExecute);
            CloseWindowCommand = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);
            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
